// Contrato mínimo que o InventoryDragController precisa para operar de forma
// genérica sobre qualquer tipo de slot (inventário, equipamento, e futuros
// containers como baú/vendedor), sem conhecer o tipo concreto de cada um.
public interface IItemSlot
{
    ItemSO Item { get; }
    bool IsEmpty { get; }
    bool CanAccept(ItemSO item);
}
