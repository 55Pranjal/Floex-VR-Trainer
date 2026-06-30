using UnityEngine;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Opens the Quest system keyboard on poke/click and writes the result into a TMP text
/// label — bypassing TMP_InputField focus (which the Meta pointable pipeline doesn't
/// reliably trigger on v74).
///
/// Setup: put this on the field's Box (e.g. Box_Height). It uses the Box's Button onClick
/// (or add one) to open the keyboard, and writes into the assigned TMP_Text (Txt_Height).
///
/// No TMP_InputField needed — plain TMP text + this script.
/// </summary>
[RequireComponent(typeof(Button))]
public class SystemKeyboardField : MonoBehaviour
{
    [Tooltip("The TMP text label that displays this field's value (e.g. Txt_Height).")]
    public TMP_Text targetText;

    [Tooltip("Numbers only (height/weight). Off = full text.")]
    public bool numericOnly = true;

    [Tooltip("Optional placeholder shown when empty.")]
    public string placeholder = "";

    TouchScreenKeyboard _keyboard;
    bool _listening;

    public string Value { get; private set; } = "";

    void Start()
    {
        var btn = GetComponent<Button>();
        btn.onClick.AddListener(OpenKeyboard);

        // ScreenNavigator.Awake blanket-disables raycasts; re-enable ours in Start
        // (after Awake) so the poke can land on this field.
        var g = GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;
    }

    public void OpenKeyboard()
    {
        TouchScreenKeyboardType type = numericOnly
            ? TouchScreenKeyboardType.NumbersAndPunctuation
            : TouchScreenKeyboardType.Default;

        // text, type, autocorrect, multiline, secure, alert, placeholder
        _keyboard = TouchScreenKeyboard.Open(Value, type, false, false, false, false, placeholder);
        _listening = true;
    }

    void Update()
    {
        if (!_listening || _keyboard == null) return;

        // Live-update the label as the user types.
        Value = _keyboard.text;
        if (targetText != null) targetText.text = Value;

        if (TouchScreenKeyboard.visible == false &&
            (_keyboard.status == TouchScreenKeyboard.Status.Done ||
             _keyboard.status == TouchScreenKeyboard.Status.Canceled ||
             _keyboard.status == TouchScreenKeyboard.Status.LostFocus))
        {
            _listening = false;
            _keyboard = null;
        }
    }

    /// <summary>Parsed numeric value, or 0 if empty/invalid.</summary>
    public float NumericValue =>
        float.TryParse(Value, out float v) ? v : 0f;
}