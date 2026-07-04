using UnityEngine;
using UnityEngine.AI;

public class Enemy_RangedAttack : MonoBehaviour
{
    [Header("Timing")]
    public float telegraphInterval = 3f;
    public float telegraphDuration = 1f;
    public float throwTravelTime = 0.2f;

    [Header("Gameplay")]
    [Tooltip("Multiplicador do ataque à distância sobre o Attack Power do arquétipo.")]
    public float skillMultiplier = 1f;
    public float areaRadius = 1.5f;

    [Header("Ponto de contato do jogador")]
    [Tooltip("Deslocamento do transform do jogador até o ponto dos \"pés\" (centro da caixinha de contato). Ajuste olhando o gizmo ciano na Scene view.")]
    public Vector2 playerFeetOffset = new Vector2(0f, -0.48f);
    [Tooltip("Tamanho da caixinha de contato do jogador — o dano só é aplicado quando ela toca o círculo do ataque.")]
    public Vector2 playerHitboxSize = new Vector2(0.3f, 0.05f);

    [Header("Visuals (opcional)")]
    public GameObject telegraphPrefab;
    public GameObject projectilePrefab;

    private const int PlaceholderTextureResolution = 64;

    private enum State
    {
        Idle,
        Telegraphing,
        Throwing
    }

    private State state = State.Idle;
    private float timer;
    private float stateTimer;
    private Vector3 targetPosition;
    private Vector3 throwOrigin;

    // Instâncias reusadas por inimigo (SetActive em vez de Instantiate/Destroy por
    // ataque) — cada inimigo tem no máximo um telégrafo e um projétil ativos.
    private GameObject telegraphInstance;
    private GameObject projectileInstance;
    private SpriteRenderer telegraphSprite;   // só preenchido no placeholder (prefab fica como veio)
    private SpriteRenderer projectileSprite;

    // O placeholder é sempre o mesmo círculo branco — a cor vai no SpriteRenderer.color.
    // Gerar a textura pixel a pixel a cada ataque era o maior custo de GC do ranged.
    private static Sprite circleSprite;

    private Enemy_Movement enemyMovement;
    private NavMeshAgent agent;
    private SpriteRenderer bodySprite;
    private EnemyStats stats;

    // Sorting layer do alvo atual (o jogador pode estar em uma Sorting Layer diferente
    // da do inimigo), para o telégrafo/projétil desenharem na camada certa.
    private int targetSortingLayerID;

    private void Awake()
    {
        enemyMovement = GetComponent<Enemy_Movement>();
        agent = GetComponent<NavMeshAgent>();
        bodySprite = GetComponent<SpriteRenderer>();
        stats = GetComponent<EnemyStats>();
    }

    private void OnDestroy()
    {
        if (telegraphInstance != null)
            Destroy(telegraphInstance);

        if (projectileInstance != null)
            Destroy(projectileInstance);
    }

    private void Update()
    {
        switch (state)
        {
            case State.Idle:
                TickIdle();
                break;

            case State.Telegraphing:
                TickTelegraphing();
                break;

            case State.Throwing:
                TickThrowing();
                break;
        }
    }

    private void TickIdle()
    {
        Transform player = enemyMovement.GetPlayer();

        if (player == null)
        {
            timer = 0f;
            return;
        }

        // Só pausa o acúmulo durante o soco corpo a corpo, sem zerar o progresso —
        // senão o soco (mais frequente que o intervalo do ranged) nunca deixa o
        // timer chegar lá enquanto o jogador fica no alcance de melee.
        if (enemyMovement.IsAttacking)
            return;

        timer += Time.deltaTime;

        if (timer < telegraphInterval)
            return;

        timer = 0f;
        BeginTelegraph(player);
    }

    private void BeginTelegraph(Transform player)
    {
        targetPosition = GetFeetPosition(player);
        stateTimer = telegraphDuration;
        state = State.Telegraphing;

        SpriteRenderer playerSprite = player.GetComponent<SpriteRenderer>();
        targetSortingLayerID = playerSprite != null
            ? playerSprite.sortingLayerID
            : (bodySprite != null ? bodySprite.sortingLayerID : 0);

        enemyMovement.enabled = false;
        enemyMovement.SetIdlePose();

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        ShowTelegraph(targetPosition);
    }

    private void TickTelegraphing()
    {
        stateTimer -= Time.deltaTime;

        if (stateTimer > 0f)
            return;

        if (telegraphInstance != null)
            telegraphInstance.SetActive(false);

        throwOrigin = transform.position;
        stateTimer = throwTravelTime;
        state = State.Throwing;

        ShowProjectile(throwOrigin);
    }

    private void TickThrowing()
    {
        stateTimer -= Time.deltaTime;

        float t = throwTravelTime > 0f
            ? 1f - Mathf.Clamp01(stateTimer / throwTravelTime)
            : 1f;

        if (projectileInstance != null)
            projectileInstance.transform.position = Vector3.Lerp(throwOrigin, targetPosition, t);

        if (stateTimer > 0f)
            return;

        if (projectileInstance != null)
            projectileInstance.SetActive(false);

        ResolveDamage(targetPosition);

        enemyMovement.RefreshAnimatorState();
        enemyMovement.enabled = true;
        state = State.Idle;
    }

