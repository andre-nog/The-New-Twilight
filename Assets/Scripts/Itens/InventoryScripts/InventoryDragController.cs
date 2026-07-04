using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controlador central do drag-and-drop de inventário/equipamento. Não conhece a
// regra de negócio de cada combinação de slots — só identifica os tipos concretos
// de origem/destino e despacha para o InventoryManager/EquipmentManager, que são os
// donos da validação e da mutação de dados. Mesmo padrão de bootstrap runtime do
// SkillBarUI (chamado por GameManager.Start()), pois só precisa de uma Canvas com
// um ícone fantasma — não há nada pra configurar manualmente na cena.
public class InventoryDragController : MonoBehaviour
{
    public static InventoryDragController Instance;

    private RectTransform ghostIcon;
    private Image ghostImage;
    private CanvasGroup sourceCanvasGroup;

    private IItemSlot sourceSlot;

    public bool IsDragging => sourceSlot != null;

    public static void EnsureCreated()
    {
        if (FindAnyObjectByType<InventoryDragController>() != null)
            return;

        GameObject canvasObject = new("Inventory Drag Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        InventoryDragController controller = canvasObject.AddComponent<InventoryDragController>();
        controller.Build();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void Build()
    {
        GameObject ghostObject = new("Drag Ghost Icon", typeof(RectTransform));
        ghostObject.transform.SetParent(transform, false);

        ghostIcon = ghostObject.GetComponent<RectTransform>();
        ghostIcon.sizeDelta = new Vector2(64f, 64f);

        ghostImage = ghostObject.AddComponent<Image>();
        ghostImage.raycastTarget = false;
        ghostImage.preserveAspect = true;

        ghostObject.SetActive(false);
    }

    public void BeginDrag(IItemSlot source, Sprite icon, PointerEventData eventData)
    {
        if (source == null || source.IsEmpty)
            return;

        sourceSlot = source;

        ghostImage.sprite = icon;
        ghostImage.enabled = true;
        ghostIcon.gameObject.SetActive(true);
        UpdateGhostPosition(eventData);

        if (TooltipManager.Instance != null)
            TooltipManager.Instance.Hide();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.DeselectAllSlots();

        Component sourceComponent = source as Component;

        if (sourceComponent != null)
        {
            sourceCanvasGroup = sourceComponent.GetComponent<CanvasGroup>();

            if (sourceCanvasGroup == null)
                sourceCanvasGroup = sourceComponent.gameObject.AddComponent<CanvasGroup>();

            sourceCanvasGroup.alpha = 0.5f;
        }
    }

    public void UpdateGhostPosition(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        ghostIcon.position = eventData.position;
    }

    public void TryDrop(IItemSlot target)
    {
        if (sourceSlot == null || target == null || ReferenceEquals(sourceSlot, target))
            return;

        switch (sourceSlot)
        {
            case ItemSlot fromInventory when target is ItemSlot toInventory:
                InventoryManager.Instance.MoveOrMergeOrSwap(
                    InventoryManager.Instance.IndexOf(fromInventory),
                    InventoryManager.Instance.IndexOf(toInventory));
                break;

            case ItemSlot fromInventory when target is EquippedSlot toEquipped:
                HandleEquip(fromInventory, toEquipped);
                break;

            case EquippedSlot fromEquipped when target is ItemSlot toInventory:
                EquipmentManager.Instance.UnequipTo(fromEquipped, toInventory);
                break;

            case EquippedSlot fromEquipped when target is EquippedSlot toEquipped:
                EquipmentManager.Instance.SwapEquipped(fromEquipped, toEquipped);
                break;
        }
    }

    private void HandleEquip(ItemSlot fromInventory, EquippedSlot toEquipped)
    {
        ItemSO draggedItem = fromInventory.Item;
        ItemSO oldItem = EquipmentManager.Instance.EquipAt(toEquipped, draggedItem);

        // Sentinela: nenhum slot compatível, nada mudou.
        if (oldItem == draggedItem)
            return;

        if (oldItem == null)
            fromInventory.ConsumeOneItem();
        else
            fromInventory.SetItem(oldItem);
    }

    public void EndDrag()
    {
        sourceSlot = null;

        if (ghostIcon != null)
            ghostIcon.gameObject.SetActive(false);

        if (sourceCanvasGroup != null)
        {
            sourceCanvasGroup.alpha = 1f;
            sourceCanvasGroup = null;
        }
    }
}
