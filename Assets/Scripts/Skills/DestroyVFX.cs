using UnityEngine;

// Self-destructs once its VFX animation finishes, reading the duration straight
// off the Animator instead of relying on a hand-placed "DestroySelf" Animation
// Event in the clip — the same event a new clip (e.g. autoattack) can easily
// ship without, leaving the last frame frozen on screen forever.
public class DestroyVFX : MonoBehaviour
{
    private const float FallbackLifetime = 1f;

    [Tooltip("Force a specific lifetime in seconds. 0 = auto-detect from the Animator's current clip length.")]
    [SerializeField] private float lifetimeOverride = 0f;

    private void Start()
    {
        Destroy(gameObject, lifetimeOverride > 0f ? lifetimeOverride : GetClipLength());
    }

    private float GetClipLength()
    {
        var animator = GetComponent<Animator>();
        var clips = animator != null ? animator.runtimeAnimatorController?.animationClips : null;
        if (clips != null && clips.Length > 0)
            return clips[0].length;

        Debug.LogWarning($"DestroyVFX on '{name}' found no Animator clip to time off of — falling back to {FallbackLifetime}s.", this);
        return FallbackLifetime;
    }

    // Kept for compatibility with anything still calling it directly; a Destroy
    // on an already-destroyed object is a safe no-op.
    public void DestroySelf()
    {
        Destroy(gameObject);
    }
}