### 5. 多人同步
验证战斗数据、事件与结果在三个客户端之间是否始终一致。

**包含内容**
- Cast 事件同步
- Projectile 位置同步
- Hit 事件同步
- Damage / HP 同步
- Shield / Invincible 状态同步
- Death 状态同步
- Win / Lose / Draw 结果同步
- 延迟网络场景
- 乱序到达场景
- 重复投递场景
- 断线 / 重连处理
- 中途加入同步

---

## 模块：多人同步

> 说明：  
> - 本模块主要用于 **PlayMode / 三端集成测试**。  
> - 重点验证 Player A、Player B、Spectator C 在同一场对局中观察到的事件、状态与结果是否一致。  
> - 建议三端同时记录日志：eventId、timestamp、casterId、targetId、projectileId、HP/Shield 变更前后值。  
> - 执行测试前请保持 `Actual Outcome` 和 `Status` 为空。  
> - `Status` 建议使用：`Not Run`、`Pass`、`Fail`、`Blocked`、`N/A`。  
> - 若某项依赖当前版本中尚未确认实现的功能，请先标记为 `Blocked`，并补充实现核查链接或说明。
>
> 量化通过标准：
> - 对于事件计数类用例，A / B / C 的成功施法次数、命中次数、回合结果次数必须完全一致；重复数与无解释丢失数都必须为 `0`。
> - 三端事件可见时机目标：Cast / Projectile / Result 的观察时间差 <= `200 ms`；HP / Shield / Dead / Result 的 UI 刷新时间差 <= `300 ms`。
> - Projectile 三端同步目标：生成位置差 <= `20 cm`，方向差 <= `10 deg`，销毁时机差 <= `200 ms`。
> - 终态必须在所有活跃客户端上完全一致：HP、Shield、Dead、Winner、MatchState、room / match 标识都必须相同。
> - 中途加入 / 重连恢复通过标准：重连客户端在 `2 s` 内进入当前比赛快照；断线结果处理在 `3 s` 内收敛。

---

#### 功能：Cast 事件同步（三端对施法事件达成一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MS-01 A 侧施法三端可见|Player A 触发 1 次 Cast|A / B / C 都能观察到 Cast；不会只在本地可见|||
|MS-02 B 侧施法三端可见|Player B 触发 1 次 Cast|A / B / C 都能观察到 Cast；施法者身份正确|||
|MS-03 单次 Cast 不会被重复同步|A 在正常网络条件下触发 1 次 Cast|B 与 C 各只收到 1 个对应 Cast 事件；不会重复生成|||
|MS-04 高频 Cast 同步完整性|A 按合法节奏施法 20 次|B / C 观察到的成功施法次数与 A 一致；无明显丢失|||
|MS-05 双方近同时施法|A 与 B 几乎同时施法|三端都能观察到两次施法；顺序遵循稳定规则（如 sequenceId）|||

---

#### 功能：Projectile 状态同步（生成位置、朝向与销毁在各端保持一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MP-01 初始生成位置同步|A 以固定站姿与姿态发射|B / C 看到的 Projectile 生成位置与 A 的发射点一致|||
|MP-02 飞行轨迹同步|Projectile 飞行 1-2 秒|A / B / C 看到相同的轨迹方向；任一端都无明显漂移 / 瞬移|||
|MP-03 销毁时机同步|Projectile 因命中或超时被销毁|三端都在可接受误差内观察到销毁；不会出现一端保留对象而另一端已删除|||
|MP-04 多个 Projectile 同步|短时间内连续发射多个 Projectile|三端对 Projectile 数量与标识（projectileId）认知一致，无混淆|||
|MP-05 观战端对 Projectile 为只读|Spectator C 仅接收战斗表现|C 只显示 Projectile 状态，不能回写或影响 A / B 的逻辑|||

---

#### 功能：Hit / Damage / HP 同步（三端对命中、伤害与状态变化达成一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MD-01 Hit 事件同步|A 的 Projectile 命中 B|A / B / C 都观察到相同的 Hit；targetId 一致|||
|MD-02 伤害后 HP 同步|B 当前 HP=100；受到 30 点伤害|A / B / C 最终都显示 B HP=70|||
|MD-03 护盾变化同步|B 有 Shield，HP=100；受到 30 点伤害|A / B / C 最终都显示 Shield 消失且 HP=100|||
|MD-04 死亡状态同步|B HP=10 并受到致命伤害|A / B / C 都进入相同的 Dead 状态；Death 只触发一次|||

