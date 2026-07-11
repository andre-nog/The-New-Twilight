using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
public abstract class Skill : ScriptableObject, IStableAssetId
#else
public abstract class Skill : ScriptableObject
#endif
{
    [Header("Info")]
    public string skillName;
    public SkillType skillType;
    public Sprite icon;

    [TextArea]
    public string description;

    [Header("Leveling")]
    [Tooltip("Se marcado, a skill já nasce no nível 1 (sem gastar ponto) — hoje só o Auto Attack. As demais começam não-aprendidas (nível 0).")]
    public bool autoLearnedAtStart;

    [Tooltip("Um item por nível da skill (levels[0] = nível 1). Cada nível define seus próprios valores de balanceamento e o nível de jogador exigido. Se vazio, os campos avulsos abaixo (Gameplay/Resource/Mana) valem como um único nível 1 — fallback para assets ainda não migrados.")]
    public List<SkillLevelData> levels = new();

    // Maior nível alcançável. 0 quando ainda não há tabela nem fallback possível —
    // na prática o fallback garante pelo menos 1.
    public int MaxLevel => levels != null && levels.Count > 0 ? levels.Count : 1;

    // Dados efetivos de um nível. Clampa para [1, MaxLevel]. Se a lista estiver vazia,
    // sintetiza o nível 1 a partir dos campos avulsos legados, então assets antigos
    // (sem a tabela preenchida) continuam funcionando como uma skill de nível único.
    public SkillLevelData GetLevelData(int skillLevel)
    {
        if (levels == null || levels.Count == 0)
            return LegacyLevelData();

        int index = Mathf.Clamp(skillLevel, 1, levels.Count) - 1;
        return levels[index];
    }

    private SkillLevelData LegacyLevelData()
    {
        return new SkillLevelData
        {
            requiredPlayerLevel = 1,
            damageMultiplier = damageMultiplier,
            manaCost = manaCost,
            resourceGenerated = resourceGenerated,
        };
    }

    // Id estável (GUID do asset) — usado por SkillProgression/SkillLoadout pra
    // referenciar a skill no save (JsonUtility não serializa a referência direta).
    [SerializeField, HideInInspector] private string id;
    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }

    // Chamado também pelo StableAssetIdPostprocessor no import de qualquer asset —
    // cobre o caso de uma skill nunca aberta no Inspector (mesmo padrão de
    // ItemSO.EnsureId()).
    public void EnsureId()
    {
        if (!string.IsNullOrEmpty(id))
            return;

        string path = AssetDatabase.GetAssetPath(this);

        if (string.IsNullOrEmpty(path))
            return;

        id = AssetDatabase.AssetPathToGUID(path);
        EditorUtility.SetDirty(this);
    }
#endif

    [Header("Slot")]
    [Tooltip("Preferred hotbar slot (0-based) to auto-place this skill into the first time it's learned, if that slot is empty. -1 = no preference. The player can always drag it to a different slot afterward.")]
    public int preferredDefaultSlot = -1;

    [Header("Gameplay")]
    public bool requiresTarget = true;
    public bool lockMovementDuringCast = false;

    [Tooltip("If checked, ExecuteEffect runs immediately when the cast commits instead of waiting for the ExecuteSkillEffect Animation Event mid-clip. For skills with no swing animation to sync to, e.g. a channel that outlives any attack clip.")]
    public bool executeEffectImmediately = false;

    [Tooltip("Se em cooldown quando usada, o jogador se aproxima e segura posição perto do alvo até o cooldown acabar, castando automaticamente assim que possível — em vez de ignorar o input. Desmarque para skills que não devem perseguir enquanto recarregam.")]
    public bool followTargetWhileOnCooldown = true;

    [Tooltip("If checked, this skill can only be cast while the player is out of combat (see CombatStateTracker).")]
    public bool requiresOutOfCombat = false;
    public float cooldown;
    public float range;
    public float damageMultiplier = 1f;
    public DamageSchool damageSchool = DamageSchool.Physical;

    [Tooltip("Se marcado, o cooldown dessa skill é acelerado por Haste: tempoFinal = tempoBase / (1 + Haste). Hoje só faz sentido pro Auto Attack.")]
    public bool affectedByHaste = false;

    [Header("Resource")]
    [FormerlySerializedAs("momentumGenerated")]
    public int resourceGenerated;
    [FormerlySerializedAs("momentumCost")]
    public int resourceCost;

    [Header("Mana")]
    [Tooltip("Custo em Mana (StatsManager), separado do Resource genérico acima (Momentum). 0 = não gasta Mana.")]
    public int manaCost;

    [Header("Visual Effects")]
    public GameObject hitVFX;
    public Vector3 hitVFXOffset;

    [Header("Animation")]
    public string animationTrigger;

    [Tooltip("Failsafe: force-clears isAttacking after this many seconds if the animation event never fires (missing/misnamed event, broken animator state).")]
    public float maxCastDuration = 3f;

    // Dano esperado "pré-mitigação" (sem crit, variância de dano ou Armor do alvo) —
    // mesma fórmula usada de fato em Player_Combat.DealDamage, só que sem os termos
    // que dependem de RNG ou de um alvo selecionado (útil pra tooltip, que não tem
    // alvo nem quer mostrar um valor que varia a cada frame). Virtual porque StompSkill
    // precisa somar seu próprio termo de Momentum.
    public virtual float GetExpectedDamage(Player_Combat combat)
    {
        float offensivePower = damageSchool == DamageSchool.Magical
            ? StatsManager.Instance.SpellPower
            : StatsManager.Instance.AttackPower;

        return offensivePower
            * SkillProgression.DataFor(this).damageMultiplier
            * combat.GetPassiveDamageMultiplier(this);
    }

    public virtual void Cast(Player_Combat combat)
    {
        combat.UseSkill(this);
    }

    // ctx carrega skill/alvo/multiplicador capturados no início do cast — efeitos
    // derivam variações via ctx.WithExtraMultiplier em vez de mutar estado do combat.
    public abstract void ExecuteEffect(Player_Combat combat, in CastContext ctx);
}