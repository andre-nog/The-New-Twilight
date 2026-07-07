using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Espelha TooltipCanvasBuilder/SkillBookCanvasBuilder: idempotente (destroy-by-name
// + recreate), painel com VerticalLayoutGroup + ContentSizeFitter vertical (largura
// fixa, altura acompanha o conteúdo), mesma paleta/Outline usados em todo o resto
// da HUD. Um único painel com 3 modos (Accept/InProgress/Complete) — QuestWindow
// troca quais botões ficam ativos, o layout é o mesmo.
public static class QuestWindowCanvasBuilder
{
    private const string CanvasName = "Quest Window Canvas";
    private const float PanelWidth = 420f;
    private const int SortingOrder = 150;

    private static readonly Color PanelBackground = new(0.04f, 0.05f, 0.07f, 0.9f);
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color ButtonColor = new(0.12f, 0.14f, 0.18f, 0.95f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Quests/Build Quest Window Canvas")]
    private static void Build()
    {
        DestroyIfExists(CanvasName);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Quest Window Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform panel = CreateUIObject("Quest Panel", canvasObject.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = PanelBackground;
        background.raycastTarget = false;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateWrappingText("Title", panel, string.Empty, 25f);
        title.fontStyle = FontStyles.Bold;
        title.color = GoldAccent;

        TMP_Text description = CreateWrappingText("Description", panel, string.Empty, 17f);

        TMP_Text objective = CreateWrappingText("Objective", panel, string.Empty, 17f);

        TMP_Text reward = CreateWrappingText("Reward", panel, string.Empty, 17f);
        reward.color = GoldAccent;

        RectTransform footer = CreateUIObject("Footer", panel);
        LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 36f;

        HorizontalLayoutGroup footerGroup = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerGroup.spacing = 8f;
        footerGroup.childAlignment = TextAnchor.MiddleLeft;
        footerGroup.childControlWidth = true;
        footerGroup.childControlHeight = true;
        footerGroup.childForceExpandWidth = true;
        footerGroup.childForceExpandHeight = true;

        GameObject acceptButton = BuildFooterButton(footer, "Accept", QuestWindowButton.Kind.Accept);
        GameObject confirmButton = BuildFooterButton(footer, "Confirm", QuestWindowButton.Kind.Confirm);

        GameObject cancelButton = BuildCloseButton(panel);

        QuestWindow questWindow = canvasObject.AddComponent<QuestWindow>();
        questWindow.Configure(canvasGroup, title, description, objective, reward, acceptButton, confirmButton, cancelButton);

        Selection.activeGameObject = canvasObject;
    }

    private static GameObject BuildFooterButton(Transform parent, string label, QuestWindowButton.Kind kind)
    {
        RectTransform rect = CreateUIObject($"{label} Button", parent);

        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = ButtonColor;
        background.raycastTarget = true;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        TMP_Text text = CreateText("Label", rect, label, 17f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;

        QuestWindowButton button = rect.gameObject.AddComponent<QuestWindowButton>();
        button.Configure(kind);

        return rect.gameObject;
    }

    // "X" no canto superior direito do painel — separado do rodapé pra ficar
    // visível nos 3 modos (Accept/InProgress/Complete).
    private static GameObject BuildCloseButton(Transform panelParent)
    {
        RectTransform rect = CreateUIObject("Close Button", panelParent);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-6f, -6f);
        rect.sizeDelta = new Vector2(22f, 22f);

        // Painel é um VerticalLayoutGroup — sem isso, o layout group ignora o
        // anchor/anchoredPosition manual acima e empilha o botão como mais uma
        // linha (ficava aparecendo embaixo do Accept, em vez de flutuar no canto).
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;

        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = ButtonColor;
        background.raycastTarget = true;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        TMP_Text text = CreateText("Label", rect, "X", 16f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;

        QuestWindowButton button = rect.gameObject.AddComponent<QuestWindowButton>();
        button.Configure(QuestWindowButton.Kind.Cancel);

        return rect.gameObject;
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

    // Título/descrição/objetivo/recompensa podem ser mais longos que um slot de
    // skill — variante que quebra linha em vez de truncar.
    private static TMP_Text CreateWrappingText(string objectName, Transform parent, string value, float fontSize)
    {
        TMP_Text text = CreateText(objectName, parent, value, fontSize, TextAlignmentOptions.TopLeft);
        text.overflowMode = TextOverflowModes.Overflow;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
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
        texture.name = "Quest Window Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Quest Window Runtime Sprite";
        return runtimeSprite;
    }
}
