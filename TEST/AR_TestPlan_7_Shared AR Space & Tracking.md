
### 7. Shared AR Space & Tracking
Verify whether the connected host/client devices, and the future spectator endpoint, share the same AR space in the same physical environment.

**Includes**
- Initial space alignment
- Same virtual origin across connected devices
- Consistent relative player positions
- Spectator sees correct player placement
- Virtual objects do not drift
- Space remains stable during long sessions
- Tracking loss and recovery
- Stability during rapid movement / head turns
- Lighting-condition impact
- Occlusion impact
- Whether the space remains aligned after relocalization

---


## Module: Shared AR Space & Tracking

> Notes:  
> - This module is primarily **PlayMode / co-located host-client integration testing**.  
> - It focuses on whether Host and Client share the same virtual space in the same real environment, preserve stable relative positions, and maintain reliable tracking.  
> - Recommended logs: shared anchor uuid (if used), worldRoot position/rotation, room/match identifiers, pose (position/rotation), and key recovery warnings in device logs.
> - Spectator-related items are intentionally kept in this plan as forward-looking coverage for the upcoming third-end role, even if the current build mainly validates Host/Client behavior.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Initial shared-space alignment target: equivalent reference-point error <= `15 cm`, and yaw error <= `10 deg` after calibration settles.
> - Relative player-placement target: remote-player position error <= `20 cm` under normal standing/movement checks.
> - Stationary shared-object drift target: <= `10 cm` over `30 s`; long-session drift target: <= `20 cm` over `10 min`.
> - Tracking-loss / relocalization recovery passes if the affected device returns to <= `20 cm` position error within `2 s` after recovery.
> - During rapid motion, transient error may rise, but must settle back to <= `30 cm` within `2 s`; spectator items use the same thresholds once the third-end role is available.
---

#### Function: Initial Space Alignment (host/client, and later spectator, share the same virtual origin and reference frame after joining)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|AR-01 First-time host/client alignment|Host and Client join the same room and complete calibration|Both devices share the same virtual origin; key reference objects appear in the same place|||
|AR-02 Consistent relative standing positions for two players|A enters from the left and B from the right|Both devices see A/B relative positions matching the real-world arrangement|||
|AR-03 Correct common-reference calibration|Use the same floor marker / shared anchor reference for calibration|The virtual reference point overlaps the same real-world location on both devices|||
|AR-04 Reconnect / rejoin does not break alignment baseline|A/B align first, then one device rejoins the room|The rejoined device aligns to the current shared space instead of creating an independent coordinate frame|||
|AR-05 Correct space baseline when a new match starts|Finish one match and start another|The new match still uses the correct shared space with no leftover offset from the previous match|||
|AR-06 Spectator joins existing shared space correctly|Spectator C joins after Host/Client have already aligned, once the third-end role is available|Spectator aligns to the current shared space instead of creating an independent coordinate frame|||

---

#### Function: Consistent Relative Positions of Players (both devices, and later spectator, agree on where each player is)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SPC-01 A sees B in the correct position|A and B stand on fixed reference markers|A sees B's virtual position consistent with B's real-world location|||
|SPC-02 B sees A in the correct position|A and B stand on fixed reference markers|B sees A's virtual position consistent with A's real-world location|||
|SPC-03 Relative positions update correctly after movement|A walks forward while B dodges right|Both devices show the correct direction of movement with no obvious inversion or offset|||
|SPC-04 Correct positions after fast role swapping|A and B swap places quickly|Both devices show the swap correctly; avatars do not get stuck at previous positions|||
|SPC-05 Remote avatar remains anchored after repeated calibration updates|Host sends repeated worldRoot sync / alignment updates during calibration|Remote avatar remains logically attached to the expected shared-space position|||
|SPC-06 Spectator sees Host/Client in the correct positions|Spectator C observes A/B from a third viewpoint once available|Spectator sees A/B relative positions consistent with the physical environment|||

---

#### Function: Virtual Object Stability (projectiles, UI, and reference objects do not drift noticeably)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|ST-01 No obvious drift while standing still|Both devices observe the same virtual reference object while stationary for 30s|Reference object stays stable with no obvious jitter or slow drift|||
|ST-02 Stable scene during player movement|A/B move slowly within the field|Virtual objects remain attached to the shared space and do not “float” globally|||
|ST-03 Stable firing point over time|A fires repeatedly from the same real-world position|Projectile spawn point stays consistent over time; no accumulated offset|||
|ST-04 Stable follow UI|HP bars / nameplates follow players|UI stays attached to the correct player and does not detach or drift away|||
|ST-05 Stability after long runtime|Observe key virtual objects again after a long runtime|Shared space remains stable; drift stays within acceptable range|||

---

#### Function: Tracking State Changes and Recovery (Tracking Lost / Regained / Relocalization)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|TR-01 Recovery after brief tracking loss|One client briefly occludes cameras / causes tracking loss, then recovers|After recovery, the client realigns to the original shared space with no obvious jump|||
|TR-02 One-side tracking loss does not affect the other device|One device briefly loses tracking while the other remains normal|The unaffected device stays normal; the affected device resynchronizes after recovery|||
|TR-03 Player-side tracking recovery during combat|A briefly loses tracking during the match and then recovers|A can continue fighting afterward; position does not become severely wrong and cause false judgments|||
|TR-04 Stable behavior across multiple loss/recovery cycles|The same client experiences several tracking lost / regained cycles in one match|System remains stable without accumulating large offset or crashing|||
|TR-05 Shared-anchor / worldRoot recovery remains observable|A relocalization or anchor-based recovery occurs|WorldRoot / shared-space relation returns to a usable state after recovery, with observable warning or recovery logs if available|||

