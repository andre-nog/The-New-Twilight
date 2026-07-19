using System.Collections.Generic;
using UnityEngine;

// Sem DontDestroyOnLoad (diferente de EnemyFlockManager): GameManager.EnterWorld()
// restaura o mundo recarregando a cena, e todo inimigo autorado volta full-health do
// zero sem serializar nada. Se este manager sobrevivesse ao reload, um respawn
// pendente de antes dele disparar criaria uma cópia duplicada em cima do inimigo já
// restaurado — ficando scene-local ele morre junto no reload, igual o timer do
// EnemySpawner antigo já morria por estar no GameObject do próprio spawner.
public class EnemyRespawnManager : MonoBehaviour
{
    public static EnemyRespawnManager Instance { get; private set; }

    private struct PendingRespawn
    {
        public GameObject prefab;
        public Vector3 position;
        public float timer;
    }

    private readonly List<PendingRespawn> pending = new();

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        Instance = new GameObject("EnemyRespawnManager").AddComponent<EnemyRespawnManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Schedule(GameObject prefab, Vector3 position, float delay)
    {
        pending.Add(new PendingRespawn { prefab = prefab, position = position, timer = delay });
    }

    private void Update()
    {
        for (int i = pending.Count - 1; i >= 0; i--)
        {
            PendingRespawn entry = pending[i];
            entry.timer -= Time.deltaTime;

            if (entry.timer <= 0f)
            {
                Instantiate(entry.prefab, entry.position, Quaternion.identity);
                pending.RemoveAt(i);
            }
            else
            {
                pending[i] = entry;
            }
        }
    }
}
