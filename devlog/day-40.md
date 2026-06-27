# Day 40 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 27 Jun 2026
_(Day 39 skipped — no project work.)_

---

## Goal

Finish the patient monitor controller, then take on smaller machine-fidelity tasks: realistic rotor direction-reversal, and a power button per pump head that gates the console screen.

## What I did

**Finished the patient monitor.** Retuned the ECG-monitor canvas to a square 720x720 (the monitor face is ~1:1, not the 3:2 of the first mockup) at scale ~6e-06. Built `Patient_Monitor.json` (square layout: 6 colour-coded vital cards — HR, ABP/MAP, SvO2, Temp, PaO2, PaCO2 — plus scenario slot, waveform placeholder, footer) via ScreenBuilder. Wrote `PatientMonitorController.cs` — reads `PatientStateDriver.State` and writes the 9 readouts by name (FindDeep, display-only, no thresholds), refreshing 4x/sec. Confirmed all 9 values render (resting placeholders, static until physiology/scenario data drives them).

**Perfusionist input captured (reshapes physiology priority).** Met a perfusionist: the core operating loop is **RPM -> blood flow** — that's the relationship perfusionists work with moment to moment in theatre. Patient demographics (height/weight) are entered once on the BSA screen and set the target; they don't change during a case. Implication: the first real physiology coupling to build is RPM->flow (not the full oxygenation web), and BSA sets the target flow (cardiac index x BSA). Flagged: even RPM->flow needs the L/min-per-RPM coefficient (per tube size) from Hashir/KRB to be correct — don't invent the number.

**Realistic rotor direction reversal.** Instant flip from forward to reverse looked fake (a spinning rotor has angular momentum). Rewrote `PumpHeadRotor` and `DoublePumpHeadRotor` to track a current angular velocity that eases toward the target via `MoveTowards` at an `angularAcceleration` limit (default 900 deg/s/s). Flipping direction now ramps down through zero and spins up the other way; also smooths START/STOP. Also cleaned a leftover bug in `PumpHeadRotor` (duplicate `[Header]` + unused fixed `rotationSpeed = 600` field). Tested all 4 heads — works.

**Power button per pump head (PH1 built).** Decided to build it as a real `powered` state (not just screen visibility) — it's a real operating step and the state is reusable for later scenario evaluation (a powered-off pump shouldn't run). Added `powered` (default false) to `PumpHeadState` and `DoublePumpHeadState`. Wrote `PumpPowerButton.cs` (toggles `powered`, shows/hides the console canvas; works for single or double via assigned state; lives on the pump-head root so it doesn't disable itself). Screen now starts OFF; poke the button to power on.

- Interaction via Route A (reuse the proven canvas-poke) rather than PokeInteractable-on-3D-mesh: duplicated the screen canvas, stripped it to one transparent button (`Btn_Power`, alpha 0) over the physical button face, kept its poke/ray pipeline, wired Button onClick -> `TogglePower()`.
- Built on PH1 only so far. PH2/PH3/double still to do.

**Off-screen backing (PH1, in progress).** When the screen powers off, the bare metal behind the canvas shows instead of a dark screen. Adding an always-on black backing canvas (`Screen_Backing_01`) sized to the canvas, sitting just behind the screen canvas (not toggled by the power button) so OFF reads as a black screen. Mid-setup at end of day.

## What broke and how I fixed it

- **Knob on PH1 stopped grabbing** after adding the power-button canvas. Cause: the duplicated canvas kept its BoundsClipper at the old screen size (800x480), so a big invisible poke surface blanketed the knob and stole the grab. Fix: shrank the BoundsClipper to the button size (60x60x0.01). Lesson for replicating to other heads: after duplicating a screen canvas for a button, always shrink the BoundsClipper (and ray surface) to the button, or it blankets the area.
- **Couldn't enter Play mode in Link** after the screen-backing work — `DeinitializeLoader without an initialized manager` (plus the harmless `XR_ERROR_ACTIONSET_NOT_ATTACHED` haptics warning, which is startup noise). Not a scene-content bug — a wedged editor XR loader after heavy scene edits; deleting the backing didn't clear it. Fix: full Unity restart reset the loader, Play returned. Lesson: that specific error = wedged XR loader, restart Unity first.

## Decisions

- **Patient monitor canvas is square 720x720** (matches the ECG face); display-only, no thresholds.
- **Power = a real `powered` state**, not just screen visibility — reusable for scenario gating later. Screen starts OFF, poke to power on.
- **Power-button interaction via Route A** (reuse canvas-poke) — avoids the unfamiliar PokeInteractable-on-3D-mesh path.
- **Off screen shows a black backing canvas** sized to the canvas (ignore the metal placeholder mismatch).
- **First physiology coupling will be RPM -> flow** (perfusionist-validated core loop), gated on the flow coefficient from Hashir/KRB.

## Open questions / next

- **Finish PH1 black backing** (seat it just behind the screen canvas, confirm OFF=black, ON=screen-over-black).
- **Replicate power button + backing to PH2, PH3, and the double head** — remember the BoundsClipper shrink each time.
- **Optional follow-up:** gate rotor/audio/run-state on `powered` (a powered-off pump shouldn't spin or hum). Not done yet.
- **Physiology (later, the real problem):** RPM->flow coupling — get the L/min-per-RPM-per-tube-size coefficient from Hashir/KRB first. BSA screen sets target flow.
- Pump-mode mapping discussion (arterial-assignable, double-head suction/vent) still pending with HLM team.
- Patient monitor shows static placeholders until physiology/scenario data drives PatientState.

## Time spent

~

## Files modified today

- `Assets/ScreenSpecs/Patient_Monitor.json` — new (square monitor layout)
- `Assets/Scripts/PatientMonitorController.cs` — new (reads PatientState -> monitor readouts)
- `Assets/Scripts/PumpHeadRotor.cs` — direction reversal now ramps (angular momentum); cleaned duplicate header / unused field
- `Assets/Scripts/DoublePumpHeadRotor.cs` — same momentum ramp for both rotors
- `Assets/Scripts/PumpHeadState.cs` — added `powered` bool
- `Assets/Scripts/DoublePumpHeadState.cs` — added `powered` bool
- `Assets/Scripts/PumpPowerButton.cs` — new (poke toggles powered, shows/hides console canvas)
- `Assets/Scenes/OR_Environment.unity` — Patient_Monitor screen on ECG canvas; PH1 power-button canvas + PumpPowerButton; Screen_Backing_01 (in progress)
- `devlog/day-40.md` — new
