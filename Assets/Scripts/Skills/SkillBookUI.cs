using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Abre/fecha o painel do Livro de Skills — mesmo padrão de
// Assets/Scripts/Itens/InventoryScripts/InventoryManager.cs (CanvasGroup pra
// alpha/interactable/blocksRaycasts, ICancelable pra ESC também fechar).
//
// Também coordena a UI de progressão: o contador de pontos e o refresh de todos os
// slots (pips, overlay de travado, botão "+"). Assina OnProgressionChanged só
// enquanto o livro está aberto — nível/aprender/upar mudam o painel na hora.
//
[DefaultExecutionOrder(-50)]
public class SkillBookUI : MonoBehaviour, ICancelable
{
    public static SkillBookUI Instance { get; private set; }

    public CanvasGroup canvasGroup;
    public InputActionReference toggleAction;

    [SerializeField] private SkillBookSlot[] slots;
    [SerializeField] private TMP_Text pointsText;

    private bool isOpen;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Chamado pelo SkillBookCanvasBuilder (edit time) pra gravar as referências dos
    // slots e do contador de pontos — persistem via serialização até o runtime.
    public void Configure(SkillBookSlot[] bookSlots, TMP_Text points)
    {
        slots = bookSlots;
        pointsText = points;
    }

    // toggleAction aponta pra uma InputAction COMPARTILHADA (mesmo asset, não
    // por-instância). Sem essa guarda, um Destroy+Instantiate concorrente (ex.: reload
    // de cena) pode deixar o OnDisable adiado de uma instância antiga desabilitar a
    // action que a instância nova acabou de ligar — mesmo bug já corrigido em
    // PlayerTargeting.cs, mesma solução (guarda de "dono atual").
    private static SkillBookUI activeInstance;

    private void OnEnable()
    {
        activeInstance = this;
        toggleAction.action.Enable();
    }

    private void OnDisable()
    {
        if (activeInstance == this)
            toggleAction.action.Disable();
    }

    private void Start()
    {
        Close();
        CancelManager.Instance.Register(this);
    }

    private void Update()
    {
        if (toggleAction.action.WasPressedThisFrame())
        {
            if (isOpen)
                Close();
            else
                Open();
        }
    }

    public void Open()
    {
        isOpen = true;
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        if (SkillProgression.Instance != null)
        {
            SkillProgression.Instance.OnProgressionChanged -= RefreshAll;
            SkillProgression.Instance.OnProgressionChanged += RefreshAll;
        }

        RefreshAll();
    }

    public void Close()
    {
        isOpen = false;
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (SkillProgression.Instance != null)
            SkillProgression.Instance.OnProgressionChanged -= RefreshAll;
    }

    private void RefreshAll()
    {
        if (slots != null)
        {
            foreach (SkillBookSlot slot in slots)
            {
                if (slot != null)
                    slot.Refresh();
            }
        }

        if (pointsText != null)
        {
            int points = SkillProgression.Instance != null ? SkillProgression.Instance.AvailablePoints : 0;
            pointsText.text = $"Points: {points}";
        }
    }

    public bool CanCancel()
    {
        return isOpen;
    }

    public void Cancel()
    {
        Close();
    }

    public int Priority => 100;

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);

        if (SkillProgression.Instance != null)
            SkillProgression.Instance.OnProgressionChanged -= RefreshAll;

        if (Instance == this)
            Instance = null;
    }
}
