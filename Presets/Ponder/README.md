# Ponder Preset

Ponder 预设用于教程、演示、镜头脚本、标注和配置化场景展示。该模块只能依赖 `RealmEX.Core`，不得成为核心运行时的反向依赖。

可见 Ponder（Dialog / 视口上屏 / 世界动画）的改动**必须**落在本目录；不得大改 Core，最多允许 Core 补少量渲染 / 相机公开能力。硬性要求见 `docs/实施计划.md` §2.4。

## 当前最小闭环

- `RealmPonderTutorial`：教程数据容器。
- `RealmPonderStep`：场景名、时间倍率、说明文字、等待秒数。
- `RealmPonderPlayer`：把教程步骤转换成 Realm 自驱动 async `RealmStoryboard`。
- `RealmPonderSamples.CreateNotGateTutorial()`：内置非门真值表示例。
- `RealmPonderDialog` / `RealmPonderVisuals`：玩家可见页面、caption、步骤进度、镜头与清屏色动画。

非门示例目前用于验证 Ponder 配置 / Storyboard / 可见 UI 闭环，只展示「输入与输出相反」的教程逻辑；它不加载完整电路系统，也不声明真实电路仿真已经接入。当前 Minimal 沙箱视口以清屏色与镜头运动表达可见动画；真实方块布局后续补。

游戏内按 **F6** 打开可见 `[RealmEX/Ponder]` Dialog。
