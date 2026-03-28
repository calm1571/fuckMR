# Project Documentation

## 1. Overview

This project is a Unity 2022.3 LTS multiplayer MR prototype built on PICO Unity Integration SDK 3.x for Android devices. The project targets a simplified HADO-style mixed reality battle experience for PICO 4 Ultra, then extends it into a three-role runtime with Host, Client, and Spectator.

Core features:
- Full-session Passthrough rendering
- Host-authoritative gameplay
- Host / Client 1v1 battle loop
- Spectator mode
- Multi-step manual alignment and local visual calibration
- LAN UDP networking
- Projectiles, shields, HP, result screen, rematch
- Spectator heal voting, barrage words, local audio cues, and wall obstacle system

## 2. Tech Stack

- Unity 2022.3.62f3
- Universal Render Pipeline 14.0.12
- TextMeshPro 3.0.7
- PICO Unity Integration SDK 3.3.0
- Android / IL2CPP / ARM64
- Custom LAN UDP transport
- Optional OpenCVForUnity / AprilTag support (currently kept in the project, but the main battle flow is using manual alignment as the active path)

> The current primary target device is PICO 4 Ultra. Standard PICO 4 may run parts of the project, but full MR feature parity is not the current target.

> The submission archive intentionally excludes the third-party OpenCVForUnity asset. As a result, the project is not expected to compile fully until that dependency is restored as described in THIRD_PARTY_COMPONENTS.md.

## 3. Project Structure

```text
Assets/_Project/
├─ Core/                 App state machine, flow control, UI views, runtime bootstrap
├─ Gameplay/
│  ├─ Combat/            Projectiles, remote proxies, shields, wall obstacles
│  └─ Input/             Input abstraction and PICO controller input implementation
├─ MRWorld/              World alignment, AprilTag, Spatial Anchor, remote alignment controllers
├─ Networking/           UDP transport, message definitions, network coordinator
├─ Prefabs/              Prefab assets (many visuals are created at runtime)
├─ Scenes/               Scene files (currently Main.unity)
├─ ScriptableObjects/    Config data objects
├─ Tools/                Utility and build-time scripts
└─ UI/                   Shared 3D UI building blocks
```

## 4. Current Feature Set

### 4.1 Player Battle
- 1v1 Host / Client battle
- Full MR passthrough experience
- Right-hand projectile shooting
- Click-triggered shield activation
- Host-authoritative hit resolution and HP updates
- Result screen with direct rematch flow

### 4.2 Three-Role Runtime
- Host: authoritative server and state owner
- Client: remote player
- Spectator: observer and support role

### 4.3 Spectator Features
- See Host / Client visual bodies, shields, and projectiles
- Vote to heal Host or Client
- Trigger local-only barrage words
- Play local-only cheering / applause sounds
- Place wall obstacles that affect the battle

### 4.4 Wall Obstacle System
- Spectator enters wall placement preview mode
- Host authoritatively spawns and replicates walls
- Walls decay over time
- Walls lose HP when hit by projectiles
- Walls disappear at zero HP
- Walls have a red/black HP bar above them
- Walls show cracks, branch cracks, and chipped corners as HP decreases
- Projectile visuals are destroyed immediately on wall impact and no longer pass through walls visually

## 5. Application State Machine

The project uses `AppStateMachine` as the top-level flow controller. Main states:

- `Boot`
- `MainMenu`
- `RoleSelect`
- `LobbyHost`
- `LobbyClient`
- `LobbySpectator`
- `Calibration`
- `Playing`
- `Result`

Main orchestration lives in [Assets/_Project/Core/M0RuntimeBootstrap.cs](Assets/_Project/Core/M0RuntimeBootstrap.cs).

## 6. Roles and Runtime Flow

### 6.1 Host
1. Enters `LobbyHost`
2. Reads local `Local IP`
3. Waits for both Client and Spectator
4. Presses `Start Match`
5. Enters Calibration
6. Presses final `Confirm` in step 5
7. Enters Playing

### 6.2 Client
1. Enters `LobbyClient`
2. Inputs Host IP
3. Presses `Connect`
4. Waits for Host to start
5. Enters Calibration

### 6.3 Spectator
1. Enters `LobbySpectator`
2. Inputs Host IP
3. Presses `Connect`
4. Waits for Host to start
5. Enters Calibration
6. Uses the spectator control panel during Playing

## 7. Calibration Flow (Current Mainline)

The active mainline flow is a five-step serialized manual calibration process. Only one device can adjust at a time; all others must wait.

1. Client adjusts the Host avatar seen locally
2. Host adjusts the Client avatar seen locally
3. Spectator adjusts the Client avatar seen locally
4. Spectator adjusts the Host avatar seen locally
5. Host performs the final confirmation and starts the match

Each step requires an explicit `Confirm Step`, and the last phase uses Host `Confirm`.

### 7.1 Calibration Input
- Right stick: move on XZ plane
- A / B: move up / down
- Hold X / Y: rotate around the visible head pivot

### 7.2 Calibration Principles
- Each device only adjusts its local view of remote visuals
- Devices outside the current step cannot interfere
- The match cannot begin before Host final confirmation

## 8. Networking

