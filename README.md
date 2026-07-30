# RealmEX

面向 Survivalcraft 2 模组开发的**并行仿真沙箱 Realm** 框架。

每个沙箱 = 独立 `SandboxProject` 实例 + 可配置 `RealmProfile` + 可选 `Realms/<id>/` 持久化；主世界继续独占 `GameManager.m_project`，`RealmHost` 在同一进程内并行 tick 与离屏渲染。

## 版本与依赖

| 项 | 值 |
| --- | --- |
| 包名 | `com.realmex` |
| 当前版本 | `1.0.0.0-preview1`（见 `modinfo.json`） |
| 目标框架 | `net10.0` |
| 宿主 API | `SurvivalcraftAPI.Survivalcraft` 1.9.2 |
| 模组依赖 | 无（仅宿主） |

## 当前进度

P1 / P2 / P3a 核心已完成：`SandboxProject`、`RealmHost.TickParallel`、`SandboxRealm`、`RealmScene` 与 `RealmStoryboard` 已通过 F8 / F9 / F10 游戏内诊断。P3b 已完成模块边界归类，核心运行时位于 `Core`，诊断位于 `Diagnostics`，并预留 `Presets.Ponder` / `Presets.Create`。P3c 已落地 `RealmViewport` 离屏渲染核心与基础 `RealmPonderWidget`；M1T 已通过 F7 BlockEntity 注册与 Terrain cell 变更隔离游戏内诊断；P3d-async 已通过 F10 async Storyboard 诊断；P3d 已新增 F6 非门 Ponder 示例教程诊断。下一步为 **P3e 可见 Ponder Dialog**（Core soft freeze：不大改 Core，改动主落 `Presets.Ponder`）；**P4 可选持久化已延后**；内容模组扩展入口仍在后续阶段。详情见 `docs/实施计划.md`。

## 文档

| 文档 | 说明 |
| --- | --- |
| [docs/README.md](docs/README.md) | 文档索引 |
| [docs/architecture/目标架构.md](docs/architecture/目标架构.md) | **目标架构**（核心类型、边界、不变量） |
| [docs/实施计划.md](docs/实施计划.md) | 阶段计划、Ponder 预设、Create 预设与近期顺序 |
| [docs/architecture/宿主耦合事实.md](docs/architecture/宿主耦合事实.md) | 宿主 `Project` / Subsystem 耦合摘录 |
| [docs/architecture/嵌套存档模式事实.md](docs/architecture/嵌套存档模式事实.md) | 子目录持久化对照（非换档运行时） |
| [docs/RELEASE.md](docs/RELEASE.md) | 发版与 CI |

## 构建

```powershell
cd Dependencies/RealmEX
dotnet build RealmEX.csproj -c Release -p:RealmEXSkipPack=true
```

本地部署：复制 `tools/pack.config.example.json` → `tools/pack.config.json`，填写 `ModsFolder` 后 `dotnet build`。

## P1 核心 M1 测试入口

1. 构建并部署 RealmEX，启动游戏后进入任意主世界。
2. 日志出现 `[RealmEX/M1] READY` 后按一次 **F8**。
3. 等待数秒后正常退出世界，不要强制结束游戏。
4. 检查 Mods 目录上一级的 `Game.log`，搜索 `[RealmEX/M1]`。

核心检查全部通过时会输出：

```text
[RealmEX/M1] RESULT=PASS scope=P1Core deviceTerrain=NOT_TESTED
```

`deviceTerrain=NOT_TESTED` 表示本入口只验证独立 Project、Players / Time / Update、Save 护栏和 Host 生命周期；含 Terrain / BlockEntity 的设备级 M1 仍需后续入口。

## M1T Terrain / BlockEntity 隔离入口

1. 构建并部署 RealmEX，启动游戏后进入任意主世界。
2. 日志出现 `[RealmEX/M1T] READY` 后按一次 **F7**。
3. 等待数秒后正常退出世界。
4. 检查 Mods 目录上一级的 `Game.log`，若不存在则查 `Bugs/Game.log`，搜索 `[RealmEX/M1T]`。

