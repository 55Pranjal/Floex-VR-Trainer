# Floex HLM VR — Project Context

VR familiarization trainer for perfusionists on the Floex 3.0 Heart-Lung Machine.
Built solo (Pranjal Agarwal, intern) with Claude as pair-programmer; Floaid team are clinical/engineering domain experts.
Target hardware: Meta Quest 3 (and Quest 3S).
Repo: github.com/55Pranjal/Floex-VR-Trainer

## Product scope (READ FIRST)

**Status: COMPLETE.** This is a finished VR familiarization trainer for the Floex 3.0
HLM. It is not under active expansion. The delivered product lets a trainee learn the
Floex console and practise the core perfusion setup task: given a patient, configure a
pump so that actual blood flow meets the patient's target flow, with a guided tutorial
that validates the setup and gives corrective feedback.

What the trainer does, end to end:
- Faithful Floex 3.0 console in VR — five interactive displays (three single pump
  heads, one double pump head, main pole console), poke + ray interaction.
- Full pump controls: power, start/stop, RPM knob, pump-type / tube-size / direction
  pickers. RPM-coupled rotor spin with momentum on reversal; spatial audio pitched to RPM.
- Patient setup on the BSA screen (on-device system keyboard) → BSA + target flow.
- Live actual flow per pump from RPM × tubing coefficient (real Floex calibration).
- Machine-wide arterial-role exclusivity across all heads/lanes.
- Patient vitals monitor on the OR's hospital monitor (not the HLM — matches real OR).
- Floating guided familiarization tutorial with setup validation + specific clues.

**Design frame.** The trainer is a scenario/task-based familiarization tool, NOT a
real-time physiology engine. It presents a patient (height/weight/CI) and assesses
whether the trainee sets the HLM up correctly for that patient. There is no
differential-equation physiology model driving the sim; clinical correctness lives in
fixed target ranges and confirmed setup facts (e.g. arterial direction). This scope was
set deliberately (Shiv, ~Day 38) and the trainer was completed within it.

Historical note: earlier context in this project aimed at a fuller clinical simulator
with a physiology engine and a multi-phase roadmap. That expansion was not pursued; the
product was completed as the familiarization trainer described above. References to
"Phase 3B physiology," a 28-week roadmap, or a scenario engine are historical and not
part of the delivered app.

## Working style (Pranjal's preferences)

- **Short, precise answers.** No fluff, no preamble. One step at a time. Wait for confirmation on ambiguity; commit to reasonable defaults otherwise.
- **VR testing is batched** — Pranjal tests multiple changes at once, not after every micro-change.
- **JSON is source of truth** for screen specs. Never nudge values in Unity Inspector; fix in JSON and rebuild via `Assets/Editor/ScreenBuilder.cs`. Unity-only nudging is reserved for final polish.
- **Devlog per day** at `devlog/day-NN.md`, fixed template: Goal / What I did / What broke and how I fixed it / Decisions / Open questions-next / Time spent. Single-line commit message given alongside each devlog.
- **Prose over bullets** in recommendations. Lists are fine for actual lists (steps, checks, parts to delete). Don't bullet-format an analysis.
- **Pranjal corrects mid-session** if a detail is wrong. Be responsive, not defensive.

## Environment (DO NOT CHANGE without good reason)

These versions were chosen after hitting bleeding-edge bugs. Do not "upgrade to latest."

- Unity 2022.3.62f3 (standard LTS — NOT Extended LTS, NOT Unity 6.x)
- Meta XR Core SDK v74.0.0 (NOT 200+ series — namespace collision bug with Gradle 9.1)
- Meta XR Interaction SDK v74.0.0 (com.meta.xr.sdk.interaction.ovr — the OVR variant)
  - MUST version-match Core SDK. Install 74.0.0 explicitly via the version dropdown — do NOT accept latest (200+) or it breaks the build.
- OpenXR + Oculus Touch Controller Profile + Meta Quest Support enabled
- URP, Single Pass Instanced rendering
- Target: Meta Quest 3 (and 3S), Android, ARM64, IL2CPP
- Package name: com.floaid.floexvr
- IDE: VS Code. Version control: Git + Git LFS. Repo: github.com/55Pranjal/Floex-VR-Trainer
- OS: Windows 11. Project path: C:\Floaid\Floex HLM VR (local, NOT OneDrive)

