# Ponder Preset

Ponder 预设用于教程、演示、镜头脚本、标注和配置化场景展示。该模块只能依赖 `RealmEX.Core`，不得成为核心运行时的反向依赖。

可见 Ponder（Dialog / 视口上屏 / 世界动画）的改动**必须**落在本目录；不得大改 Core，最多允许 Core 补少量渲染 / 相机公开能力。硬性要求见 `docs/实施计划.md` §2.4。

## 当前最小闭环

- `RealmPonderTutorial`：教程数据容器，支持 Create 风格 `SetScript`。
- `RealmPonderScriptContext`：`await ShowScene` / `await Hold`；清屏色由脚本显式传入。
- `RealmPonderCameraActions`：可 await 的摄像机动作，例如 `RotateBy(..., RealmPonderEase.SinInOut)`。
- `RealmPonderPlayer`：把教程脚本转换成 Realm 自驱动 async `RealmStoryboard`。
- `RealmPonderDialog`：沿用宿主遮罩，标题、浮动说明、场景和底部步骤条；窄窗口自适应，长说明滚动。
- `RealmPonderWidget`：正交视口，支持拖动旋转、滚轮缩放、镜头归位及世界坐标标注。
- `RealmPonderBlockPresenter`：只在布局变化时重建/上传网格；新增或变化的方块用短暂浮现动画强调。
- `RealmPonderAndGateLayouts` / `RealmPonderPumpkinLayouts`：真实方块场景布局。

F6 默认播放**与门**教程：底板 → 与门 → 输入接线 → 00 → 10 → 01 → 11 → 口诀。镜头默认保持稳定，每步约 3–6 秒。

底部支持暂停/继续、上一步/下一步、点击步骤跳转、重播和镜头归位。空格暂停，左右方向键换步骤，Esc / 返回关闭。
步骤跳转会重建临时 Realm，并重放前序布局（跳过 `Hold` 和相机动画等待），在所选步骤暂停；不复用上一轮仿真状态。
因此教程脚本的布局应只修改沙箱，避免外部副作用。与门示例仍是方块状态演示，没有完整电路仿真。

脚本风格：

```csharp
await ponder.ShowScene("and-00", "两个开关都关闭，输出保持关闭。",
    RealmPonderAndGateLayouts.Apply, title: "都关闭：0 与 0",
    annotations: [new(new(8.5f, 2.2f, 6.5f), "输出 = 0", Color.White, new(10, -80))]);
await ponder.Hold(5.0);
```

游戏内按 **F6** 打开可见 `[RealmEX/Ponder]` Dialog。Esc / 返回关闭。

布局回归：`dotnet test Tests/RealmEX.Tests.csproj --configuration Test`。测试使用宿主布局引擎与无纹理的字体度量，覆盖 6 种窗口尺寸和长说明；不替代游戏内 GPU 渲染验收。
