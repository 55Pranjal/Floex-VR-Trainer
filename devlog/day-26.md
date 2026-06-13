# Day 26 Devlog — Floex VR Trainer

**Branch:** `feature/pump-head-motor-rotation`
**Repo:** github.com/55Pranjal/Floex-VR-Trainer
**Date:** 13 Jun 2026

---

## Goal

Add RPM adjustment via the physical knob on the single pump head. Turning the knob should change the displayed RPM setpoint (0–250, 1 RPM resolution). Hashir confirmed the knob's only function on the real Floex is RPM, and that the double pump head has two separate knobs (one per pump) — so no "selected pump" logic is needed; deferred slot 4 until the second knob is modelled.

**Scope line held:** the knob changes the *displayed* RPM number only. Rotor visual speed stays fixed at 600°/s regardless of setpoint. This is a UI input, not a simulation input.

## What I did

**State + display (Steps 1–2).** Added `int rpmSetpoint` to `PumpHeadState`. Rewired `Screen1Controller` so the RPM readout is no longer part of the two-state running/stopped block — it now reads `state.rpmSetpoint` live in `OnEnable` and `Update` via a dedicated `UpdateRpmDisplay()`. RPM now shows the setpoint whether the pump is running or stopped (you dial in the target, then START), which also removed the old behaviour of START jumping RPM to a hardcoded 180. Verified in editor by dragging rpmSetpoint in the Inspector and watching the readout track.

**Knob geometry reimport (Step 3).** The existing `KNOB_ENCODER010` in the scene was the whole encoder component (cap + body + bracket) as a fused `Floex_Trainer.fbx` sub-mesh, with its pivot at the model root — so it could never rotate in place. Reimported just the visible cap+shaft through the Day 21 pipeline: FreeCAD leaf-selection of the 6 cap/shaft parts (left out the 4 body/bracket parts that sit behind the panel), Blender join into `KnobEncoder`, Origin to Center of Mass (Volume), applied Rotation & Scale, exported FBX, imported at Scale Factor 0.001. Placed at slot 1, deleted the old fused knob.

**Grab + rotation (Step 3 cont.).** Set up direct-touch grab on `KnobEncoder` (Grabbable + HandGrabInteractable + GrabInteractable + kinematic Rigidbody + collider) plus a One Grab Rotate Transformer. Got it spinning cleanly in place.

**RPM readout script (Step 4).** Wrote `KnobRpmDial.cs`. Final architecture: the **transformer drives the rotation**, and the **script only measures** it — each frame it projects a knob-fixed reference vector onto the plane perpendicular to the barrel axis, takes the signed-angle delta, accumulates it, and converts to whole-RPM steps written to `state.rpmSetpoint`. Clamped 0–250 with step-diff tracking so clamping at the ends doesn't lose count (spin past 250 → holds; spin back → resumes decreasing immediately).

## What broke and how I fixed it

This was a long debugging chain — the knob rotation fought us for most of the session. Recording the dead ends so we don't repeat them:

**Original knob couldn't rotate at all.** Fused sub-mesh, pivot at model root, grab components on a child while the mesh was on the parent, and a Missing Rigidbody. Root-caused to: (a) mesh and Grabbable must share one transform, (b) pivot must be on the barrel axis. → reimported as a clean separate mesh.

**Knob "came out" of the panel when grabbed.** A bare HandGrabInteractable with no transformer carries the grabbed object along with the hand — the transformer is what constrains the grab to rotation-only. Rigidbody position constraints do NOT stop this, because ISDK's Grabbable moves the *transform* directly, not via physics. → the fix was to keep a transformer assigned (it's purpose-built to constrain grab to rotation).

**No cardinal axis would rotate the knob correctly — always coned/swung.** The barrel tilt (to match the slanted panel) is baked into the *mesh geometry*, not the transform — the knob's local rotation is 0,0,0 and it's the parent that's upright, so `transform.up` points world-vertical while the shaft leans. No local X/Y/Z matches the true barrel axis. → created a `KnobAxis` child empty aligned by eye so one of its arrows points down the shaft, and used that as both the transformer's pivot and the axis reference. (Turned out the empty's *forward/blue* arrow was the aligned one, not up/green — handled with a `useForwardAxis` flag in the script.)

**Tried script-driven rotation (RotateAround) as an alternative — abandoned.** Spent time on a version where the script owned rotation via `RotateAround`, plus a `LateUpdate` position-pin to stop the fly-out. The pin fought RotateAround (which moves position) and caused the hand to get stuck holding the knob. → dropped script-driven rotation entirely; let the transformer rotate and made the script read-only. Cleaner, fewer moving parts.

## Decisions

- **Transformer drives rotation; script reads it for RPM.** Cleanest split — the transformer is purpose-built to constrain a grab to in-place rotation, and the read-only script avoids any fight over who controls the transform. The earlier script-driven-rotation approach is abandoned (kept in git history if ever needed).
- **`KnobAxis` empty supplies the tilted barrel axis.** A mesh with baked-in tilt has no clean local axis; an aligned child empty is the reliable way to give both the pivot and the axis. Worth remembering for the slot 4 knobs and any future tilted control.
- **RPM shows the setpoint whether running or stopped.** Matches real pump UX (dial target, then start) and makes the knob visibly do something on a stopped pump. Removed the old hardcoded "180 when running."
- **degreesPerDetent = 6** (≈4.2 turns across 0–250). Tunable; feel can be refined in polish.
- **Slot 4 deferred** until the second knob is modelled — two independent knobs, one per pump, no shared/selected-pump logic.

## Open questions / next

- **Knob feel is functional but not final** — rotation responsiveness and detent feel can be polished later (degreesPerDetent tuning, maybe haptic tick). Parked as a polish item, not blocking.
- **Propagate to single pumps 2 and 3** — repeat the KnobEncoder + KnobAxis + transformer + script setup on slots 2/3. Should be a duplicate-and-rewire now that the pattern is proven.
- **Slot 4 dual knobs** — model the second knob, then two `KnobRpmDial` instances writing to `pumpA_RpmSetpoint` / `pumpB_RpmSetpoint` (needs those fields added to `DoublePumpHeadState`, mirroring the single).
- **`KNOB_ENCODER010` cleanup** — confirm the old fused knob sub-mesh is fully removed from all three single pumps (was only swapped on slot 1 so far if 2/3 still reference the old mesh).
- **Scope check passed:** confirmed rotor visual speed does not change with RPM setpoint — `PumpHeadRotor` reads only `running` + `directionForward`, never `rpmSetpoint`.

## Time spent

~7 hours (state + display ~1h, knob reimport ~1.5h, grab/transformer/axis debugging ~3.5h, RPM script iterations ~1h).

## Files modified today

- `Assets/Scripts/PumpHeadState.cs` — added `rpmSetpoint`
- `Assets/Scripts/Screen1Controller.cs` — RPM readout now reads `rpmSetpoint` live, removed from two-state block
- `Assets/Scripts/KnobRpmDial.cs` — new (read-only RPM reader; reads knob rotation, writes rpmSetpoint)
- `Assets/Models/KnobEncoder.fbx` — new (clean cap+shaft single mesh)
- `Assets/Scenes/OR_Environment.unity` — slot 1: new KnobEncoder with grab + One Grab Rotate Transformer + KnobAxis empty + KnobRpmDial; old fused KNOB_ENCODER010 removed
- `devlog/day-26.md` — new