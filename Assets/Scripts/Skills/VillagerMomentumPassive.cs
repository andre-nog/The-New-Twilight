using UnityEngine;

[CreateAssetMenu(menuName = "Passives/Villager Momentum")]
public class VillagerMomentumPassive : Passive
{
    [SerializeField]
    private float fullMomentumBonus = 1.15f;

    public override float ModifyDamageMultiplier(Player_Combat combat, Skill skill)
    {
        if (skill.skillType != SkillType.AutoAttack)
            return 1f;

        if (combat.ResourceManager.CurrentMomentum < combat.ResourceManager.MaxMomentum)
            return 1f;

        return fullMomentumBonus;
    }
}