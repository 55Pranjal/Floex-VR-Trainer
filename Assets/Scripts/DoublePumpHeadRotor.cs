using UnityEngine;

/// <summary>
/// Spins both rotors of the double pump head independently.
/// Each rotor follows its own pump's run state and direction.
/// Visual-only — no physiology coupling. Speed is fixed.
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
    [Tooltip("Rotation speed in degrees per second. 600 = ~100 RPM visual.")]
    public float rotationSpeed = 600f;

    [Tooltip("Local axis the rotor spins around. Default Y.")]
    public Vector3 rotationAxis = Vector3.up;

    void Update()
    {
        if (state == null) return;

        if (rotorA != null && state.pumpA_Running)
        {
            float dirA = state.pumpA_Direction == "CW" ? 1f : -1f;
            rotorA.Rotate(rotationAxis, rotationSpeed * dirA * Time.deltaTime, Space.Self);
        }

        if (rotorB != null && state.pumpB_Running)
        {
            float dirB = state.pumpB_Direction == "CW" ? 1f : -1f;
            rotorB.Rotate(rotationAxis, rotationSpeed * dirB * Time.deltaTime, Space.Self);
        }
    }
}