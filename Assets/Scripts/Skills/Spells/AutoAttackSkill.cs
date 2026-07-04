using UnityEngine;

// Casca mantida só para preservar os .asset existentes (o GUID do script fica o
// mesmo) — o comportamento mora em SingleTargetDamageSkill.
[CreateAssetMenu(menuName = "Skills/Auto Attack")]
public class AutoAttackSkill : SingleTargetDamageSkill
{
}
