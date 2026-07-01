using UnityEngine;

/// <summary>
/// State container for a double pump head canvas (slot 4).
/// Tracks two independent pump lanes (A and B) plus the shared flow ratio.
/// Each lane has its own pump role (per-lane, DPH is a free extra head).
/// RPM setpoints drive both the readout and (as of Day 29, Shiv-approved) the rotor speed.
/// </summary>
public class DoublePumpHeadState : MonoBehaviour
{
    [Header("Pump A")]
    public string pumpA_TubeSize = "1/4";
    public string pumpA_Direction = "CW";
    public int pumpA_RpmSetpoint = 0;   // 0..250 (set via Pump A knob cap)
    public int pumpA_PumpIndex = 0;     // 0=Nil,1=Arterial,2=Cardio,3=Vent,4=Suct1,5=Suct2

    [Header("Pump B")]
    public string pumpB_TubeSize = "1/4";
    public string pumpB_Direction = "CW";
    public int pumpB_RpmSetpoint = 0;   // 0..250 (set via Pump B knob cap)
    public int pumpB_PumpIndex = 0;     // 0=Nil,1=Arterial,2=Cardio,3=Vent,4=Suct1,5=Suct2

    [Header("Running State")]
    public bool pumpA_Running = false;
    public bool pumpB_Running = false;
    public bool bypassOn = false;

    [Header("Power (button beside knob)")]
    public bool powered = false;

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

    // Floex HLM flow calibration: 1/4 tube only (DPH is fixed 1/4). LPM per RPM.
    const float LpmPerRpm_Quarter = 0.00602f;

    /// <summary>Pump A flow (L/min) = 1/4 coefficient × RPM. Roller pump = linear.</summary>
    public float GetFlowLpmA() => LpmPerRpm_Quarter * pumpA_RpmSetpoint;

    /// <summary>Pump B flow (L/min) = 1/4 coefficient × RPM.</summary>
    public float GetFlowLpmB() => LpmPerRpm_Quarter * pumpB_RpmSetpoint;

    // Unique per-lane claimant handles for ArterialRegistry. Not serialized;
    // field initializers hold the reference. One head = two claimable lanes,
    // so each lane needs its own key (can't use `this`).
    [System.NonSerialized] public readonly object laneKeyA = new object();
    [System.NonSerialized] public readonly object laneKeyB = new object();

    static readonly string[] PumpNames = { "PUMP SELECT", "Arterial", "Cardio", "Vent", "Suct 1", "Suct 2" };

    public string GetPumpNameA() => PumpNames[Mathf.Clamp(pumpA_PumpIndex, 0, PumpNames.Length - 1)];
    public string GetPumpNameB() => PumpNames[Mathf.Clamp(pumpB_PumpIndex, 0, PumpNames.Length - 1)];

    /// <summary>
    /// Tracks which pump (A or B) the currently-open picker belongs to.
    /// Set by the home-screen button handler before opening a picker.
    /// Read by the picker's APPLY handler to know which pump to update.
    /// </summary>
    public enum ActivePump { None, A, B }
    [HideInInspector] public ActivePump activePicker = ActivePump.None;
}