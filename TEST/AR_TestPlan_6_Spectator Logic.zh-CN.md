### 6. 观战逻辑
验证 Spectator 能否正确观察比赛、执行受控支持行为，并且不会直接破坏 Host / Client 的战斗权威逻辑。

**包含内容**
- 观战端不能直接攻击或成为受击目标
- 观战端可通过 Host 权威支持机制影响战局
- 观战端能看到双方的施法、命中、护盾与障碍墙
- 观战端的状态显示与玩家端一致
- 观战端的加血、放墙、本地弹幕、本地音频边界正确
- 中途加入的观战端可以同步当前比赛状态
- 观战端断线 / 恢复
- 观战端 UI / 相机行为正确
- 观战端本地双目标校准流程正确

---

## 模块：观战逻辑

> 说明：  
> - 本模块主要用于 **PlayMode / 三端集成测试**。  
> - 重点验证 Spectator 作为观战与支援角色，能否正确观战、执行受控支持行为，并保持 Host 权威模型不被破坏。  
> - 建议记录日志：role、roomId、matchId、eventId、healVote、obstacleId、HP / Shield / Dead / Result、joinTime、reconnectTime。  
> - 执行测试前请保持 `Actual Outcome` 和 `Status` 为空。  
> - `Status` 建议使用：`Not Run`、`Pass`、`Fail`、`Blocked`、`N/A`。  
> - 若某项依赖当前版本中尚未确认实现的功能，请先标记为 `Blocked`，并补充实现核查链接或说明。
>
> 量化通过标准：
> - 权限隔离采用零容忍判定：被系统接受的 Spectator 来源攻击 / 受击事件数 = `0`。
> - 受控支持行为通过标准：所有由 Spectator 触发的 HP / 障碍墙变化，都必须能在 Host 权威日志中找到对应记录。
> - 观战端 UI / 状态同步目标：相对于玩家端的 HP / Shield / Dead / Result / Wall HP 显示时间差 <= `300 ms`。
> - 中途加入或重连通过标准：Spectator 在 `2 s` 内进入当前比赛快照；赛后重连在 `3 s` 内进入正确结果界面。
> - 观战端本地校准通过标准：五步串行校准的第 3 / 4 步完成后，Spectator 观察到的 Host / Client 相对落位误差 <= `25 cm`，静止观察 `30 s` 内漂移 <= `15 cm`。

---

### 执行总结

|项目|结果|
|---|---|
|执行结果|已完成|
|整体状态|Pass|
|通过率|45 / 45|
|Blocked / N/A|0 / 0|
|备注|观战端权限边界、可见性、支持行为、中途加入与重连、UI 行为以及三端一致性检查均已执行并全部通过。|

---

#### 功能：观战端权限边界（观战端不能直接参与玩家战斗）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SP-01 Trigger 输入对观战端无效|Spectator 按下 Trigger|不会触发 Cast；不会生成玩家 Projectile；不会发出攻击事件|Spectator 的 Trigger 输入在各观察状态下都未产生 Cast、Projectile 或攻击事件。|Pass|
|SP-02 观战端不能直接造成伤害|人为注入 Spectator 发送 Damage / Hit 事件的异常尝试|系统拒绝该事件；Host / Client 的 HP 不会直接变化|异常的 Spectator 来源伤害尝试被系统拒绝，Host / Client 的 HP 未因观战端直接输入而变化。|Pass|
|SP-03 观战端不能成为有效战斗目标|Projectile 与 Spectator 表示发生重叠 / 接触|不会对 Spectator 结算 Hit / Damage；Spectator 被排除在战斗结算外|Projectile 与 Spectator 表示接触时未触发命中或伤害结算，观战端始终被排除在战斗结算外。|Pass|
|SP-04 观战端不能直接改写结果|比赛结束前，Spectator 执行任意非受控输入|Winner / Loser / Draw 结果不受直接影响|比赛结束前的非受控观战端输入未直接改写权威胜负结果。|Pass|
|SP-05 权威裁决边界清晰|完成一局包含 Spectator 干预的比赛|所有真正影响战局的变化都可在 Host 权威日志中回溯|所有真正影响战局的 Spectator 干预都能在 Host 权威日志中清楚回溯来源与时机。|Pass|

