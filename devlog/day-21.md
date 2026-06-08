# Day 21 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-screens`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 08 Jun 2026

---

## Goal

Reimport the single pump head with internal sub-parts preserved as separate Unity meshes — specifically `Rotor_Assembly` as an addressable top-level mesh — so motor rotation animation becomes possible. Replace the three fused-mesh slots (1, 2, 3) in scene with the new model. Foundation work shared by Option 2 and Option 3 roadmaps.

## What I did

Ran the full FreeCAD → Blender → Unity pipeline a second time, this time with sub-part preservation as the explicit goal.

**FreeCAD:**
- Opened full HLM assembly STEP. Selected only externally-visible leaf parts via visibility-toggle method (hide group, reveal one at a time). Left-out parents to avoid the fused-mesh problem from the double pump head session.
- Switched to Mesh workbench, Create mesh from shape at 0.5mm surface deviation.
- Exported `SinglePumpHead.obj` at 14MB / 166k triangles. Way cleaner starting point than the double pump head (920k).

**Blender:**
- Imported with -Z Forward / Y Up. 48 objects initially.
- Identified rotor cluster: `Roller pin` through `Roller pin007` (8 parts), `Roller_SHP_TWM_PA`, `Roller_SHP_TWM_PA001`, `MirrorRoller_Ring holder`, `Guide rollers cap_SHP_TWM`, `Thumb Wheel_SHP_TWM_P`. 12 parts total.
- Deleted internals: all ISO 1207 screws, 2 internal ISO 4762 socket-head screws, 6001-2rsh bearings. Kept external ISO 4762 screws and PINs where their removal would expose visible holes.
- Verified no hidden parts via viewport X-ray toggle (Alt+Z conflicts with NVIDIA overlay shortcut, so used the X-ray icon).
- Selected all 12 rotor parts, joined with Ctrl+J, renamed to `Rotor_Assembly`.
- Set origin via Object → Set Origin → Origin to Center of Mass (Volume). Lands cleanly on the rotation axis because rotor is symmetric.
- Decimated via Python script in Scripting tab with a protected names list (`Rotor_Assembly`, `DISP`, `SCA`, `CG`, `DST`, `Power button`, `KNOB`) at 0.75 ratio, everything else at 0.55. Landed at 100k triangles.
- Applied Shade Smooth to all meshes.
- Saved as `Single_pump_head.blend`.
- Exported `SinglePumpHead.fbx` (3.3MB) with the same settings as the double pump head: Selected Objects only, Mesh-only, -Z Forward / Y Up, Apply Scalings = FBX All, Apply Unit, Use Space Transform, Smoothing = Face.

**Unity:**
- Auto-imported into `Assets/Models/SinglePumpHead.fbx` at Scale Factor 0.001. 15 sub-meshes visible in the FBX hierarchy, `Rotor_Assembly` among them.
- Dragged into scene as child of `Floex_Trainer`. Positioned and scaled manually rather than trying to copy transforms from the existing fused-mesh instances — those were `Floex_Trainer.fbx` sub-mesh instances with scale 100 and rotation X=-89.98, not transferable to a 0.001-import.
- Duplicated twice for slots 2 and 3 (`SinglePumpHead_01`, `_02`, `_03`).
- Applied `Mat_Floex_Steel` to all sub-meshes across all three instances.
- VR-tested on Quest 3 — all four canvases still interactive, navigation works, no broken click behavior, no visual regressions.
- Deleted `Pump_assembly_FLOAID`, `_FLOAID001`, `_FLOAID002` after VR verify.

**Slot 4 (double pump head) cleanup:**
- Removed the Unity primitive placeholders (Cube knob, Cylinder button) added on Day 19.
- Duplicated `KNOB_ENCODER010` and `Power button & circuit breaker` from one of the new single pump heads, reparented under `Double_pump_head`, positioned at slot 4's knob/button holes.
- All four pump heads now have consistent CAD-quality knobs and buttons. Visual asymmetry from Day 19 closed.

## What broke and how I fixed it

