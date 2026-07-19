using UnityEngine;

// Presença deste componente é o toggle de respawn: inimigo de dungeon simplesmente
// não recebe ele e morre normal em Enemy_Health. enemyPrefab é uma auto-referência
// (arraste o próprio prefab deste inimigo aqui) — necessária porque o GameObject que
// morreu não existe mais pra ser reaproveitado; ScheduleRespawn() pede pro
// EnemyRespawnManager instanciar uma cópia nova depois do delay.
[RequireComponent(typeof(Enemy_Health))]
public class EnemyRespawn : MonoBehaviour
{
    [Tooltip("O próprio prefab deste inimigo — arraste o prefab dele aqui.")]
    [SerializeField] private GameObject enemyPrefab;

    [SerializeField] private float respawnDelay = 10f;

    public void ScheduleRespawn()
    {
        if (enemyPrefab == null)
            return;

        EnemyRespawnManager.EnsureExists();
        EnemyRespawnManager.Instance.Schedule(enemyPrefab, transform.position, respawnDelay);
    }
}
