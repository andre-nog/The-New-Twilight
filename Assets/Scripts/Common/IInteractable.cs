using UnityEngine;

// Contrato mínimo pra PlayerInteraction operar sobre qualquer NPC clicável
// (QuestGiver, Shop, futuros) sem conhecer o tipo concreto de cada um.
public interface IInteractable
{
    void OnPlayerArrived();

    // Cursor customizado pro hover deste alvo — null usa o fallback padrão
    // (customCursor do PlayerInteraction ou o orb dourado gerado em runtime).
    Texture2D CursorTexture { get; }
}
