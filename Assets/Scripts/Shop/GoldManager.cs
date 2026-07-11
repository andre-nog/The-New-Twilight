using System;
using System.Collections;
using UnityEngine;

// DTO de save deste manager — ver ISaveParticipant/SaveSystem.
[Serializable]
public class GoldSave
{
    public int gold;
}

// Fonte única de verdade pro ouro do jogador em runtime. Mesmo convencionamento
// de singleton usado por QuestManager/InventoryManager (Instance simples, sem
// DontDestroyOnLoad — só GameManager persiste entre cenas).
public class GoldManager : MonoBehaviour, ISaveParticipant
{
    public string SaveKey => "gold";
    public int SchemaVersion => 1;
    public int Order => 0;

    public static GoldManager Instance;

    // Estático pra UI (display de ouro, janela da loja) poder se inscrever em
    // OnEnable sem depender da ordem de inicialização do singleton.
    public static event Action<int> OnGoldChanged;

    [SerializeField] private int startingGold = 100;

    public int CurrentGold { get; private set; }

    // Mesmo padrão de assinatura de ExpManager pro OnMonsterDefeated: cacheado num
    // campo pra +=/-= sempre referenciarem a mesma instância de delegate.
    private Enemy_Health.MonsterDefeated onMonsterDefeatedHandler;

    private static readonly Color GoldPopupColor = new(1f, 0.85f, 0.2f);

    // Mesmo raciocínio do XpPopupDelay em ExpManager: dano precisa nascer sozinho
    // primeiro, o popup de ouro espera um instante pra não competir visualmente
    // com o número de dano do golpe que matou o inimigo.
    private const float GoldPopupDelay = 0.15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SaveRegistry.Register(this);
    }

    private void OnEnable()
    {
        onMonsterDefeatedHandler = (exp, gold, archetype, position) =>
        {
            AddGold(gold);

            if (gold > 0)
                StartCoroutine(ShowGoldPopupDelayed(gold, archetype, position));
        };

        Enemy_Health.OnMonsterDefeated += onMonsterDefeatedHandler;
    }

    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= onMonsterDefeatedHandler;
    }

    private IEnumerator ShowGoldPopupDelayed(int gold, EnemyArchetypeSO archetype, Vector3 position)
    {
        yield return new WaitForSeconds(GoldPopupDelay);

        Vector3 offset = archetype != null ? archetype.goldTextOffset : Vector3.up * 0.25f;

        if (DamageManager.Instance != null)
            DamageManager.Instance.CreateRewardPopup(position + offset, $"+{gold}g", GoldPopupColor);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        SaveRegistry.Unregister(this);
    }

    // Chamado só pelo SaveSystem, só em Novo Jogo — nunca em Start(), pra não
    // competir de ordem com RestoreState (ver ISaveParticipant).
    public void InitializeNewGame()
    {
        CurrentGold = startingGold;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public bool SpendGold(int amount)
    {
        if (amount <= 0)
            return true;

        if (amount > CurrentGold)
            return false;

        CurrentGold -= amount;
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }

    public void AddGold(int amount)
    {
        if (amount <= 0)
            return;

        CurrentGold += amount;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    public string CaptureState()
    {
        return JsonUtility.ToJson(new GoldSave { gold = CurrentGold });
    }

    public void RestoreState(string json, int schemaVersion)
    {
        GoldSave save = JsonUtility.FromJson<GoldSave>(json);

        CurrentGold = Mathf.Max(0, save != null ? save.gold : 0);
        OnGoldChanged?.Invoke(CurrentGold);
    }
}
