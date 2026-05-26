# Dev Log — Day 8 (2026-05-26)

## Goal

- Start materials/texturing — make the Floex look like the real unit instead of plain grey.
- Then begin the 7-screen console UI on the `12 Inch Assy115` face — the interactive core of Product A.
- Keep control-input/display logic decoupled from behaviour (Day-7 design note).

## What I actually did

### Git recovery (unplanned, first thing)
- Opened Unity to find the OR scene showing the OLD placeholder cube and no `Models` folder — looked like Day 6–7 work was lost.
- Diagnosed: local `main` was 6 commits behind `origin/main`. The session-start `git pull origin main` updated `origin/main`'s tracking ref but, because I was on `feature/cad-pipeline` at the time, never fast-forwarded my local `main`. So `checkout main` → `checkout -b feature/materials` branched from the stale Day-4 state.
- Confirmed everything safe on remote (`origin/main` at `ffb4450`, FBX present in commit `33c3fa6`). Fixed: `checkout main` → `reset --hard origin/main` → deleted misplaced `feature/materials` → re-cut it from correct HEAD. Real Floex back in scene.
- **Lesson:** `git pull origin main` merges into the *current* branch, not local `main`. Correct ritual: `git checkout main && git pull` at session start.

### Materials
- Created `Assets/Materials/Floex/`. Built `Mat_Floex_Steel` (URP/Lit), tuned metallic/smoothness to match the real office HLM unit (which is monotone steel-grey — confirmed in person).
- Initially explored a three-tone scheme (white chassis / steel poles / dark pumps) but **reverted to uniform steel** because the real unit is monotone — fidelity to the actual machine beats invented variety for a familiarisation trainer.
- Applied steel to all 18 parts. Console face keeps its own `color_727980` material. Deleted unused WhiteMetal/DarkPlastic materials.

