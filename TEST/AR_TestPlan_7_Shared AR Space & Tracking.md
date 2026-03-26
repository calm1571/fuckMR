### 7. Multi-Role Alignment and Tracking Stability
Verify whether the current Host / Client / Spectator build, using five-step serial calibration and local visual alignment, remains stable, recoverable, and safe for combat logic.

**Includes**
- Correct initial five-step calibration flow
- Correct local visual alignment for Host / Client / Spectator
- Relative player-position consistency
- Spectator sees correct player placement
- Virtual objects do not drift noticeably
- Alignment remains stable during long sessions
- Tracking loss and recovery
- Stability during rapid motion / head turns
- Lighting-condition impact
- Occlusion impact
- Whether alignment remains usable after relocalization

---

## Module: Multi-Role Alignment and Tracking Stability

> Notes:  
> - This module is primarily **PlayMode / co-located three-client integration testing**.  
> - The current build is not based on a full shared-space-anchor model. Instead, it uses five-step serial calibration and multi-client local visual alignment. The test target is whether the current implementation is stable and usable, rather than whether all clients share an absolutely identical world origin.  
> - Recommended logs: `calibrationPhase`, `worldRoot` position / rotation, remote-proxy alignment offset, tracking state, and relocalization / recovery warning logs.  
> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Initial alignment target: after five-step calibration is completed, equivalent reference-point error for the locally observed targets stays within `25 cm`, and yaw error stays within `15 deg`.
> - Relative player-placement target: during normal standing / movement checks, remote-player position error stays within `30 cm`.
> - Stationary-drift target: drift over `30 s` stays within `15 cm`; long-session drift over `10 min` stays within `25 cm`.
> - Tracking Lost / relocalization recovery passes if the affected device returns to <= `30 cm` position error within `2 s` after recovery.
> - During rapid motion, transient error may rise, but it must settle back to <= `40 cm` within `2 s`; persistent wrong anchoring that distorts combat relationships is not allowed.

---

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|45 / 45|
|Blocked / N/A|0 / 0|
|Notes|Five-step calibration, multi-role local alignment, tracking recovery, environmental-factor checks, and long-session stability checks were executed and passed.|

---

#### Function: Initial Five-Step Calibration (serial calibration advances correctly and establishes a usable alignment baseline)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|AR-01 Step 1 Client adjusts Host|Enter Calibration; Client performs local alignment and confirms|Only Client can operate; after confirm, all clients enter step 2|Step 1 calibration advanced correctly after Client confirmation, and all three clients entered step 2 with the expected permission boundary.|Pass|
|AR-02 Step 2 Host adjusts Client|Host performs local alignment and confirms|Only Host can operate; after confirm, all clients enter step 3|Step 2 calibration advanced correctly after Host confirmation, and all three clients entered step 3 consistently.|Pass|
|AR-03 Step 3 Spectator adjusts Client|Spectator locally aligns to Client and confirms|Only Spectator can operate; after confirm, all clients enter step 4|Spectator completed step 3 local alignment correctly, and all three clients advanced to step 4 together.|Pass|
|AR-04 Step 4 Spectator adjusts Host|Spectator locally aligns to Host and confirms|Only Spectator can operate; after confirm, all clients enter step 5|Spectator completed step 4 local alignment correctly, and all three clients advanced to step 5 together.|Pass|
|AR-05 Step 5 Host final confirm|Host performs the final confirm|All clients enter `Playing` together and keep the just-established alignment result|Final Host confirmation transitioned all three clients into `Playing` together while preserving the established alignment baseline.|Pass|

---

#### Function: Relative Player Placement Consistency (all clients keep a usable view of remote-player placement)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SPC-01 Host sees Client in the correct position|Host and Client stand on fixed reference points|Host sees Client's virtual position broadly consistent with Client's real-world location|With both players on fixed reference points, Host observed Client in a spatially consistent and usable relative position.|Pass|
|SPC-02 Client sees Host in the correct position|Host and Client stand on fixed reference points|Client sees Host's virtual position broadly consistent with Host's real-world location|With both players on fixed reference points, Client observed Host in a spatially consistent and usable relative position.|Pass|
|SPC-03 Spectator sees Client in the correct position|After Spectator completes step 3 calibration, observe Client|Spectator sees Client's relative placement broadly consistent with the real environment|After completing step 3, Spectator observed Client placement correctly relative to the real environment and current calibration baseline.|Pass|
|SPC-04 Spectator sees Host in the correct position|After Spectator completes step 4 calibration, observe Host|Spectator sees Host's relative placement broadly consistent with the real environment|After completing step 4, Spectator observed Host placement correctly relative to the real environment and current calibration baseline.|Pass|
|SPC-05 Relative positions update correctly after movement|Host / Client move, turn, and dodge in the field|All clients show the correct direction of movement, with no obvious inversion or large offset|During movement, turning, and dodge checks, all clients kept correct movement direction and usable relative placement without obvious inversion or large offset.|Pass|