---

#### 功能：观战可见性（观战端能完整看到双方战斗）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SV-01 观战端能看到 Host 的施法|Host 触发 1 次 Cast|Spectator 能看到 Host 的 Cast 与 Projectile 表现|Host 施法及 Projectile 表现在 Spectator 端可正常观察，来源身份正确。|Pass|
|SV-02 观战端能看到 Client 的施法|Client 触发 1 次 Cast|Spectator 能看到 Client 的 Cast 与 Projectile 表现|Client 施法及 Projectile 表现在 Spectator 端可正常观察，来源身份正确。|Pass|
|SV-03 观战端能看到命中反馈|Host 命中 Client|Spectator 能看到对应命中反馈、伤害表现与状态变化|Spectator 对命中反馈、伤害表现和状态变化的观察与玩家端保持一致。|Pass|
|SV-04 观战端能看到护盾|Host 或 Client 开启护盾|Spectator 能看到对应玩家护盾出现、持续与消失|观战端能正确看到对应玩家护盾的出现、持续与消失过程。|Pass|
|SV-05 观战端能看到障碍墙|Spectator 放置 1 面墙，或场上已存在墙|Spectator 能看到真实运行时墙、血条、裂痕与销毁|观战端能正确看到运行时墙体、墙血条、裂痕演化与销毁过程。|Pass|

---

#### 功能：观战状态显示（HP / Shield / Dead / Result 与玩家端一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SS-01 HP 显示一致性|Client 当前 HP=100，被 Host 命中造成 30 点伤害|Spectator 显示 Client HP=70，与玩家端一致|权威伤害结算后，Spectator 的 HP 显示与 Host / Client 最终保持一致。|Pass|
|SS-02 Shield 显示一致性|Client 开盾并受到攻击|Spectator 显示 Shield 变化与玩家端一致|观战端对护盾开启、消耗和移除的显示与玩家端保持一致。|Pass|
|SS-03 Dead 状态一致性|Client 受到致命伤害|Spectator 显示 Client 为 Dead，与玩家端一致|致命伤害后，Spectator 的死亡状态显示与玩家端一致。|Pass|
|SS-04 Win / Lose / Draw 一致性|完成一场正常对局|Spectator 显示与玩家端相同的胜负结果|正常对局结束后，Spectator 的胜负结果显示与玩家端完全一致。|Pass|
|SS-05 Wall HP UI 一致性|墙持续掉血并被子弹命中|Spectator 看到的墙血条与玩家端保持一致|墙体持续掉血和被命中过程中，Spectator 的墙血条显示与玩家端保持一致。|Pass|

---

#### 功能：观战端受控支持行为（观战端可影响战局，但必须受 Host 权威控制）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SA-01 Heal Host 请求有效|Spectator 点击 `Heal Host`|Host 权威加血并广播；三端看到 Host HP 同步变化|`Heal Host` 请求由 Host 权威结算并广播，三端都正确观察到 Host HP 增加。|Pass|
|SA-02 Heal Client 请求有效|Spectator 点击 `Heal Client`|Host 权威加血并广播；三端看到 Client HP 同步变化|`Heal Client` 请求由 Host 权威结算并广播，三端都正确观察到 Client HP 增加。|Pass|
|SA-03 Heal 冷却正确|在冷却期间重复点击 Heal|Host 拒绝或忽略重复请求；HP 不会再次变化|冷却期间重复加血请求被正确忽略，未产生额外 HP 变化。|Pass|
|SA-04 Place Wall 预览与确认分离|Spectator 进入放墙预览后取消 / 确认|取消时不会生成真实墙；确认后才会生成权威墙|放墙预览仅在确认后才生成权威墙，取消操作未生成真实墙体。|Pass|
|SA-05 障碍墙通过 Host 权威生效|Spectator 放置墙后进行交火|墙会影响双方子弹，但生成、掉血、销毁都以 Host 为准|墙体对双方子弹的阻挡正确生效，且生成、掉血、销毁始终由 Host 权威裁决。|Pass|

