using TMPro;
using UnityEngine;

// Mesmo formato da QuestWindow: singleton, CanvasGroup show/hide, fecha
// sozinha se o jogador se mover (openPlayerPosition/MoveCloseThreshold) e se
// registra no CancelManager pra ESC funcionar. Pool fixo de slots (como a
// Skill Bar) em vez de Instantiate/prefab — Open() preenche os N primeiros e
// esconde o resto.
public class ShopWindow : MonoBehaviour, ICancelable
{
    public static ShopWindow Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TMP_Text shopNameText;
    [SerializeField] private ShopSlot[] slots;

    // Retângulo do próprio painel visível ("Shop Panel") — usado só pra
    // reposicionar a janela ao lado da grade de inventário no modo venda.
    [SerializeField] private RectTransform panelRect;

    private ShopSO currentShop;
    private bool isOpen;
    private Vector3 openPlayerPosition;

    // Espaço entre a ShopWindow e a Window Panel do inventário quando as duas
    // ficam lado a lado no modo venda.
    private const float VendorGap = 16f;

    // Mesmo epsilon usado em QuestWindow/PlayerMovement.FollowPath pra "chegou".
    private const float MoveCloseThreshold = 0.1f;

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
        CancelManager.Instance.Register(this);
        Close();
    }

    public void Configure(CanvasGroup group, TMP_Text shopName, ShopSlot[] slotArray, RectTransform panel)
    {
        canvasGroup = group;
        shopNameText = shopName;
        slots = slotArray;
        panelRect = panel;
    }

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);

        if (Instance == this)
            Instance = null;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        if (GameManager.Instance == null || GameManager.Instance.Player == null)
            return;

        float moved = Vector2.Distance(GameManager.Instance.Player.position, openPlayerPosition);

        if (moved > MoveCloseThreshold)
            Close();
    }

    public void Open(ShopSO shop)
    {
        if (shop == null)
            return;

        currentShop = shop;
        shopNameText.text = shop.shopName;
        PopulateSlots(shop);
        ShowWindow();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OpenInventory();
            InventoryManager.Instance.EnterShopMode();
            PositionBesideInventory();
        }
    }

    // Padrão vendedor: a loja fica à esquerda, a grade de inventário (já
    // encolhida pelo EnterShopMode, sem o painel de equipamento) fica à
    // direita. Centraliza a caixa delimitadora do PAR na tela — os centros
    // dos dois painéis não ficam simétricos entre si a menos que tenham a
    // mesma largura (não têm).
    private void PositionBesideInventory()
    {
        RectTransform inventoryPanel = InventoryManager.Instance.WindowPanel;

        if (inventoryPanel == null || panelRect == null)
            return;

        float shopWidth = panelRect.rect.width;
        float inventoryWidth = inventoryPanel.rect.width;
        float total = shopWidth + VendorGap + inventoryWidth;

        inventoryPanel.anchoredPosition = new Vector2(total / 2f - inventoryWidth / 2f, inventoryPanel.anchoredPosition.y);
        panelRect.anchoredPosition = new Vector2(-(total / 2f - shopWidth / 2f), panelRect.anchoredPosition.y);
    }

    private void PopulateSlots(ShopSO shop)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (i < shop.entries.Count)
            {
                ShopSO.Entry entry = shop.entries[i];
                slots[i].gameObject.SetActive(true);
                slots[i].Setup(entry.item, entry.price, this);
            }
            else
            {
                slots[i].gameObject.SetActive(false);
            }
        }
    }

    public void OnSlotClicked(ItemSO item, int price)
    {
        if (PurchaseConfirmWindow.Instance != null)
            PurchaseConfirmWindow.Instance.Open(item, price, PurchaseConfirmWindow.TransactionType.Buy);
    }

    // Usado pelo ItemSlot quando o jogador clica um item do inventário em modo
    // loja — preço de venda deriva do valor base do item e da margem da loja
    // atual (arredondado pra baixo).
    public int GetSellPrice(ItemSO item)
    {
        if (currentShop == null || item == null)
            return 0;

        return Mathf.FloorToInt(item.value * currentShop.sellMultiplier);
    }

    private void ShowWindow()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isOpen = true;

        if (GameManager.Instance != null && GameManager.Instance.Player != null)
            openPlayerPosition = GameManager.Instance.Player.position;
    }

    public void Close()
    {
        isOpen = false;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        if (PurchaseConfirmWindow.Instance != null)
            PurchaseConfirmWindow.Instance.Close();

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.ExitShopMode();
            InventoryManager.Instance.CloseInventory();
        }

        if (panelRect != null)
            panelRect.anchoredPosition = new Vector2(0f, panelRect.anchoredPosition.y);
    }

    public bool CanCancel()
    {
        return isOpen;
    }

    public void Cancel()
    {
        Close();
    }

    public int Priority => 150;
}
