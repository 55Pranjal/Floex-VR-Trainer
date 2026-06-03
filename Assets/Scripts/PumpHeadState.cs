using UnityEngine;

/// <summary>
/// Per-pump-head state. One instance per pump head (mirrors firmware Model).
/// PRODUCT A: data store + label refresh only. NO live values, NO physics.
/// Live values like LPM/RPM stay as static "0.00" in Screen1 — scope-lock.
/// </summary>
public class PumpHeadState : MonoBehaviour
{
    [Header("State — mirrors firmware Model")]
    public int pumpIndex = 0;       // 0=Arterial, 1=Cardioplegia, 2=Vent, 3=Suction1, 4=Suction2
    public int tubeIndex = 0;       // 0=1/4, 1=3/8, 2=1/2, 3=5/16, 4=F1, 5=F2
    public bool directionForward = true;

    static readonly string[] PumpNames     = { "Arterial", "Cardio", "Vent", "Suct 1", "Suct 2" };
    static readonly string[] TubeNames     = { "1/4", "3/8", "1/2", "5/16", "F1", "F2" };

    public string GetPumpName()       => PumpNames[Mathf.Clamp(pumpIndex, 0, PumpNames.Length - 1)];
    public string GetTubeName()       => TubeNames[Mathf.Clamp(tubeIndex, 0, TubeNames.Length - 1)];
    public string GetDirectionName()  => directionForward ? "Forward" : "Reverse";
}