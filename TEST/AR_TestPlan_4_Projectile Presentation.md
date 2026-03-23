
### 4. Projectile / Skill Presentation
Verify whether the energy ball is generated, displayed, moved, and destroyed correctly after a cast.

**Includes**
- Spawn timing
- Spawn position
- Spawn orientation
- Flight direction
- Flight speed
- Lifecycle
- Destroy on timeout
- Destroy on max distance
- Correct object count under continuous firing
- Consistent skill presentation across connected clients

---


## Module: Projectile / Skill Presentation

> Notes:  
> - This module is primarily **PlayMode / integration testing** and focuses on actual in-scene projectile behavior after Cast.  
> - When multi-client consistency is involved, it is recommended to record video and logs from the local and remote clients simultaneously.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Spawn-count cases use exact-count judgment: spawned projectile count = expected count, duplicate spawn count = 0.
> - Spawn-position error must stay within `5 cm` of the assigned shoot origin; initial orientation error must stay within `5 deg`.
> - Projectile speed must stay within `+/-10%` of configured speed, and straight-flight lateral drift should stay within `10 cm` over `2 s` when no collision/timeout occurs.
> - Timeout / max-distance removal passes if destruction occurs within `0.2 s` or `10%` of configured lifetime, and within `0.2 m` or `5%` of configured max distance.
> - Cross-client projectile sync target: visible timing error <= `150 ms`, spawn-position delta <= `15 cm`, direction delta <= `10 deg`.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|28 / 28|
|Blocked / N/A|0 / 0|
|Notes|All executed projectile-presentation cases passed against the current build and its quantitative pass criteria.|
---

#### Function: Spawn Timing and Quantity (one Cast produces only the expected number of projectiles)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PJ-01 One Cast spawns one Projectile|Cooldown ready; trigger 1 Cast|Exactly 1 Projectile appears in the scene; only 1 spawn record is produced|Single cast produced exactly one projectile and one spawn record.|Pass|
|PJ-02 No Projectile when Cast is rejected by cooldown|Attempt Cast again before cooldown ends|No Projectile is created; no extra spawn record appears|Cooldown-rejected cast produced no projectile and no extra spawn record.|Pass|
|PJ-03 No Projectile while shooting is disabled|Trigger Cast while shooting is disabled|No Projectile is generated and no duplicate object is created|Disabled-shooting case produced no projectile and no duplicate object.|Pass|
|PJ-04 Correct quantity under high-frequency casting|Cast 20 times at valid cooldown intervals|Spawn count matches the number of successful casts; no missing or extra spawns|High-frequency legal casting kept projectile count aligned with successful casts.|Pass|

---

#### Function: Spawn Position and Orientation (Projectile birth point and direction follow the design)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PS-01 Spawn point is in front of the launcher|Player faces forward and fires from the right hand|Projectile spawns at the expected offset in front of the right hand / firing point, without overlapping the player body|Spawn point appeared at the expected forward offset without body overlap.|Pass|
|PS-02 Spawn point follows assigned shoot origin|Bind `shootOriginOverride` to the right controller transform and fire from different poses|Projectile spawn position follows the assigned shoot origin consistently|Projectile origin consistently followed the assigned shoot-origin transform across poses.|Pass|
|PS-03 Spawn orientation matches aim direction|Player fires forward / diagonally forward / sideways forward|Projectile initial forward matches aim direction; angle error stays within tolerance|Initial projectile direction matched aim direction within the defined angle tolerance.|Pass|
|PS-04 Close-range spawn still uses muzzle offset|Player fires with the controller very close to the body or another surface|Projectile still spawns using the configured muzzle offset from the shoot origin|Close-range spawn still respected the configured muzzle offset from the shoot origin.|Pass|
|PS-05 Correct spawn under fast hand/head motion|Player quickly turns or moves the right hand while triggering cast|Projectile uses the sampled shoot pose at cast time; no obvious jump occurs|Fast motion at cast time still used the sampled pose correctly with no visible jump.|Pass|

---

