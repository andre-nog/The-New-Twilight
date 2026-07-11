using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class EquippedSlot : MonoBehaviour,
    IItemSlot,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler

{
    [Header("Slot")]
    public ItemSO.ItemType acceptedType;

    [Header("UI")]
    [SerializeField] private Image itemImage;

    [Header("Placeholder")]
    [SerializeField] private GameObject placeholder;

    private ItemSO equippedItem;

    // Setada em OnBeginDrag; usada para suprimir o Unequip() que o OnPointerClick
    // ainda dispararia depois de um arrasto (mesmo cancelado).
    private bool wasDragged;

    public ItemSO Item => equippedItem;

    // Implementação explícita: a classe já tem um método IsEmpty() público (usado por
    // EquipmentManager) e C# não permite um método e uma propriedade com o mesmo nome.
    bool IItemSlot.IsEmpty => equippedItem == null;

    public bool CanAccept(ItemSO item) => item != null && CanEquip(item);

    private void Start()
    {
        // Deriva do dado (equippedItem é sempre null aqui — Awake/Start rodam antes
        // de qualquer RestoreState) em vez de forçar "vazio" incondicionalmente: um
        // componente puramente visual nunca deve decidir estado por conta própria,
        // só refletir o que já existe. Ver Equip()/Unequip(), que chamam o mesmo método.
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        bool hasItem = equippedItem != null;

        itemImage.sprite = hasItem ? equippedItem.itemSprite : null;
        itemImage.enabled = hasItem;

        if (placeholder != null)
            placeholder.SetActive(!hasItem);
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
        RefreshVisual();

        return previousItem;
    }

    public void Unequip()
    {
        equippedItem = null;
        RefreshVisual();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (wasDragged)
        {
            wasDragged = false;
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            EquipmentManager.Instance.Unequip(this);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (equippedItem == null || InventoryDragController.Instance == null)
            return;

        wasDragged = true;
        InventoryDragController.Instance.BeginDrag(this, itemImage.sprite, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (InventoryDragController.Instance == null || !InventoryDragController.Instance.IsDragging)
            return;

        InventoryDragController.Instance.UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (InventoryDragController.Instance != null)
            InventoryDragController.Instance.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (InventoryDragController.Instance != null)
            InventoryDragController.Instance.TryDrop(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (equippedItem == null || TooltipManager.Instance == null)
            return;

        if (InventoryDragController.Instance != null && InventoryDragController.Instance.IsDragging)
            return;

        TooltipManager.Instance.ShowItem(equippedItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
