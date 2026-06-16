# Day 29 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 16 Jun 2026

---

## Goal

Make the pump-head RPM knob feel more real: (1) couple the rotor's visual spin speed to the RPM setpoint instead of a fixed rate, (2) improve knob-twist measurement accuracy, and (3) plan a precise on-screen RPM input. The first item is a deliberate, approved scope change.

## Scope decision (significant — record it)

**Shiv approved coupling rotor visual spin speed to the RPM setpoint.** This is the first deliberate step from Product A (display-only familiarisation) toward physiology/Product B. Until today the #1 named project risk was exactly this kind of coupling, and it required explicit CEO sign-off — which was given today. This is now an authorised direction, not scope drift. Future physiology coupling still goes through the same approval gate; today's sign-off covers rotor-speed-tracks-RPM specifically.

## What I did

**Coupled rotor speed to RPM (PumpHeadRotor).** Changed `PumpHeadRotor` from a fixed 600°/s to `rpmSetpoint × degreesPerSecondPerRpm`, with the per-RPM rate defaulting to **6 °/s** — which is physically accurate (1 RPM = 360°/60s = 6°/s), and means the old fixed 600°/s look was already calibrated to ~100 RPM. Stopped, or running at 0 RPM, now shows a still rotor; spin visibly tracks the dialled value. (Double pump head gets the matching edit later, reading `pumpA/pumpB_RpmSetpoint`.)

**Attempted a quaternion-based knob measurement — reverted.** Rewrote `KnobRpmDial.MeasureRotation()` to use swing-twist decomposition (capture orientation at start, extract only the twist about the barrel axis each frame) plus a deadzone, aiming to kill the per-frame projection jitter. It made things worse: RPM stuck around 89 and went negative / climbed back to ~50 when twisted further. Root cause: the extracted twist angle wraps at ±180°, so twisting past half a turn flipped the sign and the count ran backwards. Reverted to the previous projection-based read-only version. Decided not to chase a turn-counting fix — the knob is an inherently imprecise way to hit an exact 1-RPM value, and the planned on-screen spinner solves precision properly.

**Planned the on-screen RPM spinner (for tomorrow).** Decided to add +/- buttons next to the RPM readout on Screen1 as the precise input path, keeping the physical knob as the "feel" interaction. Confirmed the approach: RPM lives in `PumpHeadState.rpmSetpoint` and is already displayed live by `Screen1Controller.UpdateRpmDisplay()`, so +/- buttons only need to change the setpoint — no display work. The existing `SpinnerController` won't fit cleanly (it owns its own ints + writes to Txt_*Val), so the wiring will go in `Screen1Controller` instead, matching its existing HookButton pattern. Buttons will be **hold-to-repeat with acceleration** (250 discrete taps to span 0–250 is unusable). Elements will be added to the Screen1 JSON (source-of-truth consistency) rather than hand-placed in Unity.

## What broke and how I fixed it

**Quaternion twist wrap-around.** The swing-twist angle is bounded to ±180°; past a half-turn it flips sign, so accumulated RPM ran backwards (the 89→negative→50 symptom). A correct fix would track full-turn count across the wrap boundary. Chose to revert rather than fix, because the spinner makes precise knob measurement unnecessary — the knob stays as a feel interaction (still rotates fine via the One Grab Rotate Transformer), spinner becomes the accurate path.

## Decisions

- **Rotor speed now tracks RPM** (Shiv-approved; first physiology step). 6°/s per RPM = physically accurate.
- **Knob measurement left as-is (projection version), not quaternion.** Not worth a turn-counting fix; spinner supersedes the need for knob precision.
- **Spinner over knob for precise RPM**, knob retained for feel. Real consoles often have both.
- **Spinner +/- to be hold-to-repeat with acceleration**, JSON-defined, wired in Screen1Controller writing to `state.rpmSetpoint`.

## Open questions / next (tomorrow)

- **Add Screen1 JSON elements** `Box_RpmUp` / `Box_RpmDown` (placement + sprite choice still to settle) and rebuild via ScreenBuilder. Expectation: no manual re-wiring needed, since controllers find targets by name at runtime (`FindDeep`) — only the controller-component Inspector refs persist, and those live on the screen root, not the generated elements. Verify ScreenBuilder regenerates visual children only, not the controller root, before rebuilding.
- **Hold-to-repeat tuning** — tap = ±1, hold = accelerating repeat so 0→250 takes ~1–2s.
- **Verify rotor coupling in VR** — confirm spin speed visibly tracks RPM across 0/50/150/250, and that a running pump at 0 RPM showing a still rotor reads correctly in the demo flow (dial up after START).
- **Double pump head rotor coupling** — apply the same RPM-tracking edit to `DoublePumpHeadRotor` for `pumpA`/`pumpB`.
- Carry-overs: propagate knob + spinner to single pumps 2/3 and slot 4 dual knobs.

## Time spent

~3 hours (rotor coupling ~30min, quaternion knob rewrite + debugging + revert ~2h, spinner planning + JSON/architecture review ~30min).

## Files modified today

- `Assets/Scripts/PumpHeadRotor.cs` — rotor spin speed now tracks `rpmSetpoint` (6°/s per RPM); was fixed 600°/s
- `Assets/Scripts/KnobRpmDial.cs` — quaternion-twist rewrite attempted then reverted to projection version (no net change)
- `devlog/day-29.md` — new