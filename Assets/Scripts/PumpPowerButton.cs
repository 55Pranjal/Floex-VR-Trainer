using UnityEngine;

/// <summary>
/// Power button beside the knob. Poking it toggles the pump head's `powered` state,
/// which shows/hides the console screen (canvas). Screen starts OFF.
///
/// Works for both single and double heads — assign whichever state applies
/// (leave the other null). Place this on the power-button mesh (or the pump head
/// root) — NOT under the canvas it controls, or it would disable itself.
///
/// Wire the button's poke interaction (Meta InteractableUnityEventWrapper or a
/// poke-canvas onClick) to call TogglePower().
///
/// Note: this only powers the SCREEN. Gating the rotor/audio/run-state on `powered`
/// (a powered-off pump shouldn't spin or hum) is a small follow-up, not done here.
/// </summary>
public class PumpPowerButton : MonoBehaviour
{
    [Header("State (assign ONE)")]
    [Tooltip("Single pump head state. Leave null for the double head.")]
    public PumpHeadState singleState;
    [Tooltip("Double pump head state. Leave null for single heads.")]
    public DoublePumpHeadState doubleState;

    [Header("Screen")]
    [Tooltip("The console canvas GameObject to show when powered on, hide when off.")]
    public GameObject screenCanvas;

    void Start()
    {
        Apply();   // start in whatever `powered` is (default false = off)
    }

    /// <summary>Toggle power. Hook the button's poke/select event to this.</summary>
    public void TogglePower()
    {
        SetPowered(!Powered);
    }

    public void SetPowered(bool on)
    {
        if (singleState != null) singleState.powered = on;
        if (doubleState != null) doubleState.powered = on;
        Apply();
    }

    bool Powered =>
        singleState != null ? singleState.powered :
        doubleState != null ? doubleState.powered : true;

    void Apply()
    {
        if (screenCanvas != null) screenCanvas.SetActive(Powered);
    }
}