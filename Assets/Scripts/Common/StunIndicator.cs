using UnityEngine;

// Ícone de status acima da cabeça (placeholder: disco azul sólido) — em vez de
// tingir o sprite do personagem (SpriteRenderer.color é multiplicativo: pixel
// escuro/preto da arte original nunca vira azul, 0 * qualquer cor = 0, daí o
// personagem inteiro "ficava preto"), este marcador usa uma textura BRANCA
// gerada em runtime — mesmo padrão de SelectionCircle (marcador filho, Sorting
// Layer dedicada, overrideSprite pra trocar por arte própria sem mexer em
// código), só que ancorado no topo do bounds em vez dos pés, já preparado pro
// redemoinho que vai substituir o disco.
[RequireComponent(typeof(SpriteRenderer))]
public class StunIndicator : MonoBehaviour
{
    private const string EffectsSortingLayerName = "Character Effects";
    private const float PixelsPerUnit = 100f;
    private const int TextureSize = 64;

    [Tooltip("Sprite customizado pro ícone (ex.: o redemoinho futuro) — se vazio, usa um disco gerado em runtime como placeholder.")]
    [SerializeField] private Sprite overrideSprite;

    [Tooltip("Largura do ícone relativa à largura do bounds do personagem.")]
    [SerializeField] private float widthRatio = 0.35f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer marker;

    private static Sprite generatedDiscSprite;

    private void Awake()
    {
        sourceRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetVisible(bool isVisible, Color color)
    {
        if (sourceRenderer == null)
            sourceRenderer = GetComponent<SpriteRenderer>();

        if (sourceRenderer == null)
            return;

        if (isVisible)
            EnsureBuilt();

        if (marker == null)
            return;

        marker.color = color;
        marker.gameObject.SetActive(isVisible);
    }

    private void EnsureBuilt()
    {
        if (marker != null)
            return;

        Bounds bounds = GetCharacterBounds();

        GameObject go = new("Stun Indicator");
        go.transform.SetParent(transform, false);

        // Centro X + topo do bounds, relativo à posição do personagem — mesmo
        // raciocínio de offsetX do SelectionCircle (pivot fora do centro não
        // pode travar em X=0).
        float offsetX = bounds.center.x - transform.position.x;
        float topY = bounds.max.y - transform.position.y + 0.15f;
        go.transform.localPosition = new Vector3(offsetX, topY, 0f);

        marker = go.AddComponent<SpriteRenderer>();
        marker.sprite = overrideSprite != null ? overrideSprite : GetGeneratedDiscSprite();
        marker.sortingLayerID = SortingLayer.NameToID(EffectsSortingLayerName);
        marker.sortingOrder = 1;

        float desiredWidth = bounds.size.x * widthRatio;
        float spriteWorldWidth = marker.sprite.rect.width / marker.sprite.pixelsPerUnit;
        float scale = spriteWorldWidth > 0f ? desiredWidth / spriteWorldWidth : 1f;
        go.transform.localScale = Vector3.one * scale;

        go.SetActive(false);
    }

    private Bounds GetCharacterBounds()
    {
        Collider2D col = GetComponent<Collider2D>();
        return col != null ? col.bounds : sourceRenderer.bounds;
    }

    // Disco cheio, branco — tingido por cor na hora do uso (SetVisible).
    // Placeholder até existir arte própria (troca via overrideSprite, sem
    // mudar código).
    private static Sprite GetGeneratedDiscSprite()
    {
        if (generatedDiscSprite != null)
            return generatedDiscSprite;

        Texture2D texture = new(TextureSize, TextureSize, TextureFormat.RGBA32, false);
        texture.name = "Stun Indicator Runtime Texture";

        Vector2 center = new(TextureSize / 2f, TextureSize / 2f);
        float radius = TextureSize / 2f - 2f;

        for (int y = 0; y < TextureSize; y++)
        {
            for (int x = 0; x < TextureSize; x++)
            {
                float dx = x + 0.5f - center.x;
                float dy = y + 0.5f - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = Mathf.Clamp01((radius - dist) / 3f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        generatedDiscSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureSize, TextureSize),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        generatedDiscSprite.name = "Stun Indicator Runtime Sprite";
        return generatedDiscSprite;
    }
}
