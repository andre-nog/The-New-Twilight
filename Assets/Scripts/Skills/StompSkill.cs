using UnityEngine;

[CreateAssetMenu(menuName = "Skills/Stomp")]
public class StompSkill : Skill
{
    [Header("Stomp")]
    public float radius = 2.5f;

    public override void ExecuteEffect(Player_Combat combat)
    {
        combat.DealAreaDamage(radius);
    }
}