using UnityEngine;
using System;

// Tracks whether the player is "in combat" — defined as having at least one live
// enemy currently aggroed onto them. Poll-based (not an incrementing counter):
// Enemy_Health.ChangeHealth can Destroy() an enemy mid-aggro without ever notifying
// Enemy_Movement to release its target, so a counter would leak and get stuck
// "in combat" forever. Polling Enemy_Health.Active (already reliably maintained via
// OnEnable/OnDisable) every pollInterval seconds re-derives the truth from scratch
// each tick instead, sidestepping that desync entirely.
public class CombatStateTracker : MonoBehaviour
{
    public static CombatStateTracker Instance { get; private set; }

    [Tooltip("How often (seconds) to re-scan for aggroed enemies. Combat state doesn't need per-frame precision.")]
    [SerializeField] private float pollInterval = 0.25f;

    public bool IsInCombat { get; private set; }
    public event Action<bool> OnCombatStateChanged;

    private float pollTimer;

    public static void EnsureCreated()
    {
        if (Instance != null)
            return;

        GameObject host = new("Combat State Tracker");
        DontDestroyOnLoad(host);
        host.AddComponent<CombatStateTracker>();
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

    private void Update()
    {
        pollTimer -= Time.deltaTime;

        if (pollTimer > 0f)
            return;

        pollTimer = pollInterval;
        Poll();
    }

    private void Poll()
    {
        bool aggroed = ComputeAggroed();

        if (aggroed == IsInCombat)
            return;

        IsInCombat = aggroed;
        OnCombatStateChanged?.Invoke(IsInCombat);
    }

    private static bool ComputeAggroed()
    {
        foreach (Enemy_Health enemy in Enemy_Health.Active)
        {
            if (enemy != null && enemy.Movement != null && enemy.Movement.IsAggroed)
                return true;
        }

        return false;
    }
}
