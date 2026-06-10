# Floex HLM VR — Project Context

VR training simulator for perfusionists on the Floex 3.0 Heart-Lung Machine.
Built solo (Pranjal Agarwal, intern) with Claude as pair-programmer; Floaid team are clinical/engineering domain experts.
Target hardware: Meta Quest 3 (and Quest 3S).
Repo: github.com/55Pranjal/Floex-VR-Trainer

## Product scope (READ FIRST)

**Product A: console familiarisation trainer.** Screens look real, navigation works, motor rotates visibly when started, direct touch on canvases works. NO physiology, NO patient state, NO clinical scenarios, NO physics. Display values are preset (e.g. flow reads 4.50 L/PM when the pump is running, 0.00 when stopped — not a function of RPM or anything else).

The #1 named project risk is **scope drift toward a full simulator.** Every "but wouldn't it be nice if the flow tracked RPM" suggestion sounds reasonable in isolation. The line to hold: any value tied to RPM or physiology is Product B. Visual feedback (rotor spinning, alarm lights blinking) is fine as long as it's binary on/off, never a function of another value.

The Option 2 roadmap (3 weeks of visual life — motor rotation, direct touch, audio, alarm blink, polish) stays inside this scope. The Option 3 roadmap (full physiology engine + 3 clinical scenarios) is a 28-week project that has not been committed to — it's a contingent v2.0 after Option 2 ships and customer feedback comes in.

## Working style (Pranjal's preferences)

- **Short, precise answers.** No fluff, no excessive preamble. One step at a time.
- **Wait for confirmation before proceeding** on ambiguity; commit to reasonable defaults otherwise.
- **VR testing is batched** — Pranjal will test multiple changes at once, not after every micro-change.
- **JSON is source of truth** for screen specs. Never nudge values in Unity Inspector; fix in JSON and rebuild via `Assets/Editor/ScreenBuilder.cs`. Unity-only nudging is reserved for final polish.
- **Devlog per day** at `devlog/day-NN.md`, fixed template: Goal / What I did / What broke and how I fixed it / Decisions / Open questions-next / Time spent.
- **Prose over bullets** in recommendations. Lists are fine for actual lists (steps, checks, parts to delete). Don't bullet-format an analysis.
- **Pranjal will correct mid-session** if a detail is wrong. Be responsive to corrections rather than defensive.

## Environment (DO NOT CHANGE without good reason)

These versions were chosen after hitting bleeding-edge bugs. Do not "upgrade to latest."

- Unity 2022.3.62f3 (standard LTS — NOT Extended LTS, NOT Unity 6.x)
- Meta XR Core SDK v74.0.0 (NOT 200+ series — namespace collision bug with Gradle 9.1)
- Meta XR Interaction SDK v74.0.0 (com.meta.xr.sdk.interaction.ovr — the OVR variant)
  - MUST version-match Core SDK. When installing, pick 74.0.0 explicitly via the
    version dropdown — do NOT accept the latest (200+) or it breaks the build.
- OpenXR + Oculus Touch Controller Profile + Meta Quest Support enabled
- URP, Single Pass Instanced rendering
- Target: Meta Quest 3 (and 3S), Android, ARM64, IL2CPP
- Package name: com.floaid.floexvr
- IDE: VS Code. Version control: Git + Git LFS. Repo: github.com/55Pranjal/Floex-VR-Trainer
- OS: Windows 11. Project path: C:\Floaid\Floex HLM VR (local, NOT OneDrive)

Asset pipeline tools: FreeCAD 1.1.1, Blender 4.5 LTS. Quest device tools: Meta Quest Developer Hub (logcat/device management). Screen recording: OBS Studio + Iriun Webcam (phone-as-webcam over USB, adopted after integrated webcam hardware issues on the Lenovo ThinkBook 14s Yoga G2).

## Architecture overview

**Per-canvas pattern.** Each pump head canvas is a self-contained interactive unit owning its own state, navigator, and interaction routing. Five canvases total: PumpHead_01/02/03_Canvas (singles), PumpHead_04_Canvas (double pump), plus the main pole canvas for the broader HLM display.

**Decoupled state vs display.** `PumpHeadState.cs` (and `DoublePumpHeadState.cs` for slot 4) is a pure data container — no Unity events, no display logic. Screen controller scripts read state one-way and update their UI; button OnClick handlers write to state via methods. This keeps physiology-style coupling out of Product A by construction — there's no place for it to live.

