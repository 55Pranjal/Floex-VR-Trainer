# Floex VR Trainer

A virtual-reality training simulator for perfusionists, built for the **Floex 3.0
Heart-Lung Machine (HLM)** by [Floaid MedTech](https://floaid.com/).

The trainer familiarises operators with the Floex 3.0 console and workflow in an
immersive VR environment, reducing the need for scarce hardware and clinical time
during early operator training.

> **Scope:** This is _Product A_ — a **familiarisation trainer** (a focused MVP),
> not a full physical-fidelity cardiopulmonary-bypass simulator.

## Platform

- **Headset:** Meta Quest 3 (and 3S)
- **Engine:** Unity 2022.3 LTS (URP)
- **VR stack:** Meta XR Core + Interaction SDK, OpenXR

## Status

Early development.

- [x] Build pipeline to Quest 3 working
- [x] VR interaction basics (grabbable objects via Meta Interaction SDK)
- [x] CAD-to-mesh asset pipeline validated (STEP → FreeCAD → Blender → FBX)
- [ ] Floex 3.0 model imported and OR environment built
- [ ] Interactive console (pump control UI)
- [ ] Physiology engine

## Development

This repository uses **Git LFS** for large binary assets (3D models, textures).
Make sure Git LFS is installed before cloning:

```bash
git lfs install
git clone https://github.com/55Pranjal/Floex-VR-Trainer.git
```

Open the project with **Unity 2022.3.62f3**. See `CLAUDE.md` for the full environment
specification, version pins, and development notes.

## About Floaid MedTech

Floaid MedTech Private Limited is a cardiac medical-device company developing the
Floex 3.0 Heart-Lung Machine and related cardiopulmonary technologies.

---

_This project is under active development and not yet ready for production use._
