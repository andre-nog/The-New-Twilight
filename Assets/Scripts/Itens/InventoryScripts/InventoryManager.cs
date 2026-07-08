using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour, ICancelable
{
    public static InventoryManager Instance;
    public CanvasGroup inventoryCanvas;
    public InputActionReference toggleInventory;

    [SerializeField]
    private ItemSlot[] itemSlot;

    [SerializeField]
    private GameObject worldItemPrefab;

    // Referências usadas só pelo "modo loja" (ver EnterShopMode/ExitShopMode) —
    // esconder o painel de equipamento e redimensionar a Window Panel pra
    // caber só a grade de itens, deixando espaço pra ShopWindow ao lado.
    [SerializeField]
    private GameObject equipmentPanel;

    [SerializeField]
    private RectTransform windowPanel;

    [SerializeField]
    private RectTransform itemPanelRect;

    private bool menuActivated;
    private Vector2 cachedWindowSize;
    private Vector2 cachedWindowPosition;

    // Entradas cujo itemId não resolveu no ItemDatabaseSO (asset renomeado/deletado
    // durante balanceamento) — nunca descartadas silenciosamente, ficam aqui até o
    // asset voltar a existir ou o jogador ocupar o slot com outra coisa (ver GetState).
    private readonly List<ItemStackSave> unresolvedStacks = new();

    public RectTransform WindowPanel => windowPanel;
    public bool IsShopMode { get; private set; }

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
        // Em modo loja, só ShopWindow.Close() (via InventoryWindowButton ou
        // ESC/CancelManager) pode fechar o par loja+inventário — a tecla de
        // atalho de abrir/fechar o inventário sozinha ficaria de fora dessa
        // decisão e deixaria a loja desalinhada se fechasse só a metade.
        if (IsShopMode)
            return;

        menuActivated = false;

        inventoryCanvas.alpha = 0;
        inventoryCanvas.interactable = false;
        inventoryCanvas.blocksRaycasts = false;
    }

    // Esconde o painel de equipamento e encolhe a Window Panel pra caber só a
    // grade de itens — o HorizontalLayoutGroup já ignora filhos inativos ao
    // posicionar os outros (o Item Panel desliza pra esquerda sozinho), mas o
    // tamanho da própria Window Panel não é controlado por ninguém e precisa
    // ser ajustado aqui.
    public void EnterShopMode()
    {
        if (IsShopMode)
            return;

        IsShopMode = true;
        cachedWindowSize = windowPanel.sizeDelta;
        cachedWindowPosition = windowPanel.anchoredPosition;

        DeselectAllSlots();
        equipmentPanel.SetActive(false);

        LayoutRebuilder.ForceRebuildLayoutImmediate(windowPanel);

        HorizontalLayoutGroup layout = windowPanel.GetComponent<HorizontalLayoutGroup>();
        float newWidth = layout.padding.left + layout.padding.right + itemPanelRect.rect.width;
        windowPanel.sizeDelta = new Vector2(newWidth, windowPanel.sizeDelta.y);

        foreach (ItemSlot slot in itemSlot)
            slot.RefreshShopModeVisual(true);
    }

    public void ExitShopMode()
    {
        if (!IsShopMode)
            return;

        IsShopMode = false;
        equipmentPanel.SetActive(true);
        windowPanel.sizeDelta = cachedWindowSize;
        windowPanel.anchoredPosition = cachedWindowPosition;

        DeselectAllSlots();

        foreach (ItemSlot slot in itemSlot)
            slot.RefreshShopModeVisual(false);
    }

    // Quanto o jogador possui deste item somando todos os slots — usado pra
    // pré-preencher a quantidade de venda e validar antes de remover.
    public int GetQuantity(ItemSO item)
    {
        int total = 0;

        foreach (ItemSlot slot in itemSlot)
        {
            if (slot.item == item)
                total += slot.quantity;
        }

        return total;
    }

    // Remove quantity unidades de item, varrendo quantos slots forem
    // necessários. Retorna false (sem mutar nada) se o jogador não tiver o
    // suficiente — mesma atomicidade de CanFit (valida tudo antes de mover).
    public bool RemoveItem(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
            return true;

        if (GetQuantity(item) < quantity)
            return false;

        int remaining = quantity;

        foreach (ItemSlot slot in itemSlot)
        {
            if (slot.item != item || remaining <= 0)
                continue;

            int fromThisSlot = Mathf.Min(remaining, slot.quantity);
            slot.ReduceQuantity(fromThisSlot);
            remaining -= fromThisSlot;
        }

        return true;
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

    // Pré-checagem de espaço sem mutar nada — usada pela loja pra só gastar ouro
    // depois de confirmar que o item cabe (em vez de tentar-e-reverter).
    public bool CanFit(ItemSO item, int quantity)
    {
        if (item == null || quantity <= 0)
            return true;

        int remaining = quantity;

        foreach (ItemSlot slot in itemSlot)
        {
            remaining -= slot.RemainingCapacity(item);

            if (remaining <= 0)
                return true;
        }

        return false;
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

        // Reanexa entradas não resolvidas cujo slot continua livre — preserva o dado
        // até o item ser resolvível de novo ou o jogador ocupar o slot com outra coisa
        // (nesse caso a ação real do jogador prevalece e a entrada crua é descartada).
        foreach (ItemStackSave unresolved in unresolvedStacks)
        {
            bool slotStillFree = unresolved.slotIndex < 0 || unresolved.slotIndex >= itemSlot.Length
                || itemSlot[unresolved.slotIndex].item == null;

            if (slotStillFree)
                result.Add(unresolved);
        }

        return result;
    }

    // Reconstrói o inventário do zero a partir do save — todo slot não presente na
    // lista fica vazio (Clear), então não sobra item de uma partida anterior.
    public void ApplyState(List<ItemStackSave> state, ItemDatabaseSO database)
    {
        for (int i = 0; i < itemSlot.Length; i++)
            itemSlot[i].Clear();

        unresolvedStacks.Clear();

        if (state == null || database == null)
            return;

        foreach (ItemStackSave stack in state)
        {
            if (stack.slotIndex < 0 || stack.slotIndex >= itemSlot.Length)
            {
                Debug.LogWarning($"InventoryManager: slotIndex {stack.slotIndex} do save fora do intervalo (item '{stack.itemId}') — preservado cru em vez de descartado.");
                unresolvedStacks.Add(stack);
                continue;
            }

            ItemSO item = database.GetById(stack.itemId);

            if (item == null)
            {
                Debug.LogWarning($"InventoryManager: itemId '{stack.itemId}' (slot {stack.slotIndex}) não encontrado no banco de itens — preservado cru em vez de descartado.");
                unresolvedStacks.Add(stack);
                continue;
            }

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
