// Boilerplate compartilhado por qualquer coisa que aplica dano de inimigo
// (Enemy_Combat, EnemyProjectile, TelegraphedAreaAbility): checa alvo
// nulo/morto, resolve pelo DamageCalculator (única fórmula, crítico/variância/
// Armor centralizados lá) e aplica. Não é uma abstração de "ability" — é só a
// mesma checagem+chamada que se repetiria em cada call site.
public static class EnemyDamage
{
    public static bool TryDealDamage(IDamageable target, float damage, float criticalChance, float criticalDamage)
    {
        if (target == null || !target.IsAlive)
            return false;

        DamageResult result = DamageCalculator.Calculate(
            damage,
            1f,
            1f,
            criticalChance,
            criticalDamage,
            target.Armor);

        target.TakeDamage(result);
        return true;
    }
}
