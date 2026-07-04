using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    private const float SlotSize = 76f;
    private static Sprite runtimeSprite;

    private PlayerSkillManager skillManager;
    private ResourceManager resourceManager;
    private SkillSlotUI[] slots;
    private Image[] momentumSegments;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForCurrentScene()
    {
        PlayerSkillManager manager = FindAnyObjectByType<PlayerSkillManager>();

        if (manager == null || FindAnyObjectByType<SkillBarUI>() != null)
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

        MomentumUI legacyMomentumUI = FindAnyObjectByType<MomentumUI>();

        if (legacyMomentumUI != null)
            legacyMomentumUI.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
            resourceManager.OnResourceChanged -= RefreshMomentum;
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
        resourceManager = manager.GetComponent<ResourceManager>();

        IReadOnlyList<Skill> equippedSkills = manager.EquippedSkills;

        RectTransform bar = CreateUIObject("Skill Bar", transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 28f);
        bar.sizeDelta = new Vector2(SlotSize * equippedSkills.Count + 32f, SlotSize + 16f);

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

        slots = new SkillSlotUI[equippedSkills.Count];
        for (int i = 0; i < equippedSkills.Count; i++)
            slots[i] = CreateSlot(bar, equippedSkills[i], (i + 1).ToString());

        BuildMomentumBar();
    }

    private void BuildMomentumBar()
    {
        if (resourceManager == null || resourceManager.MaxResource <= 0)
            return;

        const float segmentWidth = 34f;
        const float segmentHeight = 18f;
        const float spacing = 5f;
        const float horizontalPadding = 8f;

        int segmentCount = resourceManager.MaxResource;
        float barWidth =
            segmentCount * segmentWidth +
            (segmentCount - 1) * spacing +
            horizontalPadding * 2f;

        RectTransform momentumBar = CreateUIObject("Momentum Bar", transform);
        momentumBar.anchorMin = new Vector2(0.5f, 0f);
        momentumBar.anchorMax = new Vector2(0.5f, 0f);
        momentumBar.pivot = new Vector2(0.5f, 0f);
        momentumBar.anchoredPosition = new Vector2(0f, 128f);
        momentumBar.sizeDelta = new Vector2(barWidth, segmentHeight + 12f);

        Image background = momentumBar.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.04f, 0.05f, 0.07f, 0.85f);

        HorizontalLayoutGroup layout = momentumBar.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(
            Mathf.RoundToInt(horizontalPadding),
            Mathf.RoundToInt(horizontalPadding),
            6,
            6);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        momentumSegments = new Image[segmentCount];

        for (int i = 0; i < segmentCount; i++)
        {
            RectTransform segment = CreateUIObject($"Momentum {i + 1}", momentumBar);
            segment.sizeDelta = new Vector2(segmentWidth, segmentHeight);

            LayoutElement layoutElement = segment.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = segmentWidth;
            layoutElement.preferredHeight = segmentHeight;

            Image image = segment.gameObject.AddComponent<Image>();
            image.sprite = GetRuntimeSprite();
            image.raycastTarget = false;

            Outline outline = segment.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.65f, 0.7f, 0.8f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            momentumSegments[i] = image;
        }

        resourceManager.OnResourceChanged += RefreshMomentum;
        RefreshMomentum();
    }

    private void RefreshMomentum()
    {
        if (resourceManager == null || momentumSegments == null)
            return;

        for (int i = 0; i < momentumSegments.Length; i++)
        {
            bool filled = i < resourceManager.CurrentResource;
            momentumSegments[i].color = filled
                ? new Color(1f, 0.64f, 0.12f, 1f)
                : new Color(0.16f, 0.18f, 0.22f, 1f);
        }
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
        nameText.textWrappingMode = TextWrappingModes.NoWrap;

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
