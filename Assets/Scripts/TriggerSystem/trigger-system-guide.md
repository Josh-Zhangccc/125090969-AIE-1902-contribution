# TriggerSystem 使用指南

## 概述

TriggerSystem 是导览系统触发机制的 Unity 端实现。当玩家在场景中移动时，系统按固定间隔检测玩家与各节点的距离；进入/离开节点触发半径时发出事件，并在进入时将节点标记为已访问（无效化），避免重复触发。调览重置时清除所有失效标记。

## 文件结构

```
Assets/Scripts/TriggerSystem/
├── TriggerEvents.cs              — 事件参数定义（纯数据）
├── ProximityTrigger.cs           — 挂载到玩家，距离检测 + 事件发出
├── TourSessionManager.cs         — 单例，管理无效化状态 + 重置
├── TriggerSystemDebugger.cs      — 运行时调试，输出触发日志
├── PlayerMovement.cs             — 临时 WASD 移动脚本（测试用）
└── Editor/
    └── ProximityTriggerEditor.cs — ProximityTrigger 的自定义 Inspector
```

| 文件 | 对应 NodeSystem 中 | 职责 |
|------|-------------------|------|
| `TriggerEvents.cs` | `NodeData.cs` | 纯数据，事件参数类型 |
| `ProximityTrigger.cs` | `NodeComponent.cs` | MonoBehaviour，挂载到玩家 |
| `TourSessionManager.cs` | `NodeRegistry.cs` | 单例，运行时状态管理 |
| `TriggerSystemDebugger.cs` | `NodeSystemDebugger.cs` | 调试输出 |
| `Editor/ProximityTriggerEditor.cs` | `Editor/NodeComponentEditor.cs` | 自定义 Inspector |

---

## 各文件说明

### 1. TriggerEvents.cs — 事件参数

**用途：** 定义进入/离开节点时携带的事件参数。

```csharp
public class NodeTriggerEventArgs : EventArgs
{
    public string NodeId;              // 触发的节点 ID
    public NodeComponent NodeComponent; // 节点组件引用
}
```

- 同时携带 `NodeId`（日志/比对）和 `NodeComponent`（下游直接使用，无需再查 Registry）
- 继承 `EventArgs`，为未来扩展（如方向上下文）预留空间

---

### 2. ProximityTrigger.cs — 核心检测组件

**用途：** 挂载到玩家 GameObject，按间隔检测与最近有效节点的距离，在进入/离开时发出事件。

**挂载位置：** 玩家角色 GameObject（需要 Transform 代表玩家位置）。

**Inspector 字段：**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Check Interval` | float | 0.25 | 检测间隔（秒），值越小响应越快但开销越大 |
| `Enable Debug Log` | bool | true | 进入/离开时是否在 Console 输出日志 |

**运行时属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `CurrentNodeId` | string | 当前已触发节点的 ID，不在任何节点内时为 null |
| `ApproachedNodeComponent` | NodeComponent | 当前靠近但未触发的可见节点 |
| `HasApproachedNode` | bool | 是否有可见节点处于 approached 状态 |

**事件：**

| 事件 | 签名 | 说明 |
|------|------|------|
| `OnNodeEnter` | `EventHandler<NodeTriggerEventArgs>` | 进入节点并触发（不可见节点自动，可见节点按 F 后） |
| `OnNodeExit` | `EventHandler<NodeTriggerEventArgs>` | 离开已触发节点的半径 |
| `OnNodeApproached` | `EventHandler<NodeTriggerEventArgs>` | 进入可见节点半径但未触发（等待玩家按键） |
| `OnApproachCleared` | `Action` | 离开可见节点半径（未触发就走了） |
| `OnAllVisited` | `Action` | 所有可见节点均已触发 |

**核心逻辑（每 Check Interval 秒执行一次）：**

```
1. 跳过——如果 TourSessionManager 或 NodeRegistry 未就绪
2. 优先 sticky check：是否仍在已触发的 CurrentNodeId 半径内
3. 其次 sticky check：是否仍在 ApproachedNodeComponent（可见节点）半径内
4. 都不在则遍历 AllNodes，排除已失效节点，找距离最近的
5. 判断该最近节点的距离是否 ≤ 其 radius
6. 无状态变化则跳过
7. 有变化时：
   - 离开已触发节点 → 触发 OnNodeExit
   - 离开 approached 节点 → 触发 OnApproachCleared
   - 进入新节点，根据 isVisible 分流：
     - isVisible == true  → 设为 approached，触发 OnNodeApproached，不 Invalidate
     - isVisible == false → 自动 Invalidate + OnNodeEnter
