using TMPro;
using UnityEngine;

public class MomentumUI : MonoBehaviour
{
    public TMP_Text momentumText;
    public ResourceManager resourceManager;

    private void OnEnable()
    {
        resourceManager.OnMomentumChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (resourceManager != null)
            resourceManager.OnMomentumChanged -= Refresh;
    }

    private void Refresh()
    {
        momentumText.text =
            $"Momentum: {resourceManager.CurrentMomentum}/{resourceManager.MaxMomentum}";
    }
}
