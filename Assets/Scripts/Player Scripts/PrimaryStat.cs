using System;

// Separa o crescimento automático (Base, ganho por level up) do bônus vindo de
// fontes externas (Bonus: equipamentos, buffs, passivas). Total é sempre o que
// as fórmulas de RecalculateStats devem consumir.
[Serializable]
public struct PrimaryStat
{
    public int Base;
    public int Bonus;

    public readonly int Total => Base + Bonus;
}
