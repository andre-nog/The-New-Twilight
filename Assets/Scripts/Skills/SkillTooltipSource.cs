using System;
using System.Collections.Generic;
using UnityEngine;

public class SkillTooltipSource
{
    private readonly Skill skill;
    private readonly float remainingCooldown;

    public SkillTooltipSource(Skill skill, float remainingCooldown = 0f)
    {
        this.skill = skill;
        this.remainingCooldown = remainingCooldown;
    }

    public SkillTooltipData GetSkillTooltipData()
    {
        SkillLevelData levelData = SkillProgression.DataFor(skill);

        string resourceName = StatsManager.Instance != null && StatsManager.Instance.CurrentClass != null
            ? StatsManager.Instance.CurrentClass.resourceName
            : "Resource";

        List<string> segments = new();

        if (skill.resourceCost != 0)
            segments.Add($"{skill.resourceCost} {resourceName}");

        if (levelData.resourceGenerated != 0)
            segments.Add($"+{levelData.resourceGenerated} {resourceName}");

        if (levelData.manaCost != 0)
            segments.Add($"{levelData.manaCost} Mana");

        if (skill.cooldown != 0f)
        {
            string cooldownSegment = $"{skill.cooldown:0.0}s Cooldown";

            if (remainingCooldown > 0f)
                cooldownSegment += $" ({remainingCooldown:0.0}s remaining)";

            segments.Add(cooldownSegment);
        }

        if (skill.requiresTarget && skill.range != 0f)
            segments.Add($"{skill.range} Range");

        return new SkillTooltipData
        {
            title = skill.skillName,
            level = SkillProgression.LevelOf(skill),
            metaLine = string.Join("   ", segments),
            description = BuildDescription(),
        };
    }

    // Preenche o {0} da description com o dano esperado atual (AttackPower/SpellPower
    // + multiplicador do nível + passivas), pra bater com o dano real em combate.
    // Sem Player_Combat vivo (ex.: hover no Livro de Skills antes do player spawnar)
    // ou com um {0} malformado/ausente no asset, cai pro texto cru sem formatar —
    // nunca deve lançar por causa de um typo de designer no Inspector.
    private string BuildDescription()
    {
        Player_Combat combat = GameManager.Instance != null && GameManager.Instance.Player != null
            ? GameManager.Instance.Player.GetComponent<Player_Combat>()
            : null;

        if (combat == null)
            return skill.description;

        int expectedDamage = Mathf.RoundToInt(skill.GetExpectedDamage(combat));

        try
        {
            return string.Format(skill.description, expectedDamage);
        }
        catch (FormatException)
        {
            return skill.description;
        }
    }
}
