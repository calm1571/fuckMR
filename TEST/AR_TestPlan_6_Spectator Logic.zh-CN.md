### 6. 观战逻辑
验证观战客户端能否正确观察比赛，并且绝不干扰战斗。

**包含内容**
- 观战端只能观看，不能攻击
- 观战端不能影响 HP / Shield / Result
- 观战端能看到双方的施法与命中
- 观战端的 HP 显示与玩家端一致
- 观战端的结算结果与玩家端一致
- 中途加入的观战端可以同步当前比赛状态
- 观战端断线 / 恢复
- 观战端 UI / 相机行为正确

---

## 模块：观战逻辑

> 说明：  
> - 本模块主要用于 **PlayMode / 三端集成测试**。  
> - 重点验证 Spectator C 作为纯观察端，能否正确观战且不干扰 Player A / Player B 的战斗逻辑。  
> - 建议记录日志：role、roomId、matchId、eventId、HP / Shield / Dead / Result、joinTime、reconnectTime。  
> - 执行测试前请保持 `Actual Outcome` 和 `Status` 为空。  
> - `Status` 建议使用：`Not Run`、`Pass`、`Fail`、`Blocked`、`N/A`。  
> - 若某项依赖当前版本中尚未确认实现的功能，请先标记为 `Blocked`，并补充实现核查链接或说明。
>
> 量化通过标准：
> - 观战权限隔离采用零容忍判定：被系统接受的 spectator 来源战斗事件数 = `0`，由 spectator 导致的 HP / Shield / Result 变更数 = `0`。
> - 观战端 UI / 状态同步目标：相对于玩家端的 HP / Shield / Dead / Result 显示时间差 <= `300 ms`。
> - 中途加入或重连通过标准：Spectator C 在 `2 s` 内进入当前比赛快照；赛后重连在 `3 s` 内进入正确结果界面。
> - spectator 共享空间观察目标（可用后）：相对落位误差 <= `20 cm`，静止观察 `30 s` 内漂移 <= `10 cm`。
> - 稳定性类用例仅在崩溃次数 = `0`、重复 spectator 实例数 = `0`、身份映射错误数 = `0` 时通过。

---

#### 功能：观战端权限隔离（观战端只能观看，不能参与战斗）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SP-01 Trigger 输入对观战端无效|Spectator C 按下 Trigger|不会触发 Cast；不会生成 Projectile；不会发出战斗事件|||
|SP-02 手势施法对观战端无效|Spectator C 做出施法手势|不会触发 Cast；不会生成 Projectile；日志中无施法记录|||
|SP-03 观战端不能造成伤害|人为注入观战端发送 Damage 事件的异常尝试|系统拒绝该事件；A / B 的 HP 不会变化|||
|SP-04 观战端不能成为有效战斗目标|Projectile 与观战端位置发生重叠 / 接触|不会对 Spectator 结算 Hit / Damage；观战端被排除在战斗结算外|||
|SP-05 观战端不能影响比赛结果|比赛结束前，Spectator 执行任意输入|Winner / Loser / Draw 结果不受影响|||

---

#### 功能：观战可见性（观战端能完整看到双方战斗）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SV-01 观战端能看到 A 的施法|Player A 触发 1 次 Cast|Spectator C 能看到 A 的 Cast 与 Projectile 表现|||
|SV-02 观战端能看到 B 的施法|Player B 触发 1 次 Cast|Spectator C 能看到 B 的 Cast 与 Projectile 表现|||
|SV-03 观战端能看到命中反馈|A 命中 B|Spectator C 能看到对应命中特效 / 伤害反馈 / 状态变化|||
|SV-04 观战端能看到双方移动|A / B 在场地中移动、转身、闪避|Spectator C 持续看到正确的相对移动与动作|||
|SV-05 观战端能看到比赛结束|A 击杀 B，或时间结束结算|Spectator C 能看到正确的结算表现与结果界面|||

---

#### 功能：观战状态显示（HP / Shield / Dead / Result 与玩家端一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SS-01 HP 显示一致性|B 当前 HP=100，被 A 命中造成 30 点伤害|Spectator C 显示 B HP=70，与 A / B 玩家端一致|||
|SS-02 Shield 显示一致性|B Shield=20，HP=100；受到 30 点伤害|Spectator C 显示 Shield=0 且 HP=90，与玩家端一致|||
|SS-03 Dead 状态一致性|B 受到致命伤害|Spectator C 显示 B 为 Dead，与玩家端一致|||
|SS-04 Win / Lose / Draw 一致性|完成一场正常对局|Spectator C 显示与 A / B 玩家端相同的结果|||
|SS-05 UI 刷新无明显延迟|连续发生多次命中与状态变化|Spectator C 的 UI 更新延迟在可接受同步误差内|||

