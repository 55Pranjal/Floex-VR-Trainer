# Floex HLM VR Trainer

A virtual-reality familiarization trainer for perfusionists, built for the
**Floex 3.0 Heart-Lung Machine (HLM)** by [Floaid MedTech](https://floaid.com/).

The trainer places an operator inside a virtual operating theatre with a faithful
Floex 3.0 console and lets them learn the machine's controls and workflow — and
practise the core perfusion setup task (setting a pump to a patient's target blood
flow) — without needing scarce hardware or clinical time during early training.

## What it does

- **Faithful Floex 3.0 console in VR** — five interactive displays (three single
  pump heads, one double pump head, and the main pole console), navigable by direct
  touch (poke) and by ray, with working pump controls: power, start/stop, RPM knobs,
  and pump-type / tube-size / direction pickers.
- **Physical pump behaviour** — rotors spin at a speed proportional to RPM with
  realistic momentum on direction reversal, and spatial audio whose pitch tracks pump
  speed.
- **Patient setup** — enter patient height, weight and cardiac index on the BSA
  screen (via the Quest system keyboard) to compute body surface area and the
  **target blood flow** for that patient.
- **Live flow** — each pump computes its actual blood flow (L/min) from its RPM and
  tubing size, using calibration data from the real Floex 3.0 machine.
- **Machine-wide arterial exclusivity** — only one pump head may be assigned the
  arterial role at a time, enforced across every head and lane, mirroring real
  machine setup.
- **Patient monitor** — a hospital-style vitals monitor (on the OR's patient monitor,
  as in a real theatre) driven by a decoupled patient-state model.
- **Guided familiarization tutorial** — a floating in-world panel walks the trainee
  through the full setup: open the patient screen, calculate the target flow, power a
  pump head, assign it as arterial in the correct direction, and rotate the knob until
  actual flow matches the target. The tutorial validates the trainee's setup and gives
  specific, corrective feedback when something is wrong.

The training goal is the perfusionist's core setup skill: given a patient, configure
the pump so that **actual flow meets the patient's target flow.**

## Platform & technology

| Area            | Choice                                                     |
| --------------- | ---------------------------------------------------------- |
| Headset         | Meta Quest 3 / 3S (standalone, Android, ARM64, IL2CPP)     |
| Engine          | Unity 2022.3.62f3 LTS                                      |
| Render pipeline | URP, Single Pass Instanced                                 |
| VR stack        | Meta XR Core SDK + Meta XR Interaction SDK v74.0.0, OpenXR |
| Package         | `com.floaid.floexvr`                                       |

> Version pins are deliberate and validated against known SDK/Gradle compatibility
> issues. See [`CLAUDE.md`](CLAUDE.md) for the full environment specification and the
> reasoning behind each pin — do not upgrade to "latest" without checking it.

## Architecture

The console is organised around a **per-canvas pattern**: each pump-head display is a
self-contained interactive unit that owns its own state, screen navigation, and
interaction routing. The simulator comprises five canvases — three single pump heads,
one double pump head, and the main pole console.

Key design principles:

- **State and display are decoupled.** `PumpHeadState` / `DoublePumpHeadState` are
  pure data containers with no Unity dependencies. Screen controllers read state
  one-way to update the UI; button handlers write back through state methods.
- **Screens are generated from JSON.** Specifications in `Assets/ScreenSpecs/` are the
  source of truth; `Assets/Editor/ScreenBuilder.cs` generates the Unity objects.
  Layout changes are made in JSON and rebuilt, not nudged in the Inspector.
- **Navigators own button wiring.** Screen lifecycle and button-to-method binding are
  centralised per canvas, with controllers handling their own per-screen buttons.
- **Dual interaction model.** Both direct touch (Poke) and ray interaction work on all
  five canvases, coexisting through a custom click pipeline that resolves Meta XR's
  multi-coplanar-canvas routing limitation.
- **Physically grounded motor model.** Rotor spin speed is coupled to the RPM setpoint
  at 6°/s per RPM (1 RPM = one revolution per minute), with direction control per pump.

## Features (complete)

- [x] Standalone build pipeline to Quest 3
- [x] CAD-to-mesh asset pipeline (STEP → FreeCAD → Blender → FBX → Unity)
- [x] Floex 3.0 model imported, OR environment built
- [x] All five canvases fully interactive — screen navigation and pickers
- [x] Four pump heads with independent state (double pump: A + B lanes + shared ratio)
- [x] Direct touch (Poke) and ray interaction on all canvases
- [x] RPM-coupled rotor rotation with momentum on direction reversal
- [x] Interactive RPM knobs driving setpoint and rotor speed
- [x] Spatial audio — pump hum pitch-coupled to RPM
- [x] Power control per pump head
- [x] Patient setup (BSA screen) with on-device keyboard entry + target-flow calculation
- [x] Live actual-flow from RPM + tubing (real machine calibration)
- [x] Machine-wide arterial-role exclusivity
- [x] Patient vitals monitor
- [x] Guided familiarization tutorial with setup validation and corrective feedback
- [x] Sustained 72 fps on-device

## Overview

The Floex 3.0 is a heart-lung machine used to take over the function of the heart and
lungs during cardiopulmonary bypass. Operating one safely demands hands-on practice
that is difficult to schedule on real, in-service equipment. This project delivers that
practice in VR: a faithful interactive replica of the console, its pump heads, and the
surrounding OR, running standalone on a consumer headset, with a guided walkthrough of
the core patient-setup task.

## Getting started

This repository uses **Git LFS** for large binary assets (3D models, textures).
Install Git LFS before cloning:

```bash
git lfs install
git clone https://github.com/55Pranjal/Floex-VR-Trainer.git
```

Open with **Unity 2022.3.62f3**. See `CLAUDE.md` for the full environment specification,
version pins, architecture, and development notes.

## About Floaid MedTech

Floaid MedTech Private Limited is a cardiac medical-device company developing the
Floex 3.0 Heart-Lung Machine and related cardiopulmonary technologies.

---

_The simulator is a training aid and is not a certified medical device._