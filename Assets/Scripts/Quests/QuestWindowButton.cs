using UnityEngine;
using UnityEngine.EventSystems;

// Mesmo padrão de clique direto via IPointerClickHandler usado em ItemSlot/
// EquippedSlot/SkillBookSlot — não existe UnityEngine.UI.Button em nenhum
// outro lugar clicável do projeto.
public class QuestWindowButton : MonoBehaviour, IPointerClickHandler
{
    public enum Kind
    {
        Accept,
        Cancel,
        Confirm
    }

    [SerializeField] private Kind kind;

    public void Configure(Kind buttonKind)
    {
        kind = buttonKind;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (QuestWindow.Instance == null)
            return;

        switch (kind)
        {
            case Kind.Accept:
                QuestWindow.Instance.OnAcceptClicked();
                break;

            case Kind.Cancel:
                QuestWindow.Instance.OnCancelClicked();
                break;

            case Kind.Confirm:
                QuestWindow.Instance.OnConfirmClicked();
                break;
        }
    }
}
