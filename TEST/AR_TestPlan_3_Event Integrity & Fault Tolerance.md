
### 3. Event Integrity / Replay & Fault Tolerance
Verify whether event data, replay pipelines, and abnormal-input handling are reliable.

**Includes**
- LAN message serialization
- Payload parse safety
- Message type routing
- Local-player message filtering
- Missing / malformed payload handling
- Missing-field tolerance for JSON payloads
- Connection handshake robustness

---


## Module: Event Integrity / Replay & Fault Tolerance

> Notes:  
> - This module is primarily **EditMode / logic-layer and infrastructure testing**.  
> - It focuses on LAN message schema reliability, JSON parsing safety, message routing, and fault tolerance under malformed or unexpected network input in the current build.

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
---

#### Function: Message Serialization (current LAN payloads remain valid after JsonUtility serialize → deserialize)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SE-01 `ShootPayload` serialization|Serialize and deserialize a `ShootPayload` containing spawn position, direction, speed, maxDistance, lifetime|All fields remain intact within normal floating-point tolerance|||
|SE-02 `ShieldPayload` serialization|Serialize and deserialize a `ShieldPayload` with active flag and duration|`active` and `duration` remain intact after round-trip serialization|||
|SE-03 `HpUpdatePayload` serialization|Serialize and deserialize a `HpUpdatePayload` with host/client HP values|Both HP fields remain unchanged after round-trip serialization|||
|SE-04 `MatchResultPayload` serialization|Serialize and deserialize a `MatchResultPayload` with `winnerRole`|`winnerRole` remains intact after round-trip serialization|||
|SE-05 `LanMessage` envelope serialization|Serialize and deserialize a `LanMessage` with `type`, `playerId`, and JSON payload string|Envelope fields remain intact and payload string is preserved|||

---

#### Function: Message Routing (received message types are mapped to the correct pending payload / request flag)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RT-01 `SHOOT` routes to pending shoot payload|Receive a `LanMessage` with `type = SHOOT` and a valid JSON payload|`_pendingShoot` is populated and `_remoteShootRequested` becomes true|||
|RT-02 `SHIELD` routes to pending shield payload|Receive a `LanMessage` with `type = SHIELD` and a valid JSON payload|`_pendingShield` is populated and `_remoteShieldRequested` becomes true|||
|RT-03 `HP_UPDATE` routes to pending HP update payload|Receive a `LanMessage` with `type = HP_UPDATE` and a valid JSON payload|`_pendingHpUpdate` is populated and `_remoteHpUpdateRequested` becomes true|||
|RT-04 `MATCH_RESULT` routes to pending result payload|Receive a `LanMessage` with `type = MATCH_RESULT` and a valid JSON payload|`_pendingMatchResult` is populated and `_remoteMatchResultRequested` becomes true|||
|RT-05 Unknown message type is ignored safely|Receive a `LanMessage` whose `type` is unsupported|No crash occurs and no unrelated pending payload is modified|||

---

#### Function: Fault Tolerance and Parse Safety (malformed, self-sent, or incomplete messages do not break the client)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|EX-01 Malformed outer JSON|Receive bytes that are not valid `LanMessage` JSON|Parsing fails safely; no crash occurs|||
|EX-02 Missing `type` in outer message|Deserialize a `LanMessage` with empty or missing `type`|Message is ignored safely|||
|EX-03 Self-sent message is filtered|Receive a valid message whose `playerId` equals `_localPlayerId`|Message is ignored and does not mutate remote state|||
|EX-04 Malformed payload JSON|Receive a known message type with invalid payload JSON|`FromJson` failure is caught; no crash occurs and pending state is unchanged|||
|EX-05 Missing optional / expected payload fields|Receive a known payload JSON missing one or more fields|Deserialization succeeds with default field values, or the message is ignored safely without crashing|||
|EX-06 Handshake robustness|Client repeatedly sends `HELLO` before `HELLO_ACK`, or host receives duplicate `HELLO` after already connecting|Connection state stays stable; no crash or repeated connect callback storm occurs|||


---

