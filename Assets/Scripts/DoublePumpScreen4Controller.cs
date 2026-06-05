using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Fine Calibration screen for the double pump head canvas. 3 counters
/// (FC/Tube1/Tube2, each 0..200 with wrap) plus an on/off toggle.
/// Apply commits to DoublePumpHeadState; Cancel reverts; nav-away discards.
/// </summary>
public class DoublePumpScreen4Controller : MonoBehaviour
{
    [Header("Wiring")]
    public DoublePumpHeadState state;
    public DoublePumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel.")]
    public string returnScreen = "Screen_PumpHead04_Screen1";

    [Header("Toggle sprites")]
    [Tooltip("Drag Assets/Textures/UI/toggle_on.png here")]
    public Sprite toggleOnSprite;
    [Tooltip("Drag Assets/Textures/UI/toggle_off.png here")]
    public Sprite toggleOffSprite;

    class Spinner
    {
        public string upBtn;
        public string downBtn;
        public string counterTxt;
        public int value;
        public void Inc() { value = (value + 1) % 201; }
        public void Dec() { value = (value - 1 + 201) % 201; }
    }

    readonly Spinner fc    = new Spinner { upBtn = "Img_BtnUpFC",    downBtn = "Img_BtnDownFC",    counterTxt = "Txt_CounterFC"    };
    readonly Spinner tube1 = new Spinner { upBtn = "Img_BtnUpTube1", downBtn = "Img_BtnDownTube1", counterTxt = "Txt_CounterTube1" };
    readonly Spinner tube2 = new Spinner { upBtn = "Img_BtnUpTube2", downBtn = "Img_BtnDownTube2", counterTxt = "Txt_CounterTube2" };

    bool tempFcOn;

    void OnEnable()
    {
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireSpinner(fc);
        WireSpinner(tube1);
        WireSpinner(tube2);
        WireToggle();
        WireActions();

        LoadFromState();
        RefreshUI();

        if (navigator != null) navigator.WireNavStripButtons(transform);
    }

    void WireSpinner(Spinner s)
    {
        Transform up = FindDeep(transform, s.upBtn);
        if (up != null) HookButton(up.gameObject, () => { s.Inc(); UpdateCounter(s); });

        Transform down = FindDeep(transform, s.downBtn);
        if (down != null) HookButton(down.gameObject, () => { s.Dec(); UpdateCounter(s); });
    }

    void WireToggle()
    {
        Transform t = FindDeep(transform, "Img_ToggleFC");
        if (t != null) HookButton(t.gameObject, OnToggle);
    }

    void WireActions()
    {
        Transform cancel = FindDeep(transform, "Img_BtnCancel");
        if (cancel != null) HookButton(cancel.gameObject, OnCancel);

        Transform apply = FindDeep(transform, "Img_BtnApply");
        if (apply != null) HookButton(apply.gameObject, OnApply);
    }

    void LoadFromState()
    {
        if (state == null) return;
        fc.value    = state.fineCalibration;
        tube1.value = state.tube1;
        tube2.value = state.tube2;
        tempFcOn    = state.fineCalibrationOn;
    }

    void OnToggle()
    {
        tempFcOn = !tempFcOn;
        UpdateToggleSprite();
    }

    void OnApply()
    {
        if (state != null)
        {
            state.fineCalibration   = fc.value;
            state.tube1             = tube1.value;
            state.tube2             = tube2.value;
            state.fineCalibrationOn = tempFcOn;
        }
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnCancel()
    {
        LoadFromState();
        RefreshUI();
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void RefreshUI()
    {
        UpdateCounter(fc);
        UpdateCounter(tube1);
        UpdateCounter(tube2);
        UpdateToggleSprite();
    }

    void UpdateCounter(Spinner s)
    {
        Transform t = FindDeep(transform, s.counterTxt);
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = s.value.ToString();
    }

    void UpdateToggleSprite()
    {
        Transform t = FindDeep(transform, "Img_ToggleFC");
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img == null) return;
        Sprite target = tempFcOn ? toggleOnSprite : toggleOffSprite;
        if (target != null) img.sprite = target;
    }

    void HookButton(GameObject go, UnityAction onClick)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = graphic;
        btn.transition = Selectable.Transition.None;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
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