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
- 选定一个不含内容模组语义的原版验证对象，例如原版可产生 BlockEntity 或方块行为的最小组合。

通过标准：

- 文档能解释「为什么不能直接改 Minimal」。
- 验收对象不依赖工业模组。

### M1T-1：Host Terrain 预设模板草案

产出：

- 新增独立模板草案，而不是修改 Minimal 模板。
- 模板只声明宿主通用子系统。
- 如果依赖链过长，先只做文档化模板清单，不急于落 xdb。

通过标准：

- Core API 不新增与该模板强绑定的枚举或开关。
- Minimal 仍能通过 F8 / F9 / F10。

### M1T-2：BlockEntity 注册隔离

产出：

- 沙箱内创建或加载一个 BlockEntity。
- 检查沙箱 `SubsystemBlockEntities` 能注册该实体。
- 检查主世界 `SubsystemBlockEntities` 未新增对应坐标记录。

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

下一步不是改 Core，而是完成 M1T-0 / M1T-1：

1. 确认最小宿主通用子系统清单。
2. 确认原版验证对象。
3. 设计独立 Terrain 预设或 Contributor 入口。
4. 再实现 `[RealmEX/M1T]` 诊断。
