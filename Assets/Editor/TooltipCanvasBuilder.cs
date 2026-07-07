using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Mirrors SkillBookCanvasBuilder/SkillBarCanvasBuilder: idempotent (destroy-by-name
// + recreate), TooltipManager stays scene-baked (not runtime-instantiated), and
// this script only wires the runtime references via Configure() after building the
// hierarchy fresh. Builds two sibling panels (Item/Skill) — TooltipManager toggles
// between them per Show call, only one ever active at a time.
public static class TooltipCanvasBuilder
{
    private const string CanvasName = "Tooltip Canvas";
    private const string LegacyCanvasName = "TooltipCanvas";
    private const string StatRowPrefabPath = "Assets/Prefab/StatRow.prefab";

    // Acima de tudo, inclusive dos Drag Canvas (500) — tooltip nunca pode renderizar
    // atrás de um ghost de drag ou de qualquer outra UI da cena.
    private const int SortingOrder = 1000;

    // Mesmo par cor/distância de Outline usado nos slots de Skill Book/Skill Bar —
    // mantém a moldura do tooltip consistente com o resto da HUD.
    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    [MenuItem("Tools/Tooltip/Build Tooltip Canvas")]
    private static void Build()
    {
        DestroyIfExists(CanvasName);
        DestroyIfExists(LegacyCanvasName);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Tooltip Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SortingOrder;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // CanvasGroup fica sem alpha/interactable/blocksRaycasts setados aqui de
        // propósito — TooltipManager.Awake() força blocksRaycasts=false e
        // interactable=false incondicionalmente; Hide() (chamado em Start()) zera o
        // alpha. Ver TooltipManager.cs.
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        ItemTooltipView itemView = BuildItemPanel(canvasObject.transform);
        SkillTooltipView skillView = BuildSkillPanel(canvasObject.transform);

        // Ambos começam inativos: TooltipManager.Start() já zera o alpha via Hide(),
        // e o primeiro ShowItem()/ShowSkill() real ativa o painel certo e desativa o
        // outro antes de qualquer coisa ficar visível — não há frame com os dois
        // painéis sobrepostos.
        itemView.gameObject.SetActive(false);
        skillView.gameObject.SetActive(false);

        TooltipManager tooltipManager = canvasObject.AddComponent<TooltipManager>();
        tooltipManager.Configure(canvasGroup, itemView, skillView);

        Selection.activeGameObject = canvasObject;
    }

    private static ItemTooltipView BuildItemPanel(Transform parent)
    {
        RectTransform panel = CreatePanel("Item Tooltip", parent);

        TMP_Text nameText = CreateText("Name", panel, string.Empty, 33f, TextAlignmentOptions.TopLeft);
        nameText.fontStyle = FontStyles.Bold;

        TMP_Text rarityText = CreateText("Rarity", panel, string.Empty, 16f, TextAlignmentOptions.TopLeft);

        TMP_Text slotText = CreateText("Slot", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);

        RectTransform statsContainer = CreateUIObject("Stats Container", panel);

        ContentSizeFitter statsFitter = statsContainer.gameObject.AddComponent<ContentSizeFitter>();
        statsFitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        statsFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup statsLayout = statsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        statsLayout.spacing = 1f;
        statsLayout.childAlignment = TextAnchor.UpperLeft;
        statsLayout.childControlWidth = true;
        statsLayout.childControlHeight = true;

        TMP_Text descriptionText = CreateText("Description", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);

        GameObject statRowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StatRowPrefabPath);

        if (statRowPrefab == null)
            Debug.LogWarning($"TooltipCanvasBuilder: StatRow prefab não encontrado em \"{StatRowPrefabPath}\".");

        ItemTooltipView itemView = panel.gameObject.AddComponent<ItemTooltipView>();
        itemView.Configure(nameText, rarityText, slotText, statsContainer, statRowPrefab, descriptionText);
        return itemView;
    }

    private static SkillTooltipView BuildSkillPanel(Transform parent)
    {
        RectTransform panel = CreatePanel("Skill Tooltip", parent);

        TMP_Text nameText = CreateText("Name", panel, string.Empty, 33f, TextAlignmentOptions.TopLeft);
        nameText.fontStyle = FontStyles.Bold;

        TMP_Text levelText = CreateText("Level", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);
        TMP_Text metaLineText = CreateText("Meta Line", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);
        TMP_Text descriptionText = CreateText("Description", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);

        SkillTooltipView skillView = panel.gameObject.AddComponent<SkillTooltipView>();
        skillView.Configure(nameText, levelText, metaLineText, descriptionText);
        return skillView;
    }

    private static RectTransform CreatePanel(string objectName, Transform parent)
    {
        RectTransform panel = CreateUIObject(objectName, parent);
        panel.anchorMin = new Vector2(0f, 1f);
        panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(0f, 1f);
        panel.anchoredPosition = Vector2.zero;

        // Cor sólida (mesmo pixel branco tintado que Skill Book/Skill Bar usam) em
        // vez de uma imagem de fundo — se um dia quiser um background com textura
        // de verdade, é só trocar o sprite aqui.
        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.04f, 0.05f, 0.07f, 0.92f);
        background.raycastTarget = false;

        Outline outline = panel.gameObject.AddComponent<Outline>();
        outline.effectColor = OutlineColor;
        outline.effectDistance = OutlineDistance;

        ContentSizeFitter fitter = panel.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 6f;
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        return panel;
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

    private static Sprite GetRuntimeSprite()
    {
        if (runtimeSprite != null)
            return runtimeSprite;

        Texture2D texture = new(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Tooltip Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Tooltip Runtime Sprite";
        return runtimeSprite;
    }
}
