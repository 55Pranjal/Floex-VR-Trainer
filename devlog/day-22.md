# Day 22 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 09 Jun 2026

---

## Goal

Two sessions today. Morning: motor rotation across all pump heads (Option 2 Week 2 deliverable). Evening: start direct-touch (Poke) interaction on the pump head canvases — prototype on slot 1, then propagate.

## What I did

### Session 1 — Motor rotation + double pump head reimport

- Single pump rotation: `PumpHeadRotor.cs` (`transform.Rotate` on `Rotor_Assembly`, reads `state.running` and `state.directionForward`, 600°/s fixed). Attached to all three single pump heads with rotor refs wired.
- Double pump rotation: `DoublePumpHeadRotor.cs` handling two independent rotors (rotorA/rotorB), each reading `pumpA_Running`/`pumpB_Running` and the string-based `pumpA_Direction`/`pumpB_Direction` from `DoublePumpHeadState`.
- Discovered Day 19 double pump had no separable rotor sub-meshes — fused mesh structure made animation impossible. Reimported through the Day 21 pipeline (FreeCAD leaf-level selection → Blender join → decimate → FBX).
- Lid removal: excluded Safety cap for DHP, Service cap Female part, and Service cap Female part001 from the mesh selection (Shiv approved). Rotors now visible from above.
- Joined two rotor assemblies (`Rotor_Assembly_A` right, `Rotor_Assembly_B` left), each with ~19 parts (8 caps + 8 roller pins + thumb wheel + 2 rotor screws).
- Origin set via Center of Mass (Volume) on both rotors — landed at axis.
- Protected decimation expanded protected list to include both rotor names, plus 5Inch and Display catchalls. Landed at 221k tris (down from 300k).
- Imported v2 at Scale Factor 0.001. Both rotors wired into `DoublePumpHeadRotor`.
- VR-tested: all four pump heads spinning independently with correct direction and start/stop response.

### Session 2 — Direct touch (Poke) on slot 1

**Verified rig setup.** OVRCameraRig already has `ControllerPokeInteractor` x2 and `HandPokeInteractor` x2 wired in from earlier project setup. No rig changes needed — interactors were sitting there waiting for `PokeInteractable` targets.

**Audited slot 1 canvas's existing interaction stack.** `PumpHead_01_Canvas` has active Pointable Canvas, Graphic Raycaster, Canvas Click Bypass, plus a child `ISDK_RayInteraction` GameObject holding the Ray Interactable + Plane Surface. There are also three "Removed" components on the canvas (Pointable Canvas, Ray Interactable, Plane Surface) — leftover from earlier architecture experiments, disabled and harmless. Also noticed slot 1's canvas is a prefab instance of `PumpHead_02_Canvas` (legacy naming), so modifications need to be applied as overrides on slot 1, not propagated to the prefab.

**Built the Poke pipeline on slot 1 as a sibling to the existing Ray setup.** Created `ISDK_PokeInteraction` child under `PumpHead_01_Canvas` with:

