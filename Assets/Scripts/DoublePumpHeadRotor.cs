using UnityEngine;

/// <summary>
/// Spins both rotors of the double pump head independently.
/// Each rotor follows its own pump's run state, direction, AND RPM setpoint
/// (Day 29, Shiv-approved: 6 deg/s per RPM = physically accurate).
///
/// Direction changes RAMP through zero rather than flipping instantly (angular
/// momentum). Each rotor eases its own angular velocity toward its target.
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

    [Header("Momentum")]
    [Tooltip("How fast each rotor speeds up / slows down, in deg/s per second. " +
             "Lower = heavier. Keep equal to the single-pump value for consistency.")]
    public float angularAcceleration = 900f;

    float _currentA;   // signed angular velocity, pump A (deg/s)
    float _currentB;   // signed angular velocity, pump B (deg/s)

    void Update()
    {
        if (state == null) return;

        if (rotorA != null)
            StepRotor(rotorA, ref _currentA,
                      state.pumpA_Running, state.pumpA_RpmSetpoint, state.pumpA_Direction);

        if (rotorB != null)
            StepRotor(rotorB, ref _currentB,
                      state.pumpB_Running, state.pumpB_RpmSetpoint, state.pumpB_Direction);
    }

    void StepRotor(Transform rotor, ref float current, bool running, int rpm, string direction)
    {
        float target = 0f;
        if (running && rpm > 0)
        {
            float dir = direction == "CW" ? 1f : -1f;
            target = rpm * degreesPerSecondPerRpm * dir;
        }

        current = Mathf.MoveTowards(current, target, angularAcceleration * Time.deltaTime);

        if (Mathf.Abs(current) > 0.01f)
            rotor.Rotate(rotationAxis, current * Time.deltaTime, Space.Self);
    }
}