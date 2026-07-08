using UnityEngine;
using System;

[DefaultExecutionOrder(-100)]
public class StatsManager : MonoBehaviour
{
    public static StatsManager Instance;
    public event Action OnStatsChanged;

    // Disparado só quando o nível muda de fato (level up em jogo ou SetLevel no
    // carregamento de save) — separado de OnStatsChanged, que pulsa a cada tick de
    // regen. Quem depende de nível (progressão de skills) escuta este.
    public event Action OnLevelChanged;

    [Header("Class")]
    [Tooltip("Classe atual — dona do atributo primário, dos atributos de nível 1 e do crescimento por nível. Promoção de classe = SetClass() + PlayerSkillManager.RebuildLoadout().")]
    [SerializeField] private ClassDefinitionSO currentClass;

    public ClassDefinitionSO CurrentClass => currentClass;

    [Header("Level")]
    [Tooltip("Mudar isso recalcula os atributos como se você tivesse subido de nível até aqui, do zero (nível 1 + crescimento por nível abaixo) — funciona no Inspector, em Play ou fora dele, pra ajudar a balancear cada nível sem precisar farmar XP.")]
    [Min(1)]
    public int level = 1;

    [Header("Primary Attributes (Bonus = equip/buff/passiva; Base é derivado do nível + classe)")]
    public PrimaryStat strength;
    public PrimaryStat agility;
    public PrimaryStat intelligence;

    [Header("Secondary Stats - Baseline")]
    [Tooltip("Valor fixo antes de qualquer escala por atributo ou bônus externo.")]
    [SerializeField] private int baseMaxHealth = 40;
    [SerializeField] private float baseHealthRegen = 0f;
    [SerializeField] private int baseMaxMana = 0;
    [SerializeField] private float baseManaRegen = 0f;
    [SerializeField] private float baseAttackPower = 10f;
    [SerializeField] private float baseSpellPower = 0f;
    [SerializeField] private float baseArmor = 0f;
    [SerializeField] private float baseMoveSpeed = 3f;
    [Range(0, 100)]
    [SerializeField] private float baseCriticalChance = 0f;
    [SerializeField] private float baseCriticalDamage = 100f;

    [Header("Secondary Stats - Scaling per Attribute Point")]
    [SerializeField] private float healthPerStrength = 5f;
    [SerializeField] private float healthRegenPerStrength = 0.05f;
    [SerializeField] private float attackPowerPerStrength = 1f;

    [SerializeField] private float critChancePerAgility = 0.2f;
    [Tooltip("Constante K da fórmula de Haste: Haste = Agility / (Agility + K). Ex.: com K=2000, 500 de Agility vira 20% de Haste — mesmo formato de retorno decrescente do Armor.")]
    [SerializeField] private float hasteConstant = 2000f;
    [SerializeField] private float attackPowerPerAgility = 1f;

    [SerializeField] private float manaPerIntelligence = 5f;
    [SerializeField] private float manaRegenPerIntelligence = 0.05f;
    [SerializeField] private float spellPowerPerIntelligence = 1f;

    // Acumuladores de bônus externo (equipamento/buff/passiva) por stat secundário.
    // RecalculateStats soma isso em cima do valor derivado do atributo primário —
    // Armor e Critical Damage não têm parte derivada, então são só baseline + bônus.
    private float maxHealthBonus;
    private float healthRegenBonus;
    private float maxManaBonus;
    private float manaRegenBonus;
    private float attackPowerBonus;
    private float spellPowerBonus;
    private float armorBonus;
    private float moveSpeedBonus;
    private float criticalChanceBonus;
    private float criticalDamageBonus;
    private float hasteBonus;

    // Frações de HealthRegen/ManaRegen acumuladas entre um frame e outro — os stats
    // são "por segundo", então só aplicamos quando a soma bate um ponto inteiro.
    private float healthRegenAccumulator;
    private float manaRegenAccumulator;

