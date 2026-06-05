# Day 18 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-screens`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 05 Jun 2026

---

## What shipped today

**All three single pump head canvases (PumpHead_01/02/03) fully interactive in parallel in VR.** This was the blocker from Day 17 — closed today after a long debug session.

## The bug

After replicating PumpHead_01_Canvas to 02 and 03, only the most recently modified canvas would receive clicks — the other two would visually highlight on hover (laser dot landed on buttons) but produce zero downstream click events. All three PointableCanvas instances were correctly registered in `PointableCanvasModule`'s dictionary (confirmed via reflection-based dump), and all Inspector references were self-pointing correctly.

Diagnostic timeline:
- Verified Meta XR component refs all self-pointing per canvas
- Verified each canvas works in isolation (other two disabled)
- Confirmed via `Debug.Log` that clicks on canvas 02/03 produce zero `[Screen1] START tapped` log output — meaning `WhenPointerEventRaised` never fires for non-winner canvases at click time, even though hover-state ray work succeeds visually
- Ruled out: hierarchy order, structural pattern (PointableCanvas on parent vs. self, RayInteractable on child vs. self), surface clipping (BoundsClipper + RectTransformBoundsClipperDriver did not help), registration overwrites, prefab vs. duplication artifacts
- Compared against Meta's own `UISetExamples` sample (multiple canvases work fine there)

Root cause appears to be in how Meta XR Interaction SDK v74.0.0 routes pointer events from a shared `RayInteractor` to multiple coplanar world-space canvases — only one canvas's `WhenPointerEventRaised` actually fires per click. Not a registration bug. Not a clipping bug. Real SDK behavior that we couldn't find a supported configuration to override.

## The fix

**`CanvasClickBypass.cs`** — attached to each pump head canvas. Bypasses `PointableCanvasModule` entirely for these canvases by:

1. Reading the `RayInteractor`'s `Origin`/`Forward`/`State` directly each frame
2. Doing ray-vs-plane intersection against this canvas's transform
3. Checking the hit point against the canvas's `RectTransform.rect` (rejects rays that hit the infinite plane but miss the actual canvas bounds)
4. Running `GraphicRaycaster.Raycast` scoped to this canvas only
5. Dispatching `pointerEnter`/`pointerExit` (hover) and `pointerDown`/`pointerClick`/`pointerUp` (click) directly via `ExecuteEvents`
6. Edge-detecting selector state so each trigger press fires exactly one click

Result: each canvas runs its own independent input pipeline, immune to whatever single-canvas-wins logic exists inside Meta's module. The visible laser still comes from Meta's RayInteractor — we just read its state and route the clicks ourselves.

**Setup per pump head canvas:**
- Component `Canvas Click Bypass` added
- All 4 `RayInteractor` instances (left/right hand + left/right controller from OVRCameraRig) dragged into the `Ray Interactors` list

## What's still left for today (carry into the new chat)

1. **Double pump head 4th slot:** different screen set, different layout; build canvas + screens once 3D model is ready
2. **Replace single pump head placeholder 3D model with double pump head model**

## Carry-overs (unchanged from Day 17)

- VR scale verification using HLM dimensions (currently fine at 0.0002)
- tube_circle_3.png renders white/gray instead of green
- BSA OK/EXIT buttons unwired
- Two medical_instrument_tray instances in scene
- 16KB-aligned warning on libUnityOpenXR.so (Android 15)
- Floaid.lnk Windows shortcut in git
- Peer-reviewed publication co-authorship for Pranjal once Product A ships

## Files added/modified today

- `Assets/Scripts/CanvasClickBypass.cs` — **new**, the workaround
- `Assets/Scripts/Screen1Controller.cs` — diagnostic Debug.Log line added in OnStart (can be removed/kept; harmless)
- `Assets/Scripts/PointableCanvasMultiFixer.cs` — **delete this**, was a failed experiment that's now redundant
- Scene: 3 `Canvas Click Bypass` components added (one per pump head canvas), all wired to the 4 RayInteractors

## Commit status

Workaround working in VR. Ready to commit before moving to double pump head work.