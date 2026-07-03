using UnityEngine;

public abstract class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillName;
    public Sprite icon;

    [Header("Gameplay")]
    public bool requiresTarget = true;
    public float cooldown;
    public float range;
    public int manaCost;
    public float damageMultiplier = 1f;
    
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