```

**可见/不可见节点的不同处理：**

| 属性 | 可见节点 (isVisible=true) | 不可见节点 (isVisible=false) |
|------|--------------------------|------------------------------|
| 触发方式 | 靠近后按 F 键 | 靠近自动触发 |
| Invalidate | 按 F 时执行 | 进入范围时立即执行 |
| UI 面板 | 主内容面板（图片、正文、翻页） | 底部字幕面板（标题、正文、播放/静音） |
| 关闭方式 | ESC 手动关闭 | autoDismissDuration 秒后自动关闭，或 ESC 提前关闭 |

**关键行为规则：**
- 同时处于多个节点半径内 → 只取**最近的**一个
- 玩家停在节点内不动 → 只在进入时触发一次，不反复触发
- 可见节点：进入时仅 approach，按 F 才 Invalidate 和 OnNodeEnter
- 不可见节点：进入时自动 Invalidate，离开不恢复
- `TriggerApproachedNode()` — 手动触发当前 approached 的可见节点（由 ContentDisplay 的 F 键调用）

**辅助方法：**
- `ResetDetection()` — 清除 `CurrentNodeId`、`ApproachedNodeComponent` 和 `allVisitedFired`，配合 `TourSessionManager.Reset()` 使用
- `TriggerApproachedNode()` — 手动触发当前 approached 节点

**Gizmos（Scene View）：**
- 灰色虚线 → 到最近有效节点的连线（在范围外）
- 绿色虚线 → 到最近有效节点的连线（在范围内）

---

### 3. TourSessionManager.cs — 无效化状态管理

**用途：** 单例，管理本次导览中哪些节点已被触发（无效化），提供重置功能。

**执行顺序：** `[DefaultExecutionOrder(-200)]`，早于 NodeRegistry 的 -100，确保最先初始化。

**运行时属性：**

| 属性 | 类型 | 说明 |
|------|------|------|
| `Instance` | TourSessionManager (static) | 单例引用 |
| `InvalidatedCount` | int | 已失效节点数 |
| `TotalVisibleCount` | int | 可见节点总数 |

**核心方法：**

| 方法 | 参数 | 说明 |
|------|------|------|
| `Invalidate(string)` | nodeId | 标记节点为已触发，触发 `OnInvalidated` |
| `IsInvalidated(string)` | nodeId | 查询某节点是否已失效，返回 bool |
| `Reset()` | — | 清除所有失效标记，触发 `OnReset` |
| `GetUnvisitedVisibleNodes()` | — | 返回尚未访问的可见节点 ID 列表 |

**事件：**

| 事件 | 签名 | 说明 |
|------|------|------|
| `OnInvalidated` | `Action<string>` | 某节点被无效化时 |
| `OnReset` | `Action` | 导览重置时 |

**行为：**
- 不可见节点（路径节点）同样会被 Invalidated，防止同一路径段的旁白音频反复触发
- `Start()` 时自动调用 `Reset()`，确保每次场景启动时都是干净状态
- `Reset()` 可被外部调用（如 UI 的"重新开始导览"按钮）

**使用示例：**

```csharp
// UI 按钮点击 → 重新开始导览
public void OnRestartTourClicked()
{
    TourSessionManager.Instance.Reset();
    player.GetComponent<ProximityTrigger>().ResetDetection();
}
```

---

### 4. TriggerSystemDebugger.cs — 调试工具

**用途：** 挂载到 TourSessionManager 所在 GameObject，订阅并输出触发相关日志。

**Inspector 字段：**

| 字段 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `Show Overlay` | bool | false | 是否在 Game View 左上角显示状态浮层 |

**使用方式：**
- 挂载到与 `TourSessionManager` 同一个 GameObject 上（或任意 GameObject）
- 运行场景，Console 中可见：

```
[TriggerDebug] Invalidated: eastGate  (1/12 visible)
[TriggerDebug] Invalidated: adminBuilding  (2/12 visible)
...
[TriggerDebug] Tour reset — 0 nodes visited
```

- 勾选 `Show Overlay` 时，Game View 左上角显示 `Visited: 3 / 12`

---

### 5. Editor/ProximityTriggerEditor.cs — 自定义 Inspector

**用途：** `ProximityTrigger` 的自定义编辑器界面，提供分组折叠和运行时状态显示。

仅在 Unity Editor 中运行。

**Inspector 分区：**
- **检测 / Detection** — `Check Interval`
- **状态 / Status** — 运行时显示 `Current Node` 和 `Visited: x / y`（只读）
- **调试 / Debug** — `Enable Debug Log`

运行时自动刷新（`RequiresConstantRepaint`），无需手动点击刷新。

---

### 6. PlayerMovement.cs — 临时移动脚本（测试用）

**用途：** 临时 WASD + 鼠标旋转移动脚本，用于在触发系统开发阶段测试功能。

**操作方式：**
- `WASD` — 前后左右移动
- `鼠标右键 + 拖动` — 旋转视角

> 此脚本仅用于测试触发系统。后续由正式玩家控制器替代。

---

## 完整工作流

### 场景搭建

```
1. 创建 Player GameObject（右键 → 3D Object → Capsule，命名为 Player）
2. Player 上 Add Component → ProximityTrigger
3. Player 上 Add Component → PlayerMovement（测试用）
4. 创建空 GameObject，命名为 TourSession
5. TourSession 上 Add Component → TourSessionManager
6. TourSession 上 Add Component → TriggerSystemDebugger
7. 运行 → WASD 移动，靠近场景中任意 NodeComponent 所在的 GameObject
```

### 下游系统如何订阅触发事件

```csharp
// 内容系统 / UI 系统订阅
var trigger = player.GetComponent<ProximityTrigger>();

