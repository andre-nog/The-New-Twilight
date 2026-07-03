using UnityEngine;

public class ResourceManager : MonoBehaviour
{
    [Header("Momentum")]
    [SerializeField]
    private int maxMomentum = 6;

    public int CurrentMomentum { get; private set; }

    public int MaxMomentum => maxMomentum;

    public void AddMomentum(int amount)
    {
        CurrentMomentum = Mathf.Clamp(
            CurrentMomentum + amount,
            0,
            maxMomentum);
    }

    public bool HasMomentum(int amount)
    {
        return CurrentMomentum >= amount;
    }

    public bool SpendMomentum(int amount)
    {
        if (!HasMomentum(amount))
            return false;

        CurrentMomentum -= amount;
        return true;
    }

    public int ConsumeAllMomentum()
    {
        int consumed = CurrentMomentum;
        CurrentMomentum = 0;
        return consumed;
    }

    public void ResetMomentum()
    {
        CurrentMomentum = 0;
    }
}