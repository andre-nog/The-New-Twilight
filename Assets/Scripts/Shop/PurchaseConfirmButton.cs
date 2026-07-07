using UnityEngine;
using UnityEngine.EventSystems;

// Mesmo padrão de QuestWindowButton — clique direto via IPointerClickHandler.
public class PurchaseConfirmButton : MonoBehaviour, IPointerClickHandler
{
    public enum Kind
    {
        Ok,
        Cancel,
        Increment,
        Decrement
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

        if (PurchaseConfirmWindow.Instance == null)
            return;

        switch (kind)
        {
            case Kind.Ok:
                PurchaseConfirmWindow.Instance.OnOkClicked();
                break;

            case Kind.Cancel:
                PurchaseConfirmWindow.Instance.OnCancelClicked();
                break;

            case Kind.Increment:
                PurchaseConfirmWindow.Instance.OnIncrementClicked();
                break;

            case Kind.Decrement:
                PurchaseConfirmWindow.Instance.OnDecrementClicked();
                break;
        }
    }
}
