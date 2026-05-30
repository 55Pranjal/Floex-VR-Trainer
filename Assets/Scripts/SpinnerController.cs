using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Generic up/down spinner state for a console screen (Product B scope:
/// button clicking + values changing, no device behaviour).
///
/// Attach to a screen root (e.g. Screen_DateTime) and list its fields. For a
/// field named "Hour" it wires Box_HourUp / Box_HourDown to step the value and
/// writes the result into Txt_HourVal — all by naming convention, so the same
/// component drops onto any screen with a different field list.
///
/// Cooperates with ScreenNavigator: that script turns every graphic's raycast
/// OFF, so this one turns the spinner boxes back ON. It does so in Start, which
/// always runs after ScreenNavigator's Awake, so the re-enable can't be clobbered.
/// </summary>
public class SpinnerController : MonoBehaviour
{
    [System.Serializable]
    public class Field
    {
        public string name;          // "Hour" -> Box_HourUp / Txt_HourVal / Box_HourDown
        public int min;
        public int max = 59;
        public int step = 1;
        public int start;
        public bool wrap = true;     // past max wraps to min, below min wraps to max
    }

    [Tooltip("One entry per spinner. Element names are derived from each field name.")]
    public Field[] fields = new Field[]
    {
        new Field { name = "Day",    min = 1,    max = 31,   start = 1,    step = 1, wrap = true  },
        new Field { name = "Month",  min = 1,    max = 12,   start = 1,    step = 1, wrap = true  },
        new Field { name = "Year",   min = 2000, max = 2099, start = 2000, step = 1, wrap = false },
        new Field { name = "Hour",   min = 0,    max = 23,   start = 0,    step = 1, wrap = true  },
        new Field { name = "Minute", min = 0,    max = 59,   start = 0,    step = 1, wrap = true  },
        new Field { name = "Second", min = 0,    max = 59,   start = 0,    step = 1, wrap = true  },
    };

    [Tooltip("Hover / press tint on the +/- boxes.")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f); // FloEx coral

    const string CancelBoxName = "Box_Cancel";

    readonly Dictionary<string, int> values   = new Dictionary<string, int>(); // live value
    readonly Dictionary<string, int> snapshot = new Dictionary<string, int>(); // value at screen-open, for Cancel
    readonly Dictionary<string, TMP_Text> labels = new Dictionary<string, TMP_Text>();

    void Awake()
    {
        // Data only — no scene lookups yet (ScreenNavigator may not have run).
        foreach (Field f in fields)
            values[f.name] = Mathf.Clamp(f.start, f.min, f.max);
    }

    void OnEnable()
    {
        // Screen opened: remember current values so Cancel can restore them.
        foreach (Field f in fields)
            snapshot[f.name] = values.TryGetValue(f.name, out int v) ? v : f.start;
    }

    void Start()
    {
        // Runs after ScreenNavigator.Awake, so re-enabling raycasts sticks.
        foreach (Field f in fields)
        {
            TMP_Text lbl = FindText("Txt_" + f.name + "Val");
            if (lbl != null) labels[f.name] = lbl;

            Field cf = f;   // capture for the closures
            HookBox("Box_" + f.name + "Up",   () => Step(cf, +cf.step));
            HookBox("Box_" + f.name + "Down", () => Step(cf, -cf.step));

            Refresh(f.name);
        }

        // Cancel discards this session's edits. Navigation to CDM is already
        // wired by ScreenNavigator; we only ADD a revert (it runs after the
        // navigate, restoring values on the now-hidden screen for next open).
        Transform cancel = FindDeep(transform, CancelBoxName);
        Button cancelBtn = cancel != null ? cancel.GetComponent<Button>() : null;
        if (cancelBtn != null) cancelBtn.onClick.AddListener(RevertToSnapshot);
    }

    void Step(Field f, int delta)
    {
        int v = values[f.name] + delta;
        if (f.wrap)
        {
            int span = f.max - f.min + 1;
            v = f.min + ((v - f.min) % span + span) % span;   // safe modulo for negatives
        }
        else
        {
            v = Mathf.Clamp(v, f.min, f.max);
        }
        values[f.name] = v;
        Refresh(f.name);
    }

    /// <summary>Restore values to the screen-open snapshot. Hooked to Cancel.</summary>
    public void RevertToSnapshot()
    {
        foreach (Field f in fields)
            if (snapshot.TryGetValue(f.name, out int v))
            {
                values[f.name] = v;
                Refresh(f.name);
            }
    }

    void Refresh(string fieldName)
    {
        if (labels.TryGetValue(fieldName, out TMP_Text lbl))
            lbl.text = values[fieldName].ToString();
    }

    void HookBox(string boxName, UnityAction onClick)
    {
        Transform t = FindDeep(transform, boxName);
        if (t == null) { Debug.LogWarning($"[SpinnerController] '{boxName}' not found on {name}"); return; }

        Graphic g = t.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;          // ScreenNavigator turned this off

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

    TMP_Text FindText(string n)
    {
        Transform t = FindDeep(transform, n);
        return t != null ? t.GetComponent<TMP_Text>() : null;
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