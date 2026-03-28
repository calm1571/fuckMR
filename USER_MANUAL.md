# User Manual

## 1. Purpose

This manual explains how to use the project as an end user or demonstrator. It focuses on operating the system on PICO devices rather than on development details.

## 2. Intended Setup

The full experience is designed for three PICO headsets in the same physical space:

- 1 Host headset
- 1 Client headset
- 1 Spectator headset

Primary target device:
- PICO 4 Ultra

## 3. Required Conditions

Before use, make sure that:

- all three headsets are on the same local network
- the application is installed on each headset
- the play area is clear and safe
- all participants can see each other in the same physical space
- passthrough permissions have been granted

## 4. Roles

### 4.1 Host
The Host is the authoritative side.

Main responsibilities:
- starts the match
- performs one calibration step
- confirms the final start of the match
- decides authoritative combat results

### 4.2 Client
The Client is the second player.

Main responsibilities:
- connects to the Host using the Host IP
- performs one calibration step
- fights the Host

### 4.3 Spectator
The Spectator watches and supports the match.

Main responsibilities:
- connects to the Host using the Host IP
- performs two calibration steps
- can vote to heal players
- can show local barrage words
- can play local cheering audio
- can place temporary wall obstacles

## 5. Starting the System

### 5.1 Start the Host
1. Launch the application on the Host headset.
2. In the main menu, choose `Start Game`.
3. Select `Host`.
4. In the Host lobby, read the displayed `Local IP`.

### 5.2 Start the Client
1. Launch the application on the Client headset.
2. Choose `Start Game`.
3. Select `Client`.
4. In the lobby, enter the Host `Local IP` into the `Host IP` field.
5. Press `Connect`.

### 5.3 Start the Spectator
1. Launch the application on the Spectator headset.
2. Choose `Start Game`.
3. Select `Spectator`.
4. In the lobby, enter the Host `Local IP` into the `Host IP` field.
5. Press `Connect`.

### 5.4 Start the Match
When Host, Client, and Spectator are all connected, the Host can press `Start Match`.

## 6. Five-Step Calibration Flow

The system uses a serialized five-step calibration flow. Only the device responsible for the current step can adjust anything.

### Step 1
- Current role: Client
- Task: Client adjusts the Host visual position locally
- Action: Client presses `Confirm Step`

### Step 2
- Current role: Host
- Task: Host adjusts the Client visual position locally
- Action: Host presses `Confirm Step`

### Step 3
- Current role: Spectator
- Task: Spectator adjusts the Client visual position locally
- Action: Spectator presses `Confirm Step`

### Step 4
- Current role: Spectator
- Task: Spectator adjusts the Host visual position locally
- Action: Spectator presses `Confirm Step`

### Step 5
- Current role: Host
- Task: Final confirmation
- Action: Host presses `Confirm`

Only after Step 5 does the match enter the Playing state.

## 7. Calibration Controls

During the step where your device is allowed to calibrate, use:

- Right stick: move the selected remote visual on the XZ plane
- A: move upward
- B: move downward
- Hold X: rotate one direction around the visible head pivot
- Hold Y: rotate the opposite direction around the visible head pivot

Devices that are not responsible for the current step cannot interfere.

## 8. Player Controls During the Match

### 8.1 Shooting
- Trigger: shoot a projectile

### 8.2 Shield
- Shield button: activate shield

Gameplay notes:
- the Host decides authoritative hit results
- projectiles can be blocked by shields or temporary wall obstacles
- HP decreases when a valid hit is confirmed

## 9. Result Screen and Replay

At the end of the match:
- the winning player sees `WIN`
- the losing player sees `LOSE`
- the Spectator sees the correct winner result

To start another round:
1. Host and Client both press `Retry`
2. the system resets the match state
3. the next round starts directly in `Playing`

## 10. Spectator Functions

### 10.1 Heal Voting
The Spectator control panel can request healing:

- `Heal Host`
- `Heal Client`

The Host decides the actual HP update. Healing respects cooldown rules.

### 10.2 Local Barrage Words
The Spectator can trigger fixed barrage words such as:
- `COOL`
- `GOOD GAME`
- `NICE SHOT`

These are local-only and appear only on the Spectator headset.

### 10.3 Local Audio
The Spectator can trigger local audio:
- `Cheer`
- `Applause`

These sounds play only on the Spectator headset.

### 10.4 Wall Placement
The Spectator can place a temporary wall obstacle.

How to place a wall:
1. Press `Place Wall`
2. A preview wall appears in front of the Spectator
3. Adjust its position and rotation
4. Confirm placement
5. The Host spawns the authoritative runtime wall

Wall effects:
- projectiles are destroyed on impact
- the wall loses HP over time
- the wall also loses HP when hit by projectiles
- the wall disappears when HP reaches zero

## 11. Wall Preview Controls

When placing a wall preview, use:

- Right stick: move wall preview on XZ
- A: move up
- B: move down
- Hold X / Y: rotate around Y
- Right trigger: confirm placement
- Left trigger: cancel placement

## 12. Visual Indicators

Important visual cues include:
- `L` and `R` markers above remote left/right hands
- `+` marker on the front of the remote head visual
- player HP bars
- wall HP bars
- wall crack progression as wall HP decreases

## 13. Troubleshooting

### 13.1 Devices cannot connect
Check the following:
- all devices are on the same network
- the Host has already entered the Host lobby
- the Host `Local IP` was entered correctly on Client and Spectator
- the lobby diagnostic line `Diag` shows connection progress

### 13.2 Calibration does not continue
Check the following:
- only the current role can confirm the current step
- the correct device is performing the current step
- all three devices are still connected

### 13.3 Visual alignment looks wrong
Check the following:
- use the correct role in the correct step
- rotate around the visible head marker carefully
- make small adjustments instead of large jumps

### 13.4 Wall does not appear
Check the following:
- Spectator used `Place Wall`
- placement was confirmed, not cancelled
- active wall count or cooldown is not blocking the request

## 14. Safety Notes

- Use a clear physical space
- Keep safe distance between participants
- Avoid fast movement near furniture or walls
- Pause the session if passthrough visibility becomes unclear
