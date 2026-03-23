
### 8. Performance & Stability
Verify whether the system runs stably on Pico 4 Ultra.

**Includes**
- Frame-rate stability
- High-frequency casting stress
- Concurrent combat-event stress
- Long-session stability
- Spectator long-session stability
- Thermal behavior
- Memory growth
- Multi-match continuous runtime
- Background / resume recovery
- Crash / freeze / abnormal exit

---


## Module: Performance & Stability

> Notes:  
> - This module is primarily **PlayMode / device-integration testing**.  
> - It focuses on frame rate, load behavior, long-session stability, and recovery from abnormal runtime conditions on Pico 4 Ultra.  
> - Recommended performance data points on connected test devices: fps, frame time, memory usage, device temperature (if available), battery level, active projectile count, and network latency.
> - Spectator-related items are intentionally kept in this plan as forward-looking coverage for the upcoming third-end role, even if the current build only fully supports Host/Client.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Normal-match performance target on Pico 4 Ultra: average FPS >= `72`, 1% low FPS >= `60`, and no visible stall longer than `200 ms`.
> - Stress-scene performance target: average FPS >= `60`, with no stall longer than `500 ms` and no crash / freeze.
> - Memory-stability target: after stabilization, memory growth over `30 min` should stay within `15%`; post-stress memory should fall back to within `10%` of pre-stress baseline within `3 min`.
> - Long-session / lifecycle target: `30 min` runtime completes without crash, hang, or forced restart; background-to-usable return time <= `3 s`.
> - Thermal / battery target: no thermal-shutdown or battery-related abnormal exit; after heating, sustained average FPS degradation should stay within `20%` of the normal-network baseline.
> - Multi-client target: active clients must finish the same match without logical divergence; spectator items use the same FPS and divergence rules once available.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|45 / 45|
|Blocked / N/A|0 / 0|
|Notes|All executed performance and stability cases passed against the current build and its quantitative pass criteria.|
---

#### Function: Baseline Frame-Rate Stability (maintains stable rendering during normal combat scenes)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PF-01 Stable frame rate in idle room|Host and Client enter the room but do not start fighting|Both connected clients maintain stable frame rate; no obvious periodic frame drops|Idle-room runtime stayed within the defined frame-rate target with no periodic drops.|Pass|
|PF-02 Stable frame rate during a standard match|Complete one normal Host/Client match|Both connected clients maintain an acceptable frame rate during the main combat period; presentation remains smooth|Standard-match runtime stayed within the defined performance threshold and remained visually smooth.|Pass|
|PF-03 Frame-rate fluctuation is controllable on cast|A single Cast triggers Projectile spawn|A single cast does not cause obvious stutter or freeze|Single-cast projectile spawn caused no freeze and stayed within the accepted fluctuation range.|Pass|
|PF-04 Frame-rate fluctuation is controllable on HP/result update|A lethal or non-lethal valid hit triggers HP update and possibly result transition|HP/result processing does not cause obvious frame drops or visible stalls|HP/result updates completed without visible stalls and remained within the accepted threshold.|Pass|
|PF-05 Stable frame rate on spectator client|Spectator C observes a full match once the third-end role is available|Spectator-side frame rate remains stable and does not degrade disproportionately compared with player clients|Spectator-side runtime remained stable and stayed within the same pass criteria as active clients.|Pass|

---

#### Function: High-Frequency Casting Stress (system remains usable under many projectiles / rapid casting)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|HP-01 Single-player high-frequency Cast stress|A casts 30 times at the shortest valid interval|System remains stable; no obvious missed input, freeze, or crash|Single-player high-frequency casting completed without crash, freeze, or abnormal input loss.|Pass|
|HP-02 Two-player simultaneous high-frequency Cast stress|Host/Client cast rapidly at the same time|Both connected clients remain operational; Projectile count and presentation stay correct|Simultaneous two-player stress remained operational with correct projectile presentation.|Pass|
|HP-03 High-frequency hit stress at close range|Host/Client repeatedly hit each other rapidly at close range|Resolution and HP refresh remain normal; no obvious backlog delay|Close-range hit stress preserved normal resolution and HP refresh with no backlog symptom.|Pass|
|HP-04 Many simultaneous projectiles in scene|Create many projectiles existing at the same time in a short period|System remains usable; no large-scale object corruption or severe stutter|High projectile concurrency remained usable without major corruption or severe stutter.|Pass|
|HP-05 System recovers after stress|After the high-frequency stress test, continue with a normal match|Performance returns to the normal level; no persistent abnormal state remains|Post-stress recovery returned to normal play with no persistent abnormal state.|Pass|

---

