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

    [Header("Resource")]
    [FormerlySerializedAs("momentumGenerated")]
    public int resourceGenerated;
    [FormerlySerializedAs("momentumCost")]
    public int resourceCost;
    
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