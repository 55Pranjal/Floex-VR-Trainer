using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// CDM pole screen controller (Product B: sprite toggle only).
///
/// Wires 5 play/pause toggles and 4 reset buttons on Screen_CDM.
/// Each toggle is a 2-state sprite swap (default &lt;-&gt; toggled).
/// Each reset snaps its paired toggle back to its default sprite.
///
/// Timers do NOT count time. Per Product A/B scope, the trainer simulates
/// no physiology and no external state - the 00:00:00 displays stay
/// static. Button taps are familiarisation interactions only.
/// </summary>
public class CDMScreenController : MonoBehaviour
{
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

    [System.Serializable]
    public class ResetPair
    {
        [Tooltip("GameObject name of the reset button (e.g. Img_Timer1Reset).")]
        public string resetObjectName;

        [Tooltip("GameObject name of the paired toggle to snap back to default (e.g. Img_Timer1Play).")]
        public string pairedToggleObjectName;
    }

    [Header("Sprite toggles (5 total)")]
    [Tooltip("Drop Sprite references in the Inspector after the sprites are imported.")]
    public SpriteToggle[] toggles = new SpriteToggle[]
    {
        new SpriteToggle { objectName = "Img_LeftPlay"   },  // cardioplegia panel
        new SpriteToggle { objectName = "Img_Timer1Play" },
        new SpriteToggle { objectName = "Img_Timer2Play" },
        new SpriteToggle { objectName = "Img_Timer3Play" },
        new SpriteToggle { objectName = "Img_Speaker"    },  // bottom-right mute
    };

    [Header("Reset buttons (4 total) -> paired toggles")]
    public ResetPair[] resets = new ResetPair[]
    {
        new ResetPair { resetObjectName = "Img_LeftHistory",  pairedToggleObjectName = "Img_LeftPlay"   },
        new ResetPair { resetObjectName = "Img_Timer1Reset",  pairedToggleObjectName = "Img_Timer1Play" },
        new ResetPair { resetObjectName = "Img_Timer2Reset",  pairedToggleObjectName = "Img_Timer2Play" },
        new ResetPair { resetObjectName = "Img_Timer3Reset",  pairedToggleObjectName = "Img_Timer3Play" },
    };

    [Header("Visuals")]
    [Tooltip("Hover/press tint on all interactive buttons.")]
    public Color pressTint = new Color(0.85f, 0.85f, 0.85f);

    readonly Dictionary<string, Image>        toggleImages  = new Dictionary<string, Image>();
    readonly Dictionary<string, bool>         toggleStates  = new Dictionary<string, bool>();    // false = default, true = toggled
    readonly Dictionary<string, SpriteToggle> toggleConfigs = new Dictionary<string, SpriteToggle>();

    void Start()
    {
        // ScreenNavigator runs in Awake and turns off raycast on everything.
        // We re-enable on the 9 elements we make interactive here.

        foreach (SpriteToggle t in toggles)
            WireToggle(t);

        foreach (ResetPair r in resets)
            WireReset(r);
    }

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

    void WireReset(ResetPair r)
    {
        Transform tr = FindDeep(transform, r.resetObjectName);
        if (tr == null) { Debug.LogWarning($"[CDMScreenController] {r.resetObjectName} not found - reset skipped."); return; }

        Image img = tr.GetComponent<Image>();
        if (img == null) { Debug.LogWarning($"[CDMScreenController] {r.resetObjectName} missing Image."); return; }

        img.raycastTarget = true;

        Button btn = ConfigureButton(tr.gameObject, img);
        string pairedName = r.pairedToggleObjectName;
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => SnapToDefault(pairedName));
    }

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

    void SnapToDefault(string name)
    {
        if (!toggleImages.TryGetValue(name, out Image img))       return;
        if (!toggleConfigs.TryGetValue(name, out SpriteToggle t)) return;

        toggleStates[name] = false;
        if (t.defaultSprite != null) img.sprite = t.defaultSprite;
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