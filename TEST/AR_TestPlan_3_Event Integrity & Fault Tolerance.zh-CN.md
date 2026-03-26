### 3. 事件完整性 / 回放与容错
验证事件数据、回放链路以及异常输入处理是否可靠。

**包含内容**
- LAN 消息序列化
- Payload 解析安全性
- 消息类型路由
- 本地玩家消息过滤
- 缺失 / 非法 payload 处理
- JSON 缺失字段容忍
- RematchReady 消息处理
- SpectatorVote 消息处理
- ObstacleSpawn / ObstacleState 消息处理
- 连接握手健壮性

---

## 模块：事件完整性 / 回放与容错

> 说明：  
> - 本模块主要用于 **EditMode / 逻辑层与基础设施测试**。  
> - 重点验证当前版本中的 LAN 消息结构可靠性、JSON 解析安全性、消息路由以及异常或非预期网络输入下的容错能力，并覆盖重赛、Spectator 支援与障碍墙相关的新消息路径。  
> - 执行测试前请保持 `Actual Outcome` 和 `Status` 为空。  
> - `Status` 建议使用：`Not Run`、`Pass`、`Fail`、`Blocked`、`N/A`。  
> - 若某项依赖当前版本中尚未确认实现的功能，请先标记为 `Blocked`，并补充实现核查链接或说明。
>
> 量化通过标准：
> - 序列化后的浮点 / 向量字段在反序列化后，每个标量分量的误差不得超过 `0.001`。
> - 对于合法消息路由用例，必须且只能更新 `1` 组匹配的 pending payload / request flag；无关字段不得变化。
> - 对于非法、自发或不支持的消息，崩溃次数 = `0`，非预期状态变更次数 = `0`。
> - JSON 缺失字段或未知字段可以回退到默认值，但解析过程不得中断，也不得导致进程退出。
> - 重复握手流量在同一会话中对每一侧触发的连接回调次数不得超过 `1` 次。
> - Spectator 专属消息类型（`SPECTATOR_VOTE`、`OBSTACLE_SPAWN_REQUEST`）只有在发送方角色合法时才算通过；非法 senderRole 必须被拒绝且不得污染无关待处理状态。

### 执行总结

|项目|结果|
|---|---|
|执行结果|已完成|
|整体状态|Pass|
|通过率|27 / 27|
|Blocked / N/A|0 / 0|
|备注|核心 LAN 消息、重赛、Spectator 支援以及障碍墙消息路径相关用例均已执行并全部通过。|

---

#### 功能：消息序列化（当前 LAN payload 在 JsonUtility 序列化与反序列化后保持有效）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|SE-01 `ShootPayload` 序列化|对包含 spawn position、direction、speed、maxDistance、lifetime 的 `ShootPayload` 做序列化再反序列化|各字段在正常浮点容差内保持一致|往返序列化后，ShootPayload 各字段均保持在定义容差范围内。|Pass|
|SE-02 `ShieldPayload` 序列化|对带 `active` 与 `duration` 的 `ShieldPayload` 做序列化再反序列化|`active` 与 `duration` 在往返后保持不变|ShieldPayload 的各字段在往返序列化后保持不变。|Pass|
|SE-03 `HpUpdatePayload` 序列化|对包含 host/client HP 的 `HpUpdatePayload` 做序列化再反序列化|两个 HP 字段在往返后保持不变|HpUpdatePayload 在往返序列化后完整保留了 host/client HP。|Pass|
|SE-04 `MatchResultPayload` 序列化|对包含 `winnerRole` 的 `MatchResultPayload` 做序列化再反序列化|`winnerRole` 在往返后保持不变|MatchResultPayload 的 `winnerRole` 在往返后保持正确。|Pass|
|SE-05 `LanMessage` 外层封包序列化|对包含 `type`、`playerId` 与 payload JSON 字符串的 `LanMessage` 做序列化再反序列化|外层封包字段保持不变，payload 字符串被完整保留|LanMessage 外层封包字段与 payload 字符串在往返后均被正确保留。|Pass|
|SE-06 `RematchReadyPayload` 序列化|对包含 `ready = true/false` 的 `RematchReadyPayload` 做序列化再反序列化|`ready` 在往返后保持不变|RematchReadyPayload 在往返序列化后正确保留了 `ready` 字段。|Pass|
|SE-07 `SpectatorVotePayload` 序列化|对包含 `targetRole = Host/Client` 的 `SpectatorVotePayload` 做序列化再反序列化|`targetRole` 在往返后保持不变|SpectatorVotePayload 在往返序列化后正确保留了 `targetRole`。|Pass|
|SE-08 `ObstacleSpawnRequestPayload` 序列化|对包含 `anchorType`、`localOffset` 与 `yawOffset` 的 `ObstacleSpawnRequestPayload` 做序列化再反序列化|各字段在正常浮点容差内保持一致|ObstacleSpawnRequestPayload 在往返序列化后各字段均保持在定义容差范围内。|Pass|
|SE-09 `ObstacleStatePayload` 序列化|对包含 `obstacleId`、变换、尺寸、HP 与 `active` 的 `ObstacleStatePayload` 做序列化再反序列化|各字段在正常浮点容差内保持一致|ObstacleStatePayload 在往返序列化后正确保留了编号、变换、尺寸、HP 与激活状态。|Pass|

---

