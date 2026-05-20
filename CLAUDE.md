# Floex HLM VR — Project Context

## Environment (DO NOT CHANGE without good reason)

- Unity 2022.3.62f3 (standard LTS — NOT Extended LTS, NOT Unity 6.x)
- Meta XR Core SDK v74.0.0 (NOT 200+ series — namespace collision bug with Gradle 9.1)
- OpenXR + Oculus Touch Controller Profile + Meta Quest Support enabled
- URP, Single Pass Instanced rendering
- Target: Meta Quest 3 (and 3S), Android, ARM64, IL2CPP
- Package name: com.floaid.floexvr

## Hard-won gotchas

- Ctrl+S after changing Renderer Features / Project Settings — Unity doesn't auto-save these
- Remove SSAO from ALL UniversalRenderer assets (Mobile_Renderer, PC_Renderer)
- Developer mode only works on the Quest OWNER profile, not secondary profiles
- Use com.meta.xr.sdk.core only — NOT com.meta.xr.sdk.all (pulls conflicting modules)
- Project lives at C:\Floaid\ — NOT under OneDrive (sync corrupts Library folder)

## Current state (Day 1 complete)

- Hello-world cube renders on Quest 3. Build pipeline confirmed working.

## Next

- Git + Git LFS init, first commit, push to GitHub
- Then: Week 2 — get Floex CAD from Hashir, start asset pipeline
