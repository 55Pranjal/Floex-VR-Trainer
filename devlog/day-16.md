# Day 16 — VR Keyboard Wall, BSA Pivot, CDM Product B, Pump Head Batch 1

## Goal at start of day

Morning: finish the BSA VR keyboard by writing the TMP bridge (per Day 15
plan). Afternoon: open `feature/pump-head-screens` and start the pump-head
work — minimum bar was firmware reading + Pump_Select_1.

## What actually happened

Build & Run from Day 15 keyboard wiring revealed the keyboard plumbing was
correct in part but positioned at world origin (behind the HLM). Iterated
through positioning, focus dispatch, and event routing until we hit a wall
that makes the whole approach non-viable under Meta XR SDK v74. Pivoted BSA
to display-only with realistic clinical defaults, then did CDM Product B
interactivity (sprite toggles + resets, no time counting).

In the afternoon, branched to `feature/pump-head-screens` and got way more
done than planned: all three exclusive pickers + a stub Screen1 + the full
navigation/state/picker architecture, all wired and validated end-to-end
in VR on one pump head canvas. Batch 1 of the pump-head plan is closed.

## Diagnosis sequence (chronological — for future reference)

### 1. Keyboard spawned at origin, not in front of user

`Initial Position: Near` on `OVRVirtualKeyboard` is supposed to spawn the
keyboard ~50cm in front of the user's head when `Show()` fires. It spawned
at world (0,0,0) instead — behind the HLM, unreachable by controller ray.

Tried tagging `CenterEyeAnchor` as `MainCamera` (it already was — common Meta
gotcha ruled out). Switched `Initial Position` from `Near` to **Custom** and
manually positioned the prefab transform in front of the BSA screen. This
worked — keyboard appeared where placed, controller rays reached it.

**Lesson:** Meta's `Near` auto-positioning is unreliable in v74. Use `Custom`
and place manually.

### 2. Keyboard appeared permanently from app start, with no field-focus link

Even with positioning fixed, the keyboard:
- Appeared 2 seconds after app start, regardless of any tap
- Stayed visible across screen navigation
- Showed no response to keystrokes

Root cause: `OVRVirtualKeyboard.TextHandler` slot was `None`. Without a
handler, `IsFocused` was effectively never queried — keyboard had no hide
signal. Wired `Text Handler` slot to `Box_Name`'s
`OVRVirtualKeyboardInputFieldTextHandler` component.

### 3. Wiring complete, still no keystroke or focus response

With everything wired (Text Handler → Box_Name, InputField slot on the text
handler → Box_Name's InputField, Text Component on the InputField → Text
(Legacy) child, Raycast Target on the Text child turned off so rays could
reach the parent Box), tapping Box_Name showed:
- No caret
- No keyboard activation
- No visible hover or click state on Box_Name itself

The Box's Image had Raycast Target enabled, the InputField had its target
graphic set, EventSystem was in the scene with `PointableCanvasModule`.
Cyclers and Buttons elsewhere on the canvas worked fine.

### 4. Verifying with logcat — the wall

Added `InputFieldActivator.cs` (a small component that listens for
`IPointerDownHandler.OnPointerDown` and manually calls
`inputField.ActivateInputField()` — hypothesis was that Meta's
`PointableCanvasModule` dispatches `OnPointerDown` reliably but not
`OnPointerClick`, which is what `UnityEngine.UI.InputField` uses internally).

Added `Debug.Log("Box_Name pointer down")` inside it. Built and ran with Meta
Quest Developer Hub's log viewer open, filtered to "Box_Name".

**Pointed ray at Box_Name, pulled trigger. No log line appeared.**

Pointer events do not reach Box_Name at all. Not a click-vs-down dispatch
issue. Not a missing handler. The raycast hit is silently dropped before
reaching the GameObject's components. Suspect cause: `InputField` (and
`Selectable` in general) interferes with how Meta's `PointableCanvas`
registers raycast targets for non-Button selectables, but we did not confirm
by source-reading — Meta's internal raycast routing is opaque and the
remaining diagnostic paths would have cost another day with no guarantee of
fixing it.

## What stays valid for future work

If text entry is needed again later (Product C or beyond), the following are
already proven correct on this rig:

- **Manifest:** `Virtual Keyboard Support` in OVR Manager → Quest Features →
  General must be set to **Supported**, not None. Manifest-level flag,
  requires APK build to take effect.
- **Canvas Event Camera:** set to `CenterEyeAnchor` on the World Space
  canvas. Necessary for any pointer math on this rig.