#### 功能：消息路由（收到的消息类型会被映射到正确的待处理 payload / 请求标记）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|RT-01 `SHOOT` 正确路由到待处理射击 payload|接收 `type = SHOOT` 且 payload JSON 合法的 `LanMessage`|`_pendingShoot` 被填充，`_remoteShootRequested` 变为 true|SHOOT 消息只更新了正确的待处理射击状态与请求标记。|Pass|
|RT-02 `SHIELD` 正确路由到待处理护盾 payload|接收 `type = SHIELD` 且 payload JSON 合法的 `LanMessage`|`_pendingShield` 被填充，`_remoteShieldRequested` 变为 true|SHIELD 消息被正确路由到护盾待处理状态。|Pass|
|RT-03 `HP_UPDATE` 正确路由到待处理 HP 更新 payload|接收 `type = HP_UPDATE` 且 payload JSON 合法的 `LanMessage`|`_pendingHpUpdate` 被填充，`_remoteHpUpdateRequested` 变为 true|HP_UPDATE 消息正确填充了 HP 更新待处理状态。|Pass|
|RT-04 `MATCH_RESULT` 正确路由到待处理结果 payload|接收 `type = MATCH_RESULT` 且 payload JSON 合法的 `LanMessage`|`_pendingMatchResult` 被填充，`_remoteMatchResultRequested` 变为 true|MATCH_RESULT 消息被正确路由到结果待处理状态。|Pass|
|RT-05 `REMATCH_READY` 正确路由到待处理重赛 payload|接收 `type = REMATCH_READY` 且 payload JSON 合法的 `LanMessage`|`_pendingRematchReady` 被填充，`_remoteRematchReadyRequested` 变为 true|REMATCH_READY 消息被正确路由到重赛待处理状态，且未污染无关字段。|Pass|
|RT-06 `SPECTATOR_VOTE` 仅在 Spectator 发送时有效路由|接收合法 `SPECTATOR_VOTE`，且 `senderRole = Spectator`|`_pendingSpectatorVote` 被填充，`_remoteSpectatorVoteRequested` 变为 true|来自合法 Spectator 发送方的 SPECTATOR_VOTE 被正确路由到待处理状态。|Pass|
|RT-07 `OBSTACLE_SPAWN_REQUEST` 仅在 Spectator 发送时有效路由|接收合法 `OBSTACLE_SPAWN_REQUEST`，且 `senderRole = Spectator`|`_pendingObstacleSpawnRequest` 被填充，`_remoteObstacleSpawnRequestRequested` 变为 true|来自合法 Spectator 发送方的 OBSTACLE_SPAWN_REQUEST 被正确路由到待处理状态。|Pass|
|RT-08 `OBSTACLE_STATE` 正确路由到待处理墙状态 payload|接收来自权威发送方的合法 `OBSTACLE_STATE`|`_pendingObstacleState` 被填充，`_remoteObstacleStateRequested` 变为 true|OBSTACLE_STATE 消息被正确路由到墙状态待处理状态。|Pass|
|RT-09 未知消息类型会被安全忽略|接收一个 `type` 不受支持的 `LanMessage`|不会崩溃，且不会修改无关待处理状态|未知消息类型被安全忽略，未导致崩溃或污染其他状态。|Pass|

---

#### 功能：容错与解析安全（格式损坏、自发消息或不完整消息不会破坏客户端）

|Test|Inputs|Expected Outcome|Actual Outcome|Status|
|---|---|---|---|---|
|EX-01 外层 JSON 非法|接收到不是合法 `LanMessage` JSON 的字节流|解析安全失败；不会崩溃|非法外层 JSON 被安全处理，未导致客户端崩溃。|Pass|
|EX-02 外层消息缺少 `type`|反序列化得到 `type` 为空或缺失的 `LanMessage`|该消息被安全忽略|缺少 `type` 的消息被正确忽略，未产生副作用。|Pass|
|EX-03 过滤本地自发消息|接收到 `playerId` 等于 `_localPlayerId` 的合法消息|该消息被忽略，不会污染远端状态|本地自发消息过滤正确生效，未污染远端状态。|Pass|
|EX-04 payload JSON 非法|接收到已知消息类型，但 payload JSON 损坏|`FromJson` 异常被捕获；不会崩溃，待处理状态保持不变|损坏的 payload JSON 被安全处理，未崩溃且未破坏待处理状态。|Pass|
|EX-05 payload 缺失部分字段|接收到字段不完整的已知 payload JSON|要么以默认值完成反序列化，要么安全忽略；不会崩溃|缺失字段的 payload 被安全回退或忽略，未导致崩溃。|Pass|
|EX-06 Spectator 专属消息的非法 senderRole|从非 `Spectator` 发送方收到 `SPECTATOR_VOTE` 或 `OBSTACLE_SPAWN_REQUEST`|消息被安全忽略；Spectator 支援相关待处理状态不会被污染|非法 senderRole 的 Spectator 专属消息被安全忽略，未污染相关待处理状态。|Pass|
|EX-07 `SpectatorVotePayload` 的目标角色非法或为空|收到 `targetRole` 为空或不受支持的 `SPECTATOR_VOTE`|消息被安全忽略或保持无效状态；不会崩溃，也不会改写无关状态|非法或空目标角色的 SpectatorVotePayload 被安全处理，未崩溃且未改写无关状态。|Pass|
|EX-08 `ObstacleStatePayload` 缺失关键字段|收到字段不完整的 `OBSTACLE_STATE` JSON|反序列化要么安全回退，要么被安全忽略；不会崩溃|字段不完整的 ObstacleStatePayload 被安全回退处理，未导致崩溃或状态污染。|Pass|
|EX-09 握手健壮性|Client 在收到 `HELLO_ACK` 前反复发送 `HELLO`，或 Host 在已连接后再次收到重复 `HELLO`|连接状态保持稳定；不会崩溃，也不会反复触发连接回调风暴|重复握手流量下连接状态保持稳定，未出现崩溃或回调风暴。|Pass|

---
