# Floex HLM VR Trainer

A virtual-reality training simulator for perfusionists, built for the **Floex 3.0
Heart-Lung Machine (HLM)** by [Floaid MedTech](https://floaid.com/).

The trainer reproduces the Floex 3.0 console and operating-room workflow in an
immersive VR environment, letting operators build familiarity and procedural skill
without consuming scarce hardware or clinical time during early training.

---

## Overview

The Floex 3.0 is a heart-lung machine used to take over the function of the heart and
lungs during cardiopulmonary bypass (CPB). Operating one safely demands extensive
hands-on practice that is difficult to schedule on real, in-service equipment. This
project delivers that practice in VR: a faithful interactive replica of the console,
its pump heads, and the surrounding OR, running standalone on a consumer headset.

The project has grown from an initial familiarisation trainer into a clinical
simulator. Development now targets a full physiology-backed CPB simulator, built in
phases against a 28-week roadmap. The console, interaction model, and RPM-coupled
motor behaviour are complete; a decoupled, unit-tested patient-physiology model is
now in progress.

## Platform & technology

| Area            | Choice                                                   |
| --------------- | -------------------------------------------------------- |
| Headset         | Meta Quest 3 / 3S (standalone, Android, ARM64, IL2CPP)   |
| Engine          | Unity 2022.3.62f3 LTS                                     |
| Render pipeline | URP, Single Pass Instanced                               |
| VR stack        | Meta XR Core SDK + Meta XR Interaction SDK v74.0.0, OpenXR |
| Package         | `com.floaid.floexvr`                                     |

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

Open the project in **Unity 2022.3.62f3**. The main scene is
`Assets/Scenes/OR_Environment.unity`.

For Quest deployment, the build target is Android / ARM64 / IL2CPP with OpenXR, the
Oculus Touch Controller Profile, and Meta Quest Support enabled. For in-editor
testing over Quest Link, OpenXR and the Touch profile must also be enabled on the
Windows (desktop) tab of XR Plug-in Management.

See [`CLAUDE.md`](CLAUDE.md) for the complete environment specification, build
settings, asset-pipeline conventions, and hard-won setup notes.

## Repository layout

| Path                    | Contents                                            |
| ----------------------- | --------------------------------------------------- |
| `Assets/Scripts/`       | Runtime logic (state, navigators, controllers, rotors) |
| `Assets/ScreenSpecs/`   | JSON screen specifications (source of truth for UI) |
| `Assets/Editor/`        | `ScreenBuilder.cs` and editor tooling               |
| `Assets/Models/`        | 3D assets (pump heads, knobs, trainer, hospital)    |
| `Assets/Scenes/`        | `OR_Environment.unity` main scene                   |
| `devlog/`               | Daily development logs                              |
| `docs/`                 | Roadmaps and planning documents                     |

## Development workflow

Progress is recorded as a daily devlog in `devlog/day-NN.md`. Screen specifications
are edited in JSON and regenerated through `ScreenBuilder`; values are not adjusted
directly in the Unity Inspector except during final polish. Large binaries are tracked
through Git LFS.

## About Floaid MedTech

Floaid MedTech Private Limited is a cardiac medical-device company developing the
Floex 3.0 Heart-Lung Machine and related cardiopulmonary technologies.

---

_This project is under active development and is not a certified medical device. It is
intended for training and familiarisation, not for clinical decision-making or patient
care._
