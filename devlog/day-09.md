# Day 09 — Pole screen correction + JSON-driven screen generator + 3 screens

## Goal

Build the main pole screen of the Floex 3.0 in Unity from the TouchGFX firmware
project as visual reference. Product A scope: screens look real and will navigate,
but values are display-only — no physiology, no live behaviour.

## What I did

### The false start (and Shiv's correction)

Started the morning building what I thought was the main pole screen — but the UI I
was working from was actually the **pump-head screen**, and worse, an outdated version
of it. Shiv caught it: the pole screen and the pump-head screens are different
products with different UIs, and the pump-head UI in the PDFs we had is not current.
The CDM (the screen I should have been building) is the pole screen. Threw away the
morning's work and restarted with the right reference.

Lesson logged: when sources disagree (PDFs vs TouchGFX vs real photos), always
re-confirm which screen lives where before placing the first element. The
"which-screen-is-which" map needs to come from the team, not the PDFs.

### CDM screen — manual build, then pivot to generator

- **Branch/git**: continued on `feature/console-screens`. Session-start ritual:
  `git checkout main && git pull`.
- **Canvas resize**: confirmed the CDM canvas is 1280×800 (not the old 1024×768 from
  the outdated PDF). Canvas on the pole at uniform Scale 0.000176, World Space —
  already correctly mounted from day 8, only the screen _content_ was being rebuilt.
