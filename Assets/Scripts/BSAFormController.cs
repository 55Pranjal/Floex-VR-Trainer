using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BSA &amp; Patient form controller (display-only).
///
/// Writes realistic patient defaults to the 9 Txt_X elements on screen open
/// so the screen looks fully populated as it would in a real OR setting.
/// Calculate computes BSA from the displayed weight and height; Cancel/Exit
/// clear the computed-field outputs.
///
/// Button behaviour:
///   Calculate  -> compute BSA from weight + height
///   OK         -> keep values, navigate home (= Save &amp; Exit)
///   EXIT       -> clear computed fields, navigate home (= Cancel)
///   Save&amp;Exit -> navigate home (wired by ScreenNavigator)
///   Cancel     -> clear computed (here) + navigate home (ScreenNavigator)
///
/// VR keyboard text-entry was attempted on Days 15-16. The combination of
/// Meta XR SDK v74, TMP_InputField (and legacy UI.InputField), and Meta's
/// PointableCanvasModule does not deliver pointer events to InputField
/// components on this canvas - cyclers and Buttons work, InputFields do
/// not. Display-only is a deliberate Product A/B scope decision:
/// perfusionists doing familiarisation training don't need to enter
/// patient data, only see the screen layout and react to clinical values.
/// See devlog/day-16.md for full rationale and reproducible findings.
/// </summary>
public class BSAFormController : MonoBehaviour
{
    [System.Serializable]
    public class Field
    {
        public string name;
        public string initialValue = "";
        // charLimit and contentType are retained but unused in display-only
        // mode. Kept so a future re-enable of typing doesn't require a
        // struct change (and so the Inspector serialization stays compatible
        // with any existing overrides).
        public int charLimit = 32;
        public TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
    }

    [Header("Computed (read by tutorial validator)")]
    public float TargetFlowLpm = 0f;   // last computed target flow; 0 = not yet calculated
    public bool HasTarget = false;     // true once Calculate produced a value

