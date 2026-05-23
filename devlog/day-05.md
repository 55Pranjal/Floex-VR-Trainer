# Dev Log — Day 5 (2026-05-23)

## Goal

- Run the FIRST real pipeline pass on the actual Floex CAD: the 292MB STEP, full assembly, 1000+ components.
- Get it safely into FreeCAD, assess what's inside, and curate down to ~15–25 visible/touchable hero parts.
- Export the curated set as STEP, ready for the mesh → Blender → FBX → Unity steps tomorrow.

## What I actually did

- Set up today's branch. Hit a snag first: an uncommitted 1-line change on `feature/or-environment` (ambient sky color, warmed from neutral grey to a warm tone). Decided to keep it — committed to `feature/or-environment`, merged into `main`, pushed, then branched `feature/cad-pipeline` off the updated `main`.
- Decided NOT to put the raw 292MB STEP in the repo. It's a source input, not a project artifact — lives in `C:\Users\User\Downloads\HLM_Assembly.STEP`. Only curated outputs (curated STEP / OBJ / FBX) come back into the repo via LFS.
- Configured FreeCAD STEP import for a navigable assessment: **unchecked** "Enable STEP compound merge", **checked** "Expand compound shape", left instance names ON. (Compound merge would have fused everything into one blob and killed per-part curation.)
- Imported the 292MB STEP. Slow but successful on 32GB RAM. Model rendered as a recognizable Floex; tree came in with REAL, readable part names (not gibberish IDs) — huge win for curation.
- Saved immediately as native `HLM_Assembly.FCStd` (Downloads, not repo) so re-opening is fast and the slow STEP import is a one-time cost.
- Surveyed the tree: ~60–70 top-level items. Roughly 40 are pure fastener/hardware junk (positioning pins, rivet nuts, ISO bolts/nuts, pole nuts), plus an ELECTRONICS BOX ASSY, fans, and latches. The rest are hero parts.
- Curated via the hybrid approach: hid everything, then revealed only the hero parts. (Spacebar on the parent `HLM_Assembly` only toggled the parent's own flag — children kept their individual visibility — so manually hid all, then ctrl-selected and revealed the keepers.)
- Verified two judgement-call parts visually: the four pump bays show REAL roller-pump geometry (rollers, occluder raceway), not empty boxes — keep. `Tray_BA` (top work surface) was missing and the machine looked incomplete up top without it — added it. `BOX CAP.CATPart` left out (no visible gap).
- Final keep set = **18 parts**: Base bottom Sheet, Base side Sheet, 4× Pump_assembly_FLOAID, CDM Door + CDM Door001, Service Pannel, Bottom Frame, Top Frame, 4× Pole, Tray, 12 Inch Assy (console housing), CDM With…Wemake013.
- Exported the 18 to `C:\Users\User\Downloads\Floex_Curated.step` (STEP, Millimeter, AP214).
- Re-imported the curated STEP into a fresh FreeCAD document to verify — confirmed exactly 18 parts, no hidden junk carried over.

## What broke and how I fixed it

- **Symptom:** `git checkout main` aborted — local changes to `OR_Environment.unity` would be overwritten.
  - **Cause:** An uncommitted 1-line ambient-color edit was sitting on `feature/or-environment` from end of Day 4.
  - **Fix:** Diffed it, decided it was wanted, committed it to the branch, merged to main, then branched.

- **Symptom:** Pressing Spacebar on `HLM_Assembly` hid the parent but children stayed visible.
  - **Cause:** In FreeCAD, parent and child visibility flags are independent; toggling the parent doesn't cascade.
  - **Fix:** Manually hid all, then ctrl-selected the hero parts and Spacebar'd to reveal only those.

- **Symptom:** First curated STEP export came out at 162MB — alarmingly large for 18 parts; feared the hidden bulk had been exported.
  - **Cause (ruled out):** "Export invisible objects" was checked in the export dialog (caught and unchecked before the real export). The 162MB was NOT the junk.
  - **Actual cause:** STEP stores exact B-rep math (NURBS surfaces), not meshes — high-fidelity parts are inherently chunky as STEP. Verified by re-importing the export and counting exactly 18 parts.
  - **Fix / takeaway:** STEP file size is a poor predictor of final mesh size. The real size collapse happens at mesh + decimate in Blender. 162MB STEP is fine here.

## Decisions made

- **Raw 292MB STEP stays out of the repo** — it's a source input, not an artifact. Canonical copy is in Downloads / Drive. Keeps the repo and LFS quota lean.
- **Phase 1 scope: on-screen console only; no individually-grabbable pump heads.** (Confirmed with Shiv.) Pump-head interaction is Phase 2. This let us export ONE combined STEP rather than splitting the 4 pumps into separate files — no separation work for interactivity we're not building yet.
- **Export STEP first (not a direct mesh-to-OBJ shortcut)** — first real-data run should follow the exact dry-run-proven pipeline so any failure is attributable to the data, not a changed process. Optimize later.
- **Import settings: compound-merge OFF, expand-compound ON, keep instance names** — required to keep parts individually named and selectable for curation.
- **Curation = hide-all then reveal-keepers (non-destructive)** — pristine `HLM_Assembly.FCStd` left untouched as a Phase-2 fallback (re-export the 4 pumps separately from it later).

## Open questions / next

- **Tomorrow's first move:** mesh the curated STEP in FreeCAD (mesh-from-shape), export OBJ, then into Blender to delete any remaining internals + decimate, then FBX → Unity. This is the not-yet-on-real-data half of the pipeline.
- **Machine dimensions still unconfirmed.** Bounding-box readout showed ~3787 × 1682 mm and earlier ~4721 × 1686 mm — the large figure is almost certainly parts spread in space / a diagonal, not true height. Need the real H×W×D (mm) to scale ÷1000 in Unity so the VR machine matches a real one. Confirm with Shiv / from the CAD.
- **Pump internals:** pumps came in with real internal geometry. May still be heavier than needed even for look-only — decide in Blender whether to strip the deepest internals for poly budget.
- **`BOX CAP.CATPart`** left out — re-check tomorrow it isn't a visible cover that leaves a gap.
- **Note for Phase 2:** the pristine `HLM_Assembly.FCStd` is the source for re-exporting the 4 pumps individually. Don't delete it.

## Time spent

~Full evening session. Roughly: Git/branch + ambient-change detour ~0.5h; FreeCAD import settings + the slow 292MB import + save ~1h; tree survey + hybrid curation + visual verification ~1h; export + size diagnostic + re-import verification ~0.5h.

---

### Minor note
The `ask_user_input` elicitation tool wasn't capturing selections this session (kept echoing the question back) — fell back to prose recommendations. Worth a thumbs-down to Anthropic if it persists.