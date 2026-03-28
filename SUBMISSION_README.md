# Submission Package README

This document describes what should be included in the final submission archive for the student software project and what should be excluded.

## 1. Recommended Submission Scope

Include the following project content in the submission archive:

- `Assets/_Project/`
- `Assets/Resources/Audio/`
- `Assets/Resources/PXR_ProjectSetting.asset` if required by the runtime configuration
- Any additional Unity assets under `Assets/` that are directly referenced by the submitted scene and are required for the system description
- `Packages/manifest.json`
- `Packages/packages-lock.json` if present
- `ProjectSettings/`
- `TEST/`
- `README.md`
- `README.en.md`
- `README.zh-CN.md`
- `USER_MANUAL.en.md`
- `USER_MANUAL.zh-CN.md`
- `BUILD_GUIDE.zh-CN.md`
- `DEVELOPER_HANDOVER.en.md`
- `DEVELOPER_HANDOVER.zh-CN.md`
- `THIRD_PARTY_COMPONENTS.md`
- `SOURCE_ATTRIBUTION.md`
- `SUBMISSION_PACKAGE_CHECKLIST.md`

## 2. Excluded Content

Do not include the following generated, machine-specific, or sensitive content:

- `Library/`
- `Logs/`
- `obj/`
- `.vs/`
- `UserSettings/`
- `.git/`
- `*.csproj`
- `*.sln`
- `user.keystore`
- `Assets/OpenCVForUnity/` if redistribution rights are not being granted with the submission

## 3. Third-Party Components

Some third-party packages and assets are used by this project. They must be identified clearly and handled according to their licenses.

- Unity packages are referenced through `Packages/manifest.json`
- PICO Integration SDK is referenced through the Unity package manifest
- OpenCVForUnity is a third-party Unity asset and is excluded from the final submission archive
- spectator audio assets come from Pixabay and are treated as third-party media assets

If a third-party asset cannot be redistributed in the submission archive, document:

- the component name
- the source/vendor
- the version
- the license situation
- how the examiner can obtain or restore it

## 4. Readability Requirement

The submitted source code should be easy to read and understand. For this submission:

- team-developed files under `Assets/_Project/` contain team attribution headers
- user, maintenance, and build documentation is included
- testing documents are included in `TEST/`

## 5. Recommended Packaging Strategy

Create a clean submission copy of the project instead of compressing the working directory directly.

Recommended top-level structure inside the archive:

```text
submission/
├─ Assets/
│  ├─ _Project/
│  └─ Resources/
├─ Packages/
├─ ProjectSettings/
├─ TEST/
├─ README.md
├─ README.en.md
├─ README.zh-CN.md
├─ USER_MANUAL.en.md
├─ USER_MANUAL.zh-CN.md
├─ BUILD_GUIDE.zh-CN.md
├─ DEVELOPER_HANDOVER.en.md
├─ DEVELOPER_HANDOVER.zh-CN.md
├─ THIRD_PARTY_COMPONENTS.md
├─ SOURCE_ATTRIBUTION.md
└─ SUBMISSION_PACKAGE_CHECKLIST.md
```

## 6. Notes for Examiners

The project may not run completely out of the box on a fresh machine unless the required Unity version, Android environment, PICO device environment, and third-party assets are available. The included build, user, and handover documentation explains these requirements.

For this submission, `Assets/OpenCVForUnity/` is intentionally excluded. If OpenCV-dependent functionality needs to be restored, obtain the same `OpenCVForUnity` asset legally, import it into the Unity project, and reopen the project so Unity can rebuild references.
