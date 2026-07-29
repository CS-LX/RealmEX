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

P1 已进入实现：`RealmBootstrap` 可从专用 xdb 模板创建内存态 `SandboxProject`，`RealmHost` 负责创建、查重、销毁和主世界退出清理；`SandboxSubsystemPlayers` 允许沙箱在无玩家时运行，且不会切换主世界界面。P1 沙箱不接管 `GameManager.Project`，也不允许进入宿主存档流程。

并行 tick、场景、离屏渲染和持久化仍分别属于 P2–P4；当前模板只包含 P1 启动所需的 Players、Time 与 Update 子系统。核心 Project 隔离已通过游戏内 F8 运行时验证，含 Terrain / 设备注册的完整 M1 尚未完成。

## 文档

| 文档 | 说明 |
| --- | --- |
| [docs/README.md](docs/README.md) | 文档索引 |
| [docs/architecture/目标架构.md](docs/architecture/目标架构.md) | **实施导航**（核心类型、阶段、里程碑） |
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

## 内容模组接入（规划）

内容模组在 `modinfo.json` 声明 `"com.realmex": "x.x.x"`，通过 `IRealmProjectTemplateContributor`（待实现）扩展 `SandboxProjectTemplate`，注册自有 Subsystem / Component。**RealmEX 核心不包含任何内容模组专有语义。**

## 仓库

- 源码：[github.com/CS-LX/RealmEX](https://github.com/CS-LX/RealmEX)
- 许可：GPL-3.0（见 [LICENSE](LICENSE)）
