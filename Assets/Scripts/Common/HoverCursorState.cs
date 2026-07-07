// Coordena qual sistema de hover (NPC via PlayerInteraction, inimigo via
// PlayerTargeting) "possui" o cursor customizado no momento. Sem isso, o
// hover-check de um sistema podia resetar o cursor pro padrão no mesmo frame em
// que o outro tinha acabado de setar um cursor customizado (já que os dois rodam
// em Update() todo frame, independentes um do outro).
public static class HoverCursorState
{
    public static object CurrentOwner;
}
