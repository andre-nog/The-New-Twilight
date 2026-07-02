using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Player_Combat : MonoBehaviour
{
    public Animator anim;
    public PlayerTargeting playerTargeting;
    public bool isAttacking { get; private set; }
    public PlayerMovement playerMovement;
    private float timer;
    private GameObject attackTarget;
    private bool movingToAttack;
    private bool waitingForCooldown;

    private void Update()
    {
        if (timer > 0)
        {
            timer -= Time.deltaTime;
        }

        if (movingToAttack)
        {
            MoveToTargetAndAttack();
        }

        if (waitingForCooldown)
        {
            if (playerTargeting.currentTarget == null)
            {
                waitingForCooldown = false;
                return;
            }

            float distance = Vector2.Distance(
                transform.position,
                playerTargeting.currentTarget.transform.position);

            if (distance > StatsManager.Instance.attackRange)
            {
                waitingForCooldown = false;
                return;
            }

            if (timer <= 0)
            {
                waitingForCooldown = false;
                ExecuteAttack();
            }
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
            movingToAttack = false;

            playerMovement.autoMoving = false;
            playerMovement.CancelAutoMove();

            return;
        }

        float distance = Vector2.Distance(
            transform.position,
            playerTargeting.currentTarget.transform.position);

        if (distance <= StatsManager.Instance.attackRange)
        {
            movingToAttack = false;

            playerMovement.autoMoving = false;
            playerMovement.CancelAutoMove();

            if (timer <= 0)
            {
                ExecuteAttack();
            }
            else
            {
                waitingForCooldown = true;
            }

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

    private void ExecuteAttack()
    {
        attackTarget = playerTargeting.currentTarget;

        FaceTarget();

        isAttacking = true;
        anim.SetBool("isAttacking", true);

        timer = StatsManager.Instance.attackcooldown;
    }

    public void Attack()
    {
        if (playerTargeting.currentTarget == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            playerTargeting.currentTarget.transform.position);

        if (distance > StatsManager.Instance.attackRange)
        {
            movingToAttack = true;
            playerMovement.autoMoving = true;
            return;
        }

        if (timer <= 0)
        {
            ExecuteAttack();
        }
        else
        {
            waitingForCooldown = true;
        }
    }

    public void DealDamage()
    {
        if (attackTarget == null)
            return;

        Enemy_Health enemyHealth =
            attackTarget.GetComponent<Enemy_Health>();

        if (enemyHealth == null)
            return;

        int damage = StatsManager.Instance.damage;

        bool critical = Random.Range(0f, 100f) <
                        StatsManager.Instance.criticalChance;

        if (critical)
        {
            damage = Mathf.RoundToInt(
                damage * (1f + StatsManager.Instance.criticalDamage / 100f)
            );
        }

        enemyHealth.ChangeHealth(-damage, critical);
    }

    public void FinishAttacking()
    {
        isAttacking = false; // <-- libera o flip de novo
        anim.SetBool("isAttacking", false);
    }
}