---
kind: configuration_system
name: QTTabBar 注册表驱动的配置系统
category: configuration_system
scope:
    - '**'
source_files:
    - QTTabBar/Config.cs
    - QTTabBar/Constants.cs
    - QTTabBar/MiscClasses.cs
---

## 配置系统概述

QTTabBar Rebirth 采用**基于 Windows 注册表的强类型配置系统**，通过反射机制将 C# 类属性与注册表值进行双向映射。所有用户配置存储在 `HKEY_CURRENT_USER\Software\QTTabBar\` 下，支持运行时热重载和跨进程广播更新。

## 核心架构

### 配置模型设计
- **根配置类**: `Config` 作为配置树根节点，包含12个配置类别子对象（Window、Tabs、Tweaks、Tips、Misc、Skin、BBar、Mouse、Keys、Plugin、Lang、Desktop、Security）
- **分类管理**: 每个配置类别对应一个 `_Xxx` 内部类，使用 `[Serializable]` 标记支持 JSON 序列化
- **默认值策略**: 每个配置类构造函数中定义默认值，支持按操作系统版本动态调整默认行为

### 持久化机制
- **存储位置**: `HKEY_CURRENT_USER\Software\QTTabBar\Config\[Category]\[Property]`
- **序列化格式**: 
  - 基础类型（bool/int/string/enum）直接存储为注册表值
  - 复杂类型（Font、Color、Padding等）使用 `DataContractJsonSerializer` 序列化为 UTF8 字符串
  - 特殊类型（Font）通过 `XmlSerializableFont` 包装器处理
- **读写路径常量**: 集中在 `Constants.cs` 的 `RegConst` 类中统一管理

### 加载与初始化流程
1. **启动阶段**: `ConfigManager.Initialize()` 创建空配置对象并调用 `ReadConfig()`
2. **反射扫描**: 遍历 `Config` 类的所有可写属性，获取对应的配置类别
3. **属性映射**: 对每个配置类别的属性进行反射读取，根据类型执行相应的反序列化逻辑
4. **验证修复**: 加载后执行范围验证、路径有效性检查、数组边界修正等数据修复操作
5. **应用生效**: `UpdateConfig()` 方法广播配置变更到所有实例

## 关键特性

### 类型安全访问
```csharp
// 静态快捷方式访问
Config.Window.CaptureNewWindows = true;
Config.Tabs.NewTabPosition = TabPos.Rightmost;
```

### 向后兼容层
- 提供 `Scts` 枚举的遗留接口适配
- 自动处理旧版配置项的迁移和降级
- 支持不同 Windows 版本的差异化配置

### 配置导出功能
- 内置 `RegFileWriter` 工具类，可将注册表配置导出为标准 `.reg` 文件
- 支持选择性导出（如仅导出皮肤配置）
- 避免 UAC 弹窗，纯 .NET 实现注册表操作

## 开发者规范

### 新增配置项
1. 在对应 `_Xxx` 类中添加 `[Serializable]` 属性
2. 在构造函数中设置合理的默认值
3. 如需特殊序列化，考虑使用自定义包装类
4. 在 `ReadConfig()` 中添加必要的验证逻辑

### 配置验证规则
- 数值范围：使用 `QTUtility.ValidateMinMax()` 进行边界检查
- 路径有效性：通过 `IDLWrapper.Available` 验证文件系统路径
- 数组完整性：确保快捷键数组长度与 `BindAction` 枚举匹配
- 资源清理：移除引用已卸载插件的配置项

### 线程安全考虑
- `LoadedConfig` 使用 `volatile` 关键字保证可见性
- 配置更新通过 IPC 命令广播到所有 Explorer 实例
- 读多写少场景下的并发访问控制

## 相关文件
- `QTTabBar/Config.cs` - 配置模型定义和序列化逻辑
- `QTTabBar/Constants.cs` - 注册表路径常量定义
- `QTTabBar/MiscClasses.cs` - 注册表导出工具类
- `QTTabBar/OptionsDialog/` - WPF 选项对话框界面
- `I18N/` - 多语言配置文件集