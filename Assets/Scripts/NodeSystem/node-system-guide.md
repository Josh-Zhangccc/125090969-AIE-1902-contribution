# NodeSystem 使用指南

## 概述

NodeSystem 是导览系统节点注册流程的 Unity 端实现。它将 [node-data-schema.md](node-data-schema.md) 中定义的 JSON 数据结构映射为可视化 Inspector 组件，允许在场景中直接拖拽定位节点、填写内容信息，并通过编辑器菜单一键导出为 `nodes.json`。

## 文件结构

```
Assets/Scripts/NodeSystem/
├── NodeData.cs                      — 纯数据容器（与 JSON schema 对应）
├── NodeComponent.cs                 — MonoBehaviour，挂载到场景 GameObject
├── NodeRegistry.cs                  — 单例，运行时收集并管理所有节点
├── NodeSystemDebugger.cs            — 运行时调试工具，通过 Log 输出节点信息
└── Editor/
    ├── NodeComponentEditor.cs       — NodeComponent 的自定义 Inspector
    └── NodeJsonExporter.cs          — 编辑器工具：验证 + 导出 nodes.json
```

## 各文件说明

### 1. NodeData.cs — 数据容器

**用途：** 定义所有可序列化的数据结构，与 `nodes.json` 字段一一对应。

**包含的结构体/类：**

| 类型 | 对应 JSON | 说明 |
|------|-----------|------|
| `LocalizedString` | `{ "zh": "...", "en": "..." }` | 双语字符串 |
| `LocalizedText` | `text.zh` / `text.en` 内部 | 单语言文本内容（标题 + .md 路径） |
| `NodeImage` | `images[]` 元素 | 图片路径 + 双语描述 |
| `LocalizedAudio` | `audio.zh` / `audio.en` 内部 | 音频路径 + 音量 + 自动播放 |
| `CameraOverrideData` | `cameraOverride` | 强制视角控制 |
| `ConnectedNode` | `connectedNodes.to[]` 元素 | 下游节点 ID + 是否必经 |
| `ConnectedNodesData` | `connectedNodes` | 连接信息容器 |
| `NodeData` | 整个节点对象 | 汇总以上所有字段 |

节点数据定义文件。所有结构体均标记 `[Serializable]`，可在 Inspector 中直接编辑。

**如何使用：**
- 不需要直接使用。`NodeComponent` 内部持有这些数据，并提供 `ToNodeData()` 方法导出纯数据。
- 运行时系统如需直接操作数据（而非依赖 GameObject），可从 `NodeRegistry.ExportAllNodeData()` 获取 `List<NodeData>`。
- 如果需要扩展节点字段：先在 [node-data-schema.md](node-data-schema.md) 中定义，再在此文件中添加对应字段。

---

### 2. NodeComponent.cs — 场景组件

**用途：** 挂载到场景 GameObject 上，将节点数据以 Inspector 形式展示和编辑。节点的世界坐标和朝向由所在 GameObject 的 Transform 决定。

**Inspector 分区：**

- **标识 / Identity** — id, displayName (zh/en), shortDescription (zh/en), campusLocation, isStartingNode, isVisible
- **Trigger** — radius（触发半径，米）
- **Position / Rotation** — 只读，来自 Transform
- **导航 / Navigation (connectedNodes.to[])** — 下游连接节点列表，每项含 id + required
- **Thumbnail** — 缩略图路径
- **文本 / Text** — text.zh / text.en，各含 title + file (.md 路径)
- **图片 / Images (images[])** — 图片列表，每项含 file + description (zh/en)
- **音频 / Audio** — audio.zh / audio.en，各含 file + volume + autoPlay
- **视角控制 / Camera Override (cameraOverride)** — enabled → target + blendTime

**场景可视化：**
- 绿色半透明球：可见节点的触发范围
- 蓝色半透明球：隐形节点的触发范围
- 选中时显示黄色连线到下游节点（实线 = required，虚线 = optional）

**如何使用：**

1. 在场景中创建空 GameObject，命名为节点 ID（如 `eastGate`）
2. 在 Inspector 中点击 `Add Component` → 搜索 `Node Component`
3. 在 Scene View 中将 GameObject 拖到目标位置
4. 填入 id、displayName、连接信息等
5. 点击 Inspector 底部的 `Validate` 按钮检查数据完整性

