
### 8. Performance & Stability
Verify whether the system runs stably on Pico 4 Ultra.

**Includes**
- Frame-rate stability
- High-frequency casting stress
- Performance with multiple simultaneous skills
- Hit-effect stress
- Long-session stability
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
> - Recommended performance logs on all three clients: fps, frameTime, cpuTime, gpuTime, memoryUsage, temperature (if available), batteryLevel, activeProjectileCount, networkLatency, trackingState.

---

#### Function: Baseline Frame-Rate Stability (maintains stable rendering during normal combat scenes)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|PF-01 Stable frame rate in idle room|A/B/C enter the room but do not start fighting|All three clients maintain stable frame rate; no obvious periodic frame drops|no obvious periodic frame drops | Pass|
|PF-02 Stable frame rate during a standard match|Complete one normal match|All three clients maintain an acceptable frame rate during the main combat period; presentation remains smooth|presentation remains smooth | Pass|
|PF-03 Frame-rate fluctuation is controllable on cast|A single Cast triggers Projectile spawn and effects|A single cast does not cause obvious stutter or freeze|A single cast does not cause obvious stutter or freeze | Pass|
|PF-04 Frame-rate fluctuation is controllable on hit|Projectile hits a target and triggers feedback|Hit resolution and effects do not cause obvious frame drops|Hit resolution and effects do not cause obvious frame drops | Pass|
|PF-05 Stable frame rate on spectator client|C spectates an entire match|Spectator-side frame rate remains stable and is not obviously worse than player-side|Spectator-side frame rate remains stable | Pass|

---

#### Function: High-Frequency Casting Stress (system remains usable under many projectiles / rapid casting)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|HP-01 Single-player high-frequency Cast stress|A casts 30 times at the shortest valid interval|System remains stable; no obvious missed input, freeze, or crash|System remains stable | Pass|
|HP-02 Two-player simultaneous high-frequency Cast stress|A/B cast rapidly at the same time|All three clients remain operational; Projectile count and presentation stay correct|All three clients remain operational | Pass|
|HP-03 High-frequency hit stress at close range|A/B repeatedly hit each other rapidly at close range|Resolution, effects, and HP refresh all remain normal; no obvious backlog delay| no obvious backlog delay | Pass|
|HP-04 Many simultaneous projectiles in scene|Create many projectiles existing at the same time in a short period|System remains usable; no large-scale object corruption or severe stutter|no large-scale object corruption or severe stutter | Pass|
|HP-05 System recovers after stress|After the high-frequency stress test, continue with a normal match|Performance returns to the normal level; no persistent abnormal state remains|no persistent abnormal state remains | Pass|

---

#### Function: Multi-Effect / Multi-Event Concurrency Stress (hit, damage, death, and state changes happen together)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|FX-01 Multiple hits in the same frame|Several hit events occur at the same instant|System handles them correctly while keeping acceptable performance| System handles them correctly while keeping acceptable performance|Pass |
|FX-02 Hit + death + result resolution in the same frame|A lethal hit triggers death and match end together|Both logic and presentation remain correct; no same-frame deadlock/freeze|Both logic and presentation remain correct | Pass|
|FX-03 Same-frame mutual kill|A/B die at the same time|Result stays consistent; no long freeze or desync occurs on any client| Result stays consistent|Pass |
|FX-04 Continuous effect playback stress|Rapidly trigger many cast/hit effects in sequence|Frame rate does not keep degrading due to effect accumulation|Frame rate does not keep degrading due to effect accumulation | Pass|

---

#### Function: Memory Stability (no obvious leaks or leftover objects during long runtime)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|MM-01 Stable memory behavior within one match|Monitor memory during one full match|Memory may fluctuate, but does not grow continuously abnormally|Memory does not grow continuously abnormally | Pass|
|MM-02 Stable memory across multiple matches|Play 5 matches in sequence|Resources are released after each match; memory does not grow linearly without bound| memory does not grow linearly without bound| Pass|
|MM-03 Memory returns after heavy projectile stress|Run a heavy projectile stress test, then wait for stabilization|Temporary memory usage falls back down; no obvious object leak remains|Temporary memory usage falls back down |Pass |
|MM-04 Resource release after room/scene transitions|Enter/leave room and start/end matches multiple times|Old-match resources are released; no stale instances keep consuming memory|no stale instances keep consuming memory |Pass |
|MM-05 Stable memory during long idle|Enter the room and remain idle for a long time|Memory stays basically stable; no “do nothing” leak occurs| Memory stays basically stable|Pass |

---

#### Function: Long-Session Stability (no crash and no cumulative degradation over time)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|LT-01 10-minute continuous runtime|All three clients run continuously with basic interaction|System remains stable with no obvious performance degradation or abnormality|no obvious performance degradation or abnormality |Pass |
|LT-02 30-minute continuous runtime|All three clients run continuously through multiple rounds|System remains usable with no accumulated drift, freeze, or crash|System remains usable| Pass|
|LT-03 Start a new match after long runtime|After long runtime, start another match|The new match starts normally; both performance and logic remain correct|both performance and logic remain correct |Pass |
|LT-04 Long-term spectator stability|C spectates for a long time without interacting|Spectator side stays stable; no standalone memory growth or stutter issue|no standalone memory growth or stutter issue | Pass|
|LT-05 Logs/state machine remain healthy after long runtime|Inspect logs and state machine after long runtime|No large volume of abnormal errors; key states still progress normally|No large volume of abnormal errors | Pass|

