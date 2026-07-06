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
// DefaultExecutionOrder negativo pra Instance existir antes do Awake (ordem padrão,
// 0) de PlayerSkillManager — que consulta GetIconFor ao semear a barra, pra usar o
// mesmo ícone (possivelmente customizado no Inspector) mostrado no Livro.
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

    // Ícone tal como exibido no slot desta skill no Livro — pode ser um sprite
    // customizado atribuído no Inspector, diferente de Skill.icon. Usado pra manter
    // o ícone da barra consistente quando uma skill autoLearnedAtStart é auto-equipada
    // (sem passar pelo drag, que já carrega esse sprite via SkillBookSlot.IconSprite).
    public Sprite GetIconFor(Skill skill)
    {
        if (skill == null || slots == null)
            return null;

        foreach (SkillBookSlot slot in slots)
        {
            if (slot != null && slot.Skill == skill)
                return slot.IconSprite;
        }

        return null;
    }

    // Chamado pelo SkillBookCanvasBuilder (edit time) pra gravar as referências dos
    // slots e do contador de pontos — persistem via serialização até o runtime.
    public void Configure(SkillBookSlot[] bookSlots, TMP_Text points)
    {
        slots = bookSlots;
        pointsText = points;
    }

    private void OnEnable()
    {
        toggleAction.action.Enable();
    }

    private void OnDisable()
    {
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
