using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public PlayerTargeting playerTargeting;
    public bool isAttacking { get; private set; }
    public PlayerMovement playerMovement;

    private GameObject attackTarget;
    private bool movingToAttack;
    private Skill currentSkill;
    private PlayerSkillManager skillManager;

    private void Awake()
    {
        skillManager = GetComponent<PlayerSkillManager>();
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
        attackTarget = playerTargeting.currentTarget;

        FaceTarget();

        skillManager.StartCooldown(currentSkill);

        isAttacking = true;

        anim.SetTrigger(currentSkill.animationTrigger);
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

        currentSkill = skill;

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

    public void ExecuteSkillEffect()
    {
        currentSkill.ExecuteEffect(this);
    }

    public void DealDamageToTarget()
    {
        if (attackTarget == null)
            return;

        Enemy_Health enemyHealth =
            attackTarget.GetComponent<Enemy_Health>();

        if (enemyHealth == null)
            return;

        int damage = Mathf.RoundToInt(
            StatsManager.Instance.damage * currentSkill.damageMultiplier);

        bool critical =
            Random.Range(0f, 100f) <
            StatsManager.Instance.criticalChance;

        if (critical)
        {
            damage = Mathf.RoundToInt(
                damage * (1f + StatsManager.Instance.criticalDamage / 100f));
        }

        enemyHealth.ChangeHealth(-damage, critical);
        SpawnHitVFX();
    }
    private void SpawnHitVFX()
    {
        if (currentSkill.hitVFX == null)
            return;

        Instantiate(
            currentSkill.hitVFX,
            attackTarget.transform.position + currentSkill.hitVFXOffset,
            Quaternion.identity);
    }

    public void FinishAttacking()
    {
        isAttacking = false;
    }
}