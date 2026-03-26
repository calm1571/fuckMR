### 6. Spectator Logic
Verify whether Spectator can observe the match correctly, execute controlled support actions, and avoid directly breaking Host / Client combat authority.

**Includes**
- Spectator cannot directly attack or become a damage target
- Spectator can affect the battle only through Host-authoritative support mechanisms
- Spectator can see both players' casts, hits, shields, and obstacle walls
- Spectator state display remains consistent with player clients
- Spectator heal, wall placement, local barrage, and local audio boundaries are correct
- Mid-join spectator can synchronize to the current match state
- Spectator disconnect / recovery
- Spectator UI / camera behavior is correct
- Spectator local dual-target calibration flow is correct

---

## Module: Spectator Logic

> Notes:  
> - This module is primarily **PlayMode / three-client integration testing**.  
> - It focuses on whether Spectator, as a viewing-and-support role, can observe correctly, execute controlled support actions, and preserve the Host-authoritative model.  
> - Recommended logs: `role`, `roomId`, `matchId`, `eventId`, `healVote`, `obstacleId`, HP / Shield / Dead / Result, `joinTime`, `reconnectTime`.  
> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Permission isolation uses zero-tolerance judgment: accepted Spectator-originated attack / damage-target events = `0`.
> - Controlled support actions pass only if every HP or obstacle-wall change triggered by Spectator can be found in Host-authoritative logs.
> - Spectator UI / state sync target: HP / Shield / Dead / Result / Wall HP display deltas relative to player clients stay within `300 ms`.
> - Mid-join or reconnect passes if Spectator reaches the current match snapshot within `2 s`; post-match reconnect must reach the correct result screen within `3 s`.
> - Spectator local-calibration target: after steps 3 and 4 of the five-step serial calibration are completed, Spectator's observed Host / Client relative-placement error stays within `25 cm`, and stationary drift over `30 s` stays within `15 cm`.

---

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|45 / 45|
|Blocked / N/A|0 / 0|
|Notes|Spectator authority boundaries, visibility, support actions, mid-join/reconnect, UI behavior, and end-to-end three-client consistency checks were executed and passed.|

---

#### Function: Spectator Permission Boundaries (Spectator cannot directly participate in player combat)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SP-01 Trigger input is invalid for Spectator|Spectator presses Trigger|No Cast is triggered; no player Projectile is generated; no attack event is emitted|Spectator trigger input did not produce Cast, Projectile, or attack events in any observed match state.|Pass|
|SP-02 Spectator cannot directly deal damage|Inject an abnormal attempt for Spectator to send Damage / Hit events|System rejects the event; Host / Client HP does not change directly|Abnormal Spectator-originated damage attempts were rejected and Host / Client HP stayed unchanged by direct spectator input.|Pass|
|SP-03 Spectator cannot become a valid combat target|Projectile overlaps or touches the Spectator representation|No Hit / Damage is resolved for Spectator; Spectator is excluded from combat resolution|Projectile overlap with the spectator representation did not trigger hit resolution or spectator damage state.|Pass|
|SP-04 Spectator cannot directly rewrite the match result|Before the match ends, Spectator performs arbitrary non-support input|Winner / Loser / Draw is not directly affected|Non-support spectator input did not alter the authoritative winner / loser / draw outcome before match end.|Pass|
|SP-05 Authority boundary remains traceable|Complete one match containing Spectator interventions|All state changes that truly affect the battle can be traced back in Host-authoritative logs|All battle-affecting spectator interventions remained traceable in Host-authoritative logs with clear source and timing.|Pass|

---

#### Function: Spectator Visibility (Spectator can fully observe both players' combat)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SV-01 Spectator can see Host's cast|Host triggers 1 Cast|Spectator can see Host's Cast and Projectile presentation|Host cast and projectile presentation were visible on the spectator client with the correct source identity.|Pass|
|SV-02 Spectator can see Client's cast|Client triggers 1 Cast|Spectator can see Client's Cast and Projectile presentation|Client cast and projectile presentation were visible on the spectator client with the correct source identity.|Pass|
|SV-03 Spectator can see hit feedback|Host hits Client|Spectator can see corresponding hit feedback, damage presentation, and state change|Spectator observed hit feedback, damage presentation, and resulting state change consistently with player clients.|Pass|
|SV-04 Spectator can see shields|Host or Client activates shield|Spectator can see shield appearance, duration, and disappearance for the correct player|Spectator observed shield appearance, active duration, and disappearance on the correct player side.|Pass|
|SV-05 Spectator can see obstacle walls|Spectator places 1 wall, or a wall already exists in the field|Spectator can see the runtime wall, wall HP UI, cracks, and destruction|Runtime wall, wall HP UI, crack progression, and destruction all remained visible on spectator correctly.|Pass|

---

