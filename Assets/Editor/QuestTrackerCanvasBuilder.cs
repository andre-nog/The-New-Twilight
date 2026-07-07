using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Espelha os outros builders (Tooltip/SkillBar/SkillBook): idempotente, mesma
// paleta. Painel de HUD ancorado no canto superior direito, sem GraphicRaycaster
// (nada aqui é clicável) e abaixo da Skillbar (100) na ordem de sorting. Layout
// estilo MMORPG clássico: cabeçalho "Quests", e uma linha por quest rastreada
// com ícone + título dourado + objetivo menor logo abaixo.
public static class QuestTrackerCanvasBuilder
{
    private const string CanvasName = "Quest Tracker Canvas";
    private const int SortingOrder = 50;
    private const float PanelWidth = 260f;
    private const float IconSize = 18f;
    private const int RowCount = 6;

    private static readonly Color PanelBackground = new(0.04f, 0.05f, 0.07f, 0.8f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color IconColor = new(1f, 0.86f, 0.35f, 0.9f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Quests/Build Quest Tracker Canvas")]
    private static void Build()
    {
        DestroyIfExists(CanvasName);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Quest Tracker Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateUIObject("Tracker Panel", canvasObject.transform);
        panel.anchorMin = new Vector2(1f, 1f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.anchoredPosition = new Vector2(-24f, -24f);
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = PanelBackground;
        background.raycastTarget = false;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 12);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text header = CreateText("Header", panel, "Quests", 22f, TextAlignmentOptions.TopLeft);
        header.fontStyle = FontStyles.Bold;
        header.color = GoldAccent;

        QuestTrackerRowUI[] rows = new QuestTrackerRowUI[RowCount];

        for (int i = 0; i < RowCount; i++)
        {
            QuestTrackerRowUI row = BuildRow(panel, i);
            row.gameObject.SetActive(false);
            rows[i] = row;
        }

        QuestTrackerHUD tracker = canvasObject.AddComponent<QuestTrackerHUD>();
        tracker.Configure(rows);

        Selection.activeGameObject = canvasObject;
    }

    private static QuestTrackerRowUI BuildRow(Transform parent, int index)
    {
        RectTransform row = CreateUIObject($"Quest Row {index}", parent);

        HorizontalLayoutGroup rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 8f;
        rowLayout.childAlignment = TextAnchor.UpperLeft;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childForceExpandHeight = false;

        // Ícone placeholder — quadrado dourado até existir arte própria por quest
        // (bastaria um campo de ícone no QuestSO pra trocar isso por sprite real).
        RectTransform iconRect = CreateUIObject("Icon", row);
        LayoutElement iconLayout = iconRect.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = IconSize;
        iconLayout.preferredHeight = IconSize;
        iconLayout.minWidth = IconSize;

        Image icon = iconRect.gameObject.AddComponent<Image>();
        icon.sprite = GetRuntimeSprite();
        icon.color = IconColor;
        icon.raycastTarget = false;

        RectTransform textStack = CreateUIObject("Text", row);
        LayoutElement textLayout = textStack.gameObject.AddComponent<LayoutElement>();
        textLayout.flexibleWidth = 1f;

        VerticalLayoutGroup textLayoutGroup = textStack.gameObject.AddComponent<VerticalLayoutGroup>();
        textLayoutGroup.spacing = 1f;
        textLayoutGroup.childAlignment = TextAnchor.UpperLeft;
        textLayoutGroup.childControlWidth = true;
        textLayoutGroup.childControlHeight = true;
        textLayoutGroup.childForceExpandWidth = true;
        textLayoutGroup.childForceExpandHeight = false;

        TMP_Text title = CreateText("Title", textStack, string.Empty, 17f, TextAlignmentOptions.TopLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = GoldAccent;
        title.textWrappingMode = TextWrappingModes.Normal;

        TMP_Text objective = CreateText("Objective", textStack, string.Empty, 15f, TextAlignmentOptions.TopLeft);
        objective.textWrappingMode = TextWrappingModes.Normal;
        objective.color = Color.white;

        QuestTrackerRowUI rowUI = row.gameObject.AddComponent<QuestTrackerRowUI>();
        rowUI.Configure(title, objective);
        return rowUI;
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);
    }

    private static RectTransform CreateUIObject(string objectName, Transform parent)
    {
        GameObject gameObject = new(objectName, typeof(RectTransform));
        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
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
        text.overflowMode = TextOverflowModes.Overflow;

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

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
            return runtimeSprite;

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Quest Tracker Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Quest Tracker Runtime Sprite";
        return runtimeSprite;
    }
}
