# Dev Log — Day 12 (2026-05-30)

## Goal
1. Wire the last nav piece — Date & Time open/close — to close out the navigation layer.
2. Cross deliberately into Product B: make Date & Time and Temperature Sensor genuinely
   stateful (button clicking + values changing, no device behaviour).
3. Pivot to hospital-room set-dressing in the afternoon: pick asset strategy and curate
   the free models the scene needs.

## What I actually did
- **Date & Time linking** added to `ScreenNavigator` as a second `LinkMap` (`Txt_Date` and
  `Txt_Time` open `Screen_DateTime`; `Box_SaveExit` and `Box_Cancel` close back to `Screen_CDM`).
  Same per-screen wiring pass as the nav rows — harmless on screens that don't contain those
  elements. `Screen_DateTime` is a full screen with its own header, not a modal overlay, so it
  reuses the SetActive swap and needs no new mechanism.
- **Defensive raycast-disable** added inside `WireScreen`: turn off `raycastTarget` on every
  graphic in a screen before re-enabling only the ones we wire. Closed off label-shadowing
  problems before they hit (a TMP label sitting on top of a wired box would otherwise eat the tap).
- **`SpinnerController.cs`** — generic up/down integer stepper. Per-field `min/max/step/start/wrap`,
  Inspector-configurable. `OnEnable` snapshot, Cancel reverts to snapshot, Save just exits with
  the live values intact. Dropped onto `Screen_DateTime` with six fields (Day/Month/Year/Hour/Min/Sec)
  with sensible defaults — Year clamps 2000–2099, the rest wrap.
- **`CyclerController.cs`** — generic N-state cycler. Each click on `Box_{name}` advances through
  a list of states; matching `Img_{name}` is tinted per state; optional `Txt_{name}Label` is
  written if present in the JSON. Same `OnEnable`/Cancel pattern as Spinner; Cancel listener is
  **additive** on top of ScreenNavigator's, so when both Spinner and Cycler live on the same
  screen they both revert in one Cancel tap, no coordination needed.
- **TouchGFX sim detour** to inspect firmware button behaviour. Fixed `Model.hpp`'s missing
  virtual destructor (one-line `virtual ~Model() = default;`). Fixed `Screen_1View.cpp`'s stale
  `#include "main.h"` with `#ifndef SIMULATOR`. Then hit `Model.cpp` which is intimate with HAL
  end-to-end — `HAL_GPIO_*`, FDCAN, RTC, I²C, ADS1115 driver, SD logger, flash storage, plus
  dozens of bare extern globals pulled from `main.h`'s include chain. **Abandoned the sim**;
  reading firmware source directly is faster.
- **Read `TemperatureSensorView.cpp`** to pin down behaviour. Confirmed: spinners use `-5..150`,
  step 1, no wrap. Alarm beep is a 3-state cycler showing `"Low"`/`"Medium"`/`"High"` via a text
  area (not sprite swap). Save calls `presenter->saveTemp1Data()`; Cancel re-reads from Model but
  doesn't navigate (firmware quirk — almost certainly an oversight, not mirrored).
- **`Screen_TemperatureSensor` wired** with SpinnerController (8 fields: T1Max/T1Min … T4Max/T4Min,
  all `-5..150` no wrap) and CyclerController (4 fields: T1Alarm…T4Alarm, defaults match firmware
  ranges). Used icon tint over text label since the Unity screen lacks the alarm text element —
  info blue → caution yellow → coral (FloEx accent), climbing in urgency. Confirmed working in VR.
- Fixed `Img_T1Alarm`/`Img_T2Alarm` typo in Row_T2's JSON (collision with Row_T1's element name).
- **Two clean commits** — one for navigation (pointer bridge through Date & Time links), one for
  state controllers (Spinner + Cycler).
- **Afternoon pivot to hospital-room set-dressing.** Scoped: 4 walls already in place, need
  bed + patient + anesthesia + surgical light + IV + monitor + cart. Set-dressing patient (not
  demonstrable subject). Free curated models > paid pack.