    // Dano é aplicado quando o círculo do ataque toca a caixinha de contato do jogador
    // (não o collider físico dele, que cobre o corpo quase inteiro e deixava o dano
    // "errado" em relação à área desenhada).
    private void ResolveDamage(Vector3 position)
    {
        Transform player = enemyMovement.GetPlayer();

        if (player == null)
            return;

        if (!CircleIntersectsBox(position, areaRadius, GetFeetPosition(player), playerHitboxSize))
            return;

        IDamageable target = player.GetComponent<IDamageable>();

        if (target == null || !target.IsAlive)
            return;

        // Mesmo pipeline do jogador — crítico, variância e mitigação por Armor.
        DamageResult result = DamageCalculator.Calculate(
            stats.AttackPower,
            skillMultiplier,
            1f,
            stats.CriticalChance,
            stats.CriticalDamage,
            target.Armor);

        target.TakeDamage(result);
    }

    private static bool CircleIntersectsBox(Vector2 circleCenter, float radius, Vector2 boxCenter, Vector2 boxSize)
    {
        Vector2 halfSize = boxSize * 0.5f;
        Vector2 min = boxCenter - halfSize;
        Vector2 max = boxCenter + halfSize;

        Vector2 closestPoint = new(
            Mathf.Clamp(circleCenter.x, min.x, max.x),
            Mathf.Clamp(circleCenter.y, min.y, max.y));

        return Vector2.Distance(circleCenter, closestPoint) <= radius;
    }

    // O transform do player não fica exatamente nos pés — playerFeetOffset compensa essa
    // diferença. É um valor ajustável no Inspector (em vez de derivado do collider) porque
    // depende de como o pivot do sprite foi configurado, algo mais fácil de calibrar
    // olhando o jogo rodar do que de acertar só lendo números.
    private Vector2 GetFeetPosition(Transform player)
    {
        return (Vector2)player.position + playerFeetOffset;
    }

    private void OnDrawGizmos()
    {
        Transform player = GetGizmoPlayer();

        if (player == null)
            return;

        Vector2 feet = GetFeetPosition(player);

        // Caixinha fixa de contato do jogador — sempre relevante, mesmo fora do Play Mode.
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(feet, new Vector3(playerHitboxSize.x, playerHitboxSize.y, 0.1f));

        // Preview do círculo do ataque, centralizado nela, como se o Orc mirasse agora —
        // ajuda a calibrar os dois tamanhos juntos.
        Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.9f);
        Gizmos.DrawWireSphere(feet, areaRadius);
    }

    // Em Play Mode usa o alvo real de agressão do inimigo; fora dele (Editor parado)
    // não existe esse estado ainda, então busca o jogador pela tag só para o preview.
    private Transform GetGizmoPlayer()
    {
        if (Application.isPlaying)
            return enemyMovement != null ? enemyMovement.GetPlayer() : null;

        return FindPlayerInEditor();
    }

    private static Transform FindPlayerInEditor()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        return playerObject != null ? playerObject.transform : null;
    }

    private void ShowTelegraph(Vector3 position)
    {
        if (telegraphInstance == null)
        {
            if (telegraphPrefab != null)
            {
                telegraphInstance = Instantiate(telegraphPrefab, position, Quaternion.identity);
            }
            else
            {
                telegraphInstance = CreatePlaceholderCircle(
                    "Telegraph",
                    areaRadius * 2f,
                    new Color(1f, 0.15f, 0.1f, 0.55f),
                    -1000000);
                telegraphSprite = telegraphInstance.GetComponent<SpriteRenderer>();
            }
        }

        telegraphInstance.transform.position = position;

        // A Sorting Layer do alvo pode mudar entre um ataque e outro.
        if (telegraphSprite != null)
            telegraphSprite.sortingLayerID = targetSortingLayerID;

        telegraphInstance.SetActive(true);
    }

    private void ShowProjectile(Vector3 position)
    {
        if (projectileInstance == null)
        {
            if (projectilePrefab != null)
            {
                projectileInstance = Instantiate(projectilePrefab, position, Quaternion.identity);
            }
            else
            {
                projectileInstance = CreatePlaceholderCircle(
                    "Projectile",
                    0.3f,
                    new Color(0.25f, 0.15f, 0.1f, 1f),
                    1000000);
                projectileSprite = projectileInstance.GetComponent<SpriteRenderer>();
            }
        }

        projectileInstance.transform.position = position;

        if (projectileSprite != null)
            projectileSprite.sortingLayerID = targetSortingLayerID;

        projectileInstance.SetActive(true);
    }

    private GameObject CreatePlaceholderCircle(string objectName, float diameter, Color color, int sortingOrder)
    {
        GameObject instance = new(objectName);
        instance.transform.localScale = new Vector3(diameter, diameter, 1f);

        SpriteRenderer sprite = instance.AddComponent<SpriteRenderer>();
        sprite.sprite = GetCircleSprite();
        sprite.color = color;
        sprite.sortingOrder = sortingOrder;

        return instance;
    }

    // Círculo branco genérico, gerado uma única vez e compartilhado por todos os
    // inimigos — cada uso tinta via SpriteRenderer.color.
    private static Sprite GetCircleSprite()
    {
        if (circleSprite != null)
            return circleSprite;

        const int resolution = PlaceholderTextureResolution;

        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                // Borda suave de ~2px para não ficar serrilhado.
                float alpha = Mathf.Clamp01((radius - distance) / 2f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution,
            0,
            SpriteMeshType.FullRect);

        return circleSprite;
    }
}