Asset pipeline tools: FreeCAD 1.1.1, Blender 4.5 LTS. Quest device tools: Meta Quest Developer Hub (logcat/device management).

URP render settings note: Intermediate Texture Mode set to "Auto" (was "Always" — perf fix, Day 32). Dynamic Resolution left ENABLED for shipping; disable it only during 72fps profiling to read honest GPU cost.

## Architecture overview

**Per-canvas pattern.** Each pump head canvas is a self-contained interactive unit owning its own state, navigator, and interaction routing. Five canvases: PumpHead_01/02/03_Canvas (singles), PumpHead_04_Canvas (double pump), plus the main pole canvas.

**Decoupled state vs display.** `PumpHeadState.cs` (and `DoublePumpHeadState.cs` for slot 4) is a pure data container — no Unity events, no display logic. Screen controllers read state one-way and update UI; button handlers write to state via methods.

**Screens are JSON -> Unity.** JSON in `Assets/ScreenSpecs/` defines screens; `Assets/Editor/ScreenBuilder.cs` generates Unity objects. JSON uses positive Y; generator applies the Y-flip. To change layout: edit JSON, rebuild. Controllers find targets by name at runtime (`FindDeep`), so adding JSON elements + rebuild does NOT require re-wiring — only controller-component Inspector refs (on the screen root) persist.

**Navigators own button wiring.** `PumpHeadNavigator.cs` / `DoublePumpHeadNavigator.cs` handle screen lifecycle and button-to-method wiring. `Awake` blanket-disables raycast on the home screen, then re-enables known buttons via `HookButton`. Per-screen controllers handle their own buttons via the same pattern; navigator re-wires nav strip in each screen's OnEnable.

**Click pipeline.** Meta XR v74 `PointableCanvasModule` only routes pointer events to one of multiple coplanar canvases, which broke the 5-canvas setup. Solution: `CanvasClickBypass.cs` polls `RayInteractor.State == Select`, intersects the ray with the canvas plane, dispatches synthetic PointerClick via `GraphicRaycaster` + `ExecuteEvents`. `disablePointableCanvas` flag (default true) is unchecked on canvases also using direct touch — Poke flows through `PointableCanvas` -> PCM and needs it active; Ray bypasses that path, so the two coexist. IMPORTANT edge-state detail (Day 51 fix): the click edge is latched ONLY while the ray is over a UI element of THIS canvas, and reset on every off-canvas path — otherwise a select that begins off-canvas (e.g. grabbing the knob, or the ray crossing another canvas) can wedge the edge state and kill that canvas's ray clicks until full release.

**Direct touch (Poke) per canvas:** child `ISDK_PokeInteraction` holds `PokeInteractable` + `PlaneSurface` (Facing Backward) + `BoundsClipper` (sized to canvas, e.g. 800x480x0.01) + `ClippedPlaneSurface`. Poke Interactable's `Pointable Element` -> parent canvas; `Surface Patch` -> self (local `ClippedPlaneSurface`). Sibling `ISDK_RayInteraction` holds the Ray setup. Working on all 5 canvases.

**Motor rotation — RPM-COUPLED.** `PumpHeadRotor.cs` (singles) and `DoublePumpHeadRotor.cs` (double) spin the `Rotor_Assembly` sub-mesh at `rpmSetpoint x 6 deg/s` (6 deg/s per RPM = physically accurate, 1 RPM = 360deg/60s). Gated on running AND rpmSetpoint > 0 (running at 0 RPM = still rotor). Direction from `directionForward` (bool, single) / `pumpA_Direction` / `pumpB_Direction` ("CW"/"CCW", double). Direction reversal eases through zero (momentum ramp), not an instant flip.

