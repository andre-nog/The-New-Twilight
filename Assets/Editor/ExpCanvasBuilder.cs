using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Substitui a antiga "XPCanvas" (Slider hand-placed na cena, cujo
// Slider.set_maxValue disparava "SendMessage cannot be called during
// OnValidate" quando StatsManager.OnValidate chamava ExpManager.UpdateUI).
// Segue o mesmo padrão procedural dos outros builders (Tooltip/SkillBar/
// SkillBook/Quest): idempotente, Image com fillAmount em vez de Slider.
// Fica entre a Skill Bar (28-120) e o Momentum (162+) — ver BuildMomentumBar/
// BuildVitalBars em SkillBarUI.cs, que foram deslocados pra cima pra abrir espaço.
public static class ExpCanvasBuilder
{
    private const string CanvasName = "XP Canvas";
    private const float BarWidth = 260f;
    private const float BarHeight = 26f;
    private const float BarY = 128f;

    private static readonly Color BarBackground = new(0.04f, 0.05f, 0.07f, 0.85f);
    private static readonly Color FillColor = new(0.55f, 0.35f, 0.85f, 1f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Player/Build XP Canvas")]
    private static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create XP Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Só leitura, nada clicável aqui — sem GraphicRaycaster, igual ao Quest Tracker.
        ExpManager expManager = canvasObject.AddComponent<ExpManager>();

        RectTransform bar = CreateUIObject("XP Bar", canvasObject.transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, BarY);
        bar.sizeDelta = new Vector2(BarWidth, BarHeight);

        Image background = bar.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = BarBackground;

        Image fill = CreateImage("Fill", bar, FillColor);
        fill.sprite = GetRuntimeSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0f;
        SetStretch(fill.rectTransform, 2f);

        TMP_Text text = CreateText("Level Text", bar, "Level: 1", 19f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;
        text.color = GoldAccent;

        expManager.expFillImage = fill;
        expManager.currentLevelText = text;

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
        texture.name = "XP Canvas Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "XP Canvas Runtime Sprite";
        return runtimeSprite;
    }
}
