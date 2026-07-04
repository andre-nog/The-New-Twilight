using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Stomp")]
public class StompSkill : Skill
{
    [Header("Stomp")]
    public float radius = 2.5f;

    [Header("Momentum")]
    public float momentumScaling = 0.1f;

    public override void ExecuteEffect(Player_Combat combat, in CastContext ctx)
    {
        int momentum = combat.ResourceManager.ConsumeAllResource();

        float multiplier = 1f + (momentum * momentumScaling);

        // Deriva um contexto novo em vez de mutar estado do combat — sem
        // mutate-and-restore, um efeito assíncrono futuro não herda o bônus errado.
        combat.DealAreaDamage(radius, ctx.WithExtraMultiplier(multiplier));
    }
}