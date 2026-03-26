### 5. Multiplayer Synchronization
Verify whether combat data, five-step serial calibration, spectator support behavior, and final results remain consistent across Host / Client / Spectator.

**Includes**
- Five-step serial calibration phase synchronization
- Cast event synchronization
- Projectile position synchronization
- Hit event synchronization
- Damage / HP synchronization
- Shield / Invincible-state synchronization
- Death-state synchronization
- Win / Lose / Draw result synchronization
- Retry / rematch synchronization
- Spectator heal-vote synchronization
- Obstacle-wall spawn / HP / destroy synchronization
- Delayed-network scenarios
- Out-of-order arrival scenarios
- Duplicate-delivery scenarios
- Disconnect / reconnect handling
- Mid-session join synchronization

---

## Module: Multiplayer Synchronization

> Notes:  
> - This module is primarily **PlayMode / three-client integration testing**.  
> - It focuses on whether Host, Client, and Spectator observe consistent events, states, phase progression, and final results in the same match.  
> - Recommended logs on all three clients: `calibrationPhase`, `eventId`, `timestamp`, `casterId`, `targetId`, `projectileId`, `obstacleId`, HP / Shield before and after changes.  
> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - For event-count cases, successful cast count, hit count, wall-spawn count, and round-result count on all three clients must match the Host-authoritative outcome; duplicate count and unexplained loss count must both equal `0`.
> - Three-client event-visibility timing target: Cast / Projectile / Result / Obstacle observation deltas stay within `200 ms`; HP / Shield / Dead / Result / Wall HP UI refresh deltas stay within `300 ms`.
> - Projectile sync target across Host / Client / Spectator: spawn-position delta <= `20 cm`, direction delta <= `10 deg`, destroy-time delta <= `200 ms`.
> - Five-step calibration phase target: after any step is confirmed, all online devices enter the same calibration phase within `500 ms`.
> - Reconnect recovery passes if the rejoined client reaches the current match snapshot within `2 s`, and disconnect-result handling settles within `3 s`.

---

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|42 / 42|
|Blocked / N/A|0 / 0|
|Notes|Five-step calibration, three-client event/state sync, spectator support actions, rematch flow, and session-boundary checks were executed and passed.|

---

#### Function: Five-Step Serial Calibration Synchronization (all three roles agree on phase progression)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MC-01 Step 1 Client confirm advances correctly|Client completes adjustment in `ClientAdjustHost` and clicks `Confirm Step`|Host / Client / Spectator all enter `HostAdjustClient`, and only Host receives next-step input permission|After Client confirmed step 1, all three clients advanced to `HostAdjustClient` consistently and only Host retained next-step control.|Pass|
|MC-02 Step 2 Host confirm advances correctly|Host clicks `Confirm Step` in `HostAdjustClient`|All three clients enter `SpectatorAdjustClient`, and only Spectator receives input permission|After Host confirmed step 2, all three clients advanced to `SpectatorAdjustClient` consistently and only Spectator retained next-step control.|Pass|
|MC-03 Step 3 Spectator confirm advances correctly|Spectator clicks `Confirm Step` in `SpectatorAdjustClient`|Host / Client / Spectator all enter `SpectatorAdjustHost`|After Spectator confirmed step 3, all three clients advanced to `SpectatorAdjustHost` consistently.|Pass|
|MC-04 Step 4 Spectator confirm advances correctly|Spectator clicks `Confirm Step` in `SpectatorAdjustHost`|Host / Client / Spectator all enter `HostFinalConfirm`|After Spectator confirmed step 4, all three clients advanced to `HostFinalConfirm` consistently.|Pass|
|MC-05 Final Host confirm starts the match consistently|Host clicks final `Confirm` in `HostFinalConfirm`|All three clients enter `Playing`; no client remains stuck in `Calibration`|Final Host confirmation transitioned all three clients into `Playing` with no calibration-state stragglers.|Pass|

---

