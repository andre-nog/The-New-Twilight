using UnityEngine;
using UnityEngine.AI;

public class Enemy_Movement : MonoBehaviour
{
    public float speed;
    public float attackRange = 2;
    public float attackCooldown = 2;
    public float playerDetectRange = 5;
    public LayerMask playerLayer;
    public float flipCooldown = 0.3f; 
    public float aggroRange = 10f; // ainda sem uso - não toquei nele agora
    public float loseAggroRange = 15f;

    private float attackCooldownTimer;
    private float repathTimer;
    private float flipCooldownTimer; // NOVO
    private Rigidbody2D rb;
    private Transform player;
    private int facingDirection = 1;
    private EnemyState enemyState;
    private Animator anim;
    private NavMeshAgent agent;
    private Vector3 spawnPosition;
    private Enemy_Health enemyHealth;
    [SerializeField] private Transform healthBarTransform;

    private void OnEnable()
    {
        PlayerHealth.OnPlayerDied += ForceDeaggro;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDied -= ForceDeaggro;
    }

    // O player morreu — mesmo efeito de perder aggro por distância (solta o alvo e
    // volta pro spawn), só que disparado na hora em vez de esperar o inimigo se afastar.
    private void ForceDeaggro()
    {
        if (enemyState == EnemyState.Returning)
            return;

        player = null;
        ChangeState(EnemyState.Returning);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<Enemy_Health>();

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.updatePosition = true;
        agent.speed = speed;

        agent.autoBraking = false;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        spawnPosition = transform.position;

        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

        if (flipCooldownTimer > 0)
            flipCooldownTimer -= Time.deltaTime;

        CheckForPlayer();

        switch (enemyState)
        {
            case EnemyState.Chasing:
                if (player != null)
                    Chase();
                break;

            case EnemyState.Returning:
                ReturnToSpawn();
                break;

            case EnemyState.Attacking:
                StopMoving();
                break;
        }
    }

    void Chase()
    {
        if (player == null)
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        float distanceFromSpawn =
            Vector2.Distance(transform.position, spawnPosition);

        if (distanceFromSpawn >= loseAggroRange)
        {
            player = null;
            ChangeState(EnemyState.Returning);
            return;
        }

        float distance =
            Vector2.Distance(transform.position, player.position);

        if (distance <= attackRange)
        {
            StopMoving();
            return;
        }

        if (agent.isOnNavMesh && agent.isStopped)
            agent.isStopped = false;

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0)
        {
            agent.SetDestination(player.position);
            repathTimer = 0.2f;
        }

