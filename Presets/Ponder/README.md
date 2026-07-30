# Ponder Preset

Ponder 预设用于教程、演示、镜头脚本、标注和配置化场景展示。该模块只能依赖 `RealmEX.Core`，不得成为核心运行时的反向依赖。

可见 Ponder（Dialog / 视口上屏 / 世界动画）的改动**必须**落在本目录；不得大改 Core，最多允许 Core 补少量渲染 / 相机公开能力。硬性要求见 `docs/实施计划.md` §2.4。

## 当前最小闭环

- `RealmPonderTutorial`：教程数据容器。
- `RealmPonderStep`：场景名、时间倍率、说明文字、等待秒数。
- `RealmPonderPlayer`：把教程步骤转换成 Realm 自驱动 async `RealmStoryboard`。
- `RealmPonderSamples.CreateNotGateTutorial()`：内置非门真值表示例。
- `RealmPonderDialog` / `RealmPonderVisuals`：玩家可见页面、caption、步骤进度、镜头动画。
- `RealmPonderPumpkinLayouts` / `RealmPonderBlockPresenter`：把泥土、耕地、南瓜生长阶段、南瓜灯写入沙箱 Terrain，并生成真实方块网格。

F6 默认播放南瓜教程（Create 式分步）：田地 → 瓜苗 → 生长 → 成熟 → 南瓜灯。方块数据在 `RealmEXPonderProject` 沙箱内，不泄漏到主世界。

游戏内按 **F6** 打开可见 `[RealmEX/Ponder]` Dialog。
