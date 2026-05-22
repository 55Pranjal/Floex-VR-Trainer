# Dev Log — Day 3 (2026-05-21)

## Goal
1. Receive and verify the real Floex CAD — if it lands, it's top priority over everything.
2. Until it lands, do CAD-independent prep: Git LFS readiness, then start the OR environment.
3. Don't sit idle waiting; don't drift toward a full hardware sim.

## What I actually did
- **Wrong file diagnosed (not a blocker):** A `Pump_base_Assembly.STEP` arrived (~3.5MB, 41
  solids, ~23 named parts — base support, side frame, front lid, door, M4 fasteners). Inspected
  the STEP header/product names: it's a single sub-assembly (the pump-base cabinet), NOT the full
  ~100MB machine. In FreeCAD it "looked like a box" only because the view was buried inside one
  flat face — **View → Fit all** (numpad `0` for iso, then Fit all) showed it correctly as a 3D
  cabinet. Confirmed with the CAD team member: sent by mistake, correct file coming.
- **Git LFS readiness (ahead of the real file):** `git lfs track` showed the Unity template only
  covered `.fbx/.png/.psd/.wav` etc. — no CAD formats. Added `*.step`, `*.stp`, `*.STEP`, `*.STP`,
  `*.obj`, `*.OBJ` to `.gitattributes`, committed and pushed **before** the big file arrives (LFS
  only catches a file if the pattern is committed first). Commit: `de7615c` →
  "chore: track STEP/STP/OBJ in Git LFS ahead of Floex CAD import".
- **Two-machine setup clarified:** Personal laptop = docs/.md/Git only; Floaid laptop = all heavy
  Unity/Blender/FreeCAD. The push-rejected-then-pull dance was just the two machines reconciling
  (pulled in day-01/02 logs, CLAUDE.md updates, pipeline SVG, grab prefab). Resolved cleanly.
- **OR environment started:** New branch `feature/or-environment` off `main`. Created a dedicated
  `OR_Environment.unity` scene (URP Empty template → Main Camera, Directional Light, Global
  Volume). Laid the first object — a 10×10m `Floor` plane at world origin (0,0,0). Stopped here.
- **CAD delivery strategy resolved:** Team member confirmed the full assembly is too big to come
  as one clean STEP. Of his three options — (1) full assembly file, (2) individual component
  STEPs, (3) sit together 1–2h and pick parts — **chose option 3**. One session replaces a week
  of back-and-forth and doubles as first Floex domain-knowledge transfer.

## What broke and how I fixed it

- **Symptom:** Opened the delivered STEP in FreeCAD; it rendered as a featureless grey box. Looked
  like a wrong/corrupt file.
  - **Cause:** Two things. (1) The camera was zoomed inside one large flat face of a panel, so a
    big flat surface viewed head-on reads as a "wall." (2) Separately, it genuinely *was* the wrong
    file — a pump-base sub-assembly, not the full machine.
  - **Fix:** **Fit all** (numpad `0`, then Fit all) to see it as a proper 3D cabinet — this is the
    same "did it import right?" check to repeat on the real file. Then confirmed with the CAD team
    it was sent by mistake; correct file pending.

- **Symptom:** `git push` rejected — "Updates were rejected because the remote contains work you do
  not have locally."
  - **Cause:** `origin/main` had commits made from the *other* machine (personal laptop did the
    .md/docs work). Local was behind.
  - **Fix:** `git pull` (clean `ort` merge), then `git push`. Synced. Root cause is the two-machine
    workflow — see decision below.

- **Symptom (caught, not yet bitten):** Committed `.gitattributes` while on `main`, not the work
  branch.
  - **Cause:** Had `git checkout main` before committing.
  - **Fix/judgement:** Harmless here — `.gitattributes` is repo-wide housekeeping that belongs on
    `main` anyway. Noted so the branch map stays accurate.

## Decisions made
- **CAD delivery = option 3 (sit-together session).** Highest-leverage 2 hours available: avoids
  deleting thousands of internal parts (option 1) and avoids death-by-follow-up guessing (option 2),
  and transfers domain knowledge. The Day-2 assumption of "one ~100MB STEP" is now retired — the
  real delivery is a curated *set* of sub-assembly/part STEPs.
- **Track CAD formats in LFS ahead of time** (both upper/lowercase — Windows is case-insensitive
  but Linux/CI is not, so `*.STEP` ≠ `*.step` for future collaborators).
- **Two-machine ritual:** pull-before-start, push-before-stop; do all Unity/Blender/FreeCAD only on
  the Floaid laptop so the dangerous `.unity`/prefab merge-conflict class can't happen.
- **OR scope = bare room, stationary player, Unity primitives.** Smallest thing that makes the
  Floex feel located; detail budget goes to the console (7-screen spec), not the wallpaper. Polish
  is a deliberate later swap, not a Day-3 dependency.
- Ignored the Meta XR "1 recommended fix" nag — won't auto-apply Meta's suggestions without reading
  them (same principle as declining the "update to 201" nag).

## Open questions / next
- **CAD session:** schedule the 1–2h sit-down ASAP. Walk in with a component checklist (hero assets:
  chassis, console/control panel, pump heads, visible mounts/knobs; skip internals/fasteners). Ask
  for: STEP per logical part, overall machine dimensions (for Unity scaling), reference photos of the
  console face, record the session for domain notes.
- **Hands vs controllers** for the final trainer — still unresolved (carried from Day 2).
- **Next move (Day 4):** the CAD session if the team member is free; otherwise continue the OR
  environment (walls + ceiling + lighting on `feature/or-environment`).

## Time spent
Partial session — file diagnosis, LFS prep, two-machine cleanup, and starting the OR scene
(stopped after the floor). Most time went to diagnosing the wrong STEP and the LFS/Git sync.