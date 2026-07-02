using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
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



    private void Start()
    {
       inventoryManager = InventoryManager.Instance;
       equipmentManager = EquipmentManager.Instance;

        if (selectedShader != null)
            selectedShader.SetActive(false);
        
    }

    public int AddItem(ItemSO item, int quantity)
    {
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
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            OnLeftClick();
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void ConsumeOneItem()
    {
        quantity--;
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
        isFull = false;

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
        if (item == null)
            return;

        TooltipManager.Instance.Show(item);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance.Hide();
    }
}