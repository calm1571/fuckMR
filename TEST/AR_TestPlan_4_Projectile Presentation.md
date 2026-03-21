
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
---

#### Function: Spawn Timing and Quantity (one Cast produces only the expected number of projectiles)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PJ-01 One Cast spawns one Projectile|Cooldown ready; trigger 1 Cast|Exactly 1 Projectile appears in the scene; only 1 spawn record is produced|||
|PJ-02 No Projectile when Cast is rejected by cooldown|Attempt Cast again before cooldown ends|No Projectile is created; no extra spawn record appears|||
|PJ-03 No Projectile while shooting is disabled|Trigger Cast while shooting is disabled|No Projectile is generated and no duplicate object is created|||
|PJ-04 Correct quantity under high-frequency casting|Cast 20 times at valid cooldown intervals|Spawn count matches the number of successful casts; no missing or extra spawns|||

---

#### Function: Spawn Position and Orientation (Projectile birth point and direction follow the design)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PS-01 Spawn point is in front of the launcher|Player faces forward and fires from the right hand|Projectile spawns at the expected offset in front of the right hand / firing point, without overlapping the player body|||
|PS-02 Spawn point follows assigned shoot origin|Bind `shootOriginOverride` to the right controller transform and fire from different poses|Projectile spawn position follows the assigned shoot origin consistently|||
|PS-03 Spawn orientation matches aim direction|Player fires forward / diagonally forward / sideways forward|Projectile initial forward matches aim direction; angle error stays within tolerance|||
|PS-04 Close-range spawn still uses muzzle offset|Player fires with the controller very close to the body or another surface|Projectile still spawns using the configured muzzle offset from the shoot origin|||
|PS-05 Correct spawn under fast hand/head motion|Player quickly turns or moves the right hand while triggering cast|Projectile uses the sampled shoot pose at cast time; no obvious jump occurs|||

---

#### Function: Flight Behavior (direction, speed, and trajectory remain stable)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MV-01 Stable straight flight|No target, no collision; observe for 2 seconds after firing|Projectile moves stably along the designed trajectory; no obvious jitter or drift|||
|MV-02 Correct initial speed|Configured speed = V; measure displacement from first frame to frame N|Actual speed is approximately equal to configured speed, within tolerance|||
|MV-03 Not affected by later player movement|After firing, player immediately turns head or moves hand|Projectile continues flying independently and does not follow hand/head motion|||
|MV-04 Multiple projectiles do not interfere|Fire several projectiles in a short time|Each Projectile moves independently; no attraction, swapping, or state sharing|||
|MV-05 Stable trajectory under low FPS|Simulate low frame rate / device pressure|Trajectory remains continuous; no obvious teleporting, rollback, or abnormal jitter|||

---

#### Function: Lifecycle and Destruction (Projectile is removed correctly on timeout or max distance)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LC-01 Timeout destroy when no hit occurs|Projectile hits nothing; lifetime = T|Projectile is auto-destroyed when T is reached; no leftovers remain in the scene|||
|LC-02 Max-distance destroy|Projectile lifetime is long enough, but traveled distance reaches maxDistance first|Projectile is destroyed when max distance is reached|||
|LC-03 Destruction occurs only once|Timeout and max-distance thresholds are reached near the same frame|Only one destroy path executes; no duplicate-log or double-destroy symptoms occur|||
|LC-04 No leftover objects after many rounds|Run multiple rounds of firing and ending|No historical Projectile remains in the scene; instance count stays stable over time|||

---

#### Function: Multi-Client Presentation Consistency (local and remote clients see consistent projectile presentation)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SY-01 Same cast timing across connected clients|Player A triggers Cast; record local and remote screens|Both connected clients can see the Projectile, with timing aligned within acceptable sync error|||
|SY-02 Same spawn position across connected clients|A fires from a fixed stance and fixed posture|Remote client sees the Projectile spawn at a position logically consistent with A's firing point|||
|SY-03 Same flight path across connected clients|Projectile flies in the air for 1-2 seconds|Both clients see the same trajectory direction, with no obvious drift/teleport on either side|||
|SY-04 Same timeout/max-distance disappearance across connected clients|Projectile expires by timeout or max distance|Both clients observe the Projectile disappear without one side persisting far longer than the other|||
|SY-05 Remote shot visual spawning works correctly|Remote client fires once while local client observes|Local client spawns the remote Projectile using transformed remote position and direction correctly|||

---

#### Function: Edge Cases (Projectile still behaves correctly in extreme space/posture conditions)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|BD-01 Extremely close-range fire|Player fires when very close to the target|Projectile still spawns and resolves correctly; no abnormal repeated hit due to initial overlap|||
|BD-02 Fire toward ground/ceiling|Player fires with a large pitch angle|Projectile flies in the intended direction; no abnormal flipping occurs|||
|BD-03 Fire while moving|Player walks or sidesteps while continuously casting|Projectile spawn and flight remain stable; not broken by movement interpolation|||
|BD-04 Fire in occluded environments|Player fires near desks / walls / another player's body|Projectile behavior remains valid; no obvious clipping, sticking, or wrong destruction|||
|BD-05 Fire after long runtime|Cast again after the system has been running for a long time|Projectile still spawns, flies, and destroys correctly; no accumulated drift or abnormality|||


---

