# RealmEX — 可选 NuGet 依赖备忘

> **文档性质**：实施前调研备忘，**非**已采纳依赖清单。引入任一包前须评估 `.scmod` 体积、GPL-3.0 兼容性、与宿主 API 的耦合。  
> **最后更新**：2026-06-16

---

## 协程 / Storyboard（P3）

| 包 | 用途 |
| --- | --- |
| **UniTask**（`Cysharp.Threading.Tasks`） | 叙事序列、等待帧/时间、取消；替代自写 `IEnumerator` 调度器 |
| **R3**（`R3`） | 事件流驱动 Storyboard 步骤（可选，与 UniTask 二选一或混用） |

**备注**：工业时代 2 已有 `SCIENEW/Modules/Coroutine/`，亦可抽成共享库复用，不必上 NuGet。  
**UniTask 注意**：非 Unity 环境须在 `RealmHost` 每帧挂接驱动（自定义 PlayerLoop / `Update`）。

---

## 配置与元数据（`RealmProfile` / `realm.meta.json`）

| 包 | 用途 |
| --- | --- |
| **System.Text.Json** | 内置 BCL，读写 JSON（一般**不必**单独引包） |
| **JsonSchema.Net**（`JsonSchema.Net`） | 校验 Profile / Scene JSON 结构 |
| **FluentValidation**（`FluentValidation`） | Profile 业务规则校验（倍率、子系统集等） |
| **Microsoft.Extensions.Options** + **Options.DataAnnotations** | 配置绑定 + 注解校验 |

---

## 扩展点 / Contributor（P5）

| 包 | 用途 |
| --- | --- |
| **Scrutor**（`Scrutor`） | 扫描程序集，自动注册 `IRealmProjectTemplateContributor` |
| **Microsoft.Extensions.DependencyInjection** | 轻量 DI 容器管理 Contributor 生命周期 |

---

## 并行 Tick / 调度（P2）

| 包 | 用途 |
| --- | --- |
| **System.Threading.Channels** | 内置 BCL，Realm 间 tick 任务队列 |
| **Nito.AsyncEx**（`Nito.AsyncEx.Coordination`） | `AsyncLock`、限流，保护并行 tick 与存档 IO |

---

## 持久化 / 文件（P4）

| 包 | 用途 |
| --- | --- |
| **System.IO.Abstractions**（`System.IO.Abstractions`） | 抽象 `Realms/<id>/` 读写，便于单测 |
| **Polly**（`Polly`） | 存档读写重试、超时（可选） |

---

## 性能 / 集合

| 包 | 用途 |
| --- | --- |
| **ZLinq**（`ZLinq`） | 零分配 LINQ；同仓 RecipaediaEX 已在用 |
| **CommunityToolkit.HighPerformance** | `Span` / 池化缓冲，大量 chunk / 实体遍历时可选 |

---

## 版本与依赖声明

| 包 | 用途 |
| --- | --- |
| **NuGet.Versioning** | 解析 `modinfo.json` 里 `com.realmex` 版本范围 |

---

## 测试（建议单独 Test 工程）

| 包 | 用途 |
| --- | --- |
| **Microsoft.NET.Test.Sdk** + **xunit** | 单元测试 |
| **NSubstitute** / **Moq** | Mock `Project` / 文件系统 |
| **BenchmarkDotNet** | 并行 tick 预算与性能回归 |

---

## 一般不必引包（用 BCL 即可）

- **JSON / XML / 并发基础**：`System.Text.Json`、`System.Xml`、`ConcurrentDictionary`、`SemaphoreSlim`
- **日志**：宿主 `Engine.Log`
- **补间动画**：Storyboard 镜头 / 方块动画通常几行 Lerp 即可；DOTween 等为 Unity 向，不适用

---

## 硬性边界

1. 世界 ECS 序列化走宿主 `ValuesDictionary` / `Project.xml`，**不要用 JSON 包替代存档管线**。
2. 模组打包会把依赖 DLL 打进 `.scmod`；优先选**小体积、MIT/Apache**、无原生依赖的包。
3. 当前 `RealmEX.csproj` 仅引用 `SurvivalcraftAPI.Survivalcraft`；上表条目均未采纳。
