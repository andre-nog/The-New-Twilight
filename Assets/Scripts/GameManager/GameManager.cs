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

    // Expostos pra InventoryManager/EquipmentManager (RestoreState) e SaveSystem
    // (LoadInto) — GameManager é o único lugar com essas referências Inspector-wired,
    // então managers plugáveis as leem daqui em vez de precisar da própria cópia.
    public ItemDatabaseSO ItemDatabase => itemDatabase;
    public ExpManager ExpManager => expManager;

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

        // GameManager é DontDestroyOnLoad — Start() só roda uma vez, na primeira cena.
        // Sem isso, um reload de cena (usado pelo pipeline de load pra reconstruir o
        // mundo) deixa SkillBarUI/InventoryDragController/SkillDragController/expManager
        // apontando pra objetos antigos destruídos, quebrando HUD e drag-and-drop.
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;
    }

    // Setado por EnterWorld antes do reload, consumido uma vez em FinishEnteringWorld
    // depois que a cena termina de recarregar. null = não há reload pendente (boot
    // frio já cai direto em FinishEnteringWorld via Start()).
    private string pendingSlotToApply;

    private void Start()
    {
        // Boot: a própria engine já fez o "teardown" (carregou a cena do zero antes
        // de qualquer script rodar) — só falta o bootstrap. pendingSlotToApply começa
        // null (nenhuma UI de "Continuar" existe ainda), então o jogo sempre nasce
        // limpo; carregar um save no boot seria uma chamada explícita a EnterWorld
        // em algum ponto futuro (menu), não o padrão de hoje.
        FinishEnteringWorld();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FinishEnteringWorld();
    }

    // Único ponto que combina "bootstrap" com "aplicar save" — Start() (boot) e
    // OnSceneLoaded() (reload) convergem aqui, então não existe um segundo caminho
    // de carregamento em paralelo.
    private void FinishEnteringWorld()
    {
        InitializeSceneBootstrap();

        // Sempre um dos dois, nunca implícito — nenhum manager decide seu próprio
        // estado de gameplay em Awake/OnEnable/Start (ver ISaveParticipant); é este
        // ponto único que diz "Novo Jogo" ou "Load" depois que o bootstrap terminou.
        if (pendingSlotToApply != null)
            SaveSystem.LoadInto(pendingSlotToApply);
        else
            SaveSystem.InitializeNewGame();

        pendingSlotToApply = null;
    }

    // Reconstrói tudo que era um one-shot em Start() — precisa rodar de novo após
    // CADA carregamento de cena (boot inicial e qualquer reload posterior), não só
    // na primeira vez, senão os singletons scene-local recém-recriados ficam sem
    // seus consumidores globais reconectados.
    private void InitializeSceneBootstrap()
    {
        // Antes de SkillBarUI: a barra só semeia slots com skills já aprendidas, então
        // a progressão precisa existir primeiro. EnsureCreated() é idempotente pra
        // singletons DontDestroyOnLoad (SkillProgression) — não-ops em reloads seguintes.
        SkillProgression.EnsureCreated();
        SkillBarUI.EnsureCreated();
        InventoryDragController.EnsureCreated();
        SkillDragController.EnsureCreated();
        WorldItemRegistry.EnsureCreated();
        expManager = FindAnyObjectByType<ExpManager>();

        // virtualCamera/playerRespawnPoint são referências scene-local atribuídas no
        // Inspector — mesmo problema do expManager: um reload destrói o objeto antigo
        // e o campo fica "fake-null". Sem re-resolver aqui, a câmera nunca realinha
        // com o player novo (o mundo parece "sumir"/inimigos ficam fora de quadro,
        // ainda que o gameplay continue normal) e um respawn de morte cai em
        // Vector3.zero em vez do ponto correto.
        virtualCamera = FindAnyObjectByType<CinemachineCamera>();
        playerRespawnPoint = PlayerRespawnPoint.Instance != null ? PlayerRespawnPoint.Instance.transform : null;
    }

    // Ponto único de entrada pra "recarregar o mundo": teardown (reload de cena,
    // destrói tudo scene-local — inimigos, spawners, managers scene-local, UI) ->
    // rebuild (InitializeSceneBootstrap, via OnSceneLoaded) -> aplica o slot pedido
    // (ou nenhum, pra "Novo Jogo"). Todo carregamento passa por aqui — F9 e qualquer
    // UI futura de "Carregar"/"Novo Jogo" chamam isto, nunca LoadGame() direto. Isso
    // também é o que restaura o mundo (inimigos voltam pros spawners na posição
    // autorada, com stats corretos) sem precisar serializar nada sobre eles — ver
    // Enemy_Health/EnemySpawner, que já regeneram do zero em qualquer Awake/Start novo.
    public void EnterWorld(string slotToApply)
    {
        pendingSlotToApply = slotToApply;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // F5/F9/F6 lêem o teclado direto (Keyboard.current) em vez de entrar no Input
    // Actions asset — é debug-only, não vale adicionar bindings pra isso. Compilados
    // fora de builds reais: em release não existe quicksave/quickload pra scummar
    // (F9-undo de uma compra ruim, etc.), zero custo em runtime fora do Editor.
#if UNITY_EDITOR
    private void Update()
    {
        if (Keyboard.current == null)
            return;

        // Slot dedicado pro atalho — nunca é slot1/slot2/slot3 (esses são pra
        // progresso "de verdade"), então quicksave/quickload de iteração nunca
        // sobrescreve um save deliberado.
        if (Keyboard.current.f5Key.wasPressedThisFrame)
            SaveGame(SaveService.DebugSlot);

        if (Keyboard.current.f9Key.wasPressedThisFrame)
            EnterWorld(SaveService.DebugSlot);

        // Reconstrói o mundo do zero SEM aplicar nenhum save — útil pra testar que
        // inimigos puxados/mortos voltam certo (ver Fase 5) sem precisar de um
        // save válido.
        if (Keyboard.current.f6Key.wasPressedThisFrame)
            EnterWorld(null);
    }
#endif

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Chamado tanto no boot (player inicial da cena) quanto em todo respawn — a nova
    // instância precisa ser re-vinculada em tudo que a câmera/HUD guardavam como
    // referência fixa da instância antiga.
    public void RegisterPlayer(Transform player)
    {
        Player = player;

        // Chamado a partir do Awake() do player — que roda ANTES do evento
        // sceneLoaded (Unity só dispara sceneLoaded depois que TODO Awake/OnEnable
        // da cena nova termina). Num reload, InitializeSceneBootstrap ainda não
        // re-resolveu virtualCamera nesse ponto, então essa chamada precisa se
        // virar sozinha em vez de confiar na ordem — senão a câmera falha
        // silenciosamente sempre na primeira chamada de cada reload.
        if (virtualCamera == null)
            virtualCamera = FindAnyObjectByType<CinemachineCamera>();

        if (virtualCamera != null)
            virtualCamera.Target.TrackingTarget = player;

        PlayerSkillManager skillManager = player.GetComponent<PlayerSkillManager>();

        if (skillManager != null)
        {
            // Garante slots construídos antes do Rebind ler GetSkillAt — Awake()
            // de PlayerSkillManager pode ainda não ter rodado nesta mesma instância (ver
            // comentário em PlayerSkillManager.EnsureSlotsBuilt).
            skillManager.EnsureSlotsBuilt();
            SkillBarUI.Rebind(skillManager);
        }
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

    // Wrapper fino — toda a orquestração de save/load vive em SaveSystem (separado
    // do GameManager de propósito: aqui é só a raiz de composição de mundo/cena).
    // Mantido público porque F5 (debug, abaixo) e uma futura UI de "Salvar" chamam
    // por aqui, nunca SaveSystem diretamente.
    public void SaveGame(string slot) => SaveSystem.Save(slot);

    // Usado por SaveSystem.LoadInto — resolve o classId salvo de volta pro asset,
    // usando knownClasses[] (Inspector-wired aqui, não em StatsManager/SaveSystem).
    public ClassDefinitionSO FindClassById(string id)
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
