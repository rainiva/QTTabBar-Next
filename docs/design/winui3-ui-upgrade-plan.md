# QTTabBar WinUI 3 风格 UI 升级改造 — 设计说明

> **版本:** 0.1 (草案)  
> **日期:** 2026-07-08  
> **路线:** 混合方案 (D+A) — WPF Fluent 选项对话框 + WinForms/GDI Band Win11 视觉  
> **阶段 1 范围:** 全部 14 页 Options 一次性升级（在阶段 0 设计令牌就绪后）

---

## 1. 目标与非目标

### 1.1 目标

- 整体视觉对齐 **Windows 11 / WinUI 3 Fluent Design**（圆角、层次、Accent、浅色/深色）
- **选项对话框** 达到现代桌面应用水准（NavigationView、Card、统一间距）
- **Explorer 内标签栏/工具栏** 在 Win11 上默认呈现 Fluent 风格，同时 **保留** 自定义皮肤与插件 API
- 深色模式与系统 `AppsUseLightTheme` 一致（扩展现有 `SwitchNighMode`）

### 1.2 非目标（本计划不包含）

- 将 COM Band 重写为 WinUI 3 / .NET 8 in-process 宿主
- 在 Rebar 内嵌 XAML Islands
- 破坏 `Config._Skin` 注册表结构与 `.reg` 皮肤导入/导出格式
- 修改插件 WinForms `Control` ABI

---

## 2. 架构约束（必读）

```
┌─────────────────────────────────────────────────────────────┐
│  Explorer.exe (in-process COM)                               │
│  ┌─────────────────────────────────────────────────────┐    │
│  │ BandObject → QTTabBarClass / QTButtonBar / …         │    │
│  │   WinForms UserControl + QTabControl (GDI+ 自绘)      │    │
│  │   ❌ 不能使用 WinUI 3 HWND                             │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              │ IPC (IpcCommandMessage)
                              ▼
┌─────────────────────────────────────────────────────────────┐
│  Server 进程 STA 线程                                        │
│  OptionsDialog (WPF) ✅ 可 Fluent 化                        │
└─────────────────────────────────────────────────────────────┘
```

| 表面 | 技术 | 升级方式 |
|------|------|----------|
| 标签栏 | `QTabControl.cs` | GDI+ Fluent 绘制路径 |
| Rebar 背景 | `RebarController.cs` | DWM + `ShellColors` |
| 工具栏 | `QTButtonBar.cs` | 扁平按钮 + 令牌色 |
| 选项对话框 | `OptionsDialog/*.xaml` | **WPF Fluent 库** |
| 浮层菜单 | `DropDownMenuBase.cs` 等 | 第二阶段视觉 |

---

## 3. 设计系统（阶段 0）

### 3.1 Fluent 令牌（新增 `FluentTheme.cs` 或扩展 `ShellColors.cs`）

| 令牌 | Light | Dark | 用途 |
|------|-------|------|------|
| `BackgroundBase` | `#F3F3F3` | `#202020` | 窗口/Rebar 底 |
| `BackgroundLayer` | `#FFFFFF` | `#2D2D2D` | Card、选项页 |
| `StrokeDefault` | `#E5E5E5` | `#3D3D3D` | 分隔线 |
| `TextPrimary` | `#1A1A1A` | `#FFFFFF` | 主文本 |
| `TextSecondary` | `#605E5C` | `#C8C6C4` | 说明文字 |
| `Accent` | 系统 Accent | 系统 Accent | 选中 Tab、焦点环 |
| `CornerRadiusSm` | 4px | 4px | Checkbox 区域 |
| `CornerRadiusMd` | 8px | 8px | Card、对话框 |
| `CornerRadiusTab` | 6px (top) | 6px | 活动标签（Win11） |

- **Accent:** 读取 `SystemParameters.WindowGlassBrush` / Registry `AccentColor`（与 Win11 一致）
- **深色:** 复用 `QTUtility.InNightMode` + `Config.Skin.SwitchNighMode`

### 3.2 WPF 资源结构（新建）

```
OptionsDialog/
  Themes/
    FluentTheme.Light.xaml      ← 颜色、字体、间距 StaticResource
    FluentTheme.Dark.xaml
    FluentControls.xaml         ← 统一样式：SectionCard, FluentCheckBox, FluentButton
  OptionsDialogResources.xaml   ← 合并 Fluent 字典，逐步废弃 #FFF0F2F5 硬编码
```

### 3.3 WinForms Band 令牌消费

- `QTabControl.DrawTab` 读取 `FluentThemeTokens`（静态，随 `SwitchNighMode` 更新）
- Win11 且 `!Config.Skin.UseTabSkin` 时走 **FluentTabRenderer** 新路径
- Win7/8/10 或 `UseTabSkin=true` 时保留现有 VisualStyle / 位图皮肤

