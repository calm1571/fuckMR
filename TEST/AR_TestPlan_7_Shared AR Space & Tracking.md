
### 7. Shared AR Space & Tracking
Verify whether all three clients share the same AR space in the same physical environment.

**Includes**
- Initial space alignment
- Same virtual origin across all clients
- Consistent relative player positions
- Spectator sees correct player placement
- Virtual objects do not drift
- Space remains stable during long sessions
- Hand-tracking loss and recovery
- Stability during rapid movement / head turns
- Lighting-condition impact
- Occlusion impact
- Whether the space remains aligned after relocalization

---


## Module: Shared AR Space & Tracking

> Notes:  
> - This module is primarily **PlayMode / co-located three-client integration testing**.  
> - It focuses on whether Player A, Player B, and Spectator C share the same virtual space in the same real environment, preserve stable relative positions, and maintain reliable tracking.  
> - Recommended logs: anchorId, worldOrigin, roomId, matchId, pose(position/rotation), trackingState, relocalizationEvent, timestamp.

---

#### Function: Initial Space Alignment (all three clients share the same virtual origin and reference frame after joining)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|AR-01 First-time three-client alignment|A/B/C join the same room in sequence and complete space initialization|All three clients share the same virtual origin; key reference objects appear in the same place| | |
|AR-02 Consistent relative standing positions for two players and one spectator|A enters from the left, B from the right, C from the rear side|All three clients see A/B/C relative positions matching the real-world arrangement| | |
|AR-03 Correct common-reference calibration|Use the same floor marker / Anchor as a reference point for calibration|The virtual reference point overlaps the same real-world location on all three clients| | |
|AR-04 Join order does not break alignment|A/B join and start the match first, then C joins later|C aligns to the current shared space and does not create an independent coordinate frame| | |
|AR-05 Correct space baseline when a new match starts|Finish one match and start another|The new match still uses the correct shared space with no leftover offset from the previous match| | |

---

#### Function: Consistent Relative Positions of Players and Spectator (all clients agree on where everyone is)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SPC-01 A sees B in the correct position|A and B stand on fixed reference markers|A sees B's virtual position consistent with B's real-world location| | |
|SPC-02 B sees A in the correct position|A and B stand on fixed reference markers|B sees A's virtual position consistent with A's real-world location| | |
|SPC-03 C sees A/B in the correct positions|C observes A and B from the side|C sees A/B relative positions consistent with the physical environment| | |
|SPC-04 Relative positions update correctly after movement|A walks forward, B dodges right, C remains still|All three clients show the correct direction of movement with no obvious inversion or offset| | |
|SPC-05 Correct positions after fast role swapping|A and B swap places while C continues spectating|All three clients show the swap correctly; characters do not get stuck at previous positions| | |

---

#### Function: Virtual Object Stability (projectiles, UI, and reference objects do not drift noticeably)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|ST-01 No obvious drift while standing still|All three clients observe the same virtual reference object while stationary for 30s|Reference object stays stable with no obvious jitter or slow drift| | |
|ST-02 Stable scene during player movement|A/B move slowly within the field while C observes|Virtual objects remain attached to the shared space and do not “float” globally| | |
|ST-03 Stable firing point over time|A fires repeatedly from the same real-world position|Projectile spawn point stays consistent over time; no accumulated offset| | |
|ST-04 Stable follow UI|HP bars / nameplates follow players|UI stays attached to the correct player and does not detach or drift away| | |
|ST-05 Stability after long runtime|Observe key virtual objects again after a long runtime|Shared space remains stable; drift stays within acceptable range| | |

---

#### Function: Tracking State Changes and Recovery (Tracking Lost / Regained / Relocalization)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|TR-01 Recovery after brief tracking loss|One client briefly occludes cameras / causes tracking loss, then recovers|After recovery, the client realigns to the original shared space with no obvious jump| | |
|TR-02 One-side tracking loss does not affect the others|C briefly loses tracking while A/B remain normal|A/B combat and space relation stay normal; C resynchronizes after recovery| | |
|TR-03 Player-side tracking recovery during combat|A briefly loses tracking during the match and then recovers|A can continue fighting afterward; position does not become severely wrong and cause false judgments| | |
|TR-04 Stable behavior across multiple loss/recovery cycles|The same client experiences several tracking lost / regained cycles in one match|System remains stable without accumulating large offset or crashing| | |
|TR-05 Correct relocalization event logging|A relocalization / anchor update occurs|Logs contain a clear recovery event; space relationship is correct after recovery| | |

