using UnityEngine;

[CreateAssetMenu(menuName = "Passives/Villager Momentum")]
public class VillagerMomentumPassive : Passive
{
    [SerializeField]
    private float fullMomentumBonus = 1.15f;

    public override float ModifyDamageMultiplier(Player_Combat combat, Skill skill)
    {
        if (skill.skillType != SkillType.BasicAttack)
            return 1f;

        if (combat.ResourceManager.CurrentResource < combat.ResourceManager.MaxResource)
            return 1f;

        return fullMomentumBonus;
    }
}