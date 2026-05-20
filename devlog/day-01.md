# Dev Log — Day 1 (2026-05-19)

## Goal
1. Stand up the entire Quest 3 development toolchain from scratch.
2. Get a hello-world build running on the headset — prove the full pipeline end to end.
3. Initialise version control and push the working baseline to GitHub.

## What I actually did
- **Toolchain:** Installed Android Platform Tools (standalone ADB), Meta Quest Developer Hub
  (MQDH), Unity Hub, Unity, VS Code (+ C# Dev Kit + Unity extensions), Git, Git LFS. Added ADB
  to PATH. Confirmed `adb devices` shows the Quest as `device`.
- **Quest setup:** Enabled developer mode, paired the headset over USB, authorised USB debugging.
- **Project:** Created a 3D (URP) project, switched build target to Android, configured XR
  (OpenXR + Oculus Touch Controller Profile + Meta Quest Support), installed the Meta XR Core SDK.
- **Hello world:** Added a Camera Rig building block + a cube at (0, 1.5, 2), built to device, and
  saw the cube floating in front of me in VR. Recorded it on the Quest.
- **Version control:** Initialised Git + LFS, added the Unity `.gitignore`, committed the working
  baseline, and pushed to github.com/55Pranjal/Floex-VR-Trainer. Wrote CLAUDE.md.

## What broke and how I fixed it

- **Symptom:** Plugging the Quest into USB only showed an "Enable Link" popup in the headset —
  never the "Allow USB debugging from this computer" prompt. ADB couldn't authorise the device.
  - **Cause:** I was logged into the Quest under a *non-owner* profile. Developer features
    (debugging, sideload) only work on the owner profile.
  - **Fix:** Switched to the owner profile. The USB debugging prompt then appeared.

- **Symptom:** `adb` not recognised in PowerShell.
  - **Cause:** Platform Tools extracted but not on PATH.
  - **Fix:** Extracted to `C:\platform-tools`, added it to the User PATH env var, opened a *fresh*
    PowerShell (PATH changes don't apply to already-open terminals).

- **Symptom:** OpenXR showed a performance warning that wouldn't clear even after I removed the
  SSAO render feature from the renderer.
  - **Cause:** Unity buffers asset changes in memory; removing the feature in the Inspector wasn't
    written to disk. Also the warning checks *all* UniversalRenderer assets, not just the active one.
  - **Fix:** Removed SSAO from every renderer (Mobile_Renderer, PC_Renderer), then **Ctrl+S** to
    save the project. Warning cleared. (Lesson: Ctrl+S after Renderer/Project Settings changes.)

- **Symptom:** First build failed — "Manifest merger failed", namespace `com.oculus.Integration`
  used in multiple modules (InteractionSdk, SDKTelemetry, OVRPlugin).
  - **Cause:** I was on **Unity 6.4** (Hub defaulted to it), whose bundled **Gradle 9.1** is strict
    about duplicate namespaces. The latest Meta SDK (200-series) declares the same namespace across
    several modules — a known bleeding-edge incompatibility.
  - **Fix attempt that failed:** Tried forcing Gradle 8.9 — rejected, because Unity 6.4's Android
    plugin *requires* Gradle 9.1.0+. Dead end.
  - **Real fix (decision below):** Dropped Unity 6.4 entirely and moved to Unity 2022.3 LTS +
    Meta Core SDK v74.0.0, the battle-tested combo. Build succeeded.

- **Symptom:** Unity 2022.3.76f1 threw a "License error — requires Industry/Enterprise license."
  - **Cause:** 2022.3.63+ are **Extended LTS** builds, which need a paid license. The free Personal
    license doesn't cover them.
  - **Fix:** Installed **2022.3.62f3** instead — a standard LTS build (no "Enterprise only" tag, no
    security flag), which Personal covers.

- **Symptom:** Smaller build-time dialogs on the good build run: package-name error, "Active Input
  Handling set to Both", "Android SDK missing API 32".
  - **Fix:** Set package name to `com.floaid.floexvr`; clicked Yes to switch input handling (editor
    restart); clicked "Update Android SDK" to auto-download API 32. Build then completed.

- **Symptom:** `git commit` failed — "Author identity unknown."
  - **Cause:** First-time Git use on this machine; no global identity set.
  - **Fix:** `git config --global user.email/user.name`, then re-committed.

## Decisions made
- **Dropped Unity 6.4 → Unity 2022.3.62f3 (standard LTS).** The single most important call of the
  day. Fighting bleeding-edge Gradle/SDK bugs would have cost days; the LTS combo is what the whole
  ecosystem and every tutorial targets. Made the switch on Day 1 when nothing was invested yet.
- **Pinned Meta XR Core SDK to v74.0.0**, not the 200-series — the 200s carry the namespace bug.
- **Installed `com.meta.xr.sdk.core` only**, NOT `com.meta.xr.sdk.all` — the "all" bundle pulls in
  the conflicting Interaction/Telemetry/Platform modules.
- **Project lives at `C:\Floaid\Floex HLM VR`**, NOT under OneDrive — sync corrupts the multi-GB
  Library folder.
- **LFS tracks binaries only** (fbx, png, psd, wav, mp4, tga, jpg, cubemap) — kept `.unity` and
  `.asset` as normal Git files so they stay diffable/mergeable.

## Open questions / next
- **#1 next move:** confirm the Floex CAD format and sizing with the team (→ became Day 2 Block 1).
- Get the OR environment and Floex mesh into the scene once the CAD lands.
- CLAUDE.md to be fleshed out with the full version pins and gotchas before next session.

## Time spent
Full session. The Unity 6 → 2022 LTS migration (discovering the Gradle bug, the failed Gradle
downgrade, the Extended-LTS license trap, then a clean reinstall) ate the majority of the day. The
toolchain install and the actual hello-world build were comparatively quick. The detour wasn't
wasted — debugging it is why I could fix the last few errors solo.
