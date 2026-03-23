## Test Modules

### 1. Input & Cast Trigger
Verify whether players can trigger skill casting correctly and reliably.

**Includes**
- Trigger input
- Cooldown restriction
- Debounce / prevention of repeated casts from a single press
- Hold behavior policy
- Input response latency

---


## Module: Input & Cast Trigger

> Notes:  
> - This module is primarily **EditMode / logic-layer testing**, with one **device-side latency check**.  
> - It focuses on verifying whether Trigger input, cooldown, debounce, hold policy, and input response timing all behave as designed in the current build.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Trigger / cooldown / debounce cases use exact-count judgment: legal casts = expected count, illegal extra casts = 0.
> - Cooldown boundary cases pass only if no cast occurs before cooldown end, and the first legal cast is accepted within 1 frame after cooldown expires.
> - For 100-cycle repeat tests, allowed overfire count = 0 and allowed missed-fire count <= 1.
> - Visible Trigger-to-projectile latency target: <= 120 ms in light-load scenes, <= 150 ms during normal in-match runtime.
> - Repeated latency consistency target across 20 presses: max-min spread <= 50 ms, with no single sample > 180 ms.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|13 / 13|
|Blocked / N/A|0 / 0|
|Notes|All executed cases in this module passed against the current build and its quantitative pass criteria.|
---

#### Function: Trigger Input Basics (a valid trigger press generates exactly one cast entry point)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|TG-01 Trigger press edge generates one cast request|Press Trigger once and release once while cooldown is ready|Exactly 1 `TriggerDown` edge is generated and exactly 1 cast attempt reaches the shooter|Observed exactly one press-edge cast path with no duplicate cast attempt.|Pass|
|TG-02 Trigger release does not generate a cast|Press and then release Trigger once while cooldown is ready|`TriggerUp` is observed, but no extra cast is generated on release|Release produced no additional cast request or projectile spawn.|Pass|
|TG-03 No cast while shooting is disabled|Press Trigger while `SetShootingEnabled(false)` is active|No projectile is spawned and no shot event is emitted|No projectile or shot event was observed while shooting remained disabled.|Pass|

---

#### Function: Cooldown Logic (cannot cast during cooldown; can cast again on the first frame after cooldown ends)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CD-01 Cast attempted during cooldown|CD=2.0s; Cast at t=0; attempt Cast again at t=0.5s|The second cast is rejected; no Projectile is generated and no Cast event is emitted|Second cast during cooldown was consistently rejected with no extra spawn/event.|Pass|
|CD-02 Cast on the frame just before cooldown ends|CD=1.0s; Cast at t=0; attempt Cast again at t=0.99s|Rejected; no Projectile is generated and no Cast event is emitted|Boundary attempt before cooldown end was rejected as expected.|Pass|
|CD-03 Cast on the first frame after cooldown ends|CD=1.0s; Cast at t=0; attempt Cast again at t=1.00s (or next frame)|Cast is allowed; one Projectile is generated and one Cast event is emitted|First legal post-cooldown frame accepted one cast and produced one projectile/event.|Pass|
|CD-04 Cooldown precision boundary (floating-point tolerance)|CD=1.0s; accumulated time=0.999999 vs 1.000001|Casting is allowed only after the threshold is actually reached; no early allow or permanent lockout due to precision error|Precision-edge checks showed no early allow and no lockout after threshold was crossed.|Pass|
|CD-05 Stability across repeated cycles|Cast repeatedly with intervals = CD ± small jitter, over 100 cycles|Successful cast count is approximately equal to the number of valid windows; no missed or extra casts|Repeated-cycle run stayed within the defined count criteria, with no extra casts observed.|Pass|

---

#### Function: Debounce / Anti-Repeat (input jitter from a single action does not generate multiple casts)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|DB-01 Single-press jitter|Input sequence: Down → (Up/Down jitter multiple times, <30ms) → Up; CD ready|Only 1 Cast is generated; only 1 Cast event is emitted|Jitter sequence still produced only one cast path and one event.|Pass|
|DB-02 Hold-to-cast policy|Hold the button for 500 ms; config = "cast once on press"|Only 1 cast is triggered at the initial press; no repeated cast each frame|Long press triggered once on initial press with no frame-by-frame repeat.|Pass|

---

#### Function: Input Response Latency (trigger input reaches visible cast feedback within an acceptable delay)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LT-01 Trigger-to-projectile visible latency|Record controller and screen with high-frame-rate video; press Trigger once while cooldown is ready|The delay from physical Trigger press to visible projectile spawn stays within the project acceptance threshold|Measured visible latency remained within the defined threshold in the tested build.|Pass|
|LT-02 Trigger-to-log latency consistency|Capture device log timestamps for `TriggerDown` and shot spawn during 20 single presses|Input-to-shot timing stays stable across repeated runs, without large jitter spikes|Repeated timing samples stayed within the defined consistency window and showed no large spikes.|Pass|
|LT-03 Latency under moderate scene load|Repeat LT-01 during normal in-match runtime with remote pose/network updates active|Latency may increase slightly, but remains within the accepted range and does not feel inconsistent|Latency under normal match load remained within the accepted range and felt consistent.|Pass|


---