### Console UI foundation (the hard part)
- Measured the `12 Inch Assy115` panel via a one-shot `BoundsReader` script: world size 0.26 × 0.19 × 0.04 m, world center (-0.16, 0.71, 1.03). Panel faces the player (note: scene's "Back" gizmo view = player viewpoint).
- First attempt parented a world-space Canvas directly to the panel — painful, because the CAD sub-mesh carries a baked Scale 100 / Rotation -89.98, so every child value got multiplied/rotated.
- **Refactored to a clean anchor:** created empty `ConsoleMount` under `Floex_Trainer` (the clean root, Scale 1 / Rot 0), positioned at the panel via local coords (-0.16, 1.31, 1.03 — Y offset accounts for Floex_Trainer's -0.6). Re-parented Canvas under ConsoleMount → clean coordinate space. This made all subsequent UI work sane.
- Set Canvas to World Space, uniform Scale ~0.00022, sized to sit inset within the metal panel bezel.

### Main / Pump Operation screen (complete)
- Built full screen matching the UI spec: `Screen_Main` container + full-stretch dark `Background`.
- Header: `Txt_Logo` "FloEx 3.0" (coral, bold), `Btn_PumpSelect`, `Txt_Clock`.
- Readout: "4.2 L/PM" and "100 RPM" (big number right-aligned + small unit left-aligned to avoid overlap).
- `Btn_Start` (green) / `Btn_Stop` (red).
- Footer: blue band (`Footer_BG`) with two rows via **Horizontal Layout Group** (auto-even spacing): Row1 = CC / 1/4" / CW / CI 2.2 / MUTE; Row2 = ► / 10:10:00 / HIST / LOCK. Used text/Unicode stand-ins for icons (in scope — real icon sprites are a later polish pass).
- All text is **TextMeshPro** (crisp in VR; imported TMP Essentials).

### Decoupled architecture (proven on Main screen)
- `PumpState.cs` — pure data layer (flowRate, rpm, isRunning, tubeSize, direction, cardiacIndex). State-change methods (`StartPump`/`StopPump`) ONLY flip state, never touch UI. Deliberately computes no physiology — keeps it Product A.
- `MainScreenDisplay.cs` — display layer, one-way: reads PumpState, formats, pushes into TMP fields. Never writes state.
- START/STOP buttons wired via OnClick → PumpState methods. Loop closed: **input → state → display**, fully decoupled.
- Added `Txt_Status` (RUNNING green / STOPPED grey) as a visible cue. Verified end-to-end in Play mode.

## What broke and how I fixed it

- **Symptom:** Scene opened to old placeholder cube; Models folder + FBX gone.
  - **Cause:** Stale local `main` (6 commits behind), branched a new branch off it.
  - **Fix:** `reset --hard origin/main`, re-cut branch. Nothing lost — all was on remote.

- **Symptom:** Canvas behaved bizarrely — scale needed absurd values (0.00003), needed a 90° counter-rotation, positions drifted.
  - **Cause:** Parented under `12 Inch Assy115`, which has baked Scale 100 / Rotation -89.98 from the CAD pipeline.
  - **Fix:** Intermediate `ConsoleMount` empty under the clean `Floex_Trainer` root. Clean coordinate space; everything sane afterward.

- **Symptom:** UI elements sprawled far beyond the panel, across the back wall.
  - **Cause (two parts):** (1) `Background` was a tiny 100×100 square (insets 462/334), not filling the canvas, so the black "screen" looked tiny while elements lived on the full 1024×768 canvas. (2) Canvas scale was too large/non-uniform for the panel.
  - **Fix:** Stretched Background to fill (anchors 0/0/0/0); set Canvas to uniform ~0.00022 scale → whole screen shrank to fit the panel bezel.

- **Symptom:** Emoji glyphs (🔇 etc.) didn't render in TMP.
  - **Cause:** Not in LiberationSans SDF atlas.
  - **Fix:** Text stand-ins (MUTE, HIST, LOCK); "►" rendered fine. (TMP auto-created a "SubMeshUI" child for the ► glyph — normal, left alone.)

## Decisions made

- **Uniform steel material, not three-tone:** match the real monotone HLM unit. Fidelity > invented variety for familiarisation.
- **Intermediate ConsoleMount anchor:** decouple UI from the messy baked CAD transform. ~15 min cost now, saves hours across 7 screens.
- **Text/Unicode stand-ins for footer icons:** stay in Product A scope; real icon sprites deferred to a polish pass.
- **Architecture-first, then replicate:** prove the input→state→display seam on ONE screen now, build screens 2–7 to that pattern, wire navigation last. Avoids retrofitting decoupling into 7 screens (the Day-7 "expensive to retrofit" warning).
- **Deferred on-device fine-tuning:** Link test today confirmed machine is too small (I towered over it) — but screen text WAS readable except the footer, which was hard to read from my giant's-eye height/angle. Batching with the scale fix rather than chasing now.

## Open questions / next

- **NEXT SESSION FIRST MOVE:** build screen #2 (Tube Size Select — grid of 1/8, 3/16, 5/16, 3/8, 1/4, F1, F2 + CANCEL/APPLY) using the Main screen pattern. Then screens 3–7. Then navigation between screens. Then per-screen state wiring.
- **Still waiting on Shiv:** real Floex dimensions (H×W×D mm). On arrival → one uniform scale fix on `Floex_Trainer`; UI rides along automatically (it's parented under the machine). THEN re-judge: player spawn distance AND footer legibility at proper eye-level/scale.
- **Footer legibility:** re-check at correct scale + human eye-level before deciding to bump font size. Don't fix preemptively.
- **CDM Door panel proud of chassis:** still batched for a consolidated Blender pass.
- **Housekeeping:** `BoundsReader.cs` (one-shot measuring script) still in Assets — delete next session if not deleted today.

## Time spent

~Full session. Roughly: git recovery ~30 min; materials ~45 min; UI mounting foundation + ConsoleMount refactor ~1.5h (the bulk — lots of coordinate-space debugging); Main screen layout (header/readout/buttons/footer) ~1.5h; decoupled architecture (PumpState/Display/wiring) ~45 min.