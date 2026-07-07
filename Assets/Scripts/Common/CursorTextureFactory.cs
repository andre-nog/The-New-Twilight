using UnityEngine;

// Textura de cursor gerada em runtime (mesma ideia do GetRuntimeSprite() 1x1 usado
// nos builders de UI, só que um círculo) — placeholder até existir arte própria.
// Compartilhado entre PlayerInteraction (hover de NPC) e PlayerTargeting (hover de
// inimigo) já que os dois precisam da mesma forma, só a cor muda.
public static class CursorTextureFactory
{
    public static Texture2D CreateOrb(Color color, int size = 20)
    {
        Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated Cursor Orb";

        Vector2 center = new(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, dist <= radius ? color : Color.clear);
            }
        }

        texture.Apply();
        return texture;
    }
}
