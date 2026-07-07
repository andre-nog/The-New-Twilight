using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

// Mesmo padrão de ícone + hover-tooltip do ItemSlot, trocando a quantidade
// por um preço e o clique de usar/equipar por abrir a confirmação de compra.
public class ShopSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text priceText;

    private ItemSO item;
    private int price;
    private ShopWindow owner;

    public void Setup(ItemSO item, int price, ShopWindow owner)
    {
        this.item = item;
        this.price = price;
        this.owner = owner;

        icon.sprite = item.itemSprite;
        icon.enabled = item.itemSprite != null;
        priceText.text = price.ToString();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null || TooltipManager.Instance == null)
            return;

        TooltipManager.Instance.ShowItem(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (item == null || owner == null)
            return;

        owner.OnSlotClicked(item, price);
    }
}
