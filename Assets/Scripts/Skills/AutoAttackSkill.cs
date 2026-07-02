using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Auto Attack")]
public class AutoAttackSkill : Skill
{
    public override void ExecuteEffect(Player_Combat combat)
    {
        combat.DealDamageToTarget();
    }
}