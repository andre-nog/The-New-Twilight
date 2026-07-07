using TMPro;
using UnityEngine;

// Display de ouro genérico — só escuta GoldManager.OnGoldChanged e escreve no
// TMP_Text atribuído. Usado pelo cabeçalho construído em InventoryCanvasBuilder
// — a ShopWindow não mostra ouro (a loja sempre abre com o inventário ao
// lado, que já cobre isso).
public class GoldDisplayUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    private void OnEnable()
    {
        GoldManager.OnGoldChanged += HandleGoldChanged;

        if (GoldManager.Instance != null)
            HandleGoldChanged(GoldManager.Instance.CurrentGold);
    }

    private void OnDisable()
    {
        GoldManager.OnGoldChanged -= HandleGoldChanged;
    }

    private void HandleGoldChanged(int gold)
    {
        goldText.text = $"Gold: {gold}";
    }
}
