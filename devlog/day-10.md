# Day 10 — HLM VR scaling + finished every console screen (7 new)

## Goal
Get the Floex 3.0 model to real-world size in VR, then clear the rest of the
screen backlog using the day-9 JSON generator. By end of day, every screen in
the nav map should exist plus the one modal that hangs off the clock. Product A
scope unchanged: screens look real and will navigate, all values display-only —
no physiology, no live behaviour, no listeners.

## What I did

### HLM VR scaling
- Got the real machine dimensions from Shiv: **Length 946 mm** (runs left-right),
  **Breadth 610 mm** (front-back), **Height 1200 mm** (full height incl. poles).
- The `Floex_Trainer` FBX imports at Scale Factor 1 with Convert Units (cm→m) on.
  Wrote a throwaway editor script — **Floex → Measure Selected Bounds**
  (`Assets/Editor/MeasureBounds.cs`) — that encapsulates every child
  `Renderer.bounds` and logs the world size. Measured: **X 0.794, Y 0.843,
  Z 0.477 m**.
- Mapping real→model: 946→X, 1200→Y, 610→Z. The per-axis scale factors came out
  **unequal** (~1.19 / 1.42 / 1.28), which means the model isn't built to exact
  real-world proportion.
- Tried both: **non-uniform** scale (each axis stretched to hit the real mm) vs
  **uniform** scale (one factor, proportions preserved). Non-uniform looked better
  in the headset at first glance, but went with **uniform** — cleaner long-term,
  and non-uniform would distort anything later parented to the model (screens,
  colliders) along the stretched axes. Exact final scale + machine position parked
  for later; the decision is what mattered today.
- Known-state note for future me: if non-uniform is ever revisited, the axis
  factors were X×1.191, Y×1.424, Z×1.279 — relevant if screens ever look
  off-aspect against the body.

### Cleared the screen backlog (7 new screens via JSON generator)
All built the same way: read exact coords off the TouchGFX Properties panel,
write one screen-spec JSON, **Floex → Build Screen From JSON…**, eyeball, nudge
numbers in the JSON (never in Unity), rebuild. Generator stays dumb — static
elements only, scope-lock comment at the top of every file.

- **Level Sensor** — simplest of the set: intro line, cyan Arterial swatch, two
  toggle rows (toggles frozen OFF). Kept the source firmware typo "reservior" +
  its double-space verbatim so the trainer matches the real device text.
- **Pressure Sensor** — Shiv built this one by hand; I only fixed the group
  comments and corrected the PS2 title that read "Bubble Sensor 2" → "Pressure
  Sensor 2". Coords left untouched.
- **Temperature Sensor** — 4-row alarm grid (T1–T4), each row = label + Max
  stepper (up/value/down) + Min stepper + Alarm Beep box. 84 elements, the
  densest screen.
- **Timer** — 4 identical timer rows (Timer1–4), each = label + Set Time
  label/box + Elapse Time label + HH:MM:SS box + toggle frozen OFF. Extra button
  vs other screens: **Reset All Timer** (light grey) alongside Save & Exit +
  Cancel. 58 elements.
- **System Setting** — 6 label/value info rows (Total Runtime, Operating Days,
  Software Version, Serial Number, CAN Bus Status, Last Calibration) with static
  values + a **Reset to Factory default** button. 44 elements.
- **Cardioplegia** — last screen in the nav map. Type dropdown (frozen empty),
  First/Normal dose volume value boxes, a 3-row right column of total-volume
  inputs (Total Volume / Blood / Crystalloid, frozen empty with "ml" units),
  Save & Exit + Cancel. 48 elements.
- **Date & Time** — the one screen reached by *clicking* (off the clock) rather
  than from the nav menu, so I built it too — it'll be needed once navigation
  goes in. 6 steppers (Day/Month/Year + Hour/Minute/Second), each = green up-btn
  + value box + blue down-btn, all static. Save & Exit + CANCEL. **No nav menu**
  on this one (it's a modal). 52 elements.

That completes the full set: CDM, BSA, Bubble, Level, Pressure, Temperature,
Timer, System Setting, Cardioplegia + the Date & Time modal.

## Things I learned / decided
- **The nav menu is reused verbatim across every screen** — identical coords,
  copied block-for-block, only the "active" item differs. Pulled it straight from
  Screen_BSA each time. Worth factoring into a generator include later if it ever
  needs editing in one place.
- **Read what the inspector actually shows; infer the rest by pitch — and flag it.**
  On the dense repeating screens (Timer rows, System Setting rows, Date & Time
  steppers) I read one or two anchor rows off TouchGFX and propagated the others
  by a uniform row pitch rather than clicking every element. Faster, but the
  inferred rows can need a small uniform Y nudge. Logged each one as a flag so
  Shiv knows exactly which numbers are read vs estimated.
- **Match design intent over sloppy TouchGFX widths** (same instinct as day 9):
  e.g. collapsed multi-element splits into single strings, normalised stepper
  button boxes to a clean 60×60 + 50×50 arrow inset.
- **Caught my own canvas-space bug on Date & Time.** First pass put the bottom
  buttons at Y≈963 on an 800-tall canvas — off-screen. Re-read the screenshot
  layout and pulled them back to ~628 before handing the JSON over. Reminder that
  estimated Ys need a sanity check against the canvas height, not just against
  each other.

## Open questions / next
- **Rebuild + eyeball pass.** Several screens have flagged estimated Ys to verify
  in Unity, then push the corrected numbers back into the JSON (not nudge in
  Unity): Timer row pitch + Reset-All-Timer fill colour; System Setting row pitch;
  Cardioplegia lower rows (Normal Dose Volume, TVB/TVC); Date & Time Year/Second
  rows + button row + down-button colour.
- **Navigation.** Every screen now exists and is static. Wiring them together
  (nav menu items + home + Save/Cancel actually changing the active screen) is the
  next deliberate step — still outside the generator, still scope-aware.
- **Down-button sprite on Date & Time.** Used a colour box as a stand-in for the
  blue down arrow; swap for the real `tiny_fill` / keyboard_arrow_down button
  art if we want it pixel-exact.
- **Housekeeping carried over from day 9**: `Floaid.lnk` still needs to be in
  `.gitignore`; verify PNGs are LFS pointers (`git lfs status`) before the next
  push; commit today's 7 screens + MeasureBounds.cs.
- **Scale finalisation**: uniform factor + machine world position still parked —
  re-judge player spawn and screen legibility once the model is locked in place.
- **Colleague coordination** still pending on pump-head screens (newer UI than the
  7-PDF set, different surface).

## Time spent
Full day. Scaling + the measure-bounds detour ~1.5h; the 7 screens went fast on
the generator (~30–45 min each for the dense ones, less for Level/Pressure since
one was trivial and one was Shiv's hand-build I only touched up).