    public int currentHealth;
    public int currentMana;

    // Stats secundários calculados — o resto do jogo só lê daqui, nunca recalcula.
    public int MaxHealth { get; private set; }
    public float HealthRegen { get; private set; }
    public int MaxMana { get; private set; }
    public float ManaRegen { get; private set; }
    public float AttackPower { get; private set; }
    public float SpellPower { get; private set; }
    public float Armor { get; private set; }
    public float MoveSpeed { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamage { get; private set; }
    public float Haste { get; private set; }

    // Último "level" visto por OnValidate — não-serializado, então some num domain
    // reload (recompile com a cena aberta); re-sincronizado em Awake/OnEnable pra não
    // disparar um OnLevelChanged espúrio (0 != level) no primeiro OnValidate seguinte.
    private int cachedLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RecalculateStats();
        currentHealth = MaxHealth;
        currentMana = MaxMana;
        cachedLevel = level;
    }

    // Recompilar scripts no Editor zera campos static (domain reload) sem rodar Awake()
    // de novo para objetos que já existiam na cena — só OnEnable roda. Sem isso, Instance
    // fica null até a cena recarregar de verdade, derrubando quem depende dele.
    private void OnEnable()
    {
        if (Instance != null && Instance != this)
            return;

        Instance = this;

        // MaxHealth/AttackPower/etc. são propriedades calculadas, não seriais — um
        // recompile de script com a cena aberta (domain reload) as zera sem rodar
        // Awake() de novo. Recalcular aqui evita ficar com stats zerados até a
        // próxima mudança (equipar, level up, etc.) disparar isso de novo.
        RecalculateStats();
        cachedLevel = level;
    }

    private void Update()
    {
        TickRegen(HealthRegen, MaxHealth, ref healthRegenAccumulator, ref currentHealth);
        TickRegen(ManaRegen, MaxMana, ref manaRegenAccumulator, ref currentMana);
    }

    // HealthRegen/ManaRegen são "pontos por segundo" — acumula a fração e só aplica
    // (e só dispara OnStatsChanged) quando já der pra somar um ponto inteiro.
    private void TickRegen(float regenPerSecond, int max, ref float accumulator, ref int current)
    {
        if (regenPerSecond <= 0f || current >= max)
            return;

        accumulator += regenPerSecond * Time.deltaTime;

        int gained = Mathf.FloorToInt(accumulator);

        if (gained <= 0)
            return;

        accumulator -= gained;
        current = Mathf.Clamp(current + gained, 0, max);
        OnStatsChanged?.Invoke();
    }

    // Chamado pelo Editor sempre que um campo muda no Inspector (level, growth per
    // level, etc.) — recalcula tudo na hora, então mudar o level já simula todos os
    // level ups até ali sem precisar entrar em Play.
    //
    // Também dispara OnLevelChanged quando "level" foi o campo alterado (comparado
    // contra cachedLevel) — sem isso, editar level no Inspector durante o Play não
    // avisa quem depende de nível (XP UI, progressão de skills), que só atualizavam
    // na próxima mudança "de verdade" (GainExperience, etc.).
    private void OnValidate()
    {
        RecalculateStats();

        if (level == cachedLevel)
            return;

        cachedLevel = level;
        OnStatsChanged?.Invoke();
        OnLevelChanged?.Invoke();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ChangeHealth(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);

        OnStatsChanged?.Invoke();
    }

    public void FullHeal()
    {
        currentHealth = MaxHealth;
        OnStatsChanged?.Invoke();
    }

    public bool HasMana(int amount)
    {
        return currentMana >= amount;
    }

    public bool SpendMana(int amount)
    {
        if (!HasMana(amount))
            return false;

        currentMana -= amount;
        OnStatsChanged?.Invoke();
        return true;
    }

    public void RestoreFullMana()
    {
        currentMana = MaxMana;
        OnStatsChanged?.Invoke();
    }