---

## 4. 技术选型：WPF Fluent 库

| 库 | 优点 | 缺点 | 建议 |
|----|------|------|------|
| **[WPF-UI](https://github.com/lepoco/wpfui)** | Win11 控件多、NavigationView、Mica | .NET Framework 4.6.2+ 需验证 | **首选** |
| **ModernWpf** | 轻量、Fluent 主题 | Navigation 需自建 | 备选 |
| **MahApps.Metro** | 成熟 | 偏 Metro 非 Win11 | 不推荐 |

**决策:** 采用 **WPF-UI 3.x**（NuGet `WPF-UI`），目标 `net48` + `PresentationFramework`。

**窗口壳:**

- `ui:FluentWindow` 或标准 `Window` + `ui:ThemesDictionary`
- 左侧 `NavigationView`（PaneDisplayMode=Left）替代现有 `ListBox` 分类列表
- 内容区 `Frame` / 直接 `ContentPresenter` 切换 14 个 `OptionsDialogTab`
- 底栏：Reset / OK / Cancel / Apply（`ui:Button` Default/Secondary）

---

## 5. 选项对话框信息架构（14 页）

现有注册顺序（`OptionsDialog.xaml.cs`）保持不变，仅改视觉与布局：

| # | 页面 | Navigation 图标建议 | 布局模式 |
|---|------|-------------------|----------|
| 01 | Window | Window | Card 分组 |
| 02 | Tabs | Tab | Card + 两列 Grid |
| 03 | Tweaks | Settings | 长列表 CheckBox |
| 04 | Tooltips | Chat | Card |
| 05 | General | Home | Card + 操作按钮 |
| 06 | Appearance | Color | **重点页** — 颜色预览 + 皮肤导入 |
| 07 | Mouse | Cursor | 列表 + 交互区 |
| 08 | Keys | Keyboard | ListView 热键 |
| 09 | Groups | Folder | TreeListView（保留） |
| 10 | Apps | App | TreeListView（保留） |
| 11 | Button Bar | Toolbar | 双列表 |
| 12 | Plugins | Puzzle | ListView |
| 13 | Language | Globe | Combo + 文件路径 |
| 14 | About | Info | 居中信息 |

### 5.1 统一页面模板 `OptionsPageTemplate.xaml`

```xml
<!-- 伪结构 -->
<StackPanel Margin="24,16">
  <TextBlock Style="{StaticResource PageTitle}" />
  <TextBlock Style="{StaticResource PageDescription}" />
  <ui:Card Margin="0,12,0,0">
    <!-- 页面具体内容 -->
  </ui:Card>
</StackPanel>
```

- **SectionHeaderStyle** → 改为 Card 内 `ui:TextBlock` Subtitle 或 Expander Header
- **CheckStyle / IndentedCheckStyle** → `ui:ToggleSwitch` 或 Fluent CheckBox（保留 Label 关联）
- **PageHeaderImage** 32px 图标 → NavigationView Item 图标 + 可选页面 Hero（可逐步移除重复图标）

### 5.2 自定义控件迁移

| 控件 | 文件 | Fluent 策略 |
|------|------|-------------|
| Spinner | `Spinner.xaml` | `ui:NumberBox` 或保留自绘 + Fluent 边框 |
| MarginCombo | `MarginCombo.xaml` | 统一 `ComboBox` 样式 |
| EditableHeader | `EditableHeader.xaml` | `ui:TextBox` + 验证 |
| FileFolderEntryBox | `FileFolderEntryBox.xaml` | Path 行：TextBox + SymbolIcon 浏览按钮 |

### 5.3 仍使用 WinForms 的对话框（阶段 1 可保留）

- `ColorDialog` / `FontDialog` — 阶段 2 可换 WPF `ColorPicker` 或自定义 Fluent 弹窗
- `OpenFileDialog` — 阶段 2 换 `Microsoft.Win32` + 样式或 `CommonOpenFileDialog`

**绑定与逻辑:** 所有 `DataContext` / `CommitConfig` / `InitializeConfig` **不变**，仅 XAML 与样式变更。

---

## 6. Band 视觉升级（阶段 2，设计预研）

### 6.1 标签 (`QTabControl.cs`)

**Win11 Fluent 路径（`DrawTabFluent` 新方法）:**

```
┌──────────────────────────┐
│  🗁  Documents        ×   │  ← 圆角顶边，底边 2px Accent（活动）
└──────────────────────────┘
```

- 非活动：透明/浅灰底，`StrokeDefault` 底边
- Hover：`BackgroundLayer` 略亮
- 活动：Accent 下划线或浅 Accent 背景
- 关闭按钮：Fluent 圆形 Hover（已有逻辑，改绘制）
- **不改动:** 多行、垂直栏、拖拽、锁定图标、重叠像素

**特性开关:** `Config.Skin.UseFluentTabs`（默认 Win11+ 为 true，可关回经典）

### 6.2 Rebar (`RebarController.cs`)

- Win11：`WM_ERASEBKGND` 使用 `BackgroundBase`，可选 DWM 模糊（若稳定）
- 与 Explorer 深色标题栏协调（已有 `ShellColors`）

### 6.3 工具栏 (`QTButtonBar.cs`)

- 按钮 HotTrack：圆形 40px 逻辑区域，Alpha 混合 Hover
- 图标：优先 Segoe Fluent Icons（需评估 OS 版本回退）

---

## 7. 实施阶段与里程碑

| 阶段 | 内容 | 交付物 | 预估 |
|------|------|--------|------|
| **0** | 设计令牌 + WPF-UI PoC | `FluentTheme.*.xaml`、单页 Demo | 1 周 |
| **1a** | Options 外壳 NavigationView | `OptionsDialog.xaml` 重构 | 1 周 |
| **1b** | 14 页全部迁移 | 所有 `Options*.xaml` | 2–3 周 |
| **1c** | 自定义控件 + 深色切换 | Spinner 等 + Theme 切换 | 1 周 |
| **2** | Band Fluent 绘制 | `QTabControl` / Rebar / ButtonBar | 3–4 周 |
| **3** | 浮层 + 插件窗（可选） | TabSwitchForm 等 | 2 周 |

**阶段 1 完成标准:**

- [ ] 14 页功能与升级前一致（绑定、Reset、Apply、热键捕获）
- [ ] 浅色/深色随系统切换
- [ ] 1280×720 与 1920×1080 下无布局截断
- [ ] MSBuild Debug 构建通过，现有单元测试仍 153/153
- [ ] 中/英/日资源字符串仍通过 `{qt:Resx}` 加载

---

## 8. 风险与缓解

| 风险 | 缓解 |
|------|------|
| WPF-UI 与 net48 兼容性 | 阶段 0 PoC；不通过则换 ModernWpf |
| 14 页一次改回归面大 | 每页快照对比 + Options 手动测试清单 |
| Fluent Tab 与旧皮肤冲突 | `UseFluentTabs` 与 `UseTabSkin` 互斥文档化 |
| DPI 缩放 | 沿用现有 `SetProcessDPIAware`；WPF-UI PerMonitorV2 评估 |
| 插件期望 Win7 Aero 外观 | 保留经典路径，Win11 默认 Fluent |

---

## 9. 测试计划

### 9.1 Options 手动清单

- 每页：打开、修改一项、Apply、重启 Explorer 验证生效
- Appearance：导入/导出皮肤 `.reg`
- Keys/Groups/Apps：热键与树操作
- Language：切换语言文件后 UI 刷新

### 9.2 Band（阶段 2）

- Win10 / Win11 浅色深色
- 多行标签、垂直 SecondViewBar
- 自定义 TabImage 皮肤仍可用

---

## 10. 后续可选：独立 WinUI 3 设置 App

若 WPF Fluent 仍不满足「真 WinUI 3」：

- 新建 `QTTabBar.Settings`（WinUI 3 + .NET 8）
- 复用 `IpcCommandMessage` 打开/同步 Config
- WPF Options 保留一个版本作 fallback

**不在本计划第一阶段实施。**

---

## 11. 待确认项（实施前签字）

1. WPF-UI vs ModernWpf 最终选择（PoC 后）
2. Options 窗口是否启用 Mica/Acrylic（需 Win10 1809+）
3. 活动标签：Accent 下划线 vs 全填充（Appearance 预览可配置）
4. 是否新增 `UseFluentTabs` 用户选项或 Win11 自动启用

---

## 12. 附录：关键文件索引

| 用途 | 路径 |
|------|------|
| Options 主窗 | `QTTabBar/OptionsDialog/OptionsDialog.xaml` |
| 共享样式 | `QTTabBar/OptionsDialog/OptionsDialogResources.xaml` |
| 外观配置 | `QTTabBar/OptionsDialog/Options06_Appearance.xaml` |
| 皮肤模型 | `QTTabBar/Config.cs` → `_Skin` |
| 标签绘制 | `QTTabBar/QTabControl.cs` |
| Rebar | `QTTabBar/RebarController.cs` |
| 系统色 | `QTTabBar/ShellColors.cs` |
| IPC 打开选项 | `QTTabBar/InstanceManager.cs` → `ExecuteOnServerProcessOpenOptions` |
