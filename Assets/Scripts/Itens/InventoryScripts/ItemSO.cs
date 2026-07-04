using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName;
    public Sprite itemSprite;

    [TextArea]
    public string description;

    [Header("Modifiers")]
    public List<StatModifier> modifiers = new();

    [Header("Behaviour")]
    public ItemType itemType;
    public bool stackable = true;

    // Id estável para save/load — o GUID do próprio asset (não muda ao renomear ou
    // mover o arquivo). Nunca editar à mão; preenchido automaticamente no Editor.
    [SerializeField, HideInInspector] private string id;
    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureId();
    }

    // Chamado também por ItemDatabaseSO ao coletar itens do projeto — cobre o caso
    // de um asset em disco cujo OnValidate ainda não rodou nesta sessão do Editor.
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

    public void UseItem()
    {
        foreach (StatModifier modifier in modifiers)
        {
            switch (modifier.stat)
            {
                case StatType.Health:
                    StatsManager.Instance.ChangeHealth(modifier.amount);
                    break;
            }
        }
    }

    public enum ItemType
    {
        Consumable,
        Material,
        Quest,

        Head,
        Body,
        Legs,
        Feet,

        MainHand,
        OffHand,

        Necklace,
        Ring
    }
}