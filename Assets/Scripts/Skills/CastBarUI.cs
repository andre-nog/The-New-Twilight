using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Generic cast/channel progress bar — shows a label and a 0-1 fill the caller
// drives every tick. No assumption about fill direction (a channel like
// Recovery drains from full to empty; a future hard-cast would fill up
// instead) or about what's being cast — Recovery is just the first caller.
// Pre-baked into the scene by CastBarCanvasBuilder (Tools/Player/Build Cast
// Bar Canvas), same convention as TooltipManager/ExpManager — no EnsureCreated.
public class CastBarUI : MonoBehaviour
{
    public static CastBarUI Instance { get; private set; }

    public Image fillImage;
    public TMP_Text labelText;
    public CanvasGroup canvasGroup;

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

    private void Start()
    {
        Hide();
    }

    public void Show(string label)
    {
        if (labelText != null)
            labelText.text = label;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
    }

    public void SetProgress(float value01)
    {
        if (fillImage != null)
            fillImage.fillAmount = Mathf.Clamp01(value01);
    }

    public void Hide()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
}
