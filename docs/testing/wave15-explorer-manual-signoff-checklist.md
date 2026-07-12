# Wave 15 Explorer 手动签收 Checklist

对应 `progress.md` 中 **Wave 6–8 Explorer 真实用户操作矩阵（Wave 15 签收）** 7 项场景。  
自动化 guard 已全绿（1075 passed）；本清单用于 **W15-A2：7/7 人工 signed**。

---

## 测试前准备（全部场景共用）

- [ ] **P1** 已用当前分支 **Debug 或 Release** 构建 `QTTabBar.dll`（与日常安装方式一致）
- [ ] **P2** COM 已注册 / 已部署到 Explorer 实际加载路径（重启 Explorer 或注销后重登）
- [ ] **P3** 打开 **全新 Explorer 窗口** 作为基准（记录初始 TabCount）
- [ ] **P4** 准备路径素材：
  - 有效：`C:\Windows` 或任意存在的用户目录
  - 无效：`C:\QTTabBar_Wave15_NotExist`（确认不存在）
  - 可选：先打开某文件夹 tab，**删除该文件夹**，用于「曾有效、现已无效」恢复场景
- [ ] **P5** 选项 → **Tabs**：确认 **Never open same folder twice**（`NeverOpenSame`）可开关（场景 2 需要）
- [ ] **P6** 选项 → **Window**：确认 **Restore tabs on last closed window** 可设为 On（场景 1 需要）

**记录列（每项场景填写）：**


| 字段          | 示例                   |
| ----------- | -------------------- |
| 测试日期        | 2026-07-12           |
| 构建配置        | Debug / Release      |
| Explorer 版本 | Win11 24H2 / Win10 … |
| 结果          | Pass / Fail          |
| 备注          | 异常现象、截图路径            |


---



## 场景 1 — 会话恢复含无效路径

**通过标准：** 无效项跳过，Explorer 不崩溃，有效标签恢复。

**自动化参考：** `TabRestoreIsolationTests`、`SessionRestoreReadPathTests`、`CanonicalTabCreationTests`

### 步骤

1. [ ] 选项 → **Window** → 选择 **Restore tabs on last closed window: On** → Apply
2. [ ] 在同一 Explorer 窗口打开 **至少 2 个有效 tab**（如 `C:\Windows`、`C:\Users`）
3. [ ] 再打开 **1 个无效路径 tab**（若 UI 无法直接导航到无效路径，可跳过此 tab，改用步骤 4 的「删除文件夹」方式制造无效项）
4. [ ] **（推荐）** 打开 tab 指向 `C:\QTTabBar_Wave15_TestFolder` → 在资源管理器外删除该文件夹 → 该 tab 变为无效路径
5. [ ] **关闭整个 Explorer 窗口**（非仅关 tab）
6. [ ] 重新打开 Explorer（或重启 `explorer.exe`）



### 验证

- [ ] Explorer **未崩溃**、无持续卡死
- [ ] **有效 tab 被恢复**（步骤 2 的路径仍在）
- [ ] **无效 tab 未恢复**或打开时被跳过（TabCount 不含无效项；无反复错误弹窗）
- [ ] 恢复后切换 tab、前进/后退正常

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 2 — 启动组 + NeverOpenSame

**通过标准：** 不重复标签，无效路径跳过。

**自动化参考：** `TabCreationBypassGuardTests`、`TabValidationEquivalenceTests`

### 步骤

1. [ ] 选项 → **Tabs** → 勾选 **Never open same folder twice** → Apply
2. [ ] 选项 → **Groups** → 新建或编辑一组：
  - [ ] 勾选 **Startup**（启动时打开）
     [ ] 路径列表包含：**1 个有效路径** + **1 个无效路径**（如 `C:\QTTabBar_Wave15_NotExist`）
     [ ] 列表中 **同一有效路径不要重复出现两次**
3. [ ] Apply → 关闭所有 Explorer 窗口
4. [ ] 打开 **新的 Explorer 窗口**（或重启 Explorer），触发启动组



### 验证