**Screens are JSON → Unity, not Unity-authored.** JSON files in `Assets/ScreenSpecs/` define screens (canvas dimensions, groups of elements with positions/sizes/text/colors/sprites). `Assets/Editor/ScreenBuilder.cs` reads JSON and generates Unity objects. The JSON uses positive Y values; the generator applies the Y-flip to match Unity's coordinate system. To change a screen layout, edit JSON, validate with the Python one-liner, rebuild.

**Navigators own button wiring.** `PumpHeadNavigator.cs` (single pump) and `DoublePumpHeadNavigator.cs` (double pump) handle screen lifecycle (one screen active at a time on each canvas) and button-to-method wiring. Each canvas has its own navigator instance. The navigator's `Awake` blanket-disables raycast targets on the home screen, then re-enables only the buttons it knows about via `HookButton`. Per-screen controllers (Screen2_1Controller, Screen3Controller, etc.) handle their own screen's specific buttons via the same pattern, with the navigator re-wiring the nav strip in each screen's OnEnable.

**Click pipeline (the central interaction tangle).** Meta XR v74 has a known quirk: `PointableCanvasModule` only routes pointer events to one of multiple coplanar canvases at a time, which broke clicks across our 5-canvas setup. Solution: `CanvasClickBypass.cs` runs in Update, polls `RayInteractor.State == Select`, intersects the ray with the canvas plane manually, and dispatches synthetic PointerClick events directly via `GraphicRaycaster` + `ExecuteEvents`. Per-canvas `PointableCanvas` is disabled by default to prevent the dual-pipeline click-hold bug discovered on Day 19 (PCM and Bypass both firing on the same click, with PCM toggling state back on release).

As of Day 22, `CanvasClickBypass` has a `disablePointableCanvas` flag (defaults true) that's unchecked on canvases also using direct touch. Reason: Poke events flow through `PointableCanvas` → `PointableCanvasModule` and need it active. Ray events bypass that path entirely via this script, so the two pipelines coexist without conflict.

**Direct touch (Poke) setup per canvas:** child GameObject `ISDK_PokeInteraction` holds `PokeInteractable` + `PlaneSurface` (Facing Backward) + `BoundsClipper` (sized to canvas dimensions, e.g. 800x480x0.01) + `ClippedPlaneSurface` (combines Plane + BoundsClipper into something implementing `ISurfacePatch`). The Poke Interactable's `Pointable Element` points at the parent canvas (resolves to its `PointableCanvas` component); its `Surface Patch` points at itself (resolves to the local `ClippedPlaneSurface`). Sibling `ISDK_RayInteraction` GameObject holds the parallel Ray setup.

**Motor rotation.** `PumpHeadRotor.cs` on single pump head GameObjects, `DoublePumpHeadRotor.cs` on the double pump head. Both spin a child `Rotor_Assembly` Transform at fixed 600°/s (~100 RPM visual) when `state.running` is true (single) or `state.pumpA_Running` / `state.pumpB_Running` (double). Direction comes from `state.directionForward` (bool, single) or `state.pumpA_Direction` / `state.pumpB_Direction` (string "CW"/"CCW", double). Visual-only — speed is fixed, NOT coupled to displayed RPM. Holding that line is the #1 scope discipline issue.

## Hard-won gotchas

### Unity / build

- Ctrl+S after changing Renderer Features / Project Settings — Unity doesn't auto-save these.
- Remove SSAO from ALL UniversalRenderer assets (Mobile_Renderer, PC_Renderer) for Quest perf.
- Developer mode only works on the Quest OWNER profile, not secondary profiles.
- Use com.meta.xr.sdk.core only — NOT com.meta.xr.sdk.all (pulls conflicting modules).
- Project lives at C:\Floaid\ — NOT under OneDrive (sync corrupts Library folder).
- ADB at C:\Users\User\Downloads\platform-tools... and in PATH.
- Ignore Unity's nag to "Update to 201.0.0" on Meta SDKs — always decline (the namespace bug).

### Git on Windows

- `core.ignorecase false` is required for filename casing renames to be picked up by git.
- `git checkout main && git pull` (no args) at session start to make sure local main is current.
- TextMeshPro fallback fonts (`LiberationSans SDF - Fallback.asset`) sometimes show up as modified after a session even though you didn't touch them — Unity auto-rebuilds dynamic glyph atlases. Drop these from commits via `git restore` unless you actually meant to change them.
- Unity sometimes restamps `.fbx.meta` files with GUID/timestamp churn even when you didn't change the FBX. Verify with `git diff` before committing — if it's substantive (e.g. an actual import setting change like Scale Factor 1 → 0.001), keep it; if it's just metadata noise, drop it.

### Quest Link / Play-mode testing

