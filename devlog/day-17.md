Day 19 Devlog — Floex VR Trainer
Branch: feature/pump-head-screens
Repo: github.com/55Pranjal/Floex-VR-Trainer
Date: 04 Jun 2026

What shipped today
Completed and verified all five single-pump-head screens on PumpHead_01_Canvas:

Screen1 (home): PUMP SELECT badge, START/STOP with running-state readout swap (5 values), session timer with play/pause sprite toggle, history reset, bypass on/off toggle, picker entries for tube/direction, nav strip.
Screen2_1 (Master-Slave): 6-option master picker + flow spinner (0–200 wrap) + Apply/Cancel.
Screen3 (Pulse Mode): 4 spinners (PF/PW/BF/PFL) + pulse mode toggle + Apply/Cancel.
Screen4 (Fine Calibration): 3 spinners (FC/Tube1/Tube2) + fine cal toggle + Apply/Cancel.
Screen5 (Diagnostics): display-only.
3 picker screens (Pump_Select_1, Tube_Size_1, Direction): home button wiring fixed (correct GameObject name is Img_HomeBorder, not Img_HomeBtnBorder).

Bugs fixed

Execution-order race on Screen1: at scene load, Screen1Controller.OnEnable could run before PumpHeadNavigator.Awake finished its blanket-disable, undoing button wiring. Fix: moved button wiring to Start() (runs after all Awakes). OnEnable still handles display refresh.
Home button silent fail on pickers: ExclusivePickerController was searching for Img_HomeBtnBorder; actual name is Img_HomeBorder. Renamed.
Timer survives navigation: moved tick logic into PumpHeadNavigator.Update() so the session timer keeps counting even when user navigates away from Screen1. Screen1Controller just renders the value when visible.

Where we got stuck
After replicating PumpHead_01_Canvas to 02 and 03, only one canvas at a time is interactive — and it's always whichever was duplicated/instantiated last.
Diagnostics completed:

✅ All Meta XR component references (PointableCanvas.Canvas, RayInteractable.PointableElement, RayInteractable.Surface) verified self-pointing on each canvas via Inspector
✅ All script wiring (State, Navigator refs) verified per-canvas
✅ Each canvas works fine when others are disabled (proves the components themselves are correct)
✅ Hierarchy order doesn't matter — last edited/created wins regardless of position
✅ Solo isolation test confirmed: conflict only appears when multiple canvases are active simultaneously
✅ Prefab instantiation approach gave same result (last-dragged wins) — not a duplication artifact, actual SDK behavior
❌ Per-canvas EventSystem + PointableCanvasModule approach rejected (would conflict with the existing scene-wide EventSystem used by the main console pole canvas)
❌ Remove-and-readd PointableCanvas + RayInteractable components didn't fix it

Diagnosis: Meta XR Interaction SDK v74.0.0's PointableCanvasModule appears to maintain a Canvas-keyed registration that gets overwritten when multiple PointableCanvas instances exist in the scene. The Inspector shows correct refs, but only one canvas "wins" the registration at runtime.
Pick up tomorrow

Fix multi-canvas Meta XR registration — write a runtime fixer script that forces per-instance registration in PointableCanvasModule, OR find an SDK-supported way to scope rays per canvas (Interactor groups / interaction layers).
Once all 3 single pump heads interactive independently: start the double pump head (4th slot) — different screen set, scoped when 3D model arrives.

Carry-overs (unchanged from Day 18)

VR scale verification using HLM dimensions (Shiv confirmed scale is fine for now)
tube_circle_3.png renders white/gray instead of green
BSA OK/EXIT buttons unwired
Two medical_instrument_tray instances in scene
16KB-aligned warning on libUnityOpenXR.so (Android 15)
Floaid.lnk Windows shortcut in git
Peer-reviewed publication co-authorship for Pranjal once Product A ships

Commit status
Backup committed before per-canvas EventSystem experiment. Hierarchy state preserved on feature/pump-head-screens.