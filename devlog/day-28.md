# Day 28 Devlog — Floex VR Trainer

**Branch:** `feature/touch-screen`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 15 Jun 2026

---

## Goal

Fix the deformed Base Bottom Sheet in the HLM model. It came through deformed in the original full-HLM CAD pipeline pass; today's job was to isolate which part was causing the deformation and reimport just that part cleanly, rather than re-running the whole HLM import.

## What I did

**Isolated the deforming part.** Traced the deformation in the Base Bottom Sheet back to the specific part responsible (rather than the whole sheet assembly), so the fix could be a targeted single-part reimport instead of redoing the full HLM pipeline.

**Reimported the part through the CAD pipeline.** Ran the part through the established FreeCAD → Blender → Unity route (leaf-level selection, mesh from shape at 0.5mm surface deviation, Blender cleanup, FBX export, Unity import at Scale Factor 0.001) and dropped the corrected geometry into the scene in place of the deformed one.

**Asset import (parked).** Started bringing in two new doctor assets (one glTF, one FBX, both visual-only props). Both lost their textures on import — FBX needs texture/material extraction + URP material conversion; glTF needs glTFast (or to be dropped in favour of the FBX). Decided not to chase it today to avoid adding package dependencies to a project with a working demo build. Parked for a later session.

## What broke and how I fixed it

The deformation itself was the bug, carried over from the original full-HLM import. Root cause was a single part within the Base Bottom Sheet rather than the whole sheet — likely a tessellation/mesh artifact from the first pass. Fixing it as a targeted single-part reimport (find the offending part → reimport just it → swap in scene) avoided disturbing the rest of the working HLM geometry. Lesson reaffirmed from earlier CAD days: when one part in a multi-part import is deformed, isolate and reimport that part rather than re-running the whole assembly — far less risk to the parts that already came through clean.

## Decisions

- **Targeted single-part reimport over full-HLM re-run.** Lower risk, faster, and doesn't disturb the geometry that already imported correctly.
- **Doctor assets parked.** Both visual-only; texture loss on import isn't worth resolving right now, and adding glTFast for the glTF would touch package dependencies in a project with a verified demo build. Revisit later — likely fix the FBX (native, no new packages) and drop the glTF.

## Open questions / next

- **Doctor assets** — when revisited: FBX route (Extract Textures + Extract Materials, convert materials to URP/Lit), drop the glTF to avoid the glTFast dependency. Both are visual-only props.
- **Verify the corrected Base Bottom Sheet in VR** at scale alongside the rest of the HLM — confirm the deformation is gone and the part seats correctly.
- Earlier carry-overs still standing: propagate the RPM knob to single pumps 2/3 and the slot 4 dual knobs (on `feature/pump-head-motor-rotation`); BSA keyboard text entry (Meta SDK v74 InputField limitation).

## Time spent

~Half a day (isolating the deforming part + CAD pipeline reimport + scene swap; plus the parked doctor-asset import attempt).

## Files modified today

- `Assets/Models/` — corrected Base Bottom Sheet part reimported and swapped into the HLM
- `Assets/Scenes/OR_Environment.unity` — deformed part replaced with corrected geometry
- `devlog/day-28.md` — new