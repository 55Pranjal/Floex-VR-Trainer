using UnityEngine;

/// <summary>
/// Pure data layer for the Main pump screen.
/// Holds the values the UI displays. NOTHING here computes physiology —
/// values are set externally (by input, or later by a hypothetical Product B engine)
/// and the display layer just reads them. This is the decoupling seam.
/// </summary>
public class PumpState : MonoBehaviour
{
    [Header("Displayed Values")]
    public float flowRate = 4.2f;   // L/PM
    public int rpm = 100;           // revolutions per minute
    public bool isRunning = false;  // START/STOP state

    [Header("Footer Values")]
    public string tubeSize = "1/4\"";
    public string direction = "CW";
    public float cardiacIndex = 2.2f;

    // --- Input layer calls these. They ONLY change state, never touch UI. ---

    public void StartPump()
    {
        isRunning = true;
    }

    public void StopPump()
    {
        isRunning = false;
    }
}