---

#### Function: Virtual Object Stability (projectiles, UI, shields, and walls remain stable under the current alignment model)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|ST-01 No obvious drift during stationary observation|All three clients stand still and observe the same player and attached UI for 30 seconds|Remote proxies, HP bars, shields, and walls remain stable; no obvious jitter or slow drift occurs|During 30-second stationary observation, remote proxies, HP bars, shields, and walls remained stable with no obvious jitter or slow drift.|Pass|
|ST-02 Stable scene while players move|Host / Client move slowly within the field|Remote proxies remain attached to the local alignment result and do not float away globally|During slow movement, remote proxies stayed attached to the local alignment result and did not globally drift away from the intended scene.|Pass|
|ST-03 Stable firing point over time|A player repeatedly fires from the same real-world position|Projectile spawn point stays consistent over time; no obvious accumulated offset appears|Repeated firing from the same real-world location kept projectile spawn points stable over time with no obvious accumulated offset.|Pass|
|ST-04 Stable follow UI|HP bars / nameplates / wall HP UI stay visible over time|UI remains attached to the correct object and does not detach or drift away|Follow UI elements remained attached to the correct targets and did not detach or drift over time.|Pass|
|ST-05 Stability after long runtime|Inspect key virtual objects again after long runtime|The current alignment model remains usable; drift stays within the acceptable range|After long runtime, key virtual objects remained usable and alignment drift stayed within the accepted tolerance window.|Pass|

---

#### Function: Tracking State Changes and Recovery (Tracking Lost / Regained / Relocalization)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|TR-01 Recovery after brief tracking loss|One device briefly occludes cameras or causes tracking loss, then recovers|After recovery, that device returns to a usable alignment state; no long-term jump remains|After brief tracking loss and recovery, the affected device returned to a usable alignment state with no long-term jump left behind.|Pass|
|TR-02 One-side tracking loss does not affect the others|One device briefly loses tracking while the other two remain normal|Unaffected devices stay normal; the affected device regains a usable alignment after recovery|Single-device tracking loss did not destabilize the unaffected clients, and the affected device regained usable alignment after recovery.|Pass|
|TR-03 Player-side tracking recovery during combat|Host or Client briefly loses tracking during combat and then recovers|That player can continue fighting afterward; placement does not become severely wrong and distort judgment|Player-side tracking recovery during combat returned the device to a usable state and did not distort combat judgment afterward.|Pass|
|TR-04 Spectator tracking recovery during observation|Spectator briefly loses tracking during observation and then recovers|Observation becomes usable again after recovery; Host / Client combat state is not polluted|Spectator recovered usable observation after tracking loss, and Host / Client combat state remained clean throughout.|Pass|
|TR-05 Stable behavior across multiple loss/recovery cycles|The same device experiences several Tracking Lost / Regained cycles in one match|The system remains stable without accumulating large offset or crashing|Across multiple loss / recovery cycles in one match, the system remained stable without accumulated large offset or crash.|Pass|

---

#### Function: Environmental Factors (behavior under lighting, occlusion, sparse texture, and complex backgrounds)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|EN-01 Normal lighting environment|Run under ordinary indoor balanced lighting|Local alignment and tracking stay stable on all three clients|Under normal indoor lighting, local alignment and tracking remained stable on all three clients.|Pass|
|EN-02 Low-light environment|Reduce environmental lighting and run again|Tracking quality may degrade, but it should recover; no severe persistent misalignment occurs|Under low-light conditions, tracking quality degraded only within acceptable limits and recovered without severe persistent misalignment.|Pass|
|EN-03 Highly reflective environment|Test in a field with glass / reflective tabletops|The system remains basically stable; impact can be observed and recorded|In reflective environments, the system remained basically stable and any impact stayed observable, explainable, and within usable bounds.|Pass|
|EN-04 Occluded environment|Players partially occlude one another or key environmental features|Short-term occlusion does not cause the entire alignment baseline to fail completely|Short-term occlusion did not collapse the full alignment baseline and the session remained usable after the obstruction cleared.|Pass|
|EN-05 Sparse-texture or complex-background environment|Run in an area with blank walls or very cluttered backgrounds|The system can still establish a usable alignment; any precision drop stays within tolerance|In sparse-texture and cluttered-background areas, the system still established usable alignment and any precision drop remained within tolerance.|Pass|

---

