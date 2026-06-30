# Day 43 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 30 Jun 2026
*(Days 41-42 skipped — no project work.)*

---

## Goal

Finish the power-button feature (gate the pump on screen power), solve the long-standing BSA keyboard problem, then start real physiology: target flow from patient input, and actual flow from RPM + tubing using the real machine's calibration.

## What I did

**Finished the power button + screen backing across all heads.**
- Black backing canvas (`Screen_Backing_NN`): always-on plain canvas with a black image, seated just behind each screen canvas, NOT toggled by the power button — so a powered-off console reads as a black screen instead of bare metal. Built on PH1, replicated to PH2/PH3/double.
- Gated pump behaviour on `powered`: `PumpHeadRotor` and `PumpAudio` now require `&& state.powered`, so a powered-off pump neither spins nor hums. (Double rotor/audio to be gated when convenient; double power button is built.)

**Realistic rotor direction reversal (from Day 40, confirmed working).** Rotors ease angular velocity toward target via MoveTowards (angularAcceleration), so flipping direction ramps down through zero and back up instead of snapping. All heads.

**SOLVED the BSA keyboard problem (this was the big one).** Prior attempts (Days 15-16) burned 2 days because TMP_InputField never receives focus through Meta's pointable-canvas pipeline on v74, so the system keyboard never opened.
- New approach (`SystemKeyboardField.cs`): no InputField. Poke the field's box (normal Button onClick) → call `TouchScreenKeyboard.Open()` directly → write the returned string into the plain TMP label. Numeric fields use NumbersAndPunctuation (so decimals like CI 2.5 work).
- Re-enables its own raycast in Start (ScreenNavigator blanket-disables in Awake).
- **THE KEY LESSON: the system keyboard overlay only renders in an ON-DEVICE BUILD, never over Link / editor Play.** Testing over Link showed nothing and looked broken — that's what cost the original 2 days. On device it works: numeric keyboard appears, value lands in the field.
- Required `Require System Keyboard` on OVRManager. Applied to all editable BSA fields; all working on device.

**Target flow calculation.** `BSAFormController.Calculate()`: BSA = sqrt(height*weight/3600) (Mosteller, already present); target flow = cardiac index × BSA. CI is entered on screen (moved `Cardiac` from computedFields to editableFields) or defaults to 2.5 if blank. Written to `Txt_Target`. Temperature modifier left out for v1.

**Actual flow from the real machine tables.** Got RPM→LPM tables for 1/4, 3/8, 1/2 tubing photographed from the actual Floex HLM. Confirmed the relationship is linear (roller pump: flow ∝ RPM) and derived one coefficient per tube — verified against the machine tables (≤0.012 LPM error at 100 RPM):
- 1/4 = 0.00602, 3/8 = 0.02598, 1/2 = 0.04498 LPM/RPM.
- `PumpHeadState.GetFlowLpm()` = LpmPerRpm[tubeIndex] × rpmSetpoint. Tubes 5/16, F1, F2 = 0 (no tables; Shiv said skip for now).
- `Screen1Controller` shows it live on `Txt_LpmValue`, gated on `running && powered && rpm>0` (replaced the old static 4.50/0.00 two-state). Tube size change scales flow; knob change tracks live.

**Updated README + CLAUDE.md** to current state (scenario-based reframing, power buttons, keyboard solution, target/actual flow, open clinical questions).

## What broke and how I fixed it

- **Knob stopped grabbing after adding a power-button canvas** (carried lesson): duplicated canvas kept its 800x480 BoundsClipper, blanketing the knob. Fix: shrink BoundsClipper to button size (60x60). Applied each time when replicating.
- **Couldn't enter Play in Link after heavy scene edits** (`DeinitializeLoader without an initialized manager`): wedged XR loader, not a content bug. Fix: restart Unity.
- **Keyboard "not working"**: was actually working — just invisible over Link. Resolved by testing on-device.
- **Target flow not updating with CI**: `Cardiac` was still in computedFields (read into readOnly, not displays), so ParseFloat couldn't see it → always defaulted to 2.5. Fix: move Cardiac to editableFields (and fix the Inspector serialized array, not just the script).
- **`Txt_Hight` typo warning** persisted after script fix — serialized Inspector override; fixed in Inspector.
- **`UpdateRpmDisplay` missing** compile error — method got deleted during the flow edit; restored it.

## Decisions

- **Power = real `powered` state**, screen starts off, rotor+audio gated on it.
- **Keyboard via system overlay + manual TouchScreenKeyboard.Open()**, no InputField; test on-device only.
- **Target flow = CI × BSA**, CI entered or default 2.5, temperature out for v1 (KRB to confirm CI-as-target simplification).
- **Actual flow = linear coefficient × RPM** per tube; only 1/4, 3/8, 1/2 (Shiv-scoped); coefficients verified against the real machine tables.
- **Flow calc is pump-mode-independent** (it's mechanics — correct for all pumps). Which flow gets EVALUATED against the patient target is a separate, mode-dependent question (arterial only?) — belongs to the not-yet-built evaluation layer.

## Open questions / next

- **Evaluation layer (next real step):** compare actual flow vs target flow, give trainee feedback. GATED on the clinical question below.
- **Open clinical questions (ask perfusionist/KRB; tied to pump-mode discussion Shiv is having with HLM team):**
  - Which pump's flow is compared to the BSA target — arterial only?
  - Pump-mode assignment (arterial selectable on a single head; double head = suction + vent per Shiv).
  - Do cardioplegia/suction/vent pumps have their own correctness criteria, or pass/fail on setup?
- Missing tube coefficients (5/16, F1, F2) — get tables from team when those tubes come into scope.
- Gate double-head rotor/audio on `powered` (single done; double pending).
- Scenario engine (predefined patients + correct-operation ranges) — the larger next phase, KRB-authored content.

## Time spent

~

## Files modified today

- `Assets/Scripts/PumpHeadRotor.cs` / `DoublePumpHeadRotor.cs` — momentum ramp on reversal; rotor gated on `powered`
- `Assets/Scripts/PumpAudio.cs` — gated on `powered`
- `Assets/Scripts/PumpHeadState.cs` — `powered` bool; `GetFlowLpm()` + LpmPerRpm coefficient table
- `Assets/Scripts/DoublePumpHeadState.cs` — `powered` bool
- `Assets/Scripts/PumpPowerButton.cs` — new (poke toggles powered, shows/hides console canvas)
- `Assets/Scripts/SystemKeyboardField.cs` — new (system keyboard on poke, writes to TMP label)
- `Assets/Scripts/BSAFormController.cs` — target flow = CI × BSA; Cardiac field editable
- `Assets/Scripts/Pumphead1Screen1Controller.cs` — live computed L/PM flow display
- `Assets/Scripts/PatientMonitorController.cs` — (from Day 40) monitor readouts
- `Assets/ScreenSpecs/Patient_Monitor.json` — (from Day 40) monitor layout
- `Assets/Scenes/OR_Environment.unity` — power buttons + backings all heads; monitor canvas; keyboard fields
- `README.md` — rewritten to current state
- `CLAUDE.md` — updated to Day 43
- `devlog/day-43.md` — new