using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// CDM pole screen controller.
///
/// Two kinds of interactive element:
///   - TIMERS: a play/pause sprite toggle paired with a counting hh:mm:ss display
///     and a reset button. Tapping play starts the timer counting up and swaps the
///     sprite to pause; tapping again pauses; reset zeroes the time and restores the
///     play sprite. Mirrors the pump-head Screen1 timer behaviour.
///   - PLAIN TOGGLES: a 2-state sprite swap with no timer (e.g. the speaker/mute).
///
/// Timer state is local to this screen (there is no shared state object for the pole
/// screen). Counters tick in Update only while running.
///
/// PRODUCT A/B scope: timers count real elapsed session time; no physiology is
/// simulated. Button taps are familiarisation interactions.
/// </summary>
public class CDMScreenController : MonoBehaviour
{
    [System.Serializable]
    public class Timer
    {
        [Tooltip("Play/pause button GameObject (e.g. Img_Timer1Play).")]
        public string playObjectName;

        [Tooltip("Reset button GameObject (e.g. Img_Timer1Reset). Optional.")]
        public string resetObjectName;

        [Tooltip("hh:mm:ss display GameObject (e.g. Txt_Timer1).")]
        public string timerTextName;

        [Tooltip("Sprite shown when stopped/paused (the 'play' glyph).")]
        public Sprite playSprite;

        [Tooltip("Sprite shown while running (the 'pause' glyph).")]
        public Sprite pauseSprite;
    }

    [System.Serializable]
    public class SpriteToggle
    {
        [Tooltip("GameObject name to find (typically Img_X).")]
        public string objectName;

        [Tooltip("Sprite shown initially and after a reset.")]
        public Sprite defaultSprite;

        [Tooltip("Sprite shown after a tap.")]
        public Sprite toggledSprite;
    }

    [Header("Timers (4 total) - play toggles + counting display + reset")]
    public Timer[] timers = new Timer[]
    {
        new Timer { playObjectName = "Img_LeftPlay",   resetObjectName = "Img_LeftHistory", timerTextName = "Txt_LeftTimer" },
        new Timer { playObjectName = "Img_Timer1Play", resetObjectName = "Img_Timer1Reset", timerTextName = "Txt_Timer1" },
        new Timer { playObjectName = "Img_Timer2Play", resetObjectName = "Img_Timer2Reset", timerTextName = "Txt_Timer2" },
        new Timer { playObjectName = "Img_Timer3Play", resetObjectName = "Img_Timer3Reset", timerTextName = "Txt_Timer3" },
    };

    [Header("Plain sprite toggles (no timer)")]
    public SpriteToggle[] toggles = new SpriteToggle[]
    {
        new SpriteToggle { objectName = "Img_Speaker" },  // bottom-right mute
    };

    [Header("Visuals")]
    [Tooltip("Hover/press tint on all interactive buttons.")]
    public Color pressTint = new Color(0.85f, 0.85f, 0.85f);

    // --- timer runtime state ---
    class TimerRuntime
    {
        public Timer config;
        public Image playImage;
        public TMP_Text display;
        public bool running;
        public float seconds;
    }

    readonly List<TimerRuntime> _timers = new List<TimerRuntime>();

    // --- plain toggle runtime state ---
    readonly Dictionary<string, Image>        toggleImages  = new Dictionary<string, Image>();
    readonly Dictionary<string, bool>         toggleStates  = new Dictionary<string, bool>();
    readonly Dictionary<string, SpriteToggle> toggleConfigs = new Dictionary<string, SpriteToggle>();

    void Start()
    {
        // ScreenNavigator runs in Awake and turns off raycast on everything.
        // We re-enable raycast on the elements we make interactive here.

        foreach (Timer t in timers)
            WireTimer(t);

        foreach (SpriteToggle t in toggles)
            WireToggle(t);
    }

    void Update()
    {
        // Tick running timers and render hh:mm:ss.
        for (int i = 0; i < _timers.Count; i++)
        {
            TimerRuntime rt = _timers[i];
            if (rt.running)
            {
                rt.seconds += Time.deltaTime;
                RenderTimer(rt);
            }
        }
    }

    // ---------------- timers ----------------