通过时会输出：

```text
[RealmEX/M1T] RESULT=PASS scope=TerrainIsolation validation=BlockEntityRegistry terrainMode=diagnostic-stub
```

M1T-3 起通过时会输出：

```text
[RealmEX/M1T] RESULT=PASS scope=TerrainIsolation validation=BlockEntityRegistry,TerrainCellChange terrainMode=diagnostic-stub
```

该入口使用独立 `RealmEXSandboxTerrainProject` 模板与诊断专用 stub Terrain，验收原版 `Chest` 的 `ComponentBlockEntity` 是否注册到沙箱自己的 `SubsystemBlockEntities`，并通过 `ChangeCell` 验证沙箱 cell 变更不会影响主世界同坐标 cell / BlockEntity 注册表。

## P2 并行 Tick 测试入口

1. 构建并部署 RealmEX，启动游戏后进入任意主世界。
2. 日志出现 `[RealmEX/M2] READY` 后按一次 **F9**。
3. 等待约 2 秒后正常退出世界。
4. 检查 Mods 目录上一级的 `Game.log`，若不存在则查 `Bugs/Game.log`，搜索 `[RealmEX/M2]`。

通过时会输出：

```text
[RealmEX/M2] RESULT=PASS scope=P2Tick
```

该入口会创建一个短生命周期内存沙箱，并由 `RealmHost.TickParallel` 在主世界帧更新后推进数帧，验证主世界时间与沙箱时间都连续前进。该检查已通过游戏内验证。

## P3 场景 / Storyboard 测试入口

1. 构建并部署 RealmEX，启动游戏后进入任意主世界。
2. 日志出现 `[RealmEX/M3] READY` 后按一次 **F10**。
3. 等待约 5 秒后正常退出世界。
4. 检查 Mods 目录上一级的 `Game.log`，若不存在则查 `Bugs/Game.log`，搜索 `[RealmEX/M3]`。

通过时会输出：

```text
[RealmEX/M3] RESULT=PASS scope=P3Scene
```

该入口会创建一个短生命周期 `SandboxRealm`，通过 `RealmStoryboard` 依次应用 `m3-slow` 与 `m3-fast` 两个 `RealmScene`，验证换场景会改变沙箱时间倍率而不需要改 Host / Bootstrap。P3c 起，该入口也会做一次 256×256 离屏渲染，并检查 `viewport-texture-created` 与 `viewport-rendered`。P3d-async 起，M3 诊断使用 `await ctx.ApplyScene(...)` / `await ctx.WaitFrames(...)` 脚本语法驱动同一流程。

## P3d Ponder 示例教程入口

1. 构建并部署 RealmEX，启动游戏后进入任意主世界。
2. 日志出现 `[RealmEX/Ponder] READY` 后按一次 **F6**。
3. 等待约 2 秒后正常退出世界。
4. 检查 Mods 目录上一级的 `Game.log`，若不存在则查 `Bugs/Game.log`，搜索 `[RealmEX/Ponder]`。

通过时会输出：

```text
[RealmEX/Ponder] RESULT=PASS tutorial=not-gate
```

该入口运行内置非门真值表示例，验证 Ponder 教程数据、async Storyboard、说明步骤日志和 Realm 视口闭环。它不代表完整电路系统已接入。

## 内容模组接入（规划）

内容模组在 `modinfo.json` 声明 `"com.realmex": "x.x.x"`，通过 `IRealmProjectTemplateContributor`（待实现）扩展 `SandboxProjectTemplate`，注册自有 Subsystem / Component。**RealmEX 核心不包含任何内容模组专有语义。**

## 仓库

- 源码：[github.com/CS-LX/RealmEX](https://github.com/CS-LX/RealmEX)
- 许可：GPL-3.0（见 [LICENSE](LICENSE)）
