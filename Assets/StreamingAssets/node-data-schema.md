# Nodes.json 数据结构说明

## 概述

`nodes.json` 是导览系统的核心数据文件，定义所有节点——包括可交互的 POI、附属建筑、以及用于路径约束和沿路触发的隐形节点。文件位于 `Assets/StreamingAssets/`，运行时直接读取。

---

## 一、节点分类与命名规范

### 1. POI 节点（主节点）

用户直接导航到达、有完整内容展示的主要兴趣点。`isVisible: true`，通常 `isStartingNode` 可为 `true`。

**命名格式：** `{英文名}[{功能后缀}]`(驼峰命名法)

```
eastGate               — 校门
adminBuilding           — 建筑/楼宇
universityLibrary       — 图书馆
shawCollege             — 书院

```

- 小驼峰命名，首字母小写，后续单词首字母大写，无分隔符
- 以建筑/场所的核心英文名为基础，避免拼音（除非无对应英文词，如 `daoyuan`）
- 功能后缀直接拼接：`Gate`（门）、`Building`（楼）、`Library`（图书馆）、`College`（书院）、`Hall`（馆/堂）、`Centre`（中心）

### 2. 附属节点（可见子节点）

POI 节点下属的具体建筑或设施，同样 `isVisible: true`，在 UI 中可展示但层级低于主节点。

**命名格式：** `{父节点ID}_{子设施名}`

```
minervaCollege_canteen          — 厚含书院食堂
minervaCollege_dormA            — 厚含书院宿舍A栋
shawCollege_lectureHall         — 逸夫书院报告厅
universityLibrary_readingRoom   — 大学图书馆阅览室
```

- 必须以其父节点的完整 ID 为前缀，后接下划线 `_` 和子设施名（同样使用驼峰）

### 3. 路径节点（不可见）

用于路径生成约束、沿路自动播放音频、触发强制视角转换。`isVisible: false`。

**命名格式：** `path_{前一节点}_{后一节点}_{序号}`

```
path_eastGate_adminBuilding_01       — 东门通往行政楼第1段
```

- 前缀固定为 `path`
- 第二段为前一主节点
- 第三段为后一主节点，同第二段，不考虑路径中的子节点
- 末尾为两位数字序号，同段路径可有多段

---

## 二、字段声明

### 顶层结构

```json
{
  "nodes": [ ... ]   // 必填。节点对象数组，顺序无关
}
```

### Node 对象

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| `id` | string | 是 | — | 唯一标识，小驼峰命名。见[命名规范](#一节点分类与命名规范) |
| `displayName` | {zh, en} | 是 | — | UI 展示名称 |
| `shortDescription` | {zh, en} | 是 | — | 一句话简介，用于导航菜单或路线选择卡片 |
| `campusLocation` | string | 是 | — | `"upper"` 或 `"lower"` |
| `isStartingNode` | bool | 是 | `false` | 是否为导览推荐起点。目前仅 `eastGate`（下园）和 `xiangboGate`（上园）为 `true` |
| `isVisible` | bool | 是 | `true` | `false` 时为隐形节点，不出现在 UI 和地图中 |
| `position` | {x,y,z} | 是 | — | 世界坐标（float） |
| `rotation` | {x,y,z} | 是 | — | 欧拉角（float）。可见节点为默认朝向，隐形节点可配合 cameraOverride 使用 |
| `connectedNodes` | object | 是 | — | 有向连接信息 |
| `connectedNodes.to` | array | 是 | `[]` | 本节点可前往的下游节点列表 |
| `connectedNodes.to[].id` | string | 是 | — | 目标节点 ID |
| `connectedNodes.to[].required` | bool | 是 | — | `true` 为主线路径，`false` 为可选分支（UI 应展示但不强制） |
| `radius` | float | 是 | `5.0` | 触发半径（米），玩家进入此范围时触发事件 |
| `thumbnail` | string | 可见节点必填 | `""` | 缩略图路径，用于导航 UI 列表。隐形节点留空 |
| `text` | object | 可见节点必填 | — | 文字内容 |
| `text.{lang}.title` | string | 是 | — | 内容页标题，`lang` 为 `zh` 或 `en` |
| `text.{lang}.file` | string | 是 | — | 指向 `Content/Text/` 下的 Markdown 文件路径。隐形节点留空 |
| `images` | array | 否 | `[]` | 图片列表 |
| `images[].file` | string | 是 | — | 图片文件路径。图片本身语言无关，同一文件服务于所有语言 |
| `images[].description` | {zh, en} | 是 | — | 图片说明文字（双语），用作 alt text 或图片标题 |
| `audio` | object | 否 | — | 音频内容 |
| `audio.{lang}.file` | string | 是 | — | 指向 `Content/Audio/` 下的音频文件路径。无音频时留空 |
| `audio.{lang}.volume` | float | 是 | `1.0` | 音量，范围 0.0 ~ 1.0 |
| `audio.{lang}.autoPlay` | bool | 是 | `false` | 是否到达即自动播放。普通节点通常 `false`（由用户手动触发），路径节点通常 `true` |
| `cameraOverride` | object | 否 | — | 强制视角控制。整个字段可省略或 `enabled: false` |
| `cameraOverride.enabled` | bool | 是 | `false` | 是否启用强制视角 |
| `cameraOverride.target` | {x,y,z} | 是 | — | 相机看向的目标世界坐标 |
| `cameraOverride.blendTime` | float | 是 | `1.0` | 视角过渡时间（秒） |

---

## 三、不可见节点（隐形节点）约定

当一个节点的 `isVisible` 为 `false` 时，适用以下规则：

1. **`displayName` 和 `shortDescription`** — 必须为空字符串 `""` 或其对应语言字段
2. **`thumbnail`** — 必须为空字符串 `""`
3. **`text`** — `title` 和 `file` 均留空
4. **`images`** — 空数组 `[]`
5. **`audio.autoPlay`** — 通常为 `true`，玩家经过即自动播放
6. **`radius`** — 建议 `3.0`，比可见节点小，避免远距离误触发
7. **`cameraOverride`** — 如有需要，设定 `enabled: true` 并指定目标点和过渡时间

UI 层逻辑：**`isVisible == false` 的节点不出现在导航列表、地图标记和搜索结果中**。`from` 数组在运行时由加载器从所有节点的 `to` 反向推导生成，JSON 中不存储。

---

## 四、双语字段约定

以下字段需同时提供中文（`zh`）和英文（`en`）：

- `displayName`
- `shortDescription`
- `text.{lang}.title`
- `audio.{lang}.{file, volume, autoPlay}`
- `images[].description`

文件路径中的语言标记采用文件名后缀方式：

```
Content/Text/eastGate.zh.md       — 中文介绍
Content/Text/eastGate.en.md       — 英文介绍
Content/Audio/eastGate.zh.mp3     — 中文旁白
Content/Audio/eastGate.en.mp3     — 英文旁白
```

图片文件本身不含语言标记，描述文字在 JSON 内以 `zh`/`en` 区分。
