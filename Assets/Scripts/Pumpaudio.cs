using UnityEngine;

/// <summary>
/// Drives a pump head's running hum. Plays the looped AudioSource while the pump
/// is running, and pitch-couples the hum to RPM (Shiv-approved, Day 35).
///
/// Attach to the single pump head root (same object as PumpHeadRotor).
/// AudioSource should have: clip = pump_loop, Loop = true, Play On Awake = false,
/// Spatial Blend = 1.0 (built-in 3D audio; no Meta spatializer needed).
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class PumpAudio : MonoBehaviour
{
    [Header("References")]
    public PumpHeadState state;
    AudioSource _src;

    [Header("Pitch coupling (RPM -> pitch)")]
    [Tooltip("Pitch at 0 RPM (also the floor while just-running).")]
    public float minPitch = 0.9f;
    [Tooltip("Pitch at maxRpm.")]
    public float maxPitch = 1.2f;
    [Tooltip("RPM that maps to maxPitch.")]
    public int maxRpm = 250;
    [Tooltip("Smoothing on pitch changes (higher = snappier).")]
    public float pitchLerp = 6f;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.loop = true;
        _src.playOnAwake = false;
    }

    void Update()
    {
        if (state == null) return;

        if (state.running && state.rpmSetpoint > 0)
        {
            if (!_src.isPlaying) _src.Play();

            float t = maxRpm > 0 ? Mathf.Clamp01(state.rpmSetpoint / (float)maxRpm) : 0f;
            float target = Mathf.Lerp(minPitch, maxPitch, t);
            _src.pitch = Mathf.Lerp(_src.pitch, target, pitchLerp * Time.deltaTime);
        }
        else
        {
            if (_src.isPlaying) _src.Stop();
        }
    }
}