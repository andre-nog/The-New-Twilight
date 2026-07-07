using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Composição raiz mínima do jogo — não é DI, só o lugar único que sabe "quem é
// o player" e garante ordem explícita de bootstrap (SkillBarUI deixa de se
// autoconstruir via [RuntimeInitializeOnLoadMethod] e passa a ser chamada daqui).
// DefaultExecutionOrder(-200) roda antes do StatsManager (-100), então quem
// precisar de GameManager.Instance no próprio Awake já o encontra pronto.
[DefaultExecutionOrder(-200)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public enum GameState
    {
        Playing,
        Dead
    }

    [Header("Save/Load")]
    [Tooltip("Lookup id -> ItemSO usado para resolver inventário/equipamento salvos.")]
    [SerializeField] private ItemDatabaseSO itemDatabase;

    [Tooltip("Classes existentes no jogo — usado só para resolver o classId salvo de volta a um ClassDefinitionSO.")]
    [SerializeField] private ClassDefinitionSO[] knownClasses;

    [Header("Player")]
    [Tooltip("Instanciado no respawn (Destroy + Instantiate) — garante estado 100% limpo (Animator, cast em andamento, etc.) em vez de reativar a mesma instância.")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerRespawnPoint;
    [SerializeField] private CinemachineCamera virtualCamera;

    public GameState State { get; private set; } = GameState.Playing;
    public event Action<GameState> OnGameStateChanged;

    // Registrado pelo PlayerHealth.Awake — substitui GameObject.FindGameObjectWithTag("Player")
    // nos consumidores que hoje buscam o player por tag.
    public Transform Player { get; private set; }

    private ExpManager expManager;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        Instance = this;
    }

    private void Start()
    {
        // Antes de SkillBarUI: a barra só semeia slots com skills já aprendidas, então
        // a progressão precisa existir primeiro.
        SkillProgression.EnsureCreated();
        SkillBarUI.EnsureCreated();
        InventoryDragController.EnsureCreated();
        SkillDragController.EnsureCreated();
        expManager = FindAnyObjectByType<ExpManager>();
    }

    // F5/F9 lêem o teclado direto (Keyboard.current) em vez de entrar no Input
    // Actions asset — é debug-only, não vale adicionar bindings pra isso.
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.f5Key.wasPressedThisFrame)
            SaveGame();

        if (Keyboard.current.f9Key.wasPressedThisFrame)
            LoadGame();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Chamado tanto no boot (player inicial da cena) quanto em todo respawn — a nova
    // instância precisa ser re-vinculada em tudo que a câmera/HUD guardavam como
    // referência fixa da instância antiga.
    public void RegisterPlayer(Transform player)
    {
        Player = player;

        if (virtualCamera != null)
            virtualCamera.Target.TrackingTarget = player;

        PlayerSkillManager skillManager = player.GetComponent<PlayerSkillManager>();

        if (skillManager != null)
            SkillBarUI.Rebind(skillManager);
    }

    // Destrói o player atual (se existir) e instancia um novo a partir do prefab —
    // garante Animator/cast/estado de combate 100% limpos, em vez de reativar a
    // mesma instância e arriscar retomar uma animação/ação que ficou "congelada"
    // no meio quando o player morreu.
    public void RespawnPlayer()
    {
        if (Player != null)
            Destroy(Player.gameObject);

        StatsManager.Instance.FullHeal();
        StatsManager.Instance.RestoreFullMana();

        if (playerPrefab == null)
        {
            Debug.LogWarning("GameManager: playerPrefab não atribuído — não há o que instanciar no respawn.");
            return;
        }

        Vector3 spawnPosition = playerRespawnPoint != null ? playerRespawnPoint.position : Vector3.zero;
        Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        // Awake() da instância nova chama RegisterPlayer sozinho (PlayerHealth.Awake).
    }

    public void SetState(GameState newState)
    {
        if (State == newState)
            return;

        State = newState;
        OnGameStateChanged?.Invoke(newState);
    }

    // Seam para carregamento de cena (dungeons, menus) — hoje sem consumidor além
    // de debug, mas resolve de antemão "o que sobrevive à troca de cena": este
    // objeto sim (DontDestroyOnLoad), spawners/inimigos da cena antiga não.
    public void LoadSceneAsync(string sceneName, Action onLoaded = null)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);

        if (operation == null)
            return;

        if (onLoaded != null)
            operation.completed += _ => onLoaded();
    }

    public void SaveGame()
    {
        if (Player == null || StatsManager.Instance == null)
            return;

        SaveData data = new()
        {
            player = new PlayerSave
            {
                classId = StatsManager.Instance.CurrentClass != null ? StatsManager.Instance.CurrentClass.Id : null,
                level = StatsManager.Instance.level,
                currentExp = expManager != null ? expManager.currentExp : 0,
                currentHealth = StatsManager.Instance.currentHealth,
                currentMana = StatsManager.Instance.currentMana,
                position = Player.position
            },
            inventory = InventoryManager.Instance != null ? InventoryManager.Instance.GetState() : new(),
            equipment = EquipmentManager.Instance != null ? EquipmentManager.Instance.GetState() : new(),
            quests = QuestManager.Instance != null ? QuestManager.Instance.GetState() : new()
        };

        SaveService.Save(data);
        Debug.Log("Jogo salvo.");
    }

    // Ordem importa: classe → level/exp → recalc (já dentro de SetClass/SetLevel) →
    // equipamento (que já faz unequip-all antes de reaplicar) → inventário →
    // vitals (clampam contra o MaxHealth/MaxMana já corretos) → posição.
    public void LoadGame()
    {
        if (!SaveService.TryLoad(out SaveData data))
            return;

        if (StatsManager.Instance == null || Player == null)
            return;

        ClassDefinitionSO savedClass = FindClassById(data.player.classId);

        if (savedClass != null)
        {
            StatsManager.Instance.SetClass(savedClass);

            PlayerSkillManager skillManager = Player.GetComponent<PlayerSkillManager>();

            if (skillManager != null)
                skillManager.RebuildLoadout();
        }

        StatsManager.Instance.SetLevel(data.player.level);

        if (expManager != null)
            expManager.currentExp = data.player.currentExp;

        if (EquipmentManager.Instance != null)
            EquipmentManager.Instance.ApplyState(data.equipment, itemDatabase);

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.ApplyState(data.inventory, itemDatabase);

        StatsManager.Instance.SetVitals(data.player.currentHealth, data.player.currentMana);

        Player.position = data.player.position;

        if (expManager != null)
            expManager.UpdateUI();

        if (QuestManager.Instance != null)
            QuestManager.Instance.ApplyState(data.quests);

        Debug.Log("Jogo carregado.");
    }

    private ClassDefinitionSO FindClassById(string id)
    {
        if (string.IsNullOrEmpty(id) || knownClasses == null)
            return null;

        foreach (ClassDefinitionSO candidate in knownClasses)
        {
            if (candidate != null && candidate.Id == id)
                return candidate;
        }

        return null;
    }
}
