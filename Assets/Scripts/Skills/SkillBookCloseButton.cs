using UnityEngine;
using UnityEngine.EventSystems;

// Mesmo padrão de clique direto via IPointerClickHandler usado em ItemSlot/
// EquippedSlot/SkillBookSlot/QuestWindowButton — não UnityEngine.UI.Button.
public class SkillBookCloseButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        SkillBookUI.Instance?.Close();
    }
}
