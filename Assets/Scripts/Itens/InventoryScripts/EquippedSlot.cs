using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquippedSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler

{
    [Header("Slot")]
    public ItemSO.ItemType acceptedType;

    [Header("UI")]
    [SerializeField] private Image itemImage;

    [Header("Placeholder")]
    [SerializeField] private GameObject placeholder;

    private ItemSO equippedItem;

    private void Start()
    {
        itemImage.enabled = false;

        if (placeholder != null)
            placeholder.SetActive(true);
    }
    public bool CanEquip(ItemSO item)
    {
        return item.itemType == acceptedType;
    }

    public bool HasItem()
    {
        return equippedItem != null;
    }

    public ItemSO GetItem()
    {
        return equippedItem;
    }

    public bool IsEmpty()
    {
        return equippedItem == null;
    }

    public ItemSO Equip(ItemSO item)
    {
        ItemSO previousItem = equippedItem;

        equippedItem = item;

        itemImage.sprite = item.itemSprite;
        itemImage.enabled = true;

        if (placeholder != null)
            placeholder.SetActive(false);

        return previousItem;
    }

    public void Unequip()
    {
        equippedItem = null;

        itemImage.sprite = null;
        itemImage.enabled = false;

        if (placeholder != null)
            placeholder.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            EquipmentManager.Instance.Unequip(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equippedItem == null || TooltipManager.Instance == null)
            return;

        TooltipManager.Instance.Show(equippedItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
