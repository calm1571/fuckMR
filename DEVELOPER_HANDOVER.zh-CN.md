# 开发者接手文档 / 二开指南

## 1. 文档目标

本文档面向后续接手本工程的开发者，重点说明：
- 当前工程的架构分层
- 哪些文件是核心入口
- 哪些功能是 Host 权威
- 哪些改动风险高
- 后续二开时的推荐流程

## 2. 项目定位

这个项目不是通用 XR 模板，而是一个明确面向多人 MR 对战的定制原型，包含三种运行角色：
- Host
- Client
- Spectator

项目同时叠加了多层能力：
- 应用状态机
- MR 透视 / 世界管理
- 本地 / 远端可视化对齐
- Host 权威战斗逻辑
- Spectator 互动系统

因此任何大改都不能只从单一模块看，而要评估它对整条运行链路的影响。

## 3. 运行时总入口

主入口文件：
- `Assets/_Project/Core/M0RuntimeBootstrap.cs`

这个文件是当前工程的运行时总控，负责：
- 状态机初始化
- UI 构建
- 角色选择
- 联机启动
- 校准流程
- Playing 流程
- HP / 命中 / 结果页
- Spectator 系统
- 墙体系统

### 接手建议
不要把 `M0RuntimeBootstrap` 当作“普通小脚本”来改。它本质上是整个项目的集成层。

如果后续要重构，建议逐步拆分，而不是一次性大改。

## 4. 架构分层

### 4.1 状态机层
文件：
- `AppStateMachine.cs`
- `IAppState.cs`
- `BootState.cs`
- `MainMenuState.cs`
- `RoleSelectState.cs`
- `LobbyHostState.cs`
- `LobbyClientState.cs`
- `LobbySpectatorState.cs`
- `CalibrationState.cs`
- `PlayingState.cs`
- `ResultState.cs`

职责：
- 维护应用顶层状态切换
- 尽量少放业务逻辑
- 主要执行权仍回到 `M0RuntimeBootstrap`

### 4.2 网络层
文件：
- `LanMessage.cs`
- `NetworkRole.cs`
- `UdpLanTransport.cs`
- `M3NetworkCoordinator.cs`

职责：
- 消息定义
- 底层传输与 Host 中继
- 上层事件派发

边界规则：
- Host 仍是唯一权威裁决者
- Client / Spectator 不应直接改权威战斗状态
- Spectator 的交互必须先转成请求，再由 Host 裁决

### 4.3 MR / 对齐层
文件：
- `RemoteAlignmentController.cs`
- `WorldRootController.cs`
- `SpatialAnchorSyncService.cs`
- `AprilTagAutoTrackingSource.cs`
- `OpenCVForUnityAprilTagDetector.cs`
- `ManualMarkerTrackingSource.cs`
- 相关接口文件

职责：
- WorldRoot 管理
- Marker / AprilTag / Spatial Anchor 封装
- 本地远端视觉体的手动微调

当前主线：
- 真实对战主流程以“五步串行人工校准”为主
- AprilTag 和 Spatial Anchor 代码仍保留，但不是当前主战斗入口链路

### 4.4 玩法层
文件：
- `M1ProjectileShooter.cs`
- `M1Projectile.cs`
- `M3RemotePlayerProxy.cs`
- `M5ShieldVisual.cs`
- `WallObstacleRuntime.cs`

职责：
- 子弹生成与视觉
- 护盾显示
- 远端玩家代理
- 墙体实体、血条、裂痕、局部碰撞反馈

### 4.5 Spectator 层
文件：
- `SpectatorControlView.cs`
- `SpectatorBarrageView.cs`
- `SpectatorAudioPlayer.cs`
- `M0RuntimeBootstrap.cs` 中的观众逻辑

职责：
- 观众控制面板
- 本地弹幕
- 本地音频
- 加血、放墙等观众交互

## 5. 当前校准模型

当前校准流程是 5 步串行流程：
1. Client 调 Host
2. Host 调 Client
3. Spectator 调 Client
4. Spectator 调 Host
5. Host 最终 Confirm

关键约束：
- 一次只有一台设备能调
- 非当前阶段设备只能等待
- 阶段推进依赖 `RemoteAlignment` 消息同步

### 为什么采用这套方案
当前项目主线并不是“完整共享空间锚”方案，而是一个更务实的“局部视觉对齐”方案，用来保证在现有设备和流程下尽量可玩。

### 修改风险
如果你要改校准逻辑，必须整轮回归：
- 三端阶段同步
- 非当前阶段设备不可操作
- Host 最终 Confirm 门槛
- 观众 3/4 步确认消息是否转发到 Host / Client

## 6. 权威模型

### Host 权责
Host 负责：
- 命中判定
- HP 修改
- 结果页裁决
- 墙体权威生成与销毁
- 观众投票加血裁决

### Client 权责
Client 负责：
- 本地输入
- 本地可视化
- 向 Host 发送动作请求

### Spectator 权责
Spectator 负责：
- 本地观察与本地校准
- 本地 UI / 弹幕 / 音频
- 向 Host 发送支持型请求
- 不直接参与权威命中逻辑

