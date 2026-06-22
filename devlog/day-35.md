# Day 35 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 22 Jun 2026
*(Days 33–34 skipped — no project work.)*

---

## Goal

Finish the spatial audio task, complete the remaining Phase 3A polish, clear the Phase 3A exit demo with Shiv, then begin Phase 3B physiology (Week 4 foundation).

## What I did

**Finished pump audio across all 4 heads.** Unity built-in 3D audio (Spatial Blend 1.0) rather than the Meta XR Audio SDK — avoids a new pinned-version package dependency on the v74 stack, convincing enough for 3A (HRTF later if clinical feedback wants it). Both clips self-generated (clean provenance), Force-To-Mono.
- `PumpAudio.cs` on each single head: plays the looped hum while running, pitch-coupled to RPM (Shiv-approved coupling).
- `DoublePumpAudio.cs` on the double head: one hum for the unit, plays if either pump runs, pitch tracks the higher RPM of the running pumps.
- Initial AudioSource-only setup made no sound (AudioSource is just the speaker; the driver script was missing) — added the scripts, assigned each to the same state the rotor uses.

**Fixed two audio behaviours after testing.**
- Hum played at 0 RPM even though the rotor is still — gated audio on `running && rpmSetpoint > 0` to match the rotor (running at 0 = still + silent).
- Pitch swing too wide ("accelerating car") — narrowed the pitch range so it reads as a pump holding a steadier tone. Same values across all 4 heads.

**72fps profiling — clean pass.** Built to Quest 3 (on-device, not Link), OVR Metrics Tool: **constant 72fps** with audio + rotation under load. Dynamic Resolution not enabled (couldn't find it = not active), so the number is honest. Measured baseline to hold against when physiology load arrives.

**Regression sweep + walkthrough + Phase 3A exit demo — all done.** Full VR sweep (5 canvases poke+ray, START/STOP per pump, navigation, knob caps on all 4 heads, audio) passed. Recorded the walkthrough. **Phase 3A exit demo to Shiv — go-ahead to proceed to Phase 3B.**

**Scope: physiology build greenlit with dev-seat authority.** Shiv approved building physiology and delegated per-coupling build decisions to the dev seat (Pranjal) for the current phase — individual couplings no longer need a fresh sign-off. Approved couplings to date: rotor speed tracks RPM (Day 29), pump hum pitch tracks RPM (Day 35). Updated CLAUDE.md scope section + added a memory note to carry across sessions. Overall scope / new-phase direction still belongs to Shiv.

**Phase 3B Week 4 — PatientState foundation built.** Started the physiology engine's foundation (pure data + clock, no inter-variable math yet — that's Week 6 with KRB).
- `PatientState.cs` — pure C# (`namespace Floex.Physiology`, no UnityEngine, no MonoBehaviour). The 12 variables with documented units + resting-adult placeholder defaults; `Tick(dtSeconds)` advances time-on-bypass only when `OnBypass`. No coupling between variables yet (locked in by a guard test).
- `PatientStateDriver.cs` — thin Unity MonoBehaviour bridge; owns a PatientState and ticks it at a fixed 50ms (20 Hz) via a frame-rate-independent accumulator, so physiology runs steady regardless of the 72fps render. The only place Unity time touches the class.
- `PatientStateTests.cs` — 8 NUnit EditMode tests (defaults, time advances only on bypass, 20×50ms = 1s, negative-dt throws, Tick mutates nothing but time, reset, clock format). All passing.
- Placed `PatientSim` GameObject with `PatientStateDriver` in the scene; runs clean (no console errors). Driver heartbeat confirmed in-scene.

**Clarified the on-screen timers (ruled out a wrong build).** Considered showing `TimeOnBypass` as a screen readout (original "Option A"). Team confirmed there is NO separate patient bypass timer on the real machine. The 4 main-screen timers are user stopwatches: 1 for cardioplegia + 3 general-purpose the perfusionist starts/stops manually. So `PatientState.TimeOnBypass` stays an INTERNAL sim value — not wired to any screen element and NOT connected to the stopwatch timers (which are independent user tools). Avoided building a control the real Floex doesn't have.

## What broke and how I fixed it