- [ ] 有效路径 **至多打开 1 个 tab**（无重复 tab 指向同一路径）
- [ ] 无效路径 **未产生新 tab**
- [ ] 若当前窗口已有与启动组相同路径的 tab，**NeverOpenSame** 下不应再开第二个
- [ ] 无异常弹窗或 TabCount 异常暴增

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 3 — 插件 vs UI 无效目标

**通过标准：** 插件与 UI 两种入口对无效目标均 **不增加 TabCount**。

**自动化参考：** `CanonicalTabCreationTests`、`TabValidationEquivalenceTests`

### 步骤 A — UI 入口

1. [ ] 记录当前 **TabCount = N**
2. [ ] 通过 UI 尝试打开无效路径（任选一种）：
  - [ ] 标签栏 **「+」/ 新建** 选无效文件夹
     [ ] **拖放** 无效路径到标签栏
     [ ] 分组/菜单打开无效路径（若有）
3. [ ] 确认 TabCount **仍为 N**（或仅 +0）



### 步骤 B — 插件入口（若已安装可用插件）

1. [ ] 记录 TabCount = N'
2. [ ] 通过 **已启用插件** 触发「打开文件夹/路径」到 **无效路径**
  （无插件时可改用：**IPC/跨实例** 见场景 5 的无效路径分支，或标注 N/A 并在备注说明）
3. [ ] 确认 TabCount **仍为 N'**



### 验证

- [ ] UI 与插件（或 IPC）两条路径均 **拒绝无效目标**
- [ ] 无静默创建空 tab、无 `???` 路径 tab 增加

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 4 — 设置 Apply + Window 并发

**通过标准：** Wave 2 局部 Window 提交行为保持；Apply 不破坏其他配置、不引发多窗口竞态异常。

**自动化参考：** `ConfigPartialCommitTests`

### 步骤

1. [ ] **同时打开 ≥2 个 Explorer 窗口**（均带 QTTabBar）
2. [ ] 打开 **选项**，在 **Window** 页修改 **仅 Window 域** 设置（例如与窗口/恢复相关的任一项），**不要改 Tabs/Appearance 等其他页**
3. [ ] 点击 **Apply**（不要仅 OK 关闭），保持选项对话框打开或关闭均可
4. [ ] 在两个 Explorer 窗口间切换，各执行：切换 tab、打开新 tab、关闭 tab



### 验证

- [ ] Apply **成功**，无异常对话框
- [ ] **两个窗口** 均仍正常显示标签栏，行为一致
- [ ] 未修改的配置项（如 Tabs 页设置）**未被意外改变**（可打开选项核对）
- [ ] 无「配置回滚」「部分窗口未刷新」等 Wave 2 已知回归

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 5 — 跨进程 IPC 开标签

**通过标准：** 有效路径 **开一个** tab；无效路径 **拒绝**。

**自动化参考：** `CanonicalTabCreationTests`、`WindowCaptureSessionTests`

### 步骤

1. [ ] **主窗口** Explorer 已打开，记录 TabCount = N
2. [ ] **有效路径 IPC**（任选一种）：
  - [ ] 从 **第二个 Explorer 实例** / Desktop Tool / 带「在 QTTabBar 中打开」的入口，对 **有效路径** 调用打开（内部走 `InvokeMainOpenNewTabOrWindowFromPath` 或等价 IPC）
     [ ] 或使用项目内已知的 IPC 入口（如从另一组件「打开路径到主窗口 tab」）
3. [ ] 确认 TabCount **= N + 1**，新 tab 路径正确
4. [ ] **无效路径 IPC**：对 `C:\QTTabBar_Wave15_NotExist` 重复步骤 2
5. [ ] 确认 TabCount **不变**



### 验证

- [ ] 有效 IPC：**恰好新增 1 个 tab**
- [ ] 无效 IPC：**TabCount 不变**，无崩溃
- [ ] 主窗口当前 tab 未被意外替换（除非产品设计即如此）

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 6 — 菜单 / Shell（Context 迁移后）

**通过标准：** Wave 14 `ContextMenuedTab` 迁移后，标签菜单、Shell 命令、绑定动作 **无回归**。

