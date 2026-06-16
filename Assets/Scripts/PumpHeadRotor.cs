using UnityEngine;

/// <summary>
/// Spins a pump head's rotor sub-mesh when the pump is running.
/// Visual-only — no physiology coupling. Speed is fixed.
/// Attach to the pump head root GameObject; assign rotor + state references.
/// </summary>
public class PumpHeadRotor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Rotor_Assembly sub-mesh to spin.")]
    public Transform rotor;

    [Tooltip("The PumpHeadState that owns this pump's run/direction flags.")]
    public PumpHeadState state;

    [Header("Rotation settings")]
    [Tooltip("Rotation speed in degrees per second. 600 = ~100 RPM visual.")]
    public float rotationSpeed = 600f;

    [Tooltip("Local axis the rotor spins around. Default Y (vertical).")]
    public Vector3 rotationAxis = Vector3.up;

   [Header("Rotation settings")]
    [Tooltip("Degrees per second per 1 RPM. 6 = physically accurate (360deg/60s).")]
    public float degreesPerSecondPerRpm = 6f;

    void Update()
    {
        if (rotor == null || state == null) return;
        if (!state.running || state.rpmSetpoint <= 0) return;

        float degPerSec = state.rpmSetpoint * degreesPerSecondPerRpm;
        float direction = state.directionForward ? 1f : -1f;
        rotor.Rotate(rotationAxis, degPerSec * direction * Time.deltaTime, Space.Self);
    }
}