- **Controller wiring:** Left/Right Controller Root + Direct Transform slots
  on `OVRVirtualKeyboard` accept `LeftControllerAnchor` and
  `RightControllerAnchor` from the rig's TrackingSpace.
- **Hand slots:** leave empty. The v74 Comprehensive Interaction SDK uses
  `OVRLeftHandDataSource` / `OVRRightHandDataSource`, not the legacy `OVRHand`
  MonoBehaviour the keyboard prefab expects.
- **Initial Position: Custom** — manually position the keyboard prefab
  transform. The `Near` preset is broken or HMD-dependent.
- **Stock TextHandler is legacy-only:** `OVRVirtualKeyboardInputFieldTextHandler`
  uses `UnityEngine.UI.InputField`, not `TMP_InputField`. A TMP bridge would
  be ~80 lines, structurally identical to Meta's handler but typed against
  TMP.

If/when retried: the unsolved problem is specifically getting pointer events
to reach an `InputField` component on Meta's `PointableCanvasModule`. Likely
paths to try:
1. A custom Meta `IPointable` component on each input box that calls
   `inputField.ActivateInputField()` directly, bypassing Unity's event chain
   entirely.
2. A full custom keyboard built from `Button` instances (Buttons are
   confirmed to receive events on this canvas via the cyclers).
3. A `RayInteractable` directly on the box, hooking its `Select` event to
   activation.

Custom keyboard (option 2) is most likely to work, biggest cost. Option 1 is
worth trying first if a Meta SDK upgrade hasn't fixed the underlying issue.

## BSA pivot to display-only

`BSAFormController` rewritten. No more runtime `TMP_InputField` attach. On
screen open it writes realistic clinical defaults to the 9 `Txt_X` elements:

- Name: John Doe
- ID: P-001
- Age: 45, Weight: 75, Height: 180
- Blood: O+
- Surgeon: Dr. Sharma, Anaesthetist: Dr. Patel, Perfusionist: Dr. Kumar

Calculate still works — computes BSA = sqrt(180 x 75 / 3600) ~= 1.94 and
writes it to Txt_BSA. Cardiac Index and Target Flow stay empty (firmware
formulas depend on cardioplegia output data the trainer does not simulate —
Product A scope-lock holds).

Cancel clears the computed fields so a re-tap of Calculate shows fresh
output. Save & Exit and Cancel both still navigate via `ScreenNavigator`.

OK and EXIT buttons still have no functionality — deferred (see "Carry-over"
below).

## CDM Product B closeout

`CDMScreenController.cs` wires 5 sprite toggles + 4 reset buttons on
Screen_CDM:

- **Toggles (5):** Img_LeftPlay, Img_Timer1Play, Img_Timer2Play,
  Img_Timer3Play, Img_Speaker. Each is a 2-state sprite swap (default <->
  toggled). Play sprites toggle to pause; speaker sprite toggles to
  speaker_mute.
- **Resets (4):** Img_LeftHistory, Img_Timer1Reset, Img_Timer2Reset,
  Img_Timer3Reset. Each snaps its paired play button back to the play
  sprite regardless of current state.

Timers do NOT count time — per Product A/B scope, the 00:00:00 displays
stay static. Button taps are familiarisation-only.

Imported two new sprites: `pause.png` and `speaker_mute.png`, matching the
existing snake_case convention used by ScreenBuilder.

Tested in VR. All 5 toggles flip correctly, all 4 resets snap their paired
toggle back. No regressions on existing screens.

## Pump Head Batch 1 — pickers + nav loop on one canvas

Switched to `feature/pump-head-screens`. Goal: get one pump head fully
working before duplicating to the other three. Independent state per pump
head — instance fields, not singletons — so duplication later is just a
prefab copy.

### Canvas setup — PumpHead_01_Canvas

