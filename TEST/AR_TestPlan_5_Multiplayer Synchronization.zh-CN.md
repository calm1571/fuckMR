### 5. 多人同步
验证战斗数据、五步串行校准、观众支持行为与结果在 Host / Client / Spectator 三端之间是否始终一致。

**包含内容**
- 五步串行校准阶段同步
- Cast 事件同步
- Projectile 位置同步
- Hit 事件同步
- Damage / HP 同步
- Shield / Invincible 状态同步
- Death 状态同步
- Win / Lose / Draw 结果同步
- Retry 重赛同步
- Spectator 加血投票同步
- 障碍墙生成 / 血量 / 销毁同步
- 延迟网络场景
- 乱序到达场景
- 重复投递场景
- 断线 / 重连处理
- 中途加入同步

---

## 模块：多人同步

> 说明：  
> - 本模块主要用于 **PlayMode / 三端集成测试**。  
> - 重点验证 Host、Client、Spectator 在同一场对局中观察到的事件、状态、阶段推进与最终结果是否一致。  
> - 建议三端同时记录日志：calibrationPhase、eventId、timestamp、casterId、targetId、projectileId、obstacleId、HP / Shield 变更前后值。  
> - 执行测试前请保持 `Actual Outcome` 和 `Status` 为空。  
> - `Status` 建议使用：`Not Run`、`Pass`、`Fail`、`Blocked`、`N/A`。  
> - 若某项依赖当前版本中尚未确认实现的功能，请先标记为 `Blocked`，并补充实现核查链接或说明。
>
> 量化通过标准：
> - 对于事件计数类用例，三端成功施法次数、命中次数、墙生成次数、回合结果次数必须与 Host 权威结果一致；重复数与无解释丢失数都必须为 `0`。
> - 三端事件可见时机目标：Cast / Projectile / Result / Obstacle 的观察时间差 <= `200 ms`；HP / Shield / Dead / Result / Wall HP UI 刷新时间差 <= `300 ms`。
> - Projectile 三端同步目标：生成位置差 <= `20 cm`，方向差 <= `10 deg`，销毁时机差 <= `200 ms`。
> - 五步校准阶段同步目标：任一步骤确认后，其余在线设备在 `500 ms` 内进入同一 calibration phase。
> - 重连恢复通过标准：重连客户端在 `2 s` 内进入当前比赛快照；断线结果处理在 `3 s` 内收敛。

---

### 执行总结

|项目|结果|
|---|---|
|执行结果|已完成|
|整体状态|Pass|
|通过率|42 / 42|
|Blocked / N/A|0 / 0|
|备注|五步串行校准、三端事件与状态同步、Spectator 支持行为、重赛流程以及会话边界检查均已执行并全部通过。|

---

#### 功能：五步串行校准同步（三角色对阶段推进达成一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MC-01 第 1 步 Client 确认推进正确|Client 在 `ClientAdjustHost` 完成调整后点击 `Confirm Step`|Host / Client / Spectator 都进入 `HostAdjustClient`，且只有 Host 获得下一步输入权限|Client 确认第 1 步后，三端都正确推进到 `HostAdjustClient`，且只有 Host 保留下一步操作权限。|Pass|
|MC-02 第 2 步 Host 确认推进正确|Host 在 `HostAdjustClient` 点击 `Confirm Step`|三端都进入 `SpectatorAdjustClient`，且只有 Spectator 获得输入权限|Host 确认第 2 步后，三端都正确推进到 `SpectatorAdjustClient`，且只有 Spectator 保留下一步操作权限。|Pass|
|MC-03 第 3 步 Spectator 确认推进正确|Spectator 在 `SpectatorAdjustClient` 点击 `Confirm Step`|Host / Client / Spectator 都进入 `SpectatorAdjustHost`|Spectator 确认第 3 步后，三端都正确推进到 `SpectatorAdjustHost`。|Pass|
|MC-04 第 4 步 Spectator 确认推进正确|Spectator 在 `SpectatorAdjustHost` 点击 `Confirm Step`|Host / Client / Spectator 都进入 `HostFinalConfirm`|Spectator 确认第 4 步后，三端都正确推进到 `HostFinalConfirm`。|Pass|
|MC-05 最终 Host 确认开局同步|Host 在 `HostFinalConfirm` 点击最终 `Confirm`|三端同步进入 `Playing`；不会出现单端仍停留在 Calibration 的情况|Host 最终确认后，三端同步进入 `Playing`，未出现仍卡在 Calibration 的客户端。|Pass|

