using System.Collections.Generic;
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

    public int IndexOf(ItemSlot slot)
    {
        return System.Array.IndexOf(itemSlot, slot);
    }

    // Move, mescla ou troca o conteúdo entre dois slots do inventário — usado pelo
    // drag-and-drop. Não faz nada se os índices forem iguais (soltar no próprio slot).
    public bool MoveOrMergeOrSwap(int fromIndex, int toIndex)
    {
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= itemSlot.Length || toIndex >= itemSlot.Length)
            return false;

        if (fromIndex == toIndex)
            return false;

        ItemSlot fromSlot = itemSlot[fromIndex];
        ItemSlot toSlot = itemSlot[toIndex];

        if (fromSlot.item == null)
            return false;

        if (toSlot.item == null)
        {
            toSlot.LoadItem(fromSlot.item, fromSlot.quantity);
            fromSlot.Clear();
            return true;
        }

        if (toSlot.item == fromSlot.item && fromSlot.item.stackable)
        {
            int leftover = toSlot.AddItem(fromSlot.item, fromSlot.quantity);

            if (leftover <= 0)
                fromSlot.Clear();
            else
                fromSlot.ReduceQuantity(fromSlot.quantity - leftover);

            return true;
        }

        // Itens diferentes -> troca completa
        ItemSO fromItem = fromSlot.item;
        int fromQuantity = fromSlot.quantity;
        ItemSO toItem = toSlot.item;
        int toQuantity = toSlot.quantity;

        fromSlot.LoadItem(toItem, toQuantity);
        toSlot.LoadItem(fromItem, fromQuantity);

        return true;
    }

    public void DropItem(ItemSlot slot)
    {
        if (slot.item == null)
            return;

        Transform playerTransform = GameManager.Instance != null ? GameManager.Instance.Player : null;

        if (playerTransform == null)
            return;

        Vector3 dropPosition = playerTransform.position + playerTransform.right * 0.5f;

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

    public List<ItemStackSave> GetState()
    {
        List<ItemStackSave> result = new();

        for (int i = 0; i < itemSlot.Length; i++)
        {
            ItemSlot slot = itemSlot[i];

            if (slot.item == null)
                continue;

            result.Add(new ItemStackSave
            {
                slotIndex = i,
                itemId = slot.item.Id,
                quantity = slot.quantity
            });
        }

        return result;
    }

    // Reconstrói o inventário do zero a partir do save — todo slot não presente na
    // lista fica vazio (Clear), então não sobra item de uma partida anterior.
    public void ApplyState(List<ItemStackSave> state, ItemDatabaseSO database)
    {
        for (int i = 0; i < itemSlot.Length; i++)
            itemSlot[i].Clear();

        if (state == null || database == null)
            return;

        foreach (ItemStackSave stack in state)
        {
            if (stack.slotIndex < 0 || stack.slotIndex >= itemSlot.Length)
                continue;

            ItemSO item = database.GetById(stack.itemId);

            if (item == null)
                continue;

            itemSlot[stack.slotIndex].LoadItem(item, stack.quantity);
        }
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
