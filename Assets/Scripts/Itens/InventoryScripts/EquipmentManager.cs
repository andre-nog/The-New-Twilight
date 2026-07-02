using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Equipment Slots")]
    [SerializeField] private EquippedSlot[] equippedSlots;

    private void Awake()
    {
        Instance = this;
    }

    public ItemSO Equip(ItemSO newItem)
    {
        foreach (EquippedSlot slot in equippedSlots)
        {
            if (!slot.CanEquip(newItem))
                continue;

            // Slot vazio
            if (slot.IsEmpty())
            {
                slot.Equip(newItem);

                foreach (StatModifier modifier in newItem.modifiers)
                {
                    StatsManager.Instance.AddModifier(modifier);
                }

                return null;
            }

            // Item atualmente equipado
            ItemSO oldItem = slot.GetItem();

            // Remove os bônus do item antigo
            foreach (StatModifier modifier in oldItem.modifiers)
            {
                StatsManager.Instance.RemoveModifier(modifier);
            }

            // Equipa o novo item
            slot.Equip(newItem);

            // Aplica os bônus do novo item
            foreach (StatModifier modifier in newItem.modifiers)
            {
                StatsManager.Instance.AddModifier(modifier);
            }

            return oldItem;
        }

        // Não encontrou slot compatível
        return newItem;
    }

    public void Unequip(EquippedSlot slot)
    {
        // Não há item equipado
        if (slot.IsEmpty())
            return;

        ItemSO item = slot.GetItem();

        // Tenta adicionar ao inventário
        int leftOver = InventoryManager.Instance.AddItem(item, 1);

        // Inventário cheio
        if (leftOver > 0)
        {
            Debug.Log("Inventário cheio.");
            return;
        }

        // Remove todos os bônus do equipamento
        foreach (StatModifier modifier in item.modifiers)
        {
            StatsManager.Instance.RemoveModifier(modifier);
        }

        // Remove o item do slot equipado
        slot.Unequip();
    }
}