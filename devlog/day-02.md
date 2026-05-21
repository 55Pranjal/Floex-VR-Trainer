# Dev Log — Day 2 (2026-05-20)

## Goal
1. Unblock the critical path: confirm format and sizing for the Floex CAD model (Block 1).
2. Dry-run the CAD-to-mesh asset pipeline on a mock file so the real file isn't the first attempt.
3. Learn Unity interaction basics — make an object grabbable (Block 2).

## What I actually did
- **Block 1 (CAD):** Confirmed with the team the model comes as a SolidWorks STEP, full
  assembly, ~100–200 parts, ~100MB. That's a medium-weight file — workable. Asked them to
  suppress internal parts/hardware if easy (not a blocker).
- **Asset pipeline dry run:** Installed FreeCAD 1.1.1 and Blender 4.5 LTS. Built a mock 5-part
  STEP assembly and ran the full pipeline end to end:
  STEP → FreeCAD (import, mesh-from-shape) → OBJ → Blender (delete internals, decimate) → FBX.
  See `CLAUDE.md` for the exact settings. The whole route now works on a known file.
- **Block 2 (grab):** Installed the Meta XR Interaction SDK (version-matched to v74), added an
  OVR Interaction Rig, ran the Grab Wizard on the cube, and got grab working in VR over Quest
  Link — reach, grip, hold, release, move.

## What broke and how I fixed it

- **Symptom:** Interaction SDK install risked breaking the build.
  - **Cause:** The Asset Store one-click install grabs the *latest* (200-series) version, which
    mismatches the pinned Core v74 and reintroduces the namespace bug.
  - **Fix:** Claim the asset on the Unity account (grants entitlement, avoids "permission denied"
    errors), then explicitly select **74.0.0** in the version dropdown before installing.
    Confirmed both Core and Interaction read 74.0.0 after.

- **Symptom:** Cube wouldn't grab in VR — tried every controller button.
  - **Cause 1:** Set up *direct-touch* Hand Grab, but tried to grab from a distance with the
    controller's laser ray. Direct grab ignores the ray; the hand must touch the object.
  - **Cause 2:** The cube was floating ~2m away — out of arm's reach for a direct grab anyway.

- **Symptom:** Play mode showed a flat 2D view; couldn't look around; Link wasn't streaming.
  - **Cause:** OpenXR was enabled only on the *Android* tab. Play mode uses the *Windows/desktop*
    platform settings, where OpenXR was off and the Enabled Interaction Profiles list was empty.
  - **Fix:** Project Settings → XR Plug-in Management → **Windows tab** → enable OpenXR, and add
    the **Oculus Touch Controller Profile** to Enabled Interaction Profiles. Then Play streamed
    to the headset and grab worked.

- **Symptom:** Red Console error about a `Legacy/OVRCameraRigInteraction.prefab` nested-prefab.
  - **Cause/judgement:** Complaint about an unused *legacy* prefab inside the SDK package itself,
    not the rig actually in use (which auto-wired fine). Treated as cosmetic; confirmed harmless
    once grab worked.

## Decisions made
- Chose **direct Grab Interaction** (not ray/distance grab) — it's the mechanic every Floex dial
  and knob will use (operator touches a control, doesn't laser it).
- Unchecked **Smooth Locomotion** on the rig — the operator stands at the machine; no roaming
  needed, and it's the main cause of VR motion sickness.
- Deferred the **OpenXR Hand Skeleton** upgrade ("Remind Me Later") — hands-vs-controllers isn't
  settled for the product yet, and the migration is "not officially supported" on existing projects.
- Used a throwaway branch (`practice/grab-interaction`) for the experimental grab work, then
  merged the working setup back to main as the new baseline.

## Open questions / next
- **Hands or controllers** for the final trainer? Decides the hand-skeleton question and the grab
  interactor setup. Worth confirming with the team.
- **#1 next move:** the Floex CAD. When it lands, it jumps to top priority — run the pipeline.
- After that: build the OR environment, then Phase 2 (physiology engine + interactive console).
  Console UI spec already in hand (7 screens).

## Time spent
Full session — toolchain/CAD confirmation, the pipeline dry run, and the grab work end to end.
The version-matched SDK install and the Link/OpenXR debugging took the most time.
