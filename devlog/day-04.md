# Dev Log — Day 4 (2026-05-22)

## Goal
1. Continue the OR environment from Day 3 (had only floor + scene at end of Day 3).
2. Build a complete bare-OR interior pass: walls, materials, Floex placeholder.
3. Get a working VR camera rig into the OR scene so the room is standable in the headset.
4. (Later today) CAD session with the team member — pick the parts to export (option 3).

## What I actually did
- **Synced first:** sat down at the Floaid laptop, pushed the Day-3 devlog to `main`, pulled it
  into `feature/or-environment` to keep the branch current (two-machine ritual). No conflicts.
- **OR interior built from primitives (all on `feature/or-environment`):**
  - 4 walls from scaled cubes around the 10×10m floor. Back/Front thin in Z (`scale 10,3,0.2`),
    Left/Right thin in X (`scale 0.2,3,10`); all centred at `Y=1.5` so a 3m wall sits on the floor.
  - Two-tone materials in a new `Assets/Materials` folder: `Mat_Floor` (cool blue-grey vinyl) and
    `Mat_Wall` (warm off-white). Multi-selected all 4 walls and applied the wall material in one drag.
  - `Floex_Placeholder` — a stand-in cube `scale 0.7,1.5,0.7` at `pos 0,0.75,0` (≈70cm × 70cm × 1.5m
    tall cabinet, centre of room). Drop-in target for the real mesh after the CAD session.
  - Commit `6ce4763`: "feat: bare OR room — floor, 4 walls, two-tone materials, Floex placeholder".
- **VR camera rig added (the part that fought back — see below):** ended up creating my OWN rig
  prefab from the proven Day-2 `[BuildingBlock] Camera Rig` (saved to `Assets/Prefabs`), dropped it
  into the OR scene, positioned at `pos 0,0,-2` `rot 0,0,0` (2m back from the Floex, facing +Z toward
  it). Tested over Quest Link: standing in the room at correct height, correct facing, placeholder
  reads as sensible machine scale, all four walls visible. Committed + pushed.

## What broke and how I fixed it

- **Symptom:** Tried to add the rig by dragging in `Assets/InteractionSDK/ComprehensiveInteraction`
  (a prefab that arrived via a Git merge). On Play: 999+ red errors, black in the headset, laptop
  showed "No cameras rendering."
  - **Cause:** Read the errors bottom-up — root cause was a `StackOverflowException` in
    `Oculus.Interaction.Input.HmdRef.add_WhenUpdated` (infinite self-reference in the HMD/camera
    tracking), plus a cascade of `NullReferenceException`s in `DataModifier`. The prefab was an
    **SDK sample**, not my actual Day-2 rig — it was never wired for this project (Day-2 grab used the
    Building Blocks *wizard*, which builds rig objects directly in the scene, not this prefab). Its
    internal references pointed at nothing → infinite loop → no camera ever initialised → black.
  - **Fix:** Stopped Play. Deleted `ComprehensiveInteraction` from the OR scene. Opened SampleScene,
    confirmed the *real* working rig was `[BuildingBlock] Camera Rig` (with camera + hands/interactors
    nested inside). Dragged that to the Project window to make my **own** prefab from the proven rig,
    then dropped that into the OR scene. Worked first try.
  - **Lesson:** Read error stacks bottom-up — the root cause is at the bottom, the 999 lines above are
    just it repeating. And don't trust a prefab just because it has a plausible name and is in the repo;
    verify it's the thing that actually worked.

- **Symptom:** First successful rig test — spawned *inside* the Floex placeholder, couldn't see it.
  - **Cause:** Dragged-in prefab landed at origin `0,0,0`; the placeholder is also at `0,0,0`. Spawned
    dead-centre, inside the box.
  - **Fix:** Set rig root to `pos 0,0,-2` (2m back, on -Z), `rot 0` (facing +Z toward the Floex).
    Confirms rendering/tracking were fine all along — it was purely a spawn position.

## Decisions made
- **Build my own rig prefab** from the verified Day-2 Building Block rig (in `Assets/Prefabs`) rather
  than rely on the SDK sample. Reusable across every future scene; it's the fix for the broken-prefab
  class of problem. Accepted the "break Building Block link" warning — the functional rig is what matters.
- **OR interior = bare room, primitives only, no ceiling yet.** Ceiling deferred until interior + Floex
  are in (player rarely looks up; ceiling just darkens the workspace while building). Detail budget goes
  to the console, not the walls.
- **Floex stays a placeholder** until the CAD session — `0.7×1.5×0.7m` is a sane guess; real dims come
  from the session. Room is composed around it so the real mesh is a drop-in replacement.