---

#### 功能：观战端中途加入（晚加入的观战端可以同步到当前比赛状态）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SJ-01 中途加入后能看到当前状态|A / B 已在战斗且发生多次命中；C 加入|C 同步到当前 HP、Shield、Dead、剩余时间与玩家位置|||
|SJ-02 中途加入后能看到当前有效投射物|A / B 战斗中，场上已有 Projectile|C 能看到当前仍有效的 Projectile，或看到正确的当前场景状态|||
|SJ-03 中途加入后仍能看到正确结算|C 在比赛后半段加入，随后比赛结束|C 最终观察到的结果与玩家端一致|||
|SJ-04 中途加入不会重置比赛|比赛进行中 C 加入|A / B 的战斗状态不会被重置；比赛不会跳回起点|||
|SJ-05 中途加入失败处理|房间状态 / session 无效时 C 加入|系统给出清晰失败提示；A / B 的比赛不受影响|||

---

#### 功能：观战端断线与恢复（观战端掉线不影响比赛；恢复状态正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SR-01 观战端断线不影响玩家战斗|比赛中 Spectator C 断线|A / B 战斗正常继续；C 的断线不会暂停或错误终止比赛|||
|SR-02 观战端重连恢复当前比赛|C 断线后短时间内重连|C 恢复到当前比赛状态，而不是从比赛开头重来|||
|SR-03 长时间离线后重连|C 离线较久后重连|若比赛仍在进行，C 同步当前状态；若比赛已结束，C 进入正确结果界面|||
|SR-04 比赛结束后再重连|比赛已结束后 C 才重连|C 直接看到最终结果，而不会回到错误的“进行中”状态|||
|SR-05 多次断线 / 重连循环稳定性|同一局中多次断线 / 重连|系统保持稳定；不会出现身份混乱、重复 Spectator 实例或崩溃|||

---

#### 功能：观战视图与 UI（布局、提示与相机行为符合观战模式）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SU-01 观战端不显示玩家专属施法控件|C 进入观战模式|C 不显示或无法操作玩家专属 Cast / Skill 控件与提示|||
|SU-02 观战端能看到双方关键状态 UI|C 观看比赛|C 能清楚看到 A / B 的 HP、Shield、Result 等关键信息|||
|SU-03 观战视图不会遮挡关键信息|C 从默认观察点观看|UI 与相机布局不会遮挡主要战斗区域|||
|SU-04 观战模式提示正确|C 加入 / 断线 / 重连 / 看到比赛结束|UI 文案能清晰表明 Spectator 身份与当前状态|||
|SU-05 相机切换行为正确（若支持）|C 在自由视角 / 固定视角 / 跟随视角间切换|相机行为符合设计，且不影响玩家状态与同步|||

---

#### 功能：观战端与共享空间的关系（观战端在同一物理空间中的 AR 位置与显示正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SA-01 观战端看到正确的玩家落位|A / B 分别站在场地左右侧，C 从后方观看|C 看到的 A / B 相对位置与真实物理环境一致|||
|SA-02 观战端移动时仍保持正确视图|C 在现实空间中边走边看|C 的视图更新正确；A / B 虚拟位置无明显漂移|||
|SA-03 观战端靠近玩家不会破坏表现|C 靠近 A 或 B 进行观察|C 仍能正确看到战斗；A / B 模型与 UI 保持稳定|||
|SA-04 观战端遮挡不改变逻辑结果|C 站在 A 与 B 之间形成真实遮挡|只影响视觉可见性，不影响 Cast / Hit / Damage 逻辑|||
|SA-05 长时间观战的空间稳定性|C 长时间持续观战|观战端 AR 对齐保持稳定，不会持续累积漂移|||

---

#### 功能：观战一致性总检查（完整对局结束后观战端结果与玩家端一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SC-01 观战日志与玩家日志可对齐|完成一场标准比赛并导出 A / B / C 日志|C 侧关键观察事件可以与 A / B 的事件对齐|||
|SC-02 观战终态与玩家端一致|检查一局结束后的最终状态|C 的 HP、Dead、Winner / Loser / Draw 显示与玩家端一致|||
|SC-03 连续多局观战一致性|C 连续观战 3-5 局比赛|每局结果都正确；旧局残留不会污染新局|||
|SC-04 长时间运行后的观战一致性|系统长时间运行后再完成一局|C 依然能稳定观察到正确结果；无累计同步偏差|||
|SC-05 异常条件下观战端不破坏系统|比赛中触发乱序、重复投递、断线恢复等异常|C 可能短暂延迟显示，但不得污染系统状态或导致崩溃|||

---
