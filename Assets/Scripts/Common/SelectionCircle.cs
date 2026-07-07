using UnityEngine;

// Círculo de seleção no chão (estilo MMORPG clássico) em vez de tingir o sprite
// inteiro — genérico, qualquer GameObject com SpriteRenderer usa (inimigo,
// NPC, futuro tipo), cor e sprite passados por fora (PlayerTargeting usa
// vermelho pra inimigo, PlayerInteraction usa branco pra NPC).
//
// Fica na mesma Sorting Layer dedicada do HoverOutline ("Character Effects",
// antes de "Default" no Project Settings) — sempre atrás dos personagens,
// garantido estruturalmente por Sorting Layer, não por sortingOrder calculado
// todo frame (mesmo raciocínio do fix do HoverOutline).
[RequireComponent(typeof(SpriteRenderer))]
public class SelectionCircle : MonoBehaviour
{
    private const string EffectsSortingLayerName = "Character Effects";
    private const float PixelsPerUnit = 100f;
    private const int TextureWidth = 128;
    private const int TextureHeight = 64;

    [Tooltip("Sprite customizado pro círculo — se vazio, usa um anel achatado gerado em runtime como placeholder.")]
    [SerializeField] private Sprite overrideSprite;

    [Tooltip("Largura do círculo relativa à largura do sprite do personagem.")]
    [SerializeField] private float widthRatio = 0.9f;

    private SpriteRenderer sourceRenderer;
    private SpriteRenderer marker;

    private static Sprite generatedRingSprite;

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

        // Collider2D.bounds em vez de SpriteRenderer.bounds — sprite sheet de
        // inimigo tem padding transparente pra caber frames de ataque, então o
        // bounds do sprite fica bem maior/deslocado da silhueta real. Collider é
        // ajustado à mão pro tamanho de verdade do personagem (mesmo raciocínio
        // já usado em PlayerTargeting.SelectNextEnemy). Cai pro sprite só se não
        // tiver Collider2D.
        Bounds bounds = GetCharacterBounds();

        GameObject go = new("Selection Circle");
        go.transform.SetParent(transform, false);

        // Centro X + base Y do bounds, relativo à posição do personagem — X não
        // pode ser hardcoded em 0: sprite com pivot fora do centro (ex.: QuestGiver,
        // pivot embaixo-esquerda) deixa bounds.center.x diferente de
        // transform.position.x, e travar em 0 desalinhava o círculo horizontalmente.
        float offsetX = bounds.center.x - transform.position.x;
        float feetY = bounds.min.y - transform.position.y;
        go.transform.localPosition = new Vector3(offsetX, feetY, 0f);

        marker = go.AddComponent<SpriteRenderer>();
        marker.sprite = overrideSprite != null ? overrideSprite : GetGeneratedRingSprite();
        marker.sortingLayerID = SortingLayer.NameToID(EffectsSortingLayerName);
        marker.sortingOrder = 0;

        // Tamanho do círculo acompanha a largura do bounds — funciona pra
        // qualquer tamanho de personagem sem precisar ajustar à mão.
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

    // Anel achatado (elipse) com borda suave, branco — tingido por cor na hora
    // do uso (SetVisible). Placeholder até existir arte própria (troca via
    // overrideSprite no Inspector, sem mudar código).
    private static Sprite GetGeneratedRingSprite()
    {
        if (generatedRingSprite != null)
            return generatedRingSprite;

        Texture2D texture = new(TextureWidth, TextureHeight, TextureFormat.RGBA32, false);
        texture.name = "Selection Circle Runtime Texture";

        Vector2 center = new(TextureWidth / 2f, TextureHeight / 2f);
        float outerRadius = TextureWidth / 2f - 2f;
        float innerRadius = outerRadius * 0.65f;
        float squash = (float)TextureHeight / TextureWidth;

        for (int y = 0; y < TextureHeight; y++)
        {
            for (int x = 0; x < TextureWidth; x++)
            {
                float dx = x + 0.5f - center.x;
                float dy = (y + 0.5f - center.y) / squash;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha;

                if (dist > outerRadius || dist < innerRadius)
                {
                    alpha = 0f;
                }
                else
                {
                    float edgeDistance = Mathf.Min(dist - innerRadius, outerRadius - dist);
                    alpha = Mathf.Clamp01(edgeDistance / 3f);
                }

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        generatedRingSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, TextureWidth, TextureHeight),
            new Vector2(0.5f, 0.5f),
            PixelsPerUnit);
        generatedRingSprite.name = "Selection Circle Runtime Sprite";
        return generatedRingSprite;
    }
}
