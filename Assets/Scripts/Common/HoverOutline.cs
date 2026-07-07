using UnityEngine;

// Contorno barato pra sprite 2D sem shader customizado: um anel de 8 cópias do
// mesmo sprite, tintadas e deslocadas alguns pixels, renderizadas atrás do
// original — ao contrário de escalar o sprite (fica "esticado"/glitch), o
// contorno preserva o tamanho real do personagem. Adicionado dinamicamente em
// runtime pelo hover (PlayerInteraction/PlayerTargeting), não precisa existir
// no prefab/cena.
public class HoverOutline : MonoBehaviour
{
    private const float PixelThickness = 0.045f;
    private const float Diagonal = 0.7071f;

    private static readonly Vector2[] Offsets =
    {
        new(1f, 0f), new(-1f, 0f), new(0f, 1f), new(0f, -1f),
        new(Diagonal, Diagonal), new(Diagonal, -Diagonal), new(-Diagonal, Diagonal), new(-Diagonal, -Diagonal)
    };

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer[] outlineRenderers;
    private bool visible;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        if (!visible || outlineRenderers == null || sourceRenderer == null)
            return;

        // Acompanha o frame de animação atual do sprite original — sem isso o
        // contorno ficaria travado no frame de quando o hover começou.
        foreach (SpriteRenderer sr in outlineRenderers)
            sr.sprite = sourceRenderer.sprite;
    }

    public void SetVisible(bool isVisible, Color color)
    {
        if (sourceRenderer == null)
            sourceRenderer = GetComponent<SpriteRenderer>();

        if (sourceRenderer == null)
            return;

        visible = isVisible;

        if (isVisible)
            EnsureBuilt();

        if (outlineRenderers == null)
            return;

        foreach (SpriteRenderer sr in outlineRenderers)
        {
            sr.color = color;
            sr.gameObject.SetActive(isVisible);
        }
    }

    private void EnsureBuilt()
    {
        if (outlineRenderers != null)
            return;

        outlineRenderers = new SpriteRenderer[Offsets.Length];

        for (int i = 0; i < Offsets.Length; i++)
        {
            GameObject go = new($"Outline {i}");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(Offsets[i].x, Offsets[i].y, 0f) * PixelThickness;
            go.transform.localScale = Vector3.one;

            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sourceRenderer.sprite;
            sr.sortingLayerID = sourceRenderer.sortingLayerID;
            sr.sortingOrder = sourceRenderer.sortingOrder - 1;
            sr.gameObject.SetActive(false);

            outlineRenderers[i] = sr;
        }
    }
}
