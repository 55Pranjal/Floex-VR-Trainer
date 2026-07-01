using UnityEngine;

/// <summary>
/// Global arbiter: at most one pump head may hold "Arterial" at a time.
/// Single source of truth across all canvases. First global-state object in the app —
/// deliberate break from per-canvas isolation, required because arterial is machine-wide.
/// </summary>
public class ArterialRegistry : MonoBehaviour
{
    public static ArterialRegistry Instance { get; private set; }

    // The head currently holding arterial (null = free). Object, so both
    // PumpHeadState and DoublePumpHeadState-lane can register.
    object owner;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsFree(object claimant) => owner == null || owner == claimant;

    /// <summary>Try to take arterial. Returns false if another head owns it.</summary>
    public bool TryClaim(object claimant)
    {
        if (owner != null && owner != claimant) return false;
        owner = claimant;
        return true;
    }

    public void Release(object claimant)
    {
        if (owner == claimant) owner = null;
    }
}