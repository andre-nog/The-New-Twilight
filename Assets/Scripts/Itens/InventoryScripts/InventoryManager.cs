using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;
    public CanvasGroup inventoryCanvas;
    public InputActionReference toggleInventory;

    [SerializeField]
    private ItemSlot[] itemSlot;

    [SerializeField]
    private GameObject worldItemPrefab;

    private bool menuActivated;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        toggleInventory.action.Enable();
    }

    private void OnDisable()
    {
        toggleInventory.action.Disable();
    }

    private void Start()
    {
        menuActivated = false;

        inventoryCanvas.alpha = 0;
        inventoryCanvas.interactable = false;
        inventoryCanvas.blocksRaycasts = false;
    }

    private void Update()
    {
        if (toggleInventory.action.WasPressedThisFrame())
        {
            menuActivated = !menuActivated;

            inventoryCanvas.alpha = menuActivated ? 1 : 0;
            inventoryCanvas.interactable = menuActivated;
            inventoryCanvas.blocksRaycasts = menuActivated;
        }
    }

    public int AddItem(ItemSO item, int quantity)
    {
        // Apenas itens stackáveis tentam completar stacks existentes
        if (item.stackable)
        {
            for (int i = 0; i < itemSlot.Length; i++)
            {
                if (itemSlot[i].item == item && !itemSlot[i].isFull)
                {
                    quantity = itemSlot[i].AddItem(item, quantity);

                    if (quantity <= 0)
                        return 0;
                }
            }
        }

        // Depois procura slots vazios
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i].item == null)
            {
                quantity = itemSlot[i].AddItem(item, quantity);

                if (quantity <= 0)
                    return 0;
            }
        }

        return quantity;
    }
    public void UseItem(ItemSO item)
    {
        item.UseItem();
    }
    public void DeselectAllSlots()
    {
        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i].selectedShader.SetActive(false);
            itemSlot[i].thisItemSelected = false;
        }
    }

    public void DropItem(ItemSlot slot)
    {
        if (slot.item == null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");

        Vector3 dropPosition = player.transform.position + player.transform.right * 0.5f;

        GameObject droppedObject = Instantiate(
            worldItemPrefab,
            dropPosition,
            Quaternion.identity);

        Item droppedItem = droppedObject.GetComponent<Item>();

        if (droppedItem != null)
        {
            // Define qual item esse prefab representa
            droppedItem.SetItem(slot.item);

            // Define a quantidade dropada
            droppedItem.SetQuantity(1);
        }

        // Remove uma unidade do inventário
        slot.ConsumeOneItem();
    }
}