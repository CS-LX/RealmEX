# 发版说明

版本号以 **`modinfo.json` → `Version`** 为唯一真相源；构建前 `tools/sync-version.ps1` 同步到 `RealmEX.csproj`。

## 版本格式

| 类型 | `modinfo.json` → `Version` | git tag |
| --- | --- | --- |
| 预览 | `1.0.0.0-previewN` | `v1.0.0.0-previewN` |
| 正式 | `1.0.0.0` | `v1.0.0.0` |

tag 必须与 modinfo **完全一致**（仅多 `v` 前缀）。

## 预览发版步骤

1. 修改 `modinfo.json` 的 `Version`。
2. 提交并推送 `main`。
3. 打标签并推送：`git tag v1.0.0.0-preview2` → `git push origin v1.0.0.0-preview2`
4. GitHub Actions `Release` 工作流自动构建 `.scmod` 并创建 Release。

## CI

- **Build**：`main` / PR 推送 → 构建 + 上传 `RealmEX-ci.{sha7}.scmod` 构件。
- **Release**：`v*` 标签 → 校验版本 → `RealmEX-{Version}.scmod`。

## 下游模组

内容模组在 `modinfo.json` 的 `Dependencies` 中声明 `com.realmex` 版本；**工业时代 2 等主仓暂不强制依赖**，待 P0 骨架稳定后再接入。
