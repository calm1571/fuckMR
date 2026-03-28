# Build Guide

## 1. Scope

This document describes the release and acceptance build process for the current project. Its goal is to help the team produce a stable Android build, install it on real devices, and complete LAN-based acceptance testing without modifying the code.

Current primary target device:
- PICO 4 Ultra

## 2. Pre-Build Checks

### 2.1 Unity Version
Use the Unity version that has already been validated with this project:
- `Unity 2022.3.62f3`

Do not casually upgrade to another Unity patch or switch to another LTS version immediately before packaging, because that may introduce:
- PICO SDK compatibility issues
- IL2CPP size or compilation changes
- Android build pipeline differences

### 2.2 Target Platform
In `Build Settings`, confirm:
- `Platform = Android`
- the main scene `Assets/_Project/Scenes/Main.unity` is included in `Scenes In Build`

### 2.3 Input and XR Path
The current project uses:
- the PICO Integration SDK path
- a non-OpenXR main runtime path
- some retained XRI/controller components used together with PICO, but not XR Interaction Toolkit as the full gameplay framework

Do not modify these items without evaluation:
- the main XR Plug-in Management settings
- PICO XR provider configuration
- scene objects that bind PICO, camera, and controller components

### 2.4 Rendering and Passthrough
The project uses URP. If Vulkan is used:
- `HDR` must remain disabled

Otherwise this may cause:
- passthrough video to fail
- a black passthrough view
- unstable MR rendering behaviour

### 2.5 Console Check
Before building, confirm the Unity Console has:
- no red compile errors
- no missing scripts
- no newly failed asset imports

Pay particular attention to:
- whether new `.cs` files were imported correctly by Unity
- whether `PXR_ProjectSetting.asset` exists and can be read by the SDK

## 3. Key Resource and Configuration Checks

### 3.1 Audio Assets
Confirm the following files exist:
- `Assets/Resources/Audio/cheer.ogg`
- `Assets/Resources/Audio/yay.ogg`

### 3.2 ScriptableObjects
Confirm the following configuration assets are correctly referenced by the runtime or scene:
- `CombatBalanceConfig`
- `SpectatorSupportConfig`

Check these fields in particular:
- player HP, damage, and cooldown values
- spectator `healAmount` and `voteCooldown`
- wall obstacle HP, decay, damage, size, and maximum count

### 3.3 Host IP Mechanism
The current version uses:
- Host lobby displays `Local IP`
- Client and Spectator manually input `Host IP`

Therefore, there is no longer a need to rely on a hard-coded Host IP inside the scene.

## 4. Recommended Build Settings

### 4.1 Player Settings
Keep the already validated settings unchanged before release:
- `Scripting Backend = IL2CPP`
- `Target Architectures = ARM64`
- Android package name, version, signing, and keystore kept stable

### 4.2 Signing
If the build is used for repeated installation, iterative testing, or overwrite installation:
- the same keystore must be used consistently

Otherwise you may encounter:
- failure to overwrite an existing build
- version management confusion across devices

### 4.3 Build Type
For release or acceptance builds, use:
- `Build` or `Build And Run`

If `Build And Run` causes Android launch issues from the Unity Editor:
- build the APK first
- then install it manually through device file transfer or `adb install -r`

## 5. Recommended Build Procedure

### 5.1 Final Check
1. Open the Unity project
2. Wait for all assets to finish importing
3. Confirm there are no red console errors
4. Confirm the correct build stamp is shown in the code or menu UI

### 5.2 Build Settings
1. Open `File -> Build Settings`
2. Select `Android`
3. Confirm `Main.unity` is listed in `Scenes In Build`
4. Click `Build`

### 5.3 Output Path
Use a dedicated output directory, for example:
- `Builds/Android/`

Recommended file naming includes a version and date, for example:
- `MR-SPECTATOR-WALL-V1_2026-03-23.apk`

## 6. Installation Recommendations

### 6.1 Before Overwriting an Existing Build
If the runtime behaviour does not match the latest code:
- uninstall the old build on the device first
- then install the new build

Reason:
- if the old headset build was not properly replaced, it is easy to misdiagnose the issue as "the code change did not work"

### 6.2 Device Preparation for Three-Role Acceptance
Prepare three devices:
- Host
- Client
- Spectator

Also ensure that:
- all devices are on the same hotspot or router network
- Host starts first and enters the lobby
- Client and Spectator enter the Host `Local IP` manually before connecting

## 7. Recommended Acceptance Flow

### 7.1 Basic Startup Acceptance
Check that:
- the application starts directly in passthrough
- the main menu is visible and scaled correctly
- the build stamp is correct

### 7.2 Network Acceptance
Check that:
- Host lobby shows `Local IP`
- Client and Spectator can input `Host IP`
- all three roles can enter the same room
- Host can only start after both Client and Spectator are connected

### 7.3 Five-Step Calibration Acceptance
Check these steps in order:
1. `ClientAdjustHost`
2. `HostAdjustClient`
3. `SpectatorAdjustClient`
4. `SpectatorAdjustHost`
5. `HostFinalConfirm`

Requirements:
- every step must be confirmed separately
- non-current devices cannot adjust anything
- phase progression must stay synchronized across all devices

### 7.4 Playing Acceptance
Check that:
- Host and Client can both see each other
- projectiles, shields, hits, and HP work correctly
- Spectator can see both player avatars, shields, and projectiles
- the Spectator control panel is available

### 7.5 Spectator Feature Acceptance
Check that:
- `Heal Host / Heal Client`
- local barrage
- local cheer / applause
- `Place Wall`
- wall HP bar, cracks, HP decay, and projectile blocking
all work as expected

### 7.6 Result / Retry Acceptance
Check that:
- the result screen shows `WIN / LOSE`
- after both players press `Retry`, the next round enters `Playing` directly
- the system does not return to Calibration
- the current alignment result is preserved

## 8. Common Issues

### 8.1 Three Devices Cannot Connect
Check first:
- the Host `Local IP`
- the `Host IP` entered on Client and Spectator
- the `Diag` line shown in the lobby

### 8.2 The Runtime Does Not Match the Latest Code
Check first:
- whether the device is still running an old build
- whether overwrite installation actually succeeded
- whether the wrong Unity project directory or an old build artifact was used

### 8.3 Wall HP Bar, Cracks, or Projectile Blocking Do Not Work
Check first:
- whether the current build is actually the latest build
- whether all three devices are running the same version
- whether Spectator created an authoritative runtime wall rather than only remaining in preview mode

## 9. Things That Should Not Be Changed Immediately Before Release

Do not change these items right before release:
- networking message structures
- calibration state machine order
- world root or alignment root relationships
- core wall HP, HP bar, or crack logic
- PICO SDK version or Unity version
- Android signing configuration

## 10. Release Recommendation

If the build is shared externally for testing, also provide:
- device startup order
- Host IP input instructions
- role responsibilities for the three-role setup
- five-step calibration instructions
- a short list of battle and spectator features

It is recommended to distribute the current README together with the test build.
