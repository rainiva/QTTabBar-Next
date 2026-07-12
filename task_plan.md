# Task Plan: Structural Governance Wave 16+ Root Cure

## Goal
**彻底根治** 多入口、多真源、上帝模块、换壳 Host — Grill 1–9 已冻结。详见 `docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md`。

## Current Phase
**Phase 0** — 部署/Options 阻塞解除（D→B 第一步）

### Phase 0: 部署真源（immediate）
- [ ] 管理员安装 MSI 或 `Register/Register.bat Release`
- [ ] `restart explorer.bat`；验证 Options 无 XAML 错误
- **Status:** pending（registry 仍指向 GAC 1.0.0.0 时需管理员）

### Phase 1: Wave 16 — 门禁可信化（∥ Wave 15 7/7 可选）
- [ ] Task 16.1: `BandOrchestrationClusterBudgetTests`（7550 + TabBarBase 1928）
- [ ] Task 16.2: Hotspot 测试去矛盾
- [ ] Task 16.3: 治理文档 + AcceptanceMatrix
- **Status:** pending

### Phase 2: Wave 17 — 单入口 / 单真源
- [ ] 17.1: Options + Plugins 白名单；**删除 OpenOptionDialog**；修 QTQuick
- [ ] 17.2: `virtual SetContextMenuedTab` + 写路径扫描
- [ ] 17.3: CurrentTab UI 不变量
- [ ] 17.4: `ExplorerNavigationOrchestrator`
- **Status:** pending

### Phase 3: Wave 18 — 删 ExplorerController + 自动化 Z
- [ ] 18.1: SubDirTip 迁出；ShellHosts ≤900
- [ ] 18.2: **删除 ExplorerController**；ComponentBuild 直配 leaf
- [ ] 18.3: SingleHost + SameInstance + ComponentBuild 扫描；合并 BindAction Host
- [ ] 18.4: 删 TabManager；IPluginServerHost ≤20；Host ≤40
- **Status:** pending

### Phase 4: Wave 19 — 扫描 + Options UI
- [ ] Session/Config bypass 扫描；Options UI 测试
- **Status:** pending

### Phase 5: Wave 20 — 根治验收
- [ ] 人工 **10/10** signed（7+2+3 导航）
- [ ] Band Cluster **≤6800**；全量回归 + CI
- **Status:** pending

### Completed: Wave 10–15
- [x] 1075 tests；MenuOperations 迁出；Context 注入；OptionsDialogWpfBootstrap

## Grill 决策（冻结 2026-07-12）
| Q | 决策 |
|---|------|
| Q1 | Band Orchestration Cluster ~7550 |
| Q2 | 7550 → Wave20 ≤6800 |
| Q3 | TabBarBase ≤1928 并行 |
| Q4 | 禁止 >1 Host；删 ExplorerController；自动化 Z |
| Q5 | 人工 10 项（7+2+3） |
| Q6 | ExplorerNavigationOrchestrator |
| Q7 | virtual SetContextMenuedTab |
| Q8 | 删 OpenOptionDialog + Plugins 白名单 |
| Q9 | D→B |

## Notes
- 不 stage `.qoder/`、`_tr/`、`.codegraph/`
- Wave15 7/7 不阻塞 Wave16；Wave20 需 10/10
