# Third-Party Components

This document identifies third-party software, packages, assets, and external dependencies used by the project.

## 1. Unity Editor and Official Packages

- Unity Editor: `2022.3.62f3`
- Universal Render Pipeline: `14.0.12`
- TextMeshPro: `3.0.7`
- UGUI and other Unity official modules listed in `Packages/manifest.json`
- XR Interaction Toolkit samples and related Unity packages as referenced by the project environment

These are official Unity components and are managed through Unity Package Manager and the project manifest.

## 2. PICO Components

- PICO Unity Integration SDK / PICO XR package
- Source: referenced through `Packages/manifest.json`
- Current manifest entry:
  - `com.unity.xr.picoxr`: `https://github.com/Pico-Developer/PICO-Unity-Integration-SDK.git#release_3.3.0`

This component is third-party relative to the team and must be treated according to the PICO SDK license and terms.

## 3. OpenCVForUnity

- Component: `OpenCVForUnity`
- Vendor: Enox Software
- Original project location: `Assets/OpenCVForUnity/`
- Includes vendor notices and license files in the source project

Submission decision:
- `Assets/OpenCVForUnity/` is excluded from the final submission archive.
- Reason: redistribution rights are not being assumed by the team for the submitted coursework package.

Restore method for examiners or maintainers:
1. Obtain the same `OpenCVForUnity` asset legally from the vendor.
2. Import it back into the Unity project so that `Assets/OpenCVForUnity/` is restored.
3. Reopen the project in the specified Unity version.
4. Allow Unity to reimport and rebuild the project references.

Important note:
- This is not team-developed code.
- It remains a third-party dependency even when restored locally.

## 4. Audio Assets

- Local spectator audio files:
  - `Assets/Resources/Audio/cheer.ogg`
  - `Assets/Resources/Audio/yay.ogg`
- Source website:
  - `https://pixabay.com/zh/sound-effects/`

Submission note:
- These files are not team-developed code.
- They should be treated as third-party media assets.
- The team should preserve attribution and usage conditions required by the source platform and the specific asset page terms used at download time.
- If the final report needs explicit media attribution, record the exact Pixabay asset pages used for these files.

## 5. Team-Developed Scope

The team-developed implementation is primarily located in:

- `Assets/_Project/`
- project-authored documentation in the repository root
- test plans in `TEST/`

## 6. Components That Must Be Clearly Distinguished From Team Code

The following are not team-developed source code and should be identified as such:

- Unity official packages and modules
- PICO SDK packages
- OpenCVForUnity
- TextMesh Pro essentials and Unity-generated project infrastructure
- Any audio, textures, or imported resources not created by the team

## 7. Submission Guidance

If the final archive excludes a third-party asset for licensing reasons, the report or submission note should explain:

- what was excluded
- why it was excluded
- how it can be obtained legally
- what functionality depends on it
