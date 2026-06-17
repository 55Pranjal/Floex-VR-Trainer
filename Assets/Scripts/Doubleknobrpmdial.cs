using UnityEngine;

/// <summary>
/// Double-pump variant of KnobRpmDial. Reads a knob cap's rotation and converts it into
/// the RPM setpoint of one of the two pumps (A or B) on a DoublePumpHeadState.
///
/// The One Grab Rotate Transformer DRIVES the cap's rotation (grab + twist). This script
/// only MEASURES it and writes the result to the selected pump's RPM setpoint. Keep both
/// components active on the knob cap: transformer = rotation, this script = RPM readout.
///
/// Each knob cap gets its own instance: KnobCapA -> pump A, KnobCapB -> pump B.
///
/// Rotor spin speed tracks RPM as of Day 29 (Shiv-approved). The wide Unity-primitive knob
/// cap has a clean local axis, so the default localSpinAxisFallback (Up) usually works with
/// no Axis Source needed.
/// </summary>
public class DoubleKnobRpmDial : MonoBehaviour
{
    public enum Pump { A, B }

    [Header("State")]
    [Tooltip("The DoublePumpHeadState this knob drives.")]
    public DoublePumpHeadState state;

    [Tooltip("Which pump's RPM setpoint this knob controls.")]
    public Pump pump = Pump.A;

    [Header("Axis")]
    [Tooltip("Optional axis source. Leave empty to use this object's local axis (clean for " +
             "a Unity-primitive cap). Its UP arrow should point down the cap; tick Use " +
             "Forward Axis if FORWARD does instead.")]
    public Transform axisSource;
    public bool useForwardAxis = false;
    [Tooltip("Local axis of THIS object, used when Axis Source is empty. Cap = Up (Y).")]
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
    Vector3 _refDir;
    float _accumAngle;
    int _appliedSteps;

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

    int CurrentRpm => pump == Pump.A ? state.pumpA_RpmSetpoint : state.pumpB_RpmSetpoint;

    void SetRpm(int value)
    {
        if (pump == Pump.A) state.pumpA_RpmSetpoint = value;
        else                state.pumpB_RpmSetpoint = value;
    }

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
        if (state == null) return;

        Vector3 axis = WorldAxis;

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

        if (degreesPerDetent > 0.01f)
        {
            int totalSteps = Mathf.RoundToInt(_accumAngle / degreesPerDetent);
            int diff = totalSteps - _appliedSteps;
            _appliedSteps = totalSteps;
            SetRpm(Mathf.Clamp(CurrentRpm + diff, minRpm, maxRpm));
        }
    }
}