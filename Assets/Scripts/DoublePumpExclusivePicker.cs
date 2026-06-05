using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Tube_Size and Direction pickers for the double pump head canvas.
/// Same pattern as ExclusivePickerController, but reads/writes the per-pump
/// field on DoublePumpHeadState according to state.activePicker (A or B).
/// The navigator sets activePicker before opening this screen.
/// </summary>
public class DoublePumpExclusivePicker : MonoBehaviour
{
    public enum PickerKind { Tube, Direction }

    [Header("Wiring")]
    public PickerKind kind;
    public DoublePumpHeadState state;
    public DoublePumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel/Home.")]
    public string returnScreen = "Screen_PumpHead04_Screen1";

    // Tube option values (index -> tube size string), matches single pump firmware order
    static readonly string[] TubeValues = { "1/4", "3/8", "1/2", "5/16", "F1", "F2" };
    static readonly string[] DirectionValues = { "CW", "CCW" };

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
        string[] names = OptionNames();
        for (int i = 0; i < names.Length; i++)
        {
            Transform t = FindDeep(transform, names[i]);
            if (t == null) continue;
            int captured = i;
            HookButton(t.gameObject, () => OnOptionSelected(captured));
        }
    }

    void WireActionButtons()
    {
        Transform cancel = FindDeep(transform, "Btn_Cancel_Bg");
        if (cancel != null) HookButton(cancel.gameObject, OnCancel);

        Transform apply = FindDeep(transform, "Btn_Apply_Bg");
        if (apply != null) HookButton(apply.gameObject, OnApply);
    }

    void WireHomeButton()
    {
        Transform home = FindDeep(transform, "Img_HomeBorder");
        if (home != null) HookButton(home.gameObject, OnHomePressed);
    }

    string[] OptionNames()
    {
        switch (kind)
        {
            case PickerKind.Tube:
                return new[] { "Btn_Tube0_Outline", "Btn_Tube1_Outline", "Btn_Tube2_Outline", "Btn_Tube3_Outline", "Btn_Tube4_Outline", "Btn_Tube5_Outline" };
            case PickerKind.Direction:
                return new[] { "Btn_Forward_Border", "Btn_Reverse_Border" };
        }
        return new string[0];
    }

    void LoadFromState()
    {
        tempSelected = 0;
        changed = false;
        if (state == null) return;

        string currentValue = GetCurrentValueFromState();
        string[] values = (kind == PickerKind.Tube) ? TubeValues : DirectionValues;
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == currentValue) { tempSelected = i; break; }
        }
    }

    string GetCurrentValueFromState()
    {
        bool isPumpA = (state.activePicker != DoublePumpHeadState.ActivePump.B);
        if (kind == PickerKind.Tube)
            return isPumpA ? state.pumpA_TubeSize : state.pumpB_TubeSize;
        else
            return isPumpA ? state.pumpA_Direction : state.pumpB_Direction;
    }

    void OnOptionSelected(int idx)
    {
        tempSelected = idx;
        changed = true;
        Highlight(idx);
    }

    void OnApply()
    {
        if (changed && state != null)
        {
            string[] values = (kind == PickerKind.Tube) ? TubeValues : DirectionValues;
            if (tempSelected >= 0 && tempSelected < values.Length)
            {
                string selectedValue = values[tempSelected];
                bool isPumpA = (state.activePicker != DoublePumpHeadState.ActivePump.B);

                if (kind == PickerKind.Tube)
                {
                    if (isPumpA) state.pumpA_TubeSize = selectedValue;
                    else         state.pumpB_TubeSize = selectedValue;
                }
                else
                {
                    if (isPumpA) state.pumpA_Direction = selectedValue;
                    else         state.pumpB_Direction = selectedValue;
                }
            }
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
        string[] names = OptionNames();
        for (int i = 0; i < names.Length; i++)
        {
            Transform t = FindDeep(transform, names[i]);
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