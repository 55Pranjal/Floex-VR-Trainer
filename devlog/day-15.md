# Day 15 — VR Keyboard Plumbing for BSA

## Goal

Wire `OVRVirtualKeyboard` from Meta XR SDK v74 to BSA input fields so
perfusionists can type patient data in VR. First step toward closing out BSA at
Product B level; pump-head screens deferred to Day 16+.

## Starting state

`BSAFormController` runtime-attaches `TMP_InputField` to 9 Boxes on the BSA
screen. Tested via desktop keyboard in Unity editor on Day 14 — fields work,
Calculate computes BSA, Cancel reverts via snapshot. No VR keyboard yet.

Two `OVRVirtualKeyboard` prefab variants found in v74 SDK:

- Regular `OVRVirtualKeyboard`
- `OVRVirtualKeyboardBuildingBlock` — supposed to auto-wire to a Meta Building
  Block camera rig

Started by dragging `OVRVirtualKeyboardBuildingBlock` into the scene as a
sibling of `[BuildingBlock] Camera Rig`, and attached
`OVRVirtualKeyboardInputFieldTextHandler` to `Box_Name` as the sanity-test
field.

## What I expected

BuildingBlock variant auto-wires controller/hand inputs from the rig. Tap
`Box_Name` → keyboard appears → type → text lands in field.

## What actually happened (initial Play test)

- Hover tint on fields worked → raycasts reaching the canvas.
- Tap on `Box_Name` → field focused, caret showed.
- **No keyboard appeared.**

Inspector inspection: all input source slots on `OVRVirtualKeyboardBuildingBlock`
empty (no text handler, no controller transforms, no hand references, no
raycasters). BuildingBlock auto-wire did not happen despite the prefab name.

## Diagnosis sequence

### Issue 1: `Virtual Keyboard Support` was `None` in OVR Manager

Confirmed root cause for the keyboard not appearing at all: the Quest Features
→ General → `Virtual Keyboard Support` flag on the rig's `OVR Manager` was set
to **None**. This is a manifest-level capability — without it, the Meta Quest
runtime can't initialise the virtual keyboard service. No amount of inspector
wiring would make the keyboard appear with this flag off.

**Fix:** Changed `Virtual Keyboard Support` from None → **Supported**. This
serialises into the `AndroidManifest.xml` and requires a real APK build to take
effect — Quest Link / Play mode does not necessarily honour it.

### Issue 2: NullReferenceException on every tap (not what I first thought)

Console flooded with:

```
NullReferenceException at TMP_InputField.MouseDragOutsideRect
  → ScreenPointToWorldPointInRectangle (cam: null)
```

Initial hypothesis: Canvas `Event Camera` slot was empty. Fix: dragged
`CenterEyeAnchor` into the Canvas's `Event Camera` slot. **Necessary but not
sufficient** — NullRef persisted.

Real root cause: `TMP_InputField.MouseDragOutsideRect` uses
`eventData.pressEventCamera` (the camera written to PointerEventData by the
input module), not the Canvas's Event Camera. Meta's `PointableCanvasModule`
does not reliably populate `pressEventCamera` for World Space canvases. This is
a known noisy interaction between Meta Interaction SDK and `TMP_InputField`. It
fires on every tap (because a tap is a degenerate zero-distance drag), but only
breaks text-selection-by-dragging — which BSA doesn't need.
**Decision: accept as console noise, not blocking.**

Canvas `Event Camera` fix retained — still correct, still needed.

### Issue 3: Controller transforms not wired

Although the BuildingBlock variant should auto-wire, all slots were empty.
Inspected the `[BuildingBlock] Camera Rig` hierarchy: classic OVR rig structure
with `TrackingSpace / LeftHandAnchor / LeftControllerAnchor` and the right-hand
equivalent. Wired manually:

- Left Controller Root Transform ← `LeftControllerAnchor`
- Left Controller Direct Transform ← `LeftControllerAnchor`
- Right Controller Root Transform ← `RightControllerAnchor`
- Right Controller Direct Transform ← `RightControllerAnchor`

Controller Raycaster + Hand Raycaster slots left empty (optional, not
required).

### Issue 4: Hand slots intentionally left empty

`Hand Left` / `Hand Right` slots expect the legacy `OVRHand` MonoBehaviour.
This scene uses the new Meta Interaction SDK Comprehensive setup: hands are
represented as `OVRLeftHandDataSource` / `OVRRightHandDataSource` under
`OVRInteractionComprehensive/OVRHands` — different architecture, not
interchangeable with classic `OVRHand`. Forcing it would mean retrofitting old
hand-tracking on top of the new stack. Since the controllers-vs-hands decision
is still deferred, left these slots empty for now. Controllers alone enough to
test keyboard.

### Issue 5: Meta's stock TextHandler has no TMP support

Read `OVRVirtualKeyboardInputFieldTextHandler.cs` source:

```csharp
using UnityEngine.UI;
...
[SerializeField] private InputField inputField;
```

That's legacy `UnityEngine.UI.InputField`, not `TMP_InputField`. They don't
inherit from each other. Project-wide search across `com.meta.xr` packages: no
TMP variant exists in the v74 SDK.

This means our entire BSA setup (`BSAFormController` runtime-attaches
`TMP_InputField`) is incompatible with Meta's stock handler without a bridge.

**Decision:** Don't write the TMP bridge yet. Prove the keyboard plumbing
works end-to-end with a legacy `UI.InputField` on `Box_Name` first. If it does,
write the TMP bridge in Day 16 and restore `BSAFormController`. If it doesn't,
we'd be debugging two new things at once — bad.

## Sanity test setup

1. Disabled `BSAFormController` on `Screen_BSA` (so it wouldn't attach runtime
   `TMP_InputField` and conflict).
2. Selected `Box_Name`, Add Component → UI → Input Field (legacy, NOT TMP).
3. Dragged `Box_Name` into the text handler's Input Field slot — accepted
   (type matched).
4. Saved scene.

## Day 15 end state

Scene ready for Build & Run test. No more wiring work. Outcome of test
determines Day 16 path:

- **If keyboard appears + types into legacy InputField on Quest:** write TMP
  bridge, restore `BSAFormController`, extend to all 9 fields, commit, move to
  pump head.
- **If keyboard still doesn't appear or has issues:** deeper Meta SDK debugging
  on Day 16. Consider scope pivot (cyclers/presets instead of typing).

## Open carry-over items

- Pump-head TouchGFX firmware files received from teammate — not yet read. New
  branch `feature/pump-head-screens` planned once BSA closes out.
- `Floaid.lnk` still in git from way back.
- Two `medical_instrument_tray` instances in scene — one should be deleted.
- Real machine dimensions from Shiv still pending.
- Hand-tracking vs controllers decision still deferred — currently controllers
  only.
- Console noise (`MouseDragOutsideRect` NullRef on every tap) accepted as
  cosmetic; could be silenced with a TMP_InputField subclass later if it
  becomes a problem.

## Files touched

- Scene: `OR_Environment.unity` — Quest Features flag, Canvas Event Camera,
  OVRVirtualKeyboardBuildingBlock added + wired, BSAFormController disabled,
  legacy InputField added to Box_Name.
- No code files changed.

Not committed yet — pending sanity test outcome on Day 16.