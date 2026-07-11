using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Power Strike")]
public class PowerStrikeSkill : SingleTargetDamageSkill
{
    [Header("Stun")]
    public float stunDuration = 2f;

    public override void ExecuteEffect(Player_Combat combat, in CastContext ctx)
    {
        base.ExecuteEffect(combat, ctx);

        if (ctx.Target == null)
            return;

        IDamageable damageable = ctx.Target.GetComponent<IDamageable>();

        if (damageable == null || !damageable.IsAlive)
            return; // golpe matou o alvo — nada a atordoar

        IStunnable stunnable = ctx.Target.GetComponent<IStunnable>();
        stunnable?.ApplyStun(stunDuration);
    }
}
