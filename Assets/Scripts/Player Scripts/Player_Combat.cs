using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public PlayerTargeting playerTargeting;
    public bool isAttacking { get; private set; }
    public PlayerMovement playerMovement;
    public ResourceManager ResourceManager => resourceManager;

    private GameObject attackTarget;
    private bool movingToAttack;
    private Skill currentSkill;
    private PlayerSkillManager skillManager;
    private ResourceManager resourceManager;
    private float damageMultiplierBonus = 1f;

    [SerializeField]
    private Passive[] passives;

    [SerializeField]
    private LayerMask enemyLayer;

    [SerializeField]
    private bool showDebugRadius = true;

    [SerializeField]
    private float debugRadius = 2.5f;
    

    private void Awake()
    {
        skillManager = GetComponent<PlayerSkillManager>();
        resourceManager = GetComponent<ResourceManager>();
    }

    private void Update()
    {
        if (movingToAttack)
        {
            MoveToTargetAndAttack();
        }
    }

    public void CancelMoveToAttack()
    {
        movingToAttack = false;

        playerMovement.autoMoving = false;
        playerMovement.CancelAutoMove();
    }

    private void MoveToTargetAndAttack()
    {
        if (playerTargeting.currentTarget == null)
        {
            CancelMoveToAttack();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            playerTargeting.currentTarget.transform.position);

        if (distance <= currentSkill.range)
        {
            CancelMoveToAttack();
            ExecuteSkill();
            return;
        }

        playerMovement.MoveTo(
            playerTargeting.currentTarget.transform.position);
    }

    private void FaceTarget()
    {
        float targetX = playerTargeting.currentTarget.transform.position.x;

        if ((targetX > transform.position.x && transform.localScale.x < 0) ||
            (targetX < transform.position.x && transform.localScale.x > 0))
        {
            Vector3 scale = transform.localScale;
            scale.x *= -1;
            transform.localScale = scale;
        }
    }
    private void ExecuteSkill()
    {
        if (currentSkill.requiresTarget)
        {
            attackTarget = playerTargeting.currentTarget;
            FaceTarget();
        }

        if (currentSkill.momentumCost > 0)
        {
            resourceManager.SpendMomentum(currentSkill.momentumCost);
        }

        skillManager.StartCooldown(currentSkill);

        isAttacking = true;

        if (currentSkill.lockMovementDuringCast)
        {
            playerMovement.SetMovementLocked(true);
        }

        anim.SetTrigger(currentSkill.animationTrigger);
    }
    private float GetPassiveDamageMultiplier()
    {
        float multiplier = 1f;

        foreach (Passive passive in passives)
        {
            multiplier *= passive.ModifyDamageMultiplier(this, currentSkill);
        }

        return multiplier;
    }
    public void UseSkill(Skill skill)
    {
        // Não pode trocar durante a animação
        if (isAttacking)
            return;

        // Pode trocar enquanto ainda está caminhando
        if (movingToAttack)
        {
            CancelMoveToAttack();
        }

        // Verifica se há Momentum suficiente
        if (!resourceManager.HasMomentum(skill.momentumCost))
            return;

        currentSkill = skill;

        // Skills que não precisam de alvo (ex.: Stomp)
        if (!currentSkill.requiresTarget)
        {
            ExecuteSkill();
            return;
        }

        if (playerTargeting.currentTarget == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            playerTargeting.currentTarget.transform.position);

        if (distance > currentSkill.range)
        {
            movingToAttack = true;
            playerMovement.autoMoving = true;
            return;
        }

        ExecuteSkill();
    }

    private void DealDamage(Enemy_Health enemyHealth)
    {
        int damage = Mathf.RoundToInt(
        StatsManager.Instance.damage *
        currentSkill.damageMultiplier *
        damageMultiplierBonus *
        GetPassiveDamageMultiplier());

        bool critical =
            Random.Range(0f, 100f) <
            StatsManager.Instance.criticalChance;

        if (critical)
        {
            damage = Mathf.RoundToInt(
                damage * (1f + StatsManager.Instance.criticalDamage / 100f));
        }

        enemyHealth.ChangeHealth(-damage, critical);
    }
    public void DealAreaDamage(float radius)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            enemyLayer);

        foreach (Collider2D hit in hits)
        {
            Enemy_Health enemyHealth = hit.GetComponent<Enemy_Health>();

            if (enemyHealth == null)
                continue;

            DealDamage(enemyHealth);
        }

        SpawnHitVFX(transform.position);
    }
    public void ExecuteSkillEffect()
    {
        currentSkill.ExecuteEffect(this);

        if (currentSkill.momentumGenerated > 0)
        {
            resourceManager.AddMomentum(currentSkill.momentumGenerated);
        }
    }
    public void ReleaseMovement()
    {
        if (currentSkill.lockMovementDuringCast)
        {
            playerMovement.SetMovementLocked(false);
        }
    }

    public void SetDamageMultiplierBonus(float multiplier)
    {
        damageMultiplierBonus = multiplier;
    }

    public void ResetDamageMultiplierBonus()
    {
        damageMultiplierBonus = 1f;
    }
    public void DealDamageToTarget()
    {
        if (attackTarget == null)
            return;

        Enemy_Health enemyHealth =
            attackTarget.GetComponent<Enemy_Health>();

        if (enemyHealth == null)
            return;

        DealDamage(enemyHealth);

        SpawnHitVFX(attackTarget.transform.position);
    }
    private void SpawnHitVFX(Vector3 position)
    {
        if (currentSkill.hitVFX == null)
            return;

        Instantiate(
            currentSkill.hitVFX,
            position + currentSkill.hitVFXOffset,
            Quaternion.identity);
    }
    public void FinishAttacking()
    {
        isAttacking = false;
    }
    private void OnDrawGizmosSelected()
    {
        if (!showDebugRadius)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, debugRadius);
    }
}