    // Chamado ao subir de nível — todo o crescimento é automático, o jogador não
    // distribui pontos. O crescimento em si é derivado de "level" dentro de
    // RecalculateStats, então aqui só precisa incrementar o nível.
    // Subir de nível restaura HP e mana por completo (decisão de design — ver GDD).
    public void OnLevelUp()
    {
        level++;

        RecalculateStats();

        currentHealth = MaxHealth;
        currentMana = MaxMana;

        OnStatsChanged?.Invoke();
        OnLevelChanged?.Invoke();
    }

    // Hook da promoção de classe — troca a definição e re-deriva tudo do zero.
    // Quem chamar deve também reconstruir o loadout (PlayerSkillManager.RebuildLoadout).
    public void SetClass(ClassDefinitionSO newClass)
    {
        currentClass = newClass;

        RecalculateStats();
        OnStatsChanged?.Invoke();
    }

    // Usado pelo carregamento de save — define o nível diretamente (ao contrário de
    // OnLevelUp, que só incrementa em 1) e recalcula os stats a partir dele.
    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        RecalculateStats();
        OnStatsChanged?.Invoke();
        OnLevelChanged?.Invoke();
    }

    // Usado pelo carregamento de save — define vida/mana atuais diretamente. Chamar
    // depois de SetClass/SetLevel, para MaxHealth/MaxMana já refletirem o save antes do clamp.
    public void SetVitals(int health, int mana)
    {
        currentHealth = Mathf.Clamp(health, 0, MaxHealth);
        currentMana = Mathf.Clamp(mana, 0, MaxMana);

        OnStatsChanged?.Invoke();
    }

    // Contrato de save — GameManager compõe PlayerSave a partir disto em vez de ler
    // currentClass/level/currentHealth/currentMana direto (mesmo padrão de
    // InventoryManager/EquipmentManager/QuestManager/GoldManager). O carregamento
    // continua chamando SetClass/SetLevel/SetVitals diretamente (já eram métodos
    // públicos) — a ordem entre eles e Equipment/Inventory.ApplyState importa
    // (SetVitals precisa vir depois do equipamento recalcular MaxHealth/MaxMana
    // com os bônus de gear), então não dá pra combinar tudo num ApplyState só.
    public StatsSave GetState()
    {
        return new StatsSave
        {
            classId = currentClass != null ? currentClass.Id : null,
            level = level,
            currentHealth = currentHealth,
            currentMana = currentMana
        };
    }

    public void AddModifier(StatModifier modifier)
    {
        ApplyModifier(modifier, 1);
        RecalculateStats();
        OnStatsChanged?.Invoke();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        ApplyModifier(modifier, -1);
        RecalculateStats();
        OnStatsChanged?.Invoke();
    }

    // Só acumula o bônus bruto por stat. Nada aqui deriva valor final — quem faz
    // isso é sempre RecalculateStats, então nunca fica dessincronizado com o que
    // primaryAttribute ou o level atual dizem.
    private void ApplyModifier(StatModifier modifier, int multiplier)
    {
        float value = modifier.amount * multiplier;

        switch (modifier.stat)
        {
            case StatType.Strength:
                strength.Bonus += (int)value;
                break;

            case StatType.Agility:
                agility.Bonus += (int)value;
                break;

            case StatType.Intelligence:
                intelligence.Bonus += (int)value;
                break;

            case StatType.Armor:
                armorBonus += value;
                break;

            case StatType.AttackPower:
                attackPowerBonus += value;
                break;

            case StatType.SpellPower:
                spellPowerBonus += value;
                break;

            case StatType.MaxHealth:
                maxHealthBonus += value;
                break;

            case StatType.HealthRegen:
                healthRegenBonus += value;
                break;

            case StatType.MaxMana:
                maxManaBonus += value;
                break;

            case StatType.ManaRegen:
                manaRegenBonus += value;
                break;

            case StatType.MoveSpeed:
                moveSpeedBonus += value;
                break;

            case StatType.CriticalChance:
                criticalChanceBonus += value;
                break;

            case StatType.CriticalDamage:
                criticalDamageBonus += value;
                break;

            case StatType.Haste:
                hasteBonus += value;
                break;
        }
    }

    // Único lugar do jogo que calcula fórmula de atributo. Precisa ser chamado
    // sempre que algo mudar (level up, equipar/desequipar, ganhar/perder buff);
    // o resto do jogo só lê as propriedades calculadas acima.
    private void RecalculateStats()
    {
        // Base é sempre derivado do zero a partir do nível atual + classe — simula
        // todos os level ups de uma vez, então mudar "level" (no Inspector ou via
        // OnLevelUp) ou trocar de classe já reflete o resultado final direto.
        // Fallbacks neutros caso a classe ainda não esteja atribuída na cena.
        int baseStrength = currentClass != null ? currentClass.baseStrength : 1;
        int baseAgility = currentClass != null ? currentClass.baseAgility : 1;
        int baseIntelligence = currentClass != null ? currentClass.baseIntelligence : 1;
        int strengthPerLevel = currentClass != null ? currentClass.strengthPerLevel : 1;
        int agilityPerLevel = currentClass != null ? currentClass.agilityPerLevel : 1;
        int intelligencePerLevel = currentClass != null ? currentClass.intelligencePerLevel : 1;
        PrimaryAttribute primaryAttribute = currentClass != null ? currentClass.primaryAttribute : PrimaryAttribute.Strength;

        strength.Base = baseStrength + (level - 1) * strengthPerLevel;
        agility.Base = baseAgility + (level - 1) * agilityPerLevel;
        intelligence.Base = baseIntelligence + (level - 1) * intelligencePerLevel;

        int strTotal = strength.Total;
        int agiTotal = agility.Total;
        int intTotal = intelligence.Total;

        MaxHealth = baseMaxHealth
            + Mathf.RoundToInt(strTotal * healthPerStrength)
            + Mathf.RoundToInt(maxHealthBonus);

        HealthRegen = baseHealthRegen
            + strTotal * healthRegenPerStrength
            + healthRegenBonus;

        MaxMana = baseMaxMana
            + Mathf.RoundToInt(intTotal * manaPerIntelligence)
            + Mathf.RoundToInt(maxManaBonus);

        ManaRegen = baseManaRegen
            + intTotal * manaRegenPerIntelligence
            + manaRegenBonus;

        CriticalChance = baseCriticalChance
            + agiTotal * critChancePerAgility
            + criticalChanceBonus;

        CriticalDamage = baseCriticalDamage + criticalDamageBonus;

        // Multiplicador de velocidade, não redução de cooldown — quem consumir isso
        // deve fazer tempoFinal = tempoBase / (1 + Haste). Mesmo formato de retorno
        // decrescente do Armor: Haste = Agility / (Agility + K).
        Haste = (agiTotal <= 0 ? 0f : agiTotal / (agiTotal + hasteConstant)) + hasteBonus;

        AttackPower = attackPowerBonus + primaryAttribute switch
        {
            PrimaryAttribute.Strength => baseAttackPower + strTotal * attackPowerPerStrength,
            PrimaryAttribute.Agility => baseAttackPower + agiTotal * attackPowerPerAgility,
            _ => 0f
        };

        SpellPower = spellPowerBonus + (primaryAttribute == PrimaryAttribute.Intelligence
            ? baseSpellPower + intTotal * spellPowerPerIntelligence
            : 0f);

        // Armor não deriva de atributo nenhum — só equipamento/buff/passiva.
        Armor = baseArmor + armorBonus;

        MoveSpeed = baseMoveSpeed + moveSpeedBonus;

        currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
        currentMana = Mathf.Clamp(currentMana, 0, MaxMana);
    }
}
