using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Pulse Mode Configuration for the double pump head canvas. 4 counters
/// (PF/PW/BF/PFL, each 0..200 with wrap) plus an on/off toggle for pulse mode.
/// Apply commits to DoublePumpHeadState; Cancel reverts; nav-away discards.
/// </summary>
public class DoublePumpScreen3Controller : MonoBehaviour
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

    readonly Spinner pf  = new Spinner { upBtn = "Img_BtnUpPF",  downBtn = "Img_BtnDownPF",  counterTxt = "Txt_CounterPF"  };
    readonly Spinner pw  = new Spinner { upBtn = "Img_BtnUpPW",  downBtn = "Img_BtnDownPW",  counterTxt = "Txt_CounterPW"  };
    readonly Spinner bf  = new Spinner { upBtn = "Img_BtnUpBF",  downBtn = "Img_BtnDownBF",  counterTxt = "Txt_CounterBF"  };
    readonly Spinner pfl = new Spinner { upBtn = "Img_BtnUpPFL", downBtn = "Img_BtnDownPFL", counterTxt = "Txt_CounterPFL" };

    bool tempPulseMode;

    void OnEnable()
    {
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireSpinner(pf);
        WireSpinner(pw);
        WireSpinner(bf);
        WireSpinner(pfl);
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
        Transform t = FindDeep(transform, "Img_ToggleOnOff");
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
        pf.value  = state.pulseFrequency;
        pw.value  = state.pulseWidth;
        bf.value  = state.baseFlow;
        pfl.value = state.peakFlowLimit;
        tempPulseMode = state.pulseModeOn;
    }

    void OnToggle()
    {
        tempPulseMode = !tempPulseMode;
        UpdateToggleSprite();
    }

    void OnApply()
    {
        if (state != null)
        {
            state.pulseFrequency = pf.value;
            state.pulseWidth     = pw.value;
            state.baseFlow       = bf.value;
            state.peakFlowLimit  = pfl.value;
            state.pulseModeOn    = tempPulseMode;
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
        UpdateCounter(pf);
        UpdateCounter(pw);
        UpdateCounter(bf);
        UpdateCounter(pfl);
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
        Transform t = FindDeep(transform, "Img_ToggleOnOff");
        if (t == null) return;
        Image img = t.GetComponent<Image>();
        if (img == null) return;
        Sprite target = tempPulseMode ? toggleOnSprite : toggleOffSprite;
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