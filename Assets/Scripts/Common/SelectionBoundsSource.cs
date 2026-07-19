using UnityEngine;

// Aponta o SelectionCircle pro bounds "de verdade" do personagem quando o
// Collider2D principal é um gatilho de gameplay (raio de interação de NPC,
// por exemplo) em vez da silhueta visual — evita ter que encolher esse
// gatilho só pra alinhar o círculo. Fica num GameObject filho, com seu
// próprio Collider2D marcado como trigger (nunca bloqueia física).
[RequireComponent(typeof(Collider2D))]
public class SelectionBoundsSource : MonoBehaviour
{
    public Bounds Bounds => GetComponent<Collider2D>().bounds;
}
