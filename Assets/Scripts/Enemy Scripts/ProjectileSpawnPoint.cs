using UnityEngine;

// Marcador puro — sem lógica própria. Um child Transform com este componente
// vira o ponto de disparo do ataque básico à distância: arraste-o na Scene view
// pra reposicionar (ex.: na ponta da besta) sem tocar em código. Um pequeno
// gizmo desenha a posição mesmo sem o objeto selecionado, pra ser fácil de achar
// no meio da hierarquia do inimigo.
//
// Enemy_RangedBasicAttack usa este componente pra auto-resolver seu campo
// "muzzle" via GetComponentInChildren quando ele não foi arrastado à mão no
// Inspector — mesmo padrão de auto-descoberta já usado pra barra de vida
// (ver Enemy_Movement/Enemy_Health).
public class ProjectileSpawnPoint : MonoBehaviour
{
    private const float GizmoRadius = 0.06f;
    private static readonly Color GizmoColor = new(1f, 0.65f, 0.15f, 0.9f);

    private void OnDrawGizmos()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawSphere(transform.position, GizmoRadius);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = GizmoColor;
        Gizmos.DrawWireSphere(transform.position, GizmoRadius * 2f);
    }
}
