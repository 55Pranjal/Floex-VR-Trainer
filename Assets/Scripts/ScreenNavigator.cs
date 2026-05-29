using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Screen-to-screen navigation for the Floex console.
/// Attach to the Canvas. At launch it finds each screen's own nav rows + home
/// icon by name, turns them into Buttons at runtime, and wires them to swap
/// which screen is active. The screen JSON and ScreenBuilder stay untouched
/// (static only) — every bit of navigation behaviour lives here.
/// </summary>
public class ScreenNavigator : MonoBehaviour
{
    [Tooltip("Shown on launch and when the Home icon is tapped.")]
    public string homeScreen = "Screen_CDM";

    [Tooltip("Tint applied to a nav row / home icon on hover + press.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f); // FloEx coral E7696D

    // nav-row GameObject name -> screen it opens
    static readonly Dictionary<string, string> NavMap = new Dictionary<string, string>
    {
        { "Txt_NavBSA",      "Screen_BSA" },
        { "Txt_NavBubble",   "Screen_BubbleSensor" },
        { "Txt_NavLevel",    "Screen_LevelSensor" },
        { "Txt_NavPressure", "Screen_PressureSensor" },
        { "Txt_NavTemp",     "Screen_TemperatureSensor" },
        { "Txt_NavCardio",   "Screen_Cardioplegia" },
        { "Txt_NavTimer",    "Screen_Timer" },
        { "Txt_NavSystem",   "Screen_SystemSetting" },
    };

    const string HomeIconName = "Img_Home";

    readonly List<GameObject> screens = new List<GameObject>();

    void Awake()
    {
        // 1. Collect every managed screen (direct children named Screen_*).
        foreach (Transform child in transform)
            if (child.name.StartsWith("Screen_"))
                screens.Add(child.gameObject);

        // 2. Wire each screen's OWN nav rows + home icon (names repeat per screen,
        //    so we search inside each screen rather than globally).
        foreach (GameObject screen in screens)
            WireScreen(screen.transform);

        // 3. Launch on home; this also forces exactly one screen visible,
        //    clearing the current overlap where several are active at once.
        ShowScreen(homeScreen);
    }

    void WireScreen(Transform screenRoot)
    {
        foreach (KeyValuePair<string, string> entry in NavMap)
        {
            Transform row = FindDeep(screenRoot, entry.Key);
            if (row == null) continue;          // screen may not contain this row
            string target = entry.Value;        // capture per row for the closure
            HookButton(row.gameObject, () => ShowScreen(target));
        }

        // Home icon is absent on the home screen itself — null is fine.
        Transform home = FindDeep(screenRoot, HomeIconName);
        if (home != null)
            HookButton(home.gameObject, () => ShowScreen(homeScreen));
    }

    void HookButton(GameObject go, UnityAction onClick)
    {
        Graphic graphic = go.GetComponent<Graphic>();   // TMP text and Image both qualify
        if (graphic != null) graphic.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = graphic;

        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;          // multiplies with the designed colour = no change at rest
        cb.highlightedColor = highlightColor;
        cb.pressedColor     = highlightColor;
        cb.selectedColor    = Color.white;          // don't leave a row stuck tinted after a tap
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    /// <summary>Activates one screen, deactivates the rest. Public so a modal
    /// (e.g. Date &amp; Time) or other code can drive navigation later.</summary>
    public void ShowScreen(string screenName)
    {
        foreach (GameObject screen in screens)
            screen.SetActive(screen.name == screenName);
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