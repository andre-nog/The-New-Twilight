using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Controlador central do drag-and-drop de skills (Livro -> Barra, Barra -> Barra).
// Mesma forma de Assets/Scripts/Itens/InventoryScripts/InventoryDragController.cs
// (ghost icon reaproveitado, canvas própria criada em runtime, BeginDrag/
// UpdateGhostPosition/TryDrop/EndDrag) — arquivo separado por escolha: os domínios
// (item/equipamento vs. skill) não têm nada em comum além da mecânica de arrastar,
// então uma abstração compartilhada entre os dois não compensa.
public class SkillDragController : MonoBehaviour
{
    public static SkillDragController Instance;

    private RectTransform ghostIcon;
    private Image ghostImage;

    private object sourceSlot;

    // Marcado true quando o arrasto termina sobre um slot da barra (OnDrop dispara
    // antes de OnEndDrag). Se continuar false, o drop caiu fora da barra: arrastar um
    // slot da barra pra fora remove a skill dele.
    private bool dropHandled;

    public bool IsDragging => sourceSlot != null;

    public static void EnsureCreated()
    {
        if (FindAnyObjectByType<SkillDragController>() != null)
            return;

        GameObject canvasObject = new("Skill Drag Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        SkillDragController controller = canvasObject.AddComponent<SkillDragController>();
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

    public void BeginDrag(object source, Sprite icon, PointerEventData eventData)
    {
        if (source == null)
            return;

        sourceSlot = source;
        dropHandled = false;

        ghostImage.sprite = icon;
        ghostImage.enabled = true;
        ghostIcon.gameObject.SetActive(true);
        UpdateGhostPosition(eventData);
    }

    public void UpdateGhostPosition(PointerEventData eventData)
    {
        if (!IsDragging)
            return;

        ghostIcon.position = eventData.position;
    }

    public void TryDrop(object target)
    {
        if (sourceSlot == null || target == null)
            return;

        // Soltou sobre um slot da barra: o drop foi consumido aqui, então EndDrag não
        // deve tratar como "solto fora" (mesmo que seja o próprio slot de origem).
        if (target is SkillBarSlot)
            dropHandled = true;

        if (ReferenceEquals(sourceSlot, target))
            return;

        PlayerSkillManager manager = SkillBarUI.Instance != null ? SkillBarUI.Instance.SkillManager : null;

        if (manager == null)
            return;

        switch (sourceSlot)
        {
            case SkillBookSlot bookSource when target is SkillBarSlot barTarget:
                // RefreshAll (não só o destino): atribuir pode ter esvaziado outro slot
                // que já tinha essa skill.
                manager.SetSkillAt(barTarget.SlotIndex, bookSource.Skill, bookSource.IconSprite);
                SkillBarUI.Instance.RefreshAll();
                break;

            case SkillBarSlot barSource when target is SkillBarSlot barTarget2:
                manager.SwapSkills(barSource.SlotIndex, barTarget2.SlotIndex);
                SkillBarUI.Instance.RefreshSlot(barSource.SlotIndex);
                SkillBarUI.Instance.RefreshSlot(barTarget2.SlotIndex);
                break;
        }
    }

    public void EndDrag()
    {
        // Drop fora da barra vindo de um slot da barra = remover a skill dele.
        if (!dropHandled && sourceSlot is SkillBarSlot barSource)
        {
            PlayerSkillManager manager = SkillBarUI.Instance != null ? SkillBarUI.Instance.SkillManager : null;

            if (manager != null)
            {
                manager.SetSkillAt(barSource.SlotIndex, null, null);
                SkillBarUI.Instance.RefreshSlot(barSource.SlotIndex);
            }
        }

        sourceSlot = null;
        dropHandled = false;

        if (ghostIcon != null)
            ghostIcon.gameObject.SetActive(false);
    }
}
