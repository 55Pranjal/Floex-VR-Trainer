using UnityEngine;

/// <summary>
/// Per-pump-head state. One instance per pump head (mirrors firmware Model).
/// PRODUCT A: data store + label refresh only. NO live values, NO physics.
/// </summary>
public class PumpHeadState : MonoBehaviour
{
    [Header("Pump picker (Pump_Select_1)")]
    public int pumpIndex = 0;       // 0=Arterial, 1=Cardio, 2=Vent, 3=Suct1, 4=Suct2

    [Header("Tube picker (Tube_Size_1)")]
    public int tubeIndex = 0;       // 0=1/4, 1=3/8, 2=1/2, 3=5/16, 4=F1, 5=F2

    [Header("Direction picker (Direction)")]
    public bool directionForward = true;

    [Header("Master-Slave config (Screen2_1)")]
    public int masterIndex = 0;     // 0=Nil, 1=Arterial, 2=Cardio, 3=Vent, 4=Suct1, 5=Suct2
    public int flowPercent = 0;     // 0..200

    [Header("Pulse Mode config (Screen3)")]
    public int pulseFrequency = 0;  // 0..200
    public int pulseWidth = 0;      // 0..200
    public int baseFlow = 0;        // 0..200
    public int peakFlowLimit = 0;   // 0..200
    public bool pulseModeOn = false;

    [Header("Fine Calibration config (Screen4)")]
    public int fineCalibration = 0; // 0..200
    public int tube1 = 0;           // 0..200
    public int tube2 = 0;           // 0..200
    public bool fineCalibrationOn = false;

    [Header("Running state (Screen1 START/STOP)")]
    public bool running = false;

    [Header("Session timer (Screen1 play/history)")]
    public bool timerRunning = false;
    public float timerSeconds = 0f;

    [Header("Bypass toggle (Screen1 footer)")]
    public bool bypassOn = false;

    static readonly string[] PumpNames = { "Arterial", "Cardio", "Vent", "Suct 1", "Suct 2" };
    static readonly string[] TubeNames = { "1/4", "3/8", "1/2", "5/16", "F1", "F2" };

    public string GetPumpName()       => PumpNames[Mathf.Clamp(pumpIndex, 0, PumpNames.Length - 1)];
    public string GetTubeName()       => TubeNames[Mathf.Clamp(tubeIndex, 0, TubeNames.Length - 1)];
    public string GetDirectionName()  => directionForward ? "Forward" : "Reverse";
}