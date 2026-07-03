using TMPro;
using UnityEngine;

public class MomentumUI : MonoBehaviour
{
    public TMP_Text momentumText;
    public ResourceManager resourceManager;

    private void Update()
    {
        momentumText.text =
            $"Momentum: {resourceManager.CurrentMomentum}/{resourceManager.MaxMomentum}";
    }
}