#### Function: Spectator State Display (HP / Shield / Dead / Result stay consistent with player clients)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SS-01 HP display consistency|Client current HP=100 and is hit by Host for 30 damage|Spectator shows Client HP=70, consistent with player clients|Spectator HP UI updated to the same final Client HP as Host and Client after authoritative damage resolution.|Pass|
|SS-02 Shield display consistency|Client activates shield and is then attacked|Spectator shows shield changes consistent with player clients|Spectator shield UI stayed aligned with player clients during shield activation, consumption, and removal.|Pass|
|SS-03 Dead-state consistency|Client receives lethal damage|Spectator shows Client as Dead, consistent with player clients|Lethal damage produced the same dead-state display on spectator and player clients.|Pass|
|SS-04 Win / Lose / Draw consistency|Complete one normal match|Spectator shows the same result as player clients|Spectator final result display matched the Host / Client result outcome for the completed match.|Pass|
|SS-05 Wall HP UI consistency|A wall loses HP over time and is hit by projectiles|Spectator sees wall HP UI consistent with player clients|Spectator wall HP UI stayed consistent with the player clients during wall decay and projectile hits.|Pass|

---

#### Function: Controlled Spectator Support Actions (Spectator may affect the battle, but only through Host authority)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SA-01 Heal Host request works|Spectator clicks `Heal Host`|Host performs authoritative healing and broadcasts it; all three clients observe Host HP change consistently|Heal Host requests were resolved by Host authority and the resulting HP increase stayed consistent on all three clients.|Pass|
|SA-02 Heal Client request works|Spectator clicks `Heal Client`|Host performs authoritative healing and broadcasts it; all three clients observe Client HP change consistently|Heal Client requests were resolved by Host authority and the resulting HP increase stayed consistent on all three clients.|Pass|
|SA-03 Heal cooldown works correctly|Spectator repeatedly clicks Heal during cooldown|Host rejects or ignores duplicate requests; HP does not change again|Repeated heal input during cooldown was ignored correctly and did not produce extra HP changes.|Pass|
|SA-04 Place Wall preview and confirm are separated|Spectator enters wall-placement preview and then cancels or confirms|Cancel does not generate a real wall; confirm generates the authoritative wall|Wall placement preview remained non-authoritative until confirm; cancel produced no runtime wall and confirm produced one authoritative wall.|Pass|
|SA-05 Obstacle wall takes effect through Host authority|Spectator places a wall and players continue fighting|The wall affects both players' projectiles, while spawn / HP loss / destruction all remain Host-authoritative|Placed walls affected projectile blocking correctly while wall spawn, HP loss, and destruction remained Host-authoritative throughout.|Pass|

---

#### Function: Spectator Local-Only Behaviors (visible / audible only to Spectator locally)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SL-01 Local barrage is visible only to Spectator|Spectator clicks any barrage button|Only Spectator sees the floating text; Host / Client are unaffected|Barrage floating text remained visible only on the spectator client and did not appear on Host / Client.|Pass|
|SL-02 Local audio is audible only to Spectator|Spectator clicks `Cheer` / `Applause`|Only Spectator hears the local audio; Host / Client receive no extra audio or state change|Local spectator audio played only on the spectator device and did not affect Host / Client audio or state.|Pass|
|SL-03 Local barrage does not enter battle state|During a match, Spectator repeatedly triggers local barrage|HP, shield, result, wall state, and authoritative battle logs remain unaffected|Repeated local barrage usage did not alter HP, shield, result, wall state, or Host-authoritative battle logs.|Pass|
|SL-04 Local audio missing-resource handling|Remove or fail to configure audio assets, then click the button|UI shows the correct Ready / Missing status; the system does not crash|Missing local-audio resources were handled safely with correct UI status and no crash.|Pass|
|SL-05 Local-only features remain stable over long runtime|Trigger barrage and audio repeatedly across multiple matches|No memory leak, UI residue, or combat-state pollution occurs|Long-running repeated barrage and local-audio use stayed stable without UI residue, leaks, or combat-state pollution.|Pass|

---

#### Function: Spectator Mid-Join (a late-joining spectator can synchronize to the current match state)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SJ-01 Mid-join can see the current match state|Host / Client are already fighting and multiple hits have occurred; Spectator joins|Spectator synchronizes current HP, Shield, remaining time, wall state, and player positions|Mid-join spectator synchronized to the current HP, shield, remaining time, wall state, and player positions without needing a room restart.|Pass|
|SJ-02 Mid-join can see currently active projectiles|Projectiles already exist in the field during an active match|Spectator can see currently valid projectiles or the correct current scene state|Mid-join spectator observed the correct current projectile / scene state after joining an active match.|Pass|
|SJ-03 Mid-join still gets the correct final result|Spectator joins during the second half of a match; the match later ends|Spectator's final observed result matches the player clients|A spectator who joined mid-match still received the same final result as the player clients at match end.|Pass|
|SJ-04 Mid-join does not reset the match|Spectator joins while a match is already in progress|Host / Client combat state is not reset; the match does not jump back to the beginning|Spectator mid-join did not reset Host / Client combat state or roll the match back to the beginning.|Pass|
|SJ-05 Mid-join local calibration flow is correct|Spectator joins mid-match and enters calibration|Spectator can complete calibration steps 3 and 4 in order without breaking the authority relationships established in earlier steps|Mid-join spectator completed local steps 3 and 4 calibration in order without breaking the authority relationships from the prior steps.|Pass|

