using UnityEngine;
using Floex.Physiology;

/// <summary>
/// Thin Unity bridge that owns a PatientState and ticks it on a fixed 50ms cadence.
/// Keeps the physiology class itself Unity-free (and unit-testable) — this is the
/// ONLY place Unity time touches PatientState.
///
/// 50ms = 20 Hz physiology tick (roadmap Week 4). Uses a fixed-step accumulator so
/// the tick rate is independent of frame rate (72fps render, 20Hz physiology).
/// </summary>
public class PatientStateDriver : MonoBehaviour
{
    [Tooltip("Physiology tick period in seconds. 0.05 = 50ms = 20 Hz.")]
    public double tickPeriodSeconds = 0.05;

    [Tooltip("Start the patient on bypass immediately (for testing).")]
    public bool startOnBypass = false;

    public PatientState State { get; private set; }

    double _accumulator;

    void Awake()
    {
        State = new PatientState();
        State.OnBypass = startOnBypass;
    }

    void Update()
    {
        // Fixed-step accumulator: drain real time into discrete 50ms ticks so the
        // physiology advances at a steady 20 Hz regardless of render frame rate.
        _accumulator += Time.deltaTime;
        while (_accumulator >= tickPeriodSeconds)
        {
            State.Tick(tickPeriodSeconds);
            _accumulator -= tickPeriodSeconds;
        }
    }

    // --- Simple control surface for now (manual; sensors/UI wire in later) ---

    public void SetOnBypass(bool on) => State.OnBypass = on;
    public void ResetState() => State.ResetToRestingDefaults();
}