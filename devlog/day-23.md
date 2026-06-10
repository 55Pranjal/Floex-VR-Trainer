# Day 23 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 10 Jun 2026

---

## Goal

Morning: fix the slot 2 poke setup that failed at end of Day 22, propagate direct touch to the remaining canvases (slots 3, 4, ConsoleMount). Afternoon/evening (planned): RPM adjustment via the physical knob on each pump head — separate session, devlog to be extended.

## What I did

### Session 1 — Direct touch on all 5 canvases

**Fixed slot 2.** Worked through the three suspects from Day 22's notes (Pointable Element reference, Surface Patch / Clipped Plane Surface internals, disablePointableCanvas flag). The gap was the clipper wiring on the duplicated `ISDK_PokeInteraction` — the ClippedPlaneSurface's clipper reference hadn't carried over correctly from the slot 1 duplicate. Re-wired it; slot 2 poke working.

**Propagated to slots 3 and 4.** Same duplicate → reparent → re-wire pattern, this time checking the clipper reference explicitly. Both worked first try. Slot 4's Bounds Clipper kept the same 800x480 size (double pump canvas uses the same dimensions as singles).

**ConsoleMount (main pole canvas) — different architecture, simpler setup.** This canvas never had CanvasClickBypass; it's the one canvas still on Meta's standard PCM pipeline (works fine because it's not coplanar with the pump head canvases). Its PointableCanvas was already enabled, so no flag to flip. Added the same four-component poke stack (`Poke Interactable` + `Plane Surface` Facing Backward + `Bounds Clipper` + `Clipped Plane Surface`) on a new `ISDK_PokeInteraction` child. One critical difference: Bounds Clipper Size is **1280x800x0.01** — this canvas is 1280x800 per its Rect Transform, not 800x480 like the pump heads. Copy-pasting a pump head's clipper size would have silently broken poke detection on the outer regions.

**Wired and tested.** Pointable Element → ConsoleMount's Canvas, Surface Patch → the local ClippedPlaneSurface. VR test: poke works on the BubbleSensor screen, Setup menu, all ConsoleMount screens. Ray still works from a distance on all canvases.

**Result: direct touch working on all 5 canvases** (3 singles + double pump + ConsoleMount), dual-pipeline with ray-cast everywhere, no regressions on the Day 19 click-hold fix.

**Cleanup:** removed a Missing (Mono Script) component that was sitting on the EventSystem (orphaned slot from a deleted script).

## What broke and how I fixed it

**Slot 2's failed propagation (carried from Day 22).** Duplicating `ISDK_PokeInteraction` across canvases doesn't reliably carry the ClippedPlaneSurface's internal clipper reference. Fix: after every duplicate, explicitly verify both the Poke Interactable's two references (Pointable Element → the new parent canvas, Surface Patch → self) AND the Clipped Plane Surface's internals (Plane Surface → self, Bounds Clippers list entry → self). Four checks per canvas, ten seconds each, saves a failed VR test.

## Decisions

- **ConsoleMount keeps the standard PCM pipeline.** No CanvasClickBypass added — it doesn't have the coplanar-canvas problem, and PointableCanvas is already active, so poke routes through PCM natively. Less custom code on that canvas, not more.
- **Bounds Clipper Size must match each canvas's Rect Transform dimensions.** Pump heads: 800x480. ConsoleMount: 1280x800. Not a copy-paste field.

## Open questions / next

- **Afternoon session: RPM via physical knob.** Plan: add `rpmSetpoint` to state, make `KNOB_ENCODER010` grabbable with One Grab Rotate Transformer constrained to the knob axis, `KnobRpmDial.cs` converts rotation detents to setpoint steps, Screen1 readout displays it. **Scope line: rotor visual speed stays fixed at 600°/s — displayed RPM is a number, never a speed input.**
- **Ask Hashir/Shiv what the real Floex knob does** (RPM setpoint? menu navigation? press-to-confirm?) before finalizing the dial behavior — mimic the real encoder, don't invent one.
- **Slot 4 knob targets which pump?** One knob, two pumps. Likely the active/selected pump on the real machine — park until Hashir answers.
- **"Removed" components cleanup** on the pump head canvases (prefab-instance removals to apply) — still pending.
- **Full VR regression sweep** — pending, fold into end-of-day after the knob work.

## Time spent

~2 hours (slot 2 diagnosis + fix ~45min, slots 3/4 propagation ~30min, ConsoleMount setup + test ~45min). To be extended with the knob session.

## Files modified today

- `Assets/Scenes/OR_Environment.unity` — `ISDK_PokeInteraction` setups completed/fixed on PumpHead_02/03/04_Canvas; new `ISDK_PokeInteraction` (1280x800 bounds) under ConsoleMount's Canvas; `disablePointableCanvas` unchecked on slots 2/3/4; Missing script component removed from EventSystem
- `devlog/day-23.md` — new

Couldn't have the afternoon session as tokens finished, only studied the proposed plan for tomorrow.
