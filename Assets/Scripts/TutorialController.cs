using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

/// <summary>
/// Floating familiarization tutorial. One panel, body text swaps per step,
/// one action button whose label + behaviour changes by step.
///
/// States (stepIndex):
///   0  invite            button "START TUTORIAL" -> step 1
///   1  open BSA screen    "NEXT" -> 2
///   2  calculate target   "NEXT" -> 3
///   3  power a head       "NEXT" -> 4
///   4  set pump/dir/tube  "NEXT" -> 5
///   5  press START        "NEXT" -> 6
///   6  match target flow   "DONE" -> Validate()
///   7  passed             "RESTART" -> 0
///
/// Validation (step 6): passes if ANY checkable unit is Arterial + (Forward
/// where the model expresses direction) + powered + running + flow within
/// target +/- 10 L/min. On fail, reports the first meaningful problem as a clue.
///
/// Direction note: SPH exposes a clean Forward/Reverse (directionForward), so
/// direction is enforced there. DPH lanes use CW/CCW whose physical push mapping
/// is unconfirmed (CAD/Hashir), so direction is NOT graded on DPH — a DPH lane
/// can pass on arterial + flow alone. The tutorial text guides to a single head.
/// </summary>
public class TutorialController : MonoBehaviour
{
    [Header("Checkable heads")]
    public PumpHeadState[] singleHeads;      // P1, P2, P3
    public DoublePumpHeadState doubleHead;   // P4 (two lanes)

    [Header("Target source")]
    public BSAFormController bsaForm;

    [Header("Tolerance")]
    [Tooltip("Actual flow must be within +/- this many L/min of target (teaching tolerance, not clinical).")]
    public float flowToleranceLpm = 0.1f;

    [Tooltip("Knob RPM ceiling — used to detect 'maxed out, switch to a bigger tube'.")]
    public int rpmCeiling = 250;

    [Header("Tint")]
    public Color highlightColor = new Color(0.906f, 0.412f, 0.427f);

    const int ArterialRole = 1;  // 0=Nil,1=Arterial,...

    int stepIndex = 0;

    void Start()
    {
        WireActionButton();
        Refresh();
    }

    // ── Action button ─────────────────────────────────────────────

    float lastInputTime = -999f;
const float InputDebounce = 0.4f;

bool Debounced()
{
    if (Time.unscaledTime - lastInputTime < InputDebounce) return true;
    lastInputTime = Time.unscaledTime;
    return false;
}

void OnActionPressed()
{
    if (Debounced()) return;

    if (stepIndex == 0)              stepIndex = 1;
    else if (stepIndex >= 1 && stepIndex <= 5) stepIndex++;
    else if (stepIndex == 6)         { Validate(); return; }
    else if (stepIndex == 7)         stepIndex = 0;
    Refresh();
}

void OnBackPressed()
{
    if (Debounced()) return;

    if (stepIndex >= 1 && stepIndex <= 6) stepIndex--;
    else if (stepIndex == 7) stepIndex = 6;
    Refresh();
}

    void Validate()
    {
        string clue;
        if (Evaluate(out clue))
        {
            stepIndex = 7;
            Refresh();
        }
        else
        {
            // Stay on step 6, show the specific problem.
            Refresh(clue);
        }
    }

    // ── Evaluation ────────────────────────────────────────────────

    struct Unit
    {
        public string label;
        public int role;
        public bool dirForward;
        public bool hasCleanDir;
        public bool powered;
        public bool running;
        public float flow;
        public int rpm;
    }