**重要：**
- `id` 必须在场景内唯一，否则 `NodeRegistry` 会拒绝注册并报错
- 位置和朝向由 Transform 控制，不需要手动填写坐标字段
- 隐形节点（`isVisible = false`）不会在 UI 中出现，但仍参与路径图

---

### 3. NodeRegistry.cs — 注册中心

**用途：** 场景中的单例管理器，运行时自动收集所有 `NodeComponent`，提供查询 API。

**核心 API：**

```csharp
// 获取注册中心实例
NodeRegistry.Instance

// 按 ID 获取组件
NodeRegistry.Instance.GetComponentForNode("eastGate")
// 返回 NodeComponent，可用于获取 position 等

// 按 ID 获取纯数据
NodeRegistry.Instance.GetNodeData("eastGate")
// 返回 NodeData

// 按位置查最近节点（用于触发系统检测玩家附近节点）
NodeRegistry.Instance.GetNearestNode(playerPosition)
// 返回最近的 NodeComponent

// 获取所有节点
NodeRegistry.Instance.AllNodes          // IEnumerable<NodeComponent>

// 获取可见节点（UI 用）
NodeRegistry.Instance.VisibleNodes      // 过滤 isVisible == true

// 获取起点节点
NodeRegistry.Instance.StartingNodes     // 过滤 isStartingNode == true

// 按园区筛选
NodeRegistry.Instance.GetNodesByCampus("upper")

// 获取某节点的上游连接（运行时从 to 反向推导）
NodeRegistry.Instance.GetFromNodes("eastGate")
// 返回 IReadOnlyList<string>

// 导出所有节点为纯数据列表
NodeRegistry.Instance.ExportAllNodeData()
// 返回 List<NodeData>
```

**初始化：**
- 挂载在任意 GameObject 上即可，建议放在场景根级别
- `[DefaultExecutionOrder(-100)]` 确保在所有 NodeComponent 之前 Awake
- 在 `Start()` 中自动推导所有 `from` 连接

**如何使用：**

1. 创建空 GameObject，命名为 `NodeRegistry`
2. Add Component → 搜索 `Node Registry`
3. 无需额外配置，启动时自动工作

**系统如何获取节点信息：**

```csharp
// 获取所有可见节点，生成 UI 列表
foreach (var node in NodeRegistry.Instance.VisibleNodes)
{
    Debug.Log($"{node.displayName.zh} - {node.shortDescription.zh}");
}

// 获取离玩家最近的节点
var nearest = NodeRegistry.Instance.GetNearestNode(player.transform.position);
if (nearest != null && Vector3.Distance(player.transform.position, nearest.Position) <= nearest.radius)
{
    // 玩家在 nearest 的触发范围内
}

// 获取某节点的完整数据用于内容展示
var data = NodeRegistry.Instance.GetNodeData("eastGate");
// data.zhText.file → "Content/Text/eastGate.zh.md"
// data.images[0].description.zh → "东门全景"
```

---

### 4. NodeComponentEditor.cs — 自定义 Inspector

**用途：** `NodeComponent` 的自定义编辑器界面，提供折叠分组、类型徽章和验证按钮。

仅在 Unity Editor 中运行，不会包含在构建中。

**特性：**
- 顶部显示节点类型徽章（POI / POI·起点 / 隐形·路径）
- 六个可折叠分区，避免 Inspector 过长
- Position/Rotation 字段置灰只读，提示由 Transform 控制
- 字段名与 JSON schema 对齐（如 `connectedNodes.to[].id`、`images[].file`）
- 数组元素折叠标签自动显示关键值（如 `to[0] → eastGate`、`images[0] → Content/Images/xxx.jpg`）
- `connectedTo` 的 `id` 字段旁自动显示场景中目标 GameObject 名称，方便核对
- Validate 按钮检查：id 是否为空、是否有重复 id、连接的节点是否在场景中存在

---

### 5. NodeJsonExporter.cs — JSON 导出工具

**用途：** 编辑器工具，从场景中收集所有 `NodeComponent`，验证数据完整性，导出为标准 `nodes.json`。

仅在 Unity Editor 中运行。

**使用方式：**
- 菜单栏 `Tools → Tour System → Export Nodes to JSON`
- 输出路径：`Assets/StreamingAssets/nodes.json`

**导出前验证：**
- id 为空 → 报错
- id 重复 → 报错
- 验证失败时会弹出对话框询问是否继续导出