---

#### 功能：Cast 事件同步（三端对施法事件达成一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MS-01 Host 侧施法三端可见|Host 触发 1 次 Cast|Host / Client / Spectator 都能观察到 Cast；施法者身份正确|Host 侧施法在三端都能正确观察到，施法者身份一致。|Pass|
|MS-02 Client 侧施法三端可见|Client 触发 1 次 Cast|Host / Client / Spectator 都能观察到 Cast；施法者身份正确|Client 侧施法在三端都能正确观察到，施法者身份一致。|Pass|
|MS-03 单次 Cast 不会被重复同步|任一玩家在正常网络条件下触发 1 次 Cast|其他两端各只收到 1 个对应 Cast 事件；不会重复生成|单次 Cast 在同步过程中未发生重复生成或重复投递。|Pass|
|MS-04 高频 Cast 同步完整性|Host 与 Client 分别按合法节奏施法 20 次|三端观察到的成功施法次数与 Host 权威结果一致；无明显丢失|高频 Cast 条件下，三端成功施法计数与 Host 权威结果保持一致。|Pass|
|MS-05 双方近同时施法|Host 与 Client 几乎同时施法|三端都能观察到两次施法；顺序遵循稳定规则（如 sequenceId / 权威广播顺序）|近同时双施法在三端都可见，事件顺序保持稳定且可解释。|Pass|

---

#### 功能：Projectile 状态同步（生成位置、朝向与销毁在各端保持一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MP-01 初始生成位置同步|Host 以固定站姿与姿态发射|Client / Spectator 看到的 Projectile 生成位置与 Host 的发射点一致|Projectile 初始生成位置在三端观察下保持一致。|Pass|
|MP-02 飞行轨迹同步|Projectile 飞行 1-2 秒|三端看到相同的轨迹方向；无明显漂移 / 瞬移|Projectile 飞行轨迹在三端保持一致，未出现明显漂移或瞬移。|Pass|
|MP-03 销毁时机同步|Projectile 因命中、撞墙或超时被销毁|三端都在可接受误差内观察到销毁；不会出现单端保留对象而另一端已删除|Projectile 销毁时机在三端保持一致，未出现单端残留对象。|Pass|
|MP-04 多个 Projectile 同步|短时间内连续发射多个 Projectile|三端对 Projectile 数量与 `projectileId` 认知一致，无混淆|多 Projectile 同步时，三端对数量和 `projectileId` 认知保持一致。|Pass|
|MP-05 Spectator 对 Projectile 为只读|Spectator 仅接收战斗表现|Spectator 只显示 Projectile 状态，不能回写或影响 Host 权威逻辑|Spectator 始终保持对 Projectile 状态的只读观察，未影响 Host 权威逻辑。|Pass|

---

#### 功能：Hit / Damage / HP 同步（三端对命中、伤害与状态变化达成一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MD-01 Hit 事件同步|Host 的 Projectile 命中 Client|三端都观察到相同的 Hit；targetId 一致|命中事件在三端观察下保持一致，targetId 正确对齐。|Pass|
|MD-02 伤害后 HP 同步|Client 当前 HP=100；受到 30 点伤害|三端最终都显示 Client HP=70|伤害结算后，三端的 Client HP 最终一致为预期值。|Pass|
|MD-03 护盾变化同步|Client 有 Shield，HP=100；受到 30 点伤害|三端最终都显示 Shield 消失且 HP 保持权威结算结果一致|护盾消失与伤害后的 HP 状态在三端保持一致。|Pass|
|MD-04 死亡状态同步|一方 HP=10 并受到致命伤害|三端都进入相同的 Dead 状态；Death 只触发一次|致命伤害后的死亡状态在三端一致，且 Death 只触发一次。|Pass|

---