    List<Unit> BuildUnits()
    {
        var list = new List<Unit>();

        if (singleHeads != null)
        {
            foreach (var h in singleHeads)
            {
                if (h == null) continue;
                list.Add(new Unit
                {
                    label       = h.gameObject.name,
                    role        = h.pumpIndex,
                    dirForward  = h.directionForward,
                    hasCleanDir = true,
                    powered     = h.powered,
                    running     = h.running,
                    flow        = h.GetFlowLpm(),
                    rpm         = h.rpmSetpoint
                });
            }
        }

        if (doubleHead != null)
        {
            list.Add(new Unit
            {
                label = doubleHead.gameObject.name + " A",
                role  = doubleHead.pumpA_PumpIndex,
                dirForward = false, hasCleanDir = false,   // CW/CCW mapping unconfirmed
                powered = doubleHead.powered,
                running = doubleHead.pumpA_Running,
                flow = doubleHead.GetFlowLpmA(),
                rpm = doubleHead.pumpA_RpmSetpoint
            });
            list.Add(new Unit
            {
                label = doubleHead.gameObject.name + " B",
                role  = doubleHead.pumpB_PumpIndex,
                dirForward = false, hasCleanDir = false,
                powered = doubleHead.powered,
                running = doubleHead.pumpB_Running,
                flow = doubleHead.GetFlowLpmB(),
                rpm = doubleHead.pumpB_RpmSetpoint
            });
        }

        return list;
    }

    /// <summary>True if any head fully satisfies the task. Otherwise clue = first problem.</summary>
    bool Evaluate(out string clue)
    {
        clue = "";

        if (bsaForm == null || !bsaForm.HasTarget)
        {
            clue = "First calculate the target flow on the BSA screen.";
            return false;
        }

        float target = bsaForm.TargetFlowLpm;
        float lo = target - flowToleranceLpm;
        float hi = target + flowToleranceLpm;

        List<Unit> units = BuildUnits();

        // Pass check: any fully-correct head.
        foreach (var u in units)
        {
            bool roleOk = u.role == ArterialRole;
            bool dirOk  = !u.hasCleanDir || u.dirForward;
            bool liveOk = u.powered && u.running;
            bool flowOk = u.flow >= lo && u.flow <= hi;
            if (roleOk && dirOk && liveOk && flowOk) return true;
        }

        // No pass — critique in priority order, focusing on arterial heads.
        var arterial = units.FindAll(u => u.role == ArterialRole);
        if (arterial.Count == 0)
        {
            clue = "No pump is set to Arterial. Select Arterial on a powered head.";
            return false;
        }

        var live = arterial.FindAll(u => u.powered && u.running);
        if (live.Count == 0)
        {
            clue = "Your Arterial pump isn't running. Power it on and press START.";
            return false;
        }

        var forward = live.FindAll(u => !u.hasCleanDir || u.dirForward);
        if (forward.Count == 0)
        {
            clue = "Arterial direction is Reverse — set it to Forward.";
            return false;
        }

        // Direction/role/live are fine on at least one head — it's a flow problem.
        // Pick the arterial head closest to the range for the most useful hint.
        Unit best = forward[0];
        float bestDist = FlowDistance(best.flow, lo, hi);
        for (int i = 1; i < forward.Count; i++)
        {
            float d = FlowDistance(forward[i].flow, lo, hi);
            if (d < bestDist) { best = forward[i]; bestDist = d; }
        }

        string flowStr = best.flow.ToString("0.0");
        string tgtStr  = target.ToString("0.0");

        if (best.flow < lo)
        {
            if (best.rpm >= rpmCeiling)
                clue = $"Flow is {flowStr}, target {tgtStr}. Knob is maxed — switch to a bigger tube.";
            else
                clue = $"Flow is {flowStr}, target {tgtStr}. Turn the knob up.";
        }
        else // best.flow > hi
        {
            clue = $"Flow is {flowStr}, target {tgtStr}. Turn the knob down.";
        }
        return false;
    }

    static float FlowDistance(float flow, float lo, float hi)
    {
        if (flow < lo) return lo - flow;
        if (flow > hi) return flow - hi;
        return 0f;
    }

    // ── Panel refresh ─────────────────────────────────────────────

    void Refresh(string clue = "")
{
    SetText("Txt_Body", BuildBody(stepIndex));
    SetText("Btn_Action_Label", BuildLabel(stepIndex));
    SetText("Txt_Clue", clue);
    SetActive("Btn_Back_Bg", stepIndex >= 1);   // hidden on step 0
}

void SetActive(string name, bool on)
{
    Transform t = FindDeep(transform, name);
    if (t != null) t.gameObject.SetActive(on);
}

