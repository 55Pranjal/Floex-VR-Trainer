# Day 38 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 25 Jun 2026
*(Days 36–37 skipped — no project work. Returning after a 2-day gap.)*

---

## Goal

Resume Phase 3B. Resolve where patient data belongs (it's not on the HLM), settle the product direction with Shiv, and start building the patient monitor display on the ECG monitor asset.

## What I did

**Clarified the input mapping — taken to the team, not built.** The control->state bridge (pump RPM/mode -> PatientState) needs team input before it can be wired, because the pumps aren't interchangeable: the three single heads carry assignable modes (arterial, cardioplegia, suction, vent) and the double head is cardioplegia. Discussed with Shiv:
- He wants a detailed discussion with the HLM team on how it should work.
- Proposed direction (his): double pump head used for suction + vent; single heads for cardioplegia; arterial mode selectable on any one single head. He'll confirm with the HLM team.
- So the bridge INPUT side is parked pending that discussion.

**Key architectural clarification: patient data is NOT shown on the HLM.** None of the HLM screens display patient physiology — the HLM shows machine state only (RPMs, modes, the perfusionist's stopwatch timers). Patient data (BP, SvO2, gases, temp) is shown on the hospital's own monitor. In the scene that's the ECG monitor asset. This maps cleanly onto the architecture: HLM canvases = inputs/controls, hospital monitor = patient outputs. So the monitor is the correct home for PatientState's output values.

**Clarified the HLM timers.** The 4 main-screen timers are user stopwatches: 1 for cardioplegia + 3 general-purpose the perfusionist starts/stops manually. They are NOT a patient-bypass clock and the sim should not drive them. PatientState.TimeOnBypass stays internal.

**Captured Shiv's product framing (scenario-based trainer).** Shiv sees the product as: present predefined patient states / conditions, and the trainee perfusionist decides how to operate the HLM correctly for that patient (correct RPM, flow, mode setup). This is a scenario-based assessment trainer rather than a real-time physiology simulator — clinical knowledge lives in scenario definitions + correct-answer ranges (tractable, KRB-authorable) instead of continuous validated equations (hard, high-risk). Wrote a sketch document laying this out + the open forks, for the detailed team discussion (mine to run).

**Started the patient monitor screen.**
- Designed the monitor screen layout: numeric-first multi-parameter monitor — 6 colour-coded vital blocks (HR green, ABP/MAP red, SvO2 cyan, Temp amber, PaO2 blue, PaCO2 white) mapping to PatientState fields, plus a footer (base excess, hematocrit, time-on-bypass), a scenario-name slot up top (forward-compatible with Shiv's scenario model), and a waveform placeholder strip for later.
- ECG monitor asset was a bare 3D model; added an empty child and set it up as a World-Space Canvas (Canvas + Canvas Scaler + Graphic Raycaster) — same pattern as the HLM canvases. Confirmed the existing ScreenBuilder works unchanged for this (it builds static display elements; the PatientState-reading behaviour goes in a separate controller, like the HLM screens).
- Hit a sizing/visibility issue: the monitor screen face is a different size/aspect from the pump-head and pole canvases, and a world-space canvas with no content is invisible. Added a temporary visible Image to the canvas to see its rectangle in the Scene view, so the size/scale can be set against the actual monitor face. Ended the day here.

## What broke and how I fixed it

- **Couldn't see the world-space canvas** to size it — empty canvas draws nothing. Fix: dropped a temporary bright Image on it to make the rectangle visible in Scene view; will delete once the JSON background is built.

## Decisions

- **Patient monitor lives on the ECG/hospital monitor asset, NOT the HLM** (per Shiv — matches real OR; HLM shows machine state only).
- **Build the monitor display now** — it's correct under either product direction (scenario trainer or real-time sim) and either way patient data shows on the hospital monitor. Unblocked by the pending pump-mode discussion.
- **Numeric-first monitor**; waveforms deferred to polish.
- **No color thresholds / alarm logic on the monitor** — display values, don't judge them. Ranges are KRB territory, later.
- **Bridge INPUT side parked** pending the HLM-team discussion on pump modes.
- **PatientState.TimeOnBypass stays internal** — HLM timers are independent user stopwatches.
- Monitor JSON coordinate space provisionally 720x480 (3:2); final dimensions to match the actual ECG screen-face aspect (TBD).

## Open questions / next

- **Confirm the ECG monitor screen-face aspect ratio**; if not 3:2, rework the monitor JSON layout to match so it doesn't stretch.
- **Then (Day 40): seat the canvas on the monitor face (position/scale/rotation), build `Patient_Monitor.json` via ScreenBuilder, write `PatientMonitorController.cs`** (reads PatientStateDriver.State, writes the 6 vitals + footer by element name — same FindDeep pattern as the HLM controllers).
- **Team discussion (mine to run):** scenario-based vs real-time sim, what each main-screen timer counts, pump-mode mapping (arterial-assignable, double-head suction/vent), how "correct operation" is defined per scenario (KRB), feedback model. Sketch doc written for this.
- Monitor will show static PatientState placeholder values until physiology/scenario data drives it — expected.
- KRB still gates the actual physiology math / scenario correctness ranges.

## Time spent

~

## Files modified today

- `Assets/Scenes/OR_Environment.unity` — ECG_Monitor: added World-Space Canvas child (Canvas + Scaler + Raycaster) + temporary Image for visibility
- (doc) scenario-based architecture sketch — for team discussion, not in repo
- `devlog/day-38.md` — new