// Contrato de status effect "stun" — quem pode ser atordoado implementa isto,
// sem acoplar a skill que causa o stun ao tipo concreto (Enemy_Movement).
public interface IStunnable
{
    void ApplyStun(float duration);
}
