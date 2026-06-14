# Day 27 Devlog — Floex VR Trainer

**Branch:** `feature/touch-screen`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 14 Jun 2026

---

## Goal

Quick polish pass on the main pole (ConsoleMount / Screen_CDM) before Shiv takes the Quest 3 to the IISc Bangalore Art Park presentation tomorrow, where he may get a chance to demo the trainer. Working on `feature/touch-screen` (everything through direct-touch, no RPM knob — the knob lives on `feature/pump-head-motor-rotation` and isn't presentation-ready yet). Two specific gaps to close:

1. Screen_CDM timers swap the play/pause sprite but don't actually count — make them count like the pump-head Screen1 timers.
2. The OK and EXIT buttons on the BSA screen are dead — make them functional (navigate home, EXIT also clears the computed value).

## What I did

**CDM timers now count.** Reworked `CDMScreenController` from sprite-toggle-only to proper timers. The four play buttons (`Img_LeftPlay`, `Img_Timer1/2/3Play`) toggle run/pause: tapping starts the paired `hh:mm:ss` display counting up and swaps to the pause glyph, tapping again pauses, and the reset button (`Img_LeftHistory`, `Img_TimerNReset`) zeroes the time and restores the play sprite. Counters tick in `Update` only while running. Timer state is local to the controller — there's no shared state object for the pole screen (unlike the pump heads, which have PumpHeadState), and standing one up the night before a demo wasn't worth the risk. The speaker/mute stays a plain 2-state sprite toggle. Mirrors the Screen1 timer pattern (play toggles running, reset zeroes, Update renders).

**BSA OK / EXIT wired.** ScreenNavigator wires `Box_SaveExit` and `Box_Cancel` (both navigate to Screen_CDM) but never touched `Box_OK` / `Box_Exit` — so those two were dead. Added wiring in `BSAFormController`:
- **OK** = keep current values, navigate home (same effect as Save & Exit).
- **EXIT** = clear the computed BSA field, then navigate home (same effect as Cancel).
The controller grabs the ScreenNavigator via `GetComponentInParent` and calls its public `ShowScreen("Screen_CDM")`. The existing Save&Exit / Cancel buttons are untouched.

## What broke and how I fixed it

Nothing broke — both were additive. The main thing to watch was Unity execution order, already handled the same way the rest of the screen controllers do it: ScreenNavigator disables raycast on every graphic in `Awake`, and these controllers re-enable raycast on the specific elements they wire in `Start` (which always runs after Awake), so the re-enable can't be clobbered. OK/EXIT and the timer buttons follow that proven pattern.

## Decisions

- **CDM timer state kept local to the controller** rather than introducing a pole-screen state object. Less surface area, no new wiring, lower risk before a travelling demo. Can be refactored into a shared state later if the pole screen grows.
- **OK == Save & Exit, EXIT == Cancel.** Since the form fields are display-only (the InputField/keyboard limitation from Days 15-16 is unresolved), "save" has nothing to persist — both pairs effectively just navigate home, with the cancel-style ones also clearing the computed BSA value. Kept the behaviour consistent across both pairs so the screen behaves predictably in a demo.
- **Stayed on `feature/touch-screen`, RPM knob excluded.** The knob works functionally but the feel isn't polished; deliberately keeping it out of the presentation build.

## Open questions / next

- **Inspector wiring for CDM timers** — the component's array layout changed (was `toggles` + `resets`, now `timers` + `toggles`), so the four timers each need their Play/Pause sprites re-dragged in the Inspector. If left empty the timer still counts but the glyph won't swap.
- **BSA keyboard text entry** still unresolved (Meta SDK v74 + PointableCanvasModule doesn't deliver pointer events to InputFields). Fields remain display-only. Tracked since Day 16.
- **RPM knob feel** — polish item on the other branch, not blocking.
- Post-demo: get Shiv's feedback from the Art Park showing, fold into priorities.

## Demo-readiness checklist (for Shiv's trip)

- [x] APK builds and runs standalone on Quest 3 (Pranjal verified)
- [ ] CDM timer sprites assigned in Inspector (all 4)
- [ ] OK/EXIT verified responsive in VR
- [ ] Final regression: all 5 canvases poke + ray, pump heads START/STOP, screen navigation

## Time spent

~2 hours (CDM timer rework ~1h, BSA OK/EXIT ~30min, build + smoke test ~30min).

## Files modified today

- `Assets/Scripts/CDMScreenController.cs` — reworked to counting timers (4) + plain toggles; ticks in Update
- `Assets/Scripts/BSAFormController.cs` — wired Box_OK (save+home) and Box_Exit (clear+home) via ScreenNavigator
- `Assets/Settings/URP-HighFidelity.asset`, `ProjectSettings/QualitySettings.asset` — pre-existing uncommitted quality-setting changes (part of the verified demo build; left as-is)
- `devlog/day-27.md` — new