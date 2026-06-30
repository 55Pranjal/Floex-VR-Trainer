using UnityEngine;

/// <summary>
/// Spins a pump head's rotor sub-mesh, with speed tracking the RPM setpoint
/// (Day 29, Shiv-approved: 6 deg/s per RPM = physically accurate, 1 RPM = 360deg/60s).
///
/// Direction changes RAMP through zero rather than flipping instantly — a spinning
/// rotor has angular momentum, so it decelerates, stops, and spins up the other way.
/// Stopping (run off, or RPM 0) also ramps down rather than cutting dead.
///
/// Attach to the pump head root; assign rotor + state references.
/// </summary>
public class PumpHeadRotor : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Rotor_Assembly sub-mesh to spin.")]
    public Transform rotor;

    [Tooltip("The PumpHeadState that owns this pump's run/direction flags.")]
    public PumpHeadState state;

    [Header("Rotation settings")]
    [Tooltip("Degrees per second per 1 RPM. 6 = physically accurate (360deg/60s).")]
    public float degreesPerSecondPerRpm = 6f;

    [Tooltip("Local axis the rotor spins around. Default Y (vertical).")]
    public Vector3 rotationAxis = Vector3.up;

    [Header("Momentum")]
    [Tooltip("How fast the rotor speeds up / slows down, in deg/s per second. " +
             "Lower = heavier/slower to change direction. 900 ~= reverses in ~1-2s at mid RPM.")]
    public float angularAcceleration = 900f;

    // Current signed angular velocity (deg/s). Eases toward the target each frame.
    float _currentDegPerSec;

    void Update()
    {
        if (rotor == null || state == null) return;

        // Target speed: 0 unless running with RPM > 0. Sign from direction.
        float target = 0f;
        if (state.running && state.rpmSetpoint > 0 && state.powered)
        {
            float dir = state.directionForward ? 1f : -1f;
            target = state.rpmSetpoint * degreesPerSecondPerRpm * dir;
        }

        // Ease current velocity toward target at the acceleration limit.
        _currentDegPerSec = Mathf.MoveTowards(
            _currentDegPerSec, target, angularAcceleration * Time.deltaTime);

        if (Mathf.Abs(_currentDegPerSec) > 0.01f)
            rotor.Rotate(rotationAxis, _currentDegPerSec * Time.deltaTime, Space.Self);
    }
}