## What I actually did (afternoon additions)
- **Morning task list (5 CAD-independent items) — knocked out before the session:**
  1. **OR set as build scene:** `OR_Environment` at index 0 in Scenes-In-Build; `SampleScene` unticked.
     Committed `EditorBuildSettings.asset` ("chore: set OR_Environment as index-0 build scene").
  2. **Standalone build VERIFIED on Quest 3** — the key de-risk. Built+ran an APK on-device (no Link).
     Build succeeded in 184s (first IL2CPP/ARM64 build — slow is normal). APK saved to `C:\Floaid\Builds`
     (outside the repo, so never committed). Kept Development Build ticked for better on-device logging.
  3. **Clinical lighting:** directional light to `rot 50,-30,0`, intensity `0.8`, **No Shadows**; ambient
     set to **Source = Color** with a bright neutral fill. Room now reads bright/even/clinical. Committed.
  4. **Tidy (ComprehensiveInteraction sample) — DEFERRED on purpose** (see below).
  5. **CAD familiarisation + prep sheet** built (`CAD_session_prep_sheet.md`).
- **CAD session:** had a pre-session chat — full-assembly STEP export keeps failing (500+ of 1000+
  components dropping, "path missing", SolidWorks crashing). He'll retry tomorrow morning on a stronger
  machine. **Confirmed it's a RAM/memory issue, not broken references** — so a stronger machine genuinely
  has a good chance of working (horsepower helps when the export runs out of memory mid-flatten).
  Built a CAD prep sheet covering both paths (full export → curate, OR per-part export from checklist).

## What broke and how I fixed it (afternoon additions)

- **Symptom:** Standalone build launched on Quest but showed my real room (passthrough), not the OR scene.
  - **Cause:** First-launch timing glitch — app started before the VR compositor grabbed the display
    (common with Build-And-Run right after a Link session). NOT a scene/render bug.
  - **Fix:** Quit the app on-device and relaunched → OR rendered correctly. **Future rule: if a build
    launches into passthrough/frozen, FIRST quit+relaunch on-device before assuming anything's broken.**

- **Symptom:** "Find References In Scene" on the ComprehensiveInteraction sample flooded the Hierarchy
  with dozens of objects (Activate, Aiming, Collider, Controller, CenterEyeTransform…).
  - **Cause:** That command applies a `ref:` *search filter* to the Hierarchy — it wasn't listing true
    references, it was matching internal child objects of my working rig. Misleading, not a problem.
  - **Fix/judgement:** Cleared the filter. Decided **NOT to delete the sample prefab today** — deleting
    something SDK-adjacent right before the critical CAD session is a bad risk/reward trade when the rig
    just got working. An unused prefab in a folder is harmless; the "trap" is defused by knowing not to
    drag it in. Deferred to a low-risk later time, with a rig re-test after.

## Decisions made (afternoon additions)
- **Verify standalone build early, on the simplest possible scene.** Catching the passthrough glitch now
  (on a bare room) beats hitting a build problem later when the Floex adds ten possible causes.
- **Defer the prefab cleanup** rather than risk the working rig pre-session (see above).
- **CAD plan holds regardless of tomorrow's retry:** even a successful full export gets curated down to
  ~15–25 visible/touchable parts. Never wanted the 1000-component internals.
- **Console controls split:** physical knobs/switches → CAD geometry (grabbable); on-screen controls →
  the 7-screen Unity UI (no CAD). This drives what to ask him to export.

## Open questions / next
- **Tomorrow AM:** he retries the full-assembly STEP export on a higher-RAM machine. If it works → curate
  from it using the prep-sheet checklist. If not → per-part export. **This is the critical-path event.**
- **When the STEP(s) land:** first REAL pipeline run (STEP → FreeCAD → OBJ → Blender decimate → FBX →
  Unity), then replace `Floex_Placeholder` with the real mesh (scale CAD mm ÷1000 → meters).
- **Get from the session:** overall machine dimensions (mm), which controls are physical vs on-screen,
  reservoir/oxygenator permanent-vs-disposable, reference photos of the console face, recording.
- **Hands vs controllers** for the final trainer — still unresolved (carried from Day 2/3).
- **Deferred low-priority:** remove the `ComprehensiveInteraction` sample prefab (test rig after).

## Time spent
Full day. Morning: OR interior build (quick) + the rig detour (the bulk — diagnosing the broken
SDK-sample prefab and rebuilding from the real Building Block rig). Midday: the 5-item task list
(build-scene, standalone-build verify, lighting; tidy deferred). Afternoon: CAD familiarisation + prep
sheet. No real STEP yet — that's tomorrow.