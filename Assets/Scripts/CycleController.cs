using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class CyclerController : MonoBehaviour
{
    [System.Serializable]
    public class State
    {
        public string label = "";       // optional, written into Txt_{name}Label if present
        public Color  tint  = Color.white;
        public Sprite sprite;           // OPTIONAL: if set on an Image, swaps the sprite (tint ignored)
    }

    [System.Serializable]
    public class Field
    {
        public string  name;
        public int     start = 0;
        public State[] states;
    }

    [Tooltip("One entry per cycler control on this screen. Element names are derived from each field name.")]
    public Field[] fields = new Field[]
    {
        new Field { name = "T1Alarm", start = 0, states = DefaultAlarmStates() },
        new Field { name = "T2Alarm", start = 0, states = DefaultAlarmStates() },
        new Field { name = "T3Alarm", start = 0, states = DefaultAlarmStates() },
        new Field { name = "T4Alarm", start = 0, states = DefaultAlarmStates() },
    };

    [Tooltip("Hover/press tint on the clickable Box (separate from per-state tint applied to the Img).")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f);

    const string CancelBoxName = "Box_Cancel";

    readonly Dictionary<string, int>      indices  = new Dictionary<string, int>();
    readonly Dictionary<string, int>      snapshot = new Dictionary<string, int>();
    readonly Dictionary<string, Graphic>  visuals  = new Dictionary<string, Graphic>();
    readonly Dictionary<string, TMP_Text> labels   = new Dictionary<string, TMP_Text>();

    static State[] DefaultAlarmStates() => new State[]
    {
        new State { label = "Low",    tint = new Color(0.40f, 0.70f, 0.90f) },
        new State { label = "Medium", tint = new Color(0.98f, 0.73f, 0.09f) },
        new State { label = "High",   tint = new Color(0.906f, 0.412f, 0.427f) },
    };

    void Awake()
    {
        foreach (Field f in fields)
        {
            int i = f.start;
            if (f.states != null && f.states.Length > 0)
                i = Mathf.Clamp(i, 0, f.states.Length - 1);
            indices[f.name] = i;
        }
    }

    void OnEnable()
    {
        foreach (Field f in fields)
            snapshot[f.name] = indices.TryGetValue(f.name, out int v) ? v : f.start;
    }

    void Start()
    {
        foreach (Field f in fields)
        {
            Transform imgT = FindDeep(transform, "Img_" + f.name);
            if (imgT != null)
            {
                Graphic g = imgT.GetComponent<Graphic>();
                if (g != null) visuals[f.name] = g;
            }
            else
            {
                Debug.LogWarning($"[CyclerController] 'Img_{f.name}' not found on {name} — no visual feedback for this field.");
            }

            Transform txt = FindDeep(transform, "Txt_" + f.name + "Label");
            if (txt != null)
            {
                TMP_Text t = txt.GetComponent<TMP_Text>();
                if (t != null) labels[f.name] = t;
            }

            Field cf = f;

            Transform click = FindDeep(transform, "Box_" + f.name);
            bool isFallback = false;
            if (click == null) { click = imgT; isFallback = true; }

            if (click != null) HookClickable(click, () => Advance(cf), isFallback);
            else Debug.LogWarning($"[CyclerController] No Box_{f.name} or Img_{f.name} on {name} — field unwired.");

            Refresh(f.name);
        }

        Transform cancel = FindDeep(transform, CancelBoxName);
        Button cancelBtn = cancel != null ? cancel.GetComponent<Button>() : null;
        if (cancelBtn != null) cancelBtn.onClick.AddListener(RevertToSnapshot);
    }

    void Advance(Field f)
    {
        if (f.states == null || f.states.Length == 0) return;
        indices[f.name] = (indices[f.name] + 1) % f.states.Length;
        Refresh(f.name);
    }

    public void RevertToSnapshot()
    {
        foreach (Field f in fields)
            if (snapshot.TryGetValue(f.name, out int v))
            {
                indices[f.name] = v;
                Refresh(f.name);
            }
    }

    void Refresh(string fieldName)
    {
        Field f = System.Array.Find(fields, x => x.name == fieldName);
        if (f == null || f.states == null || f.states.Length == 0) return;
        State s = f.states[indices[fieldName]];

        if (visuals.TryGetValue(fieldName, out Graphic g))
        {
            // Sprite swap takes precedence over tint, if both are usable.
            Image img = g as Image;
            if (s.sprite != null && img != null)
            {
                img.sprite = s.sprite;
                img.color  = Color.white;   // neutralize any prior tint
            }
            else
            {
                g.color = s.tint;
            }
        }
        if (labels.TryGetValue(fieldName, out TMP_Text t)) t.text = s.label;
    }

    void HookClickable(Transform t, UnityAction onClick, bool isFallback)
    {
        Graphic g = t.GetComponent<Graphic>();
        if (g != null) g.raycastTarget = true;

        Button btn = t.GetComponent<Button>();
        if (btn == null) btn = t.gameObject.AddComponent<Button>();
        btn.targetGraphic = g;

        if (isFallback)
        {
            btn.transition = Selectable.Transition.None;
        }
        else
        {
            ColorBlock cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = highlightColor;
            cb.pressedColor     = highlightColor;
            cb.selectedColor    = Color.white;
            cb.fadeDuration     = 0.05f;
            btn.colors = cb;
            btn.transition = Selectable.Transition.ColorTint;
        }

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
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