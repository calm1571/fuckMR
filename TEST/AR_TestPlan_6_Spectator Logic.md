
### 6. Spectator Logic
Verify whether the spectator client can observe correctly and never interfere with combat.

**Includes**
- Spectator can only watch and cannot attack
- Spectator cannot affect HP / Shield / Result
- Spectator can see both players' casts and hits
- Spectator HP display stays consistent with player clients
- Spectator end-of-match result stays consistent with player clients
- Mid-join spectator can sync to the current match state
- Spectator disconnect / recovery
- Spectator UI / camera behavior is correct

---


## Module: Spectator Logic

> Notes:  
> - This module is primarily **PlayMode / three-client integration testing**.  
> - It focuses on whether Spectator C, as a spectator-only endpoint, can observe the match correctly without interfering with Player A / Player B combat logic.  
> - Recommended logs: role, roomId, matchId, eventId, HP/Shield/Dead/Result, joinTime, reconnectTime.

---

#### Function: Spectator Permission Isolation (Spectator can only watch and cannot participate in combat)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SP-01 Trigger input is invalid for spectator|Spectator C presses Trigger|No Cast is triggered; no Projectile is generated; no combat event is emitted| | |
|SP-02 Gesture cast is invalid for spectator|Spectator C performs a casting gesture|No Cast is triggered; no Projectile is generated; no cast record appears in logs| | |
|SP-03 Spectator cannot deal damage|Inject an abnormal attempt for spectator to send a Damage event|System rejects the event; A/B HP values remain unchanged| | |
|SP-04 Spectator cannot become a valid combat target|Projectile overlaps/touches spectator position|No Hit/Damage is resolved for Spectator; spectator is excluded from combat resolution| | |
|SP-05 Spectator cannot affect match result|Spectator performs arbitrary inputs before the match ends|Winner / Loser / Draw result is not affected| | |

---

#### Function: Spectator Visibility (Spectator can fully observe both players' combat)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SV-01 Spectator can see A's cast|Player A triggers 1 Cast|Spectator C can see A's Cast and Projectile presentation| | |
|SV-02 Spectator can see B's cast|Player B triggers 1 Cast|Spectator C can see B's Cast and Projectile presentation| | |
|SV-03 Spectator can see hit feedback|A hits B|Spectator C can see the corresponding hit VFX / damage feedback / state change| | |
|SV-04 Spectator can see both players move|A/B move, turn, and dodge within the arena|Spectator C continuously sees correct relative movement and actions| | |
|SV-05 Spectator can see match end|A kills B, or time-up resolution occurs|Spectator C sees the correct end-of-match presentation and result display| | |

---

#### Function: Spectator State Display (HP / Shield / Dead / Result stay consistent with player clients)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SS-01 HP display consistency|B current HP=100 and is hit by A for damage=30|Spectator C shows B HP=70, consistent with A/B player clients| | |
|SS-02 Shield display consistency|B Shield=20, HP=100; receives 30 damage|Spectator C shows Shield=0 and HP=90, consistent with player clients| | |
|SS-03 Dead-state consistency|B is hit by lethal damage|Spectator C shows B as Dead, consistent with player clients| | |
|SS-04 Win/Lose/Draw consistency|A normal match ends|Spectator C shows the same result as A/B player clients| | |
|SS-05 No obvious UI refresh delay|Multiple hits and state changes occur continuously|Spectator C's UI updates within acceptable synchronization error| | |

---

#### Function: Spectator Mid-Join (a late-joining spectator can sync to the current match state)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SJ-01 Mid-join can see the current state|A/B are already fighting and multiple hits have occurred; C joins|C synchronizes current HP, Shield, Dead state, remaining time, and player positions| | |
|SJ-02 Mid-join can see currently active projectiles|A/B are fighting and projectiles already exist in the arena|C can see currently valid projectiles or the correct current scene state| | |
|SJ-03 Mid-join still gets correct end-of-match result|C joins in the second half of the match; the match later ends|C's final observed result matches the player clients| | |
|SJ-04 Mid-join does not reset the match|C joins while a match is already in progress|A/B combat state is not reset; match does not jump back to the beginning| | |
|SJ-05 Mid-join failure handling|C joins while room state/session is invalid|System provides a clear failure message; A/B match is unaffected| | |

