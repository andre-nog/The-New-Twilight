using System.Linq;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Aplica automaticamente as configurações de import corretas (Sprite Mode
// Multiple, PPU 40, filtro Point, grade 64x64) a qualquer PNG solto sob
// Assets/Sprites/Enemies/ — elimina o passo manual de configurar isso no
// Inspector por spritesheet novo. Valores extraídos de GoblinRun.png.meta,
// não inventados.
//
// Só cobre sprites NOVOS de inimigos daqui pra frente — não retroage sobre
// sprites já existentes fora dessa pasta (ex.: Assets/Sprites/Goblinattack.png,
// que já foi importado à mão antes desta convenção existir). Novos inimigos
// devem usar Assets/Sprites/Enemies/<Nome>/<Melee ou Ranged>/... daqui em diante.
//
// Usa ISpriteEditorDataProvider (pacote com.unity.2d.sprite) em vez de
// TextureImporter.spritesheet — o campo antigo foi removido do scripting API
// nesta versão do Unity, mesmo a serialização em disco ainda existindo.
public class EnemySpriteImportProcessor : AssetPostprocessor
{
    private const string EnemySpriteRoot = "Assets/Sprites/Enemies/";
    private const int CellWidth = 64;
    private const int CellHeight = 64;
    private const int PixelsPerUnit = 40;

    private bool IsEnemySprite => assetPath.Replace('\\', '/').StartsWith(EnemySpriteRoot);

    private void OnPreprocessTexture()
    {
        if (!IsEnemySprite)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
    }

    private void OnPostprocessTexture(Texture2D texture)
    {
        if (!IsEnemySprite)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        SpriteDataProviderFactories factory = new();
        factory.Init();
        ISpriteEditorDataProvider dataProvider = factory.GetSpriteEditorDataProviderFromObject(importer);
        dataProvider.InitSpriteEditorDataProvider();

        // Reimport de um sprite já fatiado (à mão ou por esta mesma classe antes) —
        // nunca sobrescrever slices que alguém pode ter ajustado manualmente depois.
        SpriteRect[] existingRects = dataProvider.GetSpriteRects();
        if (existingRects != null && existingRects.Length > 0)
            return;

        SpriteRect[] slices = BuildGridSlices(texture.width, texture.height);
        dataProvider.SetSpriteRects(slices);

        // Sem um spriteID/nameFileId estável, o Editor não consegue reconciliar os
        // rects recém-criados com referências existentes (Animator, SpriteRenderer)
        // num reimport futuro.
        ISpriteNameFileIdDataProvider nameFileIdProvider = dataProvider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var nameFileIdPairs = slices
            .Select(rect => new SpriteNameFileIdPair(rect.name, rect.spriteID))
            .ToList();
        nameFileIdProvider.SetNameFileIdPairs(nameFileIdPairs);

        dataProvider.Apply();
    }

    private static SpriteRect[] BuildGridSlices(int textureWidth, int textureHeight)
    {
        int columns = Mathf.Max(1, textureWidth / CellWidth);
        int rows = Mathf.Max(1, textureHeight / CellHeight);
        SpriteRect[] slices = new SpriteRect[columns * rows];
        int index = 0;

        // Unity fatia de cima pra baixo visualmente, mas a origem do Rect é o
        // canto inferior esquerdo da textura — por isso a linha calcula a partir
        // do topo (textureHeight - (row + 1) * CellHeight).
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                slices[index] = new SpriteRect
                {
                    name = $"frame_{index}",
                    rect = new Rect(
                        column * CellWidth,
                        textureHeight - (row + 1) * CellHeight,
                        CellWidth,
                        CellHeight),
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };
                index++;
            }
        }

        return slices;
    }
}
