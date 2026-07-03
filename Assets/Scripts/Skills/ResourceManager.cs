using System;
using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Momentum")]
    [SerializeField]
    private int maxMomentum = 6;

    public int CurrentMomentum { get; private set; }

    public int MaxMomentum => maxMomentum;
    public event Action OnMomentumChanged;

    public void AddMomentum(int amount)
    {
        SetMomentum(CurrentMomentum + amount);
    }

    public bool HasMomentum(int amount)
    {
        return CurrentMomentum >= amount;
    }

    public bool SpendMomentum(int amount)
    {
        if (!HasMomentum(amount))
            return false;

        SetMomentum(CurrentMomentum - amount);
        return true;
    }

    public int ConsumeAllMomentum()
    {
        int consumed = CurrentMomentum;
        SetMomentum(0);
        return consumed;
    }

    public void ResetMomentum()
    {
        SetMomentum(0);
    }

    private void SetMomentum(int value)
    {
        int newValue = Mathf.Clamp(value, 0, maxMomentum);

        if (newValue == CurrentMomentum)
            return;

        CurrentMomentum = newValue;
        OnMomentumChanged?.Invoke();
    }
}
