using UnityEngine;

// Único lugar do jogo que resolve dano final. Quem ataca só entrega os
// insumos (potencial ofensivo, multiplicador da skill, crítico) e quem
// defende só entrega a própria Armor — ninguém mais refaz essa conta.
public static class DamageCalculator
{
    public static DamageResult Calculate(
        float offensivePower,
        float skillMultiplier,
        float extraMultiplier,
        float criticalChance,
        float criticalDamage,
        float targetArmor)
    {
        float raw = offensivePower * skillMultiplier * extraMultiplier;

        bool isCritical = Random.Range(0f, 100f) < criticalChance;

        if (isCritical)
            raw *= 1f + criticalDamage / 100f;

        CombatConfigSO config = CombatManager.CombatConfig;

        float variancePercent = config != null ? config.damageVariancePercent : 0f;

        float variance = Random.Range(
            1f - variancePercent / 100f,
            1f + variancePercent / 100f);

        raw *= variance;

        // mitigação% = Armor / (Armor + K) — retorno decrescente suave em vez de
        // subtração linear, pra Armor continuar relevante em qualquer escala de dano.
        // Negativo (de um futuro debuff de Vulnerability) amplifica o dano em vez de
        // reduzir; o teto de 90% evita mitigação praticamente infinita.
        float armorConstant = config != null ? config.armorConstant : 100f;

        float mitigation = targetArmor <= 0f
            ? 0f
            : targetArmor / (targetArmor + armorConstant);

        mitigation = Mathf.Clamp(mitigation, -1f, 0.9f);

        raw *= 1f - mitigation;

        return new DamageResult(Mathf.RoundToInt(raw), isCritical);
    }
}
