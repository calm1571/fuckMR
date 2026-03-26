# 发布版打包说明

## 1. 适用范围

本文档用于当前 FuckMR 工程的发布版 / 验收版打包流程说明，目标是让项目在不改代码的前提下，稳定完成 Android 真机打包、安装和联机验收。

当前主目标设备：
- PICO 4 Ultra

## 2. 打包前确认

### 2.1 Unity 版本
必须使用当前项目验证过的 Unity 版本：
- `Unity 2022.3.62f3`

不要随意升级 Unity 小版本或切换到其他 LTS 版本后直接打包，否则容易引入：
- PICO SDK 兼容问题
- IL2CPP 体积 / 编译问题
- Android 构建链差异

### 2.2 目标平台
在 `Build Settings` 中确认：
- `Platform = Android`
- 主场景 `Assets/_Project/Scenes/Main.unity` 已加入 `Scenes In Build`

### 2.3 输入与 XR 路线
当前项目是：
- PICO Integration SDK 路线
- 非 OpenXR 主路径
- 不依赖 XR Interaction Toolkit 作为整套游戏框架，但项目中保留了部分 XRI / 控制器组件配合 PICO 使用

不要在未评估前改动：
- XR Plug-in Management 主设置
- PICO XR 相关 Provider
- 场景中的 PICO / Camera / Controller 绑定对象

### 2.4 渲染与透视
当前工程使用 URP，若走 Vulkan：
- 必须确认 `HDR` 关闭

否则可能导致：
- 视频透视无效
- Passthrough 黑屏或失效

### 2.5 Console 检查
打包前必须确认 Unity Console：
- 没有红色编译错误
- 没有脚本 missing
- 没有新资源导入失败

尤其注意：
- 新增 `.cs` 是否已被 Unity 正常导入
- `PXR_ProjectSetting.asset` 是否存在且可被 SDK 正常读取

## 3. 关键资源与配置检查

### 3.1 音频资源
确认以下资源存在：
- `Assets/Resources/Audio/cheer.ogg`
- `Assets/Resources/Audio/yay.ogg`

### 3.2 ScriptableObject
确认以下配置资源已正确挂到运行时主控或被场景引用：
- `CombatBalanceConfig`
- `SpectatorSupportConfig`

重点检查字段：
- 玩家 HP / Damage / Cooldown
- Spectator healAmount / voteCooldown
- Wall obstacle 的 HP、掉血、伤害、尺寸、最大数量

### 3.3 Host IP 机制
当前版本已经改为：
- Host 在 Lobby 显示 `Local IP`
- Client / Spectator 手动输入 `Host IP`

因此打包前无需再把 Host IP 写死到场景作为唯一方式。

## 4. 打包设置建议

### 4.1 Player Settings
建议保持当前已验证过的设置，不要临时调整：
- `Scripting Backend = IL2CPP`
- `Target Architectures = ARM64`
- Android 包名、版本号、签名与 keystore 保持稳定

### 4.2 签名
如果用于重复安装、版本迭代、测试覆盖安装：
- 必须使用同一个 keystore 签名

否则会出现：
- 旧包无法覆盖安装
- 不同设备上版本管理混乱

### 4.3 构建类型
建议发布验收包时使用：
- `Build` 或 `Build And Run`

若 `Build And Run` 在 Unity Editor 端出现 Android 启动异常：
- 优先先 `Build APK`
- 再通过设备文件管理或 `adb install -r` 安装

## 5. 推荐打包流程

### 5.1 清理确认
1. 打开 Unity 工程
2. 等待所有资源导入完成
3. 确认 Console 无红错
4. 确认主菜单版本戳正确显示在代码中

### 5.2 Build Settings
1. 打开 `File -> Build Settings`
2. 选择 `Android`
3. 确认 `Main.unity` 在 `Scenes In Build`
4. 点击 `Build`

### 5.3 输出路径
建议输出到独立目录，例如：
- `Builds/Android/`

文件名建议带日期和版本，例如：
- `FuckMR_MR-SPECTATOR-WALL-V1_2026-03-23.apk`

## 6. 安装建议

### 6.1 覆盖安装前
若发现功能与最新代码不一致：
- 先卸载设备上的旧包
- 再安装新包

原因：
- 头显上旧包未正确替换时，最容易误判为“代码没生效”

### 6.2 三台设备验收前准备
建议准备三台设备：
- Host
- Client
- Spectator

并确保：
- 三台设备在同一热点 / 同一路由器网络
- Host 先启动进入 Lobby
- Client / Spectator 根据 Host 页面显示的 `Local IP` 手动输入后连接

## 7. 验收流程建议

### 7.1 基础启动验收
检查：
- 应用启动即透视
- 主菜单可见且比例正常
- 版本戳显示正确

### 7.2 联机验收
检查：
- Host Lobby 显示 `Local IP`
- Client / Spectator 可输入 `Host IP`
- 三角色都能进入房间
- Host 只有在 Client + Spectator 全连接后才能开始

### 7.3 五步校准验收
依次检查：
1. `ClientAdjustHost`
2. `HostAdjustClient`
3. `SpectatorAdjustClient`
4. `SpectatorAdjustHost`
5. `HostFinalConfirm`

要求：
- 每步都必须单独确认
- 非当前步骤设备不能调整
- 阶段推进三端同步

### 7.4 Playing 验收
检查：
- Host / Client 均可看到对方
- 子弹、护盾、命中、HP 正常
- Spectator 能看到双方视觉体、护盾、子弹
- Spectator 控制面板可用

### 7.5 Spectator 功能验收
检查：
- `Heal Host / Heal Client`
- 本地弹幕
- 本地欢呼 / 掌声
- `Place Wall`
- 墙体血条、裂痕、掉血、挡弹

### 7.6 Result / Retry 验收
检查：
- 结果页显示 `WIN / LOSE`
- 双方 `Retry` 后直接重新进入 `Playing`
- 不重新回 Calibration
- 保留当前对齐结果

## 8. 常见问题

### 8.1 三台设备连不上
优先检查：
- Host 的 `Local IP`
- Client / Spectator 输入的 `Host IP`
- Lobby 里的 `Diag`

### 8.2 打出来功能和代码不一致
优先排查：
- 设备里跑的是不是旧包
- 是否真正覆盖安装成功
- 是否用了错误的工程目录或旧构建产物

### 8.3 墙血条/裂痕/挡弹不生效
优先确认：
- 当前包是否为最新构建
- 三端是否都安装同一版本
- Spectator 是否成功创建权威墙体而不是只停留在 Preview

## 9. 不建议在发布前临时改动的内容

发布前不要临时修改：
- Networking 消息结构
- 校准状态机阶段顺序
- WorldRoot / 对齐根节点关系
- 墙体 HP / 血条 / 裂痕核心逻辑
- PICO SDK / Unity 版本
- Android 签名配置

## 10. 发布建议

如果要对外发测试包，建议同时附上：
- 设备启动顺序
- Host IP 输入说明
- 三角色分工说明
- 五步校准说明
- 对战 / 观战功能清单

建议把当前 README 一起发给测试人员或技术同事。
