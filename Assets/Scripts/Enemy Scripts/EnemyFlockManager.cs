using System.Collections.Generic;
using UnityEngine;

// Central spatial hash used for enemy-vs-enemy separation ("boids" flow-around).
//
// Every enemy registers here. On demand (at most once per frame, lazily on the first query)
// the grid is rebuilt so each enemy resolves its neighbours in O(1) instead of scanning every
// other enemy — this O(N) rebuild + O(1) query is what lets separation scale to hundreds of
// agents. Enemies still pathfind individually via their own NavMeshAgent; this class only
// supplies the local separation force that makes packed enemies flow around each other and
// surround the target instead of forming a stuck wall.
//
// Created automatically the first time an enemy needs it (EnsureExists), so no scene wiring.
public class EnemyFlockManager : MonoBehaviour
{
    public static EnemyFlockManager Instance { get; private set; }

    [Tooltip("Enemies closer than this push each other apart. Also the grid cell size.")]
    [SerializeField] private float separationRadius = 0.7f;

    private float CellSize => separationRadius;

    private readonly List<Enemy_Movement> agents = new();
    private readonly Dictionary<long, List<Enemy_Movement>> grid = new();
    private readonly Stack<List<Enemy_Movement>> pool = new(); // reused lists, near-zero GC after warmup
    private int builtFrame = -1;

    public static void EnsureExists()
    {
        if (Instance != null)
            return;

        var go = new GameObject("EnemyFlockManager");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<EnemyFlockManager>();
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

    public void Register(Enemy_Movement agent)
    {
        if (agent != null && !agents.Contains(agent))
            agents.Add(agent);
    }

    public void Unregister(Enemy_Movement agent)
    {
        agents.Remove(agent);
    }

    private static long CellKey(int cx, int cy)
    {
        // Pack two ints into one long so the dictionary key is allocation-free.
        return ((long)cx << 32) ^ (uint)cy;
    }

    private void RebuildGrid()
    {
        foreach (var kv in grid)
        {
            kv.Value.Clear();
            pool.Push(kv.Value);
        }
        grid.Clear();

        float cell = CellSize;

        for (int i = 0; i < agents.Count; i++)
        {
            var a = agents[i];
            if (a == null)
                continue;

            Vector3 p = a.transform.position;
            int cx = Mathf.FloorToInt(p.x / cell);
            int cy = Mathf.FloorToInt(p.y / cell);
            long key = CellKey(cx, cy);

            if (!grid.TryGetValue(key, out var list))
            {
                list = pool.Count > 0 ? pool.Pop() : new List<Enemy_Movement>(8);
                grid[key] = list;
            }

            list.Add(a);
        }

        builtFrame = Time.frameCount;
    }

    // Sum of pushes away from neighbours within separationRadius, weighted by closeness
    // (~0 at the edge of the radius, ~1 at contact). Not normalized: a denser clump pushes
    // harder, which is what spreads a stuck pack. Callers clamp/weight the result.
    public Vector2 ComputeSeparation(Enemy_Movement self)
    {
        if (builtFrame != Time.frameCount)
            RebuildGrid();

        float cell = CellSize;
        float radiusSqr = separationRadius * separationRadius;
        Vector2 pos = self.transform.position;
        int cx = Mathf.FloorToInt(pos.x / cell);
        int cy = Mathf.FloorToInt(pos.y / cell);

        Vector2 push = Vector2.zero;

        for (int ox = -1; ox <= 1; ox++)
        for (int oy = -1; oy <= 1; oy++)
        {
            if (!grid.TryGetValue(CellKey(cx + ox, cy + oy), out var list))
                continue;

            for (int k = 0; k < list.Count; k++)
            {
                var other = list[k];
                if (other == null || other == self)
                    continue;

                Vector2 diff = pos - (Vector2)other.transform.position;
                float d2 = diff.sqrMagnitude;
                if (d2 >= radiusSqr || d2 < 1e-6f)
                    continue;

                float d = Mathf.Sqrt(d2);
                push += (diff / d) * (1f - d / separationRadius);
            }
        }

        return push;
    }
}
