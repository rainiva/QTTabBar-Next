# QTTabBar-Next 结构治理契约

本文件记录结构治理计划冻结后的权威入口、热点预算、允许编辑与禁止职责。任何改动若违反本文件约束，必须先在架构评审中更新本文件。

## 1. 冻结的权威入口与 Source

| 能力 | Canonical Entry | Owner | 禁止路径 |
|---|---|---|---|
| 打开设置 | `OptionsDialog.Open` → `InstanceManager.ExecuteOnServerProcessOpenOptions` → `IpcCommand.OpenOptions` → `OptionsDialog.OpenOnServer` → `OpenInternal` | `OptionsDialogCoordinator` | 独立 preview、公开 PoC launcher |
| 完整配置提交 | `ConfigManager.CommitSnapshot(Config, ConfigCommitScope, bool)` | `ConfigManager`（`LoadedConfig` 私有写，`RegistryConfigWriter` 写注册表） | UI/工具赋值 `LoadedConfig` 或调用 `WriteConfig` |
| 局部配置提交 | `ConfigManager` 具名命令 / `MutateAndCommit` | `ConfigManager` | 直接修改全局 `Config` 后持久化 |
| 标签插入位置 | `TabInsertionPolicy.Resolve(TabPos, int, int)` | 操作级 `TabPos` | 临时写 `Config.Tabs.NewTabPosition` |
| COM 注册 | `ComRegistrationManager` | 编译项 + `[Guid]` 唯一性测试 | 未编译完整注册类、同 GUID 第二 coclass |
| TabBar 编排 | `QTTabBarClass` 仅作为 COM adapter/composition root | 顶层 controller + 窄 host interface | nested controller、无界 owner 回指、隐藏 facade |
| ButtonBar 编排 | `QTButtonBar` 仅作为 band adapter/composition root | lifecycle/items/command controller | 向 partial class 继续加入业务逻辑 |

## 2. 热点 no-growth 预算

| 热点 | Task 13 实测值 | 目标最终预算 | 状态 |
|---|---|---|---|
| `QTTabBarClass` family 行数 | 7160 | 7160（不增长） | ✓ no-growth 基线通过 |
| `QTTabBarClass` nested controller | 9 | 0 | ⚠ 9 个嵌套类型待提取（ComRegistrationController, MenuOperations, TabOperations, SubDirTipOperations, WindowMergeTarget 等） |
| `QTTabBarClass` `_owner.` 回指 | 0 | 0 | ✓ 已达标（Batch 3 消除） |
| `QTButtonBar` family 行数 | 2164 | 不增长 | ✓ no-growth 基线通过 |
| `QTTabBarClass.cs` 主文件 | 395 | ≤ 500 | ✓ 已达标（Batch 6 缩减） |
| `QTTabBarClass` partial 声明 | 4 | ≤ 4 | ✓ 已达标（Batch 5 合并） |
| `QTButtonBar.cs` 主文件 | 442 | ≤ 450 | ✓ 已达标 |
| `QTButtonBar` partial 声明 | 0 | 0 | ✓ 已达标 |
| `OptionsDialog.xaml.cs` | 280 | ≤ 500 | ✓ 已达标（Batch 1 拆分） |

**治理规则：**
- 除非同时降低另一热点，否则任何提交不得让上述指标超过当前基线。
- Task 13 最终验收已启用除 `NestedControllers` 外的所有预算测试（`NestedControllers` 保留 `[Explicit]`，9 个嵌套类型待后续提取）。
- 全量测试 1000 项，978 通过，22 跳过（含 `[Explicit]`），0 失败。TRX 日志保存于 `TestResults/test-results-batch7.trx`。

## 3. 允许编辑与禁止职责

### 允许编辑
- 在现有 controller 边界内修复 bug。
- 新增 controller 时，必须同时从 `QTTabBarClass`/`QTButtonBar` 中迁出相应职责，不增长基线。
- 新增配置类别必须通过 `ConfigManager.CommitSnapshot` 或 `MutateAndCommit` 提交。

### 禁止职责
- 禁止在 `QTTabBarClass` 主文件或 partial 中新增业务逻辑。
- 禁止在 `QTButtonBar` 主文件或 partial 中新增 band 业务逻辑。
- 禁止新增 `Config.*` 直接赋值后持久化；必须使用 `ConfigManager` 命名命令。
- 禁止新增 `ShowStandalonePreview`、`WriteConfig` 等第二入口或旁路 writer。
- 禁止新增未在 `CanonicalEntry` 中登记的入口点。

## 4. 升级与退役条件

- 当某 controller 被验证为单一职责、仅依赖窄 host interface 且可独立单元测试时，可从 nested 升级为顶层类型。
- 当某入口点连续两个 release 周期无生产调用时，可标记为 obsolete 并在下下个 release 移除。
- 结构治理文档的修改必须伴随至少一个测试更新，确保新约束可自动校验。
