# ContentSystem 使用说明

内容系统的角色：接收 `ProximityTrigger` 的节点触发信号 → 加载文字/图片/音频 → 传递给 UI 显示。

## 文件一览

| 文件 | 作用 | 需要挂载到场景？ |
|---|---|---|
| `LanguageManager.cs` | 中/英文切换，静态全局访问 `LanguageManager.Current` | 否 |
| `TextLoader.cs` | 同步读取 `StreamingAssets/Content/Text/*.md` | 否 |
| `ImageLoader.cs` | 协程异步加载图片为 `Sprite` | 否 |
| `ContentManager.cs` | 核心协调器，订阅触发事件，加载内容，触发 UI 事件 | **是** |
| `ContentDisplay.cs` | UI 桥接，在 Inspector 拖拽绑定 UI 组件 | **是** |

## 整体流程

```
玩家进入节点
  → ProximityTrigger.OnNodeEnter（携带 NodeComponent，内含节点全部数据）
  → ContentManager 收到事件
      ├─ 同步读 .md 文件（TextLoader）
      ├─ 异步加载图片（ImageLoader，协程）
      └─ 异步加载音频（UnityWebRequestMultimedia，协程）
  → ContentManager.OnContentReady（携带 ContentData 包）
  → ContentDisplay 收到事件 → 更新 Canvas 上的 UI 组件
```

## 场景搭建步骤

> 以下操作在 Unity Editor 的 Hierarchy 窗口中完成。

### 1. 创建 Canvas

Hierarchy 右键 → **UI** → **Canvas**

自动生成 `Canvas` + `EventSystem` 两个 GameObject。Canvas 的 Render Mode 保持默认的 **Screen Space - Overlay**。

### 2. 创建内容面板

在 Canvas 上右键 → **UI** → **Panel**，命名为 `ContentPanel`。

- 在 Inspector 左上角**取消勾选**（初始隐藏，由代码控制显示/隐藏）
- 调整 Rect Transform 到你想要的位置和大小（例如右下角 600×400）

### 3. 在 ContentPanel 下添加子组件

| 右键菜单 | 命名 | 用途 |
|---|---|---|
| UI → Text - TextMeshPro | `TitleText` | 内容页标题 |
| UI → Text - TextMeshPro | `BodyText` | 正文（Inspector 中可调整字号、勾选多行） |
| UI → Image | `MainImage` | 当前显示的图片 |
| UI → Text - TextMeshPro | `ImageDescText` | 图片说明文字 |
| UI → Text - TextMeshPro | `PageIndicator` | 页码，如 "1 / 3" |
| UI → Button - TextMeshPro | `PrevImageButton` | 上一张图片 |
| UI → Button - TextMeshPro | `NextImageButton` | 下一张图片 |
| UI → Button - TextMeshPro | `PlayAudioButton` | 播放音频（可选） |
| UI → Button - TextMeshPro | `PauseAudioButton` | 暂停音频（可选） |

> 首次使用 TextMeshPro 时，Unity 会弹出 "Import TMP Essentials" 对话框，点击导入即可。

### 4. 挂载 ContentDisplay

选中 `Canvas`（或 `ContentPanel`），Inspector 底部 → **Add Component** → 搜索 `ContentDisplay` → 添加。

将 Hierarchy 中的各个 UI 子对象**拖入** ContentDisplay 对应的槽位：

- `Content Panel` ← 拖入 `ContentPanel` GameObject
- `Title Text` ← 拖入 `TitleText`
- `Body Text` ← 拖入 `BodyText`
- `Main Image` ← 拖入 `MainImage`
- `Image Desc Text` ← 拖入 `ImageDescText`
- `Page Indicator` ← 拖入 `PageIndicator`
- `Prev Image Button` ← 拖入 `PrevImageButton`
- `Next Image Button` ← 拖入 `NextImageButton`
- `Play Audio Button` ← 拖入 `PlayAudioButton`（可选）
- `Pause Audio Button` ← 拖入 `PauseAudioButton`（可选）

### 5. 挂载 ContentManager

Hierarchy 右键 → **Create Empty**，命名为 `ContentSystem`。

Inspector 底部 → **Add Component** → 搜索 `ContentManager` → 添加。

`Enable Debug Log` 勾选后可在 Console 看到加载日志，方便调试。

### 6. 创建测试用的文字文件

在 `Assets/StreamingAssets/Content/Text/` 下创建测试文件，例如：

`test.zh.md`：
```
这是测试节点的中文介绍内容。

可以包含多行文字，支持 Markdown 格式。
```

`test.en.md`：
```
This is the English description of the test node.

It can contain multiple lines.
```

然后将场景中测试节点的 `Text → ZH → File` 字段设为 `Content/Text/test.zh.md`，英文同理。

## 运行时行为

1. 玩家进入节点触发半径 → `ContentPanel` 自动显示，文字/图片自动加载
2. 点击左右箭头切换图片
3. 如果 `autoPlay = true`，音频自动播放
4. 玩家离开节点 → `ContentPanel` 自动隐藏，音频停止

## 语言切换

在代码中任意位置调用：

```csharp
LanguageManager.SetLanguage(Language.EN); // 切换为英文
LanguageManager.SetLanguage(Language.ZH); // 切换为中文
```

可通过 UI 按钮绑定 `LanguageManager.SetLanguage` 来实现用户手动切换。

## 内容文件路径约定

`NodeData` 中存储相对于 `StreamingAssets/` 的路径：

```
StreamingAssets/Content/
├── Text/        → Content/Text/eastGate.zh.md
├── Images/      → Content/Images/eastGate_01.png
├── Audio/       → Content/Audio/eastGate.zh.mp3
└── Thumbnails/  → Content/Thumbnails/eastGate.png
```

音频支持格式：`.mp3`、`.wav`、`.ogg`、`.aiff`。
