using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Same procedural pattern as the other HUD builders (XP/SkillBar/SkillBook):
// idempotent, Image with fillAmount instead of Slider (see ExpCanvasBuilder's
// header comment for why). Placed above the Momentum bar (Y=162, SkillBarUI)
// and the XP bar (Y=128) so it doesn't overlap the existing HUD stack.
public static class CastBarCanvasBuilder
{
    private const string CanvasName = "Cast Bar Canvas";
    private const float BarWidth = 320f;
    private const float BarHeight = 28f;
    private const float BarY = 220f;

    private static readonly Color BarBackground = new(0.04f, 0.05f, 0.07f, 0.85f);
    private static readonly Color FillColor = new(1f, 0.82f, 0.25f, 1f);
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Player/Build Cast Bar Canvas")]
    private static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Cast Bar Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Só leitura, nada clicável aqui — sem GraphicRaycaster, igual à XP bar.
        CastBarUI castBarUI = canvasObject.AddComponent<CastBarUI>();
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform bar = CreateUIObject("Bar", canvasObject.transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, BarY);
        bar.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image background = bar.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = BarBackground;

        // Moldura provisória — não há sprite de moldura ornamentada importado
        // no projeto ainda (ver plano). Trocar por 9-slice quando houver arte.
        Outline outline = bar.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        Image fill = CreateImage("Fill", bar, FillColor);
        fill.sprite = GetRuntimeSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        SetStretch(fill.rectTransform, 2f);

        TMP_Text label = CreateText("Label", bar, "Channeling", 18f, TextAlignmentOptions.Center);
        SetStretch(label.rectTransform, 0f);
        label.fontStyle = FontStyles.Bold;
        label.color = Color.white;

        castBarUI.fillImage = fill;
        castBarUI.labelText = label;
        castBarUI.canvasGroup = canvasGroup;

        Selection.activeGameObject = canvasObject;
    }

    private static RectTransform CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        RectTransform rectTransform = CreateUIObject(objectName, parent);
        Image image = rectTransform.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    private static TMP_Text CreateText(
        string objectName,
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        RectTransform rectTransform = CreateUIObject(objectName, parent);
        TextMeshProUGUI text = rectTransform.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        text.raycastTarget = false;
        text.overflowMode = TextOverflowModes.Truncate;

        TMP_FontAsset font = GetBangersFont();

        if (font != null)
            text.font = font;

        return text;
    }

    private static TMP_FontAsset GetBangersFont()
    {
        if (bangersFont == null)
            bangersFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/Bangers SDF");

        return bangersFont;
    }

    private static void SetStretch(RectTransform rectTransform, float margin)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(margin, margin);
        rectTransform.offsetMax = new Vector2(-margin, -margin);
    }

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
            return runtimeSprite;

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Cast Bar Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Cast Bar Runtime Sprite";
        return runtimeSprite;
    }
}
