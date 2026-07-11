using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class Enemy_Movement : MonoBehaviour, IStunnable
{
    // Estes cinco (+ returnSpeed) agora vêm do EnemyArchetypeSO em Start() — os
    // valores aqui são só o fallback caso o inimigo não tenha um archetype
    // atribuído (nunca deveria acontecer em produção, mas evita um inimigo
    // congelado/mudo em vez de um erro silencioso).
    private float speed = 2f;
    private float returnSpeed = 2f;
    private float attackRange = 2f;
    private float attackCooldown = 2f;
    private float playerDetectRange = 5f;
    public LayerMask playerLayer;
    private float loseAggroRange = 15f;

    [Header("Facing (flip)")]
    // Facing is decided by SMOOTHED horizontal velocity, not by position relative to the target.
    // Position-based facing flipped every few frames when a crowd shoved an enemy sideways; a
    // deadband + commit-time on the smoothed velocity means only a sustained horizontal
    // commitment flips the sprite, so transient separation nudges never do. See UpdateFacing().
    [SerializeField] private float facingSmoothing = 10f;   // low-pass responsiveness (1/s)
    [SerializeField] private float flipEnterSpeed = 0.35f;  // min |vx| to commit to a side (u/s)
    [SerializeField] private float flipCommitTime = 0.12f;  // direction must persist this long to flip

    [Header("Pathing")]
    [SerializeField] private float repathMoveThreshold = 0.25f; // only repath if target moved more than this

    [Tooltip("Histerese na borda do alcance de ataque: uma vez parado, só volta a perseguir se o alvo se afastar além de attackRange + isto. Sem isso, um alvo parado bem na borda faz o inimigo oscilar Idle/Chasing a cada frame e 'deslizar' um passinho a cada oscilação, em vez de ficar parado de fato.")]
    [SerializeField] private float attackRangeHysteresis = 0.3f;

    [Header("Steering / separation")]
    // The NavMeshAgent is used for PATHFINDING ONLY (updatePosition = false): we read its path
    // (steeringTarget) and integrate the transform ourselves in Steer(), blending the path
    // direction with a local separation push from nearby enemies (boids-style). Neighbours come
    // from the shared EnemyFlockManager spatial hash, so it scales to hundreds of agents. With
    // the built-in hard avoidance (RVO) also off, enemies no longer form an impassable wall —
    // they softly spread and flow around each other to surround the target. Every frame we clamp
    // our own result back onto the navmesh with NavMesh.SamplePosition (see Steer()) so this stays
    // safe: it is the standard "agent for path, custom motor for movement" pattern, and avoids the
    // bug where calling agent.Move() while updatePosition=true fights the agent's own automatic
    // path-following for control of the transform (that caused enemies to warp to the map edge).
    [SerializeField] private float separationStrength = 1.5f; // strength of the neighbour push (u/s)
    [SerializeField] private float maxSeparation = 2f;        // clamp on the summed neighbour push
    [SerializeField] private float navSampleDistance = 0.75f; // max distance allowed when clamping to the navmesh

    [Header("Stun")]
    [Tooltip("Cor do ícone acima da cabeça enquanto atordoado. Placeholder (disco sólido) até existir um VFX de redemoinho.")]
    [SerializeField] private Color stunColor = new(0.2f, 0.4f, 1f, 1f);

    [Header("Stuck detection")]
    // Only request a fresh path when an enemy is GENUINELY jammed (wanted to move but barely
    // did), never every frame — keeps NavMesh recalculations rare. A short lateral bias breaks
    // symmetric deadlocks (a wall of enemies directly ahead).
    [SerializeField] private float stuckCheckInterval = 0.5f;  // sample displacement this often
    [SerializeField] private float stuckMoveFraction = 0.25f;  // stuck if moved < this * expected
    [SerializeField] private float unstuckDuration = 0.4f;     // how long the lateral bias lasts
    [SerializeField] private float unstuckStrength = 1f;       // magnitude of the lateral bias

    private float attackCooldownTimer;
    private float repathTimer;
    private float smoothedVelX;      // EMA of our horizontal velocity (drives facing)
    private float flipCommitTimer;   // how long desired direction has differed from current
    private Vector3 lastDestination; // last point sent to SetDestination (dedupe repaths)
    private Vector2 currentVelocity; // our own steering velocity this frame (drives facing)
    private EnemyFlockManager flock; // shared neighbour spatial hash for separation
    private float stuckTimer;        // time accumulated since last stuck sample
    private Vector2 lastStuckPos;    // position at the last stuck sample
    private float unstuckTimer;      // remaining time of the lateral unstick bias
    private float unstuckSign;       // +1/-1 side chosen for the unstick bias
    private Rigidbody2D rb;
    private Transform player;

    // Capturado junto com "player", na detecção — checagem de vida por frame,
    // redundante ao evento PlayerHealth.OnPlayerDied. O evento cobre o caso comum,
    // mas o leash (loseAggroRange) é medido a partir do SPAWN do inimigo, não da
    // posição do jogador, então qualquer desaggro perdido por timing (ex.: evento
    // disparado enquanto este componente está temporariamente desabilitado por
    // outro script de ataque) deixaria o inimigo perseguir um alvo morto por uma
    // distância grande. Essa checagem funciona mesmo se o evento falhar.
    private IDamageable playerHealth;
    private int facingDirection = 1;
    private EnemyState enemyState;
    private Animator anim;
    private NavMeshAgent agent;
    private Vector3 spawnPosition;
    private Enemy_Health enemyHealth;
    private IEnemyBasicAttack combat; // Enemy_Combat (corpo a corpo) ou Enemy_RangedBasicAttack (à distância) — o que existir no prefab
    private EnemyStats stats;
    private StunIndicator stunIndicator;
    private Coroutine stunRoutine;

    // Encontrado via GetComponentInChildren em Start() — nenhum prefab precisa mais
    // arrastar isso manualmente no Inspector, contanto que siga a hierarquia padrão
    // (Canvas > HealthBar com um Slider) do prefab-template.
    private Transform healthBarTransform;

    // Awake/OnDestroy (não OnEnable/OnDisable): scripts de ataque (ex.: Enemy_Abilities)
    // desabilitam este componente temporariamente durante telegraph/arremesso — se a
    // inscrição fosse por OnEnable/OnDisable, o desaggro seria perdido caso o player
    // morresse exatamente nessa janela, e o inimigo continuaria atacando um alvo morto.
    private void Awake()
    {
        PlayerHealth.OnPlayerDied += ForceDeaggro;

        // Register with the shared separation grid. Awake/OnDestroy (not OnEnable/OnDisable) so a
        // telegraphing enemy — which disables this component (see Enemy_Abilities) — still
        // counts as a neighbour that others steer around while it is frozen.
        EnemyFlockManager.EnsureExists();
        flock = EnemyFlockManager.Instance;
        flock.Register(this);
    }

    private void OnDestroy()
    {
        PlayerHealth.OnPlayerDied -= ForceDeaggro;

        if (flock != null)
            flock.Unregister(this);
    }

    // O player morreu — mesmo efeito de perder aggro por distância (solta o alvo e
    // volta pro spawn), só que disparado na hora em vez de esperar o inimigo se afastar.
    private void ForceDeaggro()
    {
        if (enemyState == EnemyState.Returning)
            return;

        player = null;
        playerHealth = null;
        ChangeState(EnemyState.Returning);
    }

    // Alvo perdido (nulo) ou morto — mesmo critério usado nas duas situações.
    private bool HasLiveTarget()
    {
        return player != null && (playerHealth == null || playerHealth.IsAlive);
    }

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        enemyHealth = GetComponent<Enemy_Health>();
        combat = GetComponent<IEnemyBasicAttack>();
        stats = GetComponent<EnemyStats>();

        EnemyArchetypeSO archetype = stats != null ? stats.Archetype : null;

        if (archetype != null)
        {
            speed = archetype.moveSpeed;
            returnSpeed = archetype.returnSpeed;
            attackRange = archetype.attackRange;
            attackCooldown = archetype.attackCooldown;
            playerDetectRange = archetype.detectionRange;
            loseAggroRange = archetype.chaseDistanceLimit;
        }

        // Mesma hierarquia em todo prefab de inimigo (template) — encontrado aqui
        // em vez de arrastado no Inspector por prefab.
        healthBarTransform = GetComponentInChildren<Slider>()?.transform;

        // healthBarOffset mora no pai do Slider (o Canvas world-space) — é essa
        // posição que varia de inimigo pra inimigo (ex.: Goblin baixo vs. Orc alto),
        // não a do próprio Slider.
        if (healthBarTransform != null && healthBarTransform.parent != null && archetype != null)
            healthBarTransform.parent.localPosition = archetype.healthBarOffset;

        agent.updateRotation = false;
        agent.updateUpAxis = false;

        // Agent supplies the path only; we drive the transform ourselves in Steer() (see the
        // "Steering / separation" header above for why).
        agent.updatePosition = false;
        agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        agent.speed = speed;
        agent.autoBraking = false;

        if (rb != null)
            rb.bodyType = RigidbodyType2D.Kinematic;

        spawnPosition = transform.position;
        lastDestination = Vector3.positiveInfinity; // force the first SetDestination
        lastStuckPos = transform.position;

        ChangeState(EnemyState.Idle);
    }

    void Update()
    {
        if (attackCooldownTimer > 0)
            attackCooldownTimer -= Time.deltaTime;

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

        UpdateFacing();
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

        repathTimer -= Time.deltaTime;

        if (repathTimer <= 0)
        {
            // Only repath when the target actually moved — a standing player triggers zero repaths.
            if ((player.position - lastDestination).sqrMagnitude >= repathMoveThreshold * repathMoveThreshold)
            {
                agent.SetDestination(player.position);
                lastDestination = player.position;
            }

            repathTimer = 0.2f;
        }

        Steer(player.position, speed);
    }

    // Stops our own movement. With updatePosition=false the agent's internal path-follow
    // simulation keeps running in the background regardless, so we pin its simulated position
    // (nextPosition) to where we actually are — this both prevents it drifting away from reality
    // while we stand still and is the documented way to keep a manually-driven agent's path
    // queries (steeringTarget etc.) valid for when we resume.
    void StopMoving()
    {
        currentVelocity = Vector2.zero;
        if (agent.isOnNavMesh)
            agent.nextPosition = transform.position;
    }

    // Integrates our own movement: navmesh path direction blended with a local separation push
    // from nearby enemies (+ a short lateral bias when jammed), then clamps the result back onto
    // the navmesh with NavMesh.SamplePosition before writing the transform. This is the "path +
    // boids separation" model ARPG crowds use — packed enemies flow around each other and
    // surround the target instead of stacking into a wall — while the sample-clamp keeps them
    // from ever crossing a wall or leaving the walkable area, no matter how the separation force
    // pushes them.
    void Steer(Vector3 destination, float moveSpeed)
    {
        Vector2 pathDir = GetPathDirection(destination);

        Vector2 separation = flock != null ? flock.ComputeSeparation(this) : Vector2.zero;
        separation = Vector2.ClampMagnitude(separation, maxSeparation) * separationStrength;

        Vector2 desired = pathDir * moveSpeed + separation;

        // Short lateral bias after a detected jam — breaks a symmetric wall-of-enemies deadlock.
        if (unstuckTimer > 0f)
        {
            unstuckTimer -= Time.deltaTime;
            Vector2 perp = new Vector2(-pathDir.y, pathDir.x) * unstuckSign;
            desired += perp * unstuckStrength;
        }

        Vector3 oldPos = transform.position;
        Vector3 wantedPos = oldPos + (Vector3)(desired * Time.deltaTime);

        if (NavMesh.SamplePosition(wantedPos, out NavMeshHit hit, navSampleDistance, NavMesh.AllAreas))
        {
            // Keep Z untouched — it's driven by sprite Y-sorting, not by the navmesh plane.
            transform.position = new Vector3(hit.position.x, hit.position.y, oldPos.z);
            currentVelocity = ((Vector2)transform.position - (Vector2)oldPos) / Mathf.Max(Time.deltaTime, 0.0001f);

            if (agent.isOnNavMesh)
                agent.nextPosition = transform.position; // keep the agent's path data in sync with reality
        }
        else
        {
            currentVelocity = Vector2.zero; // no valid nearby navmesh point this frame — hold position
        }

        UpdateStuck(destination, moveSpeed);
    }

    // Direction along the current navmesh path (toward the next corner), falling back to a
    // straight line at the destination when there is no path yet. Used to orient the unstick bias.
    Vector2 GetPathDirection(Vector3 destination)
    {
        if (agent.isOnNavMesh && agent.hasPath && !agent.pathPending)
        {
            Vector2 toCorner = (Vector2)(agent.steeringTarget - transform.position);
            if (toCorner.sqrMagnitude > 0.0001f)
                return toCorner.normalized;
        }

        Vector2 toDest = (Vector2)(destination - transform.position);
        return toDest.sqrMagnitude > 0.0001f ? toDest.normalized : Vector2.zero;
    }

    // Genuine-stuck check: if we wanted to move but barely did over the interval, request ONE
    // fresh path and pick a side to slip past. This is the only repath trigger besides the
    // target actually moving, so NavMesh recalculations stay rare.
    void UpdateStuck(Vector3 destination, float moveSpeed)
    {
        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckInterval)
            return;

        float moved = ((Vector2)transform.position - lastStuckPos).magnitude;
        float expected = moveSpeed * stuckTimer;

        if (moved < expected * stuckMoveFraction)
        {
            if (agent.isOnNavMesh)
            {
                agent.SetDestination(destination);
                lastDestination = destination;
            }

            unstuckSign = Random.value < 0.5f ? 1f : -1f;
            unstuckTimer = unstuckDuration;
        }

        lastStuckPos = transform.position;
        stuckTimer = 0f;
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

    // Decides which way the sprite faces. Driven by SMOOTHED horizontal velocity (with a
    // deadband + commit timer) while moving, so avoidance jostling and tiny steering
    // corrections never flip it — only a sustained horizontal commitment does. When the enemy
    // stops to attack it snaps to the target's side instead.
    void UpdateFacing()
    {
        // Attacking / stopped at range: face the target once, no chatter (velocity ~ 0 here).
        if (enemyState == EnemyState.Attacking)
        {
            if (player != null)
                FaceTowardX(player.position.x - transform.position.x);
            return;
        }

        float dt = Time.deltaTime;
        float k = 1f - Mathf.Exp(-facingSmoothing * dt); // frame-rate-independent EMA factor
        smoothedVelX = Mathf.Lerp(smoothedVelX, currentVelocity.x, k);

        // Deadband: below this the horizontal intent is just a steering correction — ignore it.
        if (Mathf.Abs(smoothedVelX) < flipEnterSpeed)
        {
            flipCommitTimer = 0f;
            return;
        }

        int desired = smoothedVelX > 0f ? 1 : -1;

        if (desired == facingDirection)
        {
            flipCommitTimer = 0f;
            return;
        }

        // Opposite side: require the new direction to persist before committing to the flip.
        flipCommitTimer += dt;
        if (flipCommitTimer >= flipCommitTime)
        {
            Flip();
            flipCommitTimer = 0f;
        }
    }

    // Flips to face a horizontal offset immediately (used when snapping to the attack target).
    void FaceTowardX(float xDiff)
    {
        if (xDiff > 0f && facingDirection == -1) Flip();
        else if (xDiff < 0f && facingDirection == 1) Flip();
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
            // Alvo morreu — checagem redundante ao evento OnPlayerDied (ver
            // comentário no campo playerHealth). Sem isso, um evento perdido faria
            // o inimigo perseguir o "cadáver" até o leash de loseAggroRange, que é
            // medido a partir do próprio spawn, não da posição do jogador.
            if (!HasLiveTarget())
            {
                player = null;
                playerHealth = null;
                ChangeState(EnemyState.Returning);
                return;
            }

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
                playerHealth = null;
                ChangeState(EnemyState.Returning);
                return;
            }

            // Histerese: uma vez parado (Idle/Attacking), o alvo precisa se afastar além
            // de attackRange + attackRangeHysteresis pra valer a pena voltar a perseguir.
            // Perseguindo, o critério pra parar continua o attackRange exato. Sem essa
            // assimetria, um alvo parado bem na borda do alcance faz o inimigo alternar
            // Idle/Chasing a cada frame — e cada entrada em Chasing desliza um passinho
            // via Steer(), mesmo sem nunca completar um ciclo de caminhada de verdade.
            bool currentlyStopped = enemyState == EnemyState.Idle || enemyState == EnemyState.Attacking;
            float effectiveAttackRange = currentlyStopped ? attackRange + attackRangeHysteresis : attackRange;

            // Está dentro do alcance de ataque.
            if (distance <= effectiveAttackRange)
            {
                StopMoving();

                if (attackCooldownTimer <= 0)
                {
                    attackCooldownTimer = attackCooldown;

                    ChangeState(EnemyState.Attacking);

                    if (combat != null)
                        combat.BeginAttack();
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
            playerHealth = hit.GetComponent<IDamageable>();
            FaceTowardX(player.position.x - transform.position.x);
            ChangeState(EnemyState.Chasing);
        }
    }

    public Transform GetPlayer()
    {
        return player;
    }

    public bool IsAttacking => enemyState == EnemyState.Attacking;

    public bool IsAggroed => player != null;

    // Usado por outras habilidades (ex.: Enemy_Abilities) para forçar a pose parada
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

        // Start stuck sampling fresh when we (re)start moving, so a just-resumed enemy isn't
        // flagged as stuck by leftover state from before it stopped.
        if (newState == EnemyState.Chasing || newState == EnemyState.Returning)
        {
            lastStuckPos = transform.position;
            stuckTimer = 0f;
            unstuckTimer = 0f;
        }
    }

    void ReturnToSpawn()
    {
        if (!agent.isOnNavMesh)
            return;

        // spawnPosition is fixed — set it once instead of every frame.
        if (lastDestination != spawnPosition)
        {
            agent.SetDestination(spawnPosition);
            lastDestination = spawnPosition;
        }

        float distance = Vector2.Distance(
            transform.position,
            spawnPosition);

        if (distance <= 0.2f)
        {
            StopMoving();

            player = null;
            playerHealth = null;
            attackCooldownTimer = 0;

            if (enemyHealth != null)
                enemyHealth.ResetEnemy();

            ChangeState(EnemyState.Idle);
            return;
        }

        Steer(spawnPosition, returnSpeed);
    }

    // Chamado por Enemy_Combat ao fim da sequência de ataque (windup -> dano ->
    // recovery), não mais por Animation Event.
    public void EndAttack()
    {
        // Cobre o caso do próprio golpe ter matado o jogador (ResolveDamage já rodou
        // antes disso, dentro da mesma sequência) e qualquer outro timing em que o
        // alvo tenha morrido durante a animação de ataque.
        if (!HasLiveTarget())
        {
            player = null;
            playerHealth = null;
            ChangeState(EnemyState.Returning);
            return;
        }

        float distanceFromSpawn = Vector2.Distance(
            transform.position,
            spawnPosition);

        if (distanceFromSpawn > loseAggroRange)
        {
            player = null;
            playerHealth = null;
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

    // IStunnable — reaplicar enquanto já atordoado só reinicia a duração (sem empilhar
    // coroutines nem somar tempo).
    public void ApplyStun(float duration)
    {
        if (stunRoutine != null)
            StopCoroutine(stunRoutine);

        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    private IEnumerator StunRoutine(float duration)
    {
        combat?.CancelAttack();

        // Sem isso o inimigo ficaria preso em Attacking pra sempre: é EndAttack() (chamado
        // pelo próprio ataque ao terminar) que normalmente tira dele desse estado, e acabamos
        // de cancelar o ataque antes disso rodar.
        if (enemyState == EnemyState.Attacking)
            ChangeState(EnemyState.Idle);

        StopMoving();
        SetIdlePose();

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (stunIndicator == null)
            stunIndicator = gameObject.AddComponent<StunIndicator>();

        stunIndicator.SetVisible(true, stunColor);

        // Desabilita o próprio componente: Update() (movimento, aggro, facing, cooldown de
        // ataque) para por completo. A coroutine continua rodando mesmo desabilitado — só
        // Update/FixedUpdate/etc. são pausados, não coroutines já iniciadas.
        enabled = false;

        yield return new WaitForSeconds(duration);

        enabled = true;

        stunIndicator.SetVisible(false, stunColor);

        if (agent.isOnNavMesh)
            agent.isStopped = false;

        RefreshAnimatorState();
        stunRoutine = null;
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