using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Power Strike")]
public class PowerStrikeSkill : Skill
{
    public override void ExecuteEffect(Player_Combat combat)
    {
        combat.DealDamageToTarget();
    }
}