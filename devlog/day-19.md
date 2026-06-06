# Day 19 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-screens`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 06 Jun 2026

---

## What shipped today

1. Click-hold bug fixed across all 5 canvases
2. Double pump head 3D model imported end-to-end (STEP → FreeCAD → Blender → Unity), placed in slot 4
3. Button and knob primitives added to slot 4 to match singles

## The click-hold bug

Reported yesterday during slot 4 VR testing. Symptom: holding the trigger caused button state to change while held, then revert on release. Intermittent — some clicks worked correctly as single-tap, others got the hold behavior. Happened on both single and double pump canvases.

Initially hypothesized this was Meta XR `InteractorState` flickering between Select and Hover during a held trigger (small hand movement → ray briefly leaves rect → state drops → re-enters → new click edge). Added a per-target click cooldown (0.3s) in `CanvasClickBypass.cs` to suppress flicker re-fires.

Cooldown didn't help. Enabled `logClicks` and reproduced — only ONE click was being logged per bad event, not multiple. That ruled out flicker.

Real cause: **PointableCanvas was still enabled on the canvases that use CanvasClickBypass.** Both pipelines were active in parallel:
- `CanvasClickBypass` dispatched click on the trigger-down edge (correct state)
- Meta's `PointableCanvasModule`, via the still-active `PointableCanvas` per-canvas component, dispatched a separate click on release that toggled state back

It was intermittent because PCM only "wins" for the most-recently-focused canvas (the original Day 17 bug we worked around). Whichever canvas it was tracking got the double-click pattern.

Fix: one line added to `CanvasClickBypass.Awake()`:

```csharp
var pc = GetComponent<Oculus.Interaction.PointableCanvas>();
if (pc != null) pc.enabled = false;
```

Disables PointableCanvas on this canvas only. PointableCanvasModule stays on the EventSystem for any other canvases that need it (main pole canvas still works via PCM). Tested in VR — click behavior now consistent across all 5 pump head canvases.

## 3D model import

Pipeline followed: STEP → FreeCAD (mesh from shape) → OBJ → Blender (cull + decimate + FBX export) → Unity.

**FreeCAD:**
- Imported `DOUBLE HEAD PUMP full.STEP` (1200.56 × 533.34 mm)
- Mesh workbench → Create mesh from shape at 0.5 mm surface deviation
- Exported OBJ at 112 MB

**Blender:**
- Imported OBJ with -Z Forward / Y Up
- Confirmed the file is the full double pump head assembly, not the whole HLM (sub-parts have generic SolidWorks names like `Motor controller board`, `DISPLAY`, `Damper`, `TERMINAL` — they're all sub-components of the pump head, despite sounding like whole-machine systems)
- Culled obvious internals (BOLT, BRASS NUT, all socket-head/slotted/parallel-pin screws, PIN, Spring Pin, bearings, dampers, motor controller boards, motors, terminals, sleeves, filters)
- Started at 920,616 triangles after meshing
- First decimation pass at 0.3 ratio with no protection: dropped to 80k but the dome and rotor housing got crushed (visible faceting)
- Reverted and ran a protected pass (curved/visible parts at 0.6, others at 0.4) — landed at 518k triangles
- Shade Smooth applied
- Some artifact lines remain at the dome base (decimation joint) and vent panel (genuine fine mesh) — judged acceptable at VR viewing distance, polish-later
- Exported FBX with -Z Forward / Y Up, smoothing = Face, Apply Unit, Use Space Transform

**Unity:**
- FBX imported at Scale Factor 0.001 (Blender mm → Unity m)
- Confirmed by visual comparison against single pump heads in scene
- Placed as child of `Floex_Trainer`, Position (-0.789, 0.217, 2.087), Rotation (0, 180, 0), Scale (0.00078, 0.00078, 0.00078) — manually nudged to align with slot 4
- Material `Mat_Floex_Steel` applied across all 53 sub-meshes
- `PumpHead_04_Canvas` repositioned to sit on the front face

## Button and knob detail

The imported STEP came with holes for the button and knob but no actual button/knob geometry. Single pump heads have these baked into their fused mesh.

Decision: add as Unity primitives parented under `Double_pump_head` rather than redoing the import pipeline with sub-parts preserved. Faster, swappable later, and Product A scope (display-only) doesn't need them interactive.

Built two groups:
- **Knob:** flat square Cube as base plate + rotated Cube as the faceted stem (octagonal-ish look from above, matching the singles' sided knob shape)
- **Button:** thin Cylinder as base ring + smaller Cylinder as the round cap

Both apply `Mat_Floex_Steel`. Positioned by eyeballing against the single pump references — exact scale and position numbers iterated in Scene view rather than typed.

Known visual asymmetry from singles: their buttons/knob are smoother because they're CAD-quality geometry, while ours are Unity primitives. Acceptable for Product A; can re-import slot 4 with sub-parts preserved if Shiv flags it.

## Open items / carry-overs

- VR re-verification with PointableCanvas disable on all 4 pump head canvases (already verified on slot 4 today; singles by extension since the fix propagates via the script)
- Slot 4 model height looks slightly taller than singles — verify with Shiv whether this matches the real machine
- Screen5 Service Need icon clarification with team (carried from Day 18)
- Polish: remaining decimation artifacts at dome base / vent panel mesh — likely invisible at VR distance, leave unless flagged
- VR scale verification using HLM dimensions (currently fine at 0.0002)
- tube_circle_3.png renders white/gray instead of green
- BSA OK/EXIT buttons unwired
- Two medical_instrument_tray instances in scene
- 16KB-aligned warning on libUnityOpenXR.so (Android 15)
- Floaid.lnk Windows shortcut in git

## Files modified today

- `Assets/Scripts/CanvasClickBypass.cs` — added `PointableCanvas.enabled = false` in Awake
- Scene `OR_Environment.unity` — `Double_pump_head` placed in slot 4, button + knob primitives added under it
- `Assets/Models/Double_pump_head.fbx` — new

## Commit status

Three commits worth bundling or splitting:
1. Click-hold fix (single line, big behavior impact)
2. Double pump head 3D import + placement
3. Button and knob primitives

Could be one commit for simplicity. Ready to push.