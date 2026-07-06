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

    [Header("Rarity")]
    public ItemRarity rarity;

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

    public ItemTooltipData GetItemTooltipData()
    {
        List<(string label, string value)> statRows = new();

        foreach (StatModifier modifier in modifiers)
            statRows.Add((StatFormatter.GetStatName(modifier.stat), $"+{modifier.amount}"));

        return new ItemTooltipData
        {
            title = itemName,
            titleColor = GetRarityColor(rarity),
            rarityLabel = rarity.ToString(),
            slotLabel = GetSlotLabel(itemType),
            statRows = statRows,
            description = description,
        };
    }

    private static string GetSlotLabel(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Consumable:
            case ItemType.Material:
            case ItemType.Quest:
                return null;

            case ItemType.MainHand: return "Main Hand";
            case ItemType.OffHand: return "Off Hand";

            default: return itemType.ToString();
        }
    }

    private static Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Uncommon: return new Color32(0x3C, 0xB0, 0x43, 0xFF);
            case ItemRarity.Rare: return new Color32(0x00, 0x70, 0xDD, 0xFF);
            default: return Color.white;
        }
    }

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

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare
    }
}