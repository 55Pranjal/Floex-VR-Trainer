using UnityEngine;

/// <summary>
/// State container for a double pump head canvas (slot 4).
/// Tracks two independent cardioplegia pumps (A and B) plus the shared flow ratio.
/// Product A scope: display-only. No physiology, no live behavior.
/// </summary>
public class DoublePumpHeadState : MonoBehaviour
{
    [Header("Pump A")]
    public string pumpA_TubeSize = "1/4";
    public string pumpA_Direction = "CW";

    [Header("Pump B")]
    public string pumpB_TubeSize = "1/4";
    public string pumpB_Direction = "CW";

    [Header("Running State")]
public bool pumpA_Running = false;
public bool pumpB_Running = false;
public bool bypassOn = false;

[Header("Master-Slave Configuration (Screen 2_1)")]
public int masterIndex = 0;     // 0=Nil, 1=Arterial, 2=Cardio, 3=Vent, 4=Suct1, 5=Suct2
public int flowPercent = 0;     // 0..200

[Header("Pulse Mode (Screen 3)")]
public int pulseFrequency = 0;
public int pulseWidth = 0;
public int baseFlow = 0;
public int peakFlowLimit = 0;
public bool pulseModeOn = false;

[Header("Fine Calibration (Screen 4)")]
public int fineCalibration = 0;
public int tube1 = 0;
public int tube2 = 0;
public bool fineCalibrationOn = false;

    [Header("Shared")]
    public string flowRatio = "Nil";

    [Header("Active Screen")]
    public string currentScreen = "Screen_PumpHead04_Screen1";

    [Header("Session Timer")]
    public bool timerRunning = false;
    public float timerSeconds = 0f;

    /// <summary>
    /// Tracks which pump (A or B) the currently-open picker belongs to.
    /// Set by the home-screen button handler before opening Tube_Size or Direction picker.
    /// Read by the picker's APPLY handler to know which pump to update.
    /// </summary>
    public enum ActivePump { None, A, B }
    [HideInInspector] public ActivePump activePicker = ActivePump.None;
}