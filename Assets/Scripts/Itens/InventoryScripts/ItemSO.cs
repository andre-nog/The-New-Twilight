using System.Collections.Generic;
using UnityEngine;

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