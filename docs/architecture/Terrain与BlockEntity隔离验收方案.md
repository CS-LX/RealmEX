# Terrain 与 BlockEntity 隔离验收方案

> 本文设计 RealmEX 的 Terrain / BlockEntity 隔离验收方式。它不是内容模组接入方案，也不是把工业设备语义写进 RealmEX Core 的理由。

## 1. 目标

验证 RealmEX 可以承载「带真实方块语义的沙箱」，并保持主世界隔离：

- 沙箱拥有独立 `SubsystemTerrain`、`SubsystemBlockEntities`、`SubsystemBlockBehaviors` 等宿主通用子系统实例。
- 沙箱内方块变化、BlockEntity 注册、方块行为触发不影响主世界 `GameManager.Project`。
- 验收使用宿主通用概念和原版类型，不引用工业、化学、电网等内容模组程序集。
- 结论用于支撑后续 Ponder / Create 预设，而不是扩大 RealmEX Core 的默认 Minimal 模板。

## 2. 已知依赖事实

`SubsystemTerrain.Load` 不是孤立子系统。宿主源码显示它至少查找：

- `SubsystemGameWidgets`
- `SubsystemGameInfo`
- `SubsystemParticles`
- `SubsystemPickables`
- `SubsystemBlockBehaviors`
- `SubsystemAnimatedTextures`
- `SubsystemFurnitureBlockBehavior`
- `SubsystemSky`
- `SubsystemTime`
- `SubsystemTimeOfDay`
- `SubsystemPalette`
- `SubsystemElectricity`
- `SubsystemMetersBlockBehavior`

`SubsystemBlockBehaviors.Load` 会遍历 `BlocksManager.Blocks[*].Behaviors`，按行为名查找对应 `SubsystemBlockBehavior`。这意味着「只加一个 `SubsystemBlockBehaviors`」通常不够，必须提供被方块声明引用的行为子系统。

`ComponentBlockEntity.Load` 至少查找：

- `SubsystemMovingBlocks`
- `SubsystemTerrain`
- `SubsystemAudio`

因此，设备级 M1 不能靠在 Minimal 模板里随手追加 `Terrain` / `BlockEntities` 完成。它需要一个明确的验收模板或 Contributor 扩展机制。

## 3. 边界

### 3.1 RealmEX Core 不做

- 不把完整宿主 `ProjectTemplate` 复制进 `Assets/RealmEX/SandboxProjectTemplate.xdb`。
- 不把 Terrain 依赖链硬塞进 Minimal 模板。
- 不出现 `DeviceProbe`、工业设备、流体、电网等内容模组命名。
- 不在 Core 里引用内容模组程序集或类型名。

### 3.2 可做的位置

- `Diagnostics/`：只放验收入口和日志判断。
- `Presets/Ponder/` 或后续中性 `Presets/Terrain/`：放需要 Terrain 的可视化预设。
- `Core/Extensibility/`：后续放 Contributor 接口，让内容模组注入自己的子系统模板。

## 4. 建议分步

### M1T-0：依赖链摸底

产出：

- 列出 `SubsystemTerrain`、`SubsystemBlockEntities`、目标方块行为所需的宿主子系统。
- 选定一个不含内容模组语义的原版验证对象。

初选对象：

- **原版箱子 `Chest`**。`SubsystemChestBlockBehavior` 处理方块内容 `45`，继承 `SubsystemEntityBlockBehavior`，加载 `Chest` EntityTemplate，并在 `OnBlockAdded` 时向当前 `Project` 创建含 `ComponentBlockEntity` 的实体。

初选理由：

- `Chest` 是宿主通用 BlockEntity，不属于工业模组。
- 触发路径清晰：方块行为 → `Project.CreateEntity` → `SubsystemBlockEntities.OnEntityAdded`。
- 可验证主世界隔离：同坐标主世界 `SubsystemBlockEntities` 不应出现沙箱箱子实体。

已知额外依赖：

- `SubsystemChestBlockBehavior` 的父类 `SubsystemEntityBlockBehavior` 需要 `SubsystemTerrain`、`SubsystemBlockEntities`、`SubsystemGameInfo`、`SubsystemAudio`。
- `ComponentBlockEntity.Load` 还需要 `SubsystemMovingBlocks`、`SubsystemTerrain`、`SubsystemAudio`。

通过标准：

- 文档能解释「为什么不能直接改 Minimal」。
- 验收对象不依赖工业模组。

### M1T-1：Host Terrain 预设模板草案

产出：

- 新增独立模板草案，而不是修改 Minimal 模板。
- 模板只声明宿主通用子系统。
- 如果依赖链过长，先只做文档化模板清单，不急于落 xdb。

草案分两级推进：