- **Built the CDM screen by hand**, region by region, reading exact coords off the
  TouchGFX Designer Properties panel and applying the TouchGFX→Unity flip
  (top-left anchor, X→X, Y→−Y, same W/H):
  - Header: FloEx 3.0 logo (coral), date, time, battery icon.
  - Cardioplegia panel: cyan header bar, two #141414 readout boxes, left readout
    (syringe + 0.00 ml + alarm/play/timer/history row on navy #0C1B37 boxes),
    right readout (syringe + 0.00 ml + 0 mmHg + download).
  - Status row: PRESSURE (P1/P2), LEVEL, BUBBLE tiles — each = coloured header bar
    - #141414 body box + split "X DETECTED : Y" text + indicator graphic.
  - Temperature strip: T1–T4, each "00.00 °C" on a navy strip.
- About two hours in, mid-CDM, the realisation hit: I was doing the same 5-step
  manual click sequence for every single element (~30 per screen × many screens
  to come). The reading-off-TouchGFX part is irreducible, but the clicking-in-Unity
  part absolutely isn't. **Stopped and built a generator.**

### The JSON-driven screen generator

- `Assets/Editor/ScreenBuilder.cs` — menu item **Floex → Build Screen From JSON…**.
  Reads a screen spec JSON, instantiates the whole hierarchy under the selected
  Canvas in one click. Does the coordinate flip once, in code. Static elements
  only (scope-lock).
- `Screen_CDM.json` — full spec table for the CDM screen.
- Validated it by renaming the hand-built CDM to `Screen_CDM_Manual`, generating
  a fresh `Screen_CDM` from JSON, and comparing side-by-side. Matched → generator
  proven → deleted the manual copy. JSON is now the source of truth.

### Sprite import workflow (reused throughout the day)

TouchGFX icons are PNG-content with misleading `.svg` names. Flow: copy →
`Assets/Textures/UI/` → rename clean `.png` → Texture Type = Sprite (2D and UI) →
Apply → assign + Preserve Aspect. Imported across the day: battery_full, syringe,
alarm, play, history, restore, download, level_bars, bubble_dots, home, speaker,
arrow_up_green, arrow_down_cyan, toggle_off.

### Screen 2 — BSA & Patient

With the generator working, BSA went _fast_: read coords off TouchGFX into
`Screen_BSA.json` → one click → eyeball → tweak JSON → rebuild. The whole screen
took maybe 40 minutes vs the 2+ hours CDM ate manually. Layout: two-column form
(left: Name, Age, Weight, Cardiac Index, BSA, Anesthesiologist; right: ID, Blood
Group, Height, Target Flow, Surgeon, Perfusionist), each row = label + #1A1F29
input box + optional unit suffix (kg, cm, m², L/min/m², L/min). Bottom buttons:
Calculate, OK, EXIT, Save & Exit, Cancel. Header reused from CDM. Nav menu added
to the spec for the first time (text + dividers, X1049 panel) — this list also IS
the screen map: Setup / BSA & Patient / Bubble Sensor / Level Sensor / Pressure
Sensor / Temperature Sensor / Cardioplegia / Timer / System Setting.

### Screen 3 — Bubble Sensor

Same workflow, same speed. Layout: two side-by-side sensor panels (BS1 left, BS2
right, +340 X offset, panel bg #121010), each with section title, Select Pump
label + dropdown box, Threshold (mm) stepper, Enable threshold-based interruption
toggle, Micro Bubble Alarm toggle. Bottom: Save & Exit, Cancel. Header + nav menu
reused.

Interactive widgets (steppers and toggles) are display-only per scope: each stepper
is a 60×60 coloured background box (green #2ECC9B up / cyan #27C0F5 down) with a
50×50 arrow PNG inset (+5,+5) on top — matches TouchGFX's icon-location pattern.
Toggles frozen in OFF state as a single `toggle_off.png` sprite. No listeners,
no behaviour. If we want ON-state toggles later, that's another sprite, not logic.

## What broke & how fixed

- **Floex menu didn't appear at first** — script was fine; Unity hadn't refocused.
  Clearing the console + refocusing surfaced it.
- **Battery sprite warning** on CDM — `[ScreenBuilder] Sprite 'battery_full' not
found`, showed as a white box. Cause: filename mismatch (`battery_full.png.png`).
  Renamed to `battery_full.png` so the builder's lookup
  (`Assets/Textures/UI/<name>.png`) resolves. The warning is the builder's safety
  net working — missing sprites fall back to a coloured box instead of failing.
- **Same again on Bubble Sensor** — `toggle_off` not found, four toggles fell back
  to coloured boxes. Same fix.
- **Unity GPU crash mid-Bubble-Sensor session** — "Failed to present D3D11 swapchain
  due to device reset/removed... GPU Timeout." Unrelated to the build (the hierarchy
  was intact on relaunch). Editor restart cleared it. Logged as a watch-item for
  driver/VR stress, not a code issue.
- **Coord gotcha tripped me once on Bubble Sensor** — instinct was to write a
  negative TouchGFX Y into the JSON because the value "looked negative." It isn't.
  Write positive TouchGFX Y; the generator flips. Re-internalised.

## Decisions

- **Switched from hand-placement to JSON-driven generation, mid-CDM.** Hand-building
  was the right _teacher_ (had to understand the coordinate mapping, anchors, sprite
  quirk first) but the wrong _production method_. The generator removes the clicking,
  not the reading (reading coords off TouchGFX is the irreducible manual part).
  Proof point: CDM took ~2h manual, BSA took ~40min via JSON, Bubble Sensor similar.
- **Generator stays dumb on purpose.** Places static display elements only — no
  scripts, no animation, no listeners. Scope-lock comment at the top of the file.
  This is the guard against Product-A scope creep. Navigation between screens is
  a deliberate later step, not baked into the builder.
- **JSON is the source of truth; Unity is the output.** Don't nudge values in Unity
  while iterating — a rebuild wipes nudges. Fix the number in the JSON and rebuild
  (seconds). Only nudge in Unity once a screen is "done."
- **Collapsed redundant TouchGFX splits.** Temperature readings were 4 elements
  in TouchGFX (label + value + "°" + "C"); collapsed to 2 in JSON (label + "00.00
  °C" as one string). TMP doesn't need the degree split. Same instinct applied
  throughout: match design _intent_ over sloppy TouchGFX source widths (e.g.
  PRESSURE P1/P2 boxes widened to fill the header).
- **Header bars rebuilt as solid-colour Images** (TouchGFX draws them as PNGs);
  palette locked: cyan #27C0F5, red #FF4D4D, green #2ECC9B, yellow #E8E337, navy
  #0C1B37, body #141414, header bg #1A1F29, dividers #3A4150.
- **Interactive widgets rendered as static.** Steppers and toggles look like the
  real thing but do nothing — scope-lock. When values need to "change" later (if
  ever), that's a JSON edit, not a behaviour script.

## Open questions / next

- **Six screens left in the nav map**: Level Sensor, Pressure Sensor, Temperature
  Sensor, Cardioplegia, Timer, System Setting. The screen map (= the nav menu) is
  the to-do list. Pump-head screens may also be in scope but live on a different
  surface; awaiting colleague's coordination on which screens go where.
- **Colleague coordination still pending** — pump-head screens have newer UI than
  the 7-PDF set; need the authoritative source before building any.
- **Scale/placement** — machine may be too small in VR; awaiting real Floex H×W×D
  mm from Shiv. Re-judge player spawn + footer legibility after scale fix.
- **Possible next tooling** (only if pain is real): AssetPostprocessor to auto-set
  Texture Type = Sprite on import. Not yet warranted — sprite imports were maybe
  10–12 today across all three screens.
- **Housekeeping**: `Floaid.lnk` (Windows shortcut) still untracked — verify
  `.gitignore` covers it before next commit.
- **Verify**: degree symbol ° renders correctly in LiberationSans (not a missing-
  glyph box) on the CDM temperature strip.

## Time spent

~6–7 hours. CDM ~2h manual + ~1h generator build + JSON conversion; BSA ~40min;
Bubble Sensor ~1h (the GPU crash + sprite-not-found round-trip ate some).
