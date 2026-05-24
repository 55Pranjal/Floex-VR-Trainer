# Dev Log — Day 6 (2026-05-24)

## Goal

- Run the second half of the asset pipeline on real data for the first time: mesh the curated STEP, take it through Blender (decimate), export FBX.
- Get the real Floex into Unity and replace the `Floex_Placeholder` cube in the OR scene.
- End the day with the actual machine standing in the scene, ready for an on-device VR check.

## What I actually did

- Started day on `feature/cad-pipeline`, Floaid laptop, `git pull` clean.
- **FreeCAD meshing:** opened `Floex_Curated.step` (18 parts), switched to Mesh workbench. Test-meshed one pump first to dial in tolerance.
- **OBJ export:** meshed all 18 at Surface deviation 1.0mm (~2.5M facets total), saved `Floex_Meshed.FCStd` as insurance, exported `Floex_Curated.obj` (~229MB).
- **Blender import:** deleted default cube, imported OBJ. All 18 parts came in named. Machine invisible at first — CAD-scale problem (imported as ~1000s of Blender units).
- **Transform cleanup (the messy part):** machine imported with Rotation X=90 and a non-clean scale. Applied All Transforms to bake them; accidentally swept Camera + Light into the operation (garbage transforms). Deleted Camera + Light (not needed — Unity provides its own). Re-derived clean state: parts in true mm, Rotation 0, Scale 1.
- **Scale to meters:** parts were in mm reading as m. Selected all → S → 0.001 → applied scale. Verified `Base side Sheet_BA` = 0.703 x 0.453 x 0.158 m (realistic). Saved `Floex_Trainer.blend`.
- **Decimation:** tested Decimate modifier on one pump at ratio 0.1 → 530K to 53K faces, still recognizable. Copied modifier to all 18 (Ctrl+L → Copy Modifiers). Inspected each part in Local View. Bumped `Base side Sheet_BA` to 0.2 (0.1 was too aggressive on its visible exterior) → 2242 faces. Rest stayed at 0.1.
- Baked modifiers (Object → Apply → Visual Geometry to Mesh), applied Shade Auto Smooth, saved.
- **FBX export:** first export was 4KB (empty — selection was lost at export time). Re-selected all 18 (verified orange), re-exported → 8.07MB `Floex_Trainer.fbx`. Settings: Selected Objects, -Z Forward, Y Up, Animation off, Apply Modifiers on.
- **Unity import:** created `Assets/Models/` folder (from inside Unity), imported FBX. Came in upright, correct scale, clean transform, all 18 sub-meshes. Dragged into OR_Environment scene.
- **Placement:** placeholder was at (0, 0.75, 0). Floex pivot is at machine vertical-center, so seated it at Position (0, -0.6, 0) to drop base onto floor. Orientation correct (front faces player spawn). Deleted `Floex_Placeholder`.
- Play-mode check on laptop: scale and orientation look correct.

## What broke and how I fixed it

- **Symptom:** Machine invisible after Blender import.
  - **Cause:** CAD mm imported as ~1000s of Blender units — too big to frame normally.
  - **Fix:** Shift+C to reframe; then scale to meters (S → 0.001) and apply.

- **Symptom:** Applying transforms gave Camera/Light garbage rotations; select-all kept sweeping them in.
  - **Cause:** Select-all (A) grabs cameras and lights too.
  - **Fix:** Deleted Camera + Light entirely — not exported to Unity anyway. Select-all is now clean (18 meshes only).

- **Symptom:** Lost the machine twice to invisible state during scale attempts; undo over-rewound.
  - **Cause:** Scaling with only ONE object selected (select-all didn't register because mouse wasn't over the viewport), then per-axis edits distorted it, then undo went too far.
  - **Fix:** Discipline — after pressing A, visually confirm ALL objects are orange before any transform. Saved the .blend immediately once clean.

- **Symptom:** First FBX export was 4KB (empty).
  - **Cause:** "Limit to: Selected Objects" was on, but selection was empty/lost at export time.
  - **Fix:** Re-select all, VERIFY orange, re-export → 8.07MB. File size is the tell: KB = empty, MB = real geometry.

- **Symptom:** Single decimation ratio (0.1) mangled the visible exterior of `Base side Sheet_BA`.
  - **Cause:** That part needed a bit more fidelity than the pumps.
  - **Fix:** Bumped only that part to 0.2; left the other 17 at 0.1. Per-part where it counts.

## Decisions made

- **Mesh tolerance 1.0mm, not finer:** triangle count on pumps is driven by feature count, not curvature, so finer tolerance gave diminishing returns. Real reduction happens in Blender decimate, not FreeCAD meshing.
- **Decimate 0.1 for 17 parts, 0.2 for base side sheet:** 0.1 is fine for a familiarisation trainer viewed from ~1m. Spent extra triangles only where the exterior visibly needed it.
- **Shade Auto Smooth instead of more triangles:** perceived smoothness gained for free via shading normals, not geometry — keeps VR poly budget low.
- **Deleted Camera + Light in Blender:** Unity provides its own; they only caused select-all contamination.
- **FBX export -Z Forward / Y Up:** Unity convention; machine arrives upright with no manual rotation needed. Confirmed correct on import.
- **Floex seated at Y = -0.6:** model pivot is at vertical center, not base. (Future nice-to-have: set origin to base in Blender so it drops in at Y=0.)
- **Scale = CAD native (mm to m):** trusting the CAD's own dimensions; still flagged for absolute confirmation with Shiv.

## Open questions / next

- **TOMORROW FIRST MOVE: on-device VR test on Quest 3.** Build/deploy APK (or Link), stand in the scene, check (a) machine scale feels human-correct standing next to it, (b) orientation, (c) player spawn distance.
- **Player spawn distance:** rig is at (0,0,-2). In laptop Play mode the player looked maybe too far from the machine — but VR distance perception differs hugely from a flat view. Do NOT change yet; judge on-device tomorrow and adjust rig Z if it feels off.
- **Machine height still unconfirmed vs physical unit** — pole ~0.8m, parts sub-meter, internally consistent and realistic, but get the real total height from Shiv and correct with one uniform scale if needed.
- **`CDM Door_BA` appears proud of the chassis face** in Blender — confirm against CAD / real unit whether intended; if not, nudge that one panel flush in Blender and re-export.
- **Materials/textures:** machine is untextured grey in Unity. Texturing/materials is a later phase.
- **The 7-screen console UI** still to come as Unity UI on the `12 Inch Assy115` console face — later phase.

## Time spent

~Full session. Roughly: FreeCAD meshing + OBJ export ~1h; Blender import + the long transform/scale cleanup (incl. two invisible-machine recoveries) ~1.5h; decimation + inspection + FBX export (incl. the 4KB dud) ~1h; Unity import + placement + play-mode check ~0.5h.

---
### Note
The `ask_user_input` elicitation tool again did not capture selections all session (kept echoing questions back) — fell back to prose recommendations throughout. Persistent across Day 5 and Day 6; worth a thumbs-down to Anthropic.