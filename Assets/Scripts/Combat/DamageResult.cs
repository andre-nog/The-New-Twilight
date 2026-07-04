// Resultado final de um cálculo de dano — o único formato que qualquer
// container de vida (Enemy_Health, PlayerHealth) precisa receber.
public readonly struct DamageResult
{
    public readonly int FinalDamage;
    public readonly bool IsCritical;

    public DamageResult(int finalDamage, bool isCritical)
    {
        FinalDamage = finalDamage;
        IsCritical = isCritical;
    }
}
