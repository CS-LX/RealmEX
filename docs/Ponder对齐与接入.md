# Ponder 对齐与接入

本轮以 [Creators-of-Create/Ponder](https://github.com/Creators-of-Create/Ponder) 的 `mc1.21.1/dev` 分支、提交 [`89819291b726708d119b118dea01667eae0b64c9`](https://github.com/Creators-of-Create/Ponder/tree/89819291b726708d119b118dea01667eae0b64c9) 为对照。日期：2026-10-08。对照的是教程编排、播放、展示和扩展机制；SC 的字体、方块模型和输入方式沿用宿主。

## 原来的主要差距

原实现用 `sceneName` 分支清空并重建布局，步骤数由调用方填写；文字永久占据侧栏，进度是等宽编号按钮；跳转靠跳过异步等待，区域揭示动画却读取系统帧时间。没有统一教程索引、按物品查找、实体回退或可复用的区域指令。因此仅修改颜色和排版不能解决与 Ponder 的差距。

## 对齐结果

| 上游能力 / 参考代码 | RealmEX 对应实现 |
| --- | --- |
| `SceneBuilder`、指令队列、`idle` | `RealmPonderSceneBuilder` 编译指令时间轴，20 Hz；指令在游标处并行启动，只有 `Idle` 推进游标 |
| 预计算时长、普通 / lazy keyframe | 自动计算总时长与关键帧；`Keyframe(title, lazy: true)` 合并间隔不足一秒的步骤 |
| 回放、前进、回退、暂停 | `RealmPonderPlayer`；回退重建状态，`RealmPonderSession` 同时重建真实沙箱，再逐 tick 重放 |
| 阅读减速、倍速、提前完成、下一教程提示 | `ComfyReading` 仅在文字出现时放慢三倍；`Speed` 0.25–4；`MarkAsFinished`、`NextUpEnabled` |
| `Selection`、`SelectionUtil` | 不可变点集、含端点长方体、并集、差集、过滤、楼层范围、中心；`Schematic.Selection` 表示全部布局 |
| schematics / 初始世界快照 | 版本化 XML、由内容模组解析的方块调色板；`RestoreBlocks` 恢复初始值；不存在示例名称判断 |
| 显隐、独立区域、合并、移动、旋转 | 命名 section、方向偏移与淡入淡出、独立位置/旋转通道、旋转中心、合并；渲染使用独立真实 Terrain 网格 |
| 镜头缩放、旋转、底板、阴影 | `ConfigureCamera` / `RotateCamera`、底板 section、`RemoveShadow`；拖动水平和垂直观察、滚轮缩放、归位 |
| 文本气泡、颜色、指向、独立位置 | 有限时长文字、定位线、颜色、靠近目标或四角定位；长文滚动，正常字号 |
| 操作与物品提示 | `Controls` 支持交互/使用/滚动/移动、潜行组合、物品名称与实际物品图标 |
| 选择高亮、线、追踪包围盒 | `Outline`、`Line`、`ChaseBounds`；同名提示替换；`RemoveOverlay` 连同动画一起取消 |
| 成功提示、粒子 | `Success` / `Particles`；粒子轨迹由教程时间计算，暂停和回退一致 |
| 掉落物与特殊展示实体 | `CreateItem` / `CreateModel`，位置、缩放、旋转、移除；使用宿主物品与模型渲染 |
| 世界 tick、实体 / 方块实体修改 | `World` 回调提供隔离的 `RealmPonderWorld`；实体命名引用、创建/删除、组件访问、`ModifyBlockEntity<T>`；真实 `SubsystemUpdate` 每教程 tick 更新一次 |
| 注册插件、多物品对应多教程、分类、共享文本 | `IRealmPonderPlugin` / `RealmPonderRegistry`；命名空间、顺序、重复检测、标签、物品反查、共享文字；失败注册回滚 |
| 教程索引和排除项 | 搜索、标签筛选、带物品图标的目录；隐藏条目不进总目录，但仍能从相关物品打开 |
| `PonderUI` / `PonderProgressBar` | 全场景视口、场景内限时说明、矢量按钮、悬停说明、按实际时间分布的细进度条；步骤跳转和教程切换各有按钮 |
| identify mode | 查看模式暂停，按独立区域变换反向拾取方块，显示宿主名称，点击进入相关教程 |
| 透明 / 镂空材质 | 分开处理宿主 opaque/alpha-tested 与 transparent 子集；流体 alpha 按宿主顶面/侧面编码解释 |

`Core/` 的 Project、Host、通用 async Storyboard、生命周期和渲染接口未改动。Ponder 拥有自己的确定步长时钟和会话；其 Realm 关闭 `ParallelTick`，避免主 Host 再推进一遍。

## 宿主适配边界

- Java、Forge/NeoForge/Fabric、Mixin、NBT、Minecraft 注册表、原版红石、parrot / minecart 的具体逻辑与素材没有搬到 SC。对应需求分别使用 XML + C#、SC 模板/组件/子系统、物品/模型 actor。工业电网仍由工业自己的子系统提供。
- 内置与门/非门用真实方块表达真值表，属于教学编排；它们没有声称正在运行完整 SC 电路仿真。要演示实际设备过程，应提供专用 Project 模板并注册所需子系统。
- 默认模板包括 Terrain、BlockEntities、Time、Update、Drawing 和必要的安全占位子系统；不会自动装载整个主世界的天空、天气、音频、AI 和设备系统。自定义模板的时间子系统必须使用 `PonderSubsystemTime`。
- 自定义世界回调必须只读写参数中的沙箱，不能捕获主世界实体；不能依赖外部墙钟或未固定种子的随机数来保证重放一致。脚本时间轴与内置动画确定性不等于任意第三方物理系统都能逐位复现。
- 上游开发者专用的 Minecraft schematic 编辑界面、语言导出工具、启动器构建任务没有复制。这里提供可复跑的宿主渲染与交互验收工具。
- 本轮不是 Minecraft 界面的逐像素复刻。多重透明物体相互穿插时仍受常规透明排序限制；预览验证也不等于已验证所有第三方模组组合。

## 编写教程

布局资产示例（放在模组 Assets 下，并在自己的项目文件声明 `CopyToOutputDirectory`）：

```xml
<PonderSchematic Version="1">
  <Fill From="6,1,6" To="10,1,10" Block="base" />
  <Fill From="8,2,8" Block="machine" />
</PonderSchematic>
```

方块名由调用方解析，避免把其他模组动态分配的数值 ID 写进资产：

```csharp
var schematic = RealmPonderSchematic.Load(
    ContentManager.Get<XElement>("MyMod/Tutorials/Machine"), ResolveBlock);
var scene = new RealmPonderSceneBuilder(
    "mymod:machine", new("机器的工作过程", "How the machine works"), schematic);
scene.ConfigureCamera(schematic.Selection.Center, viewHeight: 8)
    .IndependentSection("machine", RealmPonderSelection.At(8, 2, 8))
    .ShowSection("base", new Vector3(0, -0.5f, 0), 15)
    .Idle(20)
    .Keyframe(new("放入机器", "Place the machine"))
    .ShowSection("machine", Vector3.UnitY, 20)
    .Text("explanation", new("观察顶部接口。", "Look at the top connection."),
        new Vector3(8.5f, 2.8f, 8.5f), 80)
    .Idle(85)
    .MarkAsFinished();
registry.Register(scene.Build(), tags: ["mymod:machines"], subjects: [machineContents]);
DialogsManager.ShowDialog(host, new RealmPonderDialog(registry.Get("mymod:machine"), registry));
```

时长单位为 tick（20 tick = 1 秒）。例如 `MoveSection(..., 40).RotateCamera(..., 40).Idle(40)` 表示两秒内同时移动区域和旋转镜头。新指令覆盖同区域同通道的旧动画；不同通道并行。

操作实体使用沙箱内的命名引用，而不是在闭包里保存 Entity：

```csharp
scene.World(world => world.CreateEntity("device", "MyDevice", initialValues))
    .Idle(20)
    .World(world => world.Entity("device").FindComponent<MyDeviceComponent>(true).Start())
    .Idle(80)
    .World(world => world.ModifyBlockEntity<MyDeviceComponent>(
        new Point3(8, 2, 8), component => component.Stop()));
```

创建时重新从只读数据库填充私有 `ValuesDictionary`。不使用宿主会修改共享模板缓存的 `DatabaseManager.CreateEntity(project, template, overrides, ...)` 重载。回退会销毁旧 Entity 与 Project，重新执行回调；外部不要缓存它们。

独立使用 `RealmPonderPlayer` 适合无图形的脚本测试。实际场景用一个 `RealmPonderSession(player)` 绑定；同一 player 不能绑定多个会话。会话释放会退订事件并销毁沙箱，调用方应先解绑视口。

## 旧预览 API 迁移

本仓库仍为 preview，旧 `AddStep` / `SetScript(async ..., expectedStepCount)`、`RealmPonderScriptContext`、示例专用布局类和 `RealmPonderStep` 已删除，避免维护两套不一致的时间线：

| 旧写法 | 新写法 |
| --- | --- |
| `SetScript` + `ShowScene(sceneName, layout)` | `RealmPonderSceneBuilder` + 初始 schematic + 方块/区域指令 |
| `Hold(seconds)` | `Idle(seconds * 20)`，整数 tick |
| 手填 `expectedStepCount` | `Keyframe(title)`，`Build()` 自动统计 |
| `Camera.LookAt/SetOrbit/RotateBy` | `ConfigureCamera` / `RotateCamera` |
| `RealmPonderAnnotation[]` | 有时长的 `Text` / `Controls` / `Outline` / `Line` |
| 静态 `RealmPonderPlayer.CreateStoryboard` | `new RealmPonderPlayer(tutorial)` + `RealmPonderSession`，或直接使用 Dialog |

Core 的 `RealmStoryboard` 和 `RealmStoryboardContext` 未更改，已有非 Ponder 沙箱脚本不受影响。

## 验证与复现

```powershell
dotnet test Tests/RealmEX.Tests.csproj --configuration Test
./tools/preview/run.ps1 -GameDir '<Survivalcraft.Windows 路径>'
dotnet build RealmEX.csproj -c Release -p:RealmEXSkipPack=true -p:RealmEXSkipSyncVersion=true
```

本轮验收包括：

- 22 项测试：并行动画、暂停、阅读减速、帧拆分一致性、前后定位、快照恢复、actor、区域拾取、注册回滚/隐藏教程、追踪包围盒、插值和不同尺寸的实际 Widget 布局。
- 隔离引擎进程中走正常 Dialog 启动和 `RealmPonderSession`：验证每 tick 0.05 秒、真实 Entity 及箱子库存回退、Terrain 恢复、主 Project 引用与共享模板不变、关闭后的 Realm 释放。
- 用实际指针坐标触发 `ClickableWidget.Update`，验证重播、暂停、上/下一步、查看模式和阅读模式；从已渲染方块的投影坐标反向拾取并取得宿主名称；验证中英文搜索和分类列表的实际指针选择。
- 39 张实际引擎截图：三个教程的全部关键帧、中英文、窄横屏和竖屏、目录，以及玻璃/水/模型/包围盒。输出位于忽略的 `artifacts/ponder-preview/`，不提交引擎资源和生成图片。

工具只读取宿主 `Content.zip` / 原生图形依赖，在自己的输出目录启动隐藏的渲染进程，不打开或修改玩家存档。本轮验证包含实际场景渲染与按钮绑定，但未代替玩家主世界中的 F6 验收，也未运行工业设备全套子系统。构建目前会报告宿主传递依赖 `SixLabors.ImageSharp 3.1.12` 的 NuGet 审计警告。

游戏中按 **F6** 打开与门教程，右上目录可切换非门与南瓜；方向键切步骤，空格暂停，细进度条可拖动，放大镜进入查看模式。
