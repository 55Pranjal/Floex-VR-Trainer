# Day 44

## Goal
Close out the DPH parity gaps and ship a guided familiarization tutorial.
Planned: (1) DPH tube fix — strip tube-select, hardcode 1/4; (2) DPH dynamic
flow readout; (3) machine-wide arterial exclusivity (one head holds arterial);
(4) DPH per-lane pump-select screen; (5) TRAINING mode — floating tutorial panel
that walks the user BSA → target flow → arterial pump → match flow, with a
validation gate and clues.

## What I did
- Confirmed the evaluation-gate clinical answer: target flow is compared against
  the ARTERIAL pump only. Built against that.
- DPH tube: removed the duplicated tube-select screens (scene-level, no JSON),
  pruned the tube entries from DoublePumpHeadNavigator (Screen1PickerEntries +
  ManagedScreens). Tube labels now render static "1/4".
- DPH dynamic flow: added GetFlowLpmA/B to DoublePumpHeadState (1/4 coefficient
  0.00602 LPM/RPM per lane). DoublePumpScreen1Controller now shows live per-lane
  LPM gated on running && powered && rpm>0; moved UpdateReadouts into Update so it
  tracks the knob.
- Arterial exclusivity: new ArterialRegistry (first global-state object; one
  instance on a Managers GameObject). At most one head holds arterial machine-wide.
  Default pump role changed to Nil (index 0), roles shifted up; SPH Pump_Select_1
  gained a Btn_Nil_Bg option and shows "PUMP SELECT" when Nil. ExclusivePickerController
  claims/releases on APPLY and dims+de-interacts the arterial option when another
  head owns it (silent disabled-button block). Registry claimant is the head state
  (SPH) or a per-lane key object (DPH).
- DPH pump-select: new DPH_Pump_Select JSON (Pump_Select_1 clone + Nil), new
  DoublePumpPickerController (per-lane, keyed by activePicker, claims via
  laneKeyA/laneKeyB). Two per-lane entry buttons on Screen1 (replaced the single
  CARDIOPLEGIA header label). Exclusivity now spans all three SPH heads + both DPH
  lanes.
- Tutorial: new Tutorial_Panel JSON (standalone floating panel) + TutorialController.
  One panel, body text swaps per step, single action button whose label/behaviour
  changes by step (START TUTORIAL / NEXT / DONE / RESTART) plus a BACK button hidden
  on the invite step. Step-6 DONE validates: passes if any head is Arterial +
  Forward (SPH only — DPH direction ungraded) + powered + running + flow within
  target ± tolerance; on fail, reports the first meaningful problem in priority
  order (no arterial / not running / reversed / flow low-or-high, with a
  "bigger tube" hint when RPM is maxed). BSAFormController now stashes TargetFlowLpm
  + HasTarget for the validator to read.

## What broke and how I fixed it
- Tutorial button did nothing: the label (Btn_Action_Label) sat over the button
  background with raycastTarget on, eating the poke. Disabled the label's raycast
  in WireActionButton (same pattern as the nav-strip _Text elements).
- Body text overflowed the panel: TMP wrapping/overflow setting on the built
  Txt_Body, not JSON — enabled Wrapping + Overflow.
- Double-advance (one poke skipped two steps): NOT the bypass — Ray and Poke are
  both live on the panel, so the button's onClick fired from both pipelines one
  frame apart (confirmed: an "action pressed" with no matching bypass "Click" log).
  Chose to keep both pipelines (hands/poke are needed for the knob) and added a
  shared 0.4s silent debounce (Debounced()) covering both NEXT and BACK.
- False pass (flow 0.8 accepted against target 4.84): flowToleranceLpm was a
  nonsense ±10 (larger than the whole flow range, so lower bound went negative and
  everything passed). Corrected to ±0.1.
- Intermittent ray wedge (a canvas's ray clicks would die, seemingly after knob
  rotation or Calculate, only sometimes, and reproduced on different heads): the
  real defect was in CanvasClickBypass — the click edge was computed from the global
  interactor.State BEFORE hit-testing, so a select that started off-canvas (knob
  grab, or the ray crossing another canvas) could latch wasSelecting=true and starve
  that canvas of future click edges until full release. Fixed by computing the edge
  only after confirming the ray is over a UI element of THIS canvas, and resetting
  the latch on every off-canvas path. Added a camera-null warning log for the next
  time it flaps.

## Decisions
- Evaluation gate resolved: arterial pump only is compared to target (build-against).
- DPH is arterial-eligible per lane (perfusionist: DPH is a free extra head). Per-head
  arterial eligibility is effectively a config, not hardcoded.
- Default pump role is Nil, not Arterial — forces a deliberate selection and keeps the
  registry honest at boot (no head boots owning arterial).
- Tutorial guides to a SINGLE head (P1–P3). SPH has clean Forward/Reverse so direction
  is graded there; DPH uses CW/CCW whose physical push mapping is unconfirmed (CAD/
  Hashir), so DPH direction is NOT graded — a DPH lane can pass on arterial + flow.
- Tutorial keeps both interaction pipelines (Ray + Poke); dedupe handled by a controller-
  side silent debounce, not by disabling a pipeline or tuning the shared bypass.
- Teaching flow tolerance is ±0.1 L/min (dev-seat). The scored/clinical tolerance is
  still KRB's call, separate from this.
- No-target case left as-is: only blocked at the final DONE gate, not gated mid-walk.

## Open questions / next
- DPH direction (CW/CCW) → physical push-direction mapping: needs Hashir/CAD before
  DPH direction can be graded in the tutorial or evaluation.
- Scored evaluation tolerance (vs the ±0.1 teaching band): KRB.
- Do cardioplegia/suction/vent pumps get their own correctness criteria, or pass/fail
  on setup? (still open from Day 43)
- Tube-size correctness is taught (bigger tube = more flow) but NOT validated — any
  tube that can reach the target is accepted. Fine for v1; revisit if a scored eval
  wants tube gating.
- CanvasClickBypass edge-state fix touches all five canvases — watch for regressions;
  the wedge was intermittent so keep an eye out over the next few sessions.

## Time spent
~ (fill in)