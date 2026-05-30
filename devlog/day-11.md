# Dev Log — Day 11 (2026-05-29)

## Goal
1. Wire screen-to-screen navigation — the nav-menu rows and the home icon — across the built screens.
2. Do it without touching the JSON-driven screen generator; navigation lives as its own layer.

## What I actually did
- Confirmed the layout: one World Space `Canvas` under `ConsoleMount`, every screen a sibling
  `Screen_*` GameObject beneath it. So navigation is a SetActive swap — show one, hide the rest.
  No scene loads, no per-screen canvases.
- Diagnosed the real blocker before writing any nav code: **the VR pointer → uGUI bridge didn't
  exist.** The grabbable cube worked (grab interactor on a collider) and mouse-clicking the canvas
  worked (StandaloneInputModule + GraphicRaycaster), but neither drives a uGUI Button from the
  controller ray.
- Built the bridge on the `Canvas`:
  - `PointableCanvas` (and assigned its Canvas slot — it defaults to None).
  - `RayInteractable` + a `PlaneSurface` for the ray to intersect.
  - Event Camera → `CenterEyeAnchor` (cleared the World Space "no event camera" warning).
  - `PointableCanvasModule` on the EventSystem, as the top input module.
- Proved the whole chain with a throwaway uGUI Button + a 7-line `ClickLogger`:
  ray lands → hover → `onClick` → `Debug.Log`. Confirmed in-headset.
- Wrote **`ScreenNavigator.cs`** (one component on the `Canvas`): collects every `Screen_*`, finds
  each screen's *own* nav rows + home icon by name, adds Buttons at runtime, and wires each to
  `ShowScreen()` (SetActive the target, deactivate the rest). Generator and JSON untouched. Sets a
  coral hover tint so rows read as tappable, launches on `Screen_CDM`, and forces exactly one screen
  active — which also cleared the existing mess where several screens were live and overlapping.
- Confirmed full navigation in VR: every nav screen reachable, home returns from anywhere.
- Cleanup: deleted `Screen_CDM_Manual` (the old hand-built CDM leftover).

## What broke and how I fixed it
- **Symptom:** `RayInteractable`'s Surface field wouldn't accept the Canvas.
  - **Cause:** Surface takes an `ISurface` — a geometric plane to intersect the ray against — not a Canvas / UI object.
  - **Fix:** added a `PlaneSurface` component and assigned that. Ticked Double Sided so facing direction didn't matter for the test.
- **Symptom:** the test button's click log was nowhere in the Console.
  - **Cause:** buried under ~180 lines of `[OVRPlugin]` / `[MetaXRFeature]` session spam.
  - **Fix:** Console search box → `click`. The five "VR click landed" lines had been there the whole time — the chain worked first try.
- **Symptom:** test button showed no hover highlight; looked like the bridge had failed.
  - **Cause:** default Button Normal (255) vs Highlighted (245) is imperceptible, worse in a headset. Not a bug — the press tint (grey 200) *was* visible, which proves hover fired too.
  - **Fix:** none needed. `ScreenNavigator` sets a bold coral highlight so the real rows give visible feedback.

## Decisions made
- **Navigation is in scope.** Product A = screens look real and navigate between each other; only the
  *values* are display-only. Wiring screen-swaps is the "navigate" clause, not behaviour — not scope creep.
- **Generator and JSON stay static** (no listeners, no scripts). All navigation lives in one runtime
  script on the Canvas, wiring elements found by name. JSON stays the source of truth; navigation was
  never a visual property of a screen.
- **One World Space Canvas + SetActive swap**, not scene loads and not a canvas per screen.
- **Display-only UI principle:** nothing is raycast-interactive except what navigation explicitly wires.

## Open questions / next
- Making the whole console interactable
