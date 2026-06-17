using UnityEngine;

/// <summary>
/// Spins both rotors of the double pump head independently.
/// Each rotor follows its own pump's run state, direction, AND RPM setpoint.
/// Rotor speed tracks RPM as of Day 29 (Shiv-approved physiology coupling) —
/// 6 deg/s per RPM is physically accurate (1 RPM = 360deg/60s).
/// </summary>
public class DoublePumpHeadRotor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Pump A's rotor sub-mesh.")]
    public Transform rotorA;

    [Tooltip("Pump B's rotor sub-mesh.")]
    public Transform rotorB;

    [Tooltip("The DoublePumpHeadState that owns both pumps' flags.")]
    public DoublePumpHeadState state;

    [Header("Rotation settings")]
    [Tooltip("Degrees per second per 1 RPM. 6 = physically accurate (360deg/60s).")]
    public float degreesPerSecondPerRpm = 6f;

    [Tooltip("Local axis the rotor spins around. Default Y.")]
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        if (state == null) return;

        if (rotorA != null && state.pumpA_Running && state.pumpA_RpmSetpoint > 0)
        {
            float degPerSec = state.pumpA_RpmSetpoint * degreesPerSecondPerRpm;
            float dirA = state.pumpA_Direction == "CW" ? 1f : -1f;
            rotorA.Rotate(rotationAxis, degPerSec * dirA * Time.deltaTime, Space.Self);
        }

        if (rotorB != null && state.pumpB_Running && state.pumpB_RpmSetpoint > 0)
        {
            float degPerSec = state.pumpB_RpmSetpoint * degreesPerSecondPerRpm;
            float dirB = state.pumpB_Direction == "CW" ? 1f : -1f;
            rotorB.Rotate(rotationAxis, degPerSec * dirB * Time.deltaTime, Space.Self);
        }
    }
}