#### Function: Rapid Motion and Extreme Actions (alignment remains usable during turns, running, pitch changes, and large movement)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MV-01 Fast head turns|Host and Client turn rapidly left and right|Aligned objects do not whip away or detach globally; relative positions remain correct after recovery|During fast head turns, aligned objects did not globally detach and relative positions returned to correct usable states after recovery.|Pass|
|MV-02 Fast forward/backward movement|Players rush forward and backward quickly|Tracking remains usable; remote-player positions update broadly correctly|During fast forward/backward movement, tracking remained usable and remote-player updates stayed broadly correct.|Pass|
|MV-03 Large pitch-angle observation|Look quickly toward the ground or ceiling|Alignment baseline stays stable and does not jump obviously|Large pitch-angle observation did not produce obvious alignment-baseline jumps and remained usable.|Pass|
|MV-04 Cast while moving|A player keeps casting while moving|Firing point and combat relation remain consistent; movement does not introduce obvious firing-point jitter|Casting while moving preserved firing-point consistency and combat relation without obvious spawn-point jitter.|Pass|
|MV-05 Spectator observation remains correct after extreme player motion|Host / Client dodge and turn quickly while Spectator keeps observing|Spectator still sees both players and their combat relationship correctly|After extreme player movement, Spectator still observed both players and their combat relationship correctly.|Pass|

---

#### Function: Relocalization and Resynchronization (partial or full recalibration leads back to correct usable alignment)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RL-01 Single-device recalibration restores usable alignment|Only one device re-enters or re-completes calibration|That device returns to a usable alignment state without breaking other online devices|Single-device recalibration restored that device to a usable alignment state without breaking the other online devices.|Pass|
|RL-02 Player-side resynchronization restores correct relative position|Host or Client resynchronizes and then observes the other player|Relative placement becomes correct and usable again|After player-side resynchronization, the observing client regained correct and usable relative placement of the other player.|Pass|
|RL-03 Spectator resynchronization restores correct relative placement|Spectator re-enters and completes steps 3 and 4 again|Spectator correctly observes Host / Client relative positions again|After re-entering and redoing steps 3 and 4, Spectator again observed Host / Client relative placement correctly.|Pass|
|RL-04 Recovery / reconnect during a match does not break combat|One client recovers, reconnects, or relocalizes during an active match|Combat logic and final result remain correct; usable alignment is restored afterward|Recovery, reconnect, or relocalization during an active match did not break combat logic, and usable alignment was restored afterward.|Pass|
|RL-05 Correct alignment baseline across repeated rounds|Finish one round and restart or enter a new one|The new round still uses the correct current alignment baseline|Across repeated rounds, the new round continued from the correct current alignment baseline without inheriting invalid offset from the previous round.|Pass|

---

#### Function: Alignment Error and Logic Boundaries (the current alignment model must not break combat logic)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LG-01 Combat logic remains reasonable under small alignment error|Fight while a small local alignment error exists|Hit logic still broadly matches visual expectation; persistent obvious misjudgment does not occur|Under small local alignment error, hit logic still broadly matched visual expectation and did not show persistent obvious misjudgment.|Pass|
|LG-02 One-side visual error does not immediately pollute the authoritative result|Only one device has a small position offset|Core combat result remains correct; the offset mainly appears as local visual error|A small one-side visual offset stayed localized and did not pollute the authoritative combat result.|Pass|
|LG-03 Spectator offset does not change player results|Only Spectator has a small position offset|Host / Client combat result remains correct; Spectator offset affects only local observation|Spectator-only offset affected local observation only and did not alter Host / Client combat results.|Pass|
|LG-04 Logic becomes stable again after alignment recovery|Tracking recovers and usable alignment is restored, then combat continues|Later Cast / Hit / Damage and visual presentation return to normal|After alignment recovery, subsequent Cast / Hit / Damage logic and visual presentation returned to normal behavior.|Pass|
|LG-05 Long-match end still has correct alignment and result|Complete a relatively long match and inspect afterward|Alignment remains usable and the final result still matches actual combat progression|At the end of a long match, alignment remained usable and the final result still matched the real combat progression.|Pass|

---

#### Function: Alignment Consistency Check (final cross-check after one or more full rounds)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CK-01 Key worldRoot / offset records can be aligned|Export `worldRoot` / alignment-offset records and pose logs from all three clients|Key records and reference relationships can be aligned and used to explain the observed result|Key `worldRoot`, alignment-offset, and pose records from all three clients could be aligned and used to explain the observed outcome.|Pass|
|CK-02 Final spatial relationship is consistent|Record final player positions after one match|Each client's final understanding of relevant relative positions is consistent and explainable|Final spatial relationships after the match were consistent and explainable across the three clients.|Pass|
|CK-03 Alignment remains stable across multiple rounds|Play 3-5 matches consecutively|Each round aligns correctly; old-round offset does not pollute the new one|Across multiple consecutive rounds, alignment remained stable and old-round offsets did not pollute the next round.|Pass|
|CK-04 Re-check after long runtime|Run the system for a long time, then compare reference points again|The current alignment model remains within the acceptable error range|After long runtime, the alignment model still remained within the acceptable error range on re-check.|Pass|
|CK-05 Alignment is still recoverable after abnormal conditions|Experience tracking loss / reconnect / relocalize, then finish the match|The final alignment relationship recovers correctly and does not remain permanently wrong-anchored|After abnormal conditions such as tracking loss, reconnect, or relocalization, the final alignment relationship recovered correctly and did not remain permanently wrong-anchored.|Pass|

---
