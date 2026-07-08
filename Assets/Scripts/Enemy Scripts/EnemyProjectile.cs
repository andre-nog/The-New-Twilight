using UnityEngine;

// Voo reto genérico pra qualquer projétil de ataque básico à distância — o
// prefab define a aparência (SpriteRenderer/Animator próprios, à vontade), este
// script só cuida do deslocamento e resolve o dano no impacto. Configurável por
// inimigo via Enemy_RangedBasicAttack.projectilePrefab: trocar o visual (ou usar
// um projétil totalmente diferente por inimigo) nunca exige mexer em código.
//
// Ataque básico via tab-target: o acerto já está garantido no disparo, então o
// projétil persegue a posição atual do alvo a cada frame (homing) em vez de
// mirar num ponto fixo — evita a leitura visual de "desviou" quando na
// verdade não havia chance de esquiva nenhuma.
public class EnemyProjectile : MonoBehaviour
{
    [Tooltip("Distância pra contar como 'chegou' — evita passar direto do alvo num frame de FPS baixo.")]
    [SerializeField] private float hitDistance = 0.1f;

    [Tooltip("Tempo de vida máximo — evita o projétil voar pra sempre se o alvo desaparecer no meio do caminho.")]
    [SerializeField] private float maxLifetime = 3f;

    private Vector3 targetPosition;
    private Transform targetTransform;
    private float speed;
    private float damage;
    private float criticalChance;
    private float criticalDamage;
    private float lifeTimer;
    private bool launched;
    private SpriteRenderer spriteRenderer;

    // Se o prefab não veio com sprite próprio (ex.: o EnemyProjectile_Circle
    // genérico), usa o mesmo círculo placeholder de Enemy_RangedBasicAttack — sem
    // isso um projétil sem arte própria seria invisível.
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite == null)
            spriteRenderer.sprite = PlaceholderSpriteFactory.GetCircleSprite();
    }

    // Chamado pelo spawner (Enemy_RangedBasicAttack) logo após Instantiate. Os
    // parâmetros de dano vêm crus (damage/crit) — a Armor do alvo só é lida no
    // impacto, não aqui, igual ao resto do pipeline de combate.
    public void Launch(Transform target, float projectileSpeed, float damageAmount, float critChance, float critDamage)
    {
        targetTransform = target;
        targetPosition = target != null ? target.position : transform.position;
        speed = Mathf.Max(0.01f, projectileSpeed);
        damage = damageAmount;
        criticalChance = critChance;
        criticalDamage = critDamage;
        launched = true;

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction.normalized;
    }

    private void Update()
    {
        if (!launched)
            return;

        lifeTimer += Time.deltaTime;

        if (lifeTimer >= maxLifetime)
        {
            Destroy(gameObject);
            return;
        }

        // Hit is guaranteed by the tab-target system regardless — re-aim at the
        // target's current position every frame (homing) instead of the point
        // captured at launch, so a moving target can't visually "dodge" a hit
        // that was already locked in.
        if (targetTransform != null)
            targetPosition = targetTransform.position;

        Vector3 direction = targetPosition - transform.position;
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction.normalized;

        transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);

        if (Vector3.Distance(transform.position, targetPosition) <= hitDistance)
            ResolveImpact();
    }

    private void ResolveImpact()
    {
        launched = false;

        if (targetTransform != null)
        {
            IDamageable target = targetTransform.GetComponent<IDamageable>();
            EnemyDamage.TryDealDamage(target, damage, criticalChance, criticalDamage);
        }

        Destroy(gameObject);
    }
}