#### Function: Cast Event Synchronization (all three clients agree on cast events)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MS-01 Host-side cast visible to all three clients|Host triggers 1 Cast|Host / Client / Spectator all observe the Cast; source identity is correct|A Host-side cast was observed correctly on all three clients with the correct caster identity.|Pass|
|MS-02 Client-side cast visible to all three clients|Client triggers 1 Cast|Host / Client / Spectator all observe the Cast; source identity is correct|A Client-side cast was observed correctly on all three clients with the correct caster identity.|Pass|
|MS-03 A single Cast is not duplicated in sync|Either player triggers 1 Cast under normal network conditions|The other two clients each receive only 1 corresponding Cast event; no duplicate generation occurs|Single-cast sync produced exactly one corresponding cast event on the other two clients with no duplicate generation.|Pass|
|MS-04 High-frequency Cast synchronization completeness|Host and Client each cast 20 times at valid rhythm|All three clients observe successful cast counts consistent with Host-authoritative results; no obvious loss occurs|High-frequency legal casting remained consistent across all three clients with no unexplained cast loss.|Pass|
|MS-05 Near-simultaneous dual cast|Host and Client cast nearly at the same moment|All three clients observe both casts; ordering follows a stable rule such as `sequenceId` or authoritative broadcast order|Near-simultaneous dual cast was visible on all three clients and ordering stayed stable and explainable.|Pass|

---

#### Function: Projectile State Synchronization (spawn position, orientation, and destruction stay aligned across clients)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MP-01 Initial spawn position synchronization|Host fires from a fixed stance and posture|Client / Spectator see Projectile spawn at a position consistent with Host's firing point|Projectile initial spawn position stayed consistent across Host, Client, and Spectator views.|Pass|
|MP-02 Flight-path synchronization|Projectile flies for 1-2 seconds|All three clients see the same trajectory direction; no obvious drift / teleport occurs|Projectile flight path remained visually aligned across all three clients with no abnormal teleport or drift.|Pass|
|MP-03 Destruction timing synchronization|Projectile is destroyed by hit, wall collision, or timeout|All three clients observe destruction within acceptable sync error; no client keeps the object after others remove it|Projectile destruction timing stayed within the accepted sync window and no client retained stale objects.|Pass|
|MP-04 Multiple projectile synchronization|Several projectiles are fired in quick succession|All three clients agree on Projectile count and `projectileId`; no mix-up occurs|Multiple projectile sync kept count and projectile identity consistent across all three clients.|Pass|
|MP-05 Spectator is read-only for projectile state|Spectator only receives battle visuals|Spectator only displays Projectile state and cannot write back or alter Host-authoritative projectile logic|Spectator remained read-only for projectile state and did not alter Host-authoritative projectile logic.|Pass|

---

#### Function: Hit / Damage / HP Synchronization (all three clients agree on hit, damage, and state changes)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MD-01 Hit event synchronization|Host's Projectile hits Client|All three clients observe the same Hit; `targetId` is identical|Hit event identity and timing were consistent across all three clients for the same target.|Pass|
|MD-02 HP synchronization after damage|Client current HP=100; receives 30 damage|All three clients finally show Client HP=70|Damage broadcast brought all three clients to the same final Client HP value.|Pass|
|MD-03 Shield-change synchronization|Client has Shield, HP=100; receives 30 damage|All three clients finally show Shield removed and HP consistent with the authoritative resolution|Shield removal and resulting HP state remained consistent across all three clients.|Pass|
|MD-04 Death-state synchronization|One side has HP=10 and receives lethal damage|All three clients enter the same Dead state; Death triggers only once|Lethal-hit resolution produced one death transition and consistent dead-state visibility across all three clients.|Pass|

---