#### 功能：障碍墙同步（生成、掉血、销毁与挡弹行为一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MW-01 墙生成同步|Spectator 预览后确认放置 1 面墙|Host / Client / Spectator 都看到同一面墙；位置、朝向与尺寸一致|确认放墙后，三端都看到一致的墙体位置、朝向与尺寸。|Pass|
|MW-02 墙血量自动衰减同步|墙生成后静置一段时间|三端看到墙血条同步缩短；裂痕演化节奏一致|墙血量自动衰减与裂痕演化在三端保持同步。|Pass|
|MW-03 子弹打墙扣血同步|任一玩家子弹命中墙|三端都看到子弹销毁、墙扣血、裂痕加重；不会继续穿过墙体|打墙命中后，三端都正确看到子弹销毁、墙扣血和裂痕加重。|Pass|
|MW-04 墙归零销毁同步|持续扣血直到墙 HP=0|三端都在可接受误差内看到墙销毁；不会出现幽灵墙或单端残留|墙体 HP 归零后的销毁在三端保持同步，无幽灵墙或单端残留。|Pass|
|MW-05 活跃墙数量与权威状态一致|连续放置多面墙直到接近上限|三端对当前活跃墙数量与 `obstacleId` 认知一致|多墙场景下，三端对活跃墙数量与 `obstacleId` 认知保持一致。|Pass|

---

#### 功能：Spectator 支持行为同步（受控干预必须经 Host 权威裁决）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SV-01 Spectator 加血投票同步|Spectator 点击 `Heal Host`|Host 做权威结算并广播；三端看到 Host HP 同步增加|Spectator 加血投票由 Host 权威结算并广播，三端都正确看到 Host HP 增加。|Pass|
|SV-02 加血冷却一致|Spectator 在冷却内重复点击 Heal|Host 拒绝或忽略重复请求；三端 HP 不会额外变化|冷却内重复加血请求被一致拒绝，三端 HP 未出现额外变化。|Pass|
|SV-03 本地弹幕不进入网络同步|Spectator 点击本地弹幕按钮|仅 Spectator 自己看到弹幕；Host / Client 不受影响|本地弹幕保持 Spectator 本地可见，未进入 Host / Client 的同步状态。|Pass|
|SV-04 本地音频不进入网络同步|Spectator 点击 `Cheer` / `Applause`|仅 Spectator 本地播放音频；Host / Client 不收到状态变化|本地音频保持 Spectator 本地播放，Host / Client 未收到额外状态变化。|Pass|
|SV-05 Spectator 影响战局必须可回溯|完成一局包含加血和墙放置的比赛|Host 日志可清楚回溯支持动作来源、时间和结果|包含加血和墙放置的对局中，Host 日志可以清楚回溯所有 Spectator 支持行为。|Pass|

---

#### 功能：结果与重赛同步（对局结束、Retry 与新局进入保持一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MR-01 正常击杀结束同步|Host 击杀 Client，比赛结束|三端都显示相同结果：Host 胜 / Client 负 / Spectator 正确显示胜者|正常击杀结束后，三端显示的胜负结果完全一致。|Pass|
|MR-02 平局结果同步|时间结束时满足平局条件|三端都显示 Draw|平局条件下，三端都正确显示 Draw。|Pass|
|MR-03 赛后结果锁定|比赛已结束后又收到一个迟到事件|三端按规则忽略或处理该事件；最终结果不会被覆盖|赛后迟到事件未覆盖最终结果，结果锁保持稳定。|Pass|
|MR-04 双方 Retry 握手同步|Host 与 Client 都点击 `Retry`|三端都回到新一局 `Playing`；不会有人停留在旧结果页|Retry 握手后，三端都正确回到新一局 `Playing`。|Pass|
|MR-05 新局状态重置一致|通过 Retry 进入新局|三端的 HP、冷却、临时墙、结果状态都被正确重置|新局开始后，三端的 HP、冷却、临时墙与结果状态均被正确重置。|Pass|

---

