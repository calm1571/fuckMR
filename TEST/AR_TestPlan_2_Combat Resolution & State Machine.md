## Test Modules

### 2. Combat Resolution & State Machine
Verify whether core combat rules such as hit, damage, shield, death, and win/loss are correct.

**Includes**
- Hit detection
- Hit / miss resolution
- Single-hit damage calculation
- Accumulated damage from multiple hits
- Shield priority
- HP lower-bound clamping
- Lethal-hit result transition
- State lock after death
- Host-authoritative winner judgment

---


## Module: Combat Resolution & State Machine

> Notes:  
> - This module is primarily **EditMode / logic-layer testing**.  
> - It focuses on whether hit detection, damage, shield blocking, HP clamping, and lethal-hit result transitions are correct and deterministic in the current build.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Single valid hit damage must equal exactly `GetDamage()` for the current build configuration.
> - Repeated valid hits must accumulate exactly by `N * GetDamage()` until HP reaches 0; HP is never allowed to drop below 0.
> - Active shield blocks 100% of incoming damage during its active window; blocked hits change HP by 0.
> - After lethal damage is applied, all later hit attempts in the same round must change HP by 0.
> - Match-end result must settle to a single winner within 500 ms after the lethal hit is resolved, with no winner flip afterward.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|19 / 19|
|Blocked / N/A|0 / 0|
|Notes|All executed combat-resolution and state-machine cases passed against the current build and its quantitative pass criteria.|
---

#### Function: Hit Resolution (a shot only deals damage when the host-side geometric hit check passes)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|HR-01 Direct hit on target head position|A fires a shot whose path intersects the target head position within hit radius|Target HP is reduced by exactly `GetDamage()` once|Direct-hit case reduced HP exactly once by the configured damage value.|Pass|
|HR-02 Miss outside hit radius|A fires a shot whose closest approach stays outside hit radius|No HP change occurs and no result transition occurs|Out-of-radius shot produced no HP change and no result transition.|Pass|
|HR-03 Shot beyond max distance|A fires with a valid direction but target lies beyond `shot.maxDistance`|No hit is resolved and no HP change occurs|Beyond-max-distance shot did not resolve a hit and left HP unchanged.|Pass|
|HR-04 Shot behind the shooter|Target position lies behind the shot spawn direction|No hit is resolved and no HP change occurs|Behind-shooter case produced no hit resolution and no HP change.|Pass|
|HR-05 Repeated valid hits accumulate correctly|Apply several valid hits in sequence with no shield active|HP decreases by `N * GetDamage()` until it reaches zero, with no extra loss|Repeated valid hits accumulated exactly as expected until HP reached zero.|Pass|

---

#### Function: Shield Blocking (an active shield blocks incoming shots during its active window)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SH-01 Active shield blocks incoming hit|Target shield end time is still in the future; incoming shot would otherwise hit|The shot is blocked; HP remains unchanged|Active shield fully blocked the incoming hit and HP remained unchanged.|Pass|
|SH-02 Shield expires and later hit deals damage|The same target is hit once after shield expiry|Damage is applied normally after the active shield window ends|Post-expiry hit dealt normal damage after shield protection ended.|Pass|
|SH-03 Shield activation respects cooldown gate|Attempt to activate shield again before `shieldCooldownUntil`|Second activation is rejected; shield active window is not extended|Cooldown gate correctly rejected early reactivation and did not extend shield time.|Pass|
|SH-04 Shield cannot be reactivated while already active|Attempt to trigger shield while current shield duration is still active|Reactivation is rejected; shield end time is not refreshed early|In-duration reactivation was rejected and shield end time was not refreshed.|Pass|
|SH-05 No shield means normal damage|Target has no active shield and receives a valid hit|HP is reduced by exactly `GetDamage()`|Valid hit without shield reduced HP by exactly the configured damage value.|Pass|

---

#### Function: HP Bounds and Post-Death Lock (HP never becomes negative and dead targets stop accepting damage)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|HP-01 Overkill damage does not produce negative HP|Target HP=10 and receives lethal damage greater than remaining HP|HP becomes 0 and never drops below 0|Overkill damage clamped HP to zero with no negative value observed.|Pass|
|HP-02 Hit after death is ignored|Target HP is already 0 and receives another valid shot|No further HP reduction occurs|Post-death hit attempts produced no additional HP reduction.|Pass|
|HP-03 HP update remains exact across repeated hits|Apply repeated non-lethal hits until just above zero|HP values match exact expected decrements with no off-by-one behavior|Repeated non-lethal hits decremented HP exactly as expected with no off-by-one issue.|Pass|
|HP-04 Lethal hit only triggers one result transition|Target receives the lethal hit that brings HP to 0|Result transition happens once; no second transition is triggered by later ignored hits|Lethal hit produced a single result transition, and ignored later hits did not retrigger it.|Pass|

---

#### Function: Result Transition (host-authoritative lethal hit ends the match with a single winner)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|WR-01 Host kills Client|Client HP reaches 0 from a host-resolved valid hit|Host enters result as winner and sends `winnerRole = Host`|Host-side lethal resolution entered result state correctly and emitted `winnerRole = Host`.|Pass|
|WR-02 Client kills Host|Host HP reaches 0 from a client shot resolved by host|Host enters result with `winnerRole = Client` and remote side shows lose/win accordingly|Client-win path resolved correctly and both sides mapped the result as expected.|Pass|
|WR-03 Non-lethal hit does not end the match|Valid hit reduces HP but target remains above 0|HP updates occur, but state does not transition to Result|Non-lethal damage updated combat state without triggering Result state.|Pass|
|WR-04 Remote result payload maps to local WIN / LOSE correctly|Receive `MatchResultPayload` with `winnerRole = Host` or `Client`|Each side maps the payload to local `WIN` or `LOSE` correctly|Remote result payload mapped correctly to local win/lose presentation on both sides.|Pass|
|WR-05 New match reset restores combat state|Trigger rematch reset after a completed match|HP, shield timers, shoot cooldown gates, and result text are reset to initial values|Rematch reset restored combat state, timers, gates, and result text to initial values.|Pass|


---

