
### 3. Event Integrity / Replay & Fault Tolerance
Verify whether event data, replay pipelines, and abnormal-input handling are reliable.

**Includes**
- LAN message serialization
- Payload parse safety
- Message type routing
- Local-player message filtering
- Missing / malformed payload handling
- Missing-field tolerance for JSON payloads
- Rematch-ready message handling
- Spectator-vote message handling
- Obstacle-spawn and obstacle-state message handling
- Connection handshake robustness

---


## Module: Event Integrity / Replay & Fault Tolerance

> Notes:  
> - This module is primarily **EditMode / logic-layer and infrastructure testing**.  
> - It focuses on LAN message schema reliability, JSON parsing safety, message routing, and fault tolerance under malformed or unexpected network input in the current build, including the newer rematch, spectator-support, and obstacle-wall message paths.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Serialized float/vector payload fields pass if deserialized values differ by no more than `0.001` per scalar component.
> - For valid message-routing tests, exactly one matching pending payload / request flag pair must be updated; unrelated pending fields must remain unchanged.
> - For malformed / self-sent / unsupported messages, crash count = 0 and unintended state mutation count = 0.
> - Missing or unknown JSON fields may fall back to defaults, but parsing must complete and keep the process alive.
> - Duplicate handshake traffic must not trigger more than one connect callback per side for the same session.
> - Spectator-only message types (`SPECTATOR_VOTE`, `OBSTACLE_SPAWN_REQUEST`) pass only if invalid sender-role cases are rejected without mutating unrelated pending state.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|27 / 27|
|Blocked / N/A|0 / 0|
|Notes|All core LAN-message, rematch, spectator-support, and obstacle-wall message cases were executed and passed.|
---

#### Function: Message Serialization (current LAN payloads remain valid after JsonUtility serialize → deserialize)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SE-01 `ShootPayload` serialization|Serialize and deserialize a `ShootPayload` containing spawn position, direction, speed, maxDistance, lifetime|All fields remain intact within normal floating-point tolerance|Round-trip serialization preserved all shoot fields within the defined float tolerance.|Pass|
|SE-02 `ShieldPayload` serialization|Serialize and deserialize a `ShieldPayload` with active flag and duration|`active` and `duration` remain intact after round-trip serialization|Shield payload fields remained unchanged after serialization round-trip.|Pass|
|SE-03 `HpUpdatePayload` serialization|Serialize and deserialize a `HpUpdatePayload` with host/client HP values|Both HP fields remain unchanged after round-trip serialization|HP update payload preserved both HP values exactly after round-trip serialization.|Pass|
|SE-04 `MatchResultPayload` serialization|Serialize and deserialize a `MatchResultPayload` with `winnerRole`|`winnerRole` remains intact after round-trip serialization|Match result payload preserved `winnerRole` correctly after serialization round-trip.|Pass|
|SE-05 `LanMessage` envelope serialization|Serialize and deserialize a `LanMessage` with `type`, `playerId`, and JSON payload string|Envelope fields remain intact and payload string is preserved|LanMessage envelope fields and payload string were preserved correctly after round-trip serialization.|Pass|
|SE-06 `RematchReadyPayload` serialization|Serialize and deserialize a `RematchReadyPayload` with `ready = true/false`|`ready` remains intact after round-trip serialization|RematchReady payload preserved the `ready` field correctly after round-trip serialization.|Pass|
|SE-07 `SpectatorVotePayload` serialization|Serialize and deserialize a `SpectatorVotePayload` with `targetRole = Host/Client`|`targetRole` remains intact after round-trip serialization|SpectatorVote payload preserved `targetRole` correctly after round-trip serialization.|Pass|
|SE-08 `ObstacleSpawnRequestPayload` serialization|Serialize and deserialize an `ObstacleSpawnRequestPayload` with `anchorType`, `localOffset`, and `yawOffset`|All fields remain intact within normal floating-point tolerance|ObstacleSpawnRequest payload preserved all fields within the defined float tolerance after round-trip serialization.|Pass|
|SE-09 `ObstacleStatePayload` serialization|Serialize and deserialize an `ObstacleStatePayload` with `obstacleId`, transform, size, HP, and `active`|All fields remain intact within normal floating-point tolerance|ObstacleState payload preserved id, transform, size, HP, and active state within the defined tolerance after round-trip serialization.|Pass|

---

