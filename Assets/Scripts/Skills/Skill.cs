using UnityEngine;
using UnityEngine.Serialization;

public abstract class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillName;
    public SkillType skillType;
    public Sprite icon;

    [Header("Gameplay")]
    public bool requiresTarget = true;
    public bool lockMovementDuringCast = false;
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

    public virtual void Cast(Player_Combat combat)
    {
        combat.UseSkill(this);
    }

    public abstract void ExecuteEffect(Player_Combat combat);
}