- **Curated six free Sketchfab models** that earn their place:
  - **Patient** (edouard77) — anatomically realistic, lying flat. Will drape a white plane from chest up.
  - **Simple Hospital Bed** (Yvo Pors) — clean PBR. *Caveat:* ward bed not OR table; acceptable for
    set dressing, swap later if the demo audience includes perfusionists.
  - **Anesthesia Machine** (king7830) — recognizable silhouette, clean.
  - **IV Pole** (Mouch) — two poles, one with bag.
  - **ECG Monitor** (demagdev) — vital signs monitor on a stand.
  - **Medical Cart** (NicolasVB) — fills corner space, parallax for VR.
- **Skipped, deliberately:** "Operating Table High Poly" (wrong proportions, sci-fi look) and
  "Medical instrument tray" (only 23 downloads, low confidence).
- **Learned the import workflow** (haven't done third-party before): Sketchfab account → Download
  3D Model → FBX (autoconvert) preferred over glTF (which needs glTFast package) → extract zip →
  drag folder into `Assets/Models/Hospital/` → URP magenta-material gotcha → fix via per-material
  Shader change (`Universal Render Pipeline → Lit`) or bulk via Render Pipeline Converter → drag
  `.fbx` into Hierarchy → attach actual Unity `Light` component to fixture geometry so it emits.

## What broke and how I fixed it
- **Symptom:** attached `SpinnerController` during Play; spinner buttons stopped working but Save/Cancel did.
  - **Cause:** components added at runtime have `Awake` fire *then*. So my `SpinnerController.Start` had already
    re-enabled its boxes' raycasts at scene-load — then `ScreenNavigator.Awake` (added later) ran its
    "turn off every graphic's raycast" pass and clobbered them. Save/Cancel still worked because ScreenNavigator
    itself re-enables those.
  - **Fix:** stop and Play from scratch. With both scripts present at load, `Start` always runs after every
    `Awake` — the order that the controllers depend on. Adding during Play violates that order.
- **Symptom:** Save & Exit / Cancel boxes weren't clickable in Date & Time.
  - **Cause:** each box has a TMP label drawn on top as a flat sibling (not a child). A tap on the letters hits
    the label, the label has no `Button`, and sibling raycasts don't bubble — tap dies on the label.
  - **Fix:** `ScreenNavigator.WireScreen` now globally disables `raycastTarget` on every graphic in a screen
    and only HookButton re-enables it. The label becomes transparent to the ray; the box behind catches the tap
    across its whole face. Carries cleanly to the Temperature Sensor up/down arrows for free.
- **Symptom:** at one point everything broke — nav dead, Save/Cancel dead, default screen wrong.
  - **Cause:** `ScreenNavigator` wasn't attached to `Canvas`. Without it nothing nav-related runs and the scene
    falls back to whatever was active in the editor.
  - **Fix:** reattach. (Also: assume this whenever launch screen looks wrong.)
- **Symptom:** TouchGFX sim build failed with eight `non-virtual-dtor` errors across screens.
  - **Cause:** `Model` declares virtual methods but has no virtual destructor — UB if deleted through a base
    pointer. GCC warns; TouchGFX's Makefile promotes warnings to errors.
  - **Fix:** `virtual ~Model() = default;` in `Model.hpp`.
- **Symptom:** sim then failed on `Screen_1View.cpp`'s `#include "main.h"`.
  - **Cause:** `main.h` is generated by CubeMX for the target build; doesn't exist for the simulator.
  - **Fix:** wrap the include in `#ifndef SIMULATOR`. Verified nothing in the file actually uses anything
    from `main.h` — it was a stale include.
- **Symptom:** sim then failed wholesale on `Model.cpp` — `HAL_*`, `hrtc`, `hi2c4`, `Alarm_State`, `timer1_State`,
  hundreds of unresolved references.
  - **Cause:** the Model is intimate with hardware end-to-end and pulls bare extern globals via `main.h`'s
    include chain. Making it compile on PC would mean stubbing every HAL call and every extern, hours of edits.
  - **Fix:** abandoned the sim. Read the firmware source files directly. Five minutes per screen vs.
    a day of stubbing for a sim that wouldn't actually simulate the behaviour anyway.
- **Symptom:** no free surgical lamp on Sketchfab.
  - **Cause:** treating "OR" as a generic keyword returns the wrong category — domestic ceiling fixtures, not
    medical equipment. Even with the right search terms (`"surgical lamp"`, `"OT light"`, `"operating room light"`,
    `"medical exam light"`), free surgical lamps are rare.
  - **Fix:** Unity `Spotlight` + flattened cylinder primitive as the fixture itself. The visual impact of a
    surgical light is ~90% the light it casts and ~10% the fixture geometry, so a disc with white emissive
    material under a bright spotlight reads as "OR overhead lighting" at eye level. Five minutes of work,
    no asset hunt.

## Decisions made
- **Crossed into Product B deliberately.** Date & Time + Temperature Sensor are stateful now — button
  clicking changes values. Still no physiology, no live device behaviour. The A→B line is "values that
  *react* to anything other than a button" — we haven't crossed *that*.
- **Generic reusable controllers, not screen-specific code.** Each new screen = drop the component +
  list its fields in the Inspector. Date & Time and Temperature Sensor used the *same* `SpinnerController`
  with different field configs — that's the proof the abstraction held.
- **`OnEnable` snapshot + Cancel revert.** Cancel discards this session's edits; Save commits (live values
  stay). Real modal semantics, not just navigation. Cancel listeners are additive across controllers so
  multiple controllers on one screen revert together.
