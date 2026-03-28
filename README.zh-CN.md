# 项目说明

## 1. 项目概述

本项目是一个基于 Unity 2022.3 LTS、PICO Unity Integration SDK 3.x 和 Android 平台的多人 MR 对战原型工程。项目目标是面向 PICO 4 Ultra 构建一个类似简化版 HADO 的混合现实对战体验，并在此基础上扩展 Host / Client / Spectator 三角色协同玩法。

项目核心特性：
- 全程 Passthrough 透视
- Host 权威判定
- Host / Client 双人对战
- Spectator 观众模式
- 多阶段手动对齐 / 本地可视化校准
- 局域网 UDP 联机
- 子弹、护盾、HP、结果页、重赛
- 观众加血、弹幕、本地音效、障碍墙系统

## 2. 技术栈

- Unity 2022.3.62f3
- Universal Render Pipeline 14.0.12
- TextMeshPro 3.0.7
- PICO Unity Integration SDK 3.3.0
- Android / IL2CPP / ARM64
- 局域网 UDP 自定义传输层
- 可选 OpenCVForUnity / AprilTag 自动识别能力（当前主流程已临时切到手动对齐为主）

> 当前工程主线目标设备是 PICO 4 Ultra。普通 PICO 4 可尝试运行，但不作为当前 MR 功能完整性的目标机型。

## 3. 目录结构

```text
Assets/_Project/
├─ Core/                 应用状态机、主流程、UI 视图、运行时总控
├─ Gameplay/
│  ├─ Combat/            子弹、远端代理、护盾、障碍墙
│  └─ Input/             输入抽象与 PICO 控制器输入实现
├─ MRWorld/              世界对齐、AprilTag、Spatial Anchor、远端校准控制器
├─ Networking/           UDP 传输、消息结构、网络协调器
├─ Prefabs/              预制体资源（当前项目运行时生成较多）
├─ Scenes/               场景文件（当前为 Main.unity）
├─ ScriptableObjects/    参数配置对象
├─ Tools/                构建后处理等工具脚本
└─ UI/                   3D UI 按钮等基础 UI 组件
```

## 4. 当前功能范围

### 4.1 玩家对战
- Host / Client 两名玩家对战
- 全程透视 MR 显示
- 右手发射子弹
- 护盾点击触发
- Host 负责命中裁决、HP 修改和结果广播
- 结果页支持双方确认后直接重开

### 4.2 三角色联机
- Host：权威服务器、状态推进者
- Client：远端玩家
- Spectator：旁观者与互动支持者

### 4.3 Spectator 功能
- 观看 Host / Client 视觉代理、护盾、子弹
- 对 Host / Client 投票加血
- 触发本地弹幕词汇（仅自己可见）
- 播放本地欢呼 / 掌声音效
- 放置障碍墙并影响战局

### 4.4 障碍墙系统
- Spectator 进入放置预览模式后调整墙的位置和旋转
- 由 Host 权威生成并广播墙状态
- 墙会随时间自动掉血
- 墙被子弹击中会扣血
- 墙血量归零后消失
- 墙上方带有红黑血条
- 墙面会随受损程度出现裂痕、分叉裂纹和崩边
- 子弹视觉体打到墙会立即本地销毁，不再穿墙

## 5. 应用状态机

项目使用 `AppStateMachine` 驱动主流程，主要状态如下：

- `Boot`
- `MainMenu`
- `RoleSelect`
- `LobbyHost`
- `LobbyClient`
- `LobbySpectator`
- `Calibration`
- `Playing`
- `Result`

状态主要由 [M0RuntimeBootstrap.cs](Assets/_Project/Core/M0RuntimeBootstrap.cs) 统一调度。

## 6. 角色与流程

### 6.1 Host
1. 进入 `LobbyHost`
2. 显示本机 `Local IP`
3. 等待 Client 和 Spectator 全部连接
4. 点击 `Start Match`
5. 进入 Calibration
6. 最终在第 5 步按 `Confirm`
7. 进入 Playing

### 6.2 Client
1. 进入 `LobbyClient`
2. 输入 Host IP
3. 点击 `Connect`
4. 等待 Host 开局
5. 进入 Calibration

### 6.3 Spectator
1. 进入 `LobbySpectator`
2. 输入 Host IP
3. 点击 `Connect`
4. 等待 Host 开局
5. 进入 Calibration
6. 进入 Playing 后使用观众控制面板

## 7. 校准流程（当前主线）

当前主线使用五步串行人工校准流程，确保同一时间只有一个设备可以调整，其他设备只能等待：

1. Client 调整自己看到的 Host 视觉体
2. Host 调整自己看到的 Client 视觉体
3. Spectator 调整自己看到的 Client 视觉体
4. Spectator 调整自己看到的 Host 视觉体
5. Host 最终确认并开始比赛

每一步都要求单独 `Confirm Step`，最后一步为 Host 的 `Confirm`。

### 7.1 校准输入
- 右摇杆：平移 XZ
- A / B：上移 / 下移
- 按住 X / Y：围绕可见头部持续旋转

### 7.2 校准原则
- 只调整“本机眼中的远端视觉显示”
- 不在当前阶段的设备不能修改对齐结果
- Host 最终确认前不会进入正式对战

## 8. 联网方式

项目没有使用 Mirror / NGO，而是实现了一套最小 UDP 联机层：

