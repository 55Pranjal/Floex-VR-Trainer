# Day 14 — OR room set-dressing + every console screen interactable

## Goal
Two halves to today. Morning: make the operating room recognisable as an OR (Day 12
picked the assets; today actually places them). Afternoon: make every screen in the
CDM nav menu actually *do* something — Product B scope (button clicking + value
changing, no physiology).

Day 13 was a rest day.

## What I did

### OR room set-dressing (morning)

Six Sketchfab models imported into `Assets/Models/Hospital/`: hospital bed, patient,
anaesthesia machine, IV pole, ECG monitor, medical cart, instrument tray, ceiling
light.

- glTFast installed via "Add package by name" with `com.unity.cloud.gltfast` —
  the registry search returns "no results for gltfast" but the package install
  works fine. Known quirk.
- Burst threw `BurstCompiler.Compile` errors blocking the glTF imports after the
  package install (FBX models unaffected). Burst cache went stale on the version
  bump. Fix: closed Unity, deleted `Library/BurstCache`, reopened. Clean.
- Hospital bed needed scale 0.005, Y offset 0.347, rotation (-90, 90, 0) to land
  right. Other models close to 1:1 with minor tweaks.
- Patient FBX came with embedded materials and no Materials folder — that's the
  FBX-with-embedded variant, not a problem. URP magenta didn't need a fix on it.
- Anaesthesia machine threw polygon self-intersect warnings on Cylinder10 —
  cosmetic, ignored.
- Two instrument trays got placed by accident — cleanup TODO, delete one.
- Patient drape skipped for v1 (revisit if a demo audience reads naked patient
  as odd).

### Room geometry
Walls at scale (6, 2.3, 0.2) for the long axis and (0.2, 2.3, 6) for the short.
Final room ~6×6×2.3m at real OR scale. Floor at scale 0.6 (Unity Plane = 10m at
scale 1). Ceiling = floor duplicate at Y=2.3 with X-rotation 180°.

Shiv eyeballed the walls and they fit the trainer footprint. Real machine
dimensions still pending — but acceptable for now.

### OR lighting (where the morning time went)
First attempt parented three Spotlights to the `ceiling_light` fixture prefabs.
Looked right in the inspector — wrong in scene: the child lights inherited the
parent's rotation and shone into the walls instead of down at the bed. Prefab
restriction blocked the rotation override.

Final config: three **top-level** Spotlights, not parented. Position matches each
fixture's X/Z with Y=2.2, rotation (90, 0, 0), Intensity 15, Range 4, Spot Angle
80, pure white. Ceiling fixtures kept as decoration only. Directional Light dimmed
to 0.3 for ambient fill.

**Lesson logged**: when an object's world position looks wrong but Inspector values
are right, suspect parent-transform inheritance. Frame each object with F to see
its actual world position rather than trusting the Inspector numbers in isolation.

### Commit (morning)
102 files, 7,419 insertions, commit `484038f` on `feature/console-screens`:
"scene: hospital room set-dressing complete". LF→CRLF Windows warnings ignored
(informational). `git push` still pending.

One stray file: `devlog/day-13.md` slipped into the commit. Day 13 was a rest day
and there's no real content there. Cleanup later.

### Temperature Sensor bug fix
Up/down arrows weren't firing on the Temperature screen. Tracked it down:
`SpinnerController` had gone missing from `Screen_TemperatureSensor` somehow — only
`CyclerController` was attached, which is why the alarm cycles worked but the
spinners didn't. Re-attached with 8 fields (T1Max..T4Min, all min=-5 max=150 step=1).

### CyclerController extended twice
The original `CyclerController` expected `Box_{name}` (clickable) + `Img_{name}`
(visual tinted by state). That broke on Level Sensor where toggle sprites have
no separate Box wrapper — the visual *is* the clickable.

First extension: fallback path. Try `Box_{name}` first; if missing fall back to
`Img_{name}` and disable the Button's ColorTint transition so it doesn't fight the
state tint in `Refresh()`.

Second extension: optional `sprite` field on each `State`. If set, swap the sprite
instead of tinting. This is how toggles look right — `toggle_off` / `toggle_on` as
actual sprite swap, no muddy colour over the "off" graphic. Imported `toggle_on`
with identical import settings to `toggle_off`.

### Level / Bubble / Pressure sensor screens
- **Level Sensor**: 3 toggles (Arterial, ToggleInterrupt, ToggleAlarm). Cycler with
  Img-fallback. Worked first try.
- **Bubble Sensor**: 2 dropdowns + 2 spinners + 4 toggles. Collapsed firmware's
  expand/collapse dropdown UI into a 4-state cycler (click → ARTERIAL → CARDIOPLEGIA
  → SUCTION → VENT), each state writing the pump name into `Txt_BSnDropdownLabel`.
  Spinners 0–300, no wrap.