---

#### Function: Spectator Disconnect and Recovery (spectator drop does not disrupt the match; recovery state is correct)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SR-01 Spectator disconnect does not affect player combat|Spectator disconnects during the match|Host / Client combat continues normally; Spectator disconnect does not pause or incorrectly terminate the match|Spectator disconnect did not pause, terminate, or otherwise interfere with ongoing Host / Client combat.|Pass|
|SR-02 Spectator reconnect restores the current match|Spectator reconnects shortly after disconnect|Spectator restores the current match state rather than restarting from match beginning|Short-gap spectator reconnect restored the current match snapshot instead of restarting from match beginning.|Pass|
|SR-03 Spectator reconnect after a long absence|Spectator reconnects after being offline for a longer time|If the match is still ongoing, Spectator syncs the current state; if the match already ended, Spectator enters the correct result screen|Long-gap reconnect correctly restored either the live match snapshot or the settled result screen depending on current match state.|Pass|
|SR-04 Spectator reconnect after match end|Spectator reconnects after the match has already ended|Spectator directly sees the final result and does not return to an incorrect in-progress state|Post-match spectator reconnect entered the correct final-result state directly with no false in-progress session state.|Pass|
|SR-05 Stability under multiple disconnect/reconnect cycles|Spectator disconnects and reconnects multiple times in one match|The system remains stable; no identity confusion, duplicate Spectator instance, or crash occurs|Multiple spectator disconnect / reconnect cycles remained stable with no duplicate spectator identity, confusion, or crash.|Pass|

---

#### Function: Spectator View and UI (layout, prompts, and camera behavior are correct for spectator mode)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SU-01 Spectator does not show player-only combat controls|Spectator enters `Playing`|Spectator does not show, or cannot operate, player-only Cast / Shield combat controls|Spectator mode did not expose usable player-only Cast / Shield controls in Playing state.|Pass|
|SU-02 Spectator can see both players' key status UI|Spectator watches the match|Spectator can clearly see Host / Client HP, shield, result, and wall HP UI|Spectator UI clearly presented both players' HP, shield, result, and wall HP information throughout observation.|Pass|
|SU-03 Spectator view does not block important information|Spectator watches from the default observation point|UI and camera layout do not block the main battle area|Default spectator UI and camera layout did not block the main battle area or key runtime state.|Pass|
|SU-04 Correct spectator-mode prompts|Join / disconnect / reconnect / match end / wall-placement preview occur|UI text clearly indicates Spectator identity and current state|Spectator-facing prompts correctly reflected identity and current state during join, disconnect, reconnect, match end, and wall preview.|Pass|
|SU-05 Spectator control panel behavior is correct|During `Playing`, repeatedly open and use the control panel|Heal, Barrage, Audio, and Place Wall entry states are clear and do not get confused with player HUD|Spectator control panel entries remained clear, stable, and visually distinct from the player HUD during repeated use.|Pass|

---

#### Function: Spectator Consistency Check (spectator-side outcome aligns with player clients after a full match)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SC-01 Spectator logs align with player logs|Complete one standard match and export logs from all three clients|Spectator-side key observation events can be aligned with Host / Client events|Spectator observation logs aligned with Host / Client logs on key events, timings, and authoritative outcomes.|Pass|
|SC-02 Spectator final state matches player clients|Check final state after one match|Spectator HP display, winner/loser/draw display, and final wall state match player clients|Final spectator HP display, result display, and wall end state matched the player clients exactly.|Pass|
|SC-03 Multi-match spectator consistency|Spectator watches 3-5 matches in a row|Every match result is correct on Spectator side; old-match residue does not pollute the new one|Across consecutive matches, spectator results stayed correct and no old-match residue polluted the next round.|Pass|
|SC-04 Spectator consistency after long runtime|Run the system for a long time, then complete another match|Spectator still observes the correct outcome stably; no accumulated sync deviation occurs|After long runtime, spectator still observed correct outcomes stably with no accumulated sync deviation.|Pass|
|SC-05 Spectator does not break the system under abnormal conditions|Trigger out-of-order, duplicate delivery, disconnect/recovery, and similar faults during the match|Spectator may briefly display delay, but must not corrupt system state or crash it|Under abnormal delivery and reconnect conditions, spectator remained resilient and did not corrupt system state or crash the session.|Pass|

---
