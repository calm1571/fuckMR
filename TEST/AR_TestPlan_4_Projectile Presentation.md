
### 4. Projectile / Skill Presentation
Verify whether the energy ball is generated, displayed, moved, and destroyed correctly after a cast.

**Includes**
- Spawn timing
- Spawn position
- Spawn orientation
- Flight direction
- Flight speed
- Lifecycle
- Destroy on hit
- Destroy on timeout
- Correct object count under continuous firing
- Consistent skill presentation across all three clients

---


## Module: Projectile / Skill Presentation

> Notes:  
> - This module is primarily **PlayMode / integration testing** and focuses on actual in-scene projectile behavior after Cast.  
> - When three-client consistency is involved, it is recommended to record video and logs from Player A / Player B / Spectator C simultaneously.

---

#### Function: Spawn Timing and Quantity (one Cast produces only the expected number of projectiles)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|PJ-01 One Cast spawns one Projectile|Cooldown ready; trigger 1 Cast|Exactly 1 Projectile appears in the scene; only 1 spawn record is produced| only 1 spawn record is produced | Pass|
|PJ-02 No Projectile when Cast is rejected by cooldown|Attempt Cast again before cooldown ends|No Projectile is created; no extra spawn record appears|no extra spawn record appears | Pass|
|PJ-03 Correct count after same-frame input merge|Same frame: Trigger + Gesture both resolve to Cast|At most 1 Projectile is generated; no duplicate object is created|no duplicate object is created | Pass|
|PJ-04 Correct quantity under high-frequency casting|Cast 20 times at valid cooldown intervals|Spawn count matches the number of successful casts; no missing or extra spawns|no missing or extra spawns | Pass|

---

#### Function: Spawn Position and Orientation (Projectile birth point and direction follow the design)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|PS-01 Spawn point is in front of the launcher|Player faces forward and fires from the right hand|Projectile spawns at the expected offset in front of the right hand / firing point, without overlapping the player body|Projectile spawns at the expected offset in front of the right hand |Pass |
|PS-02 Correct left/right-hand spawn points|Cast using left hand and right hand respectively|Projectile spawns from the corresponding hand-specific firing point; left/right are not swapped|left/right are not swapped | Pass|
|PS-03 Spawn orientation matches aim direction|Player fires forward / diagonally forward / sideways forward|Projectile initial forward matches aim direction; angle error stays within tolerance|Projectile initial forward matches aim direction; angle error stays within tolerance |Pass |
|PS-04 No spawning inside wall/body at close range|Player fires with hand near a wall or body collider|Projectile does not spawn inside wall/body; the cast is rejected if needed|Projectile does not spawn inside wall/body |Pass |
|PS-05 Correct spawn under fast hand/head motion|Gesture-triggered cast while the player quickly turns or swings hand|Projectile uses the correct sampled pose at cast time; no obvious jump occurs|Projectile uses the correct sampled pose at cast time | Pass|

---

#### Function: Flight Behavior (direction, speed, and trajectory remain stable)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MV-01 Stable straight flight|No target, no collision; observe for 2 seconds after firing|Projectile moves stably along the designed trajectory; no obvious jitter or drift|Projectile moves stably along the designed trajectory; no obvious jitter or drift |Pass |
|MV-02 Correct initial speed|Configured speed = V; measure displacement from first frame to frame N|Actual speed is approximately equal to configured speed, within tolerance|Actual speed is approximately equal to configured speed, within tolerance |Pass |
|MV-03 Not affected by later player movement|After firing, player immediately turns head or moves hand|Projectile continues flying independently and does not follow hand/head motion|Projectile continues flying independently and does not follow hand/head motion |Pass |
|MV-04 Multiple projectiles do not interfere|Fire several projectiles in a short time|Each Projectile moves independently; no attraction, swapping, or state sharing|Each Projectile moves independently; no attraction, swapping, or state sharing | Pass|
|MV-05 Stable trajectory under low FPS|Simulate low frame rate / device pressure|Trajectory remains continuous; no obvious teleporting, rollback, or abnormal jitter|Trajectory remains continuous; no obvious teleporting, rollback, or abnormal jitter |Pass |

---