- **Pressure Sensor**: 2 dropdowns + 6 spinners + 2 toggles. JSON shipped with
  three elements sharing identical names (`Box_BS1Up` × 3 for the three stepper
  rows) — generator collisions would have broken it. Renamed by purpose:
  `Box_PS1CalUp` / `Box_PS1ThreshWarnUp` / `Box_PS1ThreshAlarmUp` so each stepper
  row is independently addressable.

All three tested in VR. Required two trips back: forgot to attach `CyclerController`
to Pressure Sensor first time, and Bubble Sensor had a config typo in the Inspector.
Both my fault; fixed quickly.

### Cardioplegia / System Setting / Timer
- **Cardioplegia**: one cycler. AUTOMATIC ↔ MANUAL on the Type dropdown. The
  First/Normal Dose values stay static — firmware has the keyboard editing for
  those commented out, so the trainer mirrors that. Total Volume rows frozen
  empty (physiology accumulators, out of scope).
- **System Setting**: zero interactive elements. The whole firmware View is
  `setupScreen()` reads + `timeUpdateTOH()` ticker. Not even the Reset button has
  a handler in firmware. Trainer leaves it that way — read-only by design. Honest
  mirror of firmware behaviour, and the right call: in the real Floex a perfusionist
  comes here to *read* status, not change anything.
- **Timer**: 4 toggle cyclers with sprite swap. Set Time fields would need
  keyboard input — deferred. Reset All has no firmware handler either.

I had predicted Timer would be a ticking-clock pattern with a Coroutine. Re-reading
the firmware corrected me: the elapsed-time tick lives on `Screen_1` (the main pump
screen), not here. Timer is a settings screen — same toggle pattern as the rest.
No new code needed.

### BSA & Patient — Path A scaffolding (last big push)
The biggest screen in the trainer. 9 keyboard-input fields in firmware (Name, ID,
Age, Blood Group, Weight, Height, Surgeon, Anesthesiologist, Perfusionist) + 3
calculated fields (Cardiac Index, BSA, Target Flow) + a Calculate button.

Wrote `Assets/Scripts/BSAFormController.cs`. At `Start()` it finds each `Box_X` +
matching `Txt_X` overlay, attaches a `TMP_InputField` at runtime using `Txt_X` as
display target. JSON has 12 new `Txt_X` overlays added inside the Boxes.

Calculate reads weight and height as floats, computes `BSA = √(h × w / 3600)`,
writes to `Txt_BSA`. With defaults John Doe / 75 kg / 180 cm, BSA = 1.94 m².

Cardiac Index and Target Flow stay empty. Firmware computes
`CI = cardioplegia1_Out / BSA` — needs physiology data the trainer doesn't
simulate. Honest scope-lock.

Cancel reverts every editable field to its value at screen-open (snapshot in
`OnEnable`).

- **Editor**: 9 fields focus, accept physical-keyboard input, Calculate computes,
  Cancel reverts. Clean.
- **VR**: fields show defaults, Calculate works, Save/Cancel work, fields accept
  focus — but Quest's system keyboard doesn't appear. Need Meta's
  `OVRVirtualKeyboard`. Couldn't find the prefab in the v74 SDK install path —
  may need a separate package (Meta Platform SDK?) or different install location.
  Stopped here. Separable Day-15 task.

## Decisions
- **Path A over Path D for BSA**: full interactive form is the right Product B
  shape. Path D (pre-populated + Calculate only) would have made the biggest screen
  in the trainer the *weakest* interactively. Bad asymmetry.
- **System Setting stays read-only**: firmware has no handlers, trainer mirrors
  that. Resisted the urge to add fake interactivity for its own sake.
- **OR room v1 skips patient drape and surgical lamp.** Drape is aesthetic.
  Surgical lamp couldn't be modelled cleanly in time, and the three top-down
  Spotlights cover the lighting need without a visible fixture.

## Open items
- `git push` Day 14 commit — committed locally only.
- BSA Quest virtual keyboard. Three paths to triage Day 15: Meta's
  `OVRVirtualKeyboard` (right answer if findable in v74), custom on-screen keyboard
  widget (~1.5–2 hrs, SDK-independent), Bluetooth keyboard paired to Quest (zero
  code, deployment-context-dependent). Don't pick under pressure.
- `devlog/day-13.md` slipped into the morning commit — drop it.
- `Floaid.lnk` Windows shortcut still in git from way back.
- Two instrument trays in the scene — delete one.
- Real machine dimensions from Shiv still pending. Trainer scale is acceptable but
  not confirmed-accurate.
- Pump-head screens (`Screen_Main`) still need screen-ownership clarified with the
  colleague.

## Time
~7 hours including the lighting debug detour and two screens that needed
re-attaching controllers after initial misses.

## State at EOD
Every screen in the CDM nav menu is interactive at Product B level. The OR
environment looks like an OR. BSA architecturally complete, missing only the VR
input method.