    string BuildLabel(int s)
    {
        if (s == 0) return "START TUTORIAL";
        if (s == 6) return "DONE";
        if (s == 7) return "RESTART";
        return "NEXT";
    }

    string BuildBody(int s)
    {
        switch (s)
        {
            case 0:
                return "Welcome to the Floex 3.0 familiarization trainer.\n\n" +
                       "This guided walkthrough shows you how to set an arterial pump to a patient's target flow.\n\n" +
                       "Press START TUTORIAL to begin.";
            case 1:
                return "Step 1 of 6\n\n" +
                       "Go to the main pole console and open the BSA / Patient screen using the navigation.\n\n" +
                       "Review the patient's height and weight.";
            case 2:
                return "Step 2 of 6\n\n" +
                       "On the BSA screen, press CALCULATE.\n\n" +
                       "Note the TARGET FLOW value — you'll match a pump to it.";
            case 3:
                return "Step 3 of 6\n\n" +
                       "Go to any single pump head (P1, P2 or P3).\n\n" +
                       "Press the POWER button beside the knob to switch the head ON. The screen lights up.";
            case 4:
                return "Step 4 of 6\n\n" +
                       "On that head, set:\n" +
                       "   PUMP  =  Arterial\n" +
                       "   DIRECTION  =  Forward\n" +
                       "   TUBE  =  your choice\n\n" +
                       "Bigger tube = more flow per turn. Pick a tube whose range can reach the target.";
            case 5:
                return "Step 5 of 6\n\n" +
                       "Press the green START button to run the pump.";
            case 6:
    return "Step 6 of 6\n\n" +
           $"Rotate the knob until the L/MIN reading is within {flowToleranceLpm:0.0#} of your target flow.\n\n" +
           "If the knob maxes out and flow is still too low, switch to a bigger tube.\n\n" +
           "When ready, press DONE.";
            case 7:
                return "MISSION PASSED\n\n" +
                       "Your arterial pump is running at the patient's target flow. Well done.";
        }
        return "";
    }

    // ── Wiring helpers ────────────────────────────────────────────

    void WireActionButton()
    {
        Transform t = FindDeep(transform, "Btn_Action_Bg");
        if (t == null) { Debug.LogWarning("[TutorialController] Btn_Action_Bg not found."); return; }
        HookButton(t.gameObject, OnActionPressed);

        // Label sits over the Bg — must be click-transparent or it eats the poke.
         Transform lbl = FindDeep(transform, "Btn_Action_Label");
         if (lbl != null)
        {
             Graphic g = lbl.GetComponent<Graphic>();
             if (g != null) g.raycastTarget = false;
        }
         Transform back = FindDeep(transform, "Btn_Back_Bg");
         if (back != null) HookButton(back.gameObject, OnBackPressed);
         Transform backLbl = FindDeep(transform, "Btn_Back_Label");
         if (backLbl != null) { var g = backLbl.GetComponent<Graphic>(); if (g != null) g.raycastTarget = false; }
    }

    
    void HookButton(GameObject go, UnityAction onClick)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;

        Button btn = go.GetComponent<Button>();
        if (btn == null) btn = go.AddComponent<Button>();
        btn.targetGraphic = graphic;

        ColorBlock cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = highlightColor;
        cb.pressedColor     = highlightColor;
        cb.selectedColor    = Color.white;
        cb.fadeDuration     = 0.05f;
        btn.colors = cb;
        btn.transition = Selectable.Transition.ColorTint;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(onClick);
    }

    void SetText(string name, string value)
{
    Transform t = FindDeep(transform, name);
    if (t == null) { Debug.Log($"[Tutorial] SetText MISS: {name}"); return; }
    TMP_Text tmp = t.GetComponent<TMP_Text>();
    if (tmp == null) { Debug.Log($"[Tutorial] SetText no TMP on: {name}"); return; }
    tmp.text = value;
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