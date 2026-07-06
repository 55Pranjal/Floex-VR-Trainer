# Day 49

## Goal

Diagnose the tutorial's flow-matching validation (mission-passed firing when it
shouldn't) and get the full familiarization tutorial verified end-to-end.

## What I did

- Traced the false-pass in the tutorial validation to its root cause.
- Ran the complete tutorial flow end-to-end (invite -> BSA -> calculate target ->
  power a head -> set arterial/forward/tube -> START -> match flow -> DONE ->
  MISSION PASSED -> RESTART) on-device and confirmed it works correctly, including
  the failure clues.

## What broke and how I fixed it

- Tutorial reported MISSION PASSED when flow was nowhere near target (e.g. 0.8 vs
  ~4.8). Root cause was NOT the code — the TutorialController's flowToleranceLpm
  is a public serialized field, and the component instance in OR_Environment had a
  stale Inspector value of 10 carried over from when it was first added. The script
  default had been corrected to 0.1, but the serialized Inspector value wins over
  the code default, so both Link and the APK read the same wrong 10. With tol=10 the
  lower bound (target - 10) went negative, so any flow >= 0 passed. This also
  explained the step-6 body text still showing "within 10" — that string is
  generated from the same field, so one wrong value produced both symptoms.
  Fix: set Flow Tolerance Lpm = 0.1 on the component in the Inspector, saved the
  scene. No rebuild or cache clear needed.
- (Earlier suspected a stale APK / build cache and started down that path — ruled
  out. Reproduced identically on Link, which pointed back at scene-serialized state
  rather than the binary.)

## Decisions

- For any public serialized field, the Inspector value is the source of truth, not
  the script default. Changing a code default does nothing to existing scene
  instances — check the component first when a "changed" default doesn't take.
- Correcting the Day-44 record: the tutorial "false pass" was a stale Inspector
  override, not a code/logic bug. The validation logic itself was correct.

## Open questions / next

- DPH direction (CW/CCW) -> physical push-direction mapping still needs Hashir/CAD
  before DPH direction can be graded.
- Scored evaluation tolerance (vs the 0.1 teaching band) still KRB's call.
- Tube-size correctness still taught but not validated (any capable tube accepted).
- Watch CanvasClickBypass edge-state fix over coming sessions (intermittent wedge,
  hard to prove fully gone from one clean pass).

## Time spent

~ (fill in)
