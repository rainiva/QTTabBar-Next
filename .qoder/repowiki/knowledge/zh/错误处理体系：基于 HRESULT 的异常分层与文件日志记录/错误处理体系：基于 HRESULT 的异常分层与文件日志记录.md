---
kind: error_handling
name: 错误处理体系：基于 HRESULT 的异常分层与文件日志记录
category: error_handling
scope:
    - '**'
source_files:
    - QTTabBar/Common/CoreErrorHelper.cs
    - QTTabBar/Common/ShellException.cs
    - QTTabBar/Common/PropertySystemException.cs
    - QTTabBar/ExplorerBrowser/CommonControlException.cs
    - QTTabBar/Logger.cs
    - QTTabBar/QTUtility2.cs
    - QTTabBar/Common/ShellObject.cs
    - QTTabBar/Common/ShellProperty.cs
---

## 1. 采用的系统/方法
- **HRESULT + 自定义异常**：核心库通过 `CoreErrorHelper.HResult` 枚举统一表示 COM/Win32 返回码，并在失败时抛出领域特定的异常（`ShellException`、`PropertySystemException`、`CommonControlException`），而非直接向上抛 `COMException`。
- **按域分层异常类型**：
  - `ShellException`：封装 ShellObject / ShellFolder / ShellLibrary 等 Shell 操作错误；
  - `PropertySystemException`：封装属性系统写入/读取错误；
  - `CommonControlException`：封装 ExplorerBrowser 控件相关错误。
- **全局日志子系统**：`Logger` 类提供线程安全的文件日志（`QTTabBarException.log`），支持 `log`/`err`/`flog` 级别输出以及 `MakeErrorLog(Exception)` 全量堆栈转储，并通过 `QTUtility2` 暴露静态门面供上层调用。
- **C++ 注入层无托管异常**：MinHook/QTHookLib 使用 Win32 错误码和返回值判断，不抛出 .NET 异常，由 C# 侧包装为 HResult 后再抛出自定义异常。

## 2. 关键文件与包
- `QTTabBar/Common/CoreErrorHelper.cs` — HResult 常量与 `Failed/Succeeded/Matches/HResultFromWin32` 工具方法
- `QTTabBar/Common/ShellException.cs` — Shell 操作异常基类
- `QTTabBar/Common/PropertySystemException.cs` — 属性系统异常
- `QTTabBar/ExplorerBrowser/CommonControlException.cs` — ExplorerBrowser 控件异常
- `QTTabBar/Logger.cs` — 线程安全文件日志与异常转储
- `QTTabBar/QTUtility2.cs` — 对外暴露的日志门面（`Logger.log`/`Logger.err`/`Logger.MakeErrorLog`）
- `QTTabBar/Common/ShellObject.cs`、`ShellProperty.cs`、`ShellLibrary.cs`、`KnownFolderHelper.cs` 等 — 大量将 HRESULT 转换为 `ShellException` 的调用点

## 3. 架构与约定
- **错误传播路径**：底层 P/Invoke → `CoreErrorHelper.Succeeded/Failed` 判定 → 构造 `HResult` → 抛出对应领域异常 → 上层根据业务语义 catch 并恢复或上报。
- **异常构造模式**：多数 API 采用“先检查 hr，再 throw new ShellException(hr)”的两段式写法，保证异常携带原始 HResult，便于诊断。
- **日志策略**：默认关闭（`ENABLE_LOGGER = false`），调试时可开启；所有日志落盘到 `%APPDATA%\QTTabBar\QTTabBarException.log`，内部用 Mutex 保护并发写入。`MakeErrorLog` 会附带 .NET/OS/进程位宽/版本信息，适合崩溃后收集。
- **忽略列表**：`IGNORES` 数组可屏蔽高频噪声（如 `ReleaseComObject`），避免日志膨胀。
- **无全局 try/catch 中间件**：代码中未发现统一的异常拦截器或全局未处理异常钩子；异常主要在各功能模块内就地捕获并降级。

## 4. 开发者应遵循的规则
1. **不要直接抛 `COMException`/`Exception`**：涉及 Shell/属性系统的 API 应通过 `CoreErrorHelper` 检查 HResult，并抛出 `ShellException`/`PropertySystemException`。
2. **保留原始 HResult**：构造异常时使用 `(int)hr` 或 `new ShellException(hr)`，以便外部能区分具体失败原因。
3. **在边界处记录日志**：对不可恢复的错误调用 `Logger.MakeErrorLog(ex, optional)`；对可预期分支使用 `Logger.err/log` 记录上下文。
4. **避免吞掉异常**：catch 块应至少记录日志或返回明确的失败状态，不应静默忽略。
5. **C++ 注入层只返回错误码**：MinHook/QTHookLib 中的错误一律以返回值表达，禁止在托管与非托管边界之间跨异常边界传递。
6. **面向插件的稳定性**：插件加载路径（`PluginAttribute`、`AutoLoader`）已显式捕获 `MissingMethodException`/`COMException` 以保证宿主健壮性，插件作者也应遵循同样的容错策略。