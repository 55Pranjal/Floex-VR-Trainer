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

## Afternoon session — Double pump head (slot 4) end-to-end

Built the full interactive canvas for slot 4 in one continuous session.

### Canvas + screens

- Duplicated `PumpHead_03_Canvas` → `PumpHead_04_Canvas`, repositioned on slot 4's X
- Stripped single-pump Screen1 (incompatible layout); kept Screen2_1, Screen3, Screen4, Screen5 since master-slave / pulse / fine cal / diagnostics are identical to singles
- Built two new screens via ScreenBuilder JSON:
  - `Screen_PumpHead04_Screen1` — double pump home (two independent flow/RPM/STOP columns, dark ratio strip, full-width header/footer, NavStrip spanning header-to-footer)
  - `Screen_PumpHead04_FlowRatio` — new picker, 11 options (NIL + 10 ratios), CANCEL/APPLY, home_border icon
- Duplicated `Tube_Size_1` → `Tube_Size_2` and `Direction` → `Direction_2` in-place for Pump B (single tube/direction JSON reused; the duplicates are functionally identical pickers driven by `activePicker` flag)

### Scripts written

State + navigation:
- `DoublePumpHeadState.cs` — per-canvas state container. Tracks Pump A and Pump B independently (tube size, direction, running state), shared flow ratio, session timer, bypass toggle, and all shared-screen fields (master-slave, pulse mode, fine cal). `ActivePump` enum (None/A/B) tells the picker which pump it was opened for.
- `DoublePumpHeadNavigator.cs` — clone of `PumpHeadNavigator` adapted for the double layout. 5 picker entry points on home (PumpA tube/dir, PumpB tube/dir, Ratio), sets `activePicker` before opening tube/direction. NavStrip targets shared screen names for 2_1/3/4/5 and double-pump names for Screen1/FlowRatio.

Screen controllers (all read/write `DoublePumpHeadState`, all return to `Screen_PumpHead04_Screen1`):
- `DoublePumpScreen1Controller.cs` — home screen. Two independent STOP/START buttons with per-pump readout swap (option C: STOP↔START label flip on tap, 0.00/000 ↔ runningLpm/runningRpm), play/pause sprite swap, bypass toggle, timer rendering
- `DoublePumpExclusivePicker.cs` — Tube_Size and Direction pickers. Reads `state.activePicker` to know whether to write to `pumpA_*` or `pumpB_*` field on APPLY
- `FlowRatioController.cs` — 11-option ratio picker. Single shared field, no Pump A/B branching
- `DoublePumpScreen2_1Controller.cs` — Master-Slave (6 exclusive options + 0–200 flow spinner)
- `DoublePumpScreen3Controller.cs` — Pulse Mode (4 counters + on/off toggle)
- `DoublePumpScreen4Controller.cs` — Fine Calibration (3 counters + on/off toggle)
- `DoublePumpScreen5Controller.cs` — Diagnostics (CANCEL/APPLY both navigate home, no internal logic — matches firmware Screen5Presenter behavior)

Architecture decision: went with **Option C (duplicate controllers)** over reusing the single-pump controllers via interface adapters or dual-state hack. Zero risk to working single-pump code, no overcomplicated navigator patches. Costs 4 near-duplicate files; future shared-behavior fixes need to be applied twice. Acceptable trade for Product A.

### Plus: backfilled `Screen5Controller.cs` for single pump head canvases

Single pumps were missing a Screen5 controller too. Created and attached to all three single pump canvases. Same minimal-firmware behavior (CANCEL/APPLY → home).

### Bugs surfaced in VR testing (end-of-day)

1. **Screen5 CANCEL/APPLY not navigating** on any canvas — caused by wrong button name lookup in my controllers. Used `Img_BtnCancel` / `Img_BtnApply` (matching Screen2_1/3/4 convention), but Screen5's actual names are `Btn_Cancel_Bg` / `Btn_Apply_Bg` (picker convention). Fixed both Screen5 controllers. Also added `DisableTextRaycast` calls so clicks land on the bg image not the text overlay.

2. **Intermittent hold-to-toggle behavior** on toggles/spinners across both single and double pump canvases. Symptom: holding the trigger caused the state to toggle continuously while held, reverting on release. Cause: Meta XR SDK's `InteractorState` flickers between `Select` and `Hover` during a single held trigger (small hand movement → ray briefly leaves rect → state drops → re-enters → new click edge fires). The edge detection in `CanvasClickBypass` was correct, but each flicker counted as a new edge.

   Fix in `CanvasClickBypass.cs`: added per-target click cooldown (`Dictionary<GameObject, float> lastClickTime`, 0.3s window). Same button can't re-fire within 300ms even if state flickers. Per-target (keyed by GameObject) rather than per-interactor handles both flicker-on-one-interactor and the multi-interactor-same-button case.

## What's still left

1. **VR testing of double pump head** — was deferred this afternoon because someone else was using the headset. Need to verify:
   - All 5 picker entries on Screen1 open correct pickers and update labels on APPLY
   - NavStrip P2-P5 navigates correctly
   - Per-pump STOP/START toggle each pump independently
   - Timer + bypass toggle work
   - Flow ratio picker commits to shared `state.flowRatio` and home button shows new value
   - All four other canvases (01/02/03 + 04) work in parallel after CanvasClickBypass cooldown fix
2. **Replace placeholder 3D model in slot 4** with the actual double pump head STEP file — pending teammate handoff
3. **Screen5 clarification** with the team tomorrow on actual intended functionality (the warning triangle / Service Need icon, whether days fields should derive from hours, training-mode default values)

## Carry-overs (unchanged from Day 17)

- VR scale verification using HLM dimensions (currently fine at 0.0002)
- tube_circle_3.png renders white/gray instead of green
- BSA OK/EXIT buttons unwired
- Two medical_instrument_tray instances in scene
- 16KB-aligned warning on libUnityOpenXR.so (Android 15)
- Floaid.lnk Windows shortcut in git
- Peer-reviewed publication co-authorship for Pranjal once Product A ships

## Files added/modified — afternoon

**New:**
- `Assets/ScreenSpecs/PumpHead_04_Screen1.json`
- `Assets/ScreenSpecs/PumpHead_04_FlowRatio.json`
- `Assets/Scripts/DoublePumpHeadState.cs`
- `Assets/Scripts/DoublePumpHeadNavigator.cs`
- `Assets/Scripts/DoublePumpScreen1Controller.cs`
- `Assets/Scripts/DoublePumpExclusivePicker.cs`
- `Assets/Scripts/FlowRatioController.cs`
- `Assets/Scripts/DoublePumpScreen2_1Controller.cs`
- `Assets/Scripts/DoublePumpScreen3Controller.cs`
- `Assets/Scripts/DoublePumpScreen4Controller.cs`
- `Assets/Scripts/DoublePumpScreen5Controller.cs`
- `Assets/Scripts/Screen5Controller.cs`

**Modified:**
- `Assets/Scripts/CanvasClickBypass.cs` — added per-target click cooldown (0.3s) to suppress Meta XR state-flicker re-fires

**Scene:**
- `PumpHead_04_Canvas` added with full child tree (Screen1, FlowRatio, Tube_Size_1, Tube_Size_2, Direction, Direction_2, Screen2_1, Screen3, Screen4, Screen5)
- All 10 controllers wired on slot 4
- Screen5Controller added to PumpHead_01/02/03 canvases
- CanvasClickBypass on PumpHead_04_Canvas with 4 RayInteractors wired

## Commit status

Slot 4 functionally complete pending VR verification. Single-pump Screen5 backfill complete. Click cooldown fix applied. Ready to commit at end of session.