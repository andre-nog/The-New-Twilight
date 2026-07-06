using UnityEngine;

public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public PlayerTargeting playerTargeting;
    public bool isAttacking { get; private set; }
    public PlayerMovement playerMovement;
    public ResourceManager ResourceManager => resourceManager;

    // Contexto do cast em andamento — escrito UMA vez por tentativa de cast
    // (UseSkill) e lido pelos animation events. Substitui o trio mutável
    // currentSkill/attackTarget/damageMultiplierBonus.
    private CastContext pendingCast;
    private bool movingToAttack;
    private PlayerSkillManager skillManager;
    private ResourceManager resourceManager;

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
        if (pendingCast.Target == null)
        {
            CancelMoveToAttack();
            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            pendingCast.Target.transform.position);

        if (distance <= pendingCast.Skill.range)
        {
            CancelMoveToAttack();
            ExecuteSkill();
            return;
        }

        playerMovement.MoveTo(
            pendingCast.Target.transform.position);
    }

    private void FaceTarget(GameObject target)
    {
        float targetX = target.transform.position.x;

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
        Skill skill = pendingCast.Skill;

        if (skill == null)
            return;

        if (skill.requiresTarget)
        {
            if (pendingCast.Target == null)
                return;

            FaceTarget(pendingCast.Target);
        }

        // resourceCost não varia por nível — vem direto do campo fixo em Skill.
        // manaCost sim, então continua vindo do nível ativo (SkillLevelData).
        if (skill.resourceCost > 0 &&
            !resourceManager.SpendResource(skill.resourceCost))
            return;

        int manaCost = SkillProgression.DataFor(skill).manaCost;

        if (manaCost > 0 &&
            !StatsManager.Instance.SpendMana(manaCost))
            return;

        skillManager.StartCooldown(skill);

        isAttacking = true;

        if (skill.lockMovementDuringCast)
        {
            playerMovement.SetMovementLocked(true);
        }

        anim.SetTrigger(skill.animationTrigger);
    }

    // Passivas vêm da classe atual (ClassDefinitionSO) — trocar de classe troca
    // as passivas junto, sem re-wiring no Inspector.
    public float GetPassiveDamageMultiplier(Skill skill)
    {
        ClassDefinitionSO currentClass = StatsManager.Instance != null
            ? StatsManager.Instance.CurrentClass
            : null;

        if (currentClass == null || currentClass.passives == null)
            return 1f;

        float multiplier = 1f;

        foreach (Passive passive in currentClass.passives)
        {
            if (passive == null)
                continue;

            multiplier *= passive.ModifyDamageMultiplier(this, skill);
        }

        return multiplier;
    }
    public void UseSkill(Skill skill)
    {
        if (skill == null)
            return;

        // Skills não-aprendidas (nível 0) não podem ser usadas — mesmo que uma
        // referência ainda esteja num slot da barra.
        if (SkillProgression.LevelOf(skill) < 1)
            return;

        // Não pode trocar durante a animação
        if (isAttacking)
            return;

        // Pode trocar enquanto ainda está caminhando
        if (movingToAttack)
        {
            CancelMoveToAttack();
        }

        // Verifica se há recurso suficiente. resourceCost/range são fixos (Skill);
        // manaCost varia por nível (SkillLevelData).
        if (!resourceManager.HasResource(skill.resourceCost))
            return;

        if (!StatsManager.Instance.HasMana(SkillProgression.DataFor(skill).manaCost))
            return;

        // Skills que não precisam de alvo (ex.: Stomp)
        if (!skill.requiresTarget)
        {
            pendingCast = new CastContext(skill, null);
            ExecuteSkill();
            return;
        }

        GameObject selectedTarget = playerTargeting.currentTarget;

        if (selectedTarget == null)
            return;

        pendingCast = new CastContext(skill, selectedTarget);

        float distance = Vector2.Distance(
            transform.position,
            selectedTarget.transform.position);

        if (distance > skill.range)
        {
            movingToAttack = true;
            playerMovement.autoMoving = true;
            return;
        }

        ExecuteSkill();
    }

    private void DealDamage(IDamageable target, in CastContext ctx)
    {
        float offensivePower = ctx.Skill.damageSchool == DamageSchool.Magical
            ? StatsManager.Instance.SpellPower
            : StatsManager.Instance.AttackPower;

        DamageResult result = DamageCalculator.Calculate(
            offensivePower,
            SkillProgression.DataFor(ctx.Skill).damageMultiplier,
            ctx.ExtraMultiplier * GetPassiveDamageMultiplier(ctx.Skill),
            StatsManager.Instance.CriticalChance,
            StatsManager.Instance.CriticalDamage,
            target.Armor);

        target.TakeDamage(result);
    }
    public void DealAreaDamage(float radius, in CastContext ctx)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(
            transform.position,
            radius,
            enemyLayer);

        foreach (Collider2D hit in hits)
        {
            IDamageable target = hit.GetComponent<IDamageable>();

            if (target == null || !target.IsAlive)
                continue;

            DealDamage(target, ctx);
        }

        SpawnHitVFX(transform.position, ctx.Skill);
    }
    public void ExecuteSkillEffect()
    {
        Skill skill = pendingCast.Skill;

        if (skill == null)
            return;

        skill.ExecuteEffect(this, pendingCast);

        int resourceGenerated = SkillProgression.DataFor(skill).resourceGenerated;

        if (resourceGenerated > 0)
        {
            resourceManager.AddResource(resourceGenerated);
        }
    }
    public void ReleaseMovement()
    {
        if (pendingCast.Skill != null && pendingCast.Skill.lockMovementDuringCast)
        {
            playerMovement.SetMovementLocked(false);
        }
    }
    public void DealDamageToTarget(in CastContext ctx)
    {
        if (ctx.Target == null)
            return;

        IDamageable target =
            ctx.Target.GetComponent<IDamageable>();

        if (target == null || !target.IsAlive)
            return;

        DealDamage(target, ctx);

        SpawnHitVFX(ctx.Target.transform.position, ctx.Skill);
    }
    private void SpawnHitVFX(Vector3 position, Skill skill)
    {
        if (skill.hitVFX == null)
            return;

        Instantiate(
            skill.hitVFX,
            position + skill.hitVFXOffset,
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
