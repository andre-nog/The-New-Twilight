using System;
using System.Collections;
using UnityEngine;

// Fonte única de verdade pro ouro do jogador em runtime. Mesmo convencionamento
// de singleton usado por QuestManager/InventoryManager (Instance simples, sem
// DontDestroyOnLoad — só GameManager persiste entre cenas).
public class GoldManager : MonoBehaviour
{
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
    }

    private void OnEnable()
    {
        onMonsterDefeatedHandler = (exp, gold, archetype, position) =>
        {
            AddGold(gold);

            if (gold > 0)
                StartCoroutine(ShowGoldPopupDelayed(gold, position));
        };

        Enemy_Health.OnMonsterDefeated += onMonsterDefeatedHandler;
    }

    private void OnDisable()
    {
        Enemy_Health.OnMonsterDefeated -= onMonsterDefeatedHandler;
    }

    private IEnumerator ShowGoldPopupDelayed(int gold, Vector3 position)
    {
        yield return new WaitForSeconds(GoldPopupDelay);

        if (DamageManager.Instance != null)
            DamageManager.Instance.CreateRewardPopup(position + Vector3.up * 0.25f, $"+{gold}g", GoldPopupColor);
    }

    private void Start()
    {
        CurrentGold = startingGold;
        OnGoldChanged?.Invoke(CurrentGold);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
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

    public int GetState()
    {
        return CurrentGold;
    }

    // Chamado pelo GameManager.LoadGame — reaplica o valor salvo por cima do
    // startingGold já setado em Start.
    public void ApplyState(int gold)
    {
        CurrentGold = Mathf.Max(0, gold);
        OnGoldChanged?.Invoke(CurrentGold);
    }
}
