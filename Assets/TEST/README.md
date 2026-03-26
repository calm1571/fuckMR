# Unity Test Scripts

## Purpose

This directory stores the Unity-side automated test scripts that support the markdown test plans under the root [`TEST/`](../../../TEST) folder.

These scripts are intended to verify logic that is stable and practical to automate inside Unity, such as:

- input trigger / cooldown / debounce behavior
- combat state transitions and rematch reset
- message serialization / routing / fault tolerance
- projectile spawn, motion, lifecycle, and wall collision
- synchronization-related state progression
- spectator permission boundaries and local-only behavior
- alignment-ready state and local visual-basis calculations

This folder is **not** a full replacement for manual MR / XR / multiplayer validation.  
It is the code-layer automation companion to the main test documentation.

## Mapping To Test Plans

Each subfolder corresponds to one markdown test plan in the root [`TEST/`](../../../TEST) directory:

- `Test1/`
  - corresponds to [`AR_TestPlan_1_Imput&Trigger.md`](../../../TEST/AR_TestPlan_1_Imput%26Trigger.md)
  - covers `TG / CD / DB`
- `Test2/`
  - corresponds to [`AR_TestPlan_2_Combat Resolution & State Machine.md`](../../../TEST/AR_TestPlan_2_Combat%20Resolution%20%26%20State%20Machine.md)
  - covers `SH / WO / WR`
- `Test3/`
  - corresponds to [`AR_TestPlan_3_Event Integrity & Fault Tolerance.md`](../../../TEST/AR_TestPlan_3_Event%20Integrity%20%26%20Fault%20Tolerance.md)
  - covers `SE / RT / EX`
- `Test4/`
  - corresponds to [`AR_TestPlan_4_Projectile Presentation.md`](../../../TEST/AR_TestPlan_4_Projectile%20Presentation.md)
  - covers `PJ / PS / MV / LC / BD`
- `Test5/`
  - corresponds to [`AR_TestPlan_5_Multiplayer Synchronization.md`](../../../TEST/AR_TestPlan_5_Multiplayer%20Synchronization.md)
  - currently covers part of `MC / MW / MR`
- `Test6/`
  - corresponds to [`AR_TestPlan_6_Spectator Logic.md`](../../../TEST/AR_TestPlan_6_Spectator%20Logic.md)
  - currently covers part of `SS / SA / SL / SU`
- `Test7/`
  - corresponds to [`AR_TestPlan_7_Shared AR Space & Tracking.md`](../../../TEST/AR_TestPlan_7_Shared%20AR%20Space%20%26%20Tracking.md)
  - currently covers part of `AR / RL / SPC`

## PlayMode vs EditMode

The current scripts are split by runtime characteristics:

- `PlayMode`
  - `Test1`
  - `Test4`
  - These tests instantiate runtime objects, simulate projectile creation, and observe frame / physics behavior.

- `EditMode`
  - `Test2`
  - `Test3`
  - `Test5`
  - `Test6`
  - `Test7`
  - These tests mainly use reflection, local state setup, and direct method invocation to verify code-path behavior without requiring a full live scene.

Recommended understanding:

- use `PlayMode` for object lifecycle, physics-like movement, and per-frame behavior
- use `EditMode` for pure logic, state machine transitions, and message / authority validation

## Fixture Reuse

Each test-plan folder keeps its own shared fixture file so the related tests can reuse the same setup logic:

- `Test1/Test1_InputTestFixture.cs`
  - shared setup for input-trigger tests
- `Test2/Test2_CombatTestFixture.cs`
  - shared setup for combat-resolution tests
- `Test3/Test3_MessageTestFixture.cs`
  - shared helpers for message serialization / routing tests
- `Test4/Test4_ProjectileTestFixture.cs`
  - shared projectile spawn / cleanup / wall helper logic
- `Test5/Test5_SynchronizationTestFixture.cs`
  - shared state-machine and calibration-phase helpers
- `Test6/Test6_SpectatorLogicFixture.cs`
  - shared spectator-local helper logic
- `Test7/Test7_AlignmentFixture.cs`
  - shared alignment / display-basis helper logic

When adding new tests, prefer reusing the fixture in the same folder instead of duplicating bootstrap / reflection / scene setup code.

Recommended pattern:

- add a new test file under the matching `TestX/` folder
- keep file naming aligned with the test-plan block, such as `Test4_LC_...` or `Test6_SL_...`
- extend the local fixture only when multiple files need the same setup

## Automation Scope

### Covered Well By Code-Layer Automation

These areas are currently practical to automate in Unity code:

- input trigger, cooldown, and debounce behavior
- local combat-state transitions and reset behavior
- spectator heal and wall-state logic
- message serialization / routing / malformed-input tolerance
- projectile spawn, motion, destruction, and wall collision
- calibration-phase progression and rematch state progression
- spectator local-only barrage boundaries
- spectator and alignment state-field updates

### Still Better Covered By Real Devices / Three-Client Integration

These areas still require manual or semi-manual validation on actual devices:

- true MR spatial alignment quality in real environments
- tracking loss / regain behavior on hardware
- three-device timing consistency under real LAN conditions
- spectator mid-join and reconnect in a live session
- full UI readability and observation comfort in headset
- local audio perception on device
- long-session thermal / FPS / memory behavior
- real-world shared-space usability and combat reasonableness

In short:

- code-layer scripts check whether the implementation behaves correctly in isolation
- device and three-client tests check whether the full MR experience behaves correctly in reality
