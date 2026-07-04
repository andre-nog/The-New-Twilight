using TMPro;
using UnityEngine;

public class MomentumUI : MonoBehaviour
{
    public TMP_Text momentumText;
    public ResourceManager resourceManager;

    private void OnEnable()
    {
        resourceManager.OnResourceChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (resourceManager != null)
            resourceManager.OnResourceChanged -= Refresh;
    }

    private void Refresh()
    {
        momentumText.text =
            $"{resourceManager.resourceName}: {resourceManager.CurrentResource}/{resourceManager.MaxResource}";
    }
}
