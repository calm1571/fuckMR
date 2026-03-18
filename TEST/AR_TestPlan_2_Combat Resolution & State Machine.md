## Test Modules

### 2. Combat Resolution & State Machine
Verify whether core combat rules such as hit, damage, shield, death, and win/loss are correct.

**Includes**
- Hit detection
- Duplicate-hit deduplication
- Single-hit damage calculation
- Accumulated damage from multiple hits
- Shield priority
- Invincibility-frame handling
- HP lower-bound clamping
- Death event triggered only once
- State lock after death
- Win/loss judgment
- Draw judgment
- Same-frame mutual hit / same-frame mutual kill handling

---


## Module: Combat Resolution & State Machine

> Notes:  
> - This module is primarily **EditMode / logic-layer testing**.  
> - It focuses on whether hit, damage, shield, invincibility, death, and win/loss rules are correct and deterministic.

---

#### Function: Same-Frame Mutual Hit (A hits B and B hits A in the same frame; processing order is consistent and no event is lost)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SF-01 Same-frame mutual hit (normal damage)|Same tick: Hit(A→B), Hit(B→A), damage=30, both HP=100|Both Hit events are processed; both HP values become 70; no event is lost| Both Hit events are processed; both HP values become 70; no event is lost | Pass |
|SF-02 Same-frame mutual kill|Same tick: damage=120, both HP=100|Both players die; each Death event triggers exactly once; result follows the rule set is draw | Both players die; each Death event triggers exactly once; result follows the rule set is draw | Pass |
|SF-03 Deterministic processing order|Randomly swap input order: A then B vs B then A|Final state is identical | Final state is identical | Pass |
|SF-04 Mixed same-frame events|Same tick: A Cast + A Hit B + B Hit A|Processing order does not cause loss or duplication; log order follows the design | Processing order does not cause loss or duplication; log order follows the design | Pass |

---

#### Function: Duplicate Hit Deduplication (the same projectile cannot deal damage to the same target twice)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|DD-01 Multiple colliders trigger duplicate callbacks|Target B has 2 colliders; the same Projectile triggers OnTriggerEnter twice|Damage is applied only once; the second hit is deduplicated|Damage is applied only once; the second hit is deduplicated| Pass |
|DD-02 Repeated hit callbacks caused by overlap/jitter|Projectile repeatedly enters/exits the trigger boundary|Only the first hit is resolved; later callbacks are ignored|Only the first hit is resolved; later callbacks are ignored | Pass |
|DD-04 Deduplication window policy|Dedup rule = ProjectileId + TargetId; window = Projectile lifetime|Repeated hits on the same target during the lifetime are ignored; after lifetime ends, no further events are produced| Repeated hits on the same target during the lifetime are ignored; after lifetime ends, no further events are produced | Pass |

---

#### Function: Shield (Shield only works once and is consumed after being triggered)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|SH-01 Shield blocks one hit|B Shield=Active; B HP=100; incoming damage=40|This hit is fully blocked by the shield; HP remains 100; Shield becomes Inactive|This hit is fully blocked by the shield; HP remains 100; Shield becomes Inactive |Pass |
|SH-02 Second hit after shield is consumed|B starts with Shield=Active; first hit damage=40; second hit damage=40|After the first hit, HP remains 100 and Shield becomes Inactive; the second hit deals normal damage, so HP=60|After the first hit, HP remains 100 and Shield becomes Inactive; the second hit deals normal damage, so HP=60 |Pass |
|SH-03 Single high-damage hit is still blocked once|B Shield=Active; B HP=100; incoming damage=100|The entire hit is blocked by the shield; HP remains 100; Shield becomes Inactive|The entire hit is blocked by the shield; HP remains 100; Shield becomes Inactive |Pass |
|SH-04 Multiple consecutive hits with one shield|B Shield=Active; three consecutive hits: 20 / 20 / 20; B HP=100|The first hit is blocked, so HP remains 100 and Shield becomes Inactive; the next two hits deal normal damage, so final HP=60|The first hit is blocked, so HP remains 100 and Shield becomes Inactive; the next two hits deal normal damage, so final HP=60 |Pass |
|SH-05 Normal damage without shield|B Shield=Inactive; B HP=100; incoming damage=40|Normal damage is applied; HP=60; no damage reduction occurs|Normal damage is applied; HP=60; no damage reduction occurs | Pass|

---

#### Function: HP Lower Bound (HP never becomes negative; death triggers only once)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|HP-01 Overkill damage does not produce negative HP|B HP=10; damage=50|B HP becomes 0; never negative|B HP becomes 0; never negative |Pass |
|HP-02 Death event triggers only once|B HP=10; in the same frame receives two damage events of 10 each|Death event triggers at most once; once state becomes Dead, no repeated Death trigger occurs|Death event triggers at most once; once state becomes Dead, no repeated Death trigger occurs |Pass |
|HP-03 Hit after death|B is already Dead; receives another Hit/Damage|No further HP reduction and no repeated Death event; may be logged as an invalid event|No further HP reduction and no repeated Death event; may be logged as an invalid event |Pass |
|HP-04 Respawn flow (if supported)|B Dead → Respawn; HP reset; receives damage again|After respawn, combat logic works normally again; death counting remains correct|After respawn, combat logic works normally again; death counting remains correct |Pass |

---

#### Function: Win/Lose Rules (when time ends, higher HP wins; draw logic is correct)

|Test|Inputs|Expected Outcome|Test Outcome|Result|
|---|---|---|---|---|
|WR-01 Time up, A has higher HP|TimeUp; A HP=60; B HP=40|A is declared the winner|A is declared the winner |Pass |
|WR-02 Time up, equal HP|TimeUp; A HP=50; B HP=50|Draw is declared|Draw is declared | Pass|
|WR-03 Same-frame TimeUp + Hit|Same tick: TimeUp + A hits B (which changes HP)|A clearly defined priority is followed: damage first then result; outcome is stable and consistent| A clearly defined priority is followed: damage first then result; outcome is stable and consistent|Pass |
|WR-04 Both players die in the same frame|Both HP values reach 0 in the same tick|Draw is declared|Draw is declared|Pass|
|WR-05 Spectator-side consistency|Replay the same match on the audience/spectator side|Result matches the player clients; no winner mismatch| Result matches the player clients; no winner mismatch|Pass |

---