**RPM knob (all 4 heads).** Real HLM has a wide knob cap that was missing from CAD (Shiv's diagnosis). Solution: a Unity-primitive cylinder `KnobCap` seated over the thin encoder shaft, tilted so local Y points down the barrel; rotation applied to the cap (cap-only, encoder static underneath). Primitive's clean centered origin + clean local-Y axis sidestepped all the Day 26-29 axis/origin problems. Grab + One Grab Rotate Transformer drives rotation; read-only `Knobrpmdial.cs` (singles) / `Doubleknobrpmdial.cs` (double, with Pump A/B selector) MEASURES cap rotation (projects transform.right onto axis plane, signed-angle delta, accumulates to RPM 0-250) and writes the setpoint. KnobCap is a Unity-primitive stand-in for a real part absent from source CAD.

**Arterial exclusivity.** `ArterialRegistry.cs` — the one global-state object in the app (single instance on a `Managers` GameObject). At most one pump head / lane holds the Arterial role machine-wide. `ExclusivePickerController` (SPH) and `DoublePumpPickerController` (DPH, per-lane) claim/release on APPLY; the picker dims + de-interacts the Arterial option when another head owns it (silent disabled-button block). Claimant is the head state (SPH) or a per-lane key object (DPH `laneKeyA`/`laneKeyB`). Default pump role is Nil, so no head boots owning arterial.

**Tutorial.** `TutorialController.cs` + a standalone floating `Tutorial_Panel`. One panel, body text swaps per step, one action button whose label/behaviour changes by step (START TUTORIAL / NEXT / DONE / RESTART) plus a BACK button hidden on the invite step. Six instruction steps then a DONE validation gate. Validation passes if any head is Arterial + correct direction (SPH only) + powered + running + flow within target ± tolerance; on fail it reports the first meaningful problem in priority order (no arterial / not running / wrong direction / flow low-or-high, with a "bigger tube" hint when RPM is maxed). Both Ray and Poke drive the panel; a 0.4s shared controller-side debounce (`Debounced()`) swallows the dual-pipeline double-fire. Reads target flow from `BSAFormController.TargetFlowLpm` / `HasTarget`.

## Hard-won gotchas

### Unity / build
- Ctrl+S after changing Renderer Features / Project Settings — Unity doesn't auto-save these.
- Remove SSAO from ALL UniversalRenderer assets for Quest perf.
- Developer mode only works on the Quest OWNER profile.
- Use com.meta.xr.sdk.core only — NOT com.meta.xr.sdk.all.
- Project lives at C:\Floaid\ — NOT under OneDrive (sync corrupts Library).
- Ignore Unity's nag to "Update to 201.0.0" on Meta SDKs — always decline.
- 16KB-alignment warning on `libUnityOpenXR.so` (Android 15) is benign for sideloaded/MDM Quest deploy — a Play Store gate only. Confirm-and-ignore unless targeting Play Store or bumping Unity.
- Public serialized Inspector fields ALWAYS override script defaults. Changing a default in code does nothing to an existing scene instance — the serialized value wins. When a "changed" default doesn't take, check the component in the Inspector first (this cost real debugging time on the tutorial flow tolerance, Day 49).

### Git on Windows
- `core.ignorecase false` required for casing renames to be picked up.
- `git checkout main && git pull` at session start.
- TextMeshPro fallback fonts (`LiberationSans SDF - Fallback.asset`) show as modified after sessions (Unity auto-rebuilds glyph atlases) — `git restore` them unless intended.
- Unity restamps `.fbx.meta` GUID/timestamp even when FBX unchanged — `git diff` before committing; keep substantive import-setting changes, drop metadata noise.
- No `grep` in PowerShell — use `git ls-files | Select-String <pattern>`.
- `Floaid.lnk` and `*_BurstDebugInformation_DoNotShip/` are gitignored + untracked (Day 32).

### Quest Link / Play-mode testing
- Quest Link streams the LIVE editor scene on Play — no APK build needed. Be at Link home, press Play (don't open the old standalone app).
- For Link Play-mode: OpenXR must be enabled on the WINDOWS/desktop tab of XR Plug-in Management (not just Android). Play mode uses PC platform settings.
- Oculus Touch Controller Profile must be in "Enabled Interaction Profiles" on the WINDOWS tab too, or controllers won't work in Play mode.

### Meta XR Interaction SDK v74
- Multiple coplanar world-space canvases break PCM routing — solved via `CanvasClickBypass.cs`. Don't fix at PCM level.
- `CanvasClickBypass` edge-state must be latched only after hit-testing (ray confirmed on a UI element of this canvas) and reset on every off-canvas path — else an off-canvas select (knob grab, cross-canvas ray drift) wedges the ray clicks. (Day 51.)
- `PokeInteractable.SurfacePatch` requires `ISurfacePatch`, not `ISurface` — use `ClippedPlaneSurface` (wraps `PlaneSurface` + `BoundsClipper`).
- `HandPokeInteractor` defaults to index-finger-only — correct for the trainer.
- Inspector `Stack empty` / `Getting control 1's position` errors during Poke setup when Surface Patch is empty — editor glitch, clears when wired. Ignore.
- Text drawn over a button must have `raycastTarget = false`, or it eats the poke/click meant for the button beneath it (the `_Label` over `_Bg` pattern). Controllers disable label raycasts explicitly.
- Ray + Poke both fire a Unity Button's onClick — on a state-machine button that advances (vs a toggle), the two pipelines double-fire ~1 frame apart. Guard with a short controller-side debounce; don't disable a pipeline (hands/poke are needed for the knob).

### Blender
- Ctrl+J (Join) only works with cursor over the 3D viewport, not the Outliner.
- Alt+Z (X-Ray) conflicts with NVIDIA GeForce Experience overlay — use the X-Ray icon.
- Python console chokes on for-loop indentation — wrap in `exec("...")` with `\n`.
- Decimation crushes fine detail — protected-list pattern: protect `Rotor_Assembly`, `DISP`, `SCA`, `CG`, `DST`, `Power button`, `KNOB`, `Display`, `5Inch` at 0.75; everything else 0.55.

### CAD pipeline (validated Days 17-22, 28)
- FreeCAD: open .step (uncheck "Ignore instance names"). Select ONLY leaf-level children — selecting a parent group flattens to a fused mesh.
- FreeCAD: visibility-toggle to find externally-visible parts; skip internal fasteners (BOLT, NUT, screws, PINs, SLEEVES, bearings).
- FreeCAD: Mesh workbench > "Create mesh from shape", Surface deviation 0.5mm (not 0.10).
- Blender: import OBJ -Z Forward / Y Up. For rotors, Join leaf parts into `Rotor_Assembly` (`_A`/`_B` for double), Origin to Center of Mass (Volume). Decimate (protected list). Shade Smooth. Export FBX Selected/Mesh only, -Z Forward/Y Up, Apply Scalings = FBX All, Smoothing = Face.
- Unity: import at Scale Factor 0.001 (Blender mm -> Unity m), NOT 1. Old fused-mesh transforms (scale 100, rot X=-89.98) do NOT transfer — position manually.
- Single-part fix pattern (Day 28): when one part in a multi-part import is deformed, isolate and reimport just that part, swap in scene — don't re-run the whole assembly.

### Importing external models (e.g. Sketchfab props)
- Prefer FBX for best native Unity import.
- If Extract Textures is greyed, textures aren't embedded: Extract Materials, then hand-assign each map. Suffix convention: D = albedo/ColorMap, N = NormalMap, R = RoughnessMap, M = MetallicMap.
- If colors look wrong after assigning maps, the normal map is likely not marked Texture Type = Normal map. Unity pops a "fix now" dialog — clicking Fix corrects the encoding and the whole model renders correctly. This, not sRGB/shader differences, is the usual "colors off" cause. (Doctor prop, Day 51.)

### Unity execution order
- Button wiring goes in `Start`, not `OnEnable` — `PumpHeadNavigator.Awake` blanket-disables home-screen raycasts, which would undo wiring done in OnEnable.

### VR text input / system keyboard
TMP_InputField does NOT receive focus through Meta's pointable-canvas pipeline on v74 — the system keyboard never opens via focus. Don't fight it.
Solution: catch the poke with a normal Button onClick, call TouchScreenKeyboard.Open() directly, write the returned string into a plain TMP label yourself (SystemKeyboardField.cs). No InputField needed.
THE system keyboard overlay only renders in an ON-DEVICE BUILD — never over Quest Link / editor Play. Testing the keyboard over Link shows nothing and looks broken (cost 2 days, Days 15-16). Always test keyboard on-device.
Requires Require System Keyboard checked on OVRManager (Quest Features).
Fields on the pole canvas (ScreenNavigator) need their raycast re-enabled in Start (navigator blanket-disables in Awake) — SystemKeyboardField does this itself.

## Delivered state (final, Day 51)

Completed VR familiarization trainer. All of the following built, on-device tested, and working:

- All 5 canvases interactive; navigation + pickers; all 4 pump heads independent state.
- Poke + Ray on all 5 canvases (with the Day-51 edge-state hardening in CanvasClickBypass).
- RPM-coupled rotor spin with momentum ramp on direction reversal; knob caps all 4 heads.
- Spatial audio (Unity built-in 3D, Spatial Blend 1.0; pump hum pitch-coupled to RPM; self-generated mono clips). Gated on running && rpm>0 && powered.
- 72fps on-device baseline.
- Power button per pump head (Route A: transparent button canvas over the physical button mesh). Powered state gates screen/rotor/audio; black backing canvas makes OFF read as a dark screen.
- Patient monitor on the hospital monitor asset (720x720 canvas). PatientMonitorController reads PatientStateDriver.State → vitals (display-only). Patient data on the hospital monitor, NOT the HLM (per Shiv, matches real OR).
- BSA screen: on-device system-keyboard entry via SystemKeyboardField. Calculate → BSA = sqrt(h*w/3600) (Mosteller); target flow = CI × BSA (CI entered or default 2.5). Stashes TargetFlowLpm/HasTarget for the tutorial validator.
- Actual flow per pump: GetFlowLpm() = LpmPerRpm[tubeIndex] × rpmSetpoint. Coefficients from real Floex tables (roller pump = linear): 1/4 = 0.00602, 3/8 = 0.02598, 1/2 = 0.04498 LPM/RPM. DPH is fixed 1/4 (tube-select removed), per-lane GetFlowLpmA/B.
- Arterial exclusivity across all 3 SPH heads + both DPH lanes (ArterialRegistry).
- DPH per-lane pump-select (DoublePumpPickerController), Nil-default roles.
- Guided familiarization tutorial (TutorialController + Tutorial_Panel): full BSA → target → power → arterial/direction → START → match-flow walkthrough with DONE validation and per-check corrective clues. Arterial correct direction = Reverse (CCW), confirmed by HLM lead (Day 51); graded on SPH.
- Static doctor prop in the OR (Day 51).

Confirmed clinical facts baked into the trainer:
- Target flow is evaluated against the ARTERIAL pump only.
- Correct arterial direction is Reverse / CCW (HLM lead, Day 51).
- Patient vitals live on the hospital monitor, not the HLM console.

## Team & decision-making

- **Pranjal Agarwal** — solo intern developer.
- **Shiv** — CEO, scope-decision authority.
- **Hashir** — firmware/CAD owner.
- **KRB** — clinical reviewer.
- **Sundaraganesan** — PMS/telemetry contact.
- **Jai Raman** — advisor.

## Key file/folder locations

- Repo root: `C:\Floaid\Floex HLM VR\`
- Scripts: `Assets/Scripts/` (note actual filenames: `Knobrpmdial.cs`, `Doubleknobrpmdial.cs`, `Pumphead1Screen1Controller.cs`, `SystemKeyboardField.cs`, `PumpPowerButton.cs`, `PatientMonitorController.cs`, `PatientStateDriver.cs`, `ArterialRegistry.cs`, `ExclusivePickerController.cs`, `DoublePumpPickerController.cs`, `TutorialController.cs`, `CanvasClickBypass.cs`)
- Screen JSON: `Assets/ScreenSpecs/`
- ScreenBuilder: `Assets/Editor/ScreenBuilder.cs`
- Models: `Assets/Models/` (SinglePumpHead.fbx, DoublePumpHead.fbx, KnobEncoder.fbx, Floex_Trainer.fbx, Hospital/ incl. Doctor/)
- Materials: `Assets/Materials/Floex/Mat_Floex_Steel.mat`
- Main scene: `Assets/Scenes/OR_Environment.unity`
- Devlogs: `devlog/day-NN.md`