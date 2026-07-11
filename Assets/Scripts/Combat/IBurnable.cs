// Contrato de status effect "queimadura" (dano ao longo do tempo) — quem pode
// ser queimado implementa isto, sem acoplar quem causa o burn ao tipo concreto
// (PlayerHealth). Mesmo formato de IStunnable.
public interface IBurnable
{
    void ApplyBurn(float tickDamage, float tickInterval, float duration);
}