The project does not use Mirror or NGO. It uses a minimal custom UDP LAN layer:

- `UdpLanTransport`: low-level send/receive and Host relay
- `LanMessage`: message structure definitions
- `M3NetworkCoordinator`: high-level runtime event coordinator

### 8.1 Connection Rules
- Host listens on `UDP 27777`
- Client / Spectator connect manually by entering the Host IP
- Lobby UI shows:
  - Host `Local IP`
  - Client / Spectator `Target Host IP`
  - UDP port
  - Last network diagnostic line `Diag`

### 8.2 Main Message Types
- Pose
- Shoot
- Shield
- HpUpdate
- MatchResult
- StartPlaying
- CalibrationReady
- RemoteAlignment
- RematchReady
- SpectatorVote
- ObstacleSpawnRequest
- ObstacleState

## 9. ScriptableObject Config

### 9.1 CombatBalanceConfig
Location: `Assets/_Project/ScriptableObjects/CombatBalanceConfig.cs`

Current key fields:
- `hp = 100`
- `damage = 10`
- `projectileSpeed = 5f`
- `projectileRadius = 0.033f`
- `shootCooldown = 0.5f`
- `shieldDuration = 1f`
- `shieldCooldown = 3f`

### 9.2 SpectatorSupportConfig
Location: `Assets/_Project/ScriptableObjects/SpectatorSupportConfig.cs`

Current key fields:
- `healAmount = 10`
- `voteCooldown = 3f`
- `barrageWordA/B/C`
- `audioVolume = 0.9f`
- `wallMaxHp = 100`
- `wallDecayPerSecond = 5f`
- `wallShotDamage = 10`
- `wallPlacementDistance = 1.4f`
- `wallSpawnCooldown = 2f`
- `wallMaxActiveCount = 2`
- `wallSize = (1.6, 1.35, 0.12)`

## 10. Audio Assets

Default local spectator audio resources:

- `Assets/Resources/Audio/cheer.ogg`
- `Assets/Resources/Audio/yay.ogg`

Audio fallback logic:
- `Cheer` uses configured `cheerClip`, otherwise falls back to `yay.ogg`
- `Applause` uses configured `applauseClip`, otherwise falls back to `cheer.ogg`

## 11. Key Scripts

### Core
- `M0RuntimeBootstrap.cs`: runtime master controller
- `AppStateMachine.cs`: state machine container
- `CalibrationView.cs`: calibration UI
- `LobbyView.cs`: lobby UI
- `SpectatorControlView.cs`: spectator control panel
- `SpectatorBarrageView.cs`: local barrage system
- `SpectatorAudioPlayer.cs`: local audio playback

### Networking
- `LanMessage.cs`: all message payload definitions
- `UdpLanTransport.cs`: UDP transport and Host relay
- `M3NetworkCoordinator.cs`: runtime network coordinator

### Gameplay
- `M1ProjectileShooter.cs`: projectile spawning
- `M1Projectile.cs`: projectile movement and local wall impact cleanup
- `M3RemotePlayerProxy.cs`: remote avatar body and HP bar
- `M5ShieldVisual.cs`: shield visuals
- `WallObstacleRuntime.cs`: wall entity, HP bar, crack visuals

### MRWorld
- `RemoteAlignmentController.cs`: local remote-visual adjustment controller
- `OpenCVForUnityAprilTagDetector.cs`: AprilTag detection
- `SpatialAnchorSyncService.cs`: shared spatial anchor wrapper

## 12. Build and Runtime Notes

### 12.1 Unity Build Recommendations
- Target platform: Android
- Architecture: IL2CPP + ARM64
- If using URP + Vulkan, disable HDR
- Use the PICO Integration SDK path; do not enable OpenXR as the main path

### 12.2 Recommended Device Startup Order
1. Start Host and enter Lobby
2. Read Host `Local IP`
3. Input that IP on Client and press `Connect`
4. Input that IP on Spectator and press `Connect`
5. Once all three roles are connected, Host presses `Start Match`

### 12.3 Pre-Build Checklist
- No red compilation errors in Unity Console
- Main scene is added to Build Settings
- PICO passthrough and permissions are configured
- Audio assets are imported
- ScriptableObject references are checked

## 13. Current Design Boundaries

- The active calibration path is not a shared absolute world anchor. It is a role-specific visual alignment workflow.
- Spectator calibration only affects the spectator's local view and does not override Host-authoritative combat logic.
- `Spatial Anchor` and `AprilTag` capabilities are still present in the project, but the active gameplay flow is currently centered on manual multi-role calibration.
- Standard PICO 4 may be tested, but the intended MR target remains PICO 4 Ultra.

## 14. Potential Future Extensions

- Full shared spatial anchor integration
- More robust AprilTag / multi-marker automatic alignment
- Wall hit sound and stronger impact feedback
- Broadcast barrage for all participants
- More spectator-driven gameplay objects
- Full DebugHUD completion
- Spectator replay / directing mode

## 15. Version Note

Current project build stamp:
- `MR-SPECTATOR-WALL-V1`

This version includes:
- three-role runtime
- five-step serialized calibration
- spectator features
- wall obstacle system
- wall HP bar / cracks / projectile-wall cleanup