- Quest Link streams the LIVE editor scene to the headset on Play — no APK build needed.
  Do NOT open the old standalone app on the headset; just be at the Link home and press Play.
- For Link Play-mode to actually stream to the headset, OpenXR must be enabled on the
  WINDOWS/desktop tab of XR Plug-in Management — NOT just the Android tab. Play mode uses
  the PC platform settings, not Android.
- The Oculus Touch Controller Profile must be in "Enabled Interaction Profiles" on the
  WINDOWS tab too. If that list is empty, controllers won't work in Play mode even in VR.
- Symptom of the above: you stay in the flat 2D editor Game view and can't look around.

### Meta XR Interaction SDK v74

- Multiple coplanar world-space canvases break PCM event routing — solved via
  `CanvasClickBypass.cs` (see Architecture above). Don't try to fix at the PCM level.
- For Poke + Ray coexistence on the same canvas, `PokeInteractable.SurfacePatch` requires
  `ISurfacePatch`, not `ISurface`. A bare `PlaneSurface` won't drag into the field — you
  need `ClippedPlaneSurface` (which wraps `PlaneSurface` + `BoundsClipper`).
- `HandPokeInteractor` defaults to index-finger-only. This is the right default for the
  trainer — multi-finger causes accidental presses from curled-back fingers near the palm.
- The Inspector throws `Stack empty` / `Getting control 1's position` errors during Poke
  Interactable setup when the Surface Patch field is empty or malformed. Editor rendering
  glitch only — clears once the field is properly wired. Ignore.

### Blender

- Ctrl+J (Join) is context-sensitive to mouse hover. ONLY works with cursor over the 3D
  viewport. Pressing it with cursor over the Outliner does nothing silently. Burned a
  whole minute the first time.
- Alt+Z (X-Ray toggle) conflicts with NVIDIA GeForce Experience overlay. Use the X-Ray
  toggle ICON in the top-right of the viewport instead. Or disable the NVIDIA shortcut.
- Python scripts pasted into the interactive Python console choke on for-loop indentation
  (the console interprets line-by-line). Wrap the whole script in an `exec("...")` call
  with `\n` separators to run as one statement.
- Decimation crushes fine-detail parts (display bezels, vent grilles, knobs) at aggressive
  ratios. Use a protected-list pattern: protect names like `Rotor_Assembly`, `DISP`,
  `SCA`, `CG`, `DST`, `Power button`, `KNOB`, `Display`, `5Inch` at 0.75 ratio; everything
  else at 0.55. First decimation always looks too aggressive — Ctrl+Z and rerun is fine.

### CAD pipeline (validated, refined across Days 17-22)

- FreeCAD: open the .step (uncheck "Ignore instance names" to keep part names).
- FreeCAD: **select ONLY leaf-level children, never parent groups.** Selecting a parent
  group causes FreeCAD to flatten the sub-tree into a single fused mesh on export —
  loses all sub-part separability. This bit us on the Day 19 double pump head import.
- FreeCAD: visibility-toggle method to identify externally-visible parts. Hide the parent
  group (Space), then reveal sub-parts one at a time, keep visible the ones that appear
  on the exterior, skip internal fasteners (BOLT, NUT, ISO 1207 screws, internal ISO 4762
  screws, PINs, Spring Pins, SLEEVES, bearings).
- FreeCAD: switch to Mesh workbench, Meshes > "Create mesh from shape", Surface deviation
  0.5mm (NOT the 0.10 CAD default — way too dense for VR).
- Blender: import OBJ with -Z Forward / Y Up.
- Blender: for rotors specifically, select leaf-level rotor parts (rollers, roller pins,
  thumb wheel, rotor screws, mirror ring holder, guide rollers cap) and Join (Ctrl+J)
  into a single mesh named `Rotor_Assembly` (or `Rotor_Assembly_A` / `Rotor_Assembly_B`
  for double pump). Set origin via Object > Set Origin > **Origin to Center of Mass
  (Volume)** — lands cleanly on rotation axis for symmetric rotors.
- Blender: decimate with the protected-list pattern above.
- Blender: Shade Smooth before export.
- Blender: export FBX with Selected Objects, Mesh only, -Z Forward / Y Up, Apply Scalings
  = FBX All, Apply Unit, Use Space Transform, Smoothing = Face.
- Unity: import at **Scale Factor 0.001** (Blender mm → Unity m). NOT 1.
- Unity: transforms from existing fused-mesh Floex_Trainer.fbx instances (scale 100,
  rotation X=-89.98) do NOT transfer to new 0.001-import models. Position manually.

### Unity execution order