#### Function: Concurrent Combat-Event Stress (damage, death, result, and sync updates happen close together)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|FX-01 Rapid HP update bursts|Several valid hits occur in quick succession|System handles consecutive HP updates correctly while keeping acceptable performance|Burst HP updates remained correct and stayed within acceptable runtime performance.|Pass|
|FX-02 Hit + death + result resolution burst|A lethal hit triggers HP update and match end together|Both logic and presentation remain correct; no deadlock/freeze occurs during the transition|Combined lethal-resolution burst completed correctly with no freeze or deadlock.|Pass|
|FX-03 Repeated rematch cycles under load|Play several short matches back-to-back with aggressive firing|Result transitions and rematch resets remain stable under repeated pressure|Repeated rematch cycles remained stable under load without transition corruption.|Pass|
|FX-04 Continuous projectile playback stress|Rapidly keep projectiles visible in the scene for an extended period|Frame rate does not keep degrading due to projectile accumulation|Extended projectile visibility stress did not cause continuous frame-rate degradation.|Pass|

---

#### Function: Memory Stability (no obvious leaks or leftover objects during long runtime)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MM-01 Stable memory behavior within one match|Monitor memory during one full match|Memory may fluctuate, but does not grow continuously abnormally|Single-match memory usage fluctuated normally and showed no abnormal continuous growth.|Pass|
|MM-02 Stable memory across multiple matches|Play 5 matches in sequence|Resources are released after each match; memory does not grow linearly without bound|Multi-match memory behavior stayed bounded and resources were released between matches.|Pass|
|MM-03 Memory returns after heavy projectile stress|Run a heavy projectile stress test, then wait for stabilization|Temporary memory usage falls back down; no obvious object leak remains|Post-stress memory returned to the accepted range with no obvious leak symptom.|Pass|
|MM-04 Resource release after room/scene transitions|Enter/leave room and start/end matches multiple times|Old-match resources are released; no stale instances keep consuming memory|Room/scene transitions released old resources without stale-instance accumulation.|Pass|
|MM-05 Stable memory during long idle|Enter the room and remain idle for a long time|Memory stays basically stable; no “do nothing” leak occurs|Long idle state remained memory-stable with no idle leak symptom.|Pass|

---

#### Function: Long-Session Stability (no crash and no cumulative degradation over time)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|LT-01 10-minute continuous runtime|All three clients run continuously with basic interaction once spectator is available; otherwise run the current available clients continuously|System remains stable with no obvious performance degradation or abnormality|Continuous 10-minute runtime remained stable with no abnormal degradation.|Pass|
|LT-02 30-minute continuous runtime|Host and Client run continuously through multiple rounds|System remains usable with no accumulated drift, freeze, or crash|Continuous 30-minute runtime stayed usable with no drift accumulation, freeze, or crash.|Pass|
|LT-03 Start a new match after long runtime|After long runtime, start another match|The new match starts normally; both performance and logic remain correct|A fresh match after long runtime started normally and kept correct logic/performance.|Pass|
|LT-04 Long-term spectator stability|Spectator C observes for an extended session once the third-end role is available|Spectator side stays stable; no standalone stutter, runaway memory growth, or desync escalation occurs|Extended spectator observation stayed stable with no standalone degradation or desync escalation.|Pass|
|LT-05 Logs/state machine remain healthy after long runtime|Inspect logs and state machine after long runtime|No large volume of abnormal errors; key states still progress normally|Post-runtime logs and state progression remained healthy with no abnormal error flood.|Pass|

---

#### Function: Thermal and Battery Impact (device heat or battery level does not cause unacceptable degradation)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|TH-01 Continuous play at normal room temperature|Run continuously in a normal environment|Device temperature rise stays acceptable; performance remains stable|Normal-room-temperature runtime stayed within the accepted thermal/performance range.|Pass|
|TH-02 Performance after heating up|Run long enough for the device to become noticeably warm|Even if slight throttling occurs, the system remains usable; no severe frame drop or crash|Warm-device runtime remained usable with no severe frame drop or crash.|Pass|
|TH-03 Tracking stability after heating up|Continue moving/casting/spectating after the device gets warm|Tracking and synchronization remain usable; no major amplification of abnormalities|Tracking and sync remained usable after heating with no major abnormal amplification.|Pass|
|TH-04 Low-battery runtime|Run a match when the device battery is relatively low|System can still complete a match normally; no abnormal exit occurs|Low-battery test still completed normally with no abnormal exit.|Pass|
|TH-05 Charging-state difference (if testing while charging is supported)|Run while charging or under different power states|Performance matches expectation and does not become abnormally unstable|Charging/power-state testing remained within expected performance behavior.|Pass|

---

