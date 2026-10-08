# Ponder 机器界面演示

UI 演示与方块演出共用 20 Hz 时间轴。`ShowUi` 创建独立控件树，回退、重播和重新显示时重新创建；`HideUi` 释放。可使用原机器 XML，也可以由 C# 工厂创建真正的 Widget 实例。工厂必须创建独立实例及其演示数据，禁止返回或绑定玩家正在使用的界面、库存或机器。

界面保留机器原有风格、默认字体和尺寸。大窗口将说明与机器并排，小窗口将说明置于上方，机器区双向滚动。高亮目标改变时自动滚入可见区，玩家仍可自行拖动。UI 区截获自己的拖动，不旋转后面的场景；演示控件不接收玩家的真实点击。

```csharp
builder.ShowUi(RealmPonderUiDefinition.FromXml(
    "Widgets/FurnaceWidget", new("熔炉", "Furnace"), new(614, 382)))
    .UiInventory("InventoryGrid")
    .UiInventory("FurnaceGrid")
    .UiItem("InventoryGrid.0", PlanksBlock.Index, 8)
    .Idle(20)
    .UiDrag("InventoryGrid.0", "FuelSlot", 30)
    .Idle(30)
    .UiItem("InventoryGrid.0", 0, 0)
    .UiItem("FuelSlot", PlanksBlock.Index, 8)
    .UiAnimateValue("Progress", 0, 1, 60)
    .Idle(60)
    .HideUi();
```

操作不会自动推进编排游标，用 `Idle` 表示等待。`UiDrag` 表示指针路径，物品变化由显式的 `UiItem` 编排，方便分步解释。`UiClick` 在动作中点向目标的原 Widget 处理函数交付一次点击，且不把输入发送到玩家界面。XML-only 界面没有原机器 C# 逻辑：需用脚本或 `UiEdit` 编排演示数据变化；它是教学演示，不声称在运行完整机器仿真。

| 指令 | 用途 |
| --- | --- |
| `UiText` | 修改 Label、Button 或 TextBox，接受中英文文本 |
| `UiEnabled` / `UiVisible` | 更新指定控件状态 |
| `UiValue` / `UiAnimateValue` | 修改或按时间轴动画显示 Slider / ValueBar 的数值 |
| `UiInventory` | 为现有空 Grid 创建独立库存；格子命名为 `GridName.0` 等 |
| `UiItem` | 修改演示库存槽的物品与数量；禁止修改其它库存 |
| `UiButton` | 为已有 Grid 补充由机器 C# 动态生成的按钮 |
| `UiPoint` / `UiClick` / `UiDrag` / `UiScroll` | 高亮、点击、移动指针、滚动指定控件 |
| `UiEdit` | C# 高级扩展；只修改传入的独立控件树，回放时重复执行 |

控件通过原 XML 的 `Name` 定位，找不到目标时明确报错，Dialog 停止该教程并允许重播。`UiScroll` 使用绝对滚动位置。动作、点击交付与数值变化都绑定演出 tick；暂停不会重复点击或继续改变数值。

验收：`dotnet test Tests/RealmEX.Tests.csproj --configuration Test` 共 24 项通过，包含原按钮处理函数只调用一次、回退后全新 UI 状态、数值动画、暂停和关闭。`tools/preview/run.ps1` 的实际引擎验收另外加载原版熔炉 XML、创建独立库存、验证槽位数量/进度/回退/主 Project 隔离以及双向拖动输入；输出中英文、宽屏、窄横屏与竖屏截图。它不是玩家存档中的机器仿真验收。
