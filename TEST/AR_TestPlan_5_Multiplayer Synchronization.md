
### 5. Multiplayer Synchronization
Verify whether combat data, events, and results remain consistent across all three clients.

**Includes**
- Cast event synchronization
- Projectile position synchronization
- Hit event synchronization
- Damage / HP synchronization
- Shield / Invincible-state synchronization
- Death-state synchronization
- Win / Lose / Draw result synchronization
- Delayed-network scenarios
- Out-of-order arrival scenarios
- Duplicate-delivery scenarios
- Disconnect / reconnect handling
- Mid-session join synchronization

---


## Module: Multiplayer Synchronization

> Notes:  
> - This module is primarily **PlayMode / three-client integration testing**.  
> - It focuses on whether Player A, Player B, and Spectator C observe consistent events, states, and results in the same match.  
> - It is recommended to record logs on all three clients: eventId, timestamp, casterId, targetId, projectileId, HP/Shield before and after changes.

---

#### Function: Cast Event Synchronization (all three clients agree on cast events)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MS-01 A-side cast visible to all three clients|Player A triggers 1 Cast|A/B/C all observe the Cast; it is not visible only locally|A/B/C all observe the Cast| Pass|
|MS-02 B-side cast visible to all three clients|Player B triggers 1 Cast|A/B/C all observe the Cast; source identity is correct|source identity is correct | Pass|
|MS-03 A single Cast is not duplicated in sync|A triggers 1 Cast under normal network conditions|B and C each receive only one corresponding Cast event; no duplicate generation|no duplicate generation| Pass|
|MS-04 High-frequency Cast synchronization completeness|A casts 20 times at valid rhythm|B/C observe the same number of successful casts as A; no obvious loss| no obvious loss| Pass|
|MS-05 Both players Cast at nearly the same moment|A and B cast nearly simultaneously|All three clients observe both casts; ordering follows a stable rule (sequenceId)|ordering follows a stable rule |Pass |

---

#### Function: Projectile State Synchronization (spawn position, orientation, and destruction stay aligned across clients)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MP-01 Initial spawn position synchronization|A fires from a fixed stance and posture|B/C see the Projectile spawn at a position consistent with A's firing point| B/C see the Projectile spawn at a position consistent with A's firing point|Pass |
|MP-02 Flight-path synchronization|Projectile flies for 1–2 seconds|A/B/C see the same trajectory direction, with no obvious drift/teleport on any side| A/B/C see the same trajectory direction, with no obvious drift/teleport on any side| Pass|
|MP-03 Destruction timing synchronization|Projectile is destroyed by hit or timeout|All three clients observe destruction within acceptable sync error; one side does not keep the object after others removed it| All three clients observe destruction within acceptable sync error; one side does not keep the object after others removed it| Pass|
|MP-04 Multiple projectile synchronization|Several projectiles are fired in quick succession|All three clients agree on Projectile count and identity (projectileId), with no mix-up|All three clients agree on Projectile count and identity (projectileId), with no mix-up| Pass|
|MP-05 Spectator is read-only for projectile state|Spectator C only receives battle visuals|C only displays Projectile state and cannot write back or affect A/B projectile logic|C only displays Projectile state and cannot write back or affect A/B projectile logic | Pass|

---

#### Function: Hit / Damage / HP Synchronization (all three clients agree on hit, damage, and state changes)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MD-01 Hit event synchronization|A's Projectile hits B|A/B/C all observe the same Hit; targetId is identical|targetId is identical | Pass|
|MD-02 HP synchronization after damage|B current HP=100; hit damage=30|A/B/C all finally show B HP=70|B HP=70 | Pass|
|MD-03 Shield-change synchronization|B Shield, HP=100; damage=30|A/B/C all finally show no Shield and HP=100|No Shield and HP=100 |Pass |
|MD-04 Death-state synchronization|B HP=10 and receives lethal damage|A/B/C all enter the same Dead state; Death triggers only once| Death triggers only once| Pass|

---

#### Function: Result Synchronization (match-end result is consistent on all three clients)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MR-01 Normal kill end synchronization|A kills B and match ends|A/B/C all show the same result: A win / B lose / C spectator result displayed correctly|A win / B lose / C spectator result displayed correctly | Pass|
|MR-02 Time-up result synchronization|Countdown ends; A HP > B HP|All three clients declare A as winner; no winner mismatch|All three clients declare A as winner | Pass|
|MR-03 Draw result synchronization|Time ends with A HP = B HP, or a rule-defined draw condition is met|All three clients show Draw |All three clients show Draw  | Pass|
|MR-04 Same-frame mutual-kill synchronization|A and B are killed in the same tick|All three clients produce the same rule-defined result; no A-side Draw vs B-side Win mismatch|All three clients produce the same rule-defined result | Pass|
|MR-05 Result lock after match end|A late event arrives after the match has already ended|All three clients ignore or handle the event by rule; final result is not overwritten|All three clients ignore or handle the event by rule |Pass |

