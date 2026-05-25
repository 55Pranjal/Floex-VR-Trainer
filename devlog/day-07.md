# Dev Log — Day 7 (2026-05-25)

## Goal

- Test the real Floex in VR on the Quest 3 (on-device, not just laptop Play mode).
- Close out the CAD-pipeline phase: merge `feature/cad-pipeline` into `main`.

## What I actually did

- **On-device VR test on Quest 3:** the real Floex renders correctly in the OR scene on the headset. Pipeline validated on actual hardware — the machine is there, upright, recognizable.
- Observed two things to correct later (both gated / batched, see below): machine feels **too small** and the player feels **too far** from it.
- Confirmed with the team that the **CDM Door panel** sitting proud of the chassis is NOT intentional — it's a model quirk to fix.
- **Merged `feature/cad-pipeline` into `main`** and pushed. CAD-pipeline phase is now banked on main; the real Floex is in the scene on the main branch.
- No further build/feature work today — deliberately paused rather than manufacturing work while gated on real dimensions.

## What broke and how I fixed it

- **Symptom:** Quest laggy/stuttering in Link mode during testing.
  - **Cause:** Almost certainly environmental (USB cable/port bandwidth, PC under load from Unity+Blender+FreeCAD, Link runtime hiccups) — NOT the scene, which is light (~250K tris).
  - **Fix:** Not chased — Link lag isn't the trainer's real performance. The true performance read is a standalone APK on-device (already verified works on Day 4). Noted and moved on.

## Decisions made

- **Do NOT fix scale by eyeball today.** Machine feels small, but real dimensions from Shiv are "coming soon." Guessing a scale now = two corrections instead of one. Wait for the real height, then fix once: `real_height / current_height` = uniform scale factor (applied in Unity, no re-export needed).
- **Batch the CDM Door fix** rather than do it now. It needs a Blender edit + FBX re-export + re-import, so hold it for one consolidated Blender pass alongside any other model-side corrections.
- **Pause rather than start materials today.** Materials/texturing is unblocked and high-payoff, but it's a real chunk of work better started fresh at the top of a focused session than squeezed into a gated waiting window. Merging the branch was the one valuable + fully-unblocked move, so did that and stopped.

## Open questions / next

- **NEXT SESSION FIRST MOVE:** start materials/texturing — make the machine look like the real unit instead of plain grey. Fully unblocked (independent of scale).
- **After materials:** plan + build the 7-screen console UI on the `12 Inch Assy115` face — the interactive core of Product A. Design note for later: keep control-input and display logic decoupled from behavior (cheap now, expensive to retrofit; also the clean seam if Product B / physiology engine is ever greenlit).
- **Waiting on Shiv:** real Floex dimensions (height/width/depth). On arrival → one uniform scale correction.
- **Player spawn distance:** rig at (0,0,-2) felt too far in VR. Adjust rig Z (try -1.2 to -1.5) — but ideally re-judge after the scale correction, since "too far" and "too small" may be the same problem (a correctly-scaled bigger machine may also fix the felt distance).
- **CDM Door panel:** confirmed unintentional — nudge flush in Blender, re-export, re-import (batched).

## Time spent

Short day. ~30-45 min: Quest on-device test + observations, then the branch merge and close-out.