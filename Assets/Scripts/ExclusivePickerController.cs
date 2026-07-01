using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Drives the firmware "exclusive picker" pattern (Pump_Select_1, Tube_Size_1,
/// Direction). 5-step pattern from firmware Presenter:
///   activate           -> load state from PumpHeadState, highlight current
///   onOptionSelected   -> set temp, mark changed, re-highlight
///   onApplyClicked     -> commit temp to state, nav back to home
///   onCancelClicked    -> revert temp to state, nav back to home
///   onHomePressed      -> same as cancel (revert, nav back)
/// Highlight = alpha 255 on selected, alpha 100 on rest (matches firmware).
///
/// Arterial exclusivity: Pump kind consults ArterialRegistry. At most one head
/// may hold Arterial (index 0) machine-wide. On open, if another head owns it,
/// the Arterial option is dimmed + de-interacted (silent block). On Apply,
/// claim when landing on Arterial, release when moving off it.
/// </summary>
public class ExclusivePickerController : MonoBehaviour
{
    public enum PickerKind { Pump, Tube, Direction }

    [Header("Wiring")]
    public PickerKind kind;
    public PumpHeadState state;
    public PumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel/Home.")]
    public string returnScreen = "Screen_PumpHead_Screen1";

    const int ArterialIndex = 1;

    int tempSelected;
    bool changed;

    void OnEnable()
    {
        // Display-only by default — HookButton turns raycast back on for wired elements.
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireOptions();
        WireActionButtons();
        WireHomeButton();
        LoadFromState();
        Highlight(tempSelected);
        ApplyArterialGate();
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
        // Header home button on picker screens. Behaves like Cancel.
        Transform home = FindDeep(transform, "Img_HomeBorder");
        if (home != null) HookButton(home.gameObject, OnHomePressed);
    }

    string[] OptionNames()
    {
        switch (kind)
        {
            case PickerKind.Pump:
                return new[] { "Btn_Nil_Bg", "Btn_Arterial_Bg", "Btn_Cardioplegia_Bg", "Btn_Vent_Bg", "Btn_Suction1_Bg", "Btn_Suction2_Bg" };
            case PickerKind.Tube:
                return new[] { "Btn_Tube0_Outline", "Btn_Tube1_Outline", "Btn_Tube2_Outline", "Btn_Tube3_Outline", "Btn_Tube4_Outline", "Btn_Tube5_Outline" };
            case PickerKind.Direction:
                return new[] { "Btn_Forward_Border", "Btn_Reverse_Border" };
        }
        return new string[0];
    }

    void LoadFromState()
    {
        if (state == null) { tempSelected = 0; changed = false; return; }
        switch (kind)
        {
            case PickerKind.Pump:      tempSelected = state.pumpIndex; break;
            case PickerKind.Tube:      tempSelected = state.tubeIndex; break;
            case PickerKind.Direction: tempSelected = state.directionForward ? 0 : 1; break;
        }
        changed = false;
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
            switch (kind)
            {
                case PickerKind.Pump:
                    // Registry is source of truth for arterial exclusivity.
                    if (tempSelected == ArterialIndex)
                    {
                        // Landing on Arterial — claim. Option was interactable so it
                        // should be free, but guard against a race.
                        if (ArterialRegistry.Instance != null &&
                            !ArterialRegistry.Instance.TryClaim(state))
                            break; // claim lost; leave state unchanged
                    }
                    else if (state.pumpIndex == ArterialIndex)
                    {
                        // Moving OFF arterial to something else — release.
                        if (ArterialRegistry.Instance != null)
                            ArterialRegistry.Instance.Release(state);
                    }
                    state.pumpIndex = tempSelected;
                    break;
                case PickerKind.Tube:      state.tubeIndex = tempSelected; break;
                case PickerKind.Direction: state.directionForward = (tempSelected == 0); break;
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
        // Treat as cancel — revert any pending selection, then nav home.
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

    /// <summary>
    /// Pump kind only: if another head owns Arterial, dim + de-interact the
    /// Arterial option (silent disabled-button block). Runs after Highlight so
    /// the dim alpha wins. Re-evaluated on every OnEnable — self-correcting once
    /// the other head releases.
    /// </summary>
    void ApplyArterialGate()
    {
        if (kind != PickerKind.Pump || state == null || ArterialRegistry.Instance == null) return;

        bool locked = !ArterialRegistry.Instance.IsFree(state);
        if (!locked) return;

        Transform t = FindDeep(transform, "Btn_Arterial_Bg");
        if (t == null) return;

        // Strip the Button so poke/ray do nothing and no hover/press tint fires,
        // drop raycast, dim to a clearly-disabled alpha.
        Button btn = t.GetComponent<Button>();
        if (btn != null) Destroy(btn);

        Graphic g = t.GetComponent<Graphic>();
        if (g != null)
        {
            g.raycastTarget = false;
            Color c = g.color;
            c.a = 0.15f;
            g.color = c;
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