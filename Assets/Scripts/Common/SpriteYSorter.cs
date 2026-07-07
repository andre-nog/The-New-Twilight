using UnityEngine;

// Ordena sprite por posição Y — quanto menor o Y (mais embaixo na tela), maior
// o sortingOrder (mais na frente). Genérico: qualquer GameObject com
// SpriteRenderer usa o mesmo componente (Player, NPC, inimigo, futuro
// personagem), sem lógica hardcoded por tipo.
//
// Sem tiebreak arbitrário (versão antiga usava um contador estático global por
// ordem de spawn) — esse contador não tinha relação com Y e podia vencer
// diferenças reais de Y pequenas, causando troca de ordem aparentemente
// aleatória. PrecisionFactor alto o bastante garante que qualquer diferença
// real de posição sempre decide.
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteYSorter : MonoBehaviour
{
    private const float PrecisionFactor = 1000f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * PrecisionFactor);
    }
}
