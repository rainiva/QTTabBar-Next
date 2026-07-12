# Wave 20 导航专项手动签收 Checklist（场景 10–12）

对应 `docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md` **Task 20.1** 人工签收 **10–12**。  
与 `wave15-explorer-manual-signoff-checklist.md`（场景 1–7）及部署场景 8–9 合并为 **Wave 20 人工 10/10**。

**背景：** Wave 17 `ExplorerNavigationOrchestrator` + Wave 18 **删除 `ExplorerController`** 后，须在真实 Explorer 中验证 COM 导航、TravelLog、锁定 tab 分支无回归。

---

## 测试前准备（场景 10–12 共用）

- [ ] **N1** 已完成 Wave 15 清单 **P1–P3**（构建、COM 注册、全新 Explorer 窗口）
- [ ] **N2** 当前分支已包含 Wave 17–18 导航重构（Orchestrator + 无 ExplorerController）
- [ ] **N3** 准备 **3 层有效目录** 用于多步导航，例如：
  - `C:\Windows`
  - `C:\Windows\System32`
  - `C:\Users`
- [ ] **N4** 选项 → **Tabs**：确认 **Restore session** / 锁定 tab 相关选项可配置（场景 12 需要）

**记录列（每项场景填写）：**

| 字段 | 示例 |
|------|------|
| 测试日期 | 2026-07-12 |
| 构建配置 | Release |
| Explorer 版本 | Win11 24H2 |
| 结果 | Pass / Fail |
| 备注 | 异常现象、截图路径 |

---

## 场景 10 — 后退 / 前进（TravelLog）

**通过标准：** 多步导航后，后退/前进与标签路径、Explorer 地址栏一致；无崩溃、无空白 tab。

**自动化参考：** `NavigationEntryPointTests`（Wave 17）、`TopLevelNavigationAndTabControllerTests`（Wave 18 叶控制器）

### 步骤

1. [ ] 打开新 Explorer，确认 TabBar 可见，初始 **TabCount ≥ 1**
2. [ ] 在当前 tab 依次导航到 **N3 中 3 个不同路径**（地址栏输入或双击文件夹，每步等待加载完成）
3. [ ] 点击 TabBar **后退** 按钮 **2 次**（或等价快捷键 Alt+←）
4. [ ] 再点击 **前进** 按钮 **1 次**（或 Alt+→）
5. [ ] 观察当前 tab 标题与 Explorer 文件夹路径

### 验证

- [ ] Explorer **未崩溃**、无持续卡死
- [ ] 后退后路径与步骤 2 的历史顺序 **一致**（第 2 次后退应接近起始路径）
- [ ] 前进后路径 **正确前进一级**
- [ ] 当前 tab **仅 1 个**，无重复路径 tab 暴增
- [ ] TabBar 后退/前进按钮状态（启用/禁用）与历史深度 **合理**

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---

## 场景 11 — 首次加载 / 会话恢复后导航

**通过标准：** 新窗口首次导航或会话恢复后，首次 `BeforeNavigate` 路径正常；有效 tab 恢复后切换、再导航无异常。

**自动化参考：** `SessionRestoreReadPathTests`、`TabRestoreIsolationTests`、`NavigationEntryPointTests`

### 步骤

1. [ ] 选项 → **Window** → **Restore tabs on last closed window: On** → Apply
2. [ ] 打开 **2 个有效 tab**（如 `C:\Windows`、`C:\Users`），关闭 **整个 Explorer 窗口**
3. [ ] 重新打开 Explorer，等待 tab **恢复完成**
4. [ ] 在恢复的 tab 之间 **各点击切换 1 次**
5. [ ] 在任一 tab 导航到 **新路径**（如 `C:\Program Files` 或 `%TEMP%`）
6. [ ] **（可选）** 再开全新 Explorer 窗口，在默认 tab 导航到 `C:\Windows`（首次加载路径）

### 验证

- [ ] 恢复后 **2 个有效 tab 均存在**，路径正确
- [ ] 切换 tab 时 Explorer 内容 **与 tab 标题一致**
- [ ] 步骤 5 导航 **成功**，无反复失败弹窗
- [ ] 无「OptionsDialog 式」XAML/COM 异常；Event Viewer 无未处理 .NET 异常（可选）
- [ ] 步骤 6（若做）首次导航 **正常**，TabCount 合理

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---

## 场景 12 — 锁定 tab + 分支 / SubDirTip 导航

**通过标准：** 锁定 tab 下，SubDirTip 或分支菜单导航走 **克隆/锁定策略**，不错误覆盖当前 tab 或丢失锁定状态。

**自动化参考：** `CurrentTab_SelectedTab_InvariantTests`（Wave 17）、SubDirTip / `MenuOperationsController` 相关测试

### 步骤

1. [ ] 打开 Explorer，导航到 **含子文件夹** 的路径（如 `C:\Windows`）
2. [ ] **右键当前 tab** → 选择 **锁定此标签**（或等价菜单项）
3. [ ] 确认 tab 显示锁定状态（图标/菜单文案变化）
4. [ ] 触发 **SubDirTip**（鼠标悬停 tab 上子目录区域，或项目配置的 SubDirTip 手势）→ 在弹出菜单中 **点击子文件夹**
5. [ ] 观察：应 **新开 tab 或克隆**，原锁定 tab 路径 **不变**
6. [ ] **（补充）** 右键 **另一未锁定 tab**（若无则先新建 tab），重复步骤 4，点击子文件夹 — 应在 **该 tab 或上下文 tab** 内导航（与锁定行为不同）

### 验证

- [ ] 步骤 4 后，**原锁定 tab 仍指向步骤 1 路径**（未被覆盖）
- [ ] 新导航发生在 **新 tab 或预期目标 tab**，无 Explorer 崩溃
- [ ] 步骤 6 非锁定 tab 行为与锁定 tab **有区别**（非静默相同）
- [ ] `ContextMenuedTab` 相关菜单（右键 tab 菜单项）作用在 **右键的目标 tab** 上

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---

## Wave 20 导航专项汇总

| 场景 | 标题 | 签收 |
|------|------|------|
| 10 | 后退/前进 TravelLog | ☐ |
| 11 | 首次加载/会话恢复导航 | ☐ |
| 12 | 锁定 tab + SubDirTip/分支 | ☐ |

全部 Pass 后，在 `progress.md` 写入：`Wave20 导航 10–12 signed YYYY-MM-DD`。