trigger.OnNodeApproached += (sender, args) =>
{
    // 玩家靠近可见节点，显示"按 F 查看"提示
    ShowPrompt("按 F 查看");
};

trigger.OnNodeEnter += (sender, args) =>
{
    var node = args.NodeComponent;
    // 加载并展示内容（不可见节点自动触发，可见节点由 F 键触发）
    LoadAndShowContent(node);
};

trigger.OnNodeExit += (sender, args) =>
{
    HideContentPanel();
};

trigger.OnApproachCleared += () =>
{
    // 玩家离开了可见节点，隐藏提示
    HidePrompt();
};

trigger.OnAllVisited += () =>
{
    ShowTourCompleteDialog();
};

// 玩家按 F 键时
void OnTriggerKeyPressed()
{
    if (trigger.HasApproachedNode)
        trigger.TriggerApproachedNode();
}
```

### 重新开始导览

```csharp
TourSessionManager.Instance.Reset();
// 同时重置 ProximityTrigger 的内部状态
player.GetComponent<ProximityTrigger>().ResetDetection();
```

---

## 系统间交互

```
玩家移动
    │
    ▼
ProximityTrigger.Check()   ← 每 0.25s
    │
    ├─ 遍历 NodeRegistry.Instance.AllNodes
    ├─ 过滤 TourSessionManager.Instance.IsInvalidated(id)
    ├─ 找最近的且在半径内的
    │
    ├─ 状态变化？
    │   ├─ 离开旧触发节点 → OnNodeExit(ID, Component)
    │   ├─ 离开 approached 节点 → OnApproachCleared()
    │   ├─ 进入可见节点 → OnNodeApproached(ID, Component)
    │   │   └─ ContentDisplay：显示"按 F 查看"提示
    │   │   └─ 玩家按 F → TriggerApproachedNode()
    │   │       └─ TourSessionManager.Invalidate(id) → OnNodeEnter
    │   │           ├─ 内容系统：加载文字/图片/音频
    │   │           ├─ UI 系统：展示主内容面板
    │   │           └─ Debugger：输出日志
    │   └─ 进入不可见节点 → OnNodeEnter(ID, Component)
    │       └─ TourSessionManager.Invalidate(id)
    │           ├─ 内容系统：加载文字/音频
    │           ├─ UI 系统：展示底部字幕面板 + 启动自动关闭计时器
    │           └─ Debugger：输出日志
    │
    └─ Gizmos 绘制调试连线
```

```
                    ┌─────────────────┐
                    │  NodeSystem      │
                    │                 │
                    │  NodeRegistry   │── 提供 AllNodes、GetNearestNode
                    │  NodeComponent  │── 提供 id、radius、isVisible 等
                    └───────┬─────────┘
                            │
                            ▼
                    ┌─────────────────┐
                    │  TriggerSystem   │
                    │                 │
                    │  ProximityTrigger│── 距离检测 + 事件发出
                    │  TourSessionMgr │── 无效化状态 + 重置
                    └───────┬─────────┘
                            │
              ┌─────────────┼─────────────┐
              │             │             │
              ▼             ▼             ▼
     ┌──────────┐  ┌──────────────┐  ┌──────────┐
     │  内容系统  │  │   UI 系统    │  │  音频系统  │
     │          │  │              │  │          │
     │ 文本/图片 │  │ F键提示      │  │ 旁白播放  │
     │ 加载展示  │  │ 主面板/字幕   │  │ 播放/静音 │
     │          │  │ 自动关闭计时  │  │          │
     └──────────┘  └──────────────┘  └──────────┘
```

---

## 与 NodeSystem 的关系

- **NodeRegistry.Instance.AllNodes** → 提供所有节点列表
- **NodeRegistry.Instance.VisibleNodes** → 用于计算 `TotalVisibleCount` 和 `AllVisibleNodesVisited`
- **NodeRegistry.Instance.GetComponentForNode(id)** → OnNodeExit 时获取退出节点信息
- **NodeComponent.Position** + **NodeComponent.radius** → 距离判断依据
- **NodeComponent.isVisible** → 区分可见节点（主/附属节点，按 F 触发）和不可见节点（路径节点，自动触发）
- **NodeComponent.autoDismissDuration** → 不可见节点 UI 自动消失秒数，0 表示手动关闭
