// Contrato comum entre os dois tipos de ataque básico que Enemy_Movement pode
// disparar quando o alvo entra em alcance e o cooldown está pronto: corpo a
// corpo (Enemy_Combat) ou à distância (Enemy_RangedBasicAttack). Enemy_Movement
// chama BeginAttack() sem precisar saber qual dos dois está no prefab — troca de
// comportamento é só trocar qual componente existe, sem tocar em código.
//
// Não confundir com Enemy_Abilities: aquele é o runner de habilidades (com
// cooldown e comportamento próprios, independente do ataque básico), não uma
// implementação deste contrato.
public interface IEnemyBasicAttack
{
    void BeginAttack();

    // Interrompe um ataque em andamento (windup/recovery) sem disparar seu efeito —
    // usado por stun, pra um golpe já enfileirado não acertar depois do inimigo já
    // estar congelado.
    void CancelAttack();
}
