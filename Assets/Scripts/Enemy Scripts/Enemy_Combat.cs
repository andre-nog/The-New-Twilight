using UnityEngine;

[RequireComponent(typeof(EnemyStats))]
public class Enemy_Combat : MonoBehaviour
{
    [Tooltip("Multiplicador do golpe corpo a corpo sobre o Attack Power do arquétipo.")]
    public float skillMultiplier = 1f;

    private Enemy_Movement enemyMovement;
    private EnemyStats stats;

    private void Start()
    {
        enemyMovement = GetComponent<Enemy_Movement>();
        stats = GetComponent<EnemyStats>();
    }

    public void Attack()
    {
        if (enemyMovement == null)
            return;

        Transform player = enemyMovement.GetPlayer();

        if (player == null)
            return;

        IDamageable target = player.GetComponent<IDamageable>();

        if (target == null || !target.IsAlive)
            return;

        // Mesmo pipeline do jogador: DamageCalculator resolve crítico, variância e
        // mitigação por Armor — a armadura do jogador finalmente conta contra inimigos.
        DamageResult result = DamageCalculator.Calculate(
            stats.AttackPower,
            skillMultiplier,
            1f,
            stats.CriticalChance,
            stats.CriticalDamage,
            target.Armor);

        target.TakeDamage(result);
    }
}
