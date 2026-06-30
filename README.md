# Floex HLM VR Trainer

A virtual-reality training simulator for perfusionists, built for the **Floex 3.0
Heart-Lung Machine (HLM)** by [Floaid MedTech](https://floaid.com/).

The trainer immerses operators in a virtual operating theatre with a faithful Floex 3.0
console, letting them learn the machine's controls and workflow — and practise the core
perfusion decision (setting pump flow correctly for a given patient) — without needing
scarce hardware or clinical time during early training.

## What it does

- **Faithful Floex 3.0 console** in VR — five interactive pump-head screens plus the
  main pole console, navigable by direct touch (poke) and ray, with working pump
  controls (power, start/stop, RPM knobs, tube/direction pickers).
- **Physical pump behaviour** — rotors spin at a speed proportional to RPM, with
  realistic momentum on direction reversal; spatial audio whose pitch tracks pump speed.
- **Patient setup** — enter patient height, weight and cardiac index on the BSA screen
  (via the Quest system keyboard) to compute body surface area and the **target blood
  flow** for that patient.
- **Live flow** — each pump computes its actual blood flow (L/min) from its RPM and
  tubing size, using calibration data taken from the real Floex 3.0 machine.
- **Patient monitor** — a hospital-style vitals monitor (on the OR's patient monitor,
  as in a real theatre) displaying patient state.

The training goal is the perfusionist's core skill: given a patient, set the pump up so
that **actual flow meets the patient's target flow.**

## Overview

- **Headset:** Meta Quest 3 (and 3S)
- **Engine:** Unity 2022.3 LTS (URP)
- **VR stack:** Meta XR Core + Interaction SDK (v74), OpenXR

The project has grown from an initial familiarisation trainer into a clinical
simulator. Development now targets a full physiology-backed CPB simulator, built in
phases against a 28-week roadmap. The console, interaction model, and RPM-coupled
motor behaviour are complete; a decoupled, unit-tested patient-physiology model is
now in progress.

Active development — core simulation loop working.

- [x] Build pipeline to Quest 3
- [x] CAD-to-mesh asset pipeline (STEP → FreeCAD → Blender → FBX)
- [x] Floex 3.0 model imported, OR environment built
- [x] Interactive console — all pump screens, navigation, pickers
- [x] Pump controls — power, start/stop, RPM knobs, rotor + spatial audio
- [x] Patient input (BSA screen) + target-flow calculation
- [x] Actual flow from RPM + tubing (real machine calibration)
- [x] Patient monitor display
- [ ] Scenario engine — predefined patients + correct-operation assessment
- [ ] Flow evaluation / trainee feedback

## Approach

The trainer is being built as a **scenario-based assessment tool**: it presents a
predefined patient and evaluates whether the trainee operates the HLM correctly for that
patient, rather than running a full real-time physiology engine. Clinical correctness
lives in scenario definitions reviewed by clinical advisors.

> Version pins are deliberate and validated against known SDK/Gradle compatibility
> issues. See [`CLAUDE.md`](CLAUDE.md) for the full environment specification and the
> reasoning behind each pin — do not upgrade to "latest" without checking it.

## Architecture

The console is organised around a **per-canvas pattern**: each pump-head display is a
self-contained interactive unit that owns its own state, screen navigation, and
interaction routing. The simulator comprises five canvases — three single pump heads,
one double pump head, and the main pole canvas.

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

## Project status

The interactive console is feature-complete and the first physiology work has begun.

**Complete**

- [x] Standalone build pipeline to Quest 3
- [x] CAD-to-mesh asset pipeline (STEP → FreeCAD → Blender → FBX → Unity)
- [x] Floex 3.0 model imported, OR environment built
- [x] All five canvases fully interactive — screen navigation and pickers
- [x] Four pump heads with independent state (double pump: A + B + shared flow ratio)
- [x] Direct touch (Poke) and ray interaction on all canvases
- [x] RPM-coupled rotor rotation across all pump heads
- [x] Interactive RPM knobs driving setpoint and rotor speed
- [x] Spatial audio — pump hum pitch-coupled to RPM

**In progress**

- [ ] Phase 3A polish pass (alarm/light feedback, sustained 72 fps, regression sweep)
- [ ] `PatientState` physiology model — pure C#, 50 ms ticker, unit-tested
- [ ] Full CPB scenarios

## Getting started

This repository uses **Git LFS** for large binary assets (3D models, textures).
Install Git LFS before cloning:

```bash
git lfs install
git clone https://github.com/55Pranjal/Floex-VR-Trainer.git
```

Open with **Unity 2022.3.62f3**. See `CLAUDE.md` for the full environment specification,
version pins, architecture, and hard-won development notes.

## About Floaid MedTech

Floaid MedTech Private Limited is a cardiac medical-device company developing the
Floex 3.0 Heart-Lung Machine and related cardiopulmonary technologies.

---

_This project is under active development and not yet ready for production use._
_The simulator is a training aid and is not a certified medical device._
