using UnityEngine;

/// <summary>
/// Reads the knob's rotation and converts it into an integer RPM setpoint.
///
/// The One Grab Rotate Transformer DRIVES the knob's rotation (grab + twist). This script
/// does NOT rotate anything — it only measures how far the knob has turned about its barrel
/// axis since the last frame and maps that to rpmSetpoint. Keep both components active:
/// transformer = rotation, this script = RPM readout.
///
/// Assign Axis Source = the same KnobAxis empty the transformer pivots around (its UP/green
/// arrow points down the shaft, OR set useForwardAxis if its FORWARD/blue arrow does).
///
/// PRODUCT A: changes the DISPLAYED rpm setpoint only. Rotor visual speed stays fixed
/// (see PumpHeadRotor) — never couple rotor speed to rpmSetpoint.
/// </summary>
public class KnobRpmDial : MonoBehaviour
{
    [Header("State")]
    [Tooltip("The PumpHeadState whose rpmSetpoint this knob drives.")]
    public PumpHeadState state;

    [Header("Axis")]
    [Tooltip("The KnobAxis empty the transformer rotates around. Its UP (green) arrow " +
             "should point down the shaft. If its FORWARD (blue) arrow does instead, " +
             "tick Use Forward Axis.")]
    public Transform axisSource;
    [Tooltip("Use axisSource.forward (blue) instead of axisSource.up (green) as the axis.")]
    public bool useForwardAxis = false;
    [Tooltip("Fallback if Axis Source is empty: local axis of THIS object.")]
    public Vector3 localSpinAxisFallback = Vector3.up;

    [Header("RPM mapping")]
    public int minRpm = 0;
    public int maxRpm = 250;
    [Tooltip("Degrees of knob rotation per 1 RPM step. 6 deg/RPM => 0-250 is ~4.2 turns.")]
    public float degreesPerDetent = 6f;
    [Tooltip("Flip if clockwise should DECREASE instead of increase.")]
    public bool invert = false;

    [Header("Debug")]
    public bool drawAxis = true;

    Renderer _renderer;
    bool _haveRef;
    Vector3 _refDir;             // last frame's reference direction in the axis plane
    float _accumAngle;           // accumulated rotation since start (deg)

    void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
    }

    Vector3 WorldAxis =>
        axisSource != null
            ? (useForwardAxis ? axisSource.forward : axisSource.up).normalized
            : transform.TransformDirection(localSpinAxisFallback).normalized;

    Vector3 PivotPoint =>
        axisSource != null
            ? axisSource.position
            : (_renderer != null ? _renderer.bounds.center : transform.position);

    void Update()
    {
        MeasureRotation();

        if (drawAxis)
        {
            Debug.DrawRay(PivotPoint, WorldAxis * 0.12f, Color.green);
            Debug.DrawRay(PivotPoint, -WorldAxis * 0.12f, Color.green);
        }
    }

    void MeasureRotation()
    {
        Vector3 axis = WorldAxis;

        // Pick a reference vector fixed to the knob (its right vector), projected into the
        // plane perpendicular to the barrel axis. As the knob spins, this rotates with it.
        Vector3 r = transform.right;
        if (Mathf.Abs(Vector3.Dot(r.normalized, axis)) > 0.9f)
            r = transform.forward;   // avoid a reference parallel to the axis

        Vector3 proj = Vector3.ProjectOnPlane(r, axis);
        if (proj.sqrMagnitude < 1e-6f) return;
        proj.Normalize();

        if (!_haveRef)
        {
            _refDir = proj;
            _haveRef = true;
            return;
        }

        float delta = Vector3.SignedAngle(_refDir, proj, axis);
        _refDir = proj;
        if (Mathf.Abs(delta) < 1e-4f) return;
        if (invert) delta = -delta;

        _accumAngle += delta;

        if (state != null && degreesPerDetent > 0.01f)
        {
            int steps = Mathf.RoundToInt(_accumAngle / degreesPerDetent);
            state.rpmSetpoint = Mathf.Clamp(state.rpmSetpoint + StepDiff(steps), minRpm, maxRpm);
        }
    }

    // Apply only the *change* in steps since last applied, so clamping at the ends
    // doesn't lose or double-count rotation.
    int _appliedSteps;
    int StepDiff(int totalSteps)
    {
        int diff = totalSteps - _appliedSteps;
        _appliedSteps = totalSteps;
        return diff;
    }
}