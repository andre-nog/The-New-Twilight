using System;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
