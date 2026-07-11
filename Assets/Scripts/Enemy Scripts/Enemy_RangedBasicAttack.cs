using System.Collections;
using UnityEngine;

// Ataque básico à distância — alternativa a Enemy_Combat pra um inimigo cujo
// ataque normal (o disparado por Enemy_Movement quando o alvo entra em
// attackRange e o cooldown termina) é atirar um projétil em vez de golpear
// corpo a corpo. Um prefab tem UM dos dois, nunca os dois — a presença do
// componente é o que decide o comportamento (o "tipo" do ataque básico), sem
// flag nenhuma pra configurar.
//
// Timing (attackWindup/attackRecovery) vem do mesmo archetype que Enemy_Combat
// usa — é o MESMO ataque básico, só que à distância, não uma habilidade à parte
// (essa é Enemy_Abilities, com cooldown e comportamento próprios).
[RequireComponent(typeof(EnemyStats))]
public class Enemy_RangedBasicAttack : MonoBehaviour, IEnemyBasicAttack
{
    [Tooltip("Prefab do projétil — precisa de um componente EnemyProjectile.")]
    public GameObject projectilePrefab;

    [Tooltip("Ponto de onde o projétil nasce (ex.: a ponta da besta). Se vazio, nasce na posição do próprio inimigo.")]
    public Transform muzzle;

    public float projectileSpeed = 12f;

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

        // Só auto-resolve se ninguém arrastou nada à mão no Inspector — a
        // referência manual sempre vence.
        if (muzzle == null)
        {
            ProjectileSpawnPoint spawnPoint = GetComponentInChildren<ProjectileSpawnPoint>();

            if (spawnPoint != null)
                muzzle = spawnPoint.transform;
        }
    }

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

    // Mesma estrutura windup -> disparo -> recovery -> EndAttack de Enemy_Combat —
    // só troca ResolveDamage direto por instanciar um projétil que resolve o dano
    // sozinho quando chega ao alvo.
    private IEnumerator AttackSequence()
    {
        anim.SetTrigger("Attack");

        EnemyArchetypeSO archetype = stats.Archetype;
        float windup = archetype != null ? archetype.attackWindup : 0f;
        float recovery = archetype != null ? archetype.attackRecovery : 0f;

        if (windup > 0f)
            yield return new WaitForSeconds(windup);

        FireProjectile();

        if (recovery > 0f)
            yield return new WaitForSeconds(recovery);

        attackRoutine = null;
        enemyMovement.EndAttack();
    }

    private void FireProjectile()
    {
        if (enemyMovement == null)
            return;

        Transform player = enemyMovement.GetPlayer();

        if (player == null)
            return;

        if (projectilePrefab == null)
        {
            Debug.LogWarning($"Enemy_RangedBasicAttack em '{name}': projectilePrefab não atribuído — nenhum tiro sai.", this);
            return;
        }

        Vector3 origin = muzzle != null ? muzzle.position : transform.position;
        GameObject instance = Instantiate(projectilePrefab, origin, Quaternion.identity);
        EnemyProjectile projectile = instance.GetComponent<EnemyProjectile>();

        if (projectile == null)
        {
            Debug.LogWarning($"Enemy_RangedBasicAttack em '{name}': projectilePrefab sem um componente EnemyProjectile.", this);
            Destroy(instance);
            return;
        }

        // AttackPower vem direto do archetype (EnemyStats) — é o dano deste tiro,
        // sem multiplicador nenhum por cima.
        projectile.Launch(player, projectileSpeed, stats.AttackPower, stats.CriticalChance, stats.CriticalDamage);

        if (audioSource != null && stats.Archetype != null && stats.Archetype.attackSfx != null)
            audioSource.PlayOneShot(stats.Archetype.attackSfx);
    }
}