**Ctrl+J silently failed in Blender.** Selected all 12 rotor parts, pressed Ctrl+J, nothing happened. Cause: my cursor was hovering over the Outliner panel, and Blender shortcuts are context-sensitive to whichever editor the mouse is over. Ctrl+J in the Outliner does nothing. Moved cursor into the 3D viewport, pressed again, joined cleanly. Worth remembering as a general Blender gotcha.

**Alt+Z opens NVIDIA GeForce Experience overlay.** Blender's X-ray toggle shortcut is hijacked on my laptop. Worked around by using the X-ray toggle icon in the top-right of the viewport instead. Could disable the NVIDIA shortcut globally — leaving that for later.

**Python script pasted into wrong panel.** First decimation attempt failed with `IndentationError: unexpected indent` repeating for every line. Cause: I pasted into the interactive Python console at the bottom of the Scripting tab, which interprets lines one at a time and chokes on a for-loop's indented body. Fix: wrapped the entire script in a single `exec()` call with `\n` separators so the console runs it as one statement.

**First decimation pass crushed the screen.** Ran the protected/unprotected script with 0.7 / 0.4 ratios. Triangle count landed at 87k (good), but the display bezel and side vent grilles came out as jagged garbage — those areas have fine geometry that needs more triangles to look right. Undid via Ctrl+Z and re-ran with 0.75 / 0.55 ratios and an expanded protected list (added `DISP`, `SCA`, `CG`, `DST`, `Power button`, `KNOB` to the keep-detail list). Landed at 100k triangles with the screen intact.

**Transforms don't transfer between import scales.** Considered grabbing position/rotation/scale values from one of the existing slot 1 fused-mesh instances and applying to the new model. Wouldn't have worked: existing singles are `Floex_Trainer.fbx` sub-mesh instances with instance-level scale (100, 100, 100) and rotation X=-89.98, whereas the new model imports at FBX-level Scale Factor 0.001 with no rotation. Skipped that idea, positioned and scaled manually using the double pump head's transform values as a starting reference.

## Decisions

- **Rotor as Option A (leaf-level parts → Blender Join into one mesh).** Option B (selecting only the rotor's CAD parent → single fused mesh on import) would have worked too, but keeping leaves preserves optionality if rollers ever need to spin independently of the rotor body. Cost: 12 parts to track in Blender; benefit: future flexibility.
- **Center of Mass (Volume) for origin placement.** Symmetric rotor means volumetric center sits on the rotation axis. Confirmed visually in top + front views. No manual cursor placement needed.
- **Three duplicated GameObjects instead of a prefab.** Faster today, easier to manually nudge each into position. Re-evaluate if model changes become needed — model fixes would need to be applied three times instead of once.
- **Reused CAD parts for slot 4 knob/button rather than re-importing the double pump head.** Faster, and slot 4's button/knob area now visually matches the singles.

## Open questions / next

- **Prefab the single pump head?** Skip for now. Reconsider only if model fixes become needed.
- **Motor rotation work ready to start.** Geometry foundation is in place; waiting on Shiv's confirmation of Option 2 scope (binary on/off rotation tied to START/STOP) before writing the rotation script.
- **Slot 4 model height vs singles** — still slightly taller than the singles. Carried over from Day 19. Need to verify with Shiv whether this matches the real machine.
- **VR scale verification** against real HLM dimensions — still informally "fine at 0.0002," never formally measured.
- **Polish backlog from Day 19** still standing: `tube_circle_3.png` rendering white instead of green, BSA OK/EXIT unwired, two `medical_instrument_tray` instances in scene, `Floaid.lnk` Windows shortcut still in git, 16KB-aligned warning on `libUnityOpenXR.so`.

## Time spent

~5 hours (FreeCAD ~45min, Blender ~1.5h including the decimation redo, Unity placement ~1.5h, slot 4 knob/button cleanup ~30min, VR test + cleanup + commit prep ~45min).

## Files modified today

- `Assets/Models/SinglePumpHead.fbx` — new
- `Assets/Models/SinglePumpHead.fbx.meta` — new
- `Assets/Scenes/OR_Environment.unity` — three new SinglePumpHead instances added, three Pump_assembly_FLOAID* deleted, two Unity primitive placeholders under Double_pump_head deleted, KNOB_ENCODER010 and Power button & circuit breaker reparented under Double_pump_head
- `devlog/day-21.md` — new