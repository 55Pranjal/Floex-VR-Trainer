using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Screen1 (home) controller for the double pump head canvas.
/// Owns two independent STOP/START buttons (per-pump readout swap), timer
/// play/reset, and PC bypass toggle. Picker entry buttons (PumpA/PumpB tube/dir,
/// Ratio) and nav strip are owned by DoublePumpHeadNavigator.
/// RPM readouts are knob-driven (read pumpA/pumpB_RpmSetpoint live), independent
/// of running state. LPM stays two-state. No physics, no interpolation.
/// </summary>
public class DoublePumpScreen1Controller : MonoBehaviour
{
    [Header("Wiring")]
    public DoublePumpHeadState state;

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
  

    const string StoppedLpm = "0.00";

    void Start()
    {
        WireStopButtons();
        WirePlayHistoryBypass();
    }

    void OnEnable()
    {
        UpdateReadouts();
        UpdateRpmDisplay();
        UpdateStopButtonLabels();
        UpdateTimerDisplay();
        UpdateBypassSprite();
        UpdatePlayPauseSprite();
    }

   void Update()
{
    UpdateRpmDisplay();
    UpdateReadouts();
    UpdateTimerDisplay();
}

    void WireStopButtons()
    {
        Transform a = FindDeep(transform, "Img_PumpA_Stop");
        if (a != null) HookButton(a.gameObject, OnPumpAStopTapped);

        Transform b = FindDeep(transform, "Img_PumpB_Stop");
        if (b != null) HookButton(b.gameObject, OnPumpBStopTapped);
    }

    void WirePlayHistoryBypass()
    {
        // Note: DoublePumpHeadNavigator already hooks Img_PlayIcon (toggles
        // timerRunning). We rewire here to also update the play/pause sprite.
        Transform play = FindDeep(transform, "Img_PlayIcon");
        if (play != null) HookButton(play.gameObject, OnPlayTapped);

        Transform history = FindDeep(transform, "Img_HistoryIcon");
        if (history != null) HookButton(history.gameObject, OnHistoryTapped);

        Transform bypass = FindDeep(transform, "Img_BypassToggle");
        if (bypass != null) HookButton(bypass.gameObject, OnBypassTapped);
    }

    void OnPumpAStopTapped()
    {
        if (state == null) return;
        state.pumpA_Running = !state.pumpA_Running;
        UpdateReadouts();
        UpdateStopButtonLabels();
    }

    void OnPumpBStopTapped()
    {
        if (state == null) return;
        state.pumpB_Running = !state.pumpB_Running;
        UpdateReadouts();
        UpdateStopButtonLabels();
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
    if (state == null) return;
    SetText("Txt_PumpA_Lpm", FlowText(state.pumpA_Running, state.pumpA_RpmSetpoint, state.GetFlowLpmA()));
    SetText("Txt_PumpB_Lpm", FlowText(state.pumpB_Running, state.pumpB_RpmSetpoint, state.GetFlowLpmB()));
}

string FlowText(bool running, int rpm, float lpm)
{
    bool live = running && state.powered && rpm > 0;
    return live ? lpm.ToString("0.00") : StoppedLpm;
}

    void UpdateRpmDisplay()
    {
        if (state == null) return;
        SetText("Txt_PumpA_Rpm", state.pumpA_RpmSetpoint.ToString("000"));
        SetText("Txt_PumpB_Rpm", state.pumpB_RpmSetpoint.ToString("000"));
    }

    void UpdateStopButtonLabels()
    {
        if (state == null) return;
        // When running, button says STOP (tap to stop). When stopped, says START.
        SetText("Txt_PumpA_Stop", state.pumpA_Running ? "STOP" : "START");
        SetText("Txt_PumpB_Stop", state.pumpB_Running ? "STOP" : "START");
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