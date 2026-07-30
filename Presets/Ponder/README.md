# Ponder Preset

Ponder 预设用于教程、演示、镜头脚本、标注和配置化场景展示。该模块只能依赖 `RealmEX.Core`，不得成为核心运行时的反向依赖。

可见 Ponder（Dialog / 视口上屏 / 世界动画）的改动**必须**落在本目录；不得大改 Core，最多允许 Core 补少量渲染 / 相机公开能力。硬性要求见 `docs/实施计划.md` §2.4。

## 当前最小闭环

- `RealmPonderTutorial`：教程数据容器，支持 Create 风格 `SetScript`。
- `RealmPonderScriptContext`：`await ShowScene` / `await Hold`；清屏色由脚本显式传入。
- `RealmPonderCameraActions`：可 await 的摄像机动作，例如 `RotateBy(..., RealmPonderEase.SinInOut)`。
- `RealmPonderPlayer`：把教程脚本转换成 Realm 自驱动 async `RealmStoryboard`。
- `RealmPonderDialog`：全屏 20% 黑 mask + 居中方块视口，无面板框。
- `RealmPonderAndGateLayouts` / `RealmPonderPumpkinLayouts`：真实方块场景布局。

F6 默认播放**与门**教程：介绍 → 00 → 10 → 01 → 11 → 口诀。

脚本风格：

```csharp
await ponder.ShowScene("and-00", "输入 0 和 0：两边都断开，输出保持熄灭。", RealmPonderAndGateLayouts.Apply, clearColor: Color.Transparent);
await ponder.Camera.RotateBy(50f, 1.15, RealmPonderEase.SinInOut);
```

游戏内按 **F6** 打开可见 `[RealmEX/Ponder]` Dialog。Esc / 返回关闭。