#### Function: Flight Behavior (direction, speed, and trajectory remain stable)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MV-01 Stable straight flight|No target, no collision; observe for 2 seconds after firing|Projectile moves stably along the designed trajectory; no obvious jitter or drift|Straight-flight behavior remained stable with no abnormal jitter or drift.|Pass|
|MV-02 Correct initial speed|Configured speed = V; measure displacement from first frame to frame N|Actual speed is approximately equal to configured speed, within tolerance|Measured flight speed stayed within the defined tolerance of the configured speed.|Pass|
|MV-03 Not affected by later player movement|After firing, player immediately turns head or moves hand|Projectile continues flying independently and does not follow hand/head motion|Projectile continued independently after firing and did not follow later player motion.|Pass|
|MV-04 Multiple projectiles do not interfere|Fire several projectiles in a short time|Each Projectile moves independently; no attraction, swapping, or state sharing|Multiple projectiles moved independently with no interference or shared-state symptom.|Pass|
|MV-05 Stable trajectory under low FPS|Simulate low frame rate / device pressure|Trajectory remains continuous; no obvious teleporting, rollback, or abnormal jitter|Under reduced frame rate, trajectory remained continuous without teleporting or rollback.|Pass|

---

#### Function: Lifecycle and Destruction (Projectile is removed correctly on timeout or max distance)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LC-01 Timeout destroy when no hit occurs|Projectile hits nothing; lifetime = T|Projectile is auto-destroyed when T is reached; no leftovers remain in the scene|Timeout-based destruction occurred as expected and left no residual object in scene.|Pass|
|LC-02 Max-distance destroy|Projectile lifetime is long enough, but traveled distance reaches maxDistance first|Projectile is destroyed when max distance is reached|Max-distance threshold destroyed the projectile correctly before lifetime expiry.|Pass|
|LC-03 Destruction occurs only once|Timeout and max-distance thresholds are reached near the same frame|Only one destroy path executes; no duplicate-log or double-destroy symptoms occur|Near-simultaneous destroy conditions still executed only one destroy path.|Pass|
|LC-04 No leftover objects after many rounds|Run multiple rounds of firing and ending|No historical Projectile remains in the scene; instance count stays stable over time|Repeated rounds left no lingering projectile objects and instance count stayed stable.|Pass|

---

#### Function: Multi-Client Presentation Consistency (local and remote clients see consistent projectile presentation)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SY-01 Same cast timing across connected clients|Player A triggers Cast; record local and remote screens|Both connected clients can see the Projectile, with timing aligned within acceptable sync error|Both clients observed projectile appearance within the defined sync-timing tolerance.|Pass|
|SY-02 Same spawn position across connected clients|A fires from a fixed stance and fixed posture|Remote client sees the Projectile spawn at a position logically consistent with A's firing point|Cross-client spawn position stayed consistent with the firing point within the defined tolerance.|Pass|
|SY-03 Same flight path across connected clients|Projectile flies in the air for 1-2 seconds|Both clients see the same trajectory direction, with no obvious drift/teleport on either side|Both clients saw consistent projectile flight direction with no abnormal drift or teleport.|Pass|
|SY-04 Same timeout/max-distance disappearance across connected clients|Projectile expires by timeout or max distance|Both clients observe the Projectile disappear without one side persisting far longer than the other|Projectile disappearance stayed aligned across clients within the accepted sync window.|Pass|
|SY-05 Remote shot visual spawning works correctly|Remote client fires once while local client observes|Local client spawns the remote Projectile using transformed remote position and direction correctly|Remote shot visualization spawned correctly on the observing local client.|Pass|

---

#### Function: Edge Cases (Projectile still behaves correctly in extreme space/posture conditions)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|BD-01 Extremely close-range fire|Player fires when very close to the target|Projectile still spawns and resolves correctly; no abnormal repeated hit due to initial overlap|Extreme close-range firing still spawned and resolved correctly without repeated-hit abnormality.|Pass|
|BD-02 Fire toward ground/ceiling|Player fires with a large pitch angle|Projectile flies in the intended direction; no abnormal flipping occurs|Large-pitch firing preserved intended direction and showed no abnormal flipping.|Pass|
|BD-03 Fire while moving|Player walks or sidesteps while continuously casting|Projectile spawn and flight remain stable; not broken by movement interpolation|Movement during casting did not break projectile spawn or flight stability.|Pass|
|BD-04 Fire in occluded environments|Player fires near desks / walls / another player's body|Projectile behavior remains valid; no obvious clipping, sticking, or wrong destruction|Occluded-environment firing remained valid with no obvious clipping, sticking, or wrong destroy behavior.|Pass|
|BD-05 Fire after long runtime|Cast again after the system has been running for a long time|Projectile still spawns, flies, and destroys correctly; no accumulated drift or abnormality|Long-runtime retest still showed correct spawn, flight, and destruction behavior.|Pass|


---

