using System;
using UnityEngine;
using UnityEngine.Serialization;

public class ResourceManager : MonoBehaviour
{
    [Header("Resource")]
    public string resourceName = "Momentum";

    [FormerlySerializedAs("maxMomentum")]
    [SerializeField]
    private int maxResource = 6;

    public int CurrentResource { get; private set; }

    public int MaxResource => maxResource;
    public event Action OnResourceChanged;

    public void AddResource(int amount)
    {
        SetResource(CurrentResource + amount);
    }

    public bool HasResource(int amount)
    {
        return CurrentResource >= amount;
    }

    public bool SpendResource(int amount)
    {
        if (!HasResource(amount))
            return false;

        SetResource(CurrentResource - amount);
        return true;
    }

    public int ConsumeAllResource()
    {
        int consumed = CurrentResource;
        SetResource(0);
        return consumed;
    }

    public void ResetResource()
    {
        SetResource(0);
    }

    private void SetResource(int value)
    {
        int newValue = Mathf.Clamp(value, 0, maxResource);

        if (newValue == CurrentResource)
            return;

        CurrentResource = newValue;
        OnResourceChanged?.Invoke();
    }
}
