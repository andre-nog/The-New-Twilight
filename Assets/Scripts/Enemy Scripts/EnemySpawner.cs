using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemySpawner : MonoBehaviour
{
    public GameObject enemyPrefab;
    public float respawnDelay = 10f;

    [Tooltip("Desligue para dungeons — o inimigo não volta depois de morrer.")]
    public bool respawns = true;

    private SpriteRenderer previewRenderer;
    private GameObject spawnedEnemy;
    private float respawnTimer;
    private bool waitingToRespawn;

    private void Awake()
    {
        previewRenderer = GetComponent<SpriteRenderer>();
        previewRenderer.enabled = false; // só serve de preview no editor, o inimigo real cuida do visual em Play
    }

    private void Start()
    {
        Spawn();
    }

    private void Update()
    {
        // spawnedEnemy compara igual a null assim que o inimigo é destruído
        // (Enemy_Health.ChangeHealth chama Destroy ao morrer) — não precisa de
        // evento nenhum pra saber que ele morreu.
        if (spawnedEnemy != null)
            return;

        if (!respawns)
            return;

        if (!waitingToRespawn)
        {
            waitingToRespawn = true;
            respawnTimer = respawnDelay;
            return;
        }

        respawnTimer -= Time.deltaTime;

        if (respawnTimer <= 0f)
            Spawn();
    }

    private void Spawn()
    {
        if (enemyPrefab == null)
            return;

        spawnedEnemy = Instantiate(enemyPrefab, transform.position, Quaternion.identity);
        waitingToRespawn = false;
    }

#if UNITY_EDITOR
    // Copia a aparência do enemyPrefab pro SpriteRenderer do próprio Spawner, assim ele
    // fica visível e reposicionável na Scene view igual ao Enemy real, sem precisar dar Play.
    // A cópia roda um tick depois (delayCall): setar SpriteRenderer.size direto dentro de
    // OnValidate dispara um SendMessage interno do Unity, que não é permitido nesse callback.
    private void OnValidate()
    {
        UnityEditor.EditorApplication.delayCall += SyncPreviewSprite;
    }

    private void SyncPreviewSprite()
    {
        if (this == null)
            return;

        if (previewRenderer == null)
            previewRenderer = GetComponent<SpriteRenderer>();

        if (previewRenderer == null)
            return;

        var sourceRenderer = enemyPrefab != null ? enemyPrefab.GetComponentInChildren<SpriteRenderer>() : null;
        if (sourceRenderer == null)
        {
            previewRenderer.sprite = null;
            return;
        }

        previewRenderer.enabled = !Application.isPlaying;
        previewRenderer.sprite = sourceRenderer.sprite;
        previewRenderer.color = sourceRenderer.color;
        previewRenderer.flipX = sourceRenderer.flipX;
        previewRenderer.flipY = sourceRenderer.flipY;
        previewRenderer.drawMode = sourceRenderer.drawMode;
        previewRenderer.size = sourceRenderer.size;
        previewRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        previewRenderer.sortingOrder = sourceRenderer.sortingOrder;
    }
#endif
}