- 800x480 reference resolution (matches firmware Designer canvas)
- World Space, Constant Pixel Size scaler, scale 0.0002 (≈16cm x 9.6cm,
  roughly a 7" touchscreen)
- Position (0.186, 0.41, -1.19), Rotation (45, 0, 0) — sits on the
  leftmost pump head's top surface, tilted toward the operator.
  Placeholder; refine when real machine dimensions arrive.
- Meta Interaction SDK stack matching CDM:
  - **Pointable Canvas** — Canvas slot wired to self
  - **Ray Interactable** — Pointable Element + Surface wired to self
  - **Plane Surface** — Facing Backward, Double Sided checked

Note for future canvas work: a BoxCollider does **not** work for Meta VR
ray hits. Spent ~10 minutes adding one before realizing — Meta's stack is
surface-based via PlaneSurface + RayInteractable, not collider-based.
Documenting in case I forget by Day 18.

### Screen JSONs (4)

All against the existing Screen_CDM.json schema.

- **Pump_Select_1.json** — 5 exclusive options (Arterial/Cardio/Vent/
  Suct1/Suct2) + CANCEL/APPLY + home icon. Reuses one
  `button_rounded_small.png` outline sprite for every rounded button on
  the screen — TouchGFX uses a single outline asset throughout, so we
  do the same.
- **Tube_Size_1.json** — 6 exclusive options (1/4, 3/8, 1/2, 5/16, F1, F2)
  + CANCEL/APPLY. Each button is 3 layered elements: outline sprite +
  inner circle sprite + label. 12 sprites total
  (`tube_outline_1..6.png` + `tube_circle_1..6.png`), numbered 0-5 to
  match the firmware tubeIndex.
- **Direction.json** — 2 options (Forward, Reverse) + CANCEL/APPLY. Each
  direction button is 3 layered elements: `green_border.png` (reused) +
  arrow sprite + text label.
- **Screen_1.json** — STUB. Three nav buttons (PUMP SELECT, TUBE SIZE,
  DIRECTION) + three state-display labels (Pump: --, Tube: --,
  Direction: --) + a footer note. The full Screen1 (LPM/RPM/CI displays,
  START/STOP, bottom bar) is Batch 2 work; this stub is just enough to
  validate the navigation loop.

Header pattern reused across all four: "FloEx 3.0" coral logo top-left +
home icon (`home_border.png` behind `home.png`) top-right.

### C# scripts — three new, two deletions

Cleanup first: `MainScreenDisplay.cs` and `PumpState.cs` deleted. Both
referenced live simulation state (flowRate, rpm, isRunning, cardiacIndex)
that violates the Product A scope-lock. `ClickLogger.cs` also deleted —
debug-only from Day 11, no longer needed.

**PumpHeadState.cs** — instance MonoBehaviour, one per pump head. Mirrors
the firmware Model struct: pumpIndex, tubeIndex, directionForward. Name
lookups for label refresh. Not a singleton — each canvas instance carries
its own copy.

**PumpHeadNavigator.cs** — adapted from CDM's `ScreenNavigator`. Scoped
to one pump head canvas: collects child screens, wires Screen1's three nav
buttons to ShowScreen calls, refreshes Screen1's state labels via
PumpHeadState whenever returning home. Same defensive
`raycastTarget = false` pattern as the CDM navigator.

**ExclusivePickerController.cs** — single controller for all three pickers,
parameterized by `PickerKind` enum (Pump/Tube/Direction). Mirrors the
firmware Presenter's 5-step pattern exactly:

- `OnEnable` → load state from PumpHeadState, highlight current
  (= firmware `activate`)
- Option tapped → set tempSelected, mark changed, re-highlight
  (= `onOptionSelected`)
- APPLY → commit temp to state if changed, navigator.ShowScreen("Screen1")
  (= `onApplyClicked`)
- CANCEL → revert temp to state, navigator.ShowScreen("Screen1")
  (= `onCancelClicked`)
- Highlight = alpha 1.0 on selected, 0.4 on rest (firmware uses 255/100 —
  same ratio in float).

Option names per kind hardcoded in `OptionNames()`. Coupling to JSON
GameObject names is acceptable for now — 3 pickers, ~13 dependencies.

### Sprites imported

`button_rounded_small.png`, `home.png`, `home_border.png`,
`tube_outline_1..6.png` (6), `tube_circle_1..6.png` (6),
`green_border.png`, `direction_forward.png`, `direction_reverse.png`.
All snake_case, matching ScreenBuilder convention.

### Bugs hit, lessons

- **Canvas nested under Floex_Trainer hid the Render Mode dropdown.**
  "Pixel Perfect: Inherit" was the signal — child canvases inherit render
  mode from their parent canvas. Moving PumpHead_01_Canvas to scene root
  exposed it; set to World Space.
- **Scale 0.002 vs 0.0002.** One decimal apart, 10x size difference.
  First attempt was 0.002, screen was bigger than the whole HLM.
- **Label-on-button click swallowing.** Initial ExclusivePickerController
  didn't disable raycastTarget on child labels/icons. Clicks landed on
  the label's TMP component (no Button), got dropped. Fixed by mirroring
  the CDM navigator's "turn everything off, HookButton re-enables per
  element" pattern.

### End-to-end validation

Tested in VR with controllers, leftmost pump head only:
- Walk to pump head, see Screen1 with default state
- Tap PUMP SELECT → picker appears, current selection highlighted
- Tap CARDIOPLEGIA → highlight moves
- Tap APPLY → return to Screen1, "Pump: Cardio" now showing
- Same for tube size and direction
- CANCEL on any picker correctly reverts and navigates back

Every button works except the home icon header — deferred (no Screen_Main
exists yet to navigate to). Same display-only deferral pattern as CDM's
Img_LeftAlarm and Img_RightDownload.

## Carry-over to Day 17+

- **Batch 2 of pump-head:** full Screen1 (LPM/RPM/CI/current/torque/voltage,
  START/STOP, bottom bar, fixed nav panel replacing slide menus) + Screen2_1
  Master-Slave + Screen3 Pulse Mode + Screen4 Fine Calibration. Open
  question: START/STOP buttons stay 0.00 (Option A) or toggle to typical
  running values like 4.50 L/PM, 180 RPM (Option B)? B is more educational
  without crossing into simulation — just a static two-state toggle.
  Decide before writing Screen1 JSON.
- **Batch 3 of pump-head:** Screen5 Diagnostics + duplicate
  PumpHead_01_Canvas prefab to the other 3 pump heads + double pump-head
  3D model (teammate sending).
- **Home button on all 4 pump head screens display-only** until Screen_Main
  exists. Same pattern as CDM's Img_LeftAlarm + Img_RightDownload.
- **Real machine dimensions still pending** from Shiv — current scale/
  position eyeballed. Block to Batch 3 duplication.
- **`tube_circle_3.png` renders white/gray, not green** in Unity. Visual
  issue, low priority — check sprite import settings or the source PNG.
- BSA OK and EXIT buttons still unwired (Day 15-16 morning carry-over).
- Screen_Main has no interactivity yet — the nav buttons jumping to each
  subscreen are not wired.
- CDM JSON has Img_LeftAlarm and Img_RightDownload still display-only —
  firmware has Low/Medium/High buzzer handlers but those UI buttons aren't
  on this CDM screen layout; Download has no clean no-physiology meaning.
  Flagged for Product C consideration.
- CDM JSON has Box_P1 + Box_P2 but firmware only has one `updatePressure`
  handler — either firmware to be extended or P2 is a visual-only sensor.
  Worth flagging to Hashir next sync.
- 16KB-aligned warning on `libUnityOpenXR.so` (Android 15 compat) — Unity
  fix in a future patch, nothing to do from our side.
- Two `medical_instrument_tray` instances in scene, one should be deleted.

## Files touched today

Morning (BSA + CDM, on `feature/console-screens` branch — committed
earlier in day):

- `Assets/Scripts/BSAFormController.cs` — full rewrite (display-only mode)
- `Assets/Scripts/CDMScreenController.cs` — new file
- `Assets/Sprites/pause.png` — new asset
- `Assets/Sprites/speaker_mute.png` — new asset
- Scene `OR_Environment.unity` — BSA InputField + keyboard scaffolding
  removed, CDMScreenController added to Screen_CDM with sprite refs in
  Inspector
- `Assets/Scripts/InputFieldActivator.cs` — created and then deleted
  during pivot; not in commit

Afternoon/evening (Pump Head Batch 1, on `feature/pump-head-screens`):

- `Assets/ScreenSpecs/Pump_Select_1.json` — new
- `Assets/ScreenSpecs/Tube_Size_1.json` — new
- `Assets/ScreenSpecs/Direction.json` — new
- `Assets/ScreenSpecs/Screen_1.json` — new (stub)
- `Assets/Scripts/PumpHeadState.cs` — new
- `Assets/Scripts/PumpHeadNavigator.cs` — new
- `Assets/Scripts/ExclusivePickerController.cs` — new
- `Assets/Scripts/MainScreenDisplay.cs` — deleted (deprecated)
- `Assets/Scripts/PumpState.cs` — deleted (deprecated)
- `Assets/Scripts/ClickLogger.cs` — deleted (Day 11 debug-only)
- `Assets/Textures/UI/` — 17 new sprites (see lists above)
- Scene `OR_Environment.unity` — PumpHead_01_Canvas added at scene root
  with full Meta Interaction component stack