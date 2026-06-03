using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Screen-to-screen navigation for one pump head canvas.
/// Attach to PumpHead_NN_Canvas. Wires nav buttons on Screen1 (PUMP SELECT,
/// TUBE SIZE, DIRECTION) and refreshes Screen1's state labels on return.
/// Picker apply/cancel calls back through ShowScreen("Screen1").
/// </summary>
public class PumpHeadNavigator : MonoBehaviour
{
    [Tooltip("Shown on launch and when returning from a picker.")]
    public string homeScreen = "Screen1";

    [Tooltip("Tint applied to a button on hover + press.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f);

    [Tooltip("Pump head state — refreshed labels on Screen1 read from this.")]
    public PumpHeadState state;

    // Screen1 nav button name -> screen it opens
    static readonly Dictionary<string, string> NavMap = new Dictionary<string, string>
    {
        { "Btn_PumpSelect", "Pump_Select_1" },
        { "Btn_TubeSize",   "Tube_Size_1" },
        { "Btn_Direction",  "Direction" },
    };

    readonly List<GameObject> screens = new List<GameObject>();

    void Awake()
    {
        // Collect direct-child screens (the picker root names + Screen1).
        foreach (Transform child in transform)
            if (IsManagedScreen(child.name))
                screens.Add(child.gameObject);

        // Wire Screen1's three nav buttons.
        Transform screen1 = transform.Find(homeScreen);
        if (screen1 != null)
        {
            // Display-only by default; HookButton turns raycast back on per-element.
            foreach (Graphic g in screen1.GetComponentsInChildren<Graphic>(true))
                g.raycastTarget = false;

            foreach (KeyValuePair<string, string> entry in NavMap)
            {
                Transform t = FindDeep(screen1, entry.Key);
                if (t == null) continue;
                string target = entry.Value;
                HookButton(t.gameObject, () => ShowScreen(target));
            }
        }

        ShowScreen(homeScreen);
    }

    static bool IsManagedScreen(string n) =>
        n == "Screen1" || n == "Pump_Select_1" || n == "Tube_Size_1" || n == "Direction";

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

    /// <summary>Activates one screen, deactivates the rest. Refreshes Screen1
    /// labels when returning home (picker apply path).</summary>
    public void ShowScreen(string screenName)
    {
        foreach (GameObject screen in screens)
            screen.SetActive(screen.name == screenName);

        if (screenName == homeScreen) RefreshHomeLabels();
    }

    void RefreshHomeLabels()
    {
        if (state == null) return;
        Transform screen1 = transform.Find(homeScreen);
        if (screen1 == null) return;

        SetText(screen1, "Txt_PumpState",      "Pump: "      + state.GetPumpName());
        SetText(screen1, "Txt_TubeState",      "Tube: "      + state.GetTubeName());
        SetText(screen1, "Txt_DirectionState", "Direction: " + state.GetDirectionName());
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