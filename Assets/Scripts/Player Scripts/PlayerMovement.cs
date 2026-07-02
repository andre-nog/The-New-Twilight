using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class PlayerMovement : MonoBehaviour
{
    public Rigidbody2D rb;
    public int facingDirection = 1;
    public Player_Combat player_Combat;

    private Vector2 _moveDirection;

    public InputActionReference move;
    public InputActionReference attack;

    public Animator anim;

    public bool autoMoving;

    private NavMeshPath currentPath;
    private int currentCorner;

    private void Start()
    {
        currentPath = new NavMeshPath();
    }

    private void Update()
    {
        Vector2 inputDirection = move.action.ReadValue<Vector2>();

        if (autoMoving)
        {
            if (inputDirection != Vector2.zero)
            {
                player_Combat.CancelMoveToAttack();
                _moveDirection = inputDirection;
            }
        }
        else
        {
            _moveDirection = inputDirection;
        }

        if (attack.action.WasPressedThisFrame())
        {
            player_Combat.Attack();
        }
    }

    private void FixedUpdate()
    {
        if (!autoMoving)
        {
            rb.linearVelocity = _moveDirection * StatsManager.Instance.moveSpeed;
        }
        else
        {
            FollowPath();
        }

        Vector2 velocity = rb.linearVelocity;

        anim.SetFloat("horizontal", Mathf.Abs(velocity.x));
        anim.SetFloat("vertical", Mathf.Abs(velocity.y));

        if (!player_Combat.isAttacking)
        {
            if ((velocity.x > 0.05f && transform.localScale.x < 0) ||
                (velocity.x < -0.05f && transform.localScale.x > 0))
            {
                Vector3 scale = transform.localScale;
                scale.x *= -1;
                transform.localScale = scale;
            }
        }
    }

    private void FollowPath()
    {
        if (currentPath == null || currentPath.corners.Length == 0)
        {
            StopMoving();
            return;
        }

        if (currentCorner >= currentPath.corners.Length)
        {
            StopMoving();
            return;
        }

        Vector2 target = currentPath.corners[currentCorner];

        Vector2 direction =
            (target - (Vector2)transform.position).normalized;

        rb.linearVelocity = direction * StatsManager.Instance.moveSpeed;

        if (Vector2.Distance(transform.position, target) < 0.1f)
        {
            currentCorner++;
        }
    }

    public void MoveTo(Vector3 destination)
    {
        Vector3 start = transform.position;

        // distância de busca generosa, cobre o base offset
        const float maxSnap = 2f;

        if (NavMesh.SamplePosition(start, out NavMeshHit startHit, maxSnap, NavMesh.AllAreas) &&
            NavMesh.SamplePosition(destination, out NavMeshHit endHit, maxSnap, NavMesh.AllAreas) &&
            NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, currentPath) &&
            currentPath.corners.Length > 0)
        {
            autoMoving = true;
            currentCorner = 1;
        }
        else
        {
            StopMoving(); // não conseguiu calcular, não trava em "auto movendo"
        }
    }

    public void CancelAutoMove()
    {
        autoMoving = false;
        rb.linearVelocity = Vector2.zero;
    }

    public void StopMoving()
    {
        autoMoving = false;
        _moveDirection = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }

    public void SetMoveDirection(Vector2 direction)
    {
        _moveDirection = direction;
    }
}