// Contrato mínimo de "coisa que recebe dano" — jogador, inimigos e futuros
// destrutíveis/chefes. Quem ataca resolve o dano no DamageCalculator e entrega
// o resultado pronto; o alvo só expõe a própria Armor como insumo da fórmula.
public interface IDamageable
{
    float Armor { get; }
    bool IsAlive { get; }
    void TakeDamage(DamageResult result);
}
