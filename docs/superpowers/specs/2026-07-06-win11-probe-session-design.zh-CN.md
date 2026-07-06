# Win11 Probe 会话诊断设计

日期：2026-07-06
状态：草案
范围：仅限诊断方案设计

## 摘要

新增一个面向 Win11 验证的单入口诊断会话，把分散的 `Win11Probe` 标记收束成一份可读结论。第一版交付物是一个 PowerShell 脚本 `Tools/Win11ProbeSession.ps1`：它引导一次复现会话，只读取 `%APPDATA%\QTTabBar\QTTabBarException.log` 中本轮新增的 managed probe 行，并输出一份简短的分阶段诊断摘要。native `OutputDebugString` 证据是可选输入，用来在可用时增强报告，但不是第一版可用性的前提。

长期形态如下：

1. 一套稳定的 parser 和诊断核心。
2. 一个复用该核心的 PowerShell 入口，用于终端驱动的验证流程。
3. 一个未来的仓库内小工具或 UI 包装层，继续复用同一套 parser 和规则，而不是重复实现。

## 问题

当前 Win11 验证比它本应有的复杂：

- probe 证据分散在 managed 日志和可能存在的 native 调试输出里。
- 操作者需要手工对照原始字符串，并自己推断“最后一个成功阶段”。
- 同一种失败很容易被不同人用不同说法描述，因为目前没有统一的阶段模型。
- native 捕获质量依赖机器环境，作为第一轮诊断的硬前提并不合适。

## 目标

- 为一次验证会话提供一个统一命令入口。
- 默认只依赖 managed 日志即可运行。
- 把原始 `Win11Probe` 行归一化成一小组面向用户的阶段。
- 按“最后一个成功阶段”进行判因，让定位更快。
- 输出足够紧凑，适合在终端快速扫读或直接贴到 issue 里。
- 在不改变主要使用体验的前提下，让 native 证据能增强报告。

## 非目标

- 不做常驻后台监控器。
- 不依赖 DebugView 或任何特定 native 抓取工具。
- 不修改、截断或清空现有日志文件。
- 不替代深入的 native 调试工具链。
- 第一版设计不重做现有 probe 字符串本身。

## 已选方案

采用“引导式 PowerShell 会话脚本”作为主入口，以 managed 日志解析为默认事实源，以可选 native trace 导入作为增强手段。

之所以优先它，而不是 native-first 或 UI-first 工具，是因为：

- 它能最快落地。
- 在没有 native 收集器的机器上仍然可用。
- 它把诊断逻辑集中在一个地方。
- 以后包一层更友好的工具时，无需改变判定规则或输出模型。

## 数据来源

### 必需

- `%APPDATA%\QTTabBar\QTTabBarException.log`

### 可选

- 由调用方提供的一份 native trace 文本文件，文件中包含同一会话期间捕获到的 `OutputDebugString` 行。

第一版不负责自己抓 native 输出；如果 native trace 文件已经存在，它只负责消费这份文件。

## 会话模型

每次脚本运行代表一次验证会话。

会话流程如下：

1. 解析 managed 日志路径。
2. 校验日志文件存在且可读。
3. 记录当前文件状态作为基线。
4. 提示操作者重启 Explorer 并复现一次。
5. 只读取基线之后新增的行。
6. 提取其中新增的 `Win11Probe` 条目。
7. 将这些条目归一化到阶段模型。
8. 生成诊断结论和下一步提示。
9. 如果提供了 native 证据，则把它并入单独的证据段落。

脚本应优先使用以下组合来建立稳定基线：

- 当前文件长度
- 当前时间
- 复现前最后一个已知 `Win11Probe` 的位置

这样可以降低旧日志噪声被误判为本轮复现结果的概率。

## 面向用户的阶段模型

脚本对外只暴露一组小而稳定的阶段，而不是直接展示完整的内部 probe 词汇。

| 阶段 | 含义 | 主要 managed 来源 |
| --- | --- | --- |
| `BHO entered` | AutoLoader 已进入 Explorer | `Win11Probe AutoLoader.SetSite` |
| `BrowserBar requested` | 已请求激活 QTTabBar BrowserBar | `Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar` |
| `Attach flow entered` | BrowserBar 承载后，QTTabBar 的 attach 流程已开始 | `Win11Probe QTTabBarClass.OnExplorerAttached.Start` |
| `IShellBrowser query entered` | attach 流程已走到 `IShellBrowser` 查询步骤 | `Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser` |
| `Native hook requested` | managed 侧已发起 shell-browser native hook 初始化 | `Win11Probe HookLibManager.InitShellBrowserHook.Start` |
| `Native hook result` | native hook 调用已返回结果码 | `Win11Probe HookLibManager.InitShellBrowserHook.NativeResult <code>` |
| `Post-hook continuation entered` | attach 流程已越过 native hook 设置，进入下一步查询 | `Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg` |

### 有意排除项

- `ButtonBar` 和 `SecondViewBar` 相关 probe 只作为补充证据，不进入第一版主判因链。
- `BandObject.SetSite.*` 相关 probe 暂不进入第一版主模型，因为它们当前走的是默认关闭的 logger，不能作为稳定默认证据。

## 判定规则

主要规则是：确定“最后一个成功阶段”，再把它映射到最可能的故障桶。

### 故障桶