    void WireTimer(Timer t)
    {
        Transform playTr = FindDeep(transform, t.playObjectName);
        if (playTr == null) { Debug.LogWarning($"[CDMScreenController] {t.playObjectName} not found - timer skipped."); return; }

        Image playImg = playTr.GetComponent<Image>();
        if (playImg == null) { Debug.LogWarning($"[CDMScreenController] {t.playObjectName} missing Image."); return; }

        TMP_Text display = null;
        if (!string.IsNullOrEmpty(t.timerTextName))
        {
            Transform txt = FindDeep(transform, t.timerTextName);
            if (txt != null) display = txt.GetComponent<TMP_Text>();
            if (display == null) Debug.LogWarning($"[CDMScreenController] {t.timerTextName} not found / no TMP_Text.");
        }

        TimerRuntime rt = new TimerRuntime
        {
            config = t,
            playImage = playImg,
            display = display,
            running = false,
            seconds = 0f,
        };
        _timers.Add(rt);

        if (t.playSprite != null) playImg.sprite = t.playSprite;
        RenderTimer(rt);

        // Play/pause button.
        playImg.raycastTarget = true;
        Button playBtn = ConfigureButton(playTr.gameObject, playImg);
        playBtn.onClick.RemoveAllListeners();
        playBtn.onClick.AddListener(() => ToggleTimer(rt));

        // Reset button (optional).
        if (!string.IsNullOrEmpty(t.resetObjectName))
        {
            Transform resetTr = FindDeep(transform, t.resetObjectName);
            if (resetTr != null)
            {
                Image resetImg = resetTr.GetComponent<Image>();
                if (resetImg != null)
                {
                    resetImg.raycastTarget = true;
                    Button resetBtn = ConfigureButton(resetTr.gameObject, resetImg);
                    resetBtn.onClick.RemoveAllListeners();
                    resetBtn.onClick.AddListener(() => ResetTimer(rt));
                }
            }
        }
    }

    void ToggleTimer(TimerRuntime rt)
    {
        rt.running = !rt.running;
        Sprite target = rt.running ? rt.config.pauseSprite : rt.config.playSprite;
        if (target != null) rt.playImage.sprite = target;
    }

    void ResetTimer(TimerRuntime rt)
    {
        rt.running = false;
        rt.seconds = 0f;
        if (rt.config.playSprite != null) rt.playImage.sprite = rt.config.playSprite;
        RenderTimer(rt);
    }

    void RenderTimer(TimerRuntime rt)
    {
        if (rt.display == null) return;
        int total = (int)rt.seconds;
        int h = total / 3600;
        int m = (total / 60) % 60;
        int s = total % 60;
        rt.display.text = $"{h:D2}:{m:D2}:{s:D2}";
    }

    // ---------------- plain toggles ----------------

    void WireToggle(SpriteToggle t)
    {
        Transform tr = FindDeep(transform, t.objectName);
        if (tr == null) { Debug.LogWarning($"[CDMScreenController] {t.objectName} not found - toggle skipped."); return; }

        Image img = tr.GetComponent<Image>();
        if (img == null) { Debug.LogWarning($"[CDMScreenController] {t.objectName} missing Image."); return; }

        img.raycastTarget = true;

        Button btn = ConfigureButton(tr.gameObject, img);
        string name = t.objectName;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => Toggle(name));

        toggleImages[name]  = img;
        toggleStates[name]  = false;
        toggleConfigs[name] = t;

        if (t.defaultSprite != null) img.sprite = t.defaultSprite;
    }

    void Toggle(string name)
    {
        if (!toggleImages.TryGetValue(name, out Image img))       return;
        if (!toggleStates.TryGetValue(name, out bool toggled))    return;
        if (!toggleConfigs.TryGetValue(name, out SpriteToggle t)) return;

        toggled = !toggled;
        toggleStates[name] = toggled;

        Sprite target = toggled ? t.toggledSprite : t.defaultSprite;
        if (target != null) img.sprite = target;
    }

    // ---------------- shared ----------------

    Button ConfigureButton(GameObject go, Image targetGraphic)
    {
        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = targetGraphic;

        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = pressTint;
        cb.pressedColor     = pressTint;
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        return btn;
    }

    static Transform FindDeep(Transform root, string n)
    {
        if (root.name == n) return root;
        foreach (Transform c in root)
        {
            Transform f = FindDeep(c, n);
            if (f != null) return f;
        }
        return null;
    }
}