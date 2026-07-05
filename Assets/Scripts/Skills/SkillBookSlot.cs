using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Componente do slot do Livro de Skills — a hierarquia (Icon/Name/Locked Overlay) já
// vem pronta na cena via Assets/Editor/SkillBookCanvasBuilder.cs, que também chama
// Configure() com a skill e o estado "bloqueada" de cada slot. É só origem de
// arrasto (o Livro nunca recebe um drop de volta) — skills bloqueadas não iniciam
// o arrasto, já que ainda não podem ser usadas.
public class SkillBookSlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Image icon;

    // [field: SerializeField] so the values Configure() sets at build time survive
    // serialization into runtime. Without it, Skill/Locked reset to null/false on scene
    // load and OnBeginDrag bails (Skill == null), so the drag never starts.
    [field: SerializeField] public Skill Skill { get; private set; }
    [field: SerializeField] public bool Locked { get; private set; }

    public void Configure(Skill skill, bool locked, Image iconImage)
    {
        Skill = skill;
        Locked = locked;
        icon = iconImage;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (Locked || Skill == null)
            return;

        SkillDragController.Instance.BeginDrag(this, icon.sprite, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!SkillDragController.Instance.IsDragging)
            return;

        SkillDragController.Instance.UpdateGhostPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        SkillDragController.Instance.EndDrag();
    }
}
