using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Constrói a barra de skills (9 slots, teclas 1-9) como GameObjects reais na cena,
// em vez de gerada por código em runtime como era antes. SkillBarUI.cs passa a
// ENCONTRAR essa hierarquia (por nome) e só preencher ícone/nome/cooldown de cada
// slot — a estrutura visual (Icon/Cooldown/Key/Name/Cooldown Text) é criada aqui e
// tem que bater exatamente com os nomes que SkillBarUI.PopulateSlot procura.
// Momentum, Vida/Mana e o painel de debug continuam sendo gerados por código em
// SkillBarUI.cs, sem mudança — fora de escopo desta migração.
public static class SkillBarCanvasBuilder
{
    private const float SlotSize = 76f;
    private const int SlotCount = 9;
    private const string CanvasName = "Skill Bar Canvas";

    private static Sprite runtimeSprite;

    [MenuItem("Tools/Skill Bar/Build Skill Bar Canvas")]
    private static void Build()
    {
        GameObject existing = GameObject.Find(CanvasName);

        if (existing != null)
            Undo.DestroyObjectImmediate(existing);

        GameObject canvasObject = new(CanvasName);
        Undo.RegisterCreatedObjectUndo(canvasObject, "Create Skill Bar Canvas");

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Necessário pra que os SkillBarSlot recebam eventos de ponteiro (drag/drop).
        canvasObject.AddComponent<GraphicRaycaster>();

        // O componente já vem "baked" na cena — SkillBarUI.EnsureCreated() só vai
        // achar essa instância e chamar Build(manager) nela, não criar mais nada.
        canvasObject.AddComponent<SkillBarUI>();

        RectTransform bar = CreateUIObject("Skill Bar", canvasObject.transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 28f);
        bar.sizeDelta = new Vector2(SlotSize * SlotCount + 32f, SlotSize + 16f);

        Image barBackground = bar.gameObject.AddComponent<Image>();
        barBackground.sprite = GetRuntimeSprite();
        barBackground.color = new Color(0.04f, 0.05f, 0.07f, 0.8f);

        HorizontalLayoutGroup layout = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        for (int i = 0; i < SlotCount; i++)
            BuildSlot(bar, (i + 1).ToString());

        Selection.activeGameObject = canvasObject;
    }

    // Réplica do CreateSlot que SkillBarUI.cs tinha antes de virar código de
    // runtime — mesma estrutura/cores, sempre no estado "vazio" (sem skill), já que
    // o preenchimento real acontece em runtime via SkillBarUI.PopulateSlot.
    private static void BuildSlot(Transform parent, string key)
    {
        RectTransform slot = CreateUIObject($"Skill Slot {key}", parent);
        slot.sizeDelta = new Vector2(SlotSize, SlotSize);

        LayoutElement layoutElement = slot.gameObject.AddComponent<LayoutElement>();
        layoutElement.preferredWidth = SlotSize;
        layoutElement.preferredHeight = SlotSize;

        Image background = slot.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        Outline outline = slot.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.55f, 0.6f, 0.7f, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Image icon = CreateImage("Icon", slot, new Color(1f, 1f, 1f, 0.9f));
        SetStretch(icon.rectTransform, 7f);
        icon.sprite = GetRuntimeSprite();
        icon.preserveAspect = false;
        icon.color = new Color(0.22f, 0.26f, 0.34f, 1f);

        Image cooldown = CreateImage("Cooldown", slot, new Color(0f, 0f, 0f, 0.72f));
        SetStretch(cooldown.rectTransform, 0f);
        cooldown.sprite = GetRuntimeSprite();
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;
        cooldown.enabled = false;

        TMP_Text keyText = CreateText("Key", slot, key, 19f, TextAlignmentOptions.TopLeft);
        SetStretch(keyText.rectTransform, 5f);
        keyText.fontStyle = FontStyles.Bold;
        keyText.color = new Color(1f, 0.86f, 0.35f, 1f);

        TMP_Text nameText = CreateText("Name", slot, "Empty", 12f, TextAlignmentOptions.Bottom);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 0f);
        nameText.rectTransform.pivot = new Vector2(0.5f, 0f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        nameText.rectTransform.sizeDelta = new Vector2(-8f, 20f);
        nameText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_Text cooldownText = CreateText(
            "Cooldown Text",
            slot,
            string.Empty,
            24f,
            TextAlignmentOptions.Center);
        SetStretch(cooldownText.rectTransform, 0f);
        cooldownText.fontStyle = FontStyles.Bold;
        cooldownText.enabled = false;

        // Fica sem configurar aqui — SkillBarUI.Build() chama Initialize() em runtime,
        // já que precisa do PlayerSkillManager atual (só existe com o jogo rodando).
        slot.gameObject.AddComponent<SkillBarSlot>();
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
        texture.name = "Skill Bar Runtime Texture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        runtimeSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f);
        runtimeSprite.name = "Skill Bar Runtime Sprite";
        return runtimeSprite;
    }
}