**输出格式：** 与 `nodes.example.json` 和 [node-data-schema.md](node-data-schema.md) 完全一致。

---

### 6. NodeSystemDebugger.cs — 运行时调试工具

**用途：** 挂载到 NodeRegistry 所在 GameObject 上，Start 时通过 `Debug.Log` 输出所有已注册节点的 id 列表，供确认节点是否正确加载。

**使用方式：**
- 挂载到与 `NodeRegistry` 相同的 GameObject 上
- 运行场景，在 Console 中查看输出

**输出示例：**
```
[NodeSystemDebugger] 已注册节点 id 列表：
  [0] eastGate  (GameObject: eastGate)
  [1] adminBuilding  (GameObject: adminBuilding)
  [2] universityLibrary  (GameObject: universityLibrary)
  ...
```

---

## 完整工作流

### 创建新节点

```
1. Hierarchy 右键 → Create Empty，命名为节点 ID（如 adminBuilding）
2. Add Component → Node Component
3. 在 Scene View 中拖动到目标位置
4. Inspector 中填写：
   ├── id: "adminBuilding"
   ├── Display Name: zh / en
   ├── Short Description: zh / en
   ├── Campus Location: upper
   ├── Is Starting Node: 勾选与否
   ├── Is Visible: 勾选
   ├── Radius: 5
   ├── connectedNodes.to[]: 添加下游节点 — 每个元素填写 id 和 required
   ├── Thumbnail: Content/Thumbnails/adminBuilding.png
   ├── text.zh / text.en: title + file (.md 路径)
   ├── images[]: 每项填写 file（图片路径）+ description（zh / en）
   └── audio.zh / audio.en: file（音频路径）+ volume + autoPlay
5. 点击 Validate 按钮
6. 重复以上创建所有节点
7. Tools → Tour System → Export Nodes to JSON
```

### 创建隐形路径节点

```
1. 创建空 GameObject → Add Component → Node Component
2. 取消勾选 Is Visible
3. Radius 设为 3（较小范围）
4. connectedNodes.to[] 填入下游节点 id
5. audio.zh / audio.en 中设置 autoPlay = true
6. 如需强制视角，展开 Camera Override → enabled = true → 设置 target 和 blendTime
```

### 运行时查询节点信息

```csharp
// 按 ID 获取
var comp = NodeRegistry.Instance.GetComponentForNode("adminBuilding");
var data = comp.ToNodeData();

// 按位置获取最近
var nearest = NodeRegistry.Instance.GetNearestNode(transform.position);

// 获取起点列表（用于导览开始选择）
var startingNodes = NodeRegistry.Instance.StartingNodes;
```

### 触发系统如何对接

触发系统（陈负责检测和反馈）可通过 `NodeRegistry` 获取节点信息：

```csharp
// 玩家位置检测
var nearestNode = NodeRegistry.Instance.GetNearestNode(player.position);
if (nearestNode != null)
{
    float dist = Vector3.Distance(player.position, nearestNode.Position);
    if (dist <= nearestNode.radius)
    {
        // 触发进入事件 → 通知内容系统展示 nearestNode 的信息
    }
}
```

---

## 与其他系统的关系

```
                    ┌─────────────────┐
                    │  NodeSystem      │
                    │  (张 — 本模块)   │
                    │                 │
                    │  NodeComponent  │── 场景编辑
                    │  NodeRegistry   │── 运行时查询
                    │  JSON Exporter  │── 数据导出
                    └───────┬─────────┘
                            │
              ┌─────────────┼─────────────┐
              │             │             │
              ▼             ▼             ▼
     ┌──────────┐  ┌──────────────┐  ┌──────────┐
     │ 触发系统  │  │  内容系统     │  │  UI 系统 │
     │          │  │              │  │          │
     │ 距离检测  │  │ 文本/图片/音频 │  │ 导航菜单  │
     │ 信号触发  │  │ 展示与播放    │  │ 内容展示  │
     └──────────┘  └──────────────┘  └──────────┘
```

- **触发系统 → NodeSystem**：通过 `GetNearestNode()` 确定当前最近的节点，与 `radius` 比较判断是否触发
- **内容系统 → NodeSystem**：通过 `GetNodeData()` 获取节点的文本 .md 路径、图片路径、音频路径，进行展示
- **UI 系统 → NodeSystem**：通过 `VisibleNodes`、`StartingNodes` 获取列表数据，生成导航菜单
