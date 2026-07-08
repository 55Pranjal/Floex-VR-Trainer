# Day 51

## Goal
Two things: (1) correct the arterial direction in the tutorial — the HLM lead
confirmed the correct rotation is CCW = Reverse (not Forward); (2) add a static
doctor 3D asset to the OR scene for atmosphere.

## What I did
- Direction fix (TutorialController): the correct arterial direction is now Reverse
  (CCW). Added a DirectionOk(unit) helper (correct = !hasCleanDir || !dirForward)
  used by both the pass check and the clue filter so they can't drift. SPH is graded
  on directionForward == false; DPH stays ungraded (CW/CCW push mapping still not
  fully confirmed for the double head). Updated step-4 body text to "DIRECTION =
  Reverse" and flipped the clue to "Arterial direction is Forward — set it to
  Reverse." Also removed the leftover SetText debug logs.
- Doctor asset: imported the Sketchfab FBX into Assets/Models/Hospital/Doctor,
  extracted materials (textures weren't embedded so Extract Textures was greyed;
  Extract Materials worked), and hand-assigned the texture maps. Naming was suffix-
  based: D = albedo/ColorMap, N = NormalMap, R = RoughnessMap, M = MetallicMap;
  Doc-head-* = head material (#3), Doc-*-1024 = body/coat material (#4). Placed as a
  static prop — no rig/animator (Sketchfab version was animated; deliberately kept it
  static as background dressing).

## What broke and how I fixed it
- Doctor imported grey/white: extracted materials had every map slot set to None.
  Fixed by manually dragging each texture into its slot on Material #3 (head) and
  Material #4 (body).
- Colors still looked off vs Sketchfab after assigning maps: root cause was the
  normal maps (-N textures) not being marked Texture Type = Normal map. Unity popped
  a dialog offering to fix the import setting; clicking Fix corrected the normal-map
  encoding and the whole model rendered correctly, matching Sketchfab. (Was chasing
  sRGB / shader differences before that dialog appeared — those were red herrings;
  the real issue was normal-map import type.)

## Decisions
- Arterial correct direction = Reverse (CCW), per HLM lead. This is the confirmed
  direction fact; graded on SPH, and it's the trainer's taught answer.
- Doctor is a static prop, not animated. Animating it would be a separate task (rig +
  clips + Animator Controller); a moving figure can also distract from the training
  task, so static is the right call for background dressing.

## Open questions / next
- DPH direction (CW/CCW) grading still deferred — SPH direction is now confirmed
  (Reverse), but the DPH double-head push mapping isn't, so DPH stays ungraded.
- Scored evaluation tolerance vs the 0.1 teaching band: still KRB.
- Cardioplegia/suction/vent correctness criteria: still open.
- Re-test the tutorial direction flip end-to-end on device (arterial + Forward should
  now FAIL with the corrected clue; Reverse should pass).

## Time spent
~ (fill in)