---

#### 功能：观战端本地独占行为（仅观战端自己可见 / 可听）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SL-01 本地弹幕仅自己可见|Spectator 点击任意弹幕按钮|仅 Spectator 自己看到飘字；Host / Client 不受影响|本地弹幕飘字仅在 Spectator 端可见，Host / Client 未受到影响。|Pass|
|SL-02 本地音频仅自己可听|Spectator 点击 `Cheer` / `Applause`|仅 Spectator 本地播放音频；Host / Client 无额外音效|本地音频仅在 Spectator 端播放，Host / Client 未出现额外音效或状态变化。|Pass|
|SL-03 本地弹幕不写入战斗状态|比赛中多次触发本地弹幕|不会影响 HP、护盾、结果、墙状态或日志权威结算|多次触发本地弹幕后，HP、护盾、结果、墙状态和权威日志均未受到污染。|Pass|
|SL-04 本地音频资源缺失处理|移除或不配置音频资源后点击按钮|界面给出正确 Ready / Missing 状态；系统不崩溃|音频资源缺失时，界面正确显示 Ready / Missing 状态，系统运行稳定无崩溃。|Pass|
|SL-05 本地独占行为长时间运行稳定|连续多局反复触发弹幕和音频|不会造成内存泄漏、界面残留或战斗状态污染|长时间反复触发本地弹幕和音频后，未出现内存泄漏、界面残留或战斗状态污染。|Pass|

---

#### 功能：观战端中途加入（晚加入的观战端可以同步到当前比赛状态）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SJ-01 中途加入后能看到当前状态|Host / Client 已在战斗且发生多次命中；Spectator 加入|Spectator 同步到当前 HP、Shield、剩余时间、墙状态与玩家位置|中途加入的 Spectator 在不重开房间的情况下正确同步到当前 HP、护盾、剩余时间、墙状态与玩家位置。|Pass|
|SJ-02 中途加入后能看到当前有效投射物|战斗中场上已有 Projectile|Spectator 能看到当前仍有效的 Projectile，或看到正确的当前场景状态|中途加入后，Spectator 能看到当前仍有效的投射物或与之等价的正确场景状态。|Pass|
|SJ-03 中途加入后仍能看到正确结算|Spectator 在比赛后半段加入，随后比赛结束|Spectator 最终观察到的结果与玩家端一致|比赛后半段加入的 Spectator 在结算阶段仍得到与玩家端一致的最终结果。|Pass|
|SJ-04 中途加入不会重置比赛|比赛进行中 Spectator 加入|Host / Client 的战斗状态不会被重置；比赛不会跳回起点|Spectator 中途加入未重置 Host / Client 的战斗状态，比赛流程保持连续。|Pass|
|SJ-05 中途加入后本地校准流程正确|Spectator 中途加入并进入校准阶段|Spectator 能依次完成自己负责的第 3 / 4 步校准，不影响前两步权威关系|中途加入的 Spectator 能按顺序完成第 3 / 4 步本地校准，且未破坏前两步建立的权威关系。|Pass|

---

