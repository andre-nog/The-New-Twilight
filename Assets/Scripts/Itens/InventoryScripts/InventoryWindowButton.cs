using UnityEngine;
using UnityEngine.EventSystems;

// Mesmo padrão de clique direto via IPointerClickHandler usado em
// QuestWindowButton — sem UnityEngine.UI.Button. Fica no canto superior
// direito da Inventory Window, que é o canto direito de todo o par quando a
// loja está aberta (loja à esquerda, inventário à direita) — por isso este é
// o único "X" visível nesse modo (a loja não tem mais o seu próprio).
public class InventoryWindowButton : MonoBehaviour, IPointerClickHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        if (InventoryManager.Instance == null)
            return;

        if (InventoryManager.Instance.IsShopMode && ShopWindow.Instance != null)
            ShopWindow.Instance.Close();
        else
            InventoryManager.Instance.CloseInventory();
    }
}
