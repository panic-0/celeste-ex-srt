# ex-srt 中文使用手册

`ex-srt` 是一个 Celeste Everest mod。它允许你在地图编辑器里给房间标记自动存档区域；当角色在关卡中进入这些区域时，mod 会调用 Speedrun Tool 的存档功能，把状态保存到 `Default Slot`。

## 使用前提

- 已安装 Everest
- 已安装 Speedrun Tool
- `ex-srt` 已正确放入 `Celeste\Mods\ex-srt`

## 核心工作方式

1. 你先在地图编辑器里给房间涂出一个或多个区域
2. 进入游戏后，角色第一次进入这些区域时会触发 ex-srt
3. ex-srt 会调用 Speedrun Tool，把状态保存到 `Default Slot`

当前版本不会由 ex-srt 自己主动 `LoadState`。  
如果开启 `Disable SRT Freeze On Auto-Save`，ex-srt 会在这一次自动保存时尽量取消 SRT 的冻结和擦屏效果，但不会永久修改你之后手动使用 SRT 的行为。

## 地图编辑器操作

- 画区域：`Alt + 左键`
- 擦区域：`Alt + 右键`
- 清空当前房间标记：使用 `Clear Current Room Markers` 绑定
- 关卡内望远镜编辑：进入原版望远镜或 SRT 便携望远镜后，按 `Toggle Lookout Edit` 进入编辑；`左键` 画，`右键` 擦

这些标记是按“房间”保存的，不是全局共享的一张大图。

## 游戏内设置

当前常用设置如下：

- `Enabled`
  - 是否启用 ex-srt
- `Trigger Enter Count`
  - 进入同一个区域多少次后才触发自动保存
  - 默认 `1`
- `Disable SRT Freeze On Auto-Save`
  - 默认开启
  - 开启后，ex-srt 触发的这次自动保存会尽量取消 SRT 的冻结和擦屏
- `Show Overlay In Level`
  - 是否在关卡内显示已标记区域覆盖层
- `Show Lookout Edit Overlay`
  - 是否在望远镜编辑时显示轻量提示
- `Show Popup On Auto Save`
  - 触发自动保存时是否显示提示
- `Toggle Level Overlay`
  - 游戏内切换关卡覆盖层显示的按键
- `Toggle Lookout Edit`
  - 在望远镜状态下开启或关闭关卡内涂画编辑
- `Clear Current Room Markers`
  - 清空当前房间标记的可配置按键

## 推荐使用流程

1. 先在地图编辑器里打开你要练习的房间
2. 用 `Alt + 左键` 画出想要自动存档的位置
3. 如果要擦除，使用 `Alt + 右键`
4. 进入游戏测试
5. 角色进入该区域时，观察是否出现 ex-srt 提示
6. 用 Speedrun Tool 检查 `Default Slot` 是否已经存到当前状态

## 重要说明

- ex-srt 当前使用的是 Speedrun Tool 的 `Default Slot`
- 如果 `Default Slot` 已经有存档，ex-srt 会跳过这次自动保存
- 这意味着你在重复测试时，可能需要先清空 `Default Slot`
- 如果房间出生点就在标记区域里，进房后可能会立刻触发一次自动保存

## 常见问题

### 进入区域后没有触发自动保存

先检查这些项：

- `Enabled` 是否开启
- 该房间是否真的画了区域
- `Trigger Enter Count` 是否大于 `1`
- `Default Slot` 是否已经有存档
- Speedrun Tool 是否正常加载

### 触发后感觉会卡一下

这是正常现象的一部分。  
ex-srt 触发时会调用 Speedrun Tool 的 `SaveState`，而 `SaveState` 本身会同步保存当前关卡状态，所以通常会有一个很短的卡顿。

### 为什么没有再次自动保存

如果 `Default Slot` 已经有存档，ex-srt 会跳过后续自动保存。  
这是当前设计，用来避免反复覆盖同一个自动存档。

### 游戏里看不到覆盖层

检查：

- `Show Overlay In Level` 是否开启
- 当前房间是否真的有标记
- mod 是否已经正确加载

## 当前限制

- 目前主要支持“地图编辑器里画区，游戏里触发保存”以及“望远镜内直接涂画当前房间”的工作流
- 自动保存目标槽位当前固定为 Speedrun Tool 的 `Default Slot`
