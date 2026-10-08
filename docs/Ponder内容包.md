# 可选 Ponder 内容包

内容模组不引用 RealmEX DLL，也不在 `modinfo.json` 声明 RealmEX 依赖。把 XML 清单、结构快照和 JavaScript 编排随自己的 `.scmod` 打包即可。RealmEX 打开教程时从 `ModsManager.ModList` 扫描 `*.ponder.xml`，原子注册每个内容包。未安装 RealmEX 时这些文件不参与运行。

JavaScript 源文件使用 **`.pjs`** 后缀。宿主会无条件执行模组内的普通 `.js`；换后缀是为了避免未安装 RealmEX 时的全局脚本副作用，语法仍是 JavaScript。文件由拥有该清单的 `ModEntity.GetFile` 读取，支持普通包及 FastDebug。路径相对清单所在目录，不能跨目录向上引用。

```xml
<PonderPack Version="1" Namespace="example">
  <Palette>
    <Block Id="stone" CraftingId="stone" />
    <Block Id="controller" CraftingId="YourController" DataOffset="0" />
  </Palette>
  <Tag Id="machines" Zh="机器" En="Machines" />
  <Ui Id="controller" Asset="Widgets/YourControllerWidget" Width="614" Height="382"
      Zh="控制器" En="Controller" />
  <Tutorial Id="first_machine" Zh="第一台机器" En="Your first machine"
      Script="first_machine.pjs" Schematic="first_machine.xml"
      Tags="machines" Subjects="controller" Order="10" />
</PonderPack>
```

`Palette` 用宿主 `Block.GetCraftingId` / `GetCreativeValues` 解析物品，避免把工业动态分配的数值 ID 写死。`DataOffset` 用于同一设备的方向或状态差值；无偏移时省略。原版固定值也可以写 `Value`。`Subjects` 使用完整方块值，查询时按 crafting ID 匹配方向变体，不将共用 503 的不同工业设备混在一起。`Tags`、`Subjects` 以空格分隔；`Schematic` 可省略，用 JS 的 `fill` 构建结构。

快照复用 `PonderSchematic Version="1"`，`Fill` 的 `Block` 指向清单调色板。`Ui` 复用内容模组原有机器 XML；由 C# 动态生成的库存/按钮可通过脚本显式补齐。

```javascript
scene.camera([8, 2, 8], 9, 35);
scene.keyframe(["搭建底座", "Build the base"]);
for (let x = 6; x <= 10; x++) scene.fill([x, 0, 6], [x, 0, 10], "stone");
scene.show("base", [0, -1, 0], 20);
scene.text("help", ["先搭好底座。", "Start with the base."], [8, 0, 8], 100);
scene.idle(100);
scene.keyframe(["控制器", "Controller"]);
scene.uiShow("controller");
scene.uiInventory("InventoryGrid");
scene.uiText("RunButton", ["启动", "Start"]);
scene.uiPoint("RunButton", 60);
scene.idle(60);
scene.uiClick("RunButton", 20);
scene.idle(20);
scene.uiText("RunButton", ["停机", "Stop"]);
scene.idle(60);
scene.uiHide();
```

除了 `idle`，指令不移动编排游标。同一时刻的动画并行执行。坐标为三元素数组，范围参数为两个角点数组，文本为字符串或 `[中文, English]`。时间均为 tick（每秒 20）；UI 文档见 [机器界面演示](Ponder机器界面演示.md)。

| JS 指令 | 参数顺序 |
| --- | --- |
| `idle` / `keyframe` | ticks / text |
| `camera` / `rotateCamera` | target, viewHeight, yaw / degrees, duration |
| `section` | id, from, to |
| `show` / `hide` / `move` / `rotate` | sectionId, vector, duration |
| `fill` / `restore` | from, to, paletteIdOrValue / from, to |
| `text` | id, text, target, duration |
| `outline` / `line` | id, from, to, duration, optional RGB array |
| `removeOverlay` | id |
| `item` / `moveItem` / `removeItem` | id, paletteIdOrValue, position / id, offset, duration / id |
| `success` / `finish` | target, duration / 无参数 |
| `uiShow` / `uiHide` | uiId / 无参数 |
| `uiText` / `uiFont` / `uiButtonColor` | target, text / target, fontAsset / target, RGB |
| `uiEnabled` / `uiVisible` / `uiValue` | target, value |
| `uiAnimateValue` | target, from, to, duration |
| `uiConfigure` | target, 属性对象（值使用 XML 格式的字符串） |
| `uiInventory` / `uiItem` | grid / slot, paletteIdOrValue, count |
| `uiButton` | grid, name, text, column, row, [width,height] |
| `uiPoint` / `uiClick` | target, duration |
| `uiDrag` / `uiScroll` | fromTarget, toTarget, duration / target, absolutePosition, duration |

目标可以用 `/` 限定控件路径，例如 `RunButton/BevelledButton.Canvas`。`uiConfigure` 通过宿主原有 XML 属性加载器设置当前独立实例，可补齐原机器构造器中的边距、字号及自定义量表阈值，例如 `scene.uiConfigure('TemperatureBar', { WarningValue: '0.73' })`。不修改原机器的控件实现或资源默认值。

工业启用 AMPK 时，XML 枚举由加密资源加载器接管。将整套内容放在 `Assets/Ponder/`，让清单、脚本和快照一起通过原模组资源管线加载；不要把清单留在受该管线屏蔽的包根目录。

JS 可以用函数、循环、条件、数组、计算等组织演出和附加逻辑。它在独立 Jint 实例中编译为时间轴，不使用宿主全局 JS 引擎，不暴露 CLR、玩家 Project、文件系统或网络；`Math.random` 为可重复序列，`Date` 不可用。脚本限制为 100,000 条语句、10,000 条演出指令、16 MiB JS 分配及 3 秒编译时间；单个资源小于 1 MiB，教程最多 20 分钟。编译完即释放 JS 引擎，回放执行已编排的动作，不能保留任意 JS 对象去修改主世界。

每个命名空间使用一个清单。清单/脚本/结构有错误时整包回滚，已成功加载的其它模组教程保留；日志给出模组、文件和错误。纯时间轴结构先做预演，UI 目标在创建实际控件树时检查。XML-only UI 的数值是脚本编排的教学状态，不能据此声称运行了原机器完整仿真。

验证入口：`dotnet test Tests/RealmEX.Tests.csproj --configuration Test`，涵盖自动发现、JS 计算与回退、包间隔离、注册回滚、无限循环、非法资源路径、时间/高度限制，以及共用方块编号的设备关联。实际工业资源验收由工业仓库的专用 Ponder 预览工具完成。
