using UnityEngine;

// Skill genérica de dano em alvo único — todo o comportamento vem dos dados do
// asset (multiplicador, escola de dano, custos, cooldown). Skills novas desse
// tipo usam esta classe direto, sem criar código; AutoAttackSkill e
// PowerStrikeSkill viraram cascas herdadas só para preservar os .asset existentes.
[CreateAssetMenu(menuName = "Skills/Single Target Damage")]
public class SingleTargetDamageSkill : Skill
{
    public override void ExecuteEffect(Player_Combat combat, in CastContext ctx)
    {
        combat.DealDamageToTarget(ctx);
    }
}
