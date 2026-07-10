---
kind: logging_system
name: QTTabBar 内置轻量日志系统
category: logging_system
scope:
    - '**'
source_files:
    - QTTabBar/Logger.cs
    - QTTabBar/QTUtility2.cs
    - QTTabBar/OptionsDialog/Options05_General.xaml.cs
    - QTTabBar/QTTabBarClass.cs
    - BandObjectLib/BandObject.cs
---

## 1. 使用的系统与框架
- 未引入第三方日志库（无 NLog、Serilog、log4net 等），采用自研的 `Logger` 静态类，基于 .NET `System.Diagnostics` 与原生文件 I/O。
- 通过 Win32 `AllocConsole` + `SetStdHandle` 将 `Console.Out` 重定向到调试控制台，用于开发期实时输出。
- 所有结构化字段以自定义键值对形式拼接到单行文本中，最终追加写入用户 AppData 目录下的日志文件。

## 2. 核心文件与包
- `QTTabBar/Logger.cs`：日志系统唯一实现，提供 `flog/log/err/log(level, msg, dic)`、`MakeErrorLog(Exception)`、`AllocDebugConsole()` 等方法。
- `QTTabBar/QTUtility2.cs`：对外暴露的 Facade，转发 `ENABLE_LOGGER`、`AllocDebugConsole`、`flog/log/err` 等调用，保持向后兼容。
- `QTTabBar/OptionsDialog/Options05_General.xaml.cs`：选项对话框“启用日志”复选框事件处理器，运行时切换 `QTUtility2.ENABLE_LOGGER`。
- `QTTabBar/QTTabBarClass.cs`：在初始化阶段从配置读取 `Config.Misc.EnableLog` 并设置 `QTUtility2.ENABLE_LOGGER`。
- `BandObjectLib/BandObject.cs`：独立的 `bandLog.log` 路径常量，作为 BandObject 宿主侧的备用日志落点。

## 3. 架构与约定
- **全局开关**：`Logger.ENABLE_LOGGER` 为布尔静态标志，默认关闭；仅在显式设为 true 时才执行栈跟踪与写盘，避免性能损耗。
- **结构化字段**：每条日志行包含以下固定字段（以 `\tKey:Value` 拼接）：
  - `[level]`：`flog` / `log` / `err` 等级别标签
  - `C:` 调用类名、`M:` 方法名（由 `StackTrace` 自动提取）
  - `P:` 进程 ID、`T:` 托管线程 ID
  - `cost:` 当前线程距上次同线程日志的毫秒间隔
  - 时间戳 + 原始消息
- **错误日志专用格式**：`MakeErrorLog(Exception)` 输出多行报告，包含 .NET 版本、OS 版本、位数、QT 版本、异常 Message/Source/StackTrace/TargetSite 及 InnerException 链。
- **并发安全**：写操作使用 `Mutex` 保护，文件以 `FileShare.ReadWrite` 打开，支持 Explorer 多个实例同时追加。
- **忽略过滤**：`IGNORES` 数组匹配消息片段时直接丢弃，避免高频噪音（如 `ReleaseComObject`）。
- **控制台输出**：`AllocDebugConsole` 仅分配一次，将 `Console.Out` 绑定到 ANSI 437 代码页的调试控制台，便于 VS 调试。
- **持久化位置**：统一写入 `%APPDATA%\QTTabBar\QTTabBarException.log`；BandObject 宿主另维护 `%APPDATA%\QTTabBar\bandLog.log`。

## 4. 开发者应遵循的规则
- **不要直接 new StreamWriter 写日志**：统一通过 `Logger.flog/log/err` 或 Facade `QTUtility2.*` 调用，确保字段一致与互斥锁生效。
- **开启日志需显式设置**：`QTUtility2.ENABLE_LOGGER = true;`（或通过选项界面勾选“启用日志”），生产构建默认关闭。
- **记录关键上下文**：调用 `log(level, msg, dic)` 时尽量传入 `className`/`methodName` 以外的业务字段，以便后续解析。
- **异常捕获后上报**：在顶层 catch 块调用 `Logger.MakeErrorLog(ex, optional)`，不要吞掉异常而不留痕。
- **避免高频调用**：由于每次 `log/flog/err` 都会构造 `StackTrace`，不要在热路径上频繁调用，必要时合并为单次批量记录。
- **调试控制台**：仅在本地调试时调用 `Logger.AllocDebugConsole()`，部署产物不应依赖控制台存在。