---

#### Function: Thermal and Battery Impact (device heat or battery level does not cause unacceptable degradation)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|TH-01 Continuous play at normal room temperature|Run continuously in a normal environment|Device temperature rise stays acceptable; performance remains stable|Device temperature rise stays acceptable |Pass |
|TH-02 Performance after heating up|Run long enough for the device to become noticeably warm|Even if slight throttling occurs, the system remains usable; no severe frame drop or crash|no severe frame drop or crash |Pass |
|TH-03 Tracking stability after heating up|Continue moving/casting/spectating after the device gets warm|Tracking and synchronization remain usable; no major amplification of abnormalities|no major amplification of abnormalities |Pass |
|TH-04 Low-battery runtime|Run a match when the device battery is relatively low|System can still complete a match normally; no abnormal exit occurs|no abnormal exit occurs | Pass|
|TH-05 Charging-state difference (if testing while charging is supported)|Run while charging or under different power states|Performance matches expectation and does not become abnormally unstable|no abnormally unstable | Pass|

---

#### Function: Network-Performance Coupling (acceptable runtime behavior under latency and jitter)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|NP-01 Baseline performance under normal network|Complete one match under normal network conditions|Establish baseline performance data; all three clients remain stable|all three clients remain stable | Pass|
|NP-02 Performance under mild latency|Simulate a moderate-latency network|Synchronization may become slower, but rendering and local interaction remain smooth|rendering and local interaction remain smooth | Pass|
|NP-03 Performance under jitter|Latency fluctuates in a range|Network jitter should not cause severe stutter or main-thread blocking|Network jitter should not cause severe stutter or main-thread blocking|Pass |
|NP-04 Performance under packet loss|Simulate a certain packet-loss rate|System remains usable; retry/error handling does not cause obvious freezing| |Pass |
|NP-05 Recovery performance after disconnect|A player or spectator disconnects and then recovers|Recovery may cause short fluctuations, but not crashes or long-term performance degradation| System did not crashes| Pass |

---

#### Function: Background / Pause / Resume (application lifecycle changes are handled stably)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|AP-01 Single-client background then resume|One client sends the app to background and returns|Application recovers; state and display follow the design|Application recovers | Pass|
|AP-02 Spectator backgrounding does not affect the match|C goes to background and returns|A/B combat is unaffected; C returns to the correct state|A/B combat is unaffected | Pass|
|AP-03 Short player pause and recovery|A or B briefly loses focus / pauses and then resumes|System handles it according to design; no stuck input or state corruption|no stuck input or state corruption | Pass|
|AP-04 Stable after repeated background switching|The same client backgrounds and resumes multiple times|System remains stable with no obvious resource leak or crash|System remains stable with no obvious resource leak or crash | Pass|
|AP-05 Correct recovery after match-end backgrounding|Send the app to background during the result screen and return|Client remains in the correct result/room state; does not jump to a wrong phase|Client remains in the correct result/room state | Pass|

---

#### Function: Crash / Freeze / Error Recovery (basic fault tolerance under abnormal pressure)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|ER-01 No crash under high-frequency abnormal input|Rapid input, duplicate events, and edge conditions are mixed together|System may log errors but must not crash directly|System logging errors | Pass|
|ER-02 Single-client abnormality does not drag down the whole session|One client produces abnormal logs/state errors|Other clients keep running as much as possible; the fault is isolated| | Pass|
|ER-03 No long freeze under resource pressure|Continue interacting under high load + long runtime|Even if performance degrades, the system should not become unresponsive for a long time|Performance degrades, system still responsive | Pass|
|ER-04 Able to start a new match after recovery|Start a new match after an abnormal scenario|The new match enters and runs normally|The new match enters and runs normally | Pass|
|ER-05 Errors remain traceable in logs|When abnormal performance/stability issues occur|Logs contain enough information for debugging; issues are not completely silent|Logs contain enough information for debugging | Pass|

---

#### Function: Three-Client Performance Consistency Check (A/B/C performance can be compared meaningfully)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|PC-01 Baseline performance comparison across three clients|A/B/C enter a standard match in the same environment|Performance data stays within a reasonable range; no one-side abnormal deviation| no one-side abnormal deviation | Pass|
|PC-02 Spectator vs player performance difference is explainable|Complete a match with many movements and effects|C may differ from A/B, but the difference should match expected role load|difference match expected role load | Pass|
|PC-03 Performance trends over multiple rounds|Record performance over 3–5 consecutive matches|Trends remain stable across all clients; no one client keeps degrading disproportionately|Trends remain stable across all clients | Pass|
|PC-04 Three-client consistency after long runtime|After long runtime, complete one more full match|All three clients can still finish the match; no unacceptable divergence appears| All three clients can still finish the match| Pass|
|PC-05 Reproducibility of key performance issues|Repeat the same stress script|If performance issues exist, they should be reproducible and debuggable|Performace issue is reproducible and debuggable | Pass|

---