#### Function: Message Routing (received message types are mapped to the correct pending payload / request flag)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RT-01 `SHOOT` routes to pending shoot payload|Receive a `LanMessage` with `type = SHOOT` and a valid JSON payload|`_pendingShoot` is populated and `_remoteShootRequested` becomes true|Shoot message populated the correct pending payload/flag pair with no unrelated state changes.|Pass|
|RT-02 `SHIELD` routes to pending shield payload|Receive a `LanMessage` with `type = SHIELD` and a valid JSON payload|`_pendingShield` is populated and `_remoteShieldRequested` becomes true|Shield message routed to the correct pending payload/flag pair only.|Pass|
|RT-03 `HP_UPDATE` routes to pending HP update payload|Receive a `LanMessage` with `type = HP_UPDATE` and a valid JSON payload|`_pendingHpUpdate` is populated and `_remoteHpUpdateRequested` becomes true|HP update message correctly populated the HP pending payload and request flag.|Pass|
|RT-04 `MATCH_RESULT` routes to pending result payload|Receive a `LanMessage` with `type = MATCH_RESULT` and a valid JSON payload|`_pendingMatchResult` is populated and `_remoteMatchResultRequested` becomes true|Match-result message correctly routed to the result pending payload/flag pair.|Pass|
|RT-05 `REMATCH_READY` routes to pending rematch payload|Receive a `LanMessage` with `type = REMATCH_READY` and a valid JSON payload|`_pendingRematchReady` is populated and `_remoteRematchReadyRequested` becomes true|RematchReady message routed to the correct pending payload and request flag with no unrelated state mutation.|Pass|
|RT-06 `SPECTATOR_VOTE` routes only from Spectator sender|Receive a valid `SPECTATOR_VOTE` message whose `senderRole = Spectator`|`_pendingSpectatorVote` is populated and `_remoteSpectatorVoteRequested` becomes true|SpectatorVote message from a valid Spectator sender routed correctly to the spectator-vote pending state.|Pass|
|RT-07 `OBSTACLE_SPAWN_REQUEST` routes only from Spectator sender|Receive a valid `OBSTACLE_SPAWN_REQUEST` message whose `senderRole = Spectator`|`_pendingObstacleSpawnRequest` is populated and `_remoteObstacleSpawnRequestRequested` becomes true|ObstacleSpawnRequest message from a valid Spectator sender routed correctly to the obstacle-spawn pending state.|Pass|
|RT-08 `OBSTACLE_STATE` routes to pending obstacle-state payload|Receive a valid `OBSTACLE_STATE` message from the authoritative sender|`_pendingObstacleState` is populated and `_remoteObstacleStateRequested` becomes true|ObstacleState message correctly populated the obstacle-state pending payload and request flag.|Pass|
|RT-09 Unknown message type is ignored safely|Receive a `LanMessage` whose `type` is unsupported|No crash occurs and no unrelated pending payload is modified|Unsupported message type was ignored safely with no crash and no unintended state mutation.|Pass|

---

#### Function: Fault Tolerance and Parse Safety (malformed, self-sent, or incomplete messages do not break the client)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|EX-01 Malformed outer JSON|Receive bytes that are not valid `LanMessage` JSON|Parsing fails safely; no crash occurs|Malformed outer JSON failed safely without crashing the client.|Pass|
|EX-02 Missing `type` in outer message|Deserialize a `LanMessage` with empty or missing `type`|Message is ignored safely|Message missing `type` was ignored safely with no side effects.|Pass|
|EX-03 Self-sent message is filtered|Receive a valid message whose `playerId` equals `_localPlayerId`|Message is ignored and does not mutate remote state|Self-sent message filtering worked correctly and did not mutate remote state.|Pass|
|EX-04 Malformed payload JSON|Receive a known message type with invalid payload JSON|`FromJson` failure is caught; no crash occurs and pending state is unchanged|Malformed payload JSON was handled safely with no crash and no pending-state corruption.|Pass|
|EX-05 Missing optional / expected payload fields|Receive a known payload JSON missing one or more fields|Deserialization succeeds with default field values, or the message is ignored safely without crashing|Missing-field payloads either fell back safely or were ignored without crashing.|Pass|
|EX-06 Invalid sender role for spectator-only message|Receive `SPECTATOR_VOTE` or `OBSTACLE_SPAWN_REQUEST` from a sender role other than `Spectator`|Message is ignored safely; pending spectator-support state is not mutated|Spectator-only messages from invalid sender roles were ignored safely and did not mutate spectator-support pending state.|Pass|
|EX-07 Invalid or empty target role in `SpectatorVotePayload`|Receive `SPECTATOR_VOTE` with empty or unsupported `targetRole`|Message is ignored safely or left invalid without crashing; unrelated state remains unchanged|Invalid or empty spectator vote target was handled safely without crash or unrelated state mutation.|Pass|
|EX-08 Incomplete `ObstacleStatePayload`|Receive `OBSTACLE_STATE` JSON missing one or more expected fields|Deserialization either falls back safely or is ignored safely without crash|Incomplete obstacle-state payload fell back safely without crash or unintended state corruption.|Pass|
|EX-09 Handshake robustness|Client repeatedly sends `HELLO` before `HELLO_ACK`, or host receives duplicate `HELLO` after already connecting|Connection state stays stable; no crash or repeated connect callback storm occurs|Repeated handshake traffic did not destabilize connection state or trigger callback storms.|Pass|


---

