using UnityEngine;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor;
#endif

public abstract class Skill : ScriptableObject
{
    [Header("Info")]
    public string skillName;
    public SkillType skillType;
    public Sprite icon;

    // Id estável — hoje o loadout vem de ClassDefinitionSO.defaultSkills (referência
    // direta), então nada ainda consome isto. Preparado para o dia em que o save
    // precisar referenciar skills por id (loadout customizado, especializações).
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

    [Header("Gameplay")]
    public bool requiresTarget = true;
    public bool lockMovementDuringCast = false;
    public float cooldown;
    public float range;
    public float damageMultiplier = 1f;
    public DamageSchool damageSchool = DamageSchool.Physical;

    [Tooltip("Se marcado, o cooldown dessa skill é acelerado por Haste: tempoFinal = tempoBase / (1 + Haste). Hoje só faz sentido pro Auto Attack.")]
    public bool affectedByHaste = false;

    [Header("Resource")]
    [FormerlySerializedAs("momentumGenerated")]
    public int resourceGenerated;
    [FormerlySerializedAs("momentumCost")]
    public int resourceCost;

    [Header("Mana")]
    [Tooltip("Custo em Mana (StatsManager), separado do Resource genérico acima (Momentum). 0 = não gasta Mana.")]
    public int manaCost;

    [Header("Visual Effects")]
    public GameObject hitVFX;
    public Vector3 hitVFXOffset;

    [Header("Animation")]
    public string animationTrigger;

    public virtual void Cast(Player_Combat combat)
    {
        combat.UseSkill(this);
    }

    // ctx carrega skill/alvo/multiplicador capturados no início do cast — efeitos
    // derivam variações via ctx.WithExtraMultiplier em vez de mutar estado do combat.
    public abstract void ExecuteEffect(Player_Combat combat, in CastContext ctx);
}