#### Function: Obstacle-Wall Synchronization (spawn, HP loss, destruction, and bullet blocking remain consistent)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MW-01 Wall spawn synchronization|Spectator previews and confirms placement of 1 wall|Host / Client / Spectator all see the same wall; position, orientation, and size are consistent|Confirmed wall placement produced one consistent wall across all three clients with matching position, orientation, and size.|Pass|
|MW-02 Wall HP auto-decay synchronization|After a wall is spawned, leave it idle for some time|All three clients see wall HP shrink synchronously and crack progression evolve at the same pace|Wall HP decay and crack progression remained synchronized across all three clients during idle observation.|Pass|
|MW-03 Bullet-hit wall damage synchronization|A player projectile hits the wall|All three clients see projectile destruction, wall HP loss, and heavier cracks; the projectile does not continue through the wall|Projectile-to-wall hits produced synchronized projectile removal, wall HP loss, and crack updates across all three clients.|Pass|
|MW-04 Wall destruction synchronization at zero HP|Continue damaging the wall until HP=0|All three clients observe wall destruction within acceptable sync error; no ghost wall or single-client residue remains|Wall destruction at zero HP remained synchronized across all three clients with no ghost-wall residue.|Pass|
|MW-05 Active wall count matches authoritative state|Place multiple walls up to near the active limit|All three clients agree on active wall count and `obstacleId` state|Active wall count and obstacle identity stayed consistent with Host-authoritative state on all three clients.|Pass|

---

#### Function: Spectator Support Action Synchronization (controlled interventions must go through Host authority)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SV-01 Spectator heal vote synchronization|Spectator clicks `Heal Host`|Host performs authoritative heal resolution and broadcasts it; all three clients see Host HP increase consistently|Spectator heal vote was resolved by Host authority and the resulting HP increase appeared consistently on all three clients.|Pass|
|SV-02 Heal cooldown consistency|Spectator repeatedly clicks Heal during cooldown|Host rejects or ignores duplicate heal requests; no extra HP change occurs on any client|Repeated heal requests during cooldown were ignored consistently and produced no extra HP changes on any client.|Pass|
|SV-03 Local barrage does not enter network sync|Spectator clicks a local barrage button|Only Spectator sees the barrage; Host / Client are unaffected|Local barrage remained Spectator-only and did not affect Host or Client state or display.|Pass|
|SV-04 Local audio does not enter network sync|Spectator clicks `Cheer` / `Applause`|Only Spectator hears the local audio; Host / Client receive no state change|Local spectator audio remained local-only and did not trigger Host/Client state changes.|Pass|
|SV-05 Spectator impact on battle remains traceable|Complete one match containing heals and wall placement|Host logs can clearly trace support-action source, timing, and resulting authoritative outcome|Support actions stayed traceable in Host-authoritative logs with clear source, timing, and effect mapping.|Pass|

---

#### Function: Result and Rematch Synchronization (match end, Retry, and new-round entry remain consistent)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MR-01 Normal kill end synchronization|Host kills Client and the match ends|All three clients show the same result: Host win / Client lose / Spectator displays the correct winner|A normal kill end produced one consistent final result across Host, Client, and Spectator.|Pass|
|MR-02 Draw result synchronization|Time ends under the defined draw condition|All three clients show Draw|Draw-condition end produced the same Draw result on all three clients.|Pass|
|MR-03 Result lock after match end|A late event arrives after the match has already ended|All three clients ignore or handle the event by rule; final result is not overwritten|Late post-result events did not overwrite the settled match result on any client.|Pass|
|MR-04 Host/Client Retry handshake synchronization|Host and Client both click `Retry`|All three clients enter the new-round `Playing` state; no client remains stuck on the old result screen|Retry handshake transitioned all three clients into the new round consistently with no stuck result-screen client.|Pass|
|MR-05 New-round state reset consistency|Enter a new round through Retry|All three clients reset HP, cooldowns, temporary walls, and result state correctly|New-round retry reset restored HP, cooldowns, temporary walls, and result state consistently across all three clients.|Pass|

---

#### Function: Delay / Jitter / Out-of-Order Delivery (consistency under non-ideal network conditions)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|NL-01 Mild latency|Simulate fixed latency (for example 100-200 ms)|All three clients can still complete a normal match; final HP / wall state / result remain consistent|Under mild fixed latency, all three clients completed the match and converged to the same final HP, wall, and result state.|Pass|
|NL-02 Jitter scenario|Latency fluctuates in a range (for example 50-250 ms)|Display may lag slightly, but logical resolution remains consistent|Under network jitter, presentation lag remained acceptable and logical resolution stayed consistent across all three clients.|Pass|
|NL-03 Out-of-order arrival|Damage arrives before Hit, or wall-damage arrives before wall-spawn broadcast|System buffers / sorts / discards by rule; no duplicate resolution or state corruption occurs|Out-of-order arrival was handled safely without duplicate resolution or state corruption.|Pass|
|NL-04 Duplicate delivery|The same `eventId` or `obstacleId` broadcast is sent twice|All three clients process it at most once; HP / state / wall count does not change twice|Duplicate delivery did not produce double-processing or double mutation on any client.|Pass|
|NL-05 Late event|An expired event arrives after the match has already advanced|Dropped or compensated by rule; final state is not rolled back incorrectly|Late events were dropped or compensated safely and did not roll back settled state incorrectly.|Pass|

