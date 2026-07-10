---
kind: frontend_style
name: WPF Fluent 主题与资源字典体系
category: frontend_style
scope:
    - '**'
source_files:
    - QTTabBar/FluentThemeManager.cs
    - QTTabBar/FluentThemeTokens.cs
    - QTTabBar/OptionsDialog/Themes/FluentControls.xaml
    - QTTabBar/OptionsDialog/Themes/FluentTheme.Light.xaml
    - QTTabBar/OptionsDialog/Themes/FluentTheme.Dark.xaml
    - QTTabBar/OptionsDialog/Options06_Appearance.xaml.cs
---

## 系统概述
QTTabBar Rebirth 的 WPF 选项对话框采用 **Fluent Design System** 风格，基于第三方库 **Wpf.Ui**（`ApplicationThemeManager`、`WindowBackdropType.Mica`）实现 Windows 11 原生外观。主题通过 XAML ResourceDictionary 分层管理，支持明/暗双主题切换，并与系统 DWM 强调色联动。

## 核心文件与包
- `QTTabBar/OptionsDialog/Themes/FluentControls.xaml` — 控件级样式（标题、复选框、导航列表项、按钮等）
- `QTTabBar/OptionsDialog/Themes/FluentTheme.Light.xaml` / `FluentTheme.Dark.xaml` — 明/暗主题设计令牌（颜色、圆角、字号、间距）
- `QTTabBar/FluentThemeManager.cs` — 主题应用入口，调用 `Wpf.Ui.Appearance.ApplicationThemeManager.Apply` 并动态注入主题字典
- `QTTabBar/FluentThemeTokens.cs` — C# 侧共享的设计令牌常量（背景层、描边、文本主次色、强调色、圆角半径），从注册表读取系统强调色
- `QTTabBar/OptionsDialog/Options06_Appearance.xaml.cs` — 外观设置页，演示皮肤颜色绑定与预览渲染
- `QTTabBar/packages.config` / `.csproj` — 引用 `Wpf.Ui` NuGet 包并在构建时强名签名

## 架构与约定
1. **令牌分层**：`FluentThemeTokens` 提供 C# 可读的 `Color` 常量；XAML 主题文件以 `Fluent.*` 前缀暴露 `Color`/`SolidColorBrush`/`Thickness`/`CornerRadius`/`Double` 资源键，供样式引用。
2. **运行时切换**：`FluentThemeManager.ApplyTo(root)` 先刷新系统深色模式与强调色，再调用 `ApplicationThemeManager.Apply(theme, WindowBackdropType.Mica, updateAccent: true)` 应用全局主题，最后扫描根元素的 `MergedDictionaries`，移除旧 `FluentTheme.*` 字典并按 `FluentControls.xaml` 位置插入新字典。
3. **样式命名空间**：所有自定义样式统一以 `Fluent` 前缀（如 `FluentPageTitle`、`FluentCheckBox`、`FluentNavListBoxItem`），避免与 Wpf.Ui 内置样式冲突。
4. **资源键规范**：颜色使用 `Fluent.Xxx.Color` + `Fluent.Xxx` 双层键，便于在 C# 与 XAML 间共享；尺寸类使用 `Fluent.CornerRadiusSm/Md/Tab` 与 `Fluent.PageMargin/CardPadding/SectionSpacing`。
5. **WinForms 兼容**：`FluentThemeTokens` 同时被 WinForms 侧（Band 绘制）消费，保证 Shell 扩展与 WPF 选项对话框视觉一致。

## 开发者应遵循的规则
- 新增颜色或尺寸必须先在 `FluentThemeTokens.cs` 中定义 C# 常量，再在两个主题 XAML 文件中同步声明同名资源键。
- 新建控件样式一律放在 `FluentControls.xaml`，并以 `Fluent` 前缀命名 Key，使用 `{DynamicResource Fluent.*}` 绑定令牌。
- 页面加载时通过 `FluentThemeManager.ApplyTo(this)` 初始化主题，不要直接硬编码 `Background`/`Foreground`。
- 若需覆盖默认行为，仅修改对应主题 XAML 中的资源值，保持明/暗两套文件对称。
- 对 WinForms 组件使用 `FluentThemeTokens.TextPrimary` 等属性获取当前主题色，禁止写死 ARGB 值。