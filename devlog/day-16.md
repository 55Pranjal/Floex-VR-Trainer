# Day 16 — VR Keyboard Wall, BSA Pivot, CDM Product B

## Goal at start of day

Finish the BSA VR keyboard by writing the TMP bridge (per Day 15 plan), then
move to pump-head screens.

## What actually happened

Build & Run from Day 15 keyboard wiring revealed the keyboard plumbing was
correct in part but positioned at world origin (behind the HLM). Iterated
through positioning, focus dispatch, and event routing until we hit a wall
that makes the whole approach non-viable under Meta XR SDK v74. Pivoted BSA
to display-only with realistic clinical defaults, then did CDM Product B
interactivity (sprite toggles + resets, no time counting).

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

## Carry-over to Day 17+

- BSA OK and EXIT buttons still unwired — Pranjal will determine navigation
  destinations from BSA firmware files (not in this session's set).
- Screen_Main (home screen) has no interactivity yet — the nav buttons that
  jump to each subscreen are not wired.
- Pump-head TouchGFX files received today — `feature/pump-head-screens`
  branch to open after this commit lands.
- CDM JSON has Img_LeftAlarm and Img_RightDownload still display-only —
  firmware has Low/Medium/High buzzer handlers but those UI buttons aren't
  on this CDM screen layout; Download has no clean no-physiology meaning.
  Flagged for Product C consideration.
- CDM JSON has Box_P1 + Box_P2 but firmware only has one `updatePressure`
  handler — either firmware to be extended or P2 is a visual-only sensor.
  Worth flagging to Hashir next sync.
- 16KB-aligned warning on `libUnityOpenXR.so` (Android 15 compat) — Unity
  fix in a future patch release, nothing to do from our side.

## Files touched today

- `Assets/Scripts/BSAFormController.cs` — full rewrite (display-only mode)
- `Assets/Scripts/CDMScreenController.cs` — new file
- `Assets/Sprites/pause.png` — new asset
- `Assets/Sprites/speaker_mute.png` — new asset
- Scene `OR_Environment.unity` — BSA InputField + keyboard scaffolding
  removed, CDMScreenController added to Screen_CDM with sprite refs in
  Inspector
- `Assets/Scripts/InputFieldActivator.cs` — created and then deleted during
  pivot; not in final commit