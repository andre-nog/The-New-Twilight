using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Stomp")]
public class StompSkill : Skill
{
    [Header("Stomp")]
    public float radius = 2.5f;

    [Header("Momentum")]
    public float momentumScaling = 0.1f;

    public override void ExecuteEffect(Player_Combat combat)
    {
        int momentum = combat.ResourceManager.ConsumeAllMomentum();

        float multiplier = 1f + (momentum * momentumScaling);

        combat.SetDamageMultiplierBonus(multiplier);

        combat.DealAreaDamage(radius);

        combat.ResetDamageMultiplierBonus();
    }
}