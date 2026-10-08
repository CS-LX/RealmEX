# RealmEX

面向 Survivalcraft 2 的独立沙箱与 Ponder 教程框架。教程使用自己的 Project、地形、库存和播放时钟，不接管玩家世界的 `GameManager.Project`。

## 教程入口

在游戏的暂停菜单点击 **思索教程**，直接打开教程目录。支持分类、搜索、方块关联、暂停、步骤跳转、回退重播、旋转和缩放。目录和播放界面归打开它的玩家所有；分屏玩家互不覆盖。

内置与门、非门和南瓜教程。内容模组可以提供自己的 `.ponder.xml`、`.pjs`、结构快照和机器 UI；无需引用 RealmEX DLL，也无需将 RealmEX 列为强制依赖。安装工业时代 2 后，目录中会出现它的核电教程。

F6–F10 阶段诊断已移除。历史 M1/M2/M3 的诊断入口和专用 Terrain 模板不再编译或打包；自动回归统一由测试工程与独立引擎工具承担。原有 Core 沙箱、场景和 Storyboard API 保留。

## 构建与验证

版本以 `modinfo.json` 为准；目标框架 `net10.0`，宿主包 `SurvivalcraftAPI.Survivalcraft` 1.9.3.1。

```powershell
dotnet test Tests/RealmEX.Tests.csproj --configuration Test
./tools/preview/run.ps1 -GameDir '<宿主目录>'
dotnet build RealmEX.csproj -c Release -p:RealmEXSkipPack=true
```

`Test` 不打包、不部署、不同步版本。配置被忽略的 `tools/pack.config.json` 后，普通 Debug/Release 构建会部署到本机 Mods 目录。打包只收入当前资源，避免增量构建残留的旧诊断模板重新进入发布包。

实际引擎工具覆盖时间轴、UI、地形与实体隔离；工业仓库的 `tools/PonderPreview` 进一步覆盖实际工业资源、菜单绑定、两位玩家的弹窗归属、教程切换与退出清理。它们不打开玩家存档，不能替代玩家世界中的长时间运行验收。

## 文档

- [内容包接入](docs/Ponder内容包.md)：无程序集依赖的 XML 清单与 JavaScript 教程。
- [机器界面演示](docs/Ponder机器界面演示.md)：独立库存、模拟点击/拖动与回放。
- [Ponder 对齐与接入](docs/Ponder对齐与接入.md)：时间轴、场景 API 与宿主适配。
- [目标架构](docs/architecture/目标架构.md)：沙箱边界和生命周期。
- [文档索引](docs/README.md) · [发版与 CI](docs/RELEASE.md)。

源码：[github.com/CS-LX/RealmEX](https://github.com/CS-LX/RealmEX)。许可：GPL-3.0，见 [LICENSE](LICENSE)。
