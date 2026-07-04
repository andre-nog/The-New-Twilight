using UnityEngine;

// Definição de inimigo como dado — o prefab cuida de visual/AI/física e lê os
// números daqui via EnemyStats. Variantes tipo "Elite" são feitas manualmente no
// próprio prefab do monstro (outro archetype ou tuning direto dos componentes).
[CreateAssetMenu(fileName = "New Enemy Archetype", menuName = "Enemies/Archetype")]
public class EnemyArchetypeSO : ScriptableObject
{
    public string displayName;

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

    // Loot table entra aqui quando ouro/drops existirem (planejado no GDD) — não construir antes.
}
