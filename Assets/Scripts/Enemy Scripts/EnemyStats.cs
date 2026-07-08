using System.Collections.Generic;
using UnityEngine;

// Espelha o padrão do jogador (StatsManager): valores finais só são derivados em
// RecalculateStats e o resto do inimigo lê as propriedades prontas. A base vem do
// archetype (asset), somada aos modifiers — mesma disciplina de fonte única, sem
// forçar os dois lados num modelo compartilhado (stats do jogador são derivados por
// fórmula de classe; os do inimigo, autorados).
//
// Variantes tipo "Elite" não são um sistema aqui — ajuste os valores direto no
// prefab do monstro (outro archetype, ou tuning manual dos componentes).
public class EnemyStats : MonoBehaviour
{
    [SerializeField] private EnemyArchetypeSO archetype;

    // Buffs/debuffs de inimigo entram por aqui (mesma API do StatsManager) —
    // duração/tick ficam para quando a primeira skill de DoT/buff real existir.
    private readonly List<StatModifier> modifiers = new();

    public EnemyArchetypeSO Archetype => archetype;

    public int MaxHealth { get; private set; }
    public float Armor { get; private set; }
    public float AttackPower { get; private set; }
    public float CriticalChance { get; private set; }
    public float CriticalDamage { get; private set; }
    public int ExpReward { get; private set; }
    public int GoldReward { get; private set; }
    public string DisplayName { get; private set; }

    private void Awake()
    {
        RecalculateStats();
        AutoConfigureFromArchetype();
    }

    // Aplica dados visuais/físicos do archetype direto nos componentes do próprio
    // GameObject — elimina o passo manual de arrastar sprite/controller/radius no
    // Inspector por prefab. Só toca o que existe; tudo aqui é opcional na prática.
    private void AutoConfigureFromArchetype()
    {
        if (archetype == null)
            return;

        SpriteRenderer spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && archetype.defaultSprite != null)
            spriteRenderer.sprite = archetype.defaultSprite;

        Animator animator = GetComponent<Animator>();
        if (animator != null && archetype.animatorOverride != null)
            animator.runtimeAnimatorController = archetype.animatorOverride;

        UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.radius = archetype.physicalRadius;
            agent.height = archetype.physicalHeight;
        }
    }

    public void AddModifier(StatModifier modifier)
    {
        modifiers.Add(modifier);
        RecalculateStats();
    }

    public void RemoveModifier(StatModifier modifier)
    {
        modifiers.Remove(modifier);
        RecalculateStats();
    }

    // Único lugar que deriva os valores finais do inimigo — base do archetype +
    // bônus dos modifiers (só o subconjunto de StatType que faz sentido para
    // inimigos hoje).
    //
    // Aviso de ordem: MaxHealth/Armor/AttackPower/etc. só ficam prontos DEPOIS
    // deste método rodar (chamado em Awake). Um componente irmão só pode ler essas
    // propriedades derivadas com segurança a partir do próprio Start() (Unity
    // garante que todo Awake da cena roda antes de qualquer Start). Campos crus do
    // archetype (stats.Archetype.xxx) não têm essa restrição — já valem antes de
    // qualquer Awake rodar, por serem parte do asset, não calculados aqui.
    private void RecalculateStats()
    {
        if (archetype == null)
        {
            Debug.LogWarning($"EnemyStats em '{name}' sem EnemyArchetypeSO atribuído.", this);
            return;
        }

        float maxHealth = archetype.maxHealth;
        float armor = archetype.armor;
        float attackPower = archetype.attackPower;
        float criticalChance = archetype.criticalChance;
        float criticalDamage = archetype.criticalDamage;

        foreach (StatModifier modifier in modifiers)
        {
            switch (modifier.stat)
            {
                case StatType.MaxHealth:
                    maxHealth += modifier.amount;
                    break;

                case StatType.Armor:
                    armor += modifier.amount;
                    break;

                case StatType.AttackPower:
                    attackPower += modifier.amount;
                    break;

                case StatType.CriticalChance:
                    criticalChance += modifier.amount;
                    break;

                case StatType.CriticalDamage:
                    criticalDamage += modifier.amount;
                    break;
            }
        }

        MaxHealth = Mathf.Max(1, Mathf.RoundToInt(maxHealth));
        Armor = armor;
        AttackPower = attackPower;
        CriticalChance = criticalChance;
        CriticalDamage = criticalDamage;
        ExpReward = archetype.expReward;
        GoldReward = archetype.goldReward;
        DisplayName = archetype.displayName;
    }
}
