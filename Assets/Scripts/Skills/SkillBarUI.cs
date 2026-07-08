using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SkillBarUI : MonoBehaviour
{
    public static SkillBarUI Instance { get; private set; }

    private const int SlotCount = 9;
    private static Sprite runtimeSprite;
    private static TMP_FontAsset bangersFont;

    private PlayerSkillManager skillManager;
    public PlayerSkillManager SkillManager => skillManager;

    private ResourceManager resourceManager;
    private SkillBarSlot[] slots;
    private Image[] momentumSegments;

    private Image healthFillImage;
    private TMP_Text healthBarText;
    private Image manaFillImage;
    private TMP_Text manaBarText;
    private TMP_Text debugStatsText;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Sem GraphicRaycaster, o EventSystem não faz raycast contra os SkillBarSlot,
        // então eles nunca recebem OnDrop/OnBeginDrag. O SkillBarCanvasBuilder já
        // adiciona um, mas garantimos aqui também pra funcionar mesmo com uma canvas
        // buildada antes dessa correção (sem precisar re-rodar o builder).
        if (!TryGetComponent<GraphicRaycaster>(out _))
            gameObject.AddComponent<GraphicRaycaster>();
    }

    // Chamado pelo GameManager.Start() — a Canvas/hierarquia da barra já vem
    // autorada na cena (Tools > Skill Bar > Build Skill Bar Canvas), então aqui só
    // achamos a instância existente e populamos os slots com o loadout atual.
    public static void EnsureCreated()
    {
        PlayerSkillManager manager = FindAnyObjectByType<PlayerSkillManager>();
        SkillBarUI existing = FindAnyObjectByType<SkillBarUI>();

        if (manager == null || existing == null)
            return;

        existing.Build(manager);
    }

    // Chamado pelo GameManager.RegisterPlayer a cada respawn (destroy+recreate) — o
    // player antigo é destruído junto com seu PlayerSkillManager/ResourceManager,
    // então a barra precisa apontar pra instância nova. Os slots visuais (ícone/nome/
    // tecla) não mudam: guardam referência direta ao Skill (ScriptableObject, asset
    // compartilhado entre instâncias), só a leitura de cooldown/Momentum precisa
    // do objeto novo.
    public static void Rebind(PlayerSkillManager manager)
    {
        SkillBarUI instance = FindAnyObjectByType<SkillBarUI>();

        if (instance != null)
            instance.RebindInternal(manager);
    }

    private void RebindInternal(PlayerSkillManager manager)
    {
        if (resourceManager != null)
            resourceManager.OnResourceChanged -= RefreshMomentum;

        skillManager = manager;
        resourceManager = manager.GetComponent<ResourceManager>();

        // Sem isso, cada SkillBarSlot continua com a referência ao PlayerSkillManager
        // antigo (destruído no respawn) — TickCooldown vê skillManager == null (fake-null
        // da Unity) e para de atualizar, travando o preenchimento de cooldown congelado.
        if (slots != null)
        {
            foreach (SkillBarSlot slot in slots)
                slot.Initialize(manager);
        }

        if (resourceManager != null)
        {
            resourceManager.OnResourceChanged += RefreshMomentum;
            RefreshMomentum();
        }

        RefreshStatsDriven();
    }

    private void OnDestroy()
    {
        if (resourceManager != null)
            resourceManager.OnResourceChanged -= RefreshMomentum;

        if (StatsManager.Instance != null)
            StatsManager.Instance.OnStatsChanged -= RefreshStatsDriven;

        if (Instance == this)
            Instance = null;
    }

    // Update só cuida do que é contínuo de verdade (contagem regressiva de cooldown).
    // Vida/mana/stats mudam por evento — ver RefreshStatsDriven.
    private void Update()
    {
        if (skillManager == null || slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].TickCooldown();
    }

    // Chamado pelo SkillDragController depois de um drop bem-sucedido, pra atualizar
    // ícone/nome do slot imediatamente (sem esperar o próximo tick de cooldown).
    public void RefreshSlot(int index)
    {
        if (slots != null && index >= 0 && index < slots.Length)
            slots[index].Refresh();
    }

    // Repõe todos os slots — usado após um drop do Livro, já que atribuir uma skill
    // pode esvaziar outro slot (a mesma skill não fica em dois lugares).
    public void RefreshAll()
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
            slots[i].Refresh();
    }

    private void RefreshStatsDriven()
    {
        RefreshVitalBars();
        RefreshDebugStats();
    }

    private void Build(PlayerSkillManager manager)
    {
        // Evita reassinar OnStatsChanged / repopular tudo se EnsureCreated() for
        // chamado mais de uma vez na mesma sessão.
        if (slots != null)
            return;

        skillManager = manager;
        resourceManager = manager.GetComponent<ResourceManager>();

        Transform bar = transform.Find("Skill Bar");

        if (bar == null)
        {
            Debug.LogWarning(
                "SkillBarUI: 'Skill Bar' não encontrada na hierarquia — rode Tools > Skill Bar > Build Skill Bar Canvas.",
                this);
        }
        else
        {
            slots = new SkillBarSlot[SlotCount];

            for (int i = 0; i < SlotCount; i++)
            {
                Transform slotTransform = bar.Find($"Skill Slot {i + 1}");
                slots[i] = slotTransform.GetComponent<SkillBarSlot>();
                slots[i].Initialize(manager);
            }
        }

        BuildMomentumBar();
        BuildVitalBars();
        BuildDebugStatsPanel();

        // Vida/mana/atributos só mudam quando OnStatsChanged dispara (dano, regen,
        // level up, equipar) — assinar o evento evita reler tudo a cada frame.
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnStatsChanged += RefreshStatsDriven;

        RefreshStatsDriven();
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
        // 162 em vez de 128 — abre espaço pro XP Canvas (128-154) entre a Skill Bar
        // e o Momentum. Ver Assets/Editor/ExpCanvasBuilder.cs.
        momentumBar.anchoredPosition = new Vector2(0f, 162f);
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

    // Duas barras contínuas (não segmentadas, ao contrário do Momentum) empilhadas
    // acima dele: Mana logo em cima, Vida acima da Mana.
    private void BuildVitalBars()
    {
        const float barWidth = 240f;
        const float barHeight = 24f;

        // 200 em vez de 168 — deslocado pra abrir espaço pro XP Canvas + Momentum
        // realocado (162). Ver Assets/Editor/ExpCanvasBuilder.cs.
        (manaFillImage, manaBarText) = CreateVitalBar(
            "Mana Bar", 200f, barWidth, barHeight, new Color(0.2f, 0.4f, 0.9f, 1f));

        (healthFillImage, healthBarText) = CreateVitalBar(
            "Health Bar", 200f + barHeight + 8f, barWidth, barHeight, new Color(0.8f, 0.15f, 0.15f, 1f));
    }

    private (Image fill, TMP_Text text) CreateVitalBar(
        string objectName, float y, float width, float height, Color fillColor)
    {
        RectTransform bar = CreateUIObject(objectName, transform);
        bar.anchorMin = new Vector2(0.5f, 0f);
        bar.anchorMax = new Vector2(0.5f, 0f);
        bar.pivot = new Vector2(0.5f, 0.5f);
        bar.anchoredPosition = new Vector2(0f, y);
        bar.sizeDelta = new Vector2(width, height);

        Image background = bar.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.04f, 0.05f, 0.07f, 0.85f);

        Image fill = CreateImage("Fill", bar, fillColor);
        fill.sprite = GetRuntimeSprite();
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 1f;
        SetStretch(fill.rectTransform, 2f);

        TMP_Text text = CreateText(objectName + " Text", bar, string.Empty, 16f, TextAlignmentOptions.Center);
        SetStretch(text.rectTransform, 0f);
        text.fontStyle = FontStyles.Bold;

        return (fill, text);
    }

    private void RefreshVitalBars()
    {
        if (StatsManager.Instance == null)
            return;

        StatsManager stats = StatsManager.Instance;

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = stats.MaxHealth > 0
                ? (float)stats.currentHealth / stats.MaxHealth
                : 0f;
            healthBarText.text = $"{stats.currentHealth} / {stats.MaxHealth}";
        }

        if (manaFillImage != null)
        {
            manaFillImage.fillAmount = stats.MaxMana > 0
                ? (float)stats.currentMana / stats.MaxMana
                : 0f;
            manaBarText.text = $"{stats.currentMana} / {stats.MaxMana}";
        }
    }

    // Painel de leitura rápida pra balanceamento — mostra os valores já calculados
    // (Attack Power, Strength total, etc.), nunca os "crus"/base. Ferramenta de dev,
    // não é a UI final de atributos (essa continua sendo a Canvas de Stats à parte).
    private void BuildDebugStatsPanel()
    {
        RectTransform panel = CreateUIObject("Debug Stats Panel", transform);
        panel.anchorMin = new Vector2(0f, 0f);
        panel.anchorMax = new Vector2(0f, 0f);
        panel.pivot = new Vector2(0f, 0f);
        panel.anchoredPosition = new Vector2(16f, 16f);
        panel.sizeDelta = new Vector2(260f, 340f);

        Image background = panel.gameObject.AddComponent<Image>();
        background.sprite = GetRuntimeSprite();
        background.color = new Color(0.04f, 0.05f, 0.07f, 0.75f);

        debugStatsText = CreateText("Stats Text", panel, string.Empty, 17f, TextAlignmentOptions.TopLeft);
        SetStretch(debugStatsText.rectTransform, 10f);
        debugStatsText.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private void RefreshDebugStats()
    {
        if (debugStatsText == null || StatsManager.Instance == null)
            return;

        StatsManager s = StatsManager.Instance;

        debugStatsText.text =
            $"HP: {s.currentHealth}/{s.MaxHealth}\n" +
            $"Mana: {s.currentMana}/{s.MaxMana}\n" +
            $"Strength: {s.strength.Total}\n" +
            $"Agility: {s.agility.Total}\n" +
            $"Intelligence: {s.intelligence.Total}\n" +
            $"Attack Power: {s.AttackPower:0.#}\n" +
            $"Spell Power: {s.SpellPower:0.#}\n" +
            $"Armor: {s.Armor:0.#}\n" +
            $"Crit Chance: {s.CriticalChance:0.#}%\n" +
            $"Crit Damage: {s.CriticalDamage:0.#}%\n" +
            $"Haste: {s.Haste * 100:0.#}%\n" +
            $"Health Regen: {s.HealthRegen:0.##}\n" +
            $"Mana Regen: {s.ManaRegen:0.##}\n" +
            $"Move Speed: {s.MoveSpeed:0.#}";
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

    // Público — SkillBarSlot também usa como placeholder de ícone vazio, pra não
    // duplicar essa textura/sprite runtime numa segunda cópia.
    public static Sprite GetRuntimeSprite()
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
