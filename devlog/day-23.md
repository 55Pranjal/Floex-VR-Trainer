# Day 23 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation` (continued; direct-touch work added on top)
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 09 Jun 2026

---

## Goal

Add Meta's direct-touch (Poke) interaction to the pump head canvases so users can reach out and physically press buttons with their fingertip, alongside the existing ray-cast. Prototype on slot 1 first, then propagate to slots 2/3/4. This was deferred from Day 22; Shiv had given a general go-ahead earlier and this is the most visually impactful of the Option 2 polish items.

## What I did

**Verified rig setup.** OVRCameraRig already has `ControllerPokeInteractor` x2 and `HandPokeInteractor` x2 wired in from earlier project setup. No rig changes needed — interactors were sitting there waiting for `PokeInteractable` targets.

**Audited slot 1 canvas's existing interaction stack.** Found a more sophisticated setup than I'd remembered: `PumpHead_01_Canvas` has active Pointable Canvas, Graphic Raycaster, Canvas Click Bypass, plus a child `ISDK_RayInteraction` GameObject holding the Ray Interactable + Plane Surface. There are also three "Removed" components on the canvas (Pointable Canvas, Ray Interactable, Plane Surface) — leftover from earlier architecture experiments, scheduled for removal but currently disabled and harmless. Also noticed slot 1's canvas is a prefab instance of `PumpHead_02_Canvas` (legacy naming), so any modifications need to be applied as overrides on slot 1, not propagated to the prefab.

**Built the Poke pipeline on slot 1 as a sibling to the existing Ray setup.** Created `ISDK_PokeInteraction` child under `PumpHead_01_Canvas` with three components:

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

**Started propagating to slot 2.** Duplicated `ISDK_PokeInteraction` from slot 1, reparented under `PumpHead_02_Canvas`, attempted to re-wire references, unchecked `disablePointableCanvas` on slot 2's CanvasClickBypass. VR test failed — poke didn't register on slot 2. Out of time for the day, so deferring diagnosis to tomorrow.

## What broke and how I fixed it

**Surface Patch field rejected initial drag.** `PokeInteractable.SurfacePatch` requires `ISurfacePatch`, not the `ISurface` that `PlaneSurface` implements. Plane Surface alone isn't enough. Added `BoundsClipper` (defines bounded rectangle region) + `ClippedPlaneSurface` (combines Plane Surface + Bounds Clippers into an `ISurfacePatch`-implementing wrapper). Dragged `ISDK_PokeInteraction` GameObject into the Surface Patch field, Unity resolved the interface correctly.

**Inspector throwing `Stack empty` / `Getting control 1's position` errors during Poke Interactable setup.** Editor rendering glitches from Unity trying to draw a malformed Surface Patch field before it was wired. Harmless — errors cleared once the field was populated correctly. Console showed 113 of these stacked before clearing.

**First VR test: poke detects but no click fires.** Day 19's PointableCanvas disable was blocking Poke events. Fixed via the public-flag approach described above.

**Slot 2 propagation isn't working.** Unknown cause as of end-of-day. Possibilities to check tomorrow: the duplicated `ISDK_PokeInteraction` might still have references pointing back at slot 1's canvas (especially the Pointable Element), the Bounds Clipper Size may need re-verification against slot 2's canvas position/scale, or the new component on slot 2 might not have had `disablePointableCanvas` actually unchecked (easy to miss in the Inspector).

## Decisions

- **Index finger only.** Meta's `HandPokeInteractor` defaults to using only the index fingertip as the poke origin. Considered enabling multi-finger but kept index-only: matches universal VR convention (system keyboard, all Meta demos), prevents accidental presses from curled-back fingers near the palm, and matches how a perfusionist would interact with a real touchscreen anyway.
- **Keep dual-pipeline (Ray + Poke) rather than replacing Ray with Poke.** Direct touch is the natural model for a console, but ray-cast remains useful for situations where a user can't physically reach a screen (looking around the OR from a fixed position, or interacting from outside the trainer's physical footprint). Both pipelines coexist cleanly because they use different interactor sources.
- **Slot 1 prototype first, then propagate.** Avoided the Day 19 mistake of changing all canvases at once and then debugging a 5-canvas mess. Slot 1 proved the architecture works before touching slots 2/3/4.

## Open questions / next

- **Slot 2 poke isn't working.** Investigate tomorrow — likely a reference-wiring issue from the duplicated GameObject. Once slot 2 is working, slots 3 and 4 should be one Ctrl+D away each.
- **Bounds Clipper sizing on slot 4.** Slot 4 (double pump head) uses the same 800x480 canvas dimensions as singles, so the same bounds should apply, but worth verifying visually that the wireframe overlays the canvas screen correctly after propagation.
- **The "Removed" components on each canvas** (Pointable Canvas, Ray Interactable, Plane Surface flagged as Removed but still listed in the Inspector) are accumulating cruft. Worth a cleanup pass after poke propagation is complete — should be straightforward to actually delete them now that we know they're not needed.
- **Week 2 of Option 2 roadmap is now done modulo the slot 2-4 propagation.** Once that's complete, the remaining work is Week 3 polish: bypass toggle visible reaction, alarm light blinking, basic spatial audio.

## Time spent

~4 hours (rig audit + canvas component audit ~30min, prototype build on slot 1 ~1.5h, debugging click-through issue ~30min, CanvasClickBypass update + retest ~30min, propagation attempt + failed slot 2 test + cleanup ~1h).

## Files modified today

- `Assets/Scripts/CanvasClickBypass.cs` — added `disablePointableCanvas` flag, wrapped the disable logic in `Awake()` with the conditional
- `Assets/Scenes/OR_Environment.unity` — added `ISDK_PokeInteraction` child under `PumpHead_01_Canvas` with Poke Interactable + Plane Surface + Bounds Clipper + Clipped Plane Surface; unchecked `disablePointableCanvas` on slot 1's CanvasClickBypass. Slot 2 has a partial setup that isn't yet functional.
- `devlog/day-23.md` — new