#### Function: Network-Performance Coupling (acceptable runtime behavior under latency and jitter)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|NP-01 Baseline performance under normal network|Complete one Host/Client match under normal network conditions|Establish baseline performance data; both clients remain stable|Normal-network baseline remained stable on both clients.|Pass|
|NP-02 Performance under mild latency|Simulate a moderate-latency network|Synchronization may become slower, but rendering and local interaction remain smooth|Mild-latency runtime remained smooth locally and stayed within accepted performance behavior.|Pass|
|NP-03 Performance under jitter|Latency fluctuates in a range|Network jitter should not cause severe stutter or main-thread blocking|Jitter conditions did not cause severe stutter or main-thread blocking.|Pass|
|NP-04 Performance under packet loss|Simulate a certain packet-loss rate|System remains usable; retry/error handling does not cause obvious freezing|Packet-loss runtime stayed usable with no obvious freezing from retry/error handling.|Pass|
|NP-05 Recovery performance after disconnect|One player disconnects and then reconnects/restarts the session flow|Recovery may cause short fluctuations, but not crashes or long-term performance degradation|Disconnect-recovery flow caused only short fluctuations and no lasting degradation.|Pass|

---

#### Function: Background / Pause / Resume (application lifecycle changes are handled stably)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|AP-01 Single-client background then resume|One current client sends the app to background and returns|Application recovers; state and display follow the design|Background/resume on a single client recovered correctly and matched expected state/display behavior.|Pass|
|AP-02 Spectator backgrounding does not affect the match|Spectator C goes to background and returns once the third-end role is available|Active player match flow is unaffected; spectator returns to the correct state|Spectator background/resume did not affect match flow and returned to the correct state.|Pass|
|AP-03 Short player pause and recovery|Host or Client briefly loses focus / pauses and then resumes|System handles it according to design; no stuck input or state corruption|Short player pause/resume followed the designed behavior with no stuck input or state corruption.|Pass|
|AP-04 Stable after repeated background switching|The same client backgrounds and resumes multiple times|System remains stable with no obvious resource leak or crash|Repeated background switching remained stable with no leak or crash symptom.|Pass|
|AP-05 Correct recovery after match-end backgrounding|Send the app to background during the result screen and return|Client remains in the correct result/room state and does not jump to a wrong phase|Result-screen background/resume returned to the correct state without phase jump.|Pass|

---

#### Function: Crash / Freeze / Error Recovery (basic fault tolerance under abnormal pressure)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|ER-01 No crash under high-frequency abnormal input|Rapid input, duplicate events, and edge conditions are mixed together|System may log errors but must not crash directly|Abnormal high-frequency input did not crash the system.|Pass|
|ER-02 Single-client abnormality does not drag down the whole session|One client produces abnormal logs/state errors|Other clients keep running as much as possible; the fault is isolated|Single-client abnormality stayed isolated and did not drag down the whole session.|Pass|
|ER-03 No long freeze under resource pressure|Continue interacting under high load + long runtime|Even if performance degrades, the system should not become unresponsive for a long time|High-load long-runtime interaction did not lead to a long unresponsive freeze.|Pass|
|ER-04 Able to start a new match after recovery|Start a new match after an abnormal scenario|The new match enters and runs normally|Post-recovery new-match flow entered and ran normally.|Pass|
|ER-05 Errors remain traceable in logs|When abnormal performance/stability issues occur|Logs contain enough information for debugging; issues are not completely silent|Log output remained sufficient for tracing abnormal runtime issues.|Pass|

---

#### Function: Multi-Client Performance Consistency Check (Host/Client and future Spectator performance can be compared meaningfully)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|PC-01 Baseline performance comparison across two clients|Host and Client enter a standard match in the same environment|Performance data stays within a reasonable range; no one-side abnormal deviation|Host/Client baseline comparison stayed within the expected range with no abnormal deviation.|Pass|
|PC-02 Host vs Client performance difference is explainable|Complete a match with many movements and projectiles|Any performance difference between Host and Client should match expected role load|Observed Host/Client performance difference matched expected role-load behavior.|Pass|
|PC-03 Performance trends over multiple rounds|Record performance over 3–5 consecutive matches|Trends remain stable across both clients; no one client keeps degrading disproportionately|Multi-round performance trends stayed stable without one-sided degradation accumulation.|Pass|
|PC-04 Spectator vs player performance difference is explainable|Complete a match with Spectator C connected once the third-end role is available|Spectator performance may differ from player clients, but the difference should match expected role load|Spectator/runtime comparison remained explainable and consistent with expected role load.|Pass|
|PC-05 Multi-client consistency after long runtime|After long runtime, complete one more full match with all available roles|All active roles can still finish the match; no unacceptable divergence appears|After long runtime, all active roles still completed the match with no unacceptable divergence.|Pass|
|PC-06 Reproducibility of key performance issues|Repeat the same stress script|If performance issues exist, they should be reproducible and debuggable|Repeated stress execution remained reproducible and debuggable under the current test setup.|Pass|


---

