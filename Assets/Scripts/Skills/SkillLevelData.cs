using System;
using UnityEngine;

// Valores de balanceamento de UM nível de uma skill. A Skill guarda uma lista
// destes (levels[0] = nível 1, levels[1] = nível 2, ...), então balancear é editar
// o asset, sem tocar em código. requiredPlayerLevel é o que trava a progressão:
// codifica tanto a disponibilidade (o requiredPlayerLevel do nível 1 é o nível em
// que a skill pode ser aprendida) quanto o gate de upgrade — inclusive o caso do
// Cleave Strike, que só sobe em níveis específicos (5/7/9) em vez de contíguos.
//
// cooldown/range/resourceCost NÃO entram aqui de propósito: hoje não variam por
// nível, então ficam como campos avulsos fixos em Skill (Header "Gameplay"/
// "Resource"). Se algum dia precisarem escalar por nível, é só devolvê-los aqui.
[Serializable]
public class SkillLevelData
{
    [Tooltip("Nível mínimo do jogador para alcançar ESTE nível da skill. Nível 1 = quando ela pode ser aprendida; níveis seguintes = gate do upgrade (ex.: Cleave Strike 5/7/9).")]
    public int requiredPlayerLevel = 1;

    public float damageMultiplier = 1f;
    public int manaCost;
    public int resourceGenerated;
}
