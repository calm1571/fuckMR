
### 3. Event Integrity / Replay & Fault Tolerance
Verify whether event data, replay pipelines, and abnormal-input handling are reliable.

**Includes**
- Cast / Hit / Damage event serialization
- Replay consistency
- Event ordering and determinism
- Random-seed determinism (if randomness is used)
- Cross-platform replay consistency
- targetId / projectileId fault handling
- Late events / out-of-order event handling
- Duplicate delivery deduplication
- Version compatibility and missing-field tolerance

---


## Module: Event Integrity / Replay & Fault Tolerance

> Notes:  
> - This module is primarily **EditMode / logic-layer and infrastructure testing**.  
> - It focuses on event schema reliability, replay pipelines, ordering/deduplication strategies, and fault tolerance under abnormal input.

---

#### Function: Event Serialization (Cast/Hit/Damage events remain unchanged after serialize → deserialize)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SE-01 Cast event serialization|Cast event (casterId, projectileId, pos, dir, timestamp) → Serialize → Deserialize|All fields remain identical after deserialization within allowed precision tolerance|All fields remain identical after deserialization within allowed precision tolerance |Pass |
|SE-02 Hit event serialization|Hit (projectileId, targetId, hitPoint, timestamp)|Fields remain intact; targetId and hit point information are not lost|ields remain intact; targetId and hit point information are not lost |Pass |
|SE-03 Damage event serialization|Damage (amount, shieldDelta, hpDelta, reason, timestamp)|Fields remain intact; amount and resulting deltas remain consistent|Fields remain intact; amount and resulting deltas remain consistent |Pass |
|SE-04 Floating-point precision strategy|pos / dir / point contain floating-point values|Serialization policy is fixed (for example quantization/compression); error stays below the threshold and does not affect judgment|Serialization policy is fixed; error stays below the threshold and does not affect judgment |Pass |
|SE-05 Compatibility|Old-version event format parsed by new-version code (for example added fields)|Can be parsed or default values are assigned; no crash occurs| Can be parsed or default values are assigned; no crash occurs|Pass |

---

#### Function: Replay Consistency (replaying the same event sequence twice gives the same final state)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|RP-01 Replay the same sequence twice|Replay a fixed event sequence (Cast/Hit/Damage) two times|Final HP, Shield, status, and result are completely identical|Final HP, Shield, status, and result are completely identical |Pass |
|RP-02 Event-order normalization|Provide the same batch of events in random order|After sorting by timestamp/sequenceId, the result is consistent|After sorting by timestamp/sequenceId, the result is consistent | Pass|
|RP-03 Random-seed determinism (if randomness is used)|Hit effects/spread use Random|a fixed seed ensures replay consistency|a fixed seed ensures replay consistency | Pass|
|RP-04 Cross-platform consistency|Replay the same sequence on PC vs device|Key states remain consistent (considering floating-point tolerance/quantization)|Key states remain consistent |Pass |

---

#### Function: Abnormal Input Handling (targetId missing, late events, out-of-order arrival)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|EX-01 targetId does not exist|Hit event with targetId=999 (no such object in the scene)|Event is safely dropped and logged; no crash; no valid object state is changed|Event is safely dropped or logged; no crash; no valid object state is changed |Pass |
|EX-02 projectileId does not exist|Damage references a nonexistent projectileId|Safely dropped and warned; no crash|Safely dropped and warned; no crash|Pass |
|EX-03 Late event arrives too late|Damage event timestamp lags current time beyond threshold (for example >500 ms)|Handled by policy: drop / compensate / replay; result stays consistent with design|Handled by policy: drop / compensate / replay; result stays consistent with design |Pass |
|EX-04 Out-of-order arrival|Damage arrives before Hit|System buffers/waits for dependencies, or discards by rule; no duplicate resolution occurs|System buffers/waits for dependencies, or discards by rule; no duplicate resolution occurs |Pass |
|EX-05 Duplicate delivery|The same event arrives twice|Processed only once using eventId/sequenceId deduplication|Processed only once using eventId/sequenceId deduplication |Pass |
|EX-06 Version mismatch / missing fields|Missing field or incorrect field type|Parsing fails safely without crashing|Parsing fails safely without crashing |Pass |

---