#### Function: Lifecycle and Destruction (Projectile is removed correctly on hit, timeout, or max distance)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|LC-01 Destroy on hit|Projectile hits target B|Projectile is destroyed/disabled as designed and cannot affect later targets|Projectile is destroyed/disabled as designed and cannot affect later targets |Pass |
|LC-02 Timeout destroy when no hit occurs|Projectile hits nothing; lifetime = T|Projectile is auto-destroyed/recycled when T is reached; no leftovers remain in the scene|Projectile is auto-destroyed/recycled when T is reached; no leftovers remain in the scene | Pass|
|LC-03 Destruction occurs only once|Hit and timeout conditions become true in nearly the same frame|Only one destruction path executes; no double recycle or duplicate-log error|Only one destruction path executes; no double recycle or duplicate-log error |Pass |
|LC-04 No leftover objects after many rounds|Run multiple rounds of firing and ending|No historical Projectile remains in the scene; pool/instance count stays stable| No historical Projectile remains in the scene; pool/instance count stays stable| Pass|

---

#### Function: Collision Feedback and Presentation (visual/audio feedback is correct on hit and miss)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|FB-01 Hit-target feedback|Projectile hits target B|Hit VFX is shown and aligned with the hit time|Hit VFX is shown and aligned with the hit time | |
|FB-02 No false feedback on miss|Projectile passes by target without collision|No hit effect is played and no false damage feedback is shown|No hit effect is played and no false damage feedback is shown | Pass|
|FB-03 Feedback on wall/ground hit|Projectile hits a static scene object|Impact feedback is played and projectile is destroyed/disappears as designed|Impact feedback is played and projectile is destroyed/disappears as designed | Pass|
|FB-04 Multiple hit feedback events remain distinct|Several projectiles hit in quick succession|Each hit has its own feedback; no dropped or incorrectly merged feedback|Each hit has its own feedback; no dropped or incorrectly merged feedback |Pass |
|FB-05 Feedback count is correct|The same Projectile hits the same target with deduplication enabled|Feedback appears only once and is not replayed by duplicate callbacks|Feedback appears only once and is not replayed by duplicate callbacks |Pass |

---

#### Function: Three-Client Presentation Consistency (Player A / Player B / Spectator C see the same projectile presentation)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SY-01 Same cast timing across three clients|Player A triggers Cast; record A/B/C screens|All three clients can see the Projectile; appearance timing is aligned or within acceptable sync error|All three clients can see the Projectile; appearance timing is aligned | Pass|
|SY-02 Same spawn position across three clients|A fires from a fixed stance and fixed posture|B and C see the Projectile spawn at a position logically consistent with A's firing point|B and C see the Projectile spawn at a position logically consistent with A's firing point |Pass |
|SY-03 Same flight path across three clients|Projectile flies in the air for 1–2 seconds|All three clients see the same trajectory direction, with no obvious drift/teleport on any side| All three clients see the same trajectory direction|Pass |
|SY-04 Same hit and destroy presentation across three clients|Projectile hits B|All three clients observe the hit and destruction; one side does not keep flying while another already removed it|All three clients observe the hit and destruction|Pass |
|SY-05 Spectator observes only and does not interfere|Spectator C only watches the combat|C can fully observe Projectile behavior but cannot affect spawn, flight, hit, or result|C can fully observe Projectile behavior but cannot affect spawn, flight, hit, or result | Pass|

---

#### Function: Edge Cases (Projectile still behaves correctly in extreme space/posture conditions)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|BD-01 Extremely close-range fire|Player fires when very close to the target|Projectile still spawns and resolves correctly; no abnormal repeated hit due to initial overlap|no abnormal repeated hit due to initial overlap | Pass|
|BD-02 Fire toward ground/ceiling|Player fires with a large pitch angle|Projectile flies in the intended direction; no abnormal flipping occurs|no abnormal flipping occurs |Pass |
|BD-03 Fire while moving|Player walks or sidesteps while continuously casting|Projectile spawn and flight remain stable; not broken by movement interpolation|not broken by movement interpolation |Pass |
|BD-04 Fire in occluded environments|Player fires near desks / walls / another player's body|Projectile behavior remains valid; no obvious clipping, sticking, or wrong destruction|no obvious clipping, sticking, or wrong destruction | Pass|
|BD-05 Fire after long runtime|Cast again after the system has been running for a long time|Projectile still spawns, flies, and destroys correctly; no accumulated drift or abnormality|no accumulated drift or abnormality |Pass |

---