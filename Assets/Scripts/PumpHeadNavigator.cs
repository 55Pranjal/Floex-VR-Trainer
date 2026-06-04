using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Per-canvas navigation for the pump head. Manages:
///  - Screen1 picker entry button (PUMP SELECT -> Pump_Select_1)
///  - 4-button nav strip on Screen1/2_1/3/4/5 (slot-swap pattern)
///  - Screen lifecycle (ShowScreen activates one, deactivates rest)
///  - Label refresh on return home (state mirrors)
/// Click pipeline: PointableCanvas -> GraphicRaycaster -> Unity Button.onClick.
/// </summary>
public class PumpHeadNavigator : MonoBehaviour
{
    [Tooltip("Default screen shown on launch and when returning from a picker.")]
    public string homeScreen = "Screen_PumpHead_Screen1";

    [Tooltip("Tint applied to a button on hover/press.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f);

    [Tooltip("Pump head state - refreshed labels on Screen1 read from this.")]
    public PumpHeadState state;

    // Screen1's picker entry buttons -> picker screens
    // Header: Img_PumpSelectBorder (badge); footer: Txt_Tube + Txt_Direction labels.
    // The footer text labels double as both state display (mirror state) AND clickable
    // picker entry. RefreshHomeLabels keeps their text current.
    static readonly Dictionary<string, string> Screen1PickerEntries = new Dictionary<string, string>
    {
        { "Img_PumpSelectBorder", "Pump_Select_1" },
        { "Txt_Tube",             "Tube_Size_1"   },
        { "Txt_Direction",        "Direction"     },
    };

    // Nav strip button name -> destination screen.
    // Each pump head screen has 4 of these (excludes its own label slot).
    static readonly Dictionary<string, string> NavStripMap = new Dictionary<string, string>
    {
        { "Btn_P1_Bg", "Screen_PumpHead_Screen1"   },
        { "Btn_P2_Bg", "Screen_PumpHead_Screen2_1" },
        { "Btn_P3_Bg", "Screen_PumpHead_Screen3"   },
        { "Btn_P4_Bg", "Screen_PumpHead_Screen4"   },
        { "Btn_P5_Bg", "Screen_PumpHead_Screen5"   },
    };

    static readonly HashSet<string> ManagedScreens = new HashSet<string>
    {
        "Screen_PumpHead_Screen1", "Screen_PumpHead_Screen2_1",
        "Screen_PumpHead_Screen3", "Screen_PumpHead_Screen4",
        "Screen_PumpHead_Screen5",
        "Pump_Select_1", "Tube_Size_1", "Direction",
    };

    readonly List<GameObject> screens = new List<GameObject>();

    void Awake()
    {
        // Collect managed direct-child screens
        foreach (Transform child in transform)
            if (ManagedScreens.Contains(child.name))
                screens.Add(child.gameObject);

        // Home (Screen1): blanket-disable raycast, then re-enable specific buttons
        WireHomeScreen();

        // Other pump head screens: only enable nav strip buttons; leave the rest
        // alone so future per-screen controllers (Screen2_1Controller etc.) own them
        WireNavStripOnly("Screen_PumpHead_Screen2_1");
        WireNavStripOnly("Screen_PumpHead_Screen3");
        WireNavStripOnly("Screen_PumpHead_Screen4");
        WireNavStripOnly("Screen_PumpHead_Screen5");

        ShowScreen(homeScreen);
    }

    void Update()
    {
        // Session timer ticks here so it survives screen navigation.
        if (state != null && state.timerRunning)
            state.timerSeconds += Time.deltaTime;
    }

    void WireHomeScreen()
    {
        Transform home = transform.Find(homeScreen);
        if (home == null) return;

        // Display-only by default; HookButton turns raycast back on per-element
        foreach (Graphic g in home.GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        // Nav strip
        WireNavStripButtons(home);

        // Picker entries (PUMP SELECT etc.)
        foreach (KeyValuePair<string, string> entry in Screen1PickerEntries)
        {
            Transform t = FindDeep(home, entry.Key);
            if (t == null) continue;
            string target = entry.Value;
            HookButton(t.gameObject, () => ShowScreen(target));
        }
    }

    void WireNavStripOnly(string screenName)
    {
        Transform screen = transform.Find(screenName);
        if (screen == null) return;
        WireNavStripButtons(screen);
    }

    /// <summary>Wire the 4 nav strip buttons on a screen. Called at canvas Awake
    /// AND by each screen controller's OnEnable (so the blanket raycast disable
    /// they do doesn't leave the nav strip unclickable).</summary>
    public void WireNavStripButtons(Transform screen)
    {
        foreach (KeyValuePair<string, string> entry in NavStripMap)
        {
            Transform bgT = FindDeep(screen, entry.Key);
            if (bgT == null) continue;  // slot not on this screen (current screen's button)

            string target = entry.Value;
            HookButton(bgT.gameObject, () => ShowScreen(target));

            // Disable raycast on the text overlay so the click lands on the bg
            string textName = entry.Key.Replace("_Bg", "_Text");
            Transform textT = FindDeep(screen, textName);
            if (textT != null)
            {
                Graphic tg = textT.GetComponent<Graphic>();
                if (tg != null) tg.raycastTarget = false;
            }
        }
    }

    /// <summary>Activates one screen, deactivates the rest. Refreshes Screen1
    /// labels when returning home (picker apply path).</summary>
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
            Debug.LogWarning($"[PumpHeadNavigator] No screen named '{screenName}' under " +
                             $"{gameObject.name}. Check the homeScreen field and child names.");
        }

        if (screenName == homeScreen) RefreshHomeLabels();
    }

    void RefreshHomeLabels()
    {
        if (state == null) return;
        Transform home = transform.Find(homeScreen);
        if (home == null) return;

        // Badge in header mirrors current pump (e.g. "Arterial").
        // Footer labels mirror current tube + direction (and are clickable picker entries).
        // Direction renders as CW/CCW in the footer; PumpHeadState keeps Forward/Reverse semantics internally.
        SetText(home, "Txt_PumpSelect", state.GetPumpName());
        SetText(home, "Txt_Tube",       state.GetTubeName());
        SetText(home, "Txt_Direction",  state.directionForward ? "CW" : "CCW");
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