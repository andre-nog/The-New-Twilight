using UnityEngine;

public class Enemy_Combat : MonoBehaviour
{
    public int damage = 1;

    private Enemy_Movement enemyMovement;

    private void Start()
    {
        enemyMovement = GetComponent<Enemy_Movement>();
    }

    public void Attack()
    {
        if (enemyMovement == null)
            return;

        Transform player = enemyMovement.GetPlayer();

        if (player == null)
            return;

        PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();

        if (playerHealth != null)
        {
            playerHealth.ChangeHealth(-damage);
        }
    }
}