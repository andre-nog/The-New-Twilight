using System.Collections.Generic;
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
    private const float SlotSize = 92f;
    private const float GridSpacing = 14f;
    private const float GridPadding = 16f;
    private const float TitleBarHeight = 38f;
    private const int Columns = 3;
    private const string CanvasName = "Skill Book Canvas";

    private static readonly Color OutlineColor = new(0.55f, 0.6f, 0.7f, 0.9f);
    private static readonly Vector2 OutlineDistance = new(2f, -2f);
    private static readonly Color ButtonColor = new(0.12f, 0.14f, 0.18f, 0.95f);

    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;
    private static TMP_FontAsset defaultFont;

    // Lista fixa e ordenada das skills que já existem no projeto — casadas pelo
    // campo Skill.skillName (mais robusto que casar pelo nome do arquivo do asset).
    // Travado/aprendido NÃO é mais baked aqui: vem da SkillProgression em runtime.
    private static readonly string[] SkillNames =
    {
        "Auto Attack",
        "Power Strike",
        "Stomp",
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

        // Faltava a moldura que Tooltip/Quest Window têm — sem isso o painel ficava
        // "lavado", sem contraste contra o fundo do jogo.
        Outline panelOutline = panel.gameObject.AddComponent<Outline>();
        panelOutline.effectColor = OutlineColor;
        panelOutline.effectDistance = OutlineDistance;

        TMP_Text title = CreateText("Title", panel, "Skills", 25f, TextAlignmentOptions.Center);
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

        List<SkillBookSlot> bookSlots = new();

        foreach (string skillName in SkillNames)
        {
            Skill skill = FindSkillByName(skillName);

            if (skill == null)
                Debug.LogWarning($"SkillBookCanvasBuilder: nenhum asset de Skill encontrado com skillName \"{skillName}\".");

            bookSlots.Add(BuildSlot(grid, skill, skillName));
        }

        TMP_Text pointsText = BuildPointsLabel(panel);
        skillBookUI.Configure(bookSlots.ToArray(), pointsText);

        BuildCloseButton(panel);

        Selection.activeGameObject = canvasObject;
    }

    // "X" no canto superior direito do painel — mesmo estilo/tamanho do close
    // button da Quest Window. Painel aqui não tem LayoutGroup (Title/Grid/Points
    // já são posicionados manualmente por anchor), então não precisa de
    // LayoutElement.ignoreLayout como lá.
    private static void BuildCloseButton(Transform panelParent)
    {
        RectTransform rect = CreateUIObject("Close Button", panelParent);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-6f, -6f);
        rect.sizeDelta = new Vector2(22f, 22f);

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

        rect.gameObject.AddComponent<SkillBookCloseButton>();
    }

    // "Points: N" no canto superior direito da barra de título (o título "Skills"
    // fica centralizado; textos curtos não colidem).
    private static TMP_Text BuildPointsLabel(RectTransform panel)
    {
        TMP_Text points = CreateText("Points", panel, "Points: 0", 18f, TextAlignmentOptions.Right);
        points.rectTransform.anchorMin = new Vector2(0f, 1f);
        points.rectTransform.anchorMax = new Vector2(1f, 1f);
        points.rectTransform.pivot = new Vector2(0.5f, 1f);
        // Folga bem maior que o botão de fechar (22 + 6 de gap) pra não ficar
        // grudado nele — primeira tentativa (34/16) ainda ficava perto demais.
        points.rectTransform.anchoredPosition = new Vector2(-26f, 0f);
        points.rectTransform.sizeDelta = new Vector2(-50f, TitleBarHeight);
        points.textWrappingMode = TextWrappingModes.NoWrap;
        points.fontStyle = FontStyles.Bold;
        return points;
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

    private static SkillBookSlot BuildSlot(Transform parent, Skill skill, string displayName)
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
        icon.color = Color.white; // o Refresh() em runtime escurece se estiver travada

        TMP_Text nameText = CreateText("Name", slot, displayName, 15f, TextAlignmentOptions.Bottom);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 0f);
        nameText.rectTransform.pivot = new Vector2(0.5f, 0f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        nameText.rectTransform.sizeDelta = new Vector2(-8f, 20f);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;
        nameText.color = Color.white;

        // Pips de nível (topo do slot) — preenchido pelo Refresh() em runtime (●●○).
        TMP_Text pips = CreateText("Pips", slot, string.Empty, 15f, TextAlignmentOptions.Top);
        pips.rectTransform.anchorMin = new Vector2(0f, 1f);
        pips.rectTransform.anchorMax = new Vector2(1f, 1f);
        pips.rectTransform.pivot = new Vector2(0.5f, 1f);
        pips.rectTransform.anchoredPosition = new Vector2(0f, -2f);
        pips.rectTransform.sizeDelta = new Vector2(-8f, 16f);
        pips.textWrappingMode = TextWrappingModes.NoWrap;

        // Overlay de "bloqueada" — raycastTarget desligado (via CreateImage) pra não
        // engolir o clique do botão "+" quando a skill está travada mas aprendível.
        // Criado ANTES do botão "+" pra ser sibling anterior: UGUI desenha por ordem
        // de filho, então o "+" (criado depois) fica por cima do overlay. Antes o "+"
        // sumia sob o overlay preto 55% justo quando a skill tava travada — que é
        // exatamente o estado em que precisa do "+" pra gastar ponto.
        Image lockedOverlay = CreateImage("Locked Overlay", slot, new Color(0f, 0f, 0f, 0.55f));
        SetStretch(lockedOverlay.rectTransform, 0f);
        lockedOverlay.sprite = GetRuntimeSprite();
        lockedOverlay.gameObject.SetActive(false);

        Button plusButton = BuildPlusButton(slot);

        SkillBookSlot bookSlot = slot.gameObject.AddComponent<SkillBookSlot>();
        bookSlot.Configure(skill, icon, plusButton, pips, lockedOverlay.gameObject);
        return bookSlot;
    }

    // Botãozinho "+" no canto superior direito do slot. Tem raycastTarget próprio
    // (Image) e é um filho separado, então o clique nele não dispara o drag do corpo
    // do slot. O Refresh() liga/desliga o interactable conforme dá pra aprender/upar.
    private static Button BuildPlusButton(Transform parent)
    {
        RectTransform rect = CreateUIObject("Plus Button", parent);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-2f, -2f);
        rect.sizeDelta = new Vector2(26f, 26f);

        Image image = rect.gameObject.AddComponent<Image>();
        image.sprite = GetRuntimeSprite();
        image.color = new Color(0.18f, 0.5f, 0.24f, 1f);
        image.raycastTarget = true;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;

        TMP_Text plus = CreateText("Plus Text", rect, "+", 30f, TextAlignmentOptions.Center);
        SetStretch(plus.rectTransform, 0f);
        plus.fontStyle = FontStyles.Bold;

        // Truncate (padrão do CreateText) corta o glifo no limite do box 26x26, que
        // travava o "+" em ~19. Overflow deixa desenhar além dos bounds, centralizado,
        // então a fonte pode subir sem clip.
        plus.overflowMode = TextOverflowModes.Overflow;

        // Fica na fonte padrão do TMP por pedido, não a Bangers SDF que o resto
        // do Skill Book usa — CreateText já aplicou Bangers, sobrescreve de volta.
        TMP_FontAsset plusFont = GetDefaultFont();

        if (plusFont != null)
            plus.font = plusFont;

        return button;
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

    private static TMP_FontAsset GetDefaultFont()
    {
        if (defaultFont == null)
            defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        return defaultFont;
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
