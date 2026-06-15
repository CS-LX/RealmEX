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

## 内容模组接入（规划）

内容模组在 `modinfo.json` 声明 `"com.realmex": "x.x.x"`，通过 `IRealmProjectTemplateContributor`（待实现）扩展 `SandboxProjectTemplate`，注册自有 Subsystem / Component。**RealmEX 核心不包含任何内容模组专有语义。**

## 仓库

- 源码：[github.com/CS-LX/RealmEX](https://github.com/CS-LX/RealmEX)
- 许可：GPL-3.0（见 [LICENSE](LICENSE)）