---

#### Function: Delay / Jitter / Out-of-Order Delivery (consistency under non-ideal network conditions)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|NL-01 Mild latency|Simulate fixed latency (for example 100–200 ms)|All three clients can still complete a normal match; final HP/result remain consistent|final HP/result remain consistent | Pass|
|NL-02 Jitter scenario|Latency fluctuates in a range (for example 50–250 ms)|Display may be slightly delayed, but logical resolution stays consistent|logical resolution stays consistent | Pass|
|NL-03 Out-of-order arrival|Damage arrives before Hit, or Cast/Spawn order is swapped|System buffers/sorts/discards by rule; no duplicated resolution or state corruption| no duplicated resolution or state corruption|Pass |
|NL-04 Duplicate delivery|The same eventId is sent twice|All three clients process it at most once; HP/state does not change twice|HP/state does not change twice | Pass|
|NL-05 Late event|An expired event arrives after the match has already advanced|Dropped/compensated according to policy; final state is not rolled back incorrectly| final state is not rolled back incorrectly|Pass |

---

#### Function: Disconnect / Reconnect / Mid-Join (session management behaves correctly)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|RC-01 Player disconnects mid-match|During the match, Player B disconnects|System ends by rule; A/C display a consistent disconnect result|A/C display a consistent disconnect result |Pass|
|RC-02 Spectator disconnects mid-match|During the match, Spectator C disconnects|A/B combat continues normally; C disconnect does not affect combat logic| C disconnect does not affect combat logic|Pass |
|RC-03 Spectator reconnect recovery|C reconnects to the current room after disconnect|C recovers the current match state; HP, result, and player positions match the ongoing session| HP, result, and player positions match the ongoing session| Pass|
|RC-04 Mid-join spectator|C joins when the match is already halfway through|C synchronizes to current score/HP/in-scene state, rather than starting from match start|C synchronizes to current score/HP/in-scene state | Pass|

---

#### Function: Role Permissions and Boundaries (identity isolation across three clients)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|RB-01 Spectator cannot Cast|Spectator C performs Trigger / Gesture input|No Cast / Projectile is generated; A/B combat state is unaffected|A/B combat state is unaffected | Pass|
|RB-02 Spectator cannot take damage|Projectile touches C's spectator representation (if any)|No Damage / HP change is triggered; spectator is not part of combat resolution|No Damage / HP change is triggered | Pass|
|RB-03 Correct role mapping|A/B/C join the same match simultaneously|Each role is unique and stable; no A-sees-self-as-B or C-recognized-as-player issue|Each role is unique and stable | Pass|
|RB-04 Correct room-state broadcast|A new match starts, ends, or restarts|A/B/C all receive the same room state; no one-side-started / one-side-not-started mismatch| A/B/C all receive the same room state| Pass|
|RB-05 Old-match event isolation|A new match starts immediately after the previous one ends|Old-match events do not pollute the new match; match/session IDs are isolated correctly|match/session IDs are isolated correctly | Pass|

---

#### Function: Three-Client Consistency Check (key states remain aligned after a full match)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|CK-01 Full-match log alignment|Complete one standard match and export A/B/C logs|Key event sequences can be aligned; event counts and critical fields match|Key event sequences can be aligned |Pass |
|CK-02 Final-state alignment|Check final states after one match|A/B/C final HP, Shield, Dead, Winner, and MatchState are consistent|A/B/C final HP, Shield, Dead, Winner, and MatchState are consistent |Pass|
|CK-03 Replay-result alignment|Replay/analyze the same match logs on all three clients|Replay results stay consistent; no winner mismatch| Replay results stay consistent|Pass |
|CK-04 Multi-round consistency|Play 3–5 matches consecutively|Each match remains independent; all three clients stay consistent in every round|Each match remains independent |Pass |
|CK-05 Consistency after long runtime|Run the system for a long time, then complete a full match|All three clients still stay synchronized; no accumulated drift causes state errors|All three clients still stay synchronized | Pass|

---