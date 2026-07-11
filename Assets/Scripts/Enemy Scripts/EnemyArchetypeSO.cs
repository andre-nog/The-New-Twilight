using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Definição de inimigo como dado — o prefab cuida de visual/AI/física e lê os
// números daqui via EnemyStats. Variantes tipo "Elite" são feitas manualmente no
// próprio prefab do monstro (outro archetype ou tuning direto dos componentes).
[CreateAssetMenu(fileName = "New Enemy Archetype", menuName = "Enemies/Archetype")]
public class EnemyArchetypeSO : ScriptableObject
{
    public string displayName;

    // Id estável para uso futuro em save/world-delta (ex.: "boss X já morto, não
    // respawnar") — o GUID do próprio asset, não muda ao renomear/mover o arquivo.
    // Não consumido hoje (ver Assets/Scripts/Quests/QuestSO.cs, que referencia o
    // archetype diretamente em vez de por string), preparado pro dia em que um
    // sistema de save precisar identificar um archetype por string. Nunca editar
    // à mão; preenchido automaticamente no Editor.
    [SerializeField, HideInInspector] private string id;
    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }

    public void EnsureId()
    {
        if (!string.IsNullOrEmpty(id))
            return;

        string path = AssetDatabase.GetAssetPath(this);

        if (string.IsNullOrEmpty(path))
            return;

        id = AssetDatabase.AssetPathToGUID(path);
        EditorUtility.SetDirty(this);
    }
#endif

    [Header("Defesa")]
    public int maxHealth = 100;
    [Tooltip("Mitigação de dano recebido — a fórmula fica centralizada em DamageCalculator, não aqui.")]
    public float armor = 0f;

    [Header("Ataque")]
    [Tooltip("Dano do ataque básico deste inimigo (corpo a corpo ou projétil, conforme o componente presente) — valor direto, mitigado pela Armor do alvo. Habilidades (Enemy_Abilities) têm seu próprio dano, independente deste.")]
    public float attackPower = 1f;
    [Range(0, 100)]
    public float criticalChance = 0f;
    public float criticalDamage = 100f;

    [Tooltip("Tempo entre um ataque corpo a corpo e o próximo ficar disponível.")]
    public float attackCooldown = 2f;
    [Tooltip("Tempo entre o gatilho da animação de ataque e o dano ser de fato aplicado — substitui o Animation Event antigo, cravado no clipe. Ajuste pra casar com o instante do golpe na animação.")]
    public float attackWindup = 0.28f;
    [Tooltip("Tempo depois do dano até o inimigo voltar a poder se mover/perseguir.")]
    public float attackRecovery = 0.2f;

    [Header("Burn (opcional)")]
    [Tooltip("Se marcado, um ataque básico bem-sucedido deste inimigo aplica queimadura no alvo.")]
    public bool appliesBurn = false;
    public float burnTickDamage = 5f;
    public float burnTickInterval = 2f;
    public float burnDuration = 6f;

    [Header("Movimento")]
    public float moveSpeed = 2f;
    [Tooltip("Velocidade ao retornar pro ponto de spawn — hoje pode ser igual ou diferente de moveSpeed.")]
    public float returnSpeed = 2f;
    public float detectionRange = 5f;
    public float attackRange = 1.2f;
    [Tooltip("Distância do spawn a partir da qual o inimigo desiste da perseguição e volta.")]
    public float chaseDistanceLimit = 15f;

    [Header("Físico")]
    [Tooltip("Aplicado no NavMeshAgent do inimigo ao entrar em cena.")]
    public float physicalRadius = 0.2f;
    public float physicalHeight = 0.2f;

    [Header("Visual")]
    [Tooltip("Sprite inicial do SpriteRenderer — a animação assume o controle depois.")]
    public Sprite defaultSprite;
    [Tooltip("Animator Override Controller sobre a base compartilhada (EnemyBase.controller) — troca só os clipes, nunca o grafo/parâmetros.")]
    public AnimatorOverrideController animatorOverride;

    [Header("Áudio (opcional)")]
    public AudioClip attackSfx;
    public AudioClip hitSfx;
    public AudioClip deathSfx;

    [Header("UI Flutuante")]
    [Tooltip("Deslocamento da barra de vida acima do inimigo — depende do tamanho do sprite.")]
    public Vector3 healthBarOffset = new Vector3(0f, 0.637f, 0f);
    public Vector3 damageTextOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 xpTextOffset = new Vector3(0f, 0.5f, 0f);
    public Vector3 goldTextOffset = new Vector3(0f, 0.25f, 0f);

    [Header("Recompensa")]
    public int expReward = 3;
    public int goldReward = 1;

    // Loot table entra aqui quando ouro/drops existirem (planejado no GDD) — não construir antes.
}