    [Tooltip("One entry per displayed field on this screen. Display target is Txt_{name}.")]
    public Field[] editableFields = new Field[]
    {
        new Field { name = "Name",      initialValue = "John Doe",   charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "ID",        initialValue = "P-001",      charLimit = 16, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Age",       initialValue = "45",         charLimit = 3,  contentType = TMP_InputField.ContentType.IntegerNumber },
        new Field { name = "Blood",     initialValue = "O+",         charLimit = 4,  contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Weight",    initialValue = "75",         charLimit = 6,  contentType = TMP_InputField.ContentType.DecimalNumber },
        new Field { name = "Height",     initialValue = "180",        charLimit = 3,  contentType = TMP_InputField.ContentType.IntegerNumber },
        new Field { name = "Surgeon",   initialValue = "Dr. Sharma", charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Anesth",    initialValue = "Dr. Patel",  charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Perfusion", initialValue = "Dr. Kumar",  charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Cardiac",   initialValue = "",           charLimit = 4,  contentType = TMP_InputField.ContentType.DecimalNumber },
    };

    [Tooltip("Read-only computed fields - Txt_{name} is written by Calculate.")]
    public string[] computedFields = new string[] { "BSA", "Target" };

    [Tooltip("Hover/press tint on Calculate button.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f); // FloEx coral

    [Tooltip("Screen to return to on OK / EXIT. Must match a ScreenNavigator screen name.")]
    public string homeScreen = "Screen_CDM";

    const string CalculateBoxName = "Box_Calculate";
    const string CancelBoxName    = "Box_Cancel";
    const string OKBoxName        = "Box_OK";
    const string ExitBoxName      = "Box_Exit";

    readonly Dictionary<string, TMP_Text> displays = new Dictionary<string, TMP_Text>();
    readonly Dictionary<string, TMP_Text> readOnly = new Dictionary<string, TMP_Text>();

    ScreenNavigator _navigator;

    void Start()
    {
        // ScreenNavigator runs in Awake and turns off raycast on everything.
        // In display-only mode we re-enable raycast only on the buttons we wire
        // (Calculate, OK, EXIT) - the field boxes intentionally stay non-interactive.

        _navigator = GetComponentInParent<ScreenNavigator>();
        if (_navigator == null)
            Debug.LogWarning("[BSAFormController] ScreenNavigator not found in parents - OK/EXIT navigation disabled.");

        foreach (Field f in editableFields)
        {
            Transform txt = FindDeep(transform, "Txt_" + f.name);
            if (txt == null)
            {
                Debug.LogWarning($"[BSAFormController] Txt_{f.name} not found - field skipped.");
                continue;
            }

            TMP_Text text = txt.GetComponent<TMP_Text>();
            if (text == null)
            {
                Debug.LogWarning($"[BSAFormController] Txt_{f.name} missing TMP_Text.");
                continue;
            }

            text.text = f.initialValue;
            displays[f.name] = text;
        }

        foreach (string n in computedFields)
        {
            Transform txt = FindDeep(transform, "Txt_" + n);
            if (txt == null)
            {
                Debug.LogWarning($"[BSAFormController] Txt_{n} not found (computed field).");
                continue;
            }
            TMP_Text t = txt.GetComponent<TMP_Text>();
            if (t != null)
            {
                readOnly[n] = t;
                t.text = "";  // start cleared - Calculate fills it in
            }
        }

        WireCalculate();
        WireCancel();
        WireOK();
        WireExit();
    }

    void WireCalculate()
    {
        Transform calc = FindDeep(transform, CalculateBoxName);
        if (calc == null) { Debug.LogWarning("[BSAFormController] Box_Calculate not found."); return; }

        Image g = calc.GetComponent<Image>();
        if (g != null) g.raycastTarget = true;

        Button btn = calc.GetComponent<Button>();
        if (btn == null) btn = calc.gameObject.AddComponent<Button>();
        btn.targetGraphic = g;

        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = highlightColor;
        cb.pressedColor     = highlightColor;
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(Calculate);
    }

    void WireCancel()
    {
        // Additive listener on top of whatever ScreenNavigator wired (the navigate).
        // Clears the computed-field outputs so the user can re-tap Calculate.
        Transform cancel = FindDeep(transform, CancelBoxName);
        Button btn = cancel != null ? cancel.GetComponent<Button>() : null;
        if (btn != null) btn.onClick.AddListener(ClearComputed);
    }

    void WireOK()
    {
        // OK = Save & Exit: keep current values, navigate home. ScreenNavigator
        // does not wire Box_OK, so we set it up fully here.
        HookNavButton(OKBoxName, GoHome);
    }

    void WireExit()
    {
        // EXIT = Cancel: clear computed fields, then navigate home.
        HookNavButton(ExitBoxName, () => { ClearComputed(); GoHome(); });
    }

    void HookNavButton(string boxName, UnityEngine.Events.UnityAction onClick)
    {
        Transform t = FindDeep(transform, boxName);
        if (t == null) { Debug.LogWarning($"[BSAFormController] {boxName} not found - button skipped."); return; }

        Image g = t.GetComponent<Image>();
        if (g != null) g.raycastTarget = true;   // ScreenNavigator turned this off

        Button btn = t.GetComponent<Button>();
        if (btn == null) btn = t.gameObject.AddComponent<Button>();
        btn.targetGraphic = g;

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

    void GoHome()
    {
        if (_navigator != null) _navigator.ShowScreen(homeScreen);
    }

   void Calculate()
    {
        float weight = ParseFloat("Weight");
        float height = ParseFloat("Height");
        if (weight <= 0f || height <= 0f) return;

        float bsa = Mathf.Sqrt((height * weight) / 3600f);
        if (readOnly.TryGetValue("BSA", out TMP_Text bsaTxt))
            bsaTxt.text = bsa.ToString("F2");

        // Target flow = cardiac index x BSA. CI entered on screen; default 2.5 if blank.
        // (Real machine derives CI from flow sensors; here the entered value is a TARGET
        // CI used to compute target flow — trainer simplification, KRB to confirm.)
        float ci = ParseFloat("Cardiac");
        if (ci <= 0f) ci = 2.5f;

        float targetFlow = ci * bsa;
        TargetFlowLpm = targetFlow;   // stash for validator
        HasTarget = true;
        if (readOnly.TryGetValue("Target", out TMP_Text targetTxt))
            targetTxt.text = targetFlow.ToString("F2");
    }

    float ParseFloat(string fieldName)
    {
        if (!displays.TryGetValue(fieldName, out TMP_Text t)) return 0f;
        return float.TryParse(t.text, out float v) ? v : 0f;
    }

    /// <summary>Clear all computed-field outputs. Hooked to Cancel and Exit.</summary>
    public void ClearComputed()
    {
        foreach (var kv in readOnly)
            kv.Value.text = "";
        TargetFlowLpm = 0f;
        HasTarget = false;
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