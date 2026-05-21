# Floex HLM VR — Project Context

VR training simulator for perfusionists on the Floex 3.0 Heart-Lung Machine.
Product A: a familiarisation trainer (narrow MVP) — NOT a full physical-fidelity sim.
Built solo with Claude Code as pair-programmer; Floaid team are clinical/engineering domain experts.
Target hardware: Meta Quest 3.

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

## Hard-won gotchas

### Unity / build

- Ctrl+S after changing Renderer Features / Project Settings — Unity doesn't auto-save these
- Remove SSAO from ALL UniversalRenderer assets (Mobile_Renderer, PC_Renderer) for Quest perf
- Developer mode only works on the Quest OWNER profile, not secondary profiles
- Use com.meta.xr.sdk.core only — NOT com.meta.xr.sdk.all (pulls conflicting modules)
- Project lives at C:\Floaid\ — NOT under OneDrive (sync corrupts Library folder)
- ADB at C:\Users\User\Downloads\platform-tools... and in PATH
- Ignore Unity's nag to "Update to 201.0.0" on Meta SDKs — always decline (the namespace bug)

### Quest Link / Play-mode testing (learned the hard way)

- Quest Link streams the LIVE editor scene to the headset on Play — no APK build needed.
  Do NOT open the old standalone app on the headset; just be at the Link home and press Play.
- For Link Play-mode to actually stream to the headset, OpenXR must be enabled on the
  WINDOWS/desktop tab of XR Plug-in Management — NOT just the Android tab. Play mode uses
  the PC platform settings, not Android.
- The Oculus Touch Controller Profile must be in "Enabled Interaction Profiles" on the
  WINDOWS tab too (Project Settings > XR Plug-in Management > OpenXR > desktop tab).
  If that list is empty, controllers won't work in Play mode even if you're in VR.
- Symptom of the above: you stay in the flat 2D editor Game view and can't look around.

### Meta Interaction SDK / grab

- Grab interaction is added via right-click in Hierarchy > Interaction SDK > Add Grab Interaction
  (with the target object selected). The Grab Wizard's "Fix All" adds the needed Rigidbody.
- "Add Grab Interaction" = DIRECT-TOUCH grab. The controller/hand must physically touch the
  object — a laser ray will NOT grab it. (Ray Grab / Distance Grab are separate options.)
- Direct grab needs the object within arm's reach. A cube floating 2m away can't be touched.
- The grab wizard sets the Rigidbody to Is Kinematic + Use Gravity OFF, so grabbables hang
  in place rather than falling. That's intended.

## 3D asset pipeline: STEP → Unity-ready mesh (validated dry run)

The route from a CAD STEP file to a game-ready Quest mesh. Tools: FreeCAD 1.1.1 + Blender 4.5 LTS.

1. FreeCAD: open the .step (uncheck "Ignore instance names" to keep part names).
2. FreeCAD: switch to Mesh workbench, select parts, Meshes > "Create mesh from shape".
   Use Standard tessellation, Surface deviation ~0.5mm (NOT the 0.10 CAD default — too dense
   for VR), Angular deviation 30°, tick "Apply face colors to mesh".
3. FreeCAD: select the green (Meshed) objects only, File > Export > Wavefront OBJ.
4. Blender: delete default cube, File > Import > Wavefront (.obj).
5. Blender: delete the internal parts the operator never sees (keep outer shell + console).
6. Blender: per object, add Decimate modifier (Collapse), lower Ratio while watching the
   Face Count and the silhouette. Find the lowest ratio before the shape visibly breaks.
   On real Floex parts (tens of thousands of tris) this can go to 0.1 or lower.
   Then modifier dropdown > Apply.
7. Blender: select the mesh objects, File > Export > FBX, tick "Limit to > Selected Objects".
   (Blender Z-up vs Unity Y-up is handled by the FBX exporter defaults; if a part comes in
   rotated 90° in Unity, fix with a single rotation there — not a mistake.)

## Current state

- Day 1: Hello-world cube renders on Quest 3. Build pipeline confirmed.
- Git + Git LFS initialised, pushed to GitHub.
- Day 2: Learned Unity interaction basics. Installed Interaction SDK v74 (version-matched).
  Added OVR Interaction Rig + made a cube grabbable; confirmed working in VR over Quest Link.
- Asset pipeline (STEP → FBX) dry-run completed on a mock file — ready for the real CAD.

## Next / on the horizon

- #1 critical-path dependency: get the Floex 3.0 CAD from the team (full SolidWorks assembly,
  ~100-200 parts, ~100MB STEP). When it arrives, it JUMPS to top priority — run the asset
  pipeline above. Flag its arrival immediately in whatever chat is active.
- Then: build the OR environment, then (Phase 2) the physiology engine + interactive console.
- UI spec for the console already received (Pump Head UI: 7 screens — pump main, tube select,
  master-slave, direction, fine calibration, pulse mode, diagnostics). Phase 2 work.

## #1 project risk

Scope drift toward a full CALIFIA-style hardware sim. Keep it NARROW. Product A is a
familiarisation trainer, not a physical-fidelity simulator.
