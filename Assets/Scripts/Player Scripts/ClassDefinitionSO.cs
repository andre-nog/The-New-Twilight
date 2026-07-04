using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Uma classe jogável como dado: identidade de atributos, crescimento por nível,
// kit de skills, passivas e (futuramente) promoção. Promover o jogador vira
// "trocar o asset": StatsManager.SetClass + PlayerSkillManager.RebuildLoadout.
[CreateAssetMenu(fileName = "New Class", menuName = "Classes/Class Definition")]
public class ClassDefinitionSO : ScriptableObject
{
    [Header("Identidade")]
    public string className;

    // Id estável para save/load — mesmo esquema de ItemSO.Id.
    [SerializeField, HideInInspector] private string id;
    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
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

    [Tooltip("Qual atributo primário gera Attack Power / Spell Power para esta classe.")]
    public PrimaryAttribute primaryAttribute = PrimaryAttribute.Strength;

    [Header("Atributos - Nível 1")]
    public int baseStrength = 1;
    public int baseAgility = 1;
    public int baseIntelligence = 1;

    [Header("Crescimento por nível")]
    public int strengthPerLevel = 1;
    public int agilityPerLevel = 1;
    public int intelligencePerLevel = 1;

    [Header("Kit")]
    [Tooltip("Skills equipadas na barra, na ordem dos slots/teclas.")]
    public List<Skill> defaultSkills = new();
    public Passive[] passives;

    [Header("Recurso")]
    [Tooltip("Configuração do recurso da classe (ex.: Momentum). Aplicada pelo fluxo de promoção quando ele existir — hoje o ResourceManager da cena ainda é a fonte.")]
    public string resourceName = "Momentum";
    public int maxResource = 6;

    [Header("Promoção (apenas dados — UI e fluxo ainda não existem)")]
    [Tooltip("Nível em que esta classe pode ser promovida. 0 = não promove.")]
    public int promotionLevel;
    public ClassDefinitionSO[] promotesTo;
}
