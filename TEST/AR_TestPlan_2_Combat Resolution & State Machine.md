## Test Modules

### 2. Combat Resolution & State Machine
Verify whether core combat rules such as hit, damage, shield, death, and win/loss are correct.

**Includes**
- Hit detection
- Hit / miss resolution
- Single-hit damage calculation
- Accumulated damage from multiple hits
- Shield priority
- Spectator-authoritative support heal resolution
- Wall obstacle blocking and wall HP resolution
- HP lower-bound clamping
- Lethal-hit result transition
- State lock after death
- Host-authoritative winner judgment
- Match reset of temporary combat state

---


## Module: Combat Resolution & State Machine

> Notes:  
> - This module is primarily **EditMode / logic-layer testing**.  
> - It focuses on whether hit detection, damage, shield blocking, spectator support healing, wall-obstacle interaction, HP clamping, and result transitions are correct and deterministic in the current build.

> - Execution status: leave `Actual Outcome` and `Status` blank until the test is run.
> - Allowed `Status` values: `Not Run`, `Pass`, `Fail`, `Blocked`, `N/A`.
> - If a test depends on a feature that is not confirmed in the current build, mark it `Blocked` first and link the implementation check.
>
> Quantitative Pass Criteria:
> - Single valid hit damage must equal exactly `GetDamage()` for the current build configuration.
> - Repeated valid hits must accumulate exactly by `N * GetDamage()` until HP reaches 0; HP is never allowed to drop below 0.
> - Active shield blocks 100% of incoming damage during its active window; blocked hits change HP by 0.
> - Spectator heal support must increase HP by exactly `GetSpectatorHealAmount()`, but HP may never exceed `GetMaxHp()`.
> - A projectile intercepted by an active wall must deal `0` player HP damage; wall HP changes by exactly `GetWallShotDamage()` per valid projectile hit and is never allowed to drop below `0`.
> - After lethal damage is applied, all later hit attempts in the same round must change HP by 0.
> - Match-end result must settle to a single winner within 500 ms after the lethal hit is resolved, with no winner flip afterward.
> - New-match reset must clear temporary combat state completely: rematch enters with HP reset, shield gates reset, spectator-heal cooldown reset, and active wall count reset to `0`.

### Execution Summary

|Item|Result|
|---|---|
|Execution Result|Completed|
|Overall Status|Pass|
|Pass Rate|31 / 31|
|Blocked / N/A|0 / 0|
|Notes|All core combat, spectator-heal, wall-obstacle, and rematch-reset cases were executed and passed.|
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

#### Function: Spectator Support Heal Resolution (Spectator support can heal through Host-authoritative resolution only)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|HL-01 Heal Host increases HP by configured amount|Host current HP is below max; Spectator triggers `Heal Host` once|Host HP increases by exactly `GetSpectatorHealAmount()`, capped at `GetMaxHp()`|Heal Host increased Host HP by the configured amount and correctly stopped at max HP.|Pass|
|HL-02 Heal Client increases HP by configured amount|Client current HP is below max; Spectator triggers `Heal Client` once|Client HP increases by exactly `GetSpectatorHealAmount()`, capped at `GetMaxHp()`|Heal Client increased Client HP by the configured amount and correctly stopped at max HP.|Pass|
|HL-03 Heal cooldown blocks repeated support|Spectator triggers Heal again before spectator-vote cooldown ends|Second heal request is ignored or rejected; HP does not change a second time|Repeated heal during cooldown was ignored and did not apply extra HP change.|Pass|
|HL-04 Invalid heal target does not mutate combat state|Host receives a malformed or unsupported heal target role|No HP change occurs and no unrelated combat state is modified|Invalid heal target was safely ignored with no HP mutation or unrelated combat-state change.|Pass|

---

#### Function: HP Bounds and Post-Death Lock (HP never becomes negative and dead targets stop accepting damage)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|HP-01 Overkill damage does not produce negative HP|Target HP=10 and receives lethal damage greater than remaining HP|HP becomes 0 and never drops below 0|Overkill damage clamped HP to zero with no negative value observed.|Pass|
|HP-02 Hit after death is ignored|Target HP is already 0 and receives another valid shot|No further HP reduction occurs|Post-death hit attempts produced no additional HP reduction.|Pass|
|HP-03 HP update remains exact across repeated hits|Apply repeated non-lethal hits until just above zero|HP values match exact expected decrements with no off-by-one behavior|Repeated non-lethal hits decremented HP exactly as expected with no off-by-one issue.|Pass|
|HP-04 Lethal hit only triggers one result transition|Target receives the lethal hit that brings HP to 0|Result transition happens once; no second transition is triggered by later ignored hits|Lethal hit produced a single result transition, and ignored later hits did not retrigger it.|Pass|

---

#### Function: Wall Obstacle Resolution (walls block projectiles, lose HP, and disappear at zero HP)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|WO-01 Wall blocks projectile before player hit resolves|An active wall lies between shooter and target along the shot ray|Player HP remains unchanged; the shot is consumed by wall interception instead of reaching the player|Active wall interception prevented player HP loss and consumed the shot before player-hit resolution.|Pass|
|WO-02 Projectile hit reduces wall HP by configured damage|A valid projectile hits an active wall once|Wall HP decreases by exactly `GetWallShotDamage()` and remains above `0` if not yet destroyed|Valid projectile hit reduced wall HP by the configured wall-shot damage value.|Pass|
|WO-03 Wall HP clamps to zero and wall destroys once|Repeated projectile hits or decay reduce wall HP to `0`|Wall HP becomes `0`, never negative, and destruction occurs only once|Wall HP clamped to zero correctly and wall destruction occurred exactly once.|Pass|
|WO-04 Destroyed wall no longer blocks later shots|A wall has already been removed after HP reaches `0`|Later shots are no longer intercepted by that wall and combat resolution follows the normal hit path|Destroyed wall no longer intercepted later shots and normal combat resolution resumed.|Pass|

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

#### Function: Round Reset and Temporary-State Cleanup (Retry clears support-state and temporary combat objects)

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RS-01 Retry clears active wall obstacles|Complete a match with one or more active walls, then trigger Retry|New round starts with active wall count = `0`; no old wall remains in combat or visuals|Retry cleared all active wall obstacles and the next round started with no wall residue.|Pass|
|RS-02 Retry clears spectator support cooldowns|Spectator uses Heal shortly before the previous round ends, then Retry starts a new round|Spectator support cooldown state is reset for the new round according to design; no stale cooldown is carried incorrectly|Retry reset spectator support cooldown correctly and no stale cooldown carried into the new round.|Pass|
|RS-03 Retry clears result-lock state and accepts fresh combat|A previous round ended with a lethal result, then Retry starts the next round|New round accepts fresh damage / shield / hit resolution normally and is not stuck in prior round result lock|Retry cleared prior-round result lock and the new round accepted fresh combat resolution normally.|Pass|
|RS-04 Retry keeps max-HP and damage rules intact|After Retry, apply normal hit and optional heal again|Damage, heal cap, and HP bounds in the new round still follow the same authoritative rules as a fresh session|Post-retry combat still followed the same damage, heal-cap, and HP-bound rules as a fresh match.|Pass|


---

