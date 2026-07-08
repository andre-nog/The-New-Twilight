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
    [Tooltip("Potencial ofensivo bruto — cada golpe multiplica isso pelo skillMultiplier do componente de ataque.")]
    public float attackPower = 1f;
    [Range(0, 100)]
    public float criticalChance = 0f;
    public float criticalDamage = 100f;

    [Header("Recompensa")]
    public int expReward = 3;
    public int goldReward = 1;

    // Loot table entra aqui quando ouro/drops existirem (planejado no GDD) — não construir antes.
}
