using UnityEngine;

/// <summary>
/// Drives the double pump head's running hum (one AudioSource for the unit).
/// Plays while EITHER pump runs; pitch tracks the higher of the two pumps' RPMs
/// (one physical unit, so the faster pump dominates what you'd hear).
/// Pitch coupling Shiv-approved (Day 35).
///
/// Attach to the double pump head root (same object as DoublePumpHeadRotor).
/// AudioSource: clip = pump_loop, Loop = true, Play On Awake = false,
/// Spatial Blend = 1.0.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DoublePumpAudio : MonoBehaviour
{
    [Header("References")]
    public DoublePumpHeadState state;
    AudioSource _src;

    [Header("Pitch coupling (RPM -> pitch)")]
    public float minPitch = 0.9f;
    public float maxPitch = 1.2f;
    public int maxRpm = 250;
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

        bool anyRunning = (state.pumpA_Running && state.pumpA_RpmSetpoint > 0)
               || (state.pumpB_Running && state.pumpB_RpmSetpoint > 0);

        if (anyRunning)
        {
            if (!_src.isPlaying) _src.Play();

            // Only count RPM of pumps that are actually running.
            int rpmA = state.pumpA_Running ? state.pumpA_RpmSetpoint : 0;
            int rpmB = state.pumpB_Running ? state.pumpB_RpmSetpoint : 0;
            int rpm = Mathf.Max(rpmA, rpmB);

            float t = maxRpm > 0 ? Mathf.Clamp01(rpm / (float)maxRpm) : 0f;
            float target = Mathf.Lerp(minPitch, maxPitch, t);
            _src.pitch = Mathf.Lerp(_src.pitch, target, pitchLerp * Time.deltaTime);
        }
        else
        {
            if (_src.isPlaying) _src.Stop();
        }
    }
}