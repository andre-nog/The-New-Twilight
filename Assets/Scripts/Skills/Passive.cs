using UnityEngine;

public abstract class Passive : ScriptableObject
{
    public virtual float ModifyDamageMultiplier(Player_Combat combat, Skill skill)
    {
        return 1f;
    }
}