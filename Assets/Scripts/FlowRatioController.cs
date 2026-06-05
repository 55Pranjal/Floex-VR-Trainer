using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Flow Ratio picker for the double pump head canvas.
/// 11 options total: NIL + 10 fixed ratios (1/1, 2/1, 3/1, 4/1, 6/1, 8/1, 10/1, 12/1, 14/1, 16/1).
/// Single shared field on DoublePumpHeadState.flowRatio — no Pump A/B branching.
/// Same activate/select/apply/cancel pattern as other pickers.
/// </summary>
public class FlowRatioController : MonoBehaviour
{
    [Header("Wiring")]
    public DoublePumpHeadState state;
    public DoublePumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel/Home.")]
    public string returnScreen = "Screen_PumpHead04_Screen1";

    // Index 0 = NIL, then ratios in visual order (top-left to bottom-right, column-by-column)
    static readonly string[] OptionButtons =
    {
        "Img_Nil_Bg",      // 0
        "Img_R_1_1_Bg",    // 1
        "Img_R_2_1_Bg",    // 2
        "Img_R_3_1_Bg",    // 3
        "Img_R_4_1_Bg",    // 4
        "Img_R_6_1_Bg",    // 5
        "Img_R_8_1_Bg",    // 6
        "Img_R_10_1_Bg",   // 7
        "Img_R_12_1_Bg",   // 8
        "Img_R_14_1_Bg",   // 9
        "Img_R_16_1_Bg",   // 10
    };

    static readonly string[] OptionValues =
    {
        "Nil",  "1/1",  "2/1",  "3/1",  "4/1",
        "6/1",  "8/1",  "10/1", "12/1", "14/1", "16/1",
    };

    int tempSelected;
    bool changed;

    void OnEnable()
    {
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireOptions();
        WireActionButtons();
        WireHomeButton();
        LoadFromState();
        Highlight(tempSelected);
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

    void WireActionButtons()
    {
        Transform cancel = FindDeep(transform, "Img_Cancel_Bg");
        if (cancel != null) HookButton(cancel.gameObject, OnCancel);

        Transform apply = FindDeep(transform, "Img_Apply_Bg");
        if (apply != null) HookButton(apply.gameObject, OnApply);
    }

    void WireHomeButton()
    {
        Transform home = FindDeep(transform, "Img_Home_Bg");
        if (home != null) HookButton(home.gameObject, OnHomePressed);
    }

    void LoadFromState()
    {
        tempSelected = 0;
        changed = false;
        if (state == null) return;

        for (int i = 0; i < OptionValues.Length; i++)
        {
            if (OptionValues[i] == state.flowRatio) { tempSelected = i; break; }
        }
    }

    void OnOptionSelected(int idx)
    {
        tempSelected = idx;
        changed = true;
        Highlight(idx);
    }

    void OnApply()
    {
        if (changed && state != null
            && tempSelected >= 0 && tempSelected < OptionValues.Length)
        {
            state.flowRatio = OptionValues[tempSelected];
        }
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnCancel()
    {
        LoadFromState();
        Highlight(tempSelected);
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnHomePressed()
    {
        OnCancel();
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
            c.a = (i == selected) ? 1.0f : 0.4f;
            img.color = c;
        }
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