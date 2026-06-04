using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Master-Slave configuration screen. 6 exclusive options (firmware order:
/// Nil=0, Arterial=1, Cardio=2, Vent=3, Suct1=4, Suct2=5) plus a 0..200 flow
/// spinner. Same activate/select/apply/cancel pattern as ExclusivePickerController.
/// Apply commits temps to PumpHeadState and navigates home. Cancel/nav-away discards.
/// </summary>
public class Screen2_1Controller : MonoBehaviour
{
    [Header("Wiring")]
    public PumpHeadState state;
    public PumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel.")]
    public string returnScreen = "Screen_PumpHead_Screen1";

    // Index = firmware option value
    static readonly string[] OptionButtons =
    {
        "Img_BtnNil",          // 0
        "Img_BtnArterial",     // 1
        "Img_BtnCardioplegia", // 2
        "Img_BtnVent",         // 3
        "Img_BtnSuction1",     // 4
        "Img_BtnSuction2",     // 5
    };

    int tempMaster;
    int tempFlow;

    void OnEnable()
    {
        // Display-only by default; HookButton re-enables wired elements.
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireOptions();
        WireSpinner();
        WireActions();
        LoadFromState();
        Highlight(tempMaster);
        UpdateCounterText();

        // Re-wire nav strip (our blanket disable above turned it off).
        if (navigator != null) navigator.WireNavStripButtons(transform);
    }

    void WireOptions()
    {
        for (int i = 0; i < OptionButtons.Length; i++)
        {
            Transform t = FindDeep(transform, OptionButtons[i]);
            if (t == null) continue;
            int captured = i;
            HookButton(t.gameObject, () => OnOptionSelected(captured));
        }
    }

    void WireSpinner()
    {
        Transform up = FindDeep(transform, "Img_BtnUp");
        if (up != null) HookButton(up.gameObject, OnIncrement);

        Transform down = FindDeep(transform, "Img_BtnDown");
        if (down != null) HookButton(down.gameObject, OnDecrement);
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
        if (state == null) { tempMaster = 0; tempFlow = 0; return; }
        tempMaster = state.masterIndex;
        tempFlow   = state.flowPercent;
    }

    void OnOptionSelected(int idx)
    {
        tempMaster = idx;
        Highlight(idx);
    }

    // 0..200 with wrap on both ends (matches firmware unsigned counter).
    void OnIncrement()
    {
        tempFlow = (tempFlow + 1) % 201;
        UpdateCounterText();
    }

    void OnDecrement()
    {
        tempFlow = (tempFlow - 1 + 201) % 201;
        UpdateCounterText();
    }

    void OnApply()
    {
        if (state != null)
        {
            state.masterIndex = tempMaster;
            state.flowPercent = tempFlow;
        }
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnCancel()
    {
        LoadFromState();
        Highlight(tempMaster);
        UpdateCounterText();
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void Highlight(int selected)
    {
        for (int i = 0; i < OptionButtons.Length; i++)
        {
            Transform t = FindDeep(transform, OptionButtons[i]);
            if (t == null) continue;
            Image img = t.GetComponent<Image>();
            if (img == null) continue;
            Color c = img.color;
            c.a = (i == selected) ? 1.0f : 0.4f;   // matches firmware 255 vs 100
            img.color = c;
        }
    }

    void UpdateCounterText()
    {
        Transform t = FindDeep(transform, "Txt_CounterValue");
        if (t == null) return;
        TMP_Text tmp = t.GetComponent<TMP_Text>();
        if (tmp != null) tmp.text = tempFlow.ToString();
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