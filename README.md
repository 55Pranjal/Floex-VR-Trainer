# Floex VR Trainer

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

## Platform

- **Headset:** Meta Quest 3 (and 3S)
- **Engine:** Unity 2022.3 LTS (URP)
- **VR stack:** Meta XR Core + Interaction SDK (v74), OpenXR

## Status

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

## Development

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