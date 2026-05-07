# AIE 1902 — Individual Contribution

**Zhang Zhuoxiao** · 125090969 · Group 1

This repository contains my individual contributions to the AIE 1902 course project at CUHKSZ — a Unity-based interactive campus tour system modeled after Assassin's Creed Origins' Discovery Tour, plus performance optimizations to the main project.

---

## Repository Structure

```
.
├── Assets/
│   └── Scripts/                  # Tour system source code (22 .cs files)
│       ├── NodeSystem/           #   Data definitions, registry, Editor tools
│       ├── TriggerSystem/        #   Proximity detection, state machine, session management
│       └── ContentSystem/        #   Event-driven content loading, UI bridge, camera control
├── OtherFiles/
│   ├── Scripts/                  # Main project optimizations (7 .cs files)
│   └── Structure Changes/        #   Config adjustments
├── updateFiles/                  # Iteration changelogs
└── 导览系统分工图.pdf             # Team division diagram (authoritative)
```

---

## Tour System Architecture

The entire backend was built independently across three subsystems:

| Subsystem | Files | Lines | Description |
|-----------|-------|-------|-------------|
| **NodeSystem** | 8 | 1,358 | Data structures (`NodeData`, `LocalizedString`, `CameraOverrideData`), `NodeComponent` for scene placement, `NodeRegistry` singleton, custom Inspector, JSON import/export with conflict detection, batch validator |
| **TriggerSystem** | 6 | 533 | `ProximityTrigger` with approach/trigger separation state machine (5 transitions), `TourSessionManager` for one-shot node invalidation, player movement controller |
| **ContentSystem** | 8 | 1,380 | Event-driven `ContentManager` (sync text + async image + async audio loading), `ContentDisplay` UI bridge with dual-panel routing and CJK fallback fonts, `CameraController` with Slerp rotation, `MarkdownParser` (compiled regex), `LanguageManager` (ZH/EN bilingual switching) |
| **Total** | **22** | **3,271** | 16 runtime scripts + 6 Editor tool scripts |

### Key Design Decisions

- **Approach/trigger separation** — visible nodes require player input (F key); invisible nodes auto-trigger for ambient narration
- **Inspector slot binding** — UI components are dragged into slots in the Inspector; code drives all interaction logic, fully decoupling art from programming
- **Event-driven pipeline** — `ProximityTrigger` → `ContentManager` → `ContentDisplay` / `CameraController`
- **Editor toolchain** — scene is the source of truth; JSON export with auto-naming, import with 3-strategy conflict resolution, batch validation

---

## Main Project Optimizations

7 `.cs` files, 1,181 lines refactored across the main CUHKSZ project:

| File | Lines | Optimization |
|------|-------|-------------|
| `MAPSYSTEM.cs` | 401 | `Camera.main` → cached private field |
| `scenetransfer.cs` | 355 | `GameObject.Find` → cached references |
| `camera.cs` | 234 | `GameObject.Find` → cached references |
| `FPSAdaptiveResolution.cs` | 103 | Dynamic resolution scaling based on real-time FPS |
| `Telsys.cs` | 45 | `GameObject.Find` → cached references |
| `FPSCounter.cs` | 33 | FPS telemetry utility |
| `GameController.cs` | 10 | Minor refactoring |

Additional: LOD Group configuration for high-poly models, camera clipping parameter tuning.

---

## Key Metrics

| Metric | Value |
|--------|-------|
| Total `.cs` files | **29** |
| Total lines of C# | **4,452** |
| Tour System files | 22 (3,271 lines) |
| Main project files | 7 (1,181 lines) |
| Campus locations covered | 11 (bilingual ZH/EN) |
| Major iterations | 3 |
| Runtime bugs fixed | 2 |

---

## AI-Assisted Development

DeepSeek V4 Pro (via Claude Code + OpenClaw) was used for code review, architectural discussion, and text polishing.

Based on actual usage data (May 1–7, 2026):

| Period | Tokens | Cost (CNY) | Avg/Day |
|--------|--------|------------|---------|
| 7-day sample | 36.13 M | ¥8.08 | ¥1.15 |
| Semester projection (70 days) | ~361 M | ~¥80.75 | — |

---

## Iteration History

- [update2026_05_03.md](updateFiles/update2026_05_03.md) — UI trigger interaction rewrite (approach/trigger separation)
- [update_2026_05_05.md](updateFiles/update_2026_05_05.md) — Camera control + 2 bug fixes
- [update_2026_05_06.md](updateFiles/update_2026_05_06.md) — JSON import/export improvements

---

## Team

See `导览系统分工图.pdf` for the authoritative division of work.

- **Zhang Zhuoxiao** — backend architecture, all code, UI binding logic, bilingual content
- **Wang Yixiang** — UI animations & keyframes, lower-campus nodes, node deployment
- **Wang Jingcheng** — node image content
- **Wang Jiahao** — integration debugging