# Developer Handover / Secondary Development Guide

## 1. Purpose

This document is for engineers who will continue development on the project after the current milestone is complete. It focuses on architecture, responsibilities, extension points, and the places that are risky to modify.

## 2. Project Positioning

The project is not a generic XR template. It is a purpose-built MR multiplayer prototype with three runtime roles:
- Host
- Client
- Spectator

The project mixes several layers in one runtime flow:
- app state machine
- MR passthrough setup
- local/remote visual alignment
- host-authoritative combat
- spectator interaction systems

That means any large change should be evaluated against all five layers, not just the gameplay code.

## 3. Main Runtime Entry

Primary runtime entry:
- `Assets/_Project/Core/M0RuntimeBootstrap.cs`

This file is the project orchestrator. It owns or coordinates:
- state machine startup
- UI creation
- role selection
- network startup
- calibration flow
- playing flow
- HP / combat / result logic
- spectator systems
- wall obstacle flow

### Important implication
Do not treat `M0RuntimeBootstrap` as a safe “small edit” file. It is effectively the integration hub of the whole project.

If you plan to refactor it, split responsibilities gradually and keep runtime parity after each step.

## 4. Core Architecture

### 4.1 State Layer
Files:
- `AppStateMachine.cs`
- `IAppState.cs`
- `BootState.cs`
- `MainMenuState.cs`
- `RoleSelectState.cs`
- `LobbyHostState.cs`
- `LobbyClientState.cs`
- `LobbySpectatorState.cs`
- `CalibrationState.cs`
- `PlayingState.cs`
- `ResultState.cs`

Responsibility:
- top-level app flow only
- no heavy gameplay logic inside state classes
- most actual behavior is delegated back into `M0RuntimeBootstrap`

### 4.2 Networking Layer
Files:
- `LanMessage.cs`
- `NetworkRole.cs`
- `UdpLanTransport.cs`
- `M3NetworkCoordinator.cs`

Responsibility:
- transport and message definitions
- Host relay behavior
- exposing typed events to gameplay/runtime layer

Important boundary:
- Host remains authoritative
- Client and Spectator must not directly mutate authoritative combat state
- Spectator requests are requests, not direct world mutations

### 4.3 MR / Alignment Layer
Files:
- `RemoteAlignmentController.cs`
- `WorldRootController.cs`
- `SpatialAnchorSyncService.cs`
- `AprilTagAutoTrackingSource.cs`
- `OpenCVForUnityAprilTagDetector.cs`
- `ManualMarkerTrackingSource.cs`
- related interfaces

Responsibility:
- world root motion
- marker / AprilTag / future anchor support
- local visual offset adjustment

Current mainline:
- the active battle flow currently relies on serialized manual calibration
- AprilTag and Spatial Anchor code is retained, but not the main battle entry path

### 4.4 Gameplay Layer
Files:
- `M1ProjectileShooter.cs`
- `M1Projectile.cs`
- `M3RemotePlayerProxy.cs`
- `M5ShieldVisual.cs`
- `WallObstacleRuntime.cs`

Responsibility:
- projectile visuals
- local fire behavior
- remote avatar rendering
- shield visuals
- wall obstacle visuals and local hit feedback

### 4.5 Spectator Layer
Files:
- `SpectatorControlView.cs`
- `SpectatorBarrageView.cs`
- `SpectatorAudioPlayer.cs`
- spectator-related branches in `M0RuntimeBootstrap.cs`

Responsibility:
- spectator local control panel
- local-only audience interaction feedback
- heal voting
- wall placement preview

## 5. Current Calibration Model

Current calibration is a serialized five-step process:
1. Client adjusts Host
2. Host adjusts Client
3. Spectator adjusts Client
4. Spectator adjusts Host
5. Host final confirm

Important design rule:
- only one device can adjust during a given step
- all others must wait
- phase transitions are synchronized through `RemoteAlignment` messages

### Why this exists
The current system is not a true shared-world anchor pipeline. It is a pragmatic local-visual alignment workflow that keeps gameplay usable without requiring fully reliable shared anchor deployment.

### Risk when modifying
If you change calibration logic, re-test:
- phase progression on all three devices
- non-current role input lockout
- remote alignment message relay
- final Host-only confirm gate

## 6. Combat Authority Model

### Host responsibilities
Host owns:
- hit resolution
- HP updates
- match result
- wall obstacle authority
- spectator vote application

