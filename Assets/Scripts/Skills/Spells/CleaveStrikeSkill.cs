using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Cleave Strike")]
public class CleaveStrikeSkill : Skill
{
    [Header("Secondary Target")]
    [Tooltip("Raio ao redor do alvo primário pra procurar um segundo alvo próximo.")]
    public float secondaryTargetRadius = 2.5f;

    [Range(0f, 2f)]
    [Tooltip("Fração do dano do alvo primário aplicada ao alvo secundário (0.8 = 80%).")]
    public float secondaryDamagePercent = 0.8f;

    public override void ExecuteEffect(Player_Combat combat, in CastContext ctx)
    {
        combat.DealDamageToTarget(ctx);

        combat.DealDamageToNearbySecondaryTarget(
            ctx.Target, secondaryTargetRadius, secondaryDamagePercent, ctx);
    }
}
