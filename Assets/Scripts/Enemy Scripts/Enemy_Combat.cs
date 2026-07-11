using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyStats))]
public class Enemy_Combat : MonoBehaviour, IEnemyBasicAttack
{
    private Enemy_Movement enemyMovement;
    private EnemyStats stats;
    private Animator anim;
    private AudioSource audioSource;
    private Coroutine attackRoutine;

    private void Start()
    {
        enemyMovement = GetComponent<Enemy_Movement>();
        stats = GetComponent<EnemyStats>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
    }

    // Chamado por Enemy_Movement.CheckForPlayer() no lugar do antigo
    // anim.SetTrigger("Attack") direto. Dono da sequência windup -> dano ->
    // recovery -> EndAttack — o timing vem do archetype (attackWindup/
    // attackRecovery), não mais de um Animation Event cravado no clipe. Um clipe
    // de ataque novo não precisa de nenhum evento; só a duração aproximada bate.
    public void BeginAttack()
    {
        if (attackRoutine != null)
            StopCoroutine(attackRoutine);

        attackRoutine = StartCoroutine(AttackSequence());
    }

    public void CancelAttack()
    {
        if (attackRoutine == null)
            return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
    }

    private IEnumerator AttackSequence()
    {
        anim.SetTrigger("Attack");

        EnemyArchetypeSO archetype = stats.Archetype;
        float windup = archetype != null ? archetype.attackWindup : 0f;
        float recovery = archetype != null ? archetype.attackRecovery : 0f;

        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        ResolveDamage();

        if (recovery > 0f)
            yield return new WaitForSeconds(recovery);

        attackRoutine = null;
        enemyMovement.EndAttack();
    }

    private void ResolveDamage()
    {
        if (enemyMovement == null)
            return;

        Transform player = enemyMovement.GetPlayer();

        if (player == null)
            return;

        IDamageable target = player.GetComponent<IDamageable>();

        // Mesmo pipeline do jogador: DamageCalculator resolve crítico, variância e
        // mitigação por Armor. AttackPower vem direto do archetype (EnemyStats) —
        // é o dano deste golpe, sem multiplicador nenhum por cima.
        bool hit = EnemyDamage.TryDealDamage(target, stats.AttackPower, stats.CriticalChance, stats.CriticalDamage);

        if (hit && audioSource != null && stats.Archetype != null && stats.Archetype.attackSfx != null)
            audioSource.PlayOneShot(stats.Archetype.attackSfx);

        if (hit && stats.Archetype != null && stats.Archetype.appliesBurn)
        {
            IBurnable burnable = player.GetComponent<IBurnable>();
            burnable?.ApplyBurn(stats.Archetype.burnTickDamage, stats.Archetype.burnTickInterval, stats.Archetype.burnDuration);
        }
    }
}