- **Display-only UI principle reinforced.** Nothing raycast-interactive in a screen except what we
  explicitly wire. Disable-all-then-wire pattern makes this enforceable, not just convention.
- **Sim abandoned, source reading wins.** TouchGFX sim was the wrong tool — too much HAL coupling.
  The relevant behaviour for mirroring lives in `*View.cpp` files and is a five-minute read each.
- **Alarm beep: icon tint, not text.** Firmware shows `"Low"/"Medium"/"High"` text; our Unity screen
  has no text element for it. Used three climbing-urgency tints instead. Inspector-configurable per state,
  so adding text later is one line.
- **Hospital room: set-dressing only, curated free models over paid pack.** Better per-asset quality;
  costs only curation time. Patient is body-under-sheet (set dressing), not chest-exposed (demonstrable
  subject).
- **Surgical light as primitive + Unity Spotlight, not a hunted model.** Light cast > fixture geometry
  for selling "this is an OR."
- **Skip leak/piracy sites** (e.g. unityassetcollection.com pushing free downloads of paid Mixall assets).
  Floaid is a real medtech company; pirated assets in a product demo is unacceptable.

## Open questions / next
- **Day 14 first move:** import the six Sketchfab models into `Assets/Models/Hospital/`, scale-check
  next to the Floex trainer pole (Sketchfab models are usually authored in metres but worth confirming),
  then place them.
- Build the surgical-light fixture (disc + spotlight) and position it above where the table will go.
- Sheet drape for the patient (stretched white plane + cloth material from chest up; face never shows).
- Placement strategy: trainer's natural forward arc should contain patient + anesthesia machine +
  surgical light. Bed perpendicular to where the perfusionist stands. IV pole at head, monitor cart
  visible to the (imagined) anesthesiologist position, medical cart in a corner for parallax.
- After the room is done: pump-head screens (`Screen_Main`) — first state-pattern reuse on a screen
  we haven't touched yet.

## Time spent
≈ __ hrs — heavy implementation morning (state controllers + Temperature Sensor), sim detour ate
~30 min before the abandon call, afternoon lighter (planning + Sketchfab curation + import workflow study).