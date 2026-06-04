using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Screen1 (home) controller. Owns START/STOP buttons (running-state readout swap),
/// session timer play/reset, and bypass toggle. PumpHeadNavigator owns the nav strip
/// and picker entries (PUMP SELECT / Txt_Tube / Txt_Direction).
/// PRODUCT A: two-state static readouts. No interpolation, no physics.
/// </summary>
public class Screen1Controller : MonoBehaviour
{
    [Header("Wiring")]
    public PumpHeadState state;

    [Header("Toggle sprites (bypass)")]
    [Tooltip("Drag Assets/Textures/UI/toggle_on.png here")]
    public Sprite toggleOnSprite;
    [Tooltip("Drag Assets/Textures/UI/toggle_off.png here")]
    public Sprite toggleOffSprite;

    [Header("Timer sprites (play/pause)")]
    [Tooltip("Drag Assets/Textures/UI/play.png here")]
    public Sprite playSprite;
    [Tooltip("Drag Assets/Textures/UI/pause.png here")]
    public Sprite pauseSprite;

    [Header("Running readouts (Stopped always shows zero values)")]
    public string runningLpm     = "4.50";
    public string runningRpm     = "180";
    public string runningCurrent = "I:0.50A";
    public string runningTorque  = "TQ:0.04Nm";
    public string runningVoltage = "V:1.50V";

    const string StoppedLpm     = "0.00";
    const string StoppedRpm     = "000";
    const string StoppedCurrent = "I:0.00A";
    const string StoppedTorque  = "TQ:0.00Nm";
    const string StoppedVoltage = "V:0.00V";

    void Start()
    {
        // Wire once after all Awakes complete, so PumpHeadNavigator's blanket-disable
        // (in its Awake) doesn't undo our wiring. Buttons persist across enable/disable.
        WireStartStop();
        WirePlayHistoryBypass();
    }

    void OnEnable()
    {
        // Refresh display each time Screen1 becomes visible.
        UpdateReadouts();
        UpdateTimerDisplay();
        UpdateBypassSprite();
        UpdatePlayPauseSprite();
    }

    void Update()
    {
        // Tick the timer display while Screen1 is visible. PumpHeadNavigator
        // increments state.timerSeconds, this just renders it.
        UpdateTimerDisplay();
    }

    void WireStartStop()
    {
        Transform start = FindDeep(transform, "Img_StartBg");
        if (start != null) HookButton(start.gameObject, OnStart);

        Transform stop = FindDeep(transform, "Img_StopBg");
        if (stop != null) HookButton(stop.gameObject, OnStop);
    }

    void WirePlayHistoryBypass()
    {
        Transform play = FindDeep(transform, "Img_PlayIcon");
        if (play != null) HookButton(play.gameObject, OnPlayTapped);

        Transform history = FindDeep(transform, "Img_HistoryIcon");
        if (history != null) HookButton(history.gameObject, OnHistoryTapped);

        Transform bypass = FindDeep(transform, "Img_BypassToggle");
        if (bypass != null) HookButton(bypass.gameObject, OnBypassTapped);
    }
void OnStart()
{
    if (state == null) return;
    Debug.Log($"[Screen1] START tapped on canvas: {transform.root.name}");
    state.running = true;
    UpdateReadouts();
}

    void OnStop()
    {
        if (state == null) return;
        state.running = false;
        UpdateReadouts();
    }

    void OnPlayTapped()
    {
        if (state == null) return;
        state.timerRunning = !state.timerRunning;
        UpdatePlayPauseSprite();
    }

    void OnHistoryTapped()
    {
        if (state == null) return;
        state.timerSeconds = 0f;
        UpdateTimerDisplay();
    }

    void OnBypassTapped()
    {
        if (state == null) return;
        state.bypassOn = !state.bypassOn;
        UpdateBypassSprite();
    }

    void UpdateReadouts()
    {
        bool running = state != null && state.running;
        SetText("Txt_LpmValue", running ? runningLpm     : StoppedLpm);
        SetText("Txt_RpmValue", running ? runningRpm     : StoppedRpm);
        SetText("Txt_Current",  running ? runningCurrent : StoppedCurrent);
        SetText("Txt_Torque",   running ? runningTorque  : StoppedTorque);
        SetText("Txt_Voltage",  running ? runningVoltage : StoppedVoltage);
    }

    void UpdateTimerDisplay()
    {
        if (state == null) return;
        int totalSec = (int)state.timerSeconds;
        int h = totalSec / 3600;
        int m = (totalSec / 60) % 60;
        int s = totalSec % 60;
        SetText("Txt_Timer", $"{h:D2}:{m:D2}:{s:D2}");
    }

    void UpdateBypassSprite()
    {
        if (state == null) return;
        Transform t = FindDeep(transform, "Img_BypassToggle");
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img == null) return;
        Sprite target = state.bypassOn ? toggleOnSprite : toggleOffSprite;
        if (target != null) img.sprite = target;
    }

    void UpdatePlayPauseSprite()
    {
        if (state == null) return;
        Transform t = FindDeep(transform, "Img_PlayIcon");
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img == null) return;
        Sprite target = state.timerRunning ? pauseSprite : playSprite;
        if (target != null) img.sprite = target;
    }

    void SetText(string name, string value)
    {
        Transform t = FindDeep(transform, name);
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }

    void HookButton(GameObject go, UnityAction onClick)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = graphic;
        btn.transition = Selectable.Transition.None;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform child in root)
        {
            Transform found = FindDeep(child, name);
            if (found != null) return found;
        }
        return null;
    }
}