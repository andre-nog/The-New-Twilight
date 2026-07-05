using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Mockup visual do Livro de Skills — constrói objetos reais na cena (não em runtime,
// diferente do SkillBarUI) pra revisão de layout antes de conectar qualquer lógica
// (tecla C, drag-and-drop pro SkillBarUI, tooltip de descrição). Estilo (cores,
// tamanhos, fonte) espelhado de SkillBarUI.cs pra manter consistência visual com a
// hotbar já existente.
public static class SkillBookCanvasBuilder
{
    private const float SlotSize = 76f;
    private const float GridSpacing = 8f;
    private const float GridPadding = 12f;
    private const float TitleBarHeight = 32f;
    private const int Columns = 3;
    private const string CanvasName = "Skill Book Canvas";

    private static Sprite runtimeSprite;

    // Lista fixa e ordenada das skills que já existem no projeto — casadas pelo
    // campo Skill.skillName (mais robusto que casar pelo nome do arquivo do asset).
    private static readonly (string skillName, bool locked)[] SkillEntries =
    {
        ("Auto Attack", false),
        ("Power Strike", false),
        ("Stomp", false),
    };

    [MenuItem("Tools/Skill Book/Build Skill Book Canvas")]
    private static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Skill Book Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 150;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        // O componente de abrir/fechar (tecla C) já vem "baked" na cena — precisa de
        // um CanvasGroup pra controlar alpha/interactable/blocksRaycasts. O campo
        // Toggle Action fica sem wireiar aqui: é uma InputActionReference de projeto,
        // arrastada manualmente na Inspector depois de criada (mesmo padrão de
        // InventoryManager.toggleInventory).
        CanvasGroup canvasGroup = canvasObject.AddComponent<CanvasGroup>();
        SkillBookUI skillBookUI = canvasObject.AddComponent<SkillBookUI>();
        skillBookUI.canvasGroup = canvasGroup;

        float panelWidth = Columns * SlotSize + (Columns - 1) * GridSpacing + GridPadding * 2f;
        float gridHeight = SlotSize + GridPadding * 2f;
        float panelHeight = TitleBarHeight + gridHeight;

        RectTransform panel = CreateUIObject("Skill Book Panel", canvasObject.transform);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = Vector2.zero;
        panel.sizeDelta = new Vector2(panelWidth, panelHeight);

        Image panelBackground = panel.gameObject.AddComponent<Image>();
        panelBackground.sprite = GetRuntimeSprite();
        panelBackground.color = new Color(0.04f, 0.05f, 0.07f, 0.9f);

        TMP_Text title = CreateText("Title", panel, "Skills", 20f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        title.rectTransform.anchorMin = new Vector2(0.5f, 1f);
        title.rectTransform.anchorMax = new Vector2(0.5f, 1f);
        title.rectTransform.pivot = new Vector2(0.5f, 1f);
        title.rectTransform.anchoredPosition = Vector2.zero;
        title.rectTransform.sizeDelta = new Vector2(panelWidth, TitleBarHeight);

        RectTransform grid = CreateUIObject("Skill Grid", panel);
        grid.anchorMin = new Vector2(0.5f, 0f);
        grid.anchorMax = new Vector2(0.5f, 0f);
        grid.pivot = new Vector2(0.5f, 0f);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = new Vector2(panelWidth, gridHeight);

        GridLayoutGroup layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.padding = new RectOffset(
            Mathf.RoundToInt(GridPadding),
            Mathf.RoundToInt(GridPadding),
            Mathf.RoundToInt(GridPadding),
            Mathf.RoundToInt(GridPadding));
        layout.cellSize = new Vector2(SlotSize, SlotSize);
        layout.spacing = new Vector2(GridSpacing, GridSpacing);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Columns;

        foreach ((string skillName, bool locked) in SkillEntries)
        {
            Skill skill = FindSkillByName(skillName);

            if (skill == null)
                Debug.LogWarning($"SkillBookCanvasBuilder: nenhum asset de Skill encontrado com skillName \"{skillName}\".");

            BuildSlot(grid, skill, skillName, locked);
        }

        Selection.activeGameObject = canvasObject;
    }

    private static Skill FindSkillByName(string skillName)
    {
        string[] guids = AssetDatabase.FindAssets("t:Skill");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Skill skill = AssetDatabase.LoadAssetAtPath<Skill>(path);

            if (skill != null && skill.skillName == skillName)
                return skill;
        }

        return null;
    }

    private static void BuildSlot(Transform parent, Skill skill, string displayName, bool locked)
    {
        RectTransform slot = CreateUIObject($"Skill Slot ({displayName})", parent);
        slot.sizeDelta = new Vector2(SlotSize, SlotSize);

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        Outline outline = slot.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.6f, 0.7f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        bool hasIcon = skill != null && skill.icon != null;

        Image icon = CreateImage("Icon", slot, new Color(1f, 1f, 1f, 0.9f));
        SetStretch(icon.rectTransform, 7f);
        icon.sprite = hasIcon ? skill.icon : GetRuntimeSprite();
        icon.preserveAspect = hasIcon;
        icon.color = locked
            ? new Color(0.22f, 0.26f, 0.34f, 1f)
            : Color.white;

        TMP_Text nameText = CreateText("Name", slot, displayName, 12f, TextAlignmentOptions.Bottom);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 0f);
        nameText.rectTransform.pivot = new Vector2(0.5f, 0f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        nameText.rectTransform.sizeDelta = new Vector2(-8f, 20f);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.color = locked ? new Color(0.6f, 0.6f, 0.6f, 1f) : Color.white;

        // Overlay de "bloqueada" — mesma linguagem visual do overlay de cooldown da
        // hotbar, só que sem preenchimento radial: liga/desliga por inteiro.
        Image lockedOverlay = CreateImage("Locked Overlay", slot, new Color(0f, 0f, 0f, 0.55f));
        SetStretch(lockedOverlay.rectTransform, 0f);
        lockedOverlay.sprite = GetRuntimeSprite();
        lockedOverlay.gameObject.SetActive(locked);

        SkillBookSlot bookSlot = slot.gameObject.AddComponent<SkillBookSlot>();
        bookSlot.Configure(skill, locked, icon);
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
        texture.name = "Skill Book Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Skill Book Runtime Sprite";
        return runtimeSprite;
    }
}
