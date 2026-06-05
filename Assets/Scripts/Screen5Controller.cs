using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Diagnostics screen for single pump head canvases. Firmware-confirmed minimal:
/// the firmware Screen5Presenter only forwards external timeUpdateTOH/TMRH calls
/// to the view — no internal apply/cancel logic, no spinners, no toggles.
/// CANCEL and APPLY both just navigate home.
///
/// Displayed fields (Total Operating hours/days, Total Motor Running hours/days,
/// Service Need) are externally driven in the firmware and stay at default "0"
/// / "00:00:00" in Product A scope.
/// </summary>
public class Screen5Controller : MonoBehaviour
{
    [Header("Wiring")]
    public PumpHeadState state;
    public PumpHeadNavigator navigator;
    [Tooltip("Screen name to return to on Apply/Cancel.")]
    public string returnScreen = "Screen_PumpHead_Screen1";

    void OnEnable()
    {
        foreach (Graphic g in GetComponentsInChildren<Graphic>(true))
            g.raycastTarget = false;

        WireActions();
DisableTextRaycast("Btn_Cancel_Text");
DisableTextRaycast("Btn_Apply_Text");
        if (navigator != null) navigator.WireNavStripButtons(transform);
    }

   void WireActions()
{
    Transform cancel = FindDeep(transform, "Btn_Cancel_Bg");
    if (cancel != null) HookButton(cancel.gameObject, OnNavHome);

    Transform apply = FindDeep(transform, "Btn_Apply_Bg");
    if (apply != null) HookButton(apply.gameObject, OnNavHome);
}

    void OnNavHome()
    {
        if (navigator != null) navigator.ShowScreen(returnScreen);
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

    void DisableTextRaycast(string name)
{
    Transform t = FindDeep(transform, name);
    if (t == null) return;
    Graphic g = t.GetComponent<Graphic>();
    if (g != null) g.raycastTarget = false;
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