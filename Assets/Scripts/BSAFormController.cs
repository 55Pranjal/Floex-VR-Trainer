using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BSA &amp; Patient form controller. Attaches TMP_InputField to 9 editable
/// Box_X elements at runtime, using each matching Txt_X as the display target.
/// Tapping a field gives it focus; in editor a physical keyboard works, in VR
/// the Quest system keyboard (or OVRVirtualKeyboard if installed) appears.
///
/// Calculate computes BSA = sqrt(height * weight / 3600) and writes the result
/// to Txt_BSA. Cardiac Index and Target Flow are left empty - their firmware
/// formulas depend on cardioplegia output data, which the trainer doesn't have.
///
/// Cancel reverts every editable field to the value it had when the screen
/// last became active. Save &amp; Exit just navigates (ScreenNavigator does that)
/// and leaves typed values in place.
/// </summary>
public class BSAFormController : MonoBehaviour
{
    [System.Serializable]
    public class Field
    {
        public string name;
        public string initialValue = "";
        public int    charLimit    = 32;
        public TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard;
    }

    [Tooltip("One entry per editable field on this screen. Element lookup is Box_{name} + Txt_{name}.")]
    public Field[] editableFields = new Field[]
    {
        new Field { name = "Name",      initialValue = "John Doe",   charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "ID",        initialValue = "P-001",      charLimit = 16, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Age",       initialValue = "45",         charLimit = 3,  contentType = TMP_InputField.ContentType.IntegerNumber },
        new Field { name = "Blood",     initialValue = "O+",         charLimit = 4,  contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Weight",    initialValue = "75",         charLimit = 6,  contentType = TMP_InputField.ContentType.DecimalNumber },
        new Field { name = "Hight",     initialValue = "180",        charLimit = 3,  contentType = TMP_InputField.ContentType.IntegerNumber },
        new Field { name = "Surgeon",   initialValue = "Dr. Sharma", charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Anesth",    initialValue = "Dr. Patel",  charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
        new Field { name = "Perfusion", initialValue = "Dr. Kumar",  charLimit = 30, contentType = TMP_InputField.ContentType.Standard },
    };

    [Tooltip("Read-only computed fields - Txt_{name} is written by Calculate.")]
    public string[] computedFields = new string[] { "Cardiac", "BSA", "Target" };

    [Tooltip("Hover/press tint on Calculate button.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f); // FloEx coral

    [Tooltip("Selection background colour inside InputFields.")]
    public Color selectionColor = new Color(0.906f, 0.412f, 0.427f, 0.5f);

    [Tooltip("Caret colour.")]
    public Color caretColor = new Color(0.906f, 0.412f, 0.427f, 1.0f);

    const string CalculateBoxName = "Box_Calculate";
    const string CancelBoxName    = "Box_Cancel";

    readonly Dictionary<string, TMP_InputField> inputs   = new Dictionary<string, TMP_InputField>();
    readonly Dictionary<string, TMP_Text>       readOnly = new Dictionary<string, TMP_Text>();
    readonly Dictionary<string, string>         snapshot = new Dictionary<string, string>();

    void OnEnable()
    {
        // First activation: inputs dict is empty, Start will handle snapshot.
        // Re-activation: refresh snapshot from current text so Cancel reverts
        // to whatever the user sees right now, not the original initials.
        if (inputs.Count == 0) return;
        foreach (var kv in inputs)
            snapshot[kv.Key] = kv.Value.text;
    }

    void Start()
    {
        // ScreenNavigator runs in Awake and turns off raycast on everything.
        // We re-enable on the boxes we actually need clickable.

        foreach (Field f in editableFields)
        {
            Transform box = FindDeep(transform, "Box_" + f.name);
            Transform txt = FindDeep(transform, "Txt_" + f.name);
            if (box == null || txt == null)
            {
                Debug.LogWarning($"[BSAFormController] Box_{f.name} or Txt_{f.name} not found on {name} - field skipped.");
                continue;
            }

            Image  img    = box.GetComponent<Image>();
            TMP_Text text = txt.GetComponent<TMP_Text>();
            if (img == null || text == null)
            {
                Debug.LogWarning($"[BSAFormController] Box_{f.name} missing Image or Txt_{f.name} missing TMP_Text.");
                continue;
            }

            img.raycastTarget = true;

            TMP_InputField input = box.GetComponent<TMP_InputField>();
            if (input == null) input = box.gameObject.AddComponent<TMP_InputField>();

            input.textComponent  = text;
            input.targetGraphic  = img;
            input.text           = f.initialValue;
            input.characterLimit = f.charLimit;
            input.contentType    = f.contentType;
            input.lineType       = TMP_InputField.LineType.SingleLine;
            input.selectionColor = selectionColor;
            input.caretColor     = caretColor;
            input.customCaretColor = true;

            inputs[f.name]   = input;
            snapshot[f.name] = input.text;
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
            if (t != null) readOnly[n] = t;
        }

        WireCalculate();
        WireCancel();
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
        Transform cancel = FindDeep(transform, CancelBoxName);
        Button btn = cancel != null ? cancel.GetComponent<Button>() : null;
        if (btn != null) btn.onClick.AddListener(RevertToSnapshot);
    }

    void Calculate()
    {
        float weight = ParseFloat("Weight");
        float height = ParseFloat("Hight");
        if (weight <= 0f || height <= 0f) return;

        float bsa = Mathf.Sqrt((height * weight) / 3600f);
        if (readOnly.TryGetValue("BSA", out TMP_Text bsaTxt))
            bsaTxt.text = bsa.ToString("F2");

        // Cardiac Index and Target Flow intentionally left empty.
        // Firmware computes CI = cardioplegia1_Out / BSA, which depends on
        // physiology data the trainer does not simulate (Product A scope-lock).
    }

    float ParseFloat(string fieldName)
    {
        if (!inputs.TryGetValue(fieldName, out TMP_InputField input)) return 0f;
        return float.TryParse(input.text, out float v) ? v : 0f;
    }

    /// <summary>Restore every editable field to its screen-open value. Hooked to Cancel.</summary>
    public void RevertToSnapshot()
    {
        foreach (var kv in inputs)
            if (snapshot.TryGetValue(kv.Key, out string s))
                kv.Value.text = s;

        // Clear any computed-field output too - it doesn't survive Cancel
        // since the inputs that produced it just reverted.
        foreach (var kv in readOnly)
            kv.Value.text = "";
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