        if (flipCooldownTimer <= 0)
        {
            float xDiff = player.position.x - transform.position.x;

            if ((xDiff > 0.1f && facingDirection == -1) ||
                (xDiff < -0.1f && facingDirection == 1))
            {
                Flip();
                flipCooldownTimer = flipCooldown;
            }
        }
    }

    // Para o movimento sem brigar com o agent.
    void StopMoving()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }
    }

    void Flip()
    {
        facingDirection *= -1;
        transform.localScale = new Vector3(
            transform.localScale.x * -1,
            transform.localScale.y,
            transform.localScale.z
        );

        // NOVO: cancela o flip herdado na health bar, já que ela é filha do inimigo
        if (healthBarTransform != null)
        {
            healthBarTransform.localScale = new Vector3(
                healthBarTransform.localScale.x * -1,
                healthBarTransform.localScale.y,
                healthBarTransform.localScale.z
            );
        }
    }

    private void CheckForPlayer()
    {
        if (enemyState == EnemyState.Returning)
            return;

        if (enemyState == EnemyState.Attacking)
            return;

        // Já possui um alvo.
        if (player != null)
        {
            float distance = Vector2.Distance(
                transform.position,
                player.position);

            float distanceFromSpawn = Vector2.Distance(
                transform.position,
                spawnPosition);

            // Perdeu o aggro.
            if (distanceFromSpawn > loseAggroRange)
            {
                player = null;
                ChangeState(EnemyState.Returning);
                return;
            }

            // Está dentro do alcance de ataque.
            if (distance <= attackRange)
            {
                StopMoving();

                if (attackCooldownTimer <= 0)
                {
                    attackCooldownTimer = attackCooldown;

                    ChangeState(EnemyState.Attacking);
                    anim.SetTrigger("Attack");
                }
                else
                {
                    if (enemyState != EnemyState.Idle)
                        ChangeState(EnemyState.Idle);
                }
            }
            // Saiu do alcance de ataque.
            else
            {
                if (enemyState != EnemyState.Chasing)
                    ChangeState(EnemyState.Chasing);
            }

            return;
        }

        // Ainda não possui alvo.
        Collider2D hit = Physics2D.OverlapCircle(
            transform.position,
            playerDetectRange,
            playerLayer);

        if (hit != null)
        {
            player = hit.transform;
            ChangeState(EnemyState.Chasing);
        }
    }

    public Transform GetPlayer()
    {
        return player;
    }

    public bool IsAttacking => enemyState == EnemyState.Attacking;

    // Usado por outras habilidades (ex.: Enemy_RangedAttack) para forçar a pose parada
    // enquanto assumem o controle do inimigo temporariamente.
    public void SetIdlePose()
    {
        anim.SetBool("isChasing", false);
        anim.SetBool("isAttacking", false);
        anim.SetBool("isIdle", true);
    }

    // Reaplica os bools do Animator para o estado atual, para desfazer SetIdlePose().
    // Não reusa ChangeState porque ela só alterna o bool do estado antigo/novo — como o
    // enemyState nunca mudou enquanto pausado, ela não zeraria o isIdle forçado.
    public void RefreshAnimatorState()
    {
        anim.SetBool("isIdle", enemyState == EnemyState.Idle);
        anim.SetBool("isChasing", enemyState == EnemyState.Chasing || enemyState == EnemyState.Returning);
        anim.SetBool("isAttacking", enemyState == EnemyState.Attacking);
    }

    void ChangeState(EnemyState newState)
    {
        if (enemyState == EnemyState.Idle)
    anim.SetBool("isIdle", false);
    else if (enemyState == EnemyState.Chasing)
        anim.SetBool("isChasing", false);
    else if (enemyState == EnemyState.Attacking)
        anim.SetBool("isAttacking", false);
    else if (enemyState == EnemyState.Returning)
        anim.SetBool("isChasing", false);

        enemyState = newState;

        if (enemyState == EnemyState.Idle)
    anim.SetBool("isIdle", true);
    else if (enemyState == EnemyState.Chasing)
        anim.SetBool("isChasing", true);
    else if (enemyState == EnemyState.Attacking)
        anim.SetBool("isAttacking", true);
    else if (enemyState == EnemyState.Returning)
        anim.SetBool("isChasing", true);
    }

    void ReturnToSpawn()
    {
        if (!agent.isOnNavMesh)
            return;

        if (agent.isStopped)
            agent.isStopped = false;

        agent.SetDestination(spawnPosition);

        if (flipCooldownTimer <= 0)
        {
            float xDiff = spawnPosition.x - transform.position.x;

            if ((xDiff > 0.1f && facingDirection == -1) ||
                (xDiff < -0.1f && facingDirection == 1))
            {
                Flip();
                flipCooldownTimer = flipCooldown;
            }
        }

        float distance = Vector2.Distance(
            transform.position,
            spawnPosition);

        if (distance <= 0.2f)
        {
            StopMoving();

            player = null;
            attackCooldownTimer = 0;

            if (enemyHealth != null)
                enemyHealth.ResetEnemy();

            ChangeState(EnemyState.Idle);
        }
    }

    public void EndAttack()
    {
        if (player == null)
        {
            ChangeState(EnemyState.Returning);
            return;
        }

        float distanceFromSpawn = Vector2.Distance(
            transform.position,
            spawnPosition);

        if (distanceFromSpawn > loseAggroRange)
        {
            player = null;
            ChangeState(EnemyState.Returning);
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            player.position);

        if (distance > attackRange)
            ChangeState(EnemyState.Chasing);
        else
            ChangeState(EnemyState.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        // Detect Range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(
            transform.position,
            playerDetectRange);

        // Lose Aggro Range
        Gizmos.color = Color.yellow;

        if (Application.isPlaying)
            Gizmos.DrawWireSphere(
                spawnPosition,
                loseAggroRange);
        else
            Gizmos.DrawWireSphere(
                transform.position,
                loseAggroRange);
    }
}

public enum EnemyState
{
    Idle,
    Chasing,
    Attacking,
    Returning
}