| 最后一个成功阶段 | 诊断结论 |
| --- | --- |
| 无 | 日志未开启，或 BHO 未加载 |
| 只有 `BHO entered` | BHO 已进入，但 BrowserBar 激活流程没有继续 |
| 只有 `BrowserBar requested` | BrowserBar 已请求，但 attach 流程没有真正开始 |
| 只有 `Attach flow entered` | attach 流程已开始，但未走到 `IShellBrowser` 查询步骤 |
| 只有 `IShellBrowser query entered` | 在发起 native hook 之前，获取或使用 `IShellBrowser` 的过程中失败 |
| 到 `Native hook requested` 但无结果 | native hook 调用已开始，但没有干净返回 |
| `Native hook result != 0` | native hook 初始化失败 |
| `Native hook result == 0` 且出现 `Post-hook continuation entered` | 当前 probe 窗口基本跑通，应继续看更后续阶段或用户可见行为 |

### 置信度处理

如果 probe 顺序不完整或存在异常，脚本不应直接崩溃，而应给出最保守的结论，并标记结果为“部分置信”。

## Native 证据整合

第一版里，native 证据是增强项，不是主事实源。

### 规则

- 即使没有 native 证据，报告也必须完整可用。
- native 证据不能改变对外公开的阶段名。
- native 证据可以在单独段落里提供确认信息或更细节的内部进度。
- 第一版仍以 managed 证据作为主要诊断依据。

### Native 映射示例

| Native 标记 | 用途 |
| --- | --- |
| `Win11Probe QTHookLib.Initialize.Start` | 作为 native 预热阶段证据 |
| `Win11Probe QTHookLib.InitShellBrowserHook.Start` | 证明 native 侧已收到 hook 请求 |
| `Win11Probe QTHookLib.InitShellBrowserHook.CreateComHook.BrowseObject` | 证明 native hook 内部流程已有进一步推进 |

## 脚本接口

默认调用方式：

```powershell
.\Tools\Win11ProbeSession.ps1
```

### 参数

- `-LogPath <path>`
  手工覆盖 managed 日志路径。
- `-NativeTracePath <path>`
  可选指定 native trace 文本文件路径。
- `-ShowRaw`
  在摘要后附带本轮捕获到的原始 probe 行。

## 输出约定

脚本应始终按同样顺序输出同样的高层段落：

1. `Session`
2. `Stages`
3. `Diagnosis`
4. `Next hint`
5. 可选的 `Native evidence`
6. 可选的 `Raw probes`

### 状态词汇

阶段显示只使用三种主状态：

- `OK`
- `FAIL`
- `MISS`

这样最利于快速扫读，也方便后续包装层复用。

### 示例

```text
Session
2026-07-06 15:02:11
Log source: C:\Users\...\AppData\Roaming\QTTabBar\QTTabBarException.log
Native evidence: not provided

Stages
[OK] BHO entered
[OK] BrowserBar requested
[OK] Attach flow entered
[OK] IShellBrowser query entered
[OK] Native hook requested
[FAIL] Native hook result = 5
[MISS] Post-hook continuation entered

Diagnosis
native hook 初始化失败

Next hint
检查 HookLib 加载、native Initialize/InitShellBrowserHook 返回码，以及对应的 native probe 行
```

## 异常处理

脚本在出错时应尽量给出可操作的信息：

- managed 日志文件不存在
  - 报告未找到 QTTabBar 日志文件
  - 提示先检查安装状态和日志开关
- managed 日志存在，但本轮没有新增 `Win11Probe`
  - 报告本轮未捕获到 probe
  - 提示优先检查日志开关或 BHO 是否进入
- 提供了 native trace 路径，但文件不可读
  - 主流程继续，仍给出 managed-only 诊断
  - 报告中标记 native 证据不可用
- probe 顺序异常
  - 输出 best-effort 结论
  - 把结果标记为部分置信

## 退出码

预留稳定退出码，便于后续包装层或自动化消费：

- `0` 当前 probe 窗口内未看到明显失败
- `1` 没有有效 probe 证据
- `2` 失败发生在 BHO 或 BrowserBar 阶段
- `3` 失败发生在 `IShellBrowser` 获取阶段
- `4` 失败发生在 native hook 阶段
- `5` 证据不足，无法可靠分类

## 验证计划

在任何实现被视为完成前，至少要用以下场景验证 parser 和判因规则：

1. 没有新增 probe 行
2. 停在 `AutoLoader.SetSite`
3. 停在 `ShowBrowserBar.TabBar` 之后
4. 停在 `QueryService.IShellBrowser` 前后
5. `InitShellBrowserHook.NativeResult != 0`
6. `InitShellBrowserHook.NativeResult == 0` 且 attach 流程继续

实现应先用 fixture 风格样例验证，再用一次真实 Win11 复现会话验证。

## 后续封装

后续的仓库内小工具应直接复用同一套诊断核心，而不是重新实现解析逻辑。推荐拆分为：

- parser 与归一化核心
- PowerShell 会话包装层
- 未来的 UI 或更友好的包装层

这样可以让首版脚本和后续工具保持一致，减少诊断规则漂移。

## 风险

- 现有 probe 覆盖度可能仍不足以解释某些 post-hook 之后的失败。
- managed 日志只能诊断那些已经被埋点并实际写出的阶段。
- native 增强效果取决于外部抓取条件。
- 如果 probe 字符串发生变化而归一化表没有同步更新，诊断质量会下降。

## 设计后的开放后续项

这些是实现阶段的后续项，不是当前设计的阻塞项：

- 决定 parser 规则是全部放在 PowerShell 中，还是拆成一个小型 helper module
- 决定脚本后续是否提供可选 JSON 输出，供机器消费
- 决定未来是否补更多 always-on probe，以覆盖 post-hook 之后的行为
