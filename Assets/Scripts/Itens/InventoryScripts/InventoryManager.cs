using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour, ICancelable
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
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        toggleInventory.action.Enable();
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        toggleInventory.action.Disable();
    }

    private void Start()
    {
        menuActivated = false;

        inventoryCanvas.alpha = 0;
        inventoryCanvas.interactable = false;
        inventoryCanvas.blocksRaycasts = false;
        CancelManager.Instance.Register(this);
    }

    private void Update()
    {
        if (toggleInventory.action.WasPressedThisFrame())
        {
            if (menuActivated)
                CloseInventory();
            else
                OpenInventory();
        }
    }
    public void OpenInventory()
    {
        menuActivated = true;

        inventoryCanvas.alpha = 1;
        inventoryCanvas.interactable = true;
        inventoryCanvas.blocksRaycasts = true;
    }
    public void CloseInventory()
    {
        menuActivated = false;

        inventoryCanvas.alpha = 0;
        inventoryCanvas.interactable = false;
        inventoryCanvas.blocksRaycasts = false;
    }

    public int AddItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
            return quantity;

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
            if (itemSlot[i].selectedShader != null)
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
    private void OnDestroy()
    {
        if (CancelManager.Instance != null)
            CancelManager.Instance.Unregister(this);

        if (Instance == this)
            Instance = null;
    }

    public bool CanCancel()
    {
        return menuActivated;
    }

    public void Cancel()
    {
        CloseInventory();
    }

    public int Priority => 100;
}
