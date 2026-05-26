using UnityEngine;
using TMPro;

/// <summary>
/// Display layer for the Main pump screen.
/// READS from PumpState and pushes values into the TMP text fields.
/// It never changes state — it only reflects it. One-way: state -> display.
/// </summary>
public class MainScreenDisplay : MonoBehaviour
{
    [Header("State Source")]
    public PumpState state;

    [Header("Readout Text Fields")]
    public TMP_Text flowText;   // the "4.2"
    public TMP_Text rpmText;    // the "100"

    [Header("Footer Text Fields")]
    public TMP_Text tubeSizeText;   // "1/4\""
    public TMP_Text directionText;  // "CW"
    public TMP_Text ciText;         // "CI 2.2"

    [Header("Status Indicator (optional)")]
    public TMP_Text statusText;     // shows RUNNING / STOPPED, or leave empty

    void Update()
    {
        if (state == null) return;

        // Read state, format, push to display. No logic beyond formatting.
        flowText.text = state.flowRate.ToString("0.0");
        rpmText.text  = state.rpm.ToString();

        tubeSizeText.text  = state.tubeSize;
        directionText.text = state.direction;
        ciText.text        = "CI " + state.cardiacIndex.ToString("0.0");

        if (statusText != null)
{
    statusText.text = state.isRunning ? "RUNNING" : "STOPPED";
    statusText.color = state.isRunning ? Color.green : Color.gray;
}
    }
}