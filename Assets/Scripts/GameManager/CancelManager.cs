using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CancelManager : MonoBehaviour
{
    public static CancelManager Instance;

    [SerializeField] private InputActionReference cancelAction;

    private readonly List<ICancelable> cancelables = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance != this)
            return;

        cancelAction.action.Enable();
    }

    private void OnDisable()
    {
        if (Instance != this)
            return;

        cancelAction.action.Disable();
    }

    private void Update()
    {
        if (!cancelAction.action.WasPressedThisFrame())
            return;

        ExecuteCancel();
    }

    public void Register(ICancelable cancelable)
    {
        if (!cancelables.Contains(cancelable))
            cancelables.Add(cancelable);

        cancelables.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    public void Unregister(ICancelable cancelable)
    {
        cancelables.Remove(cancelable);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    private void ExecuteCancel()
    {
        foreach (ICancelable cancelable in cancelables)
        {
            if (!cancelable.CanCancel())
                continue;

            cancelable.Cancel();
            return;
        }
    }
}
