## Test Modules

### 1. Input & Cast Trigger
Verify whether players can trigger skill casting correctly and reliably.

**Includes**
- Trigger input
- Gesture input
- Cooldown restriction
- Debounce / prevention of repeated casts from a single press
- Hold behavior policy
- Conflict handling for multiple input sources
- Input frame-drop / state recovery
- Left/right hand consistency
- Input response latency

---


## Module: Input & Cast Trigger

> Notes:  
> - This module is primarily **EditMode / logic-layer testing**.  
> - It focuses on verifying whether Trigger / Gesture input, cooldown, debounce, hold policy, and input-state recovery all behave as designed.

---

#### Function: Cooldown Logic (cannot cast during cooldown; can cast again on the first frame after cooldown ends)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|CD-01 Cast attempted during cooldown|CD=2.0s; Cast at t=0; attempt Cast again at t=0.5s|The second cast is rejected; no Projectile is generated and no Cast event is emitted| The second cast is rejected; no Projectile is generated and no Cast event is emitted | Pass |
|CD-02 Cast on the frame just before cooldown ends|CD=1.0s; Cast at t=0; attempt Cast again at t=0.99s|Rejected; no Projectile is generated and no Cast event is emitted| Rejected; no Projectile is generated and no Cast event is emitted | Pass |
|CD-03 Cast on the first frame after cooldown ends|CD=1.0s; Cast at t=0; attempt Cast again at t=1.00s (or next frame)|Cast is allowed; one Projectile is generated and one Cast event is emitted| Cast is allowed; one Projectile is generated and one Cast event is emitted | Pass |
|CD-04 Cooldown precision boundary (floating-point tolerance)|CD=1.0s; accumulated time=0.999999 vs 1.000001|Casting is allowed only after the threshold is actually reached; no early allow or permanent lockout due to precision error| Casting is allowed only after the threshold is actually reached; no early allow or permanent lockout due to precision error | Pass |
|CD-05 Stability across repeated cycles|Cast repeatedly with intervals = CD ± small jitter, over 100 cycles|Successful cast count is approximately equal to the number of valid windows; no missed or extra casts| Successful cast count is approximately equal to the number of valid windows; no missed or extra casts | Pass |

---

#### Function: Debounce / Anti-Repeat (input jitter from a single action does not generate multiple casts)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|DB-01 Single-press jitter|Input sequence: Down → (Up/Down jitter multiple times, <30ms) → Up; CD ready|Only 1 Cast is generated; only 1 Cast event is emitted| Only 1 Cast is generated; only 1 Cast event is emitted | Pass |
|DB-02 Hold-to-cast policy|Hold the button for 500 ms; config = "cast once on press"|Only 1 cast is triggered at the initial press; no repeated cast each frame| Only 1 cast is triggered at the initial press; no repeated cast each frame | Pass |
|DB-03 Lost input frame|Down occurs on the previous frame, Up on the next frame (or Up is lost)|System does not get stuck in a “continuous trigger” state; timeout/state-reset strategy exists| System does not get stuck in a “continuous trigger” state; timeout/state-reset strategy exists | Pass |

---