**自动化参考：** `MenuContextInjectionTests`、`CompositionContextBoundaryTests`、`MenuOperationsExtractionTests`

### 步骤 — 标签右键菜单

1. [ ] 在非当前 tab 上 **右键** → 打开上下文菜单
2. [ ] 执行：**关闭此标签**、**复制/克隆**（若有）、**打开命令行**（OpenCmd）
3. [ ] 在当前 tab 上右键，重复 1–2



### 步骤 — 绑定动作（若已配置）

1. [ ] 触发 **Show tab menu** / **OpenCmd** 绑定（选项 → Mouse/Keys 中查看绑定）
2. [ ] 确认菜单弹出位置正确，OpenCmd 打开的路径对应当前/右键 tab



### 步骤 — 插件菜单（可选）

1. [ ] 若插件菜单项依赖当前 tab，右键某 tab 后点击插件项，应作用于 **右键 tab** 而非错误 tab



### 验证

- [ ] 右键菜单项作用对象 = **右键的那枚 tab**（非总是当前 tab）
- [ ] **OpenCmd** 路径正确，无空引用异常
- [ ] **MenuOperations** 相关项（关闭/锁定/分组/分支菜单）正常
- [ ] 无「菜单空白」「点击无反应」等 Wave 12–14 回归

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 场景 7 — SecondView 显示 / 隐藏 / 关闭

**通过标准：** 第二视图（`QTSecondViewBar` / InfoBand 垂直标签栏）显示与关闭后 **无 hook 残留**；Explorer 可正常重启。

**自动化参考：** `SecondViewBoundaryTests`

### 步骤

1. [ ] 在 Explorer **视图** 菜单中启用 **QTTabBar 第二视图 / InfoBand（垂直标签栏）**（名称以实际菜单为准）
2. [ ] 确认垂直标签栏 **显示**，可切换 tab、导航
3. [ ] **隐藏** 第二视图（取消视图菜单勾选）
4. [ ] 再次 **显示** → 确认仍正常
5. [ ] **关闭 Explorer 窗口** → 重新打开 Explorer
6. [ ] （可选）任务管理器结束 `explorer.exe` 后自动重启，观察是否卡死



### 验证

- [ ] 显示/隐藏 **无崩溃**
- [ ] 关闭后重开 Explorer **响应正常**（无全局键盘/鼠标异常、无 Explorer 假死）
- [ ] 主标签栏（水平 QTTabBar）与第二视图 **互不干扰**
- [ ] 事件查看器 / QTTabBar 日志无持续 hook 相关错误（若开启日志）

**签收：** ☐ Pass ☐ Fail — 签名/日期：__________

---



## 签收汇总

全部 Pass 后，更新 `progress.md` 矩阵「人工签收」列：

```markdown
| 1 | … | … | … | signed YYYY-MM-DD |
| 2 | … | … | … | signed YYYY-MM-DD |
…
| 7 | … | … | … | signed YYYY-MM-DD |
```

并可将 `structural-governance.md` §6 表格 F 行改为：**✓ 人工 7/7 signed（日期）**。

---



## 失败时记录模板

```
场景 #：
现象：
复现步骤：
TabCount 前/后：
Explorer 是否崩溃：
日志/截图：
是否阻塞 Wave 15 签收：是/否
```

---



## 快速对照 — 自动化 guard（无需重复测逻辑，仅测 UI 集成）


| #   | 场景         | 关键 guard fixture                |
| --- | ---------- | ------------------------------- |
| 1   | 会话恢复       | `SessionRestoreReadPathTests`   |
| 2   | 启动组        | `TabValidationEquivalenceTests` |
| 3   | 无效目标       | `CanonicalTabCreationTests`     |
| 4   | 配置 Apply   | `ConfigPartialCommitTests`      |
| 5   | IPC        | `WindowCaptureSessionTests`     |
| 6   | Context 菜单 | `MenuContextInjectionTests`     |
| 7   | SecondView | `SecondViewBoundaryTests`       |


