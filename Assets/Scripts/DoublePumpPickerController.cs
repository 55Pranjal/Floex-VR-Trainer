using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Per-lane pump-type picker for the double pump head (slot 4). Mirrors the
/// single-head ExclusivePickerController (Pump kind) but commits to the lane
/// named by state.activePicker (A or B) instead of a single head role.
///
/// Options are index-aligned to pumpX_PumpIndex:
///   0=Nil 1=Arterial 2=Cardio 3=Vent 4=Suct1 5=Suct2
///
/// Arterial exclusivity uses ArterialRegistry keyed by the ACTIVE LANE's key
/// (state.laneKeyA / laneKeyB) — one head owns two independently-claimable lanes.
/// On open, a locked Arterial option is dimmed + de-interacted (silent block).
/// </summary>
public class DoublePumpPickerController : MonoBehaviour
{
    [Header("Wiring")]
    public DoublePumpHeadState state;
    public DoublePumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel/Home.")]
    public string returnScreen = "Screen_PumpHead04_Screen1";

    const int ArterialIndex = 1;

    static readonly string[] OptionNames =
        { "Btn_Nil_Bg", "Btn_Arterial_Bg", "Btn_Cardioplegia_Bg", "Btn_Vent_Bg", "Btn_Suction1_Bg", "Btn_Suction2_Bg" };

    int tempSelected;
    bool changed;

    // Resolve the active lane once per open.
    bool ActiveIsA => state != null && state.activePicker == DoublePumpHeadState.ActivePump.A;
    object ActiveLaneKey => ActiveIsA ? state.laneKeyA : state.laneKeyB;
    int ActiveIndex => ActiveIsA ? state.pumpA_PumpIndex : state.pumpB_PumpIndex;

    void OnEnable()
    {
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
        for (int i = 0; i < OptionNames.Length; i++)
        {
            Transform t = FindDeep(transform, OptionNames[i]);
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

    void LoadFromState()
    {
        tempSelected = (state == null) ? 0 : ActiveIndex;
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
            object laneKey = ActiveLaneKey;

            if (tempSelected == ArterialIndex)
            {
                // Landing on Arterial — claim for this lane. Guard against a race.
                if (ArterialRegistry.Instance != null &&
                    !ArterialRegistry.Instance.TryClaim(laneKey))
                {
                    if (navigator != null) navigator.ShowScreen(returnScreen);
                    return; // claim lost; leave lane role unchanged
                }
            }
            else if (ActiveIndex == ArterialIndex)
            {
                // Moving this lane OFF arterial — release.
                if (ArterialRegistry.Instance != null)
                    ArterialRegistry.Instance.Release(laneKey);
            }

            if (ActiveIsA) state.pumpA_PumpIndex = tempSelected;
            else           state.pumpB_PumpIndex = tempSelected;
        }
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnCancel()
    {
        LoadFromState();
        Highlight(tempSelected);
        if (navigator != null) navigator.ShowScreen(returnScreen);
    }

    void OnHomePressed() => OnCancel();

    void Highlight(int selected)
    {
        for (int i = 0; i < OptionNames.Length; i++)
        {
            Transform t = FindDeep(transform, OptionNames[i]);
            if (t == null) continue;
            Image img = t.GetComponent<Image>();
            if (img == null) continue;
            Color c = img.color;
            c.a = (i == selected) ? 1.0f : 0.4f;
            img.color = c;
        }
    }

    /// <summary>
    /// If another lane/head owns Arterial, dim + de-interact the Arterial option
    /// for THIS lane (silent block). Runs after Highlight so the dim wins.
    /// Re-evaluated each open — self-correcting once the owner releases.
    /// </summary>
    void ApplyArterialGate()
    {
        if (state == null || ArterialRegistry.Instance == null) return;

        bool locked = !ArterialRegistry.Instance.IsFree(ActiveLaneKey);
        if (!locked) return;

        Transform t = FindDeep(transform, "Btn_Arterial_Bg");
        if (t == null) return;

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