---

#### Function: Environmental Factors (performance under lighting, occlusion, sparse texture, and complex backgrounds)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|EN-01 Normal lighting environment|Run under ordinary indoor balanced lighting|Shared-space alignment and tracking stay stable on all three clients| | |
|EN-02 Low-light environment|Reduce environmental lighting and run again|Tracking may degrade but should still recover; no severe persistent misalignment| | |
|EN-03 Highly reflective environment|Test in a field with glass / reflective tabletops|System remains basically stable; any impact can be observed and recorded| | |
|EN-04 Occluded environment|Players/spectator partially occlude each other's view|Short-term occlusion does not cause the whole shared space to shift| | |
|EN-05 Sparse-texture or complex-background environment|Run in an area with blank walls or very cluttered backgrounds|System can still establish shared-space alignment; any precision drop stays within tolerance| | |

---

#### Function: Rapid Motion and Extreme Actions (space remains usable during turns, running, pitch changes, and large movements)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MV-01 Fast head turns|A/B/C each turn rapidly left and right|The virtual space does not whip or detach globally; relative positions remain correct after recovery| | |
|MV-02 Fast forward/backward movement|Players rush forward and backward quickly|Tracking remains usable; virtual player positions update correctly| | |
|MV-03 Large pitch-angle observation|Look quickly toward the ground or ceiling|Space anchors remain stable and do not jump obviously| | |
|MV-04 Cast while moving|Player keeps casting while moving|Shared space and firing point remain consistent; movement does not cause firing-point jitter| | |
|MV-05 Spectator observation remains correct after extreme player motion|A/B dodge and turn quickly while C continues observing|C still sees both players and their combat relationship correctly| | |

---

#### Function: Relocalization and Resynchronization (correct results after partial or full recalibration)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|RL-01 Correct alignment after single-client recalibration|Only C relocalizes / rejoins the shared space|C returns to the same coordinate frame as A/B| | |
|RL-02 Correct positions after player-side resynchronization|A resynchronizes the shared space|A again sees B/C in the correct relative positions| | |
|RL-03 Correct alignment after everyone rejoins|A/B/C all leave and re-enter the same room|All three clients rebuild a consistent shared space, matching first-time entry behavior| | |
|RL-04 Resynchronization during a match does not break combat|One client recovers/reconnects/relocalizes during an active match|Combat logic and final result remain correct; after recovery, space stays consistent| | |
|RL-05 Correct object positions after Anchor update|Shared Anchor is refreshed/replaced (if supported)|All objects depending on that Anchor are reattached correctly| | |

---

#### Function: Collision and Logic Boundaries in Shared Space (alignment error should not break combat logic)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|LG-01 Combat logic remains reasonable under small alignment error|Fight while a small amount of space-alignment error exists|Hit logic still broadly matches visual expectation; no obvious “looked like a miss but counted as hit” case| | |
|LG-02 Spectator-side spatial error does not affect combat result|Only C has a small position offset|A/B combat result stays correct; C may show only minor visual deviation| | |
|LG-03 One-side drift does not spread to other clients|A experiences local drift|B/C are not pulled off; the system isolates single-client abnormalities| | |
|LG-04 Logic remains stable after space recovery|Tracking recovers and space is realigned, then combat continues|Subsequent Cast / Hit / Damage and space presentation return to normal| | |
|LG-05 Long-match end still has correct space and result|Complete a relatively long match and inspect afterward|Shared space remains usable and final result matches actual combat progression| | |

---

#### Function: Three-Client Shared-Space Consistency Check (final cross-check after a full match or several rounds)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|CK-01 Key anchor records can be aligned across clients|Export A/B/C anchor and pose logs|Key anchorIds and reference-point relationships can be aligned| | |
|CK-02 Final space relationship is consistent across clients|Record final player positions after one match|All three clients agree on A/B/C relative positions| | |
|CK-03 Shared space remains stable across multiple rounds|Play 3–5 matches consecutively|Each match aligns correctly; no offset residue leaks from old rounds| | |
|CK-04 Re-check after long runtime|Run the system for a long time, then compare reference points again|Shared space remains within acceptable error tolerance| | |
|CK-05 Shared space still recoverable after abnormal conditions|Experience tracking lost / relocalization / reconnect, then finish the match|Final space relationship recovers correctly and does not stay permanently misaligned| | |

---