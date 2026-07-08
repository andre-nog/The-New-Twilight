using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Janela de confirmação de compra — mesmo estilo/CanvasGroup das outras
// janelas, prioridade de ESC maior que a ShopWindow (fecha ela primeiro,
// deixando a loja aberta por baixo). Sem close-on-move: já é filha da loja,
// que se fecha sozinha quando o jogador anda (e fecha esta junto).
public class PurchaseConfirmWindow : MonoBehaviour, ICancelable
{
    public enum TransactionType
    {
        Buy,
        Sell
    }

    public static PurchaseConfirmWindow Instance { get; private set; }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text unitPriceText;
    [SerializeField] private TMP_Text totalPriceText;
    [SerializeField] private GameObject quantityGroup;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private CanvasGroup okButtonGroup;
    [SerializeField] private TMP_Text warningText;

    private ItemSO currentItem;
    private TransactionType transactionType;
    private int unitPrice;
    private int quantity;
    private int maxQuantity;
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

    private void Start()
    {
        CancelManager.Instance.Register(this);
        Close();
    }

    public void Configure(
        CanvasGroup group,
        Image icon,
        TMP_Text itemName,
        TMP_Text unitPriceLabel,
        TMP_Text totalPrice,
        GameObject quantitySelector,
        TMP_Text quantityValue,
        CanvasGroup okGroup,
        TMP_Text warning)
    {
        canvasGroup = group;
        itemIcon = icon;
        itemNameText = itemName;
        unitPriceText = unitPriceLabel;
        totalPriceText = totalPrice;
        quantityGroup = quantitySelector;
        quantityText = quantityValue;
        okButtonGroup = okGroup;
        warningText = warning;
    }

    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);

        if (Instance == this)
            Instance = null;
    }

    public void Open(ItemSO item, int price, TransactionType type)
    {
        if (item == null)
            return;

        currentItem = item;
        unitPrice = price;
        transactionType = type;

        // Vender pré-preenche com o total possuído (não só o do slot clicado)
        // e trava o incremento nesse teto — comprar continua sem limite,
        // igual antes.
        maxQuantity = type == TransactionType.Sell
            ? Mathf.Max(1, InventoryManager.Instance.GetQuantity(item))
            : int.MaxValue;
        quantity = type == TransactionType.Sell ? maxQuantity : 1;

        itemIcon.sprite = item.itemSprite;
        itemIcon.enabled = item.itemSprite != null;
        itemNameText.text = (type == TransactionType.Sell ? "Sell: " : "Buy: ") + item.itemName;
        unitPriceText.text = $"Unit: {price}";

        quantityGroup.SetActive(item.stackable);
        RefreshTotal();
        ShowWindow();
    }

    public void OnIncrementClicked()
    {
        quantity = Mathf.Min(quantity + 1, maxQuantity);
        RefreshTotal();
    }

    public void OnDecrementClicked()
    {
        quantity = Mathf.Max(1, quantity - 1);
        RefreshTotal();
    }

    private void RefreshTotal()
    {
        if (quantityText != null)
            quantityText.text = quantity.ToString();

        long total = (long)unitPrice * quantity;
        totalPriceText.text = total.ToString();

        // Vender não trava no ouro (não há custo pro jogador) — só compra
        // precisa desse gate; a quantidade já vem travada em maxQuantity.
        bool canProceed = transactionType == TransactionType.Sell
            || (GoldManager.Instance != null && GoldManager.Instance.CurrentGold >= total);

        okButtonGroup.interactable = canProceed;
        okButtonGroup.alpha = canProceed ? 1f : 0.5f;

        if (warningText != null)
            warningText.gameObject.SetActive(!canProceed);

        if (warningText != null && !canProceed)
            warningText.text = "Insufficient gold";
    }

    public void OnOkClicked()
    {
        if (!okButtonGroup.interactable)
            return;

        if (currentItem == null || GoldManager.Instance == null || InventoryManager.Instance == null)
            return;

        long total = (long)unitPrice * quantity;

        if (transactionType == TransactionType.Buy)
        {
            if (GoldManager.Instance.CurrentGold < total)
                return;

            if (!InventoryManager.Instance.CanFit(currentItem, quantity))
            {
                if (warningText != null)
                {
                    warningText.gameObject.SetActive(true);
                    warningText.text = "Inventory full";
                }

                return;
            }

            // Safe to cast: the affordability check above already proved
            // total <= CurrentGold <= int.MaxValue.
            GoldManager.Instance.SpendGold((int)total);
            InventoryManager.Instance.AddItem(currentItem, quantity);
        }
        else
        {
            // A quantidade possuída pode mudar entre Open() (pré-preenchimento)
            // e este clique — nada bloqueia raycasts pro resto da grade
            // enquanto este painel pequeno está aberto. Revalida na hora,
            // igual compra revalida CanFit/ouro em vez de confiar no que foi
            // cacheado no Open().
            if (!InventoryManager.Instance.RemoveItem(currentItem, quantity))
            {
                if (warningText != null)
                {
                    warningText.gameObject.SetActive(true);
                    warningText.text = "You don't have that many";
                }

                return;
            }

            GoldManager.Instance.AddGold((int)System.Math.Min(total, int.MaxValue));
        }

        Close();
    }

    public void OnCancelClicked()
    {
        Close();
    }

    private void ShowWindow()
    {
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        isOpen = true;
    }

    public void Close()
    {
        isOpen = false;
        currentItem = null;

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    public bool CanCancel()
    {
        return isOpen;
    }

    public void Cancel()
    {
        Close();
    }

    public int Priority => 200;
}