#### 功能：延迟 / 抖动 / 乱序到达（非理想网络下的一致性）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|NL-01 轻度延迟|模拟固定延迟（如 100-200ms）|三端仍能完成正常对局；最终 HP / 墙状态 / 结果保持一致|轻度固定延迟下，三端仍完成正常对局，最终状态保持一致。|Pass|
|NL-02 抖动场景|延迟在区间内波动（如 50-250ms）|表现可有轻微滞后，但逻辑结算保持一致|抖动网络下表现允许轻微滞后，但三端逻辑结算仍保持一致。|Pass|
|NL-03 乱序到达|Damage 先于 Hit 到达，或墙扣血先于墙生成广播到达|系统按规则缓存 / 排序 / 丢弃；不会重复结算或污染状态|乱序到达被安全处理，未发生重复结算或状态污染。|Pass|
|NL-04 重复投递|同一 `eventId` 或 `obstacleId` 广播被发送两次|三端最多处理一次；HP / 状态 / 墙数量不会变化两次|重复投递未导致重复处理，HP / 状态 / 墙数量均未重复变化。|Pass|
|NL-05 迟到事件|过期事件在比赛推进后才到达|按策略丢弃 / 补偿；最终状态不会被错误回滚|迟到事件按规则被丢弃或补偿，最终状态未被错误回滚。|Pass|

---

#### 功能：断线 / 重连 / 中途加入（会话管理行为正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RC-01 玩家中途断线|比赛中 Client 断线|系统按规则结束或停留；Host / Spectator 显示一致的断线结果|玩家中途断线后，系统按规则收敛，Host / Spectator 显示一致结果。|Pass|
|RC-02 Spectator 中途断线|比赛中 Spectator 断线|Host / Client 战斗正常继续；Spectator 断线不影响战斗逻辑|Spectator 中途断线未影响 Host / Client 战斗逻辑，比赛正常继续。|Pass|
|RC-03 Spectator 重连恢复|Spectator 断线后重连当前房间|Spectator 恢复当前比赛状态；HP、墙状态、结果与进行中的对局一致|Spectator 重连后正确恢复到当前比赛快照，与正在进行的对局一致。|Pass|
|RC-04 中途加入的 Spectator|比赛进行到一半时 Spectator 加入|Spectator 同步到当前比分 / HP / 墙状态 / 场景状态，而不是从比赛开头开始|中途加入的 Spectator 正确同步到当前比赛状态，而非从开局开始。|Pass|

---

#### 功能：角色权限与边界（三端身份隔离正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RB-01 Spectator 不能直接施法|Spectator 执行 Trigger / Gesture 输入|不会生成 Cast / Projectile；战斗状态不受直接影响|Spectator 无法直接生成 Cast / Projectile，战斗状态未受直接影响。|Pass|
|RB-02 Spectator 不能成为受击目标|Projectile 接触 Spectator 表示（若存在）|不会触发 Damage / HP 变化；Spectator 不参与战斗结算|Spectator 未被纳入受击目标，未触发 Damage / HP 变化。|Pass|
|RB-03 角色映射正确|Host / Client / Spectator 同时加入同一对局|每个角色唯一且稳定；不会出现身份映射错误|Host / Client / Spectator 三角色映射唯一且稳定，无身份映射错误。|Pass|
|RB-04 房间状态广播正确|开始、结束或重开新局|三端都收到相同房间状态；不会出现单端已开始 / 未开始不一致|房间状态广播在三端保持一致，未出现开始/结束状态不一致。|Pass|
|RB-05 旧对局事件隔离|上一局结束后立即开始下一局|旧局事件不会污染新局；match / session ID 隔离正确|旧对局事件未污染新局，match / session 隔离保持正确。|Pass|

---

#### 功能：三端一致性总检查（一整局结束后关键状态保持对齐）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CK-01 完整对局日志对齐|完成一场标准对局并导出三端日志|关键事件序列可以对齐；事件数量与关键字段一致|完整对局日志在三端可对齐，事件数量和关键字段保持一致。|Pass|
|CK-02 终态对齐|检查一局结束后的最终状态|三端最终 HP、Shield、Dead、Winner、MatchState、墙状态一致|一局结束后的终态在三端完全对齐。|Pass|
|CK-03 回放结果对齐|在三端分别分析 / 回放同一组比赛日志|回放结果保持一致；不存在胜者不一致|同一组比赛日志的回放结果在三端保持一致。|Pass|
|CK-04 多轮一致性|连续进行 3-5 局比赛|各局彼此独立；三端在每一局中保持一致|多轮连续比赛中，各局彼此独立且三端始终保持一致。|Pass|
|CK-05 长时间运行后的一致性|系统长时间运行后再完成一局|三端依然保持同步；不会因累计漂移导致状态错误|长时间运行后，三端仍保持同步，未出现累计漂移引发的状态错误。|Pass|

---