- **BlockEntity 注册隔离级**：先覆盖 `Chest` 的实体创建与 `SubsystemBlockEntities` 注册路径，不要求真实地形区块生成或完整方块行为总线。
- **Terrain 方块变更级**：再覆盖 `SubsystemTerrain.ChangeCell` / 方块行为派发 / 邻居通知等路径，作为 M1T-3 的独立扩展。

BlockEntity 注册隔离级建议成员：

- 保留 Minimal 已有的 `Players`、`Time`、`Update`、`Drawing`。
- 增加宿主通用子系统：`GameInfo`、`Audio`、`MovingBlocks`、`BlockEntities`。
- 增加目标行为：`EntityBlockBehavior`、`ChestBehavior`。
- 增加 `Terrain` 时只用于满足 `ComponentBlockEntity` 和行为父类依赖；若 `SubsystemTerrain.Load` 继续拉长依赖链，本级允许改为文档化 BLOCKED，不把全量 Terrain 依赖塞入 Minimal。

Terrain 方块变更级待核实成员：

- `Terrain`、`BlockBehaviors`。
- `SubsystemTerrain.Load` 已知依赖：`GameWidgets`、`GameInfo`、`Particles`、`Pickables`、`BlockBehaviors`、`AnimatedTextures`、`FurnitureBlockBehavior`、`Sky`、`Time`、`TimeOfDay`、`Palette`、`Electricity`、`MetersBlockBehavior`。
- 上述清单若继续触发更多宿主 UI / 渲染 / 电路依赖，应暂停在 M1T-1，改为 Contributor 机制设计，不应复制完整宿主 `ProjectTemplate`。

落地约束：

- 独立 xdb 草案可命名为 `SandboxTerrainProjectTemplate.xdb`，但在验收通过前不接入 `RealmBootstrap.Create` 默认路径。
- 不新增 `RealmProfile.SubsystemSet` 或类似 Core 枚举。
- 不把 `Chest` 以外的内容模组对象写入 RealmEX Core。

通过标准：

- Core API 不新增与该模板强绑定的枚举或开关。
- Minimal 仍能通过 F8 / F9 / F10。

### M1T-2：BlockEntity 注册隔离

产出：

- 沙箱内创建或加载一个 BlockEntity。
- 检查沙箱 `SubsystemBlockEntities` 能注册该实体。
- 检查主世界 `SubsystemBlockEntities` 未新增对应坐标记录。
- F7 诊断入口：`[RealmEX/M1T]`。

当前实现：

- 独立模板：`RealmEXSandboxTerrainProject`。
- 验收对象：原版 `Chest`。
- 触发路径：`SubsystemChestBlockBehavior.OnBlockAdded` → `Project.CreateEntity` → `Project.AddEntity` → `SubsystemBlockEntities.OnEntityAdded`。
- Terrain 形态：`terrainMode=diagnostic-stub`，只满足 `ComponentBlockEntity` 的类型依赖；不代表 M1T-3 的真实 Terrain 方块变更验收。

通过标准：

- `GameManager.Project` 引用不变。
- 沙箱与主世界的 `SubsystemBlockEntities` 实例不同。
- 沙箱销毁后，沙箱 BlockEntity 注册表随之释放。

### M1T-3：Terrain 方块变化隔离

产出：

- 在沙箱 Terrain 内变更一个方块。
- 触发必要的修改队列 / 邻居通知。
- 检查主世界同坐标方块值不变。

通过标准：

- 沙箱 `SubsystemTerrain` 与主世界实例不同。
- 沙箱方块变化只进入沙箱 Terrain。
- 主世界 Terrain、BlockEntity、BlockBehavior 均无新增状态。

## 5. 验收日志建议

日志前缀使用 `[RealmEX/M1T]`，不要复用 `[RealmEX/M1]` 的核心隔离结论。

建议输出：

```text
[RealmEX/M1T] READY scope=TerrainIsolation action=...
[RealmEX/M1T] CHECK=PASS name=terrain-subsystem-isolated
[RealmEX/M1T] CHECK=PASS name=block-entity-registry-isolated
[RealmEX/M1T] CHECK=PASS name=main-world-cell-unchanged
[RealmEX/M1T] RESULT=PASS scope=TerrainIsolation
```

若模板依赖未满足，应输出 `RESULT=BLOCKED`，不要把依赖未满足伪装成测试失败。

## 6. 当前结论

M1T-0 / M1T-1 当前结论：

1. 原版验证对象初选 `Chest`。
2. F7 已先做 BlockEntity 注册隔离级验收，不直接追求完整 Terrain 方块变更。
3. `SandboxTerrainProjectTemplate.xdb` 作为独立诊断模板，不接入默认 Minimal。
4. 下一步是 M1T-3：真实 Terrain 方块变更隔离；若宿主依赖未满足，应输出 `RESULT=BLOCKED`，而不是把模板依赖问题当作隔离失败。