- Button wiring goes in `Start`, not `OnEnable`. Reason: `PumpHeadNavigator.Awake` does
  a blanket disable of raycast on the home screen, and if a screen controller wires
  buttons in `OnEnable` instead of `Start`, that blanket disable will undo the wiring.

## Current state (as of Day 22)

**Product A is functionally complete plus partway through the Option 2 visual-life pass.**

Built and working:
- All 5 canvases (3 singles + double pump + main pole) fully interactive
- All screen navigation (Screen1/2_1/3/4/5 + Pump Select / Tube Size / Direction / Flow Ratio pickers)
- All four pump heads with independent state, including the double pump head's two pump-state pair (pump A + pump B + shared flow ratio)
- Click-hold bug resolved (Day 19 PointableCanvas-disable fix)
- All four pump heads with spinning rotors (Day 22 — `PumpHeadRotor` + `DoublePumpHeadRotor`)
- Single pump head reimported with rotor as separate sub-mesh (Day 21)
- Double pump head reimported with both rotors as separate sub-meshes + lid removed (Day 22 — Shiv-approved scope expansion)
- Direct touch (Poke) working on slot 1 canvas (Day 22)

In progress:
- Direct touch propagation to slots 2, 3, 4 (slot 2 attempted Day 22, not working yet — Day 23 debugging task)

## Next / on the horizon

**Immediate (Day 23+):**
- Debug slot 2 poke setup — likely a reference-wiring issue from the duplicated GameObject. Once slot 2 works, slots 3/4 are one Ctrl+D each.
- Clean up the "Removed" components left on each canvas (Pointable Canvas / Ray Interactable / Plane Surface marked Removed) — accumulated cruft worth removing now that the architecture is settled.

**Week 3 of Option 2 (~1 week of work):**
- Bypass toggle visible reaction
- Alarm light blinking on state changes
- Basic spatial audio via Meta XR Audio SDK (pump sound, alarm beep)
- Performance profiling on Quest 3 (hit stable 72fps with all animations running)
- Polish backlog: `tube_circle_3.png` rendering white instead of green, BSA OK/EXIT unwired, two `medical_instrument_tray` instances, `Floaid.lnk` Windows shortcut still in git, 16KB-aligned warning on `libUnityOpenXR.so`

**After Option 2 ships:**
- Customer demo + feedback
- Decision on Option 3 (full physiology + scenarios, ~28 weeks) vs ship Option 2 as v1.0

**Open project-level questions:**
- VR scale verification against real HLM dimensions (currently fine at 0.0002 per scene, never formally measured against actual Floex)
- Slot 4 model height looks slightly taller than singles — verify with Shiv whether this matches the real machine
- Screen5 Service Need icon clarification with team
- Real-RPM-vs-visual-rotation-speed coupling: hold the line, binary on/off only

## Team & decision-making

- **Pranjal Agarwal** — solo intern developer building everything
- **Shiv** — CEO of Floaid MedTech, project stakeholder and scope-decision authority
- **Hashir** — firmware/CAD owner. Floex 3.0 firmware specs and CAD files come from him.
- **KRB** — clinical reviewer (for any clinical accuracy questions, esp. relevant if Option 3 is greenlit)
- **Sundaraganesan** — PMS/telemetry contact (relevant when telemetry pipeline starts)
- **Jai Raman** — advisor

Scope decisions (Product A vs A.5 vs B/3) require Shiv's explicit approval. Don't drift into expanded scope on the fly even if the change feels small.

## Key file/folder locations

- Repo root: `C:\Floaid\Floex HLM VR\`
- Scripts: `Assets/Scripts/`
- Screen JSON specs: `Assets/ScreenSpecs/`
- ScreenBuilder: `Assets/Editor/ScreenBuilder.cs`
- Models: `Assets/Models/` (SinglePumpHead.fbx, DoublePumpHead.fbx, Floex_Trainer.fbx, plus Hospital subfolder)
- Materials: `Assets/Materials/Floex/Mat_Floex_Steel.mat`
- Main scene: `Assets/Scenes/OR_Environment.unity`
- Devlogs: `devlog/day-NN.md`
- CAD intermediates: `C:\Floaid\Floex HLM VR\CAD\` (OBJs from FreeCAD, Blender .blend files)

## Useful references

- Floaid_VR_36Week_Roadmap.docx — original pre-project roadmap (stale, but useful for context)
- Floex_VR_Option2_Roadmap.docx — current 3-week milestone roadmap
- Floex_VR_Option3_Roadmap.docx — contingent 28-week full simulator roadmap
- `docs/step_to_unity_asset_pipeline.svg` — visual reference for the asset pipeline