---

#### Function: Spectator Disconnect and Recovery (spectator drop does not disrupt the match; recovery state is correct)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SR-01 Spectator disconnect does not affect player combat|During the match, Spectator C disconnects|A/B combat continues normally; C disconnect does not pause or incorrectly terminate the match| | |
|SR-02 Spectator reconnect restores current match|C reconnects shortly after disconnect|C recovers the current match state rather than restarting from the beginning| | |
|SR-03 Spectator reconnect after a long absence|C reconnects after being offline for a longer period|If the match is still ongoing, C syncs current state; if the match already ended, C enters the correct result screen| | |
|SR-04 Spectator recovery after match end|C reconnects after the match has ended|C directly sees the final result and does not return to an incorrect “in-progress” state| | |
|SR-05 Stability under multiple disconnect/reconnect cycles|C disconnects/reconnects multiple times in one match|System remains stable; no identity confusion, duplicate spectator instance, or crash occurs| | |

---

#### Function: Spectator View and UI (layout, prompts, and camera/view behavior are correct for spectator mode)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SU-01 Spectator UI does not show player-only cast controls|C enters spectator mode|C does not see, or cannot interact with, player-only Cast / Skill controls and prompts| | |
|SU-02 Spectator can see both players' key status UI|C watches the match|C can clearly see A/B HP, Shield, Result, and other key information| | |
|SU-03 Spectator view does not block important information|C watches from the default observation point|UI and camera layout do not block the main battle area| | |
|SU-04 Correct spectator-mode prompts|C joins / disconnects / reconnects / sees match end|UI text clearly indicates Spectator identity and current state| | |
|SU-05 Correct camera switching behavior (if supported)|C switches among free view / fixed view / follow view|Camera behavior matches the design and does not affect player state or synchronization| | |

---

#### Function: Spectator and Shared Space Relation (spectator's AR position and display in the same physical space are correct)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SA-01 Spectator sees correct player placement|A/B stand on the left/right sides of the field and C watches from the rear side|C sees A/B relative positions consistent with the physical environment| | |
|SA-02 Spectator movement preserves a correct view|C walks around in the physical space while observing|C's view updates correctly; A/B virtual positions do not show obvious drift| | |
|SA-03 Spectator approaching players does not break combat display|C moves closer to A or B to observe|C still sees the combat correctly; A/B models and UI remain stable| | |
|SA-04 Spectator occlusion does not change logical results|C stands between A and B and creates real-world occlusion|Only visual visibility is affected; Cast / Hit / Damage logic is unaffected| | |
|SA-05 Long-session spectator-space stability|C spectates for a long time continuously|Spectator-side AR alignment stays stable without accumulating drift| | |

---

#### Function: Spectator Consistency Check (spectator-side outcome aligns with player clients after a full match)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SC-01 Spectator log aligns with player logs|Complete one standard match and export A/B/C logs|C's key observation events can be aligned with A/B events| | |
|SC-02 Spectator final state matches player clients|Check final state after one match|C's HP display, Dead display, Winner/Loser/Draw display match the player clients| | |
|SC-03 Multi-match spectator consistency|Play 3–5 matches in a row while C spectates continuously|Every match result is correct on spectator side; no old-match residue pollutes the new one| | |
|SC-04 Spectator consistency after long runtime|Run the system for a long time, then complete another match|C still sees the correct outcome stably; no accumulated sync deviation| | |
|SC-05 Spectator does not break the system under abnormal conditions|During the match, trigger out-of-order, duplicate delivery, disconnect/recovery, and similar faults|C may briefly show delayed display, but must not corrupt system state or crash it| | |

---