---

#### Function: Environmental Factors (performance under lighting, occlusion, sparse texture, and complex backgrounds)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|EN-01 Normal lighting environment|Run under ordinary indoor balanced lighting|Shared-space alignment and tracking stay stable on both devices|||
|EN-02 Low-light environment|Reduce environmental lighting and run again|Tracking may degrade but should still recover; no severe persistent misalignment|||
|EN-03 Highly reflective environment|Test in a field with glass / reflective tabletops|System remains basically stable; any impact can be observed and recorded|||
|EN-04 Occluded environment|Players partially occlude each other's view or the marker area|Short-term occlusion does not cause the whole shared space to shift|||
|EN-05 Sparse-texture or complex-background environment|Run in an area with blank walls or very cluttered backgrounds|System can still establish shared-space alignment; any precision drop stays within tolerance|||

---

#### Function: Rapid Motion and Extreme Actions (space remains usable during turns, running, pitch changes, and large movements)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MV-01 Fast head turns|A and B each turn rapidly left and right|The virtual space does not whip or detach globally; relative positions remain correct after recovery|||
|MV-02 Fast forward/backward movement|Players rush forward and backward quickly|Tracking remains usable; virtual player positions update correctly|||
|MV-03 Large pitch-angle observation|Look quickly toward the ground or ceiling|Space anchors remain stable and do not jump obviously|||
|MV-04 Cast while moving|Player keeps casting while moving|Shared space and firing point remain consistent; movement does not cause firing-point jitter|||
|MV-05 Remote avatar observation remains correct after extreme player motion|A/B dodge and turn quickly while the other device observes|The observing device still sees the other player's combat relationship correctly|||
|MV-06 Spectator observation remains correct after extreme player motion|Spectator C observes while A/B dodge and turn quickly once the third-end role is available|Spectator still sees both players and their combat relationship correctly|||

---

#### Function: Relocalization and Resynchronization (correct results after partial or full recalibration)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RL-01 Correct alignment after single-device recalibration|Only one device relocalizes / rejoins the shared space|That device returns to the same coordinate frame as the other device|||
|RL-02 Correct positions after player-side resynchronization|A resynchronizes the shared space|A again sees B in the correct relative position|||
|RL-03 Correct alignment after both devices rejoin|A/B both leave and re-enter the same room|Both devices rebuild a consistent shared space, matching first-time entry behavior|||
|RL-04 Resynchronization during a match does not break combat|One client recovers/reconnects/relocalizes during an active match|Combat logic and final result remain correct; after recovery, space stays consistent|||
|RL-05 Correct object positions after Anchor update|Shared Anchor is refreshed/replaced (if supported)|All objects depending on that Anchor are reattached correctly|||

---

#### Function: Collision and Logic Boundaries in Shared Space (alignment error should not break combat logic)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LG-01 Combat logic remains reasonable under small alignment error|Fight while a small amount of space-alignment error exists|Hit logic still broadly matches visual expectation; no obvious “looked like a miss but counted as hit” case|||
|LG-02 One-side spatial error does not immediately affect the other device's result view|Only one device has a small position offset|Core combat result remains correct; the offset mainly shows as local visual deviation|||
|LG-03 One-side drift does not spread to the other device|A experiences local drift|B is not pulled off; the system isolates single-device abnormalities as much as possible|||
|LG-04 Logic remains stable after space recovery|Tracking recovers and space is realigned, then combat continues|Subsequent Cast / Hit / Damage and space presentation return to normal|||
|LG-05 Long-match end still has correct space and result|Complete a relatively long match and inspect afterward|Shared space remains usable and final result matches actual combat progression|||
|LG-06 Spectator-side spatial error does not alter combat result|Only Spectator C has a small position offset once the third-end role is available|Host/Client combat result remains correct; spectator offset is limited to local visual deviation|||

---

#### Function: Shared-Space Consistency Check (final cross-check after a full match or several rounds)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CK-01 Key anchor/worldRoot records can be aligned across devices|Export A/B anchor or worldRoot records and pose logs|Key shared-space records and reference-point relationships can be aligned|||
|CK-02 Final space relationship is consistent across devices|Record final player positions after one match|Both devices agree on A/B relative positions|||
|CK-03 Shared space remains stable across multiple rounds|Play 3–5 matches consecutively|Each match aligns correctly; no offset residue leaks from old rounds|||
|CK-04 Re-check after long runtime|Run the system for a long time, then compare reference points again|Shared space remains within acceptable error tolerance|||
|CK-05 Shared space still recoverable after abnormal conditions|Experience tracking lost / relocalization / reconnect, then finish the match|Final space relationship recovers correctly and does not stay permanently misaligned|||
|CK-06 Spectator shared-space consistency check|Run one match with Spectator C connected once the third-end role is available|Host, Client, and Spectator agree on the shared-space relationship within acceptable tolerance|||


---

