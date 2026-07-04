using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Equipment Slots")]
    [SerializeField] private EquippedSlot[] equippedSlots;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void AddModifiers(ItemSO item)
    {
        foreach (StatModifier modifier in item.modifiers)
        {
            StatsManager.Instance.AddModifier(modifier);
        }
    }

    private void RemoveModifiers(ItemSO item)
    {
        foreach (StatModifier modifier in item.modifiers)
        {
            StatsManager.Instance.RemoveModifier(modifier);
        }
    }

    public ItemSO Equip(ItemSO newItem)
    {
        foreach (EquippedSlot slot in equippedSlots)
        {
            if (!slot.CanEquip(newItem))
                continue;

            return EquipAt(slot, newItem);
        }

        // Não encontrou slot compatível
        return newItem;
    }

    // Equipa em um slot ESPECÍFICO (sem varrer os demais) — usado pelo drag-and-drop,
    // onde o jogador soltou o item em cima de um slot exato (ex: o 2º de dois anéis).
    public ItemSO EquipAt(EquippedSlot slot, ItemSO newItem)
    {
        if (!slot.CanEquip(newItem))
            return newItem; // sentinela: nenhuma mudança feita

        if (slot.IsEmpty())
        {
            slot.Equip(newItem);
            AddModifiers(newItem);
            return null;
        }

        ItemSO oldItem = slot.GetItem();

        RemoveModifiers(oldItem);
        slot.Equip(newItem);
        AddModifiers(newItem);

        return oldItem;
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

        RemoveModifiers(item);

        // Remove o item do slot equipado
        slot.Unequip();
    }

    // Troca dois slots de equipamento entre si (ex: dois anéis, ou Ring<->Necklace).
    // Valida compatibilidade cruzada antes de mutar qualquer coisa: se algum lado não
    // aceitar o item do outro, cancela tudo e nada muda.
    public bool SwapEquipped(EquippedSlot slotA, EquippedSlot slotB)
    {
        if (slotA == slotB)
            return false;

        ItemSO itemA = slotA.GetItem();
        ItemSO itemB = slotB.GetItem();

        if (itemA == null && itemB == null)
            return false;

        if (itemA != null && !slotB.CanEquip(itemA))
            return false;

        if (itemB != null && !slotA.CanEquip(itemB))
            return false;

        if (itemA != null)
            RemoveModifiers(itemA);

        if (itemB != null)
            RemoveModifiers(itemB);

        if (itemB != null)
            slotA.Equip(itemB);
        else
            slotA.Unequip();

        if (itemA != null)
            slotB.Equip(itemA);
        else
            slotB.Unequip();

        if (itemB != null)
            AddModifiers(itemB);

        if (itemA != null)
            AddModifiers(itemA);

        return true;
    }

    // Desequipa um slot especificamente para um slot de inventário indicado (não o
    // AddItem genérico do Unequip()). Se o destino já tiver um item incompatível com
    // este slot de equipamento, cancela a operação inteira (nada muda).
    public bool UnequipTo(EquippedSlot slot, ItemSlot inventoryTarget)
    {
        if (slot.IsEmpty())
            return false;

        ItemSO equippedItem = slot.GetItem();

        if (inventoryTarget.item == null)
        {
            RemoveModifiers(equippedItem);
            slot.Unequip();
            inventoryTarget.LoadItem(equippedItem, 1);
            return true;
        }

        if (!slot.CanEquip(inventoryTarget.item))
            return false;

        ItemSO otherItem = inventoryTarget.item;

        RemoveModifiers(equippedItem);
        slot.Equip(otherItem);
        AddModifiers(otherItem);

        inventoryTarget.LoadItem(equippedItem, 1);

        return true;
    }

    public int IndexOf(EquippedSlot slot)
    {
        return System.Array.IndexOf(equippedSlots, slot);
    }

    public List<EquippedSave> GetState()
    {
        List<EquippedSave> result = new();

        for (int i = 0; i < equippedSlots.Length; i++)
        {
            ItemSO item = equippedSlots[i].GetItem();

            if (item != null)
                result.Add(new EquippedSave { slotIndex = i, itemId = item.Id });
        }

        return result;
    }

    // Remove todos os itens equipados e os modifiers correspondentes sem devolver
    // nada ao inventário — usado antes de aplicar um save, cujo inventário já vai
    // ser reconstruído do zero em seguida (ver InventoryManager.ApplyState).
    public void UnequipAllSilent()
    {
        foreach (EquippedSlot slot in equippedSlots)
        {
            if (slot.IsEmpty())
                continue;

            foreach (StatModifier modifier in slot.GetItem().modifiers)
                StatsManager.Instance.RemoveModifier(modifier);

            slot.Unequip();
        }
    }

    // Reconstrói o equipamento do zero a partir do save, reaplicando pelo Equip()
    // normal — assim os modifiers fluem pelo StatsManager como em qualquer equipar.
    public void ApplyState(List<EquippedSave> state, ItemDatabaseSO database)
    {
        UnequipAllSilent();

        if (state == null || database == null)
            return;

        foreach (EquippedSave saved in state)
        {
            ItemSO item = database.GetById(saved.itemId);

            if (item != null)
                Equip(item);
        }
    }
}
