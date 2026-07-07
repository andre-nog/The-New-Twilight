using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Espelha QuestWindowCanvasBuilder — mesma paleta/Outline/fonte. sortingOrder
// maior que a Shop Window Canvas (200 > 150) pra desenhar por cima dela.
public static class PurchaseConfirmCanvasBuilder
{
    private const string CanvasName = "Purchase Confirm Canvas";
    private const float PanelWidth = 320f;
    private const int SortingOrder = 200;

    private static readonly Color PanelBackground = new(0.04f, 0.05f, 0.07f, 0.92f);
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color ButtonColor = new(0.12f, 0.14f, 0.18f, 0.95f);
    private static readonly Color WarningColor = new(0.85f, 0.25f, 0.25f, 1f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Shop/Build Purchase Confirm Canvas")]
    private static void Build()
    {
        DestroyIfExists(CanvasName);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Purchase Confirm Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform panel = CreateUIObject("Confirm Panel", canvasObject.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = PanelBackground;
        // true — mesma razão de ShopWindowCanvasBuilder: janela interativa,
        // precisa bloquear o mundo atrás dela em toda a área do painel.
        background.raycastTarget = true;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image icon = CreateImage("Icon", panel, Color.white);
        LayoutElement iconLayout = icon.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 64f;
        iconLayout.preferredHeight = 64f;
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text itemName = CreateText("Item Name", panel, string.Empty, 21f, TextAlignmentOptions.Center);
        itemName.fontStyle = FontStyles.Bold;
        itemName.color = GoldAccent;

        TMP_Text unitPrice = CreateText("Unit Price", panel, string.Empty, 15f, TextAlignmentOptions.Center);

        TMP_Text totalPrice = CreateText("Total Price", panel, "0", 19f, TextAlignmentOptions.Center);

        GameObject quantityGroup = BuildQuantityRow(panel, out TMP_Text quantityText);

        TMP_Text warning = CreateText("Warning", panel, string.Empty, 15f, TextAlignmentOptions.Center);
        warning.color = WarningColor;
        warning.gameObject.SetActive(false);

        RectTransform footer = CreateUIObject("Footer", panel);
        LayoutElement footerLayout = footer.gameObject.AddComponent<LayoutElement>();
        footerLayout.preferredHeight = 36f;

        HorizontalLayoutGroup footerGroup = footer.gameObject.AddComponent<HorizontalLayoutGroup>();
        footerGroup.spacing = 8f;
        footerGroup.childAlignment = TextAnchor.MiddleCenter;
        footerGroup.childControlWidth = true;
        footerGroup.childControlHeight = true;
        footerGroup.childForceExpandWidth = true;
        footerGroup.childForceExpandHeight = true;

        CanvasGroup okGroup = BuildFooterButton(footer, "OK", PurchaseConfirmButton.Kind.Ok);
        BuildFooterButton(footer, "Cancel", PurchaseConfirmButton.Kind.Cancel);

        PurchaseConfirmWindow confirmWindow = canvasObject.AddComponent<PurchaseConfirmWindow>();
        confirmWindow.Configure(canvasGroup, icon, itemName, unitPrice, totalPrice, quantityGroup, quantityText, okGroup, warning);

        Selection.activeGameObject = canvasObject;
    }

    private static GameObject BuildQuantityRow(Transform parent, out TMP_Text quantityText)
    {
        RectTransform row = CreateUIObject("Quantity Group", parent);
        LayoutElement rowLayout = row.gameObject.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 32f;

        HorizontalLayoutGroup rowGroup = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowGroup.spacing = 8f;
        rowGroup.childAlignment = TextAnchor.MiddleCenter;
        rowGroup.childControlWidth = false;
        rowGroup.childControlHeight = true;
        rowGroup.childForceExpandWidth = false;
        rowGroup.childForceExpandHeight = true;

        BuildSmallButton(row, "-", PurchaseConfirmButton.Kind.Decrement);

        RectTransform quantityRect = CreateUIObject("Quantity", row);
        LayoutElement quantityLayout = quantityRect.gameObject.AddComponent<LayoutElement>();
        quantityLayout.preferredWidth = 48f;
        quantityText = CreateText("Value", quantityRect, "1", 19f, TextAlignmentOptions.Center);
        SetStretch(quantityText.rectTransform, 0f);

        BuildSmallButton(row, "+", PurchaseConfirmButton.Kind.Increment);

        return row.gameObject;
    }

    private static void BuildSmallButton(Transform parent, string label, PurchaseConfirmButton.Kind kind)
    {
        RectTransform rect = CreateUIObject($"{label} Button", parent);
        LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = 32f;
        layoutElement.preferredHeight = 32f;

        Image background = rect.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = ButtonColor;
        background.raycastTarget = true;

        Outline outline = rect.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        TMP_Text text = CreateText("Label", rect, label, 18f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;

        PurchaseConfirmButton button = rect.gameObject.AddComponent<PurchaseConfirmButton>();
        button.Configure(kind);
    }

    private static CanvasGroup BuildFooterButton(Transform parent, string label, PurchaseConfirmButton.Kind kind)
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

        PurchaseConfirmButton button = rect.gameObject.AddComponent<PurchaseConfirmButton>();
        button.Configure(kind);

        return rect.gameObject.AddComponent<CanvasGroup>();
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
        texture.name = "Purchase Confirm Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Purchase Confirm Runtime Sprite";
        return runtimeSprite;
    }
}