- `Poke Interactable` (the main interaction registrant)
- `Plane Surface` (defines the canvas plane, facing Backward to match Ray's existing Plane Surface)
- `Bounds Clipper` (Position 0,0,0, Size 800x480x0.01 — matches canvas dimensions from the JSON specs)
- `Clipped Plane Surface` (combines the above into something that implements `ISurfacePatch`, which is what `PokeInteractable.SurfacePatch` requires)

**Wired references on Poke Interactable:**

- Pointable Element → `PumpHead_01_Canvas` (resolves to its `PointableCanvas` component)
- Surface Patch → `ISDK_PokeInteraction` (resolves to its `ClippedPlaneSurface`)

**Hit and resolved the click-routing issue.** Initial VR test showed: hand stops at canvas (proximity detection working), but clicks don't fire. Root cause: Day 19's `CanvasClickBypass` disables this canvas's `PointableCanvas` to prevent dual-pipeline Ray clicks. Poke events flow through `PointableCanvas` → `PointableCanvasModule`, so a disabled PointableCanvas means Poke events go nowhere.

Considered two fixes:

- Extend `CanvasClickBypass` to also poll `PokeInteractor.State` and dispatch clicks the same way it does for Ray. Architecturally consistent, but Poke has fundamentally different mechanics than Ray (fingertip position vs ray intersection, depth-crossing detection vs trigger-button edge). Would have ended up as two separate code paths inside one script.
- Re-enable PointableCanvas selectively per canvas, letting Poke flow through PCM normally while Ray continues to use the bypass. The Day 19 dual-pipeline bug doesn't recur because Ray and Poke use different interactor sources — PCM only receives Poke events on this canvas, Bypass only handles Ray.

Went with the second approach. Added a public `disablePointableCanvas` flag to `CanvasClickBypass`, defaulting to `true` to preserve existing behavior on Ray-only canvases. Wrapped the disable line in `Awake()` with `if (disablePointableCanvas)`. Slot 1 has the flag unchecked; slots 2/3/4 keep it checked until they're also poke-enabled.

**VR test on slot 1: working perfectly.** Both interactions live and non-conflicting:

- Reaching out with index finger triggers proximity detection — hand stops at canvas surface
- Touching a button fires the click; navigation between Screen1/2_1/3/4/5 works via poke
- Ray-cast from a distance still works exactly as before (trigger button on controller, or hand point + pinch)
- No regressions on the Day 19 dual-pipeline click-hold bug

**Started propagating to slot 2.** Duplicated `ISDK_PokeInteraction` from slot 1, reparented under `PumpHead_02_Canvas`, attempted to re-wire references, unchecked `disablePointableCanvas` on slot 2's CanvasClickBypass. VR test failed — poke didn't register on slot 2. Out of time for the day, deferring diagnosis to tomorrow.

## What broke and how I fixed it

**Initial decimation script attempts (in Python text editor) didn't run cleanly** — used the `exec()` one-liner from the Day 21 pattern instead. Same workaround.

**Initially misread a rotor in front-orthographic view as a structural piece** (mistook the rotor disc + roller arms for a bracket). Rotated view confirmed it was a complete rotor. Lesson: always rotate before diagnosing.

**Day 19 double pump FBX had to be entirely reimported** because rotors weren't separable. Roughly 3 hours of rework, but result is cleaner geometry (no more dome base zigzag, no vent panel mesh artifacts) and proper sub-mesh structure for any future animation.

**Surface Patch field rejected initial drag.** `PokeInteractable.SurfacePatch` requires `ISurfacePatch`, not the `ISurface` that `PlaneSurface` implements. Plane Surface alone isn't enough. Added `BoundsClipper` (defines bounded rectangle region) + `ClippedPlaneSurface` (combines Plane Surface + Bounds Clippers into an `ISurfacePatch`-implementing wrapper). Dragged `ISDK_PokeInteraction` GameObject into the Surface Patch field, Unity resolved the interface correctly.

**Inspector throwing `Stack empty` / `Getting control 1's position` errors during Poke Interactable setup.** Editor rendering glitches from Unity trying to draw a malformed Surface Patch field before it was wired. Harmless — errors cleared once the field was populated correctly. Console showed 113 of these stacked before clearing.

**First Poke VR test: poke detects but no click fires.** Day 19's PointableCanvas disable was blocking Poke events. Fixed via the public-flag approach described above.

**Slot 2 propagation isn't working.** Unknown cause as of end-of-day. Possibilities to check tomorrow: the duplicated `ISDK_PokeInteraction` might still have references pointing back at slot 1's canvas (especially the Pointable Element), the Bounds Clipper Size may need re-verification against slot 2's canvas position/scale, or `disablePointableCanvas` may not have actually been unchecked on slot 2 (easy to miss in the Inspector).

## Decisions

- **Rotor speed: 600°/s (~100 RPM visual).** Looks clearly alive without being jarring. Worth confirming with Shiv against real pump RPM ranges, but acceptable for Product A.
- **Run-state trigger: tied rotation to `state.running` (STOP/START button on Screen1)** rather than `state.timerRunning` (Play/Pause). Timer is a separate concept from pump operation.
- **Direction: CW = positive rotation, CCW = negative.** Single uses bool `directionForward`; double uses string "CW"/"CCW". Both work.
- **Lid removal on slot 4: Shiv approved.** Worth flagging in v1.0 demo notes that real Floex has the cap on; we removed it so the rotors are visible.
- **Index finger only for Poke.** Meta's `HandPokeInteractor` defaults to using only the index fingertip as the poke origin. Considered enabling multi-finger but kept index-only: matches universal VR convention (system keyboard, all Meta demos), prevents accidental presses from curled-back fingers near the palm, and matches how a perfusionist would interact with a real touchscreen anyway.
- **Keep dual-pipeline (Ray + Poke) rather than replacing Ray with Poke.** Direct touch is the natural model for a console, but ray-cast remains useful where a user can't physically reach a screen. Both pipelines coexist cleanly because they use different interactor sources.
- **Slot 1 prototype first, then propagate.** Avoided the Day 19 mistake of changing all canvases at once and then debugging a 5-canvas mess.

## Open questions / next

- **Slot 2 poke isn't working.** Investigate tomorrow — likely a reference-wiring issue from the duplicated GameObject. Once slot 2 is working, slots 3 and 4 should be one Ctrl+D away each.
- **Bounds Clipper sizing on slot 4.** Same 800x480 canvas dimensions as singles, so the same bounds should apply, but verify visually after propagation.
- **The "Removed" components on each canvas** (Pointable Canvas, Ray Interactable, Plane Surface flagged as Removed) are accumulating cruft. Cleanup pass after poke propagation is complete.
- **Real-RPM-vs-visual-RPM coupling: hold the line.** Binary on/off only.
- **Week 2 of Option 2 roadmap is done modulo the slot 2-4 poke propagation.** Then Week 3 polish: bypass toggle visible reaction, alarm light blinking, basic spatial audio.

## Time spent

~11 hours across two sessions (morning ~7h: rotation scripts + double pump reimport; evening ~4h: poke prototype on slot 1 + failed slot 2 propagation).

## Files modified today

- `Assets/Scripts/PumpHeadRotor.cs` — new
- `Assets/Scripts/DoublePumpHeadRotor.cs` — new
- `Assets/Scripts/CanvasClickBypass.cs` — added `disablePointableCanvas` flag, wrapped the disable logic in `Awake()` with the conditional
- `Assets/Models/DoublePumpHead.fbx` — new (replaces `Double_pump_head.fbx`, deleted)
- `Assets/Scenes/OR_Environment.unity` — rotor scripts attached to all four pump heads; new double pump head placed; old placeholder knob/button and Day 21 duplicated CAD knob/button removed; `ISDK_PokeInteraction` added under `PumpHead_01_Canvas`; slot 1's `disablePointableCanvas` unchecked; slot 2 has a partial poke setup that isn't yet functional
- `devlog/day-22.md` — new