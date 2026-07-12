# Wave 20 人工签收总表（10/10）

对应 `docs/superpowers/plans/2026-07-12-structural-remediation-wave16-root-cure.md` **Task 20.1**（Grill Q5=B）。

**组成：** 7 项核心 Explorer 场景 + 2 项部署 + 1 项导航专项（含场景 10–12 三项，**三项全 Pass 才签第 10 项**）。

**自动化前置：** Wave 16–19 guard 全绿；Wave 20 门禁见 `Wave20AcceptanceGuardTests`、`BandOrchestrationClusterBudgetTests`（Cluster **5986** ≤6800，root cure 测试已启用）。

---

## 签收总表

| # | 类别 | 场景 | 详细清单 | 人工签收 |
|---|------|------|----------|----------|
| 1 | 核心 | 会话恢复含无效路径 | [wave15 §场景 1](wave15-explorer-manual-signoff-checklist.md) | pending |
| 2 | 核心 | 启动组 + NeverOpenSame | [wave15 §场景 2](wave15-explorer-manual-signoff-checklist.md) | pending |
| 3 | 核心 | 插件 vs UI 无效目标 | [wave15 §场景 3](wave15-explorer-manual-signoff-checklist.md) | pending |
| 4 | 核心 | 设置 Apply + 局部 Window | [wave15 §场景 4](wave15-explorer-manual-signoff-checklist.md) | pending |
| 5 | 核心 | 跨进程 IPC 开标签 | [wave15 §场景 5](wave15-explorer-manual-signoff-checklist.md) | pending |
| 6 | 核心 | 菜单/Shell（Context 迁移后） | [wave15 §场景 6](wave15-explorer-manual-signoff-checklist.md) | pending |
| 7 | 核心 | SecondView 显示/隐藏/关闭 | [wave15 §场景 7](wave15-explorer-manual-signoff-checklist.md) | pending |
| 8 | 部署 | 新 MSI / 注册后 **Options 可打开** | 见下方 §部署 8 | pending |
| 9 | 部署 | GAC **1.0.0.0 → 1.5.6.7** 升级无 XAML 异常 | 见下方 §部署 9 | pending |
| 10 | 导航 | 后退/前进 + 会话恢复导航 + 锁定 tab 分支 | [wave20-navigation-signoff.md](wave20-navigation-signoff.md)（场景 10–12 **全部 Pass**） | pending |

**全部 Pass 后**，在 `progress.md` 写入：

```
Wave20 人工 10/10 signed YYYY-MM-DD
Wave20 导航 10–12 signed YYYY-MM-DD   （若与上同日可合并一行）
```

---

## 测试前准备（全部场景共用）

- [ ] **P1** Release（或与日常安装一致）构建 `QTTabBar.dll`
- [ ] **P2** 管理员 MSI 或 `Register/Register.bat Release` 完成 COM 注册
- [ ] **P3** 重启 Explorer（或注销重登）后打开 **全新窗口**
- [ ] **P4** 确认 TabBar 可见；记录 Explorer 版本与构建配置

---

## §部署 8 — 新 MSI / 注册后 Options 可打开

**通过标准：** 右键 TabBar 空白或系统托盘 → **选项/Options** 打开设置对话框，无 XAML/COM 崩溃。

### 步骤

1. [ ] 安装当前分支 **Release MSI**（或 regasm 注册后复制 BAML 完整程序集）
2. [ ] 重启 Explorer
3. [ ] 打开 Options（TabBar 右键菜单或托盘）
4. [ ] 切换 1–2 个选项页（General、Tabs、Window）

### 验证

- [ ] Options 窗口 **正常显示**（非空白、非立即崩溃）
- [ ] Event Viewer 无未处理 `XamlParseException` / `FileNotFoundException`（OptionsDialog BAML）

**签收：** ☐ Pass ☐ Fail — 日期：__________

---

## §部署 9 — GAC 1.0.0.0 → 1.5.6.7 升级

**通过标准：** 自旧 GAC 1.0.0.0 升级至当前构建（如 1.5.6.7）后，Explorer 加载新 Band，Options 与 TabBar 行为正常。

### 步骤

1. [ ] （若仍指向旧版）确认注册表/COM 曾指向 **1.0.0.0** 或记录当前已升级状态
2. [ ] 执行 **Wave 20 Release 部署**（MSI 或 Register.bat + 复制输出）
3. [ ] 重启 Explorer
4. [ ] 打开 Options + 新建/切换 tab 各 1 次

### 验证

- [ ] `gacutil -l QTTabBarLib`（或 MSI 安装日志）显示 **新版本** 程序集
- [ ] TabBar 与 Options **无**「缺少 BAML/资源」类异常
- [ ] 与场景 8 不重复失败

**签收：** ☐ Pass ☐ Fail — 日期：__________

---

## 导航专项（第 10 项）

详见 [wave20-navigation-signoff.md](wave20-navigation-signoff.md)。**场景 10、11、12 均 Pass** 后，将上表第 10 行改为 `signed YYYY-MM-DD`。

---

## 汇总

| 已签 | 待签 |
|------|------|
| 0 / 10 | 10 |

*Wave 20 manual sign-off master — 2026-07-12*
