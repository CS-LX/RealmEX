# Ponder Preset

Ponder 预设用于教程、演示、镜头脚本、标注和配置化场景展示。该模块只能依赖 `RealmEX.Core`，不得成为核心运行时的反向依赖。

## 当前最小闭环

- `RealmPonderTutorial`：教程数据容器。
- `RealmPonderStep`：场景名、时间倍率、说明文字、等待秒数。
- `RealmPonderPlayer`：把教程步骤转换成 Realm 自驱动 async `RealmStoryboard`。
- `RealmPonderSamples.CreateNotGateTutorial()`：内置非门真值表示例。

非门示例目前用于验证 Ponder 配置 / Storyboard / 视口闭环，只展示「输入与输出相反」的教程逻辑；它不加载完整电路系统，也不声明真实电路仿真已经接入。

游戏内按 **F6** 可运行 `[RealmEX/Ponder]` 诊断。
