using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Per-canvas navigation for the double pump head (slot 4). Manages:
///  - Screen1 picker entry buttons: PumpA tube/dir, PumpB tube/dir, shared Ratio
///  - 4-button nav strip on Screen1/2_1/3/4/5 (slot-swap pattern)
///  - Tracks which pump (A or B) opened the active tube/direction picker
///  - Screen lifecycle (ShowScreen activates one, deactivates rest)
///  - Label refresh on return home (state mirrors)
/// Click pipeline: PointableCanvas -> GraphicRaycaster -> Unity Button.onClick.
/// </summary>
public class DoublePumpHeadNavigator : MonoBehaviour
{
    [Tooltip("Default screen shown on launch and when returning from a picker.")]
    public string homeScreen = "Screen_PumpHead04_Screen1";

    [Tooltip("Tint applied to a button on hover/press.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f);

    [Tooltip("Double pump head state - refreshed labels on Screen1 read from this.")]
    public DoublePumpHeadState state;

    // Screen1 picker entry buttons. Tube + Direction entries also set activePicker
    // (A or B) before opening, so the shared Tube_Size_1/Direction picker knows which
    // pump's state to update on APPLY.
    struct PickerEntry
    {
        public string target;
        public DoublePumpHeadState.ActivePump pump; // None for shared (Ratio)
        public PickerEntry(string t, DoublePumpHeadState.ActivePump p) { target = t; pump = p; }
    }

    static readonly Dictionary<string, PickerEntry> Screen1PickerEntries = new Dictionary<string, PickerEntry>
    {
        { "Txt_PumpA_Tube", new PickerEntry("Tube_Size_1", DoublePumpHeadState.ActivePump.A) },
        { "Txt_PumpA_Dir",  new PickerEntry("Direction",   DoublePumpHeadState.ActivePump.A) },
        { "Txt_PumpB_Tube", new PickerEntry("Tube_Size_2", DoublePumpHeadState.ActivePump.B) },
        { "Txt_PumpB_Dir",  new PickerEntry("Direction_2", DoublePumpHeadState.ActivePump.B) },
        { "Txt_Ratio",      new PickerEntry("Screen_PumpHead04_FlowRatio", DoublePumpHeadState.ActivePump.None) },
    };

    // Nav strip button name -> destination screen.
    static readonly Dictionary<string, string> NavStripMap = new Dictionary<string, string>
    {
        { "Btn_P1_Bg", "Screen_PumpHead04_Screen1"   },
        { "Btn_P2_Bg", "Screen_PumpHead_Screen2_1"   },
        { "Btn_P3_Bg", "Screen_PumpHead_Screen3"     },
        { "Btn_P4_Bg", "Screen_PumpHead_Screen4"     },
        { "Btn_P5_Bg", "Screen_PumpHead_Screen5"     },
    };

    static readonly HashSet<string> ManagedScreens = new HashSet<string>
    {
        "Screen_PumpHead04_Screen1",
        "Screen_PumpHead_Screen2_1",
        "Screen_PumpHead_Screen3",
        "Screen_PumpHead_Screen4",
        "Screen_PumpHead_Screen5",
        "Screen_PumpHead04_FlowRatio",
        "Tube_Size_1", "Tube_Size_2",
        "Direction", "Direction_2",
    };

    readonly List<GameObject> screens = new List<GameObject>();

    void Awake()
    {
        foreach (Transform child in transform)
            if (ManagedScreens.Contains(child.name))
                screens.Add(child.gameObject);

        WireHomeScreen();

        WireNavStripOnly("Screen_PumpHead_Screen2_1");
        WireNavStripOnly("Screen_PumpHead_Screen3");
        WireNavStripOnly("Screen_PumpHead_Screen4");
        WireNavStripOnly("Screen_PumpHead_Screen5");

        ShowScreen(homeScreen);
    }

    void WireHomeScreen()
    {
        Transform home = transform.Find(homeScreen);
        if (home == null) return;

        // Display-only by default; HookButton turns raycast back on per-element
        foreach (Graphic g in home.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireNavStripButtons(home);

        foreach (KeyValuePair<string, PickerEntry> kv in Screen1PickerEntries)
        {
            Transform t = FindDeep(home, kv.Key);
            if (t == null) continue;
            PickerEntry entry = kv.Value;
            HookButton(t.gameObject, () =>
            {
                if (state != null) state.activePicker = entry.pump;
                ShowScreen(entry.target);
            });
        }

        Transform play = FindDeep(home, "Img_PlayIcon");
if (play != null)
{
    HookButton(play.gameObject, () =>
    {
        if (state != null) state.timerRunning = !state.timerRunning;
    });
}
    }

    void WireNavStripOnly(string screenName)
    {
        Transform screen = transform.Find(screenName);
        if (screen == null) return;
        WireNavStripButtons(screen);
    }

    public void WireNavStripButtons(Transform screen)
    {
        foreach (KeyValuePair<string, string> entry in NavStripMap)
        {
            Transform bgT = FindDeep(screen, entry.Key);
            if (bgT == null) continue;

            string target = entry.Value;
            HookButton(bgT.gameObject, () => ShowScreen(target));

            string textName = entry.Key.Replace("_Bg", "_Text");
            Transform textT = FindDeep(screen, textName);
            if (textT != null)
            {
                Graphic tg = textT.GetComponent<Graphic>();
                if (tg != null) tg.raycastTarget = false;
            }
        }
    }

    public void ShowScreen(string screenName)
    {
        bool found = false;
        foreach (GameObject screen in screens)
        {
            bool match = screen.name == screenName;
            screen.SetActive(match);
            if (match) found = true;
        }

        if (!found)
        {
            Debug.LogWarning($"[DoublePumpHeadNavigator] No screen named '{screenName}' under " +
                             $"{gameObject.name}. Check the homeScreen field and child names.");
        }

        if (screenName == homeScreen) RefreshHomeLabels();
    }

    void RefreshHomeLabels()
{
    if (state == null) return;
    Transform home = transform.Find(homeScreen);
    if (home == null) return;

    SetText(home, "Txt_PumpA_Tube", state.pumpA_TubeSize);
    SetText(home, "Txt_PumpA_Dir",  state.pumpA_Direction);
    SetText(home, "Txt_PumpB_Tube", state.pumpB_TubeSize);
    SetText(home, "Txt_PumpB_Dir",  state.pumpB_Direction);
    SetText(home, "Txt_Ratio",      state.flowRatio);
    UpdateTimerLabel();
}

    void HookButton(GameObject go, UnityAction onClick)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = graphic;

        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = highlightColor;
        cb.pressedColor     = highlightColor;
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    void SetText(Transform root, string name, string value)
    {
        Transform t = FindDeep(root, name);
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = value;
    }

    void Update()
{
    if (state == null || !state.timerRunning) return;
    state.timerSeconds += Time.deltaTime;
    UpdateTimerLabel();
}

void UpdateTimerLabel()
{
    Transform home = transform.Find(homeScreen);
    if (home == null || !home.gameObject.activeSelf) return;

    int total = Mathf.FloorToInt(state.timerSeconds);
    int h = total / 3600;
    int m = (total % 3600) / 60;
    int s = total % 60;
    SetText(home, "Txt_Timer", $"{h:00}:{m:00}:{s:00}");
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