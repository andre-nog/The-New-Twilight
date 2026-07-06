using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    public static TooltipManager Instance;

    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ItemTooltipView itemView;
    [SerializeField] private SkillTooltipView skillView;

    [Header("Position")]
    [SerializeField] private Vector2 mouseOffset = new Vector2(20f, -20f);

    private RectTransform activeRect;

    public void Configure(CanvasGroup canvasGroup, ItemTooltipView itemView, SkillTooltipView skillView)
    {
        this.canvasGroup = canvasGroup;
        this.itemView = itemView;
        this.skillView = skillView;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // O Canvas tem o maior sortingOrder da cena (ver TooltipCanvasBuilder) —
        // então isto precisa ficar sempre falso, senão o tooltip passaria a roubar
        // clique/hover de qualquer UI que ele visualmente sobreponha.
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Start()
    {
        Hide();
    }

    private void Update()
    {
        if (Mouse.current == null || canvasGroup.alpha == 0f || activeRect == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector2 position = mousePos + mouseOffset;

        float width = activeRect.rect.width;
        float height = activeRect.rect.height;

        // Pivot do painel é top-left (0,1): position é a borda SUPERIOR, o painel
        // se estende PRA BAIXO por "height" — borda inferior = position.y - height.

        // Direita
        if (position.x + width > Screen.width)
            position.x = mousePos.x - width - mouseOffset.x;

        // Esquerda
        if (position.x < 0)
            position.x = 0;

        // Baixo: borda inferior passaria do rodapé da tela — espelha pra cima do mouse.
        if (position.y - height < 0)
            position.y = mousePos.y - mouseOffset.y + height;

        // Topo: mesmo espelhado, ainda estoura o topo da tela — gruda na borda superior.
        if (position.y > Screen.height)
            position.y = Screen.height;

        activeRect.position = position;
    }

    public void ShowItem(ItemSO item)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        itemView.Populate(item.GetItemTooltipData());
        itemView.gameObject.SetActive(true);
        skillView.gameObject.SetActive(false);

        activeRect = itemView.PanelRect;
        canvasGroup.alpha = 1f;
    }

    public void ShowSkill(SkillTooltipSource source)
    {
        if (source == null)
        {
            Hide();
            return;
        }

        skillView.Populate(source.GetSkillTooltipData());
        skillView.gameObject.SetActive(true);
        itemView.gameObject.SetActive(false);

        activeRect = skillView.PanelRect;
        canvasGroup.alpha = 1f;
    }

    public void Hide()
    {
        canvasGroup.alpha = 0f;
    }
}