- `UdpLanTransport`：底层收发与 Host 中继
- `LanMessage`：消息结构
- `M3NetworkCoordinator`：高层协调与运行时事件分发

### 8.1 连接规则
- Host 在 Lobby 中监听 `UDP 27777`
- Client / Spectator 手动输入 Host IP 后连接
- Lobby 中会显示：
  - Host 的 `Local IP`
  - Client / Spectator 的 `Target Host IP`
  - 当前 UDP 端口
  - 最近一次网络诊断信息 `Diag`

### 8.2 主要消息类型
- Pose
- Shoot
- Shield
- HpUpdate
- MatchResult
- StartPlaying
- CalibrationReady
- RemoteAlignment
- RematchReady
- SpectatorVote
- ObstacleSpawnRequest
- ObstacleState

## 9. ScriptableObject 配置

### 9.1 CombatBalanceConfig
位置：`Assets/_Project/ScriptableObjects/CombatBalanceConfig.cs`

当前关键字段：
- `hp = 100`
- `damage = 10`
- `projectileSpeed = 5f`
- `projectileRadius = 0.033f`
- `shootCooldown = 0.5f`
- `shieldDuration = 1f`
- `shieldCooldown = 3f`

### 9.2 SpectatorSupportConfig
位置：`Assets/_Project/ScriptableObjects/SpectatorSupportConfig.cs`

当前关键字段：
- `healAmount = 10`
- `voteCooldown = 3f`
- `barrageWordA/B/C`
- `audioVolume = 0.9f`
- `wallMaxHp = 100`
- `wallDecayPerSecond = 5f`
- `wallShotDamage = 10`
- `wallPlacementDistance = 1.4f`
- `wallSpawnCooldown = 2f`
- `wallMaxActiveCount = 2`
- `wallSize = (1.6, 1.35, 0.12)`

## 10. 音频资源

默认本地观众音频资源位于：

- `Assets/Resources/Audio/cheer.ogg`
- `Assets/Resources/Audio/yay.ogg`

逻辑上：
- `Cheer` 优先读取配置中的 `cheerClip`，否则回退到 `yay.ogg`
- `Applause` 优先读取配置中的 `applauseClip`，否则回退到 `cheer.ogg`

## 11. 关键核心脚本

### Core
- `M0RuntimeBootstrap.cs`：整个项目的运行时主控
- `AppStateMachine.cs`：状态机容器
- `CalibrationView.cs`：校准 UI
- `LobbyView.cs`：联机 Lobby UI
- `SpectatorControlView.cs`：观众控制面板
- `SpectatorBarrageView.cs`：观众本地弹幕
- `SpectatorAudioPlayer.cs`：观众本地音频播放

### Networking
- `LanMessage.cs`：所有消息数据定义
- `UdpLanTransport.cs`：底层 UDP 收发与 Host 中继
- `M3NetworkCoordinator.cs`：运行时网络协调器

### Gameplay
- `M1ProjectileShooter.cs`：子弹发射
- `M1Projectile.cs`：子弹移动与碰墙销毁
- `M3RemotePlayerProxy.cs`：远端头手代理与血条显示
- `M5ShieldVisual.cs`：护盾可视化
- `WallObstacleRuntime.cs`：障碍墙实体、血条、裂痕表现

### MRWorld
- `RemoteAlignmentController.cs`：本地远端显示微调
- `OpenCVForUnityAprilTagDetector.cs`：AprilTag 检测
- `SpatialAnchorSyncService.cs`：共享空间锚能力封装

## 12. 打包与运行建议

### 12.1 Unity 设置建议
- 平台：Android
- 架构：IL2CPP + ARM64
- URP + Vulkan 时关闭 HDR
- 使用 PICO Integration SDK 路线，不启用 OpenXR 主路径

### 12.2 真机启动顺序建议
1. Host 启动并进入 Lobby
2. 记录 Host 页面上的 `Local IP`
3. Client 输入该 IP 并点击 `Connect`
4. Spectator 输入该 IP 并点击 `Connect`
5. 三角色全部连接后由 Host 点击 `Start Match`

### 12.3 打包前检查
- Console 无红色编译错误
- Main 场景已加入 Build Settings
- PICO 透视权限相关配置已完整
- 音频资源已导入
- ScriptableObject 引用已检查

## 13. 已知设计边界

- 当前主线校准不是共享绝对世界锚，而是多角色各自视角下的本地显示对齐
- Spectator 对 Host / Client 的校准也是本地视图校准，不反向影响玩家权威判定
- `Spatial Anchor` 和 `AprilTag` 能力保留在工程中，但当前主战斗流程主打人工对齐链路
- 普通 PICO 4 可尝试运行，但当前 MR 主体验以 PICO 4 Ultra 为主

## 14. 后续可扩展方向

- 完整共享空间锚接入
- 更稳定的 AprilTag / 多标记板自动对齐
- 障碍墙音效与受击反馈
- Spectator 弹幕广播到全员
- Spectator 放置更多可交互场景机关
- DebugHUD 完整化
- Spectator 回放 / 导播模式

## 15. 版本说明

当前工程代码版本戳：
- `MR-SPECTATOR-WALL-V1`

该版本包含：
- 三角色流程
- 五步串行校准
- 观众能力
- 障碍墙系统
- 墙血条 / 裂痕 / 碰墙销弹


