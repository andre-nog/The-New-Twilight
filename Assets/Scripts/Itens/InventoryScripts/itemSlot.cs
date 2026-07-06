using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour,
    IItemSlot,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    // ===== ITEM DATA =====
    public ItemSO item;
    public int quantity;

    [SerializeField]
    private int maxNumberOfItems = 99;

    public bool isFull;

    // ===== ITEM SLOT =====
    [SerializeField]
    private TMP_Text quantityText;

    [SerializeField]
    private Image itemImage;

    // ===== SELECTION =====
    public GameObject selectedShader;
    public bool thisItemSelected;

    private InventoryManager inventoryManager;
    private EquipmentManager equipmentManager;

    // Setada em OnBeginDrag; usada para suprimir o toggle de seleção do OnPointerClick
    // que o EventSystem ainda dispara depois de um arrasto.
    private bool wasDragged;

    public ItemSO Item => item;
    public bool IsEmpty => item == null;
    public bool CanAccept(ItemSO otherItem) => true;

    private void Start()
    {
       inventoryManager = InventoryManager.Instance;
       equipmentManager = EquipmentManager.Instance;

        if (selectedShader != null)
            selectedShader.SetActive(false);

    }

    public int AddItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
            return quantity;

        // Se já existe um item diferente neste slot
        if (this.item != null && this.item != item)
            return quantity;

        // Se o stack já está cheio
        if (isFull)
            return quantity;

        // Atualiza os dados do item
        this.item = item;
        itemImage.sprite = item.itemSprite;
        itemImage.enabled = true;

        if (!item.stackable)
        {
            this.quantity = 1;
            isFull = true;
            quantityText.enabled = false;
            return quantity - 1;
        }

        // Soma a quantidade ao stack
        this.quantity += quantity;

        // Verifica se atingiu o limite
        if (this.quantity >= maxNumberOfItems)
        {
            int extraItems = this.quantity - maxNumberOfItems;

            this.quantity = maxNumberOfItems;
            isFull = true;

            quantityText.text = this.quantity.ToString();
            quantityText.enabled = true;

            return extraItems;
        }

        isFull = false;

        quantityText.text = this.quantity.ToString();
        quantityText.enabled = true;

        return 0;
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
            OnLeftClick();
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (item == null)
            return;

        wasDragged = true;
        InventoryDragController.Instance.BeginDrag(this, itemImage.sprite, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!InventoryDragController.Instance.IsDragging)
            return;

        InventoryDragController.Instance.UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        InventoryDragController.Instance.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventoryDragController.Instance.TryDrop(this);
    }

    // Restaura um slot com quantidade exata já conhecida — usado pelo carregamento
    // de save, que não deve passar pela lógica de "completar stack aos poucos" de
    // AddItem (essa já sabe a quantidade final, não uma quantidade chegando aos poucos).
    public void LoadItem(ItemSO item, int quantity)
    {
        this.item = item;
        this.quantity = quantity;
        isFull = !item.stackable || quantity >= maxNumberOfItems;

        itemImage.sprite = item.itemSprite;
        itemImage.enabled = true;

        if (item.stackable)
        {
            quantityText.text = quantity.ToString();
            quantityText.enabled = true;
        }
        else
        {
            quantityText.enabled = false;
        }
    }

    public void Clear()
    {
        item = null;
        quantity = 0;
        isFull = false;

        itemImage.enabled = false;
        quantityText.enabled = false;

        if (selectedShader != null)
            selectedShader.SetActive(false);

        thisItemSelected = false;
    }

    public void ConsumeOneItem()
    {
        ReduceQuantity(1);
    }

    // Reduz a quantidade em `amount` unidades, limpando o slot se chegar a zero ou
    // menos. Usado pelo merge de stacks no drag-and-drop, onde a quantidade a remover
    // da origem é a diferença entre o que foi movido e a sobra que não coube no destino.
    public void ReduceQuantity(int amount)
    {
        quantity -= amount;
        isFull = false;

        if (quantity <= 0)
        {
            item = null;
            quantity = 0;
            isFull = false;

            itemImage.enabled = false;
            quantityText.enabled = false;

            if (selectedShader != null)
                selectedShader.SetActive(false);

            thisItemSelected = false;
        }
        else
        {
            quantityText.text = quantity.ToString();
        }
    }

    private void OnLeftClick()
    {
        if (item == null)
            return;

        if (thisItemSelected)
        {
            switch (item.itemType)
            {
                case ItemSO.ItemType.Consumable:
                    inventoryManager.UseItem(item);
                    ConsumeOneItem();
                    break;

                case ItemSO.ItemType.Head:
                case ItemSO.ItemType.Body:
                case ItemSO.ItemType.Legs:
                case ItemSO.ItemType.Feet:
                case ItemSO.ItemType.MainHand:
                case ItemSO.ItemType.OffHand:
                case ItemSO.ItemType.Necklace:
                case ItemSO.ItemType.Ring:

                    ItemSO oldItem = equipmentManager.Equip(item);

                    // Não existe slot compatível
                    if (oldItem == item)
                        break;

                    // Equipou em um slot vazio
                    if (oldItem == null)
                    {
                        ConsumeOneItem();
                    }
                    // Houve troca
                    else
                    {
                        SetItem(oldItem);
                    }

                    break;

                case ItemSO.ItemType.Material:
                    Debug.Log("Material não pode ser usado.");
                    break;

                case ItemSO.ItemType.Quest:
                    Debug.Log("Item de missão.");
                    break;
            }

            return;
        }

        inventoryManager.DeselectAllSlots();

        if (selectedShader != null)
            selectedShader.SetActive(true);

        thisItemSelected = true;
    }

    public void SetItem(ItemSO newItem)
    {
        item = newItem;
        quantity = 1;
        isFull = !newItem.stackable;

        itemImage.sprite = newItem.itemSprite;
        itemImage.enabled = true;

        if (newItem.stackable)
        {
            quantityText.text = "1";
            quantityText.enabled = true;
        }
        else
        {
            quantityText.enabled = false;
        }
    }

    private void OnRightClick()
    {
        if (item == null)
            return;

        inventoryManager.DropItem(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (item == null || TooltipManager.Instance == null)
            return;

        if (InventoryDragController.Instance != null && InventoryDragController.Instance.IsDragging)
            return;

        TooltipManager.Instance.ShowItem(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();
    }
}
