using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    private const float SlotSize = 76f;
    private static Sprite runtimeSprite;

    private PlayerSkillManager skillManager;
    private SkillSlotUI[] slots;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForCurrentScene()
    {
        PlayerSkillManager manager = FindFirstObjectByType<PlayerSkillManager>();

        if (manager == null || FindFirstObjectByType<SkillBarUI>() != null)
            return;

        GameObject canvasObject = new("Skill Bar Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        SkillBarUI skillBar = canvasObject.AddComponent<SkillBarUI>();
        skillBar.Build(manager);
    }

    private void Update()
    {
        if (skillManager == null || slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Refresh(skillManager);
    }

    private void Build(PlayerSkillManager manager)
    {
        skillManager = manager;

        RectTransform bar = CreateUIObject("Skill Bar", transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 28f);
        bar.sizeDelta = new Vector2(SlotSize * 3f + 32f, SlotSize + 16f);

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

        slots = new[]
        {
            CreateSlot(bar, manager.autoAttack, "1"),
            CreateSlot(bar, manager.powerStrike, "2"),
            CreateSlot(bar, manager.stomp, "3")
        };
    }

    private static SkillSlotUI CreateSlot(Transform parent, Skill skill, string key)
    {
        RectTransform slot = CreateUIObject($"Skill {key}", parent);
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
        icon.sprite = skill != null && skill.icon != null ? skill.icon : GetRuntimeSprite();
        icon.preserveAspect = skill != null && skill.icon != null;
        icon.color = skill != null && skill.icon != null
            ? Color.white
            : new Color(0.22f, 0.26f, 0.34f, 1f);

        Image cooldown = CreateImage("Cooldown", slot, new Color(0f, 0f, 0f, 0.72f));
        SetStretch(cooldown.rectTransform, 0f);
        cooldown.sprite = GetRuntimeSprite();
        cooldown.type = Image.Type.Filled;
        cooldown.fillMethod = Image.FillMethod.Radial360;
        cooldown.fillOrigin = (int)Image.Origin360.Top;
        cooldown.fillClockwise = false;
        cooldown.fillAmount = 0f;

        TMP_Text keyText = CreateText("Key", slot, key, 19f, TextAlignmentOptions.TopLeft);
        SetStretch(keyText.rectTransform, 5f);
        keyText.fontStyle = FontStyles.Bold;
        keyText.color = new Color(1f, 0.86f, 0.35f, 1f);

        string displayName = skill != null ? skill.skillName : "Empty";
        TMP_Text nameText = CreateText("Name", slot, displayName, 12f, TextAlignmentOptions.Bottom);
        nameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 0f);
        nameText.rectTransform.pivot = new Vector2(0.5f, 0f);
        nameText.rectTransform.anchoredPosition = new Vector2(0f, 4f);
        nameText.rectTransform.sizeDelta = new Vector2(-8f, 20f);
        nameText.enableWordWrapping = false;

        TMP_Text cooldownText = CreateText(
            "Cooldown Text",
            slot,
            string.Empty,
            24f,
            TextAlignmentOptions.Center);
        SetStretch(cooldownText.rectTransform, 0f);
        cooldownText.fontStyle = FontStyles.Bold;

        return new SkillSlotUI(skill, cooldown, cooldownText);
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
        text.overflowMode = TextOverflowModes.Ellipsis;
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

    private sealed class SkillSlotUI
    {
        private readonly Skill skill;
        private readonly Image cooldownImage;
        private readonly TMP_Text cooldownText;

        public SkillSlotUI(Skill skill, Image cooldownImage, TMP_Text cooldownText)
        {
            this.skill = skill;
            this.cooldownImage = cooldownImage;
            this.cooldownText = cooldownText;
        }

        public void Refresh(PlayerSkillManager manager)
        {
            float remaining = manager.GetRemainingCooldown(skill);
            float duration = skill != null ? skill.cooldown : 0f;
            bool coolingDown = remaining > 0f && duration > 0f;

            cooldownImage.enabled = coolingDown;
            cooldownText.enabled = coolingDown;

            if (!coolingDown)
                return;

            cooldownImage.fillAmount = Mathf.Clamp01(remaining / duration);
            cooldownText.text = remaining >= 1f
                ? Mathf.CeilToInt(remaining).ToString()
                : remaining.ToString("0.0");
        }
    }
}
