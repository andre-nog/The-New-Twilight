using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Espelha QuestWindowCanvasBuilder/SkillBarCanvasBuilder: mesma paleta/Outline/
// fonte, painel com VerticalLayoutGroup + ContentSizeFitter. Pool fixo de
// ShopSlot (como a Skill Bar) num GridLayoutGroup — ShopWindow.Open() só
// preenche os N primeiros conforme o ShopSO e esconde o resto.
public static class ShopWindowCanvasBuilder
{
    private const string CanvasName = "Shop Window Canvas";
    private const float PanelWidth = 420f;
    private const int SortingOrder = 150;
    private const int SlotCount = 16;
    private const int Columns = 4;
    private const float SlotSize = 76f;
    private const float SlotSpacing = 8f;

    private static readonly Color PanelBackground = new(0.04f, 0.05f, 0.07f, 0.9f);
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);
    private static readonly Color GoldAccent = new(1f, 0.86f, 0.35f, 1f);
    private static readonly Color SlotBackground = new(0.12f, 0.14f, 0.18f, 0.95f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Shop/Build Shop Window Canvas")]
    private static void Build()
    {
        DestroyIfExists(CanvasName);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Shop Window Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        RectTransform panel = CreateUIObject("Shop Panel", canvasObject.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(PanelWidth, 0f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = PanelBackground;
        // true (não false como os outros builders copiaram da Tooltip) — a
        // Tooltip pode deixar passar clique porque ela é decorativa e segue o
        // mouse; esta janela é interativa e precisa bloquear o mundo atrás
        // dela em toda a área do painel, não só em cima de cada slot.
        background.raycastTarget = true;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(16, 16, 16, 16);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        TMP_Text title = CreateText("Title", panel, "Shop", 25f, TextAlignmentOptions.TopLeft);
        title.fontStyle = FontStyles.Bold;
        title.color = GoldAccent;

        // Sem display de ouro aqui — a loja sempre abre com o inventário (modo
        // venda) ao lado, que já mostra o ouro atual; duplicar seria redundante.
        // Sem botão "X" próprio também — a loja sempre abre em par com o
        // inventário, cujo "X" (canto superior direito do par inteiro) fecha
        // os dois juntos; ver InventoryWindowButton.
        ShopSlot[] slots = BuildSlotGrid(panel);

        ShopWindow shopWindow = canvasObject.AddComponent<ShopWindow>();
        shopWindow.Configure(canvasGroup, title, slots, panel);

        Selection.activeGameObject = canvasObject;
    }

    private static ShopSlot[] BuildSlotGrid(Transform parent)
    {
        RectTransform grid = CreateUIObject("Slots", parent);

        int rows = Mathf.CeilToInt(SlotCount / (float)Columns);
        float gridWidth = SlotSize * Columns + SlotSpacing * (Columns - 1);
        float gridHeight = SlotSize * rows + SlotSpacing * (rows - 1);

        LayoutElement gridLayoutElement = grid.gameObject.AddComponent<LayoutElement>();
        gridLayoutElement.preferredWidth = gridWidth;
        gridLayoutElement.preferredHeight = gridHeight;

        GridLayoutGroup gridLayout = grid.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.cellSize = new Vector2(SlotSize, SlotSize);
        gridLayout.spacing = new Vector2(SlotSpacing, SlotSpacing);
        gridLayout.childAlignment = TextAnchor.UpperLeft;
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = Columns;

        ShopSlot[] slots = new ShopSlot[SlotCount];

        for (int i = 0; i < SlotCount; i++)
            slots[i] = BuildSlot(grid, i);

        return slots;
    }

    private static ShopSlot BuildSlot(Transform parent, int index)
    {
        RectTransform slot = CreateUIObject($"Shop Slot {index}", parent);
        slot.sizeDelta = new Vector2(SlotSize, SlotSize);

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = SlotBackground;

        Outline outline = slot.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        Image icon = CreateImage("Icon", slot, Color.white);
        SetStretch(icon.rectTransform, 8f);
        icon.rectTransform.offsetMax = new Vector2(icon.rectTransform.offsetMax.x, -16f);
        icon.preserveAspect = true;
        icon.enabled = false;

        TMP_Text price = CreateText("Price", slot, string.Empty, 15f, TextAlignmentOptions.Bottom);
        price.rectTransform.anchorMin = new Vector2(0f, 0f);
        price.rectTransform.anchorMax = new Vector2(1f, 0f);
        price.rectTransform.pivot = new Vector2(0.5f, 0f);
        price.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        price.rectTransform.sizeDelta = new Vector2(-4f, 18f);
        price.color = GoldAccent;
        price.fontStyle = FontStyles.Bold;

        ShopSlot shopSlot = slot.gameObject.AddComponent<ShopSlot>();
        AssignSlotFields(shopSlot, icon, price);

        return shopSlot;
    }

    // ShopSlot mantém icon/priceText privados (mesma encapsulação de ItemSlot) —
    // o builder usa SerializedObject em vez de expor um Configure público só
    // pra esses dois campos de exibição.
    private static void AssignSlotFields(ShopSlot slot, Image icon, TMP_Text price)
    {
        SerializedObject serialized = new(slot);
        serialized.FindProperty("icon").objectReferenceValue = icon;
        serialized.FindProperty("priceText").objectReferenceValue = price;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
        texture.name = "Shop Window Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Shop Window Runtime Sprite";
        return runtimeSprite;
    }
}