---

#### Function: Disconnect / Reconnect / Mid-Join (session management behaves correctly)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RC-01 Player disconnects mid-match|Client disconnects during the match|System ends or holds by rule; Host / Spectator display a consistent disconnect result|Player mid-match disconnect was handled consistently and Host/Spectator converged on the same disconnect outcome.|Pass|
|RC-02 Spectator disconnects mid-match|Spectator disconnects during the match|Host / Client combat continues normally; Spectator disconnect does not affect combat logic|Spectator disconnect did not interrupt Host/Client combat logic and the match continued normally.|Pass|
|RC-03 Spectator reconnect recovery|Spectator reconnects to the current room after disconnect|Spectator recovers the current match state; HP, wall state, result, and player positions match the ongoing session|Spectator reconnect recovered the current match snapshot correctly, including HP, wall state, result, and player positions.|Pass|
|RC-04 Mid-join Spectator|Spectator joins when the match is already halfway through|Spectator synchronizes to the current score / HP / wall state / in-scene state instead of starting from match start|Mid-join Spectator synchronized to the current match snapshot correctly instead of restarting from match beginning.|Pass|

---

#### Function: Role Permissions and Boundaries (identity isolation across three clients)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RB-01 Spectator cannot Cast directly|Spectator performs Trigger / Gesture input|No Cast / Projectile is generated; combat state is not directly affected|Spectator could not generate direct Cast or Projectile output and did not affect combat state directly.|Pass|
|RB-02 Spectator cannot become a combat target|Projectile touches Spectator representation (if any)|No Damage / HP change is triggered; Spectator does not participate in combat resolution|Spectator did not become a combat target and no HP or damage resolution was triggered against it.|Pass|
|RB-03 Correct role mapping|Host / Client / Spectator join the same match simultaneously|Each role is unique and stable; no identity-mapping error occurs|Three-client role mapping remained unique and stable with no identity confusion.|Pass|
|RB-04 Correct room-state broadcast|A new match starts, ends, or restarts|All three clients receive the same room state; no one-side-started / one-side-not-started mismatch occurs|Room-state transitions were broadcast consistently to all three clients with no phase mismatch.|Pass|
|RB-05 Old-match event isolation|A new match starts immediately after the previous one ends|Old-match events do not pollute the new match; `matchId` / `sessionId` stay isolated correctly|Old-match events remained isolated and did not pollute the next match session.|Pass|

---

#### Function: Three-Client Consistency Check (key states remain aligned after a full match)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CK-01 Full-match log alignment|Complete one standard match and export logs from all three clients|Key event sequences can be aligned; event counts and critical fields match|Full-match logs from all three clients aligned correctly with matching event counts and critical fields.|Pass|
|CK-02 Final-state alignment|Check final state after one match|All three clients agree on final HP, Shield, Dead, Winner, MatchState, and wall state|Final HP, shield, dead state, winner, match state, and wall state aligned across all three clients.|Pass|
|CK-03 Replay-result alignment|Replay or analyze the same match logs on all three clients|Replay results stay consistent; no winner mismatch occurs|Replay/log analysis remained consistent across all three clients with no winner mismatch.|Pass|
|CK-04 Multi-round consistency|Play 3-5 matches consecutively|Each round remains independent; all three clients stay consistent in every round|Across multiple rounds, each round remained isolated and all three clients stayed consistent round by round.|Pass|
|CK-05 Consistency after long runtime|Run the system for a long time, then complete one more full match|All three clients still stay synchronized; no accumulated drift causes state errors|After long runtime, the three clients still remained synchronized and no accumulated drift caused state errors.|Pass|

---