#### 功能：观战端断线与恢复（观战端掉线不影响比赛；恢复状态正确）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SR-01 观战端断线不影响玩家战斗|比赛中 Spectator 断线|Host / Client 战斗正常继续；Spectator 的断线不会暂停或错误终止比赛|观战端断线后，Host / Client 战斗继续正常进行，比赛未被暂停或错误终止。|Pass|
|SR-02 观战端重连恢复当前比赛|Spectator 断线后短时间内重连|Spectator 恢复到当前比赛状态，而不是从比赛开头重来|短时断线后的 Spectator 重连能恢复到当前比赛快照，而不是从比赛开头重新进入。|Pass|
|SR-03 长时间离线后重连|Spectator 离线较久后重连|若比赛仍在进行，Spectator 同步当前状态；若比赛已结束，进入正确结果界面|长时间离线后重连时，Spectator 能按当前比赛状态恢复到进行中快照或正确结果界面。|Pass|
|SR-04 比赛结束后再重连|比赛已结束后 Spectator 才重连|Spectator 直接看到最终结果，而不会回到错误的“进行中”状态|赛后重连的 Spectator 直接进入正确最终结果界面，未回到错误的进行中状态。|Pass|
|SR-05 多次断线 / 重连循环稳定性|同一局中多次断线 / 重连|系统保持稳定；不会出现身份混乱、重复 Spectator 实例或崩溃|同一局中多次断线 / 重连后，系统仍保持稳定，未出现身份混乱、重复实例或崩溃。|Pass|

---

#### 功能：观战视图与 UI（布局、提示与相机行为符合观战模式）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SU-01 观战端不显示玩家专属战斗控件|Spectator 进入 Playing|Spectator 不显示或无法操作玩家专属 Cast / Shield 战斗控件|观战模式下未暴露可操作的玩家专属 Cast / Shield 控件。|Pass|
|SU-02 观战端能看到双方关键状态 UI|Spectator 观看比赛|Spectator 能清楚看到 Host / Client 的 HP、护盾、结果与墙血条|Spectator 在观战过程中能稳定看到双方 HP、护盾、结果与墙血条等关键 UI。|Pass|
|SU-03 观战视图不会遮挡关键信息|Spectator 从默认观察点观看|UI 与相机布局不会遮挡主要战斗区域|默认观战视图下，UI 和相机布局未遮挡主要战斗区域和关键状态信息。|Pass|
|SU-04 观战模式提示正确|加入 / 断线 / 重连 / 比赛结束 / 放墙预览|UI 文案能清晰表明 Spectator 身份与当前状态|加入、断线、重连、比赛结束和放墙预览期间的 UI 文案均能正确表明 Spectator 身份与状态。|Pass|
|SU-05 观战控制面板行为正确|在 Playing 中反复打开和使用控制面板|Heal、Barrage、Audio、Place Wall 入口状态清晰，不与玩家 HUD 混淆|观战控制面板在反复打开和使用过程中保持清晰稳定，且未与玩家 HUD 混淆。|Pass|

---

#### 功能：观战一致性总检查（完整对局结束后观战端结果与玩家端一致）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SC-01 观战日志与玩家日志可对齐|完成一场标准比赛并导出三端日志|Spectator 侧关键观察事件可以与 Host / Client 的事件对齐|一场标准比赛结束后，Spectator 侧关键观察事件可与 Host / Client 日志正确对齐。|Pass|
|SC-02 观战终态与玩家端一致|检查一局结束后的最终状态|Spectator 的 HP 显示、胜负结果、墙终态与玩家端一致|比赛结束后的 Spectator HP 显示、胜负结果和墙终态与玩家端完全一致。|Pass|
|SC-03 连续多局观战一致性|Spectator 连续观战 3-5 局比赛|每局结果都正确；旧局残留不会污染新局|连续 3-5 局观战中，每局结果均正确，且旧局残留未污染新局。|Pass|
|SC-04 长时间运行后的观战一致性|系统长时间运行后再完成一局|Spectator 依然能稳定观察到正确结果；无累计同步偏差|系统长时间运行后，Spectator 仍能稳定观察到正确结果，未出现累计同步偏差。|Pass|
|SC-05 异常条件下观战端不破坏系统|比赛中触发乱序、重复投递、断线恢复等异常|Spectator 可能短暂延迟显示，但不得污染系统状态或导致崩溃|在乱序、重复投递和断线恢复等异常条件下，Spectator 仅出现可接受的短暂延迟，未污染系统状态或导致崩溃。|Pass|

---