## 7. 墙体系统说明

关键文件：
- `Assets/_Project/Gameplay/Combat/WallObstacleRuntime.cs`

当前墙体系统分两层：
1. `Preview Wall`
2. `Runtime Wall`

### Preview Wall
- Spectator 本地预览
- 可平移、升降、旋转
- 不参与真实判定
- 不应被当成真正墙体

### Runtime Wall
- 由 Host 权威生成
- 广播到所有端
- 有 HP、自动掉血、裂痕、血条
- 能挡住子弹并造成墙体扣血

### 二开注意
不要把 Preview 和 Runtime 混在一起，否则很容易出现：
- 观众本地看到了墙
- 但 Host 权威世界里根本没有墙

## 8. 网络扩展规则

如果要加新的互动功能，建议严格按这个顺序扩展：
1. 在 `LanMessage.cs` 增加 payload
2. 在 `M3NetworkCoordinator.cs` 增加 send API 和接收事件
3. 在 `UdpLanTransport.cs` 增加底层发送 / 必要中继逻辑
4. 决定该消息是否只给 Host，还是由 Host 再转发
5. 在 `M0RuntimeBootstrap.cs` 消费这个事件

### 不建议的做法
不要：
- 在非 Host 设备直接修改权威战斗状态
- 用本地视觉状态替代权威状态
- 绕开既有网络层直接在多个模块里散写 socket 逻辑

## 9. 推荐重构方向

项目现在能跑，但集成层比较集中。

### 最适合后续拆分的方向
1. 从 `M0RuntimeBootstrap` 中拆出：
- CalibrationCoordinator
- CombatCoordinator
- SpectatorCoordinator
- WallObstacleCoordinator

2. 拆出 UI 构建器：
- Lobby
- Calibration
- Result
- Spectator 面板

3. 拆出网络消息域处理：
- 校准消息
- 战斗消息
- Spectator 消息
- 障碍墙消息

### 重构原则
不要一次同时改多个底层系统。每次只拆一个方向，并保持运行结果不变。

## 10. 高风险文件

以下文件改动时要格外谨慎：
- `Assets/_Project/Core/M0RuntimeBootstrap.cs`
- `Assets/_Project/Networking/M3NetworkCoordinator.cs`
- `Assets/_Project/Networking/UdpLanTransport.cs`
- `Assets/_Project/Gameplay/Combat/WallObstacleRuntime.cs`
- `Assets/_Project/Gameplay/Combat/M1ProjectileShooter.cs`
- `Assets/_Project/Gameplay/Combat/M3RemotePlayerProxy.cs`

原因：
这些文件都位于“模块交叉点”，一处改动可能连带影响：
- 校准
- 联机
- 命中
- Spectator
- 墙体复制

## 11. 推荐开发流程

后续二开建议按以下顺序：
1. 先判断功能是“纯本地视觉”还是“影响权威逻辑”
2. 如果影响权威逻辑，先设计 Host 裁决路径
3. 再扩消息结构与传输
4. 再改运行时主控
5. 最后做三端联调

## 12. 二开回归检查表

任何非小改都建议回归以下内容：
- 启动与透视
- Host / Client / Spectator 联机
- 五步校准
- Host 最终 Confirm
- Playing 进入
- 子弹 / 护盾 / HP 循环
- Result / Retry
- Spectator 控制面板
- 墙体生命周期

## 13. 已知边界

- 当前主线不是完整共享绝对空间锚方案
- Spectator 校准只影响 Spectator 自己的视图
- 普通 PICO 4 不是当前首要验证机型
- 某些环境下无法直接通过命令行 `dotnet build` 验证 Unity C# 工程

## 14. 新功能风险分级建议

### 低风险功能
- 本地 UI
- 文档
- 本地表现层反馈
- Spectator 本地视觉增强

### 中风险功能
- Spectator 新增请求型能力
- DebugHUD 扩展
- 墙体参数扩展

### 高风险功能
- 校准流程改动
- 网络中继改动
- 权威命中逻辑改动
- WorldRoot / 对齐根关系改动
- 替换当前权威模型

## 15. 新接手开发者阅读顺序

建议按这个顺序读代码：
1. `README.zh-CN.md`
2. `M0RuntimeBootstrap.cs`
3. `M3NetworkCoordinator.cs`
4. `UdpLanTransport.cs`
5. `RemoteAlignmentController.cs`
6. `WallObstacleRuntime.cs`
7. `M1ProjectileShooter.cs`
8. `M3RemotePlayerProxy.cs`

## 16. 接手建议

如果你是第一次接手这个工程，不建议一上来就做大重构。

建议先做：
- 小范围 UI 调整
- 小范围参数调整
- 小范围观众功能扩展

等真正理解了以下三件事后，再考虑大改：
- 哪些是 Host 权威
- 哪些只是本地视觉补偿
- 哪些对齐偏移是“观察者相关”的

在此之前，尽量不要直接替换：
- 校准模型
- 网络模型
- 世界锚定模型