---

#### 功能：结果同步（对局结束结果在三端保持一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|MR-01 正常击杀结束同步|A 击杀 B，比赛结束|A / B / C 都显示相同结果：A 胜 / B 负 / C 正确显示观战结果|||
|MR-02 时间结束结果同步|倒计时结束，且 A HP > B HP|三端都判定 A 获胜；不存在胜者不一致|||
|MR-03 平局结果同步|时间结束时 A HP = B HP，或满足规则定义的平局条件|三端都显示 Draw|||
|MR-04 同帧互杀结果同步|A 与 B 在同一 tick 内同时死亡|三端都得出相同的规则结果；不会出现 A 侧 Draw、B 侧 Win 的分歧|||
|MR-05 赛后结果锁定|比赛已结束后又收到一个迟到事件|三端按规则忽略或处理该事件；最终结果不会被覆盖|||

---

#### 功能：延迟 / 抖动 / 乱序到达（非理想网络下的一致性）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|NL-01 轻度延迟|模拟固定延迟（如 100-200ms）|三端仍能完成正常对局；最终 HP / 结果保持一致|||
|NL-02 抖动场景|延迟在区间内波动（如 50-250ms）|表现可能有轻微滞后，但逻辑结算保持一致|||
|NL-03 乱序到达|Damage 先于 Hit 到达，或 Cast / Spawn 顺序交换|系统按规则缓存 / 排序 / 丢弃；不会重复结算或污染状态|||
|NL-04 重复投递|同一 eventId 被发送两次|三端最多处理一次；HP / 状态不会变化两次|||
|NL-05 迟到事件|过期事件在比赛推进后才到达|按策略丢弃 / 补偿；最终状态不会被错误回滚|||

---

#### 功能：断线 / 重连 / 中途加入（会话管理行为正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RC-01 玩家中途断线|比赛中 Player B 断线|系统按规则结束；A / C 显示一致的断线结果|||
|RC-02 观战端中途断线|比赛中 Spectator C 断线|A / B 战斗正常继续；C 断线不影响战斗逻辑|||
|RC-03 观战端重连恢复|C 断线后重连当前房间|C 恢复当前比赛状态；HP、结果与玩家位置与进行中的对局一致|||
|RC-04 中途加入的观战端|比赛进行到一半时 C 加入|C 同步到当前比分 / HP / 场景状态，而不是从比赛开头开始|||

---

#### 功能：角色权限与边界（三端身份隔离正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RB-01 观战端不能施法|Spectator C 执行 Trigger / Gesture 输入|不会生成 Cast / Projectile；A / B 战斗状态不受影响|||
|RB-02 观战端不能受伤|Projectile 接触 C 的观战表示（若存在）|不会触发 Damage / HP 变化；观战端不参与战斗结算|||
|RB-03 角色映射正确|A / B / C 同时加入同一对局|每个角色唯一且稳定；不会出现身份映射错误|||
|RB-04 房间状态广播正确|开始、结束或重开新局|A / B / C 都收到相同房间状态；不会出现单端已开始 / 未开始不一致|||
|RB-05 旧对局事件隔离|上一局结束后立即开始下一局|旧局事件不会污染新局；match / session ID 隔离正确|||

---

#### 功能：三端一致性总检查（一整局结束后关键状态保持对齐）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|CK-01 完整对局日志对齐|完成一场标准对局并导出 A / B / C 日志|关键事件序列可以对齐；事件数量与关键字段一致|||
|CK-02 终态对齐|检查一局结束后的最终状态|A / B / C 最终 HP、Shield、Dead、Winner、MatchState 一致|||
|CK-03 回放结果对齐|在三端分别分析 / 回放同一组比赛日志|回放结果保持一致；不存在胜者不一致|||
|CK-04 多轮一致性|连续进行 3-5 局比赛|各局彼此独立；三端在每一局中保持一致|||
|CK-05 长时间运行后的一致性|系统长时间运行后再完成一局|三端依然保持同步；不会因累计漂移导致状态错误|||

---
