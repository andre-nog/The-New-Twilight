using UnityEngine;

// Contraparte do NPCInteractable pra lojas — mesmo pipeline de hover/círculo/
// walk-to do PlayerInteraction (via IInteractable), só que ao chegar abre a
// ShopWindow em vez da QuestWindow. Estoque infinito, então sem indicador de
// estado dinâmico acima da cabeça (só um sprite placeholder fixo, configurado
// direto num child GameObject na cena).
public class ShopInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private ShopSO shop;

    [Tooltip("Cursor customizado (bola) pro hover desta loja — se vazio, usa um placeholder gerado em runtime.")]
    [SerializeField] private Texture2D cursorTexture;

    private static Texture2D generatedFallback;
    private static readonly Color FallbackColor = new(0.7f, 0.5f, 0.25f, 1f);

    public Texture2D CursorTexture
    {
        get
        {
            if (cursorTexture != null)
                return cursorTexture;

            if (generatedFallback == null)
                generatedFallback = CursorTextureFactory.CreateOrb(FallbackColor);

            return generatedFallback;
        }
    }

    public void OnPlayerArrived()
    {
        if (ShopWindow.Instance == null || shop == null)
            return;

        ShopWindow.Instance.Open(shop);
    }
}
