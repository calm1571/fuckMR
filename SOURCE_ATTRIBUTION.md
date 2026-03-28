# Source Attribution

This document explains which source files were developed by Team-GRP01 and how third-party or adapted material is identified.

## 1. Team-Developed Source Code

The primary team-developed source code is located under:

- `Assets/_Project/`

These files now contain a standard attribution header indicating:

- the file is team-developed
- the authoring team is `Team-GRP01`
- the purpose of the file
- whether the file is considered a third-party adaptation

## 2. Meaning of "Third-party adaptation"

For the purposes of this project:

- `Third-party adaptation: No` means the file was authored by the team for this project, even if it uses external SDK APIs.
- `Third-party adaptation: Yes` means the file was substantially adapted around a third-party integration workflow or implementation pattern and should be reviewed together with third-party dependency notes.

## 3. Files Marked as Adaptations

The following team files are integration-heavy wrappers around external SDK or third-party functionality and are marked as adaptations:

- `Assets/_Project/MRWorld/AndroidAprilTagDetectorBridge.cs`
- `Assets/_Project/MRWorld/OpenCVForUnityAprilTagDetector.cs`
- `Assets/_Project/MRWorld/SpatialAnchorSyncService.cs`

These files remain project-developed integration code, but they are closely tied to external APIs and should be treated with additional attribution awareness.

## 4. Non-Team Source and Assets

The following are not team-developed source code and are not covered by the Team-GRP01 attribution header:

- Unity official packages
- PICO SDK packages
- OpenCVForUnity source and assets
- Unity-generated project and editor files
- Imported third-party assets and resources

See `THIRD_PARTY_COMPONENTS.md` for dependency details.

## 5. Scope of Attribution in the Submission

When preparing the final submission archive:

- keep team-developed files clearly separated where possible
- do not present third-party code as original team work
- preserve third-party notices and license files where required
- document any omitted third-party assets if license restrictions apply
