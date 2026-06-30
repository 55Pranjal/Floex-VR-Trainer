# Floex HLM VR — Project Context

VR training simulator for perfusionists on the Floex 3.0 Heart-Lung Machine.
Built solo (Pranjal Agarwal, intern) with Claude as pair-programmer; Floaid team are clinical/engineering domain experts.
Target hardware: Meta Quest 3 (and Quest 3S).
Repo: github.com/55Pranjal/Floex-VR-Trainer

## Product scope (READ FIRST)

**Currently building toward Option 3 (full clinical simulator), Phase 3A.** The project has moved past pure Product A. As of Day 29, Shiv formally approved the first deliberate step into physiology: rotor visual spin speed is now coupled to the RPM setpoint. This was previously the #1 named project risk ("don't let rotor speed track RPM") — it is now an authorised direction.

**Physiology build is GREENLIT and decision authority is delegated to the dev seat (Pranjal) for the current phase (as of Day 35).** Shiv has approved building physiology and delegated the per-coupling build decisions to Pranjal — they no longer require a fresh Shiv sign-off each time. Approved couplings so far: rotor visual speed tracks RPM (Day 29); pump hum audio pitch tracks RPM (Day 35). New physiology couplings can be built without gating.

Claude's standing instruction under this delegation: do NOT block or demand re-approval for physiology work — the build call is Pranjal's. Claude should still briefly *flag* (one line, non-blocking) when a step (a) introduces a genuinely new class of physiology behavior, or (b) touches clinical correctness/accuracy (KRB's domain), so it's visible in the log and chosen knowingly. Flagging != gating. Overall scope direction (e.g. committing to a whole new phase or product) still belongs to Shiv.

Roadmap state: Floex_VR_Option3_Roadmap.docx is the reference plan, but the product has been reframed (Shiv, ~Day 38) as a scenario-based trainer: present a predefined patient (height/weight/CI/condition) and assess whether the trainee operates the HLM correctly for that patient — NOT a continuous real-time physiology engine. Clinical knowledge lives in scenario definitions + correct-answer ranges (KRB-authorable), not validated differential equations. Phase 3A cleared (exit demo passed, Day 35). Now building the physiology/scenario foundation: patient input (BSA screen), target flow, actual flow.

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

Asset pipeline tools: FreeCAD 1.1.1, Blender 4.5 LTS. Quest device tools: Meta Quest Developer Hub (logcat/device management). Screen recording: OBS Studio + Iriun Webcam (phone-as-webcam over USB).

URP render settings note: Intermediate Texture Mode set to "Auto" (was "Always" — perf fix, Day 32). Dynamic Resolution left ENABLED for shipping; disable it only during 72fps profiling to read honest GPU cost.

## Architecture overview

**Per-canvas pattern.** Each pump head canvas is a self-contained interactive unit owning its own state, navigator, and interaction routing. Five canvases: PumpHead_01/02/03_Canvas (singles), PumpHead_04_Canvas (double pump), plus the main pole canvas.

**Decoupled state vs display.** `PumpHeadState.cs` (and `DoublePumpHeadState.cs` for slot 4) is a pure data container — no Unity events, no display logic. Screen controllers read state one-way and update UI; button handlers write to state via methods.

**Screens are JSON -> Unity.** JSON in `Assets/ScreenSpecs/` defines screens; `Assets/Editor/ScreenBuilder.cs` generates Unity objects. JSON uses positive Y; generator applies the Y-flip. To change layout: edit JSON, rebuild. Controllers find targets by name at runtime (`FindDeep`), so adding JSON elements + rebuild does NOT require re-wiring — only controller-component Inspector refs (on the screen root) persist.

**Navigators own button wiring.** `PumpHeadNavigator.cs` / `DoublePumpHeadNavigator.cs` handle screen lifecycle and button-to-method wiring. `Awake` blanket-disables raycast on the home screen, then re-enables known buttons via `HookButton`. Per-screen controllers handle their own buttons via the same pattern; navigator re-wires nav strip in each screen's OnEnable.

**Click pipeline.** Meta XR v74 `PointableCanvasModule` only routes pointer events to one of multiple coplanar canvases, which broke the 5-canvas setup. Solution: `CanvasClickBypass.cs` polls `RayInteractor.State == Select`, intersects the ray with the canvas plane, dispatches synthetic PointerClick via `GraphicRaycaster` + `ExecuteEvents`. `disablePointableCanvas` flag (default true) is unchecked on canvases also using direct touch — Poke flows through `PointableCanvas` -> PCM and needs it active; Ray bypasses that path, so the two coexist.

**Direct touch (Poke) per canvas:** child `ISDK_PokeInteraction` holds `PokeInteractable` + `PlaneSurface` (Facing Backward) + `BoundsClipper` (sized to canvas, e.g. 800x480x0.01) + `ClippedPlaneSurface`. Poke Interactable's `Pointable Element` -> parent canvas; `Surface Patch` -> self (local `ClippedPlaneSurface`). Sibling `ISDK_RayInteraction` holds the Ray setup. Working on all 5 canvases.

**Motor rotation — NOW RPM-COUPLED (Shiv-approved Day 29).** `PumpHeadRotor.cs` (singles) and `DoublePumpHeadRotor.cs` (double) spin the `Rotor_Assembly` sub-mesh at `rpmSetpoint x 6 deg/s` (6 deg/s per RPM = physically accurate, 1 RPM = 360deg/60s). Gated on running AND rpmSetpoint > 0 (running at 0 RPM = still rotor). Direction from `directionForward` (bool, single) / `pumpA_Direction` / `pumpB_Direction` ("CW"/"CCW", double). This replaced the old fixed 600 deg/s.

**RPM knob (complete across all 4 heads).** Real HLM has a wide knob cap that was missing from CAD (Shiv's diagnosis). Solution: a Unity-primitive cylinder `KnobCap` seated over the thin encoder shaft, tilted so local Y points down the barrel; rotation applied to the cap (cap-only, encoder static underneath). Primitive's clean centered origin + clean local-Y axis sidestepped all the Day 26-29 axis/origin problems. Grab + One Grab Rotate Transformer drives rotation; read-only `Knobrpmdial.cs` (singles) / `Doubleknobrpmdial.cs` (double, with Pump A/B selector) MEASURES cap rotation (projects transform.right onto axis plane, signed-angle delta, accumulates to RPM 0-250) and writes the setpoint. On-screen +/- spinner plan was dropped — knob precision on the clean cap is sufficient. KnobCap is a Unity-primitive stand-in for a real part absent from source CAD; may want a proper mesh eventually.

## Hard-won gotchas

### Unity / build
- Ctrl+S after changing Renderer Features / Project Settings — Unity doesn't auto-save these.
- Remove SSAO from ALL UniversalRenderer assets for Quest perf.
- Developer mode only works on the Quest OWNER profile.
- Use com.meta.xr.sdk.core only — NOT com.meta.xr.sdk.all.
- Project lives at C:\Floaid\ — NOT under OneDrive (sync corrupts Library).
- Ignore Unity's nag to "Update to 201.0.0" on Meta SDKs — always decline.
- 16KB-alignment warning on `libUnityOpenXR.so` (Android 15) is benign for sideloaded/MDM Quest deploy — a Play Store gate only. Confirm-and-ignore unless targeting Play Store or bumping Unity.

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
- `PokeInteractable.SurfacePatch` requires `ISurfacePatch`, not `ISurface` — use `ClippedPlaneSurface` (wraps `PlaneSurface` + `BoundsClipper`).
- `HandPokeInteractor` defaults to index-finger-only — correct for the trainer.
- Inspector `Stack empty` / `Getting control 1's position` errors during Poke setup when Surface Patch is empty — editor glitch, clears when wired. Ignore.

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

### Unity execution order
- Button wiring goes in `Start`, not `OnEnable` — `PumpHeadNavigator.Awake` blanket-disables home-screen raycasts, which would undo wiring done in OnEnable.

### VR text input / system keyboard
TMP_InputField does NOT receive focus through Meta's pointable-canvas pipeline on v74 — the system keyboard never opens via focus. Don't fight it.
Solution: catch the poke with a normal Button onClick, call TouchScreenKeyboard.Open() directly, write the returned string into a plain TMP label yourself (SystemKeyboardField.cs). No InputField needed.
THE system keyboard overlay only renders in an ON-DEVICE BUILD — never over Quest Link / editor Play. Testing the keyboard over Link shows nothing and looks broken (cost 2 days, Days 15-16). Always test keyboard on-device.
Requires Require System Keyboard checked on OVRManager (Quest Features).
Fields on the pole canvas (ScreenNavigator) need their raycast re-enabled in Start (navigator blanket-disables in Awake) — SystemKeyboardField does this itself.

## Current state (as of Day 43)

Phase 3A cleared (exit demo passed Day 35). Building the scenario/physiology foundation. Core target-vs-actual flow loop working.

Built and working:


All 5 canvases interactive; navigation + pickers; all 4 pump heads independent state
Poke + Ray on all 5 canvases; RPM-coupled rotor spin (now with momentum ramp on direction reversal — eases through zero, not instant flip); knob caps all 4 heads
Spatial audio (Unity built-in 3D, Spatial Blend 1.0; pump hum pitch-coupled to RPM; clips self-generated, mono). Audio gated on running && rpm>0 && powered.
72fps on-device baseline confirmed (constant 72, Day 35)
Power button per pump head (all heads). Real powered state on PumpHeadState/DoublePumpHeadState (default false). Poke button (Route A: tiny transparent button canvas over the physical button mesh, reusing the proven canvas-poke pipeline) toggles powered → shows/hides the console canvas. Screen starts OFF. Rotor + audio gated on powered. A black backing canvas (Screen_Backing_NN, always-on, behind the screen canvas) makes OFF read as a dark screen instead of bare metal.
Patient monitor on the ECG/hospital monitor asset (square 720x720 canvas). PatientMonitorController reads PatientStateDriver.State → 9 vitals (display-only, no thresholds). Patient data shows on the hospital monitor, NOT the HLM (per Shiv — matches real OR).
Physiology foundation: PatientState (pure C#, Floex.Physiology asmdef, 12 vars + 50ms tick), PatientStateDriver (20Hz fixed-step), 8 passing NUnit EditMode tests.
BSA screen text entry SOLVED. System keyboard overlay via SystemKeyboardField.cs — poke a field → TouchScreenKeyboard.Open() (NumbersAndPunctuation for numeric) → writes result into the TMP label. NO TMP_InputField (Meta pointable pipeline won't focus it). CRITICAL: the system keyboard ONLY appears in an on-device BUILD, never over Link — that's what cost 2 days previously. All editable BSA fields working.
Target flow: BSAFormController.Calculate() → BSA = sqrt(h*w/3600) (Mosteller); target flow = CI × BSA, CI entered on screen (Cardiac field, moved to editableFields) or default 2.5. Temperature modifier out for v1. Written to Txt_Target. (Real machine derives CI from flow sensors; entered CI is a TARGET-CI trainer simplification — KRB to confirm.)
Actual flow: PumpHeadState.GetFlowLpm() = LpmPerRpm[tubeIndex] × rpmSetpoint. Coefficients from the real Floex tables (roller pump = linear; verified vs machine tables): 1/4 = 0.00602, 3/8 = 0.02598, 1/2 = 0.04498 LPM/RPM. Tubes 5/16, F1, F2 = 0 (not provided; Shiv said skip for now). Screen1Controller shows it live on Txt_LpmValue, gated on running && powered && rpm>0.


Open clinical questions (gate the next step — evaluation layer):


Which pump's flow is compared to the BSA target — arterial only? (Flow calc itself is mode-independent mechanics — correct for all pumps. But evaluation against the patient target is arterial-pump-only.)
Pump-mode assignment (arterial selectable on any single head; double head = suction + vent per Shiv) — Shiv resolving with HLM team.
Do cardioplegia/suction/vent pumps have their own correctness criteria, or pass/fail on setup?

## Next / on the horizon

- Finish Phase 3A polish -> exit demo to Shiv.
- **Phase 3B (Week 4): `PatientState` pure-C# class** — 12 variables (HR, BP, SvO2, hematocrit, temp, arterial PO2/PCO2, pump flow, gas flow, FiO2, sweep gas, base excess, time-on-bypass), 50ms ticker, unit tests, fully decoupled from Unity.
- **GATING DEPENDENCY before 3B:** confirm KRB weekly availability Wk 6-9 with Shiv/KRB. Roadmap says slip 3B rather than start without KRB.
- Roadmap cadence: Shiv demo every 2 weeks; KRB weekly from Wk 6; advisory-board demo each phase end (Wk 3/9/13/19/28); CLAUDE.md weekly update; 25-30 hrs/week sustainable.

## Team & decision-making

- **Pranjal Agarwal** — solo intern developer.
- **Shiv** — CEO, scope-decision authority. Scope changes require his explicit approval.
- **Hashir** — firmware/CAD owner (Floex firmware specs + CAD; alarm spec for 3D).
- **KRB** — clinical reviewer (gating for Phase 3B physiology).
- **Sundaraganesan** — PMS/telemetry contact (Phase 3E telemetry).
- **Jai Raman** — advisor.

## Key file/folder locations

- Repo root: `C:\Floaid\Floex HLM VR\`
- Scripts: `Assets/Scripts/` (note actual filenames: `Knobrpmdial.cs`, `Doubleknobrpmdial.cs`, `Pumphead1Screen1Controller.cs`, `SystemKeyboardField.cs`, `PumpPowerButton.cs`, `PatientMonitorController.cs`, `PatientStateDriver.cs`, `Physiology/PatientState.cs`)
- Screen JSON: `Assets/ScreenSpecs/`
- ScreenBuilder: `Assets/Editor/ScreenBuilder.cs`
- Models: `Assets/Models/` (SinglePumpHead.fbx, DoublePumpHead.fbx, KnobEncoder.fbx, Floex_Trainer.fbx, Hospital/)
- Materials: `Assets/Materials/Floex/Mat_Floex_Steel.mat`
- Main scene: `Assets/Scenes/OR_Environment.unity`
- Devlogs: `devlog/day-NN.md`
- Roadmaps: `docs/Floex_VR_Option3_Roadmap.docx` (active), Option2 + 36Week (context)