### Client responsibilities
Client owns:
- local input
- local visuals
- sending requests / actions to Host

### Spectator responsibilities
Spectator owns:
- local observation
- local calibration of Host and Client views
- sending support requests to Host
- no direct authority over combat results

## 7. Wall Obstacle System

Key file:
- `Assets/_Project/Gameplay/Combat/WallObstacleRuntime.cs`

Current behavior:
- Spectator enters placement preview
- preview is local-only
- Host spawns authoritative wall from request
- wall HP decays over time
- wall takes damage from player shots
- wall blocks shots
- wall shows HP bar and damage cracks
- wall disappears at zero HP

### Important note
There are two layers:
1. preview wall
2. authoritative runtime wall

Do not mix them. The preview must never be treated as a real gameplay wall.

## 8. Networking Extension Rules

If you add a new interactive feature, follow this pattern:
1. add payload to `LanMessage.cs`
2. add send API to `M3NetworkCoordinator.cs`
3. add transport send in `UdpLanTransport.cs`
4. decide whether Host should relay it or process it locally
5. expose a typed event
6. consume the event in `M0RuntimeBootstrap.cs`

### Do not shortcut this by
- directly mutating remote objects on non-Host peers
- injecting role-specific hacks into unrelated message handlers
- relying on visual state as authoritative gameplay state

## 9. Recommended Refactor Direction

The project works, but the main integration file is large.

### Best next refactor candidates
1. split `M0RuntimeBootstrap` into feature modules:
- calibration coordinator
- combat coordinator
- spectator coordinator
- wall obstacle coordinator

2. isolate UI builders:
- lobby/result/menu creation
- spectator panel
- calibration panel

3. isolate network message handling:
- one message router per feature domain

### Refactor rule
Do not refactor multiple foundational systems in the same change.
Keep runtime parity after each step.

## 10. Files That Need Extra Caution

### High-risk files
- `Assets/_Project/Core/M0RuntimeBootstrap.cs`
- `Assets/_Project/Networking/M3NetworkCoordinator.cs`
- `Assets/_Project/Networking/UdpLanTransport.cs`
- `Assets/_Project/Gameplay/Combat/WallObstacleRuntime.cs`
- `Assets/_Project/Gameplay/Combat/M1ProjectileShooter.cs`
- `Assets/_Project/Gameplay/Combat/M3RemotePlayerProxy.cs`

### Why high-risk
These files sit on integration boundaries. A small change can break:
- calibration flow
- three-role synchronization
- hit authority
- wall obstacle replication
- spectator features

## 11. Recommended Development Workflow

For future work, use this order:
1. identify whether the feature is visual-only or authority-affecting
2. if authority-affecting, design Host-side ownership first
3. update payloads and transport
4. update runtime orchestration
5. test Host / Client / Spectator separately
6. test the whole three-device chain

## 12. Testing Checklist for Secondary Development

Any non-trivial feature change should re-test:
- app boot and passthrough
- Host / Client / Spectator lobby connect
- five-step calibration
- Host final confirm
- entering Playing on all devices
- projectile / shield / HP loop
- result / retry flow
- spectator panel basics
- wall obstacle lifecycle

## 13. Known Boundaries

- active flow is still not a full shared absolute spatial anchor solution
- spectator alignment is local to spectator view
- standard PICO 4 is not the primary verified MR target
- command-line `dotnet build` may not be available on all dev environments

## 14. New Feature Guidelines

### Safe feature types
- local-only UI
- spectator local presentation
- non-authoritative visual feedback
- documentation and tooling

### Medium-risk feature types
- new spectator requests
- new wall parameters
- new debug overlays

### High-risk feature types
- calibration flow changes
- transport relay changes
- world/root alignment changes
- hit resolution changes
- replacing current authority model

## 15. Recommended Reading Order for New Developers

1. `README.md`
2. `M0RuntimeBootstrap.cs`
3. `M3NetworkCoordinator.cs`
4. `UdpLanTransport.cs`
5. `RemoteAlignmentController.cs`
6. `WallObstacleRuntime.cs`
7. `M1ProjectileShooter.cs`
8. `M3RemotePlayerProxy.cs`

## 16. Handover Advice

If you are taking over this project, do not begin with architectural cleanup.
Begin with small verification changes so you understand:
- which logic is Host-authoritative
- which visuals are local-only
- which alignment offsets are observer-specific

Only after that should you attempt larger changes such as:
- shared spatial anchor integration
- calibration model replacement
- transport redesign


