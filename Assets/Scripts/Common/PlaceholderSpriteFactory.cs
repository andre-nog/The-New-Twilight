using UnityEngine;

// Sprite branco circular (borda suave de ~2px) gerado em runtime, tingido via
// SpriteRenderer.color — placeholder compartilhado até existir arte própria.
// Extraído de TelegraphedAreaAbility (telegraph/projétil da habilidade) pra
// também ser reusado por EnemyProjectile (ataque básico à distância) sem
// duplicar a rasterização pixel a pixel.
public static class PlaceholderSpriteFactory
{
    private static Sprite circleSprite;

    public static Sprite GetCircleSprite(int resolution = 64)
    {
        if (circleSprite != null)
            return circleSprite;

        Texture2D texture = new(resolution, resolution, TextureFormat.RGBA32, false);
        Vector2 center = new(resolution / 2f, resolution / 2f);
        float radius = resolution / 2f;

        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);

                // Borda suave de ~2px pra não ficar serrilhado.
                float alpha = Mathf.Clamp01((radius - distance) / 2f);

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();

        circleSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, resolution, resolution),
            new Vector2(0.5f, 0.5f),
            resolution,
            0,
            SpriteMeshType.FullRect);

        circleSprite.name = "Generated Circle Sprite";

        return circleSprite;
    }
}