- **No audio at all** — missing driver script (AudioSource alone does nothing with Play On Awake off). Adding `PumpAudio`/`DoublePumpAudio` + assigning `state` fixed it. (Ruled out: editor mute, AudioListener (on CenterEyeAnchor, correct), clip integrity, world scale.)
- **Audio/rotor mismatch at 0 RPM** + **over-wide pitch** — fixed via the gate + pitch-range tuning.
- **Test Runner showed "No tests to show"** despite no compile errors and the Floex.Physiology reference present — the hand-formatted `Tests.asmdef` wasn't being recognised as a test assembly. Fix: deleted it, re-created via Test Runner's "Create EditMode Test Assembly Folder" (correct flags for this Unity version), re-added the Floex.Physiology reference via Inspector.
- **1 test failed** (`TimeOnBypassClock_FormatsMmSs`) — float accumulation drift from 1500× `Tick(0.05)` landed time at 74.999…s → "01:14". Fixed the test to use a single `Tick(75.0)` (tests formatting without lossy accumulation). All 8 green. Noted for Week 6: long-run float accrual; if exact time ever matters, count integer ticks ×period rather than summing floats.

## Decisions

- **Unity built-in 3D audio, not Meta XR Audio SDK** — no new pinned-version dependency.
- **Audio gated on `running && rpm > 0`** to match rotor behaviour.
- **Alarm (blink + beep) + bypass-toggle visible reaction DEFERRED to the physiology phase** — what *fires* them is clinical/sensor logic (Hashir's alarm spec / Phase 3B sensor state); building the mechanism now would hardcode triggers we'd redo. Beep AudioSource stays placed but unwired.
- **PatientState = pure C#, Unity-free**; the 50ms tick lives only in the driver. Week 4 = container + clock, zero inter-variable physiology.
- **No patient-bypass screen readout** — the real machine has none; main-screen timers are independent user stopwatches.
- **Phase 3A cleared**; Phase 3B Week-4 foundation complete.

## Open questions / next

- **Tomorrow: build the control->state->display bridge** (RPM knobs as INPUTS to PatientState; pump-flow/pressure readouts as OUTPUTS) — still NO inter-variable math, just plumbing. This is dev-domain, no KRB needed, and sets up Week 6 cleanly. Week 6 (the actual physiology equations) is KRB-gated — pick that up after the bridge, with KRB.
- **Gating dependency:** KRB weekly availability for the Week 6 math. Plan is to bring KRB in when the equations start (team agreed clinical consultancy comes when needed).
- **Spec question for Shiv/engineer (not blocking):** confirm exactly what each main-screen timer counts, for when display wiring matters.
- Deferred-to-physiology: alarm blink + beep, bypass-toggle reaction.
- (Carry) audio pitch *character* — clinical-realism detail worth a KRB ear; non-blocking.
- Resting default values in PatientState are clinical placeholders — KRB to validate before any physiology math reads them.

## Time spent

~

## Files modified today

- `Assets/Scripts/PumpAudio.cs` — new (single pump hum, RPM pitch-coupled, gated on rpm>0)
- `Assets/Scripts/DoublePumpAudio.cs` — new (double pump hum, higher-of-A/B pitch)
- `Assets/Scripts/Physiology/PatientState.cs` — new (pure C#, 12 vars + 50ms-tickable clock)
- `Assets/Scripts/Physiology/Floex.Physiology.asmdef` — new (physiology assembly)
- `Assets/Scripts/PatientStateDriver.cs` — new (Unity bridge, 20 Hz fixed-step tick)
- `Assets/Tests/Tests.asmdef` — recreated as proper EditMode test assembly, references Floex.Physiology
- `Assets/Tests/PatientStateTests.cs` — new (8 NUnit tests, all passing)
- `Assets/Audio/` — `pump_loop.wav` + alarm beep clip (alarm clip placed, unwired)
- `Assets/Scenes/OR_Environment.unity` — AudioSources on all 4 heads + alarm source on pole; audio scripts wired; `PatientSim` GameObject with PatientStateDriver added
- `CLAUDE.md` — physiology greenlit / dev-seat authority; current state → Day 35
- `devlog/day-35.md` — new