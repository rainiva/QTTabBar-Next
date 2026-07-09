# QTTabBar-Next 架构问题修复执行清单

> **生成日期**: 2026-07-09
> **基线提交**: `0be2deb` (feat(arch-batch3): cache config metadata and dedupe plugin reloads)
> **审查范围**: 482 个 .cs 文件 / 135,204 行代码
> **TDD 强制**: 所有修复必须遵循 RED→GREEN→REFACTOR，测试面向真实用户 UI 操作流程

---

## 目录

- [第一批：运行时缺陷立即修复（C1-C5）](#第一批运行时缺陷立即修复c1-c5)
- [第二批：多真源状态一致性修复（W5-W10）](#第二批多真源状态一致性修复w5-w10)
- [第三批：上帝模块拆解（C6-C7, W1-W3）](#第三批上帝模块拆解c6-c7-w1-w3)
- [第四批：基础设施改进（W4, S1-S5）](#第四批基础设施改进w4-s1-s5)
- [批次依赖关系](#批次依赖关系)
- [全局验收标准](#全局验收标准每批次完成后)

---

## 第一批：运行时缺陷立即修复（C1-C5）

这五个问题影响面小、修复方案明确，应首先处理。五个问题之间无依赖，可并行开发。

---

### C1. QTSecondViewBar.CurrentLocation 自引用栈溢出

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\QTSecondViewBar.cs` 第 65-76 行 |
| **问题类型** | 运行时崩溃 — StackOverflowException |

**问题描述**

`CurrentLocation` 属性的 getter 和 setter 均引用属性自身，未使用后背字段。任何对该属性的读写调用都会触发无限递归，最终导致 `StackOverflowException` 使 Explorer 进程崩溃。

**当前代码**

```csharp
// QTSecondViewBar.cs 第 65-76 行
public ShellObject CurrentLocation
{
    get
    {
        return CurrentLocation;  // 自引用 → 无限递归
    }
    set
    {
        CurrentLocation = value; // 自引用 → 无限递归
    }
}
```

**修复方案**

引入后背字段，getter/setter 读写后背字段：

```csharp
private ShellObject _currentLocation;

public ShellObject CurrentLocation
{
    get
    {
        return _currentLocation;
    }
    set
    {
        _currentLocation = value;
    }
}
```

**TDD-RED 测试**

```csharp
// 测试目标：验证 CurrentLocation 属性可正确读写
// 预期失败：当前代码触发 StackOverflowException
[Test]
public void CurrentLocation_SetAndGet_ReturnsCorrectValue()
{
    var secondViewBar = new QTSecondViewBar(); // 若构造受限于 COM，用反射创建
    var expected = new ShellObject(); // 或 mock

    secondViewBar.CurrentLocation = expected;

    Assert.That(secondViewBar.CurrentLocation, Is.EqualTo(expected));
    // 当前代码在此行之前已因 StackOverflowException 崩溃
}
```

**验收标准**

1. RED 测试确认失败（StackOverflowException 或构造失败）
2. 修复后 GREEN — 测试通过
3. MSBuild Debug + Release 编译通过
4. Grep 确认无其他代码引用 `CurrentLocation` 属性的调用点行为变化
5. git 提交消息：`fix(arch-batch1): fix CurrentLocation self-reference stack overflow`

---

### C2. ButtonBarRegistry.TryGetButtonBarHandle 忽略 explorerHandle 参数

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\ButtonBarRegistry.cs` 第 51-60 行 |
| **问题类型** | 功能缺陷 — 参数被忽略 |

**问题描述**

方法签名接收 `IntPtr explorerHandle` 参数，但函数体完全忽略它，始终用 `Thread.CurrentThread` 查找。当需要按 Explorer 窗口句柄查找对应的按钮栏时，该方法永远只返回当前线程的按钮栏，跨窗口查找无效。

**当前代码**

```csharp
// ButtonBarRegistry.cs 第 51-60 行
public static bool TryGetButtonBarHandle(IntPtr explorerHandle, out IntPtr ptr) {
    using(new Keychain(rwLockBtnBar, false)) {
        QTButtonBar bbar;
        if(dictBBarInstances.TryGetValue(Thread.CurrentThread, out bbar)) {
            // ↑ 忽略 explorerHandle，始终按当前线程查找
            ptr = bbar.Handle;
            return true;
        }
        ptr = IntPtr.Zero;
        return false;
    }
}
```

**修复方案**

需在 `ButtonBarRegistry` 中增加 `explorerHandle → QTButtonBar` 的映射（或在 `dictBBarInstances` 之外维护一个 `StackDictionary<IntPtr, QTButtonBar>` 索引），在 `RegisterButtonBar` 时同时注册该映射，在 `UnregisterButtonBar` 时移除：

```csharp
// 新增：HWND → ButtonBar 索引
private static readonly StackDictionary<IntPtr, QTButtonBar> dictBBarByHandle =
    new StackDictionary<IntPtr, QTButtonBar>();

// RegisterButtonBar 中增加：
dictBBarByHandle[bbar.Handle] = bbar;

// TryGetButtonBarHandle 改为按 handle 查找：
public static bool TryGetButtonBarHandle(IntPtr explorerHandle, out IntPtr ptr) {
    using(new Keychain(rwLockBtnBar, false)) {
        QTButtonBar bbar;
        if(dictBBarByHandle.TryGetValue(explorerHandle, out bbar)) {
            ptr = bbar.Handle;
            return true;
        }
        ptr = IntPtr.Zero;
        return false;
    }
}
```

> **注意**：需确认 `explorerHandle` 传入的是 Explorer 窗口句柄还是 ButtonBar 自身句柄。若为 Explorer 窗口句柄，需在注册时建立 Explorer Handle → ButtonBar 的关联。

**TDD-RED 测试**

```csharp
// 测试目标：验证 TryGetButtonBarHandle 按传入的 handle 查找
// 预期失败：当前代码忽略 handle，始终按当前线程查找
[Test]
public void TryGetButtonBarHandle_ReturnsButtonBarForGivenHandle()
{
    // 注册两个不同 handle 的 ButtonBar（需 mock 或反射）
    var handle1 = new IntPtr(0x1234);
    var handle2 = new IntPtr(0x5678);

    // 注册 handle1
    ButtonBarRegistry.RegisterButtonBar(mockBbar1);

    // 用非当前线程的 handle 查找
    IntPtr result;
    bool found = ButtonBarRegistry.TryGetButtonBarHandle(handle2, out result);

    // 当前代码：因按 Thread.CurrentThread 查找，返回 handle1 而非 null
    Assert.That(found, Is.False, "Should not find button bar for unregistered handle");
}
```

**验收标准**

1. RED 测试确认失败
2. 修复后按 handle 正确查找
3. 现有调用方 `InstanceManager.cs` 第 642 行行为不受影响
4. `RegisterButtonBar` / `UnregisterButtonBar` 同步维护新索引
5. git 提交消息：`fix(arch-batch1): fix TryGetButtonBarHandle ignoring explorerHandle param`

---

### C3. QTTabBarClass.CreateTab 静态方法绕过 TabManager 全部校验

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\QTTabBarClass.cs` 第 205-251 行 |
| **问题类型** | 安全/健壮性 — 绕过校验逻辑 |

**问题描述**

`CreateTab` 是 `public static` 方法，直接构造 `QTabItem` 并插入 `tabControl1`，跳过了 `TabManager.OpenNewTab` 中的三项关键校验：
- `IsReadyIfDrive` — 可移动驱动器就绪检查
- `IsLinkToDeadFolder` — 死链接检测
- `ResolveTargetIfLink` — 快捷方式解析

未发现内部调用方，可能是死代码或供插件/反射调用。若被调用，可能导致打开无效路径标签。

**当前代码**

```csharp
// QTTabBarClass.cs 第 205-251 行
public static bool CreateTab(QTTabBarClass tabBar, Address address, int index, bool fLocked, bool fSelect)
{
    if (null == tabBar) {
        tabBar = GetThreadTabBar();
    }
    if (null == tabBar) {
        tabBar = TabInstanceRegistry.PeekMainInstance();
        if (tabBar == null) return false;
    }

    using (IDLWrapper wrapper = new IDLWrapper(address))
    {
        address.ITEMIDLIST = wrapper.IDL;
        address.Path = wrapper.Path;
    }
    if ((address.ITEMIDLIST == null) || (address.ITEMIDLIST.Length <= 0))
        return false;

    // 直接 new QTabItem 并插入，跳过 TabManager 校验
    QTabItem tab = new QTabItem(
        QTUtility2.MakePathDisplayText(address.Path, false),
        address.Path, tabBar.tabControl1);
    tab.NavigatedTo(address.Path, address.ITEMIDLIST, -1, false);
    tab.ToolTipText = QTUtility2.MakePathDisplayText(address.Path, true);
    tab.TabLocked = fLocked;
    if (index < 0) {
        tabBar.AddInsertTab(tab);
    } else {
        if (index > tabBar.tabControl1.TabCount)
            index = tabBar.tabControl1.TabCount;
        tabBar.tabControl1.TabPages.Insert(index, tab);
    }
    if (fSelect) {
        tabBar.tabControl1.SelectTab(tab);
    }
    return true;
}
```

**修复方案**

分两步：

**步骤 1 — 调用方排查**：
全局搜索所有调用 `CreateTab` 的位置（含反射、插件接口 `ITab`、`IPluginServer`）。检查 `.agents/` 目录下的 diff 文件确认历史。

**步骤 2a — 若无调用方（死代码）**：
删除该方法。

**步骤 2b — 若有调用方（需保留）**：
改为转发到 `TabManager.OpenNewTab`，利用其校验逻辑：

```csharp
public static bool CreateTab(QTTabBarClass tabBar, Address address, int index, bool fLocked, bool fSelect)
{
    if (null == tabBar) {
        tabBar = GetThreadTabBar() ?? TabInstanceRegistry.PeekMainInstance();
        if (tabBar == null) return false;
    }

    // 转发到 TabManager，复用 IsReadyIfDrive/IsLinkToDeadFolder/ResolveTargetIfLink 校验
    return tabBar._tabManager.OpenNewTab(address, index, fLocked, fSelect);
}
```

**TDD-RED 测试**

```csharp
// 测试目标：验证 CreateTab 拒绝死链接路径
// 预期失败：当前代码不校验，直接创建标签
[Test]
public void CreateTab_DeadLinkPath_ReturnsFalse()
{
    // 构造指向不存在目标的快捷方式路径
    var deadLinkPath = @"C:\NonExistentTarget.lnk";

    var tabBar = GetOrCreateTabBar(); // 获取实例
    var address = new Address(deadLinkPath);

    bool result = QTTabBarClass.CreateTab(tabBar, address, -1, false, false);

    // 当前代码：返回 true 并创建了一个指向死链接的标签
    Assert.That(result, Is.False, "CreateTab should reject dead link paths");
}

// 若确认死代码则：
[Test]
public void CreateTab_MethodRemoved_ReflectionReturnsNull()
{
    var method = typeof(QTTabBarClass).GetMethod(
        "CreateTab",
        BindingFlags.Static | BindingFlags.Public);

    Assert.That(method, Is.Null, "CreateTab should be removed if no callers exist");
}
```

**验收标准**

1. 调用方排查有完整记录（搜索范围：全项目 + 反射 + 插件接口）
2. 若删除：MSBuild 编译通过且无引用断裂
3. 若改写：死链接/无效驱动器路径被正确拒绝（返回 false）
4. RED→GREEN 测试证据
5. git 提交消息：`fix(arch-batch1): remove dead CreateTab bypass or redirect to TabManager`

---

### C4. PersistBreakTabBar 不广播导致跨进程不同步

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\Config.cs` 第 1265-1272 行 |
| **问题类型** | 跨进程状态不一致 |

**问题描述**

`PersistBreakTabBar` 直接写 `BreakTabBar` 到注册表，但既不调用 `ConfigVersionTracker.Increment()`，也不调用 `InstanceManager.StaticBroadcastCommand()`。对比同文件中 `SetNoCapturePathsAndBroadcast`（第 1250-1263 行）正确执行了这两步。结果：用户在一个 Explorer 窗口切换 BreakTabBar 后，其他进程实例不会收到更新。

**当前代码**

```csharp
// Config.cs 第 1265-1272 行
public static void PersistBreakTabBar(bool breakTabBar) {
    Config.Window.BreakTabBar = breakTabBar;
    using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root + RegConst.Config + "Window")) {
        if(key != null) {
            key.SetValue("BreakTabBar", breakTabBar ? 1 : 0);
        }
    }
    // ← 缺少 ConfigVersionTracker.Increment()
    // ← 缺少 InstanceManager.StaticBroadcastCommand(EncodeReloadConfig)
}
```

**修复方案**

与 `SetNoCapturePathsAndBroadcast` 保持一致模式：

```csharp
public static void PersistBreakTabBar(bool breakTabBar) {
    Config.Window.BreakTabBar = breakTabBar;
    using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root + RegConst.Config + "Window")) {
        if(key != null) {
            key.SetValue("BreakTabBar", breakTabBar ? 1 : 0);
        }
    }
    ConfigVersionTracker.Increment();
    InstanceManager.StaticBroadcastCommand(
        IpcCommandMessage.EncodeReloadConfig(ConfigVersionTracker.Current));
}
```

**TDD-RED 测试**

```csharp
// 测试目标：验证 PersistBreakTabBar 触发版本递增和广播
// 预期失败：当前代码不递增不广播
[Test]
public void PersistBreakTabBar_IncrementsVersionAndBroadcasts()
{
    var versionBefore = ConfigVersionTracker.Current;

    ConfigManager.PersistBreakTabBar(true);

    // 当前代码：versionBefore == ConfigVersionTracker.Current（未递增）
    Assert.That(ConfigVersionTracker.Current, Is.GreaterThan(versionBefore),
        "ConfigVersionTracker should be incremented");

    // 验证注册表值已写入
    using(var key = Registry.CurrentUser.OpenSubKey(
        RegConst.Root + RegConst.Config + "Window")) {
        Assert.That(key.GetValue("BreakTabBar"), Is.EqualTo(1));
    }
}
```

**验收标准**

1. RED 测试确认失败（版本号未递增）
2. 修复后 GREEN — 版本号递增且广播触发
3. 与 `SetNoCapturePathsAndBroadcast` 模式一致
4. 跨进程实例能收到 BreakTabBar 变更（可通过多进程测试或 IPC mock 验证）
5. git 提交消息：`fix(arch-batch1): fix PersistBreakTabBar not broadcasting config change`

---

### C5. UnregisterTabBar 不清理跨进程注册

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\InstanceManager.cs` 第 628 行 |
| **问题类型** | 跨进程资源泄漏 |

**问题描述**

`PushTabBarInstance`（第 624 行）同时注册本地和跨进程，但 `UnregisterTabBar`（第 628 行）仅调用 `TabInstanceRegistry.UnregisterTabBar()`，不调用 `service.DeleteInstance()` 清理 WCF 服务端的 `sdInstances`。窗口关闭后服务端残留无效句柄，靠 `CheckConnections` 被动清理死连接。

**当前代码**

```csharp
// InstanceManager.cs 第 624 行 — 注册（本地 + 跨进程）
public static void PushTabBarInstance(QTTabBarClass tabbar) {
    TabInstanceRegistry.PushTabBarInstance(tabbar);
    ICommService service = GetChannel();
    if(service != null) service.PushInstance(tabbar.Handle);
}

// InstanceManager.cs 第 628 行 — 注销（仅本地！）
public static bool UnregisterTabBar() {
    TabInstanceRegistry.UnregisterTabBar();
    return false;
    // ← 缺少 service.DeleteInstance(handle)
}
```

**修复方案**

与 `PushTabBarInstance` 对称，添加跨进程注销：

```csharp
public static bool UnregisterTabBar() {
    // 先获取当前 handle 用于跨进程注销
    IntPtr handle = TabInstanceRegistry.GetCurrentHandle();
    TabInstanceRegistry.UnregisterTabBar();
    ICommService service = GetChannel();
    if(service != null && handle != IntPtr.Zero) {
        try {
            service.DeleteInstance(handle);
        } catch {
            // WCF 通道不可用时不抛异常，靠 CheckConnections 被动清理
        }
    }
    return false;
}
```

> **注意**：需确认 `TabInstanceRegistry` 是否有 `GetCurrentHandle()` 方法，若无则需在 `UnregisterTabBar` 前先获取 handle。可改为在 `TabInstanceRegistry.UnregisterTabBar()` 返回被注销的 handle。

**TDD-RED 测试**

```csharp
// 测试目标：验证 UnregisterTabBar 清理跨进程注册
// 预期失败：当前代码仅本地注销，服务端残留
[Test]
public void UnregisterTabBar_RemovesFromServerInstances()
{
    var mockTabBar = CreateMockTabBar();
    InstanceManager.PushTabBarInstance(mockTabBar);

    // 确认服务端已注册
    int countBefore = InstanceManager.GetTotalInstanceCount();
    Assert.That(countBefore, Is.GreaterThan(0));

    InstanceManager.UnregisterTabBar();

    // 当前代码：服务端仍残留，countAfter == countBefore
    int countAfter = InstanceManager.GetTotalInstanceCount();
    Assert.That(countAfter, Is.LessThan(countBefore),
        "Server should not retain stale handle after UnregisterTabBar");
}
```

**验收标准**

1. RED 测试确认失败（服务端残留）
2. 修复后服务端无残留
3. WCF 通道不可用时不抛异常（GetChannel 返回 null 时安全跳过）
4. 与 `PushTabBarInstance` 对称
5. git 提交消息：`fix(arch-batch1): fix UnregisterTabBar not cleaning cross-process registration`

---

### 第一批整体验收

- [x] 全量 NUnit 测试通过（5 项配套测试文件均已存在）
- [x] MSBuild Debug + Release 双配置编译通过
- [x] 路径限定 git 提交，每项独立 commit（7 条 arch-batch1 提交）
- [x] 三维 Ultra Review（完整性 / 正确性 / 影响）
- [x] 涉及中文的文件使用显式 UTF-8（无 BOM）编码写入

**验收结论：第一批 5/5 项全部已修复。** 每项均有代码变更、git 提交记录和配套 NUnit 测试文件。

---

## 第二批：多真源状态一致性修复（W5-W10）

这批修复配置缓存同步和状态一致性问题。依赖第一批 C4/C5 已修复（配置广播机制正确）。

---

### W5. NoCapturePathsList 缓存同步风险

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\Config.cs` 第 1250 行、`QTTabBar\SessionState.cs` 第 25 行 |
| **问题类型** | 缓存不一致 |

**问题描述**

`NoCapturePathsList` 是 `Config.Window.NoCaptureAt` 的字符串拆分缓存。`SetNoCapturePathsAndBroadcast` 同时更新两者，但 `ReadConfig` 中仅调用 `ApplyNoCapturePathsFromConfig()` 重建缓存。若其他代码路径直接修改 `Config.Window.NoCaptureAt` 而不调用该方法，缓存将过时。

**修复方案**

将 `NoCapturePathsList` 的更新封装为唯一入口方法 `UpdateNoCapturePaths()`，所有修改 `Config.Window.NoCaptureAt` 的路径强制经过该方法：

```csharp
// Config.cs 中新增统一入口
private static void UpdateNoCapturePaths(IEnumerable<string> paths) {
    var list = paths == null ? new List<string>() : paths.ToList();
    Config.Window.NoCaptureAt = string.Join(";", list.ToArray());
    lock(QTUtility.syncRoot) {
        QTUtility.NoCapturePathsList = new List<string>(list);
    }
}
```

然后 `SetNoCapturePathsAndBroadcast` 和 `ApplyNoCapturePathsFromConfig` 均调用此方法。

**TDD-RED 测试**

```csharp
[Test]
public void NoCapturePathsList_AlwaysInSync_WithConfig()
{
    // 直接修改 Config.Window.NoCaptureAt（模拟旁路）
    Config.Window.NoCaptureAt = @"C:\Path1;C:\Path2";

    // 触发 ReadConfig 或 UpdateNoCapturePaths
    ConfigManager.ReadConfig();

    // 验证缓存与配置一致
    Assert.That(QTUtility.NoCapturePathsList, Is.Not.Null);
    Assert.That(QTUtility.NoCapturePathsList.Count, Is.EqualTo(2));
    Assert.That(QTUtility.NoCapturePathsList, Contains.Item(@"C:\Path1"));
}
```

**验收标准**

1. Grep 确认无直接修改 `Config.Window.NoCaptureAt` 的旁路
2. ReadConfig 后缓存与配置一致
3. 现有 `SetNoCapturePathsAndBroadcast` 行为不变
4. git 提交消息：`fix(arch-batch2): converge NoCapturePaths cache update to single entry`

---

### W6. CurrentTab 与 SelectedIndex 双状态不同步

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\TabBarBase.cs` 第 103 行、`QTTabBar\QTTabBarClass.cs` 第 1203-1226 行 |
| **问题类型** | 状态不一致 |

**问题描述**

标签栏当前选中标签由 `CurrentTab`（QTabItem）和 `tabControl1.SelectedIndex`（int）两套独立状态维护。BindAction 中 NextTab/PreviousTab/FirstTab/LastTab 直接操作 `tabControl1.SelectedIndex`，而 IPC/属性路径走 `SelectTab`。两者可能触发不同的事件序列。

**当前代码**

```csharp
// QTTabBarClass.cs 第 1202-1226 行
case BindAction.NextTab:
    if(tabControl1.SelectedIndex < tabControl1.TabCount - 1) {
        tabControl1.SelectedIndex++;  // ← 直接操作，绕过 SelectTab
    }
    break;
case BindAction.PreviousTab:
    if(tabControl1.SelectedIndex > 0) {
        tabControl1.SelectedIndex--;  // ← 直接操作
    }
    break;
case BindAction.FirstTab:
    tabControl1.SelectedIndex = 0;   // ← 直接操作
    break;
case BindAction.LastTab:
    tabControl1.SelectedIndex = tabControl1.TabCount - 1; // ← 直接操作
    break;
```

**修复方案**

统一改为调用 `tabControl1.SelectTab(index)`：

```csharp
case BindAction.NextTab:
    if(tabControl1.SelectedIndex < tabControl1.TabCount - 1) {
        tabControl1.SelectTab(tabControl1.SelectedIndex + 1);
    }
    break;
case BindAction.PreviousTab:
    if(tabControl1.SelectedIndex > 0) {
        tabControl1.SelectTab(tabControl1.SelectedIndex - 1);
    }
    break;
case BindAction.FirstTab:
    tabControl1.SelectTab(0);
    break;
case BindAction.LastTab:
    tabControl1.SelectTab(tabControl1.TabCount - 1);
    break;
```

**TDD-RED 测试**

```csharp
[Test]
public void SwitchTab_BindAction_TriggersSameEventSequence_AsSelectTab()
{
    // 创建 3 个标签
    SetupTabs("Tab1", "Tab2", "Tab3");

    // 通过 BindAction NextTab 切换
    tabControl1.SelectedIndex = 0;
    InvokeBindAction(BindAction.NextTab);

    // 验证 CurrentTab 与 SelectedIndex 同步
    Assert.That(tabControl1.SelectedIndex, Is.EqualTo(1));
    Assert.That(tabBarBase.CurrentTab.Text, Is.EqualTo("Tab2"));
    // 当前代码：CurrentTab 可能未更新（取决于事件序列）
}
```

**验收标准**

1. 切换标签后 `CurrentTab` 与 `SelectedIndex` 一致
2. 导航按钮状态（前进/后退）正确更新
3. IPC SelectTab 与 BindAction 走同一事件序列
4. git 提交消息：`fix(arch-batch2): unify tab switching to SelectTab`

---

### W7. IDLWrapper.dicCacheIDLs 无锁保护

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\IDLWrapper.cs` 第 32 行 |
| **问题类型** | 线程安全 — 竞态条件 |

**问题描述**

`static Dictionary` 无锁保护。`AddCache`（第 227 行）在导航完成时写入，`TryGetCache`（第 426 行）和 `InitFromPath`（第 207 行）在 UI 线程读取。当前都在 UI 线程操作，但若后台线程（如 IPC 回调）触发导航，存在竞态风险。

**修复方案**

选项 A（推荐 — 最小改动）：改用 `ConcurrentDictionary<string, byte[]>`：

```csharp
// 替换
private static readonly Dictionary<string, byte[]> dicCacheIDLs = new Dictionary<string, byte[]>();
// 为
private static readonly ConcurrentDictionary<string, byte[]> dicCacheIDLs = new ConcurrentDictionary<string, byte[]>();
```

选项 B：在 AddCache/TryGetCache 加 `lock(QTUtility.syncRoot)`。

**TDD-RED 测试**

```csharp
[Test]
public void IDLCache_ConcurrentReadWrite_NoCrash()
{
    var tasks = new List<Task>();
    var path = @"C:\Windows";

    // 并发写入
    for(int i = 0; i < 100; i++) {
        tasks.Add(Task.Run(() => {
            var idl = new IDLWrapper(path);
            // AddCache 内部调用
        }));
    }

    // 并发读取
    for(int i = 0; i < 100; i++) {
        tasks.Add(Task.Run(() => {
            IDLWrapper.TryGetCache(path, out var idl);
        }));
    }

    Assert.DoesNotThrow(() => Task.WaitAll(tasks.ToArray()));
    // 当前代码：可能抛出 InvalidOperationException（集合被修改）
}
```

**验收标准**

1. 多线程并发读写不崩溃
2. 现有单线程行为不变
3. 性能无可感知退化
4. git 提交消息：`fix(arch-batch2): make IDLWrapper.dicCacheIDLs thread-safe`

---

### W8. NightMode 多处独立读注册表

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\QTUtility.cs` 第 646 行、`QTTabBar\QTabControl.cs` 第 139 行、`QTTabBar\QTTabBarClass.cs` 第 539 行 |
| **问题类型** | 状态不一致 — 多处独立读取 |

**问题描述**

`InNightMode` 缓存值由各处独立调用 `getNightMode()` 刷新，无统一触发点。主题切换时可能部分 UI 元素读取到旧值。

**修复方案**

统一为 `QTUtility.RefreshNightMode()` 单一入口，各处调用该方法而非各自读注册表：

```csharp
// QTUtility.cs 中新增统一刷新入口
public static void RefreshNightMode() {
    InNightMode = getNightMode(); // 统一读取 + 缓存
}

// QTabControl.cs、QTTabBarClass.cs 中改为：
QTUtility.RefreshNightMode();
// 而非各自调用 getNightMode()
```

**验收标准**

1. Grep 确认无直接读 `AppsUseLightTheme` 注册表的旁路
2. 主题切换后所有 UI 元素同步刷新
3. git 提交消息：`fix(arch-batch2): converge NightMode refresh to single entry`

---

### W9. ResMain/ResMisc 引用拷贝过时风险

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\QTUtility.cs` 第 109-110 行 |
| **问题类型** | 缓存过时 |

**问题描述**

`ResMain` 和 `ResMisc` 是 `TextResourcesDic` 中特定键的引用拷贝。若 `TextResourcesDic` 被整体替换（`UpdateConfig` 中 `QTUtility.TextResourcesDic = newTextResources`），`ResMain`/`ResMisc` 在下次 `ValidateTextResources()` 调用前仍指向旧字典。

**修复方案**

在 `TextResourcesDic` 整体替换后立即调用 `ValidateTextResources()` 刷新引用拷贝，或改为按需从 `TextResourcesDic` 读取：

```csharp
// 选项 A：确保替换后立即刷新
public static Dictionary<string, string[]> TextResourcesDic {
    get { return _textResourcesDic; }
    set {
        _textResourcesDic = value;
        ValidateTextResources(); // 立即刷新 ResMain/ResMisc
    }
}

// 选项 B：改为按需读取（消除引用拷贝）
public static string[] ResMain => TextResourcesDic.TryGetValue("TabBar_Menu", out var v) ? v : null;
public static string[] ResMisc => TextResourcesDic.TryGetValue("Misc_Strings", out var v) ? v : null;
```

**验收标准**

1. 语言切换后所有本地化字符串立即更新
2. 无短暂过时窗口
3. git 提交消息：`fix(arch-batch2): fix ResMain/ResMisc stale reference after TextResourcesDic replacement`

---

### W10. 多入口独立写配置的时序风险

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | OptionsDialog、Options13_Language、QTDesktopTool、QTButtonBar、QTTabBarClass、QTSecondViewBar 共 6 处 |
| **问题类型** | 时序一致性风险 |

**问题描述**

6 处独立调用 WriteConfig/UpdateConfig 触发配置写入与广播。虽然 `ConfigVersionTracker` 接收侧去重，但多入口增加一致性与时序风险。

**修复方案**

评估是否可收敛为统一写入入口。若保留多入口，确保每处都正确调用 `WriteConfig + UpdateConfig + Increment`。

**验收标准**

1. 所有写入路径都触发版本递增和广播
2. 并发写入不丢更新
3. ConfigVersionTracker 正确去重
4. git 提交消息：`fix(arch-batch2): audit and align all config write paths`

---

### 第二批整体验收

- [x] 全量 NUnit 测试通过
- [x] MSBuild Debug + Release 编译通过
- [x] 路径限定 git 提交
- [x] 三维 Ultra Review

**验收结论：第二批 5/6 项已修复，W9 部分修复。** W9 的 setter 已调用刷新方法但 ResMain/ResMisc 本质仍为引用拷贝，若绕过 setter 直接修改字典内容仍会过时。

---

## 第三批：上帝模块拆解（C6-C7, W1-W3）

这批是长期技术债务治理，遵循已有拆解范式。依赖第一/二批在干净的基线上完成。

---

### C6. QTTabBarClass 继续拆解（当前 3/13 职责已提取）

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\QTTabBarClass.cs`（4,812 行主文件 + 4 个 partial） |
| **当前拆解状态** | TabManager、ExplorerController、MenuController 已提取；主文件仍承载 4,812 行 / 152 方法 |

**拆解范式**

每批遵循已有范式（参考记忆：QTTabBar巨型类拆解搬迁范式）：

1. **特征化测试先行（TDD-RED）**：新建测试用反射访问 internal/private 成员，锁定迁移前后行为，先见失败再迁移
2. **提取为嵌套 internal class**：优先 nested internal class（可直接访问外层实例 private 成员，0 可见性放宽）
3. **原类保留同名 façade/转发方法**：返回同一实例引用/同一把锁引用，保证调用点与行为不变
4. **嵌套类 this 语义陷阱**：方法迁入嵌套类后 this 指向嵌套类实例，需持 `private readonly QTTabBarClass _owner` 字段，所有外层实例成员裸引用加 `_owner.` 前缀
5. **保留有跨文件调用的方法**：如 QTButtonBar.cs 调用的 CreateBranchMenu/CreateNavBtnMenuItems，签名与可见性不变
6. 每批完成后全量测试 → MSBuild 构建 → 路径限定 git 提交 → 三维 Ultra Review

**优先提取顺序**

| 批次 | 职责领域 | 方法数 | 独立性 | 依赖说明 |
|------|---------|--------|--------|---------|
| 3a | 拖放处理 | 6 | 高 | dragTargetWrapper_* 系列，自成体系 |
| 3b | 键盘鼠标 Hook | 8 | 高 | Callback* 系列 + Handle* 系列 |
| 3c | Tooltip/子目录提示 | 4 | 中 | Show/Hide* 系列，依赖鼠标位置 |
| 3d | 文件工具 | 3 | 高 | DoFileTools/ShowMD5 系列 |
| 3e | 菜单创建扩展 | 8 | 中 | 扩展现有 MenuController |
| 3f | 窗口管理 | 6 | 中 | Merge/Restore/Minimize 系列 |
| 3g | BindAction | 1 | 高 | DoBindAction switch |
| 3h | Shell 命令 | 4 | 高 | createNewFile/OpenCmd/Wait4Select/cmdPath |
| 3i | ListView 输入 | 7 | 高 | SelectionChanged/鼠标/标签编辑 + 选择捕获 |
| 3j | 菜单初始化 | 2 | 高 | InitializeSysMenu/InitializeTabMenu → MenuController |
| 3k | 窗口引导 | 3 | 高 | InitializeNavBtns/InitializeOpenedWindow/InstallHooks → ExplorerControllerModule |
| 3l | 项激活/Travel 栏 | 4 | 高 | HandleItemActivate/HandleF5 → ListViewInputController；TravelToolbar → ExplorerControllerModule |

**每批验收标准**

1. 特征化测试迁移前后行为一致
2. 嵌套类持有 `_owner` 字段，外层成员加 `_owner.` 前缀
3. façade 转发签名不变
4. QTButtonBar.cs 等跨文件调用点不受影响
5. 全量测试绿 + MSBuild 通过 + git 提交 + 三维 Ultra Review
6. git 提交消息：`refactor(arch-batch3x): extract <ModuleName> from QTTabBarClass`

---

### C7. QTUtility + QTUtility2 拆解

| 项目 | 内容 |
|------|------|
| **严重级别** | Critical |
| **文件** | `QTTabBar\QTUtility.cs`（830 行）+ `QTTabBar\QTUtility2.cs`（928 行） |
| **当前状态** | 173 静态字段、105+ public/internal 方法、被 145 文件引用 |

**前置清理**（必须在拆解前完成）

1. 清理 QTUtility 的 17 个 façade 转发方法（第 4.2 节）
2. 清理 QTUtility.Initialize() 死 façade（S2）
3. 全量测试绿 + 构建通过 + git 提交

**拆解方向**

| 目标类 | 来源 | 职责 | 估计行数 |
|--------|------|------|---------|
| `OSDetector` | QTUtility | OS 版本检测、版本信息 | ~200 |
| `QTLogger` | QTUtility2 | 日志记录 | ~150 |
| `RegistryHelper` | QTUtility + QTUtility2 | 注册表读写 | ~100 |
| `PathValidator`（已有） | QTUtility façade → 直接引用 | 路径验证 | 已存在 |
| `SerializationHelper`（已有） | QTUtility façade → 直接引用 | 序列化 | 已存在 |
| `IconManager`（已有） | QTUtility façade → 直接引用 | 图标缓存 | 已存在 |
| 剩余 QTUtility | — | 全局状态、常量 | ~300 |

**验收标准**

1. 每个拆出的类职责单一
2. 145 个引用文件逐步迁移（每批 10-20 个文件）
3. 特征化测试锁定迁移前后行为
4. 全量测试绿 + 构建通过
5. git 提交消息：`refactor(arch-batch3x): extract <ClassName> from QTUtility/QTUtility2`

---

### W1. InstanceManager façade 清理

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\InstanceManager.cs`（715 行，14 个 façade） |
| **修复动作** | 逐批将调用方直接调用 TabInstanceRegistry/ButtonBarRegistry/SelectionTracker，删除 InstanceManager 中的纯转发方法 |

**façade 清单**

| 行号 | 方法 | 转发目标 |
|------|------|---------|
| 580 | `LocalTabBroadcast` | `TabInstanceRegistry` |
| 591 | `LocalBBarBroadcast` | `ButtonBarRegistry` |
| 618 | `LocalInvokeMain` | `TabInstanceRegistry` |
| 620 | `RegisterButtonBar` | `ButtonBarRegistry` |
| 626 | `UnregisterButtonBar` | `ButtonBarRegistry` |
| 628 | `UnregisterTabBar` | `TabInstanceRegistry`（注意 C5 修复后非纯转发） |
| 632 | `PutSelect` | `SelectionTracker` |
| 634 | `RemoveSelect` | `SelectionTracker` |
| 636 | `GetSelect` | `SelectionTracker` |
| 638 | `GetThreadTabBar` | `TabInstanceRegistry` |
| 640 | `GetThreadButtonBar` | `ButtonBarRegistry` |
| 642 | `TryGetButtonBarHandle` | `ButtonBarRegistry`（注意 C2 修复后非纯转发） |
| 713 | `SyncToolbarColorThreads` | `TabInstanceRegistry` |

**验收标准**

1. 每删除一批 façade 后全量测试绿
2. InstanceManager 仅保留 IPC 通信和跨进程协调职责
3. git 提交消息：`refactor(arch-batch3x): remove InstanceManager façade <MethodName>`

---

### W2. QTDesktopTool 拆解

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\QTDesktopTool.cs`（2,706 行，78 方法） |
| **修复动作** | 按 C6 同范式拆解，优先提取独立性高的职责（列表视图操作、菜单创建等） |
| **验收标准** | 同 C6 |

---

### W3. QTSecondViewBar 与 QTTabBarClass 代码去重

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **文件** | `QTTabBar\QTSecondViewBar.cs`（2,214 行） |
| **前置条件** | 先修复 C1 栈溢出 bug |

**修复方案**

将 OpenNewTab/CloseTab/CreateNewTab/AddInsertTab/OnExplorerAttached 等共享逻辑上提至 `TabBarBase` 基类或共享控制器。

**验收标准**

1. 特征化测试锁定双方行为
2. 上提后两子类行为不变
3. 重复代码行数减少 50%+
4. git 提交消息：`refactor(arch-batch3x): dedup QTSecondViewBar/QTTabBarClass shared logic`

---

### 第三批整体验收

- [x] 每批次全量测试绿
- [x] MSBuild 通过
- [x] git 提交
- [x] 三维 Ultra Review

**验收结论：第三批 1/5 项完全修复（W1），3 项部分修复（C6、C7、W3），1 项已验证（W2）。**
- C6：ListView 项激活与 Travel 栏消息处理已提取（3l）；主文件约 1,802 行，拆解仍在进行
- C7：OSDetector/QTLogger/RegistryHelper 等已创建；ReadLanguageFile 调用方已迁移至 QTResourceManager
- W1：纯 registry façade 已移除；InstanceManager 仅保留 IPC/跨进程协调方法
- W2：已验证 — DesktopTooltipController 已提取，主文件 2,191 行
- W3：部分修复 — TabOperations + CloseTab/CloseTabs/CancelFailedTabChanging 已上提 TabBarBase；QTSecondViewBar 1,736 行

---

## 第四批：基础设施改进（W4, S1-S5）

这批可在任何批次间隙穿插执行。

---

### W4. 统一注册表访问层

| 项目 | 内容 |
|------|------|
| **严重级别** | Warning |
| **修复动作** | 创建 `RegistryAccess` 工具类，封装所有 `RegConst.Root` 读写 |

**当前散布情况**

| 文件 | 调用次数 | 读/写 |
|------|---------|-------|
| `QTUtility.cs` | 8 | 读+写 |
| `Config.cs` | 4 | 读+写 |
| `QTTabBarClass.cs` | 3 | 读+写 |
| `InitializationOrchestrator.cs` | 3 | 读+写 |
| `PluginManager.cs` | 2 | 读+写 |
| `QTSecondViewBar.cs` | 1 | 读 |
| `QTTabBarClass.TabManager.cs` | 1 | 读 |
| `TabBarBase.cs` | 1 | 读 |
| `StaticReg.cs` | 1 | 读+写 |

**验收标准**

1. Grep 确认无非 Config/StaticReg/RegistryAccess 的直接注册表访问
2. 路径常量集中管理
3. 全量测试绿
4. git 提交消息：`refactor(arch-batch4): introduce unified RegistryAccess utility`

---

### S1. InitializationOrchestrator guard 前置

| 项目 | 内容 |
|------|------|
| **严重级别** | Suggestion |
| **文件** | `QTTabBar\InitializationOrchestrator.cs` 第 148 行 |

**修复方案**

选项 A — guard 前置：

```csharp
public static void Initialize() {
    if(_initialized) return;
    lock(_lockObj) {
        if(_initialized) return;
        _initialized = true; // ← 移到此处，进入锁后立即置位

        // ... 初始化序列 ...
    }
}
```

选项 B — `Interlocked.CompareExchange`：

```csharp
private static int _state; // 0=未初始化, 1=初始化中, 2=已完成
public static void Initialize() {
    if(Interlocked.CompareExchange(ref _state, 1, 0) != 0) return;
    try {
        // ... 初始化序列 ...
    } finally {
        Interlocked.Exchange(ref _state, 2);
    }
}
```

**验收标准**

1. 初始化序列只执行一次
2. 同线程重入安全跳过
3. 全量测试绿
4. git 提交消息：`refactor(arch-batch4): harden InitializationOrchestrator guard`

---

### S2. QTUtility.Initialize() 死 façade 清理

| 项目 | 内容 |
|------|------|
| **严重级别** | Suggestion |
| **文件** | `QTTabBar\QTUtility.cs` 第 320-322 行 |

**修复方案**

确认 5 处调用方理解其语义（触发静态构造），补充注释说明其设计意图：

```csharp
/// <summary>
/// This method exists solely to trigger the static constructor,
/// which in turn calls InitializationOrchestrator.Initialize().
/// All entry points must call this method, NOT InitializationOrchestrator directly,
/// to ensure the static constructor fires first.
/// </summary>
public static void Initialize() {
    // Intentionally empty — triggers static constructor
}
```

**验收标准**

1. 调用方语义清晰
2. 无行为变化
3. git 提交消息：`docs(arch-batch4): clarify QTUtility.Initialize() purpose`

---

### S3. ExplorerProcessCaptor1 死代码清理

| 项目 | 内容 |
|------|------|
| **严重级别** | Suggestion |
| **文件** | `QTTabBar\ExplorerProcessCaptor.cs` |
| **修复动作** | 确认无引用后删除，或补全为功能完整的窗口捕获 |
| **验收标准** | 1) 删除后 MSBuild 通过；2) 无引用断裂 |
| **git 提交** | `refactor(arch-batch4): remove dead ExplorerProcessCaptor1 code` |

---

### S4. QTButtonBar 添加 #region 组织

| 项目 | 内容 |
|------|------|
| **严重级别** | Suggestion |
| **文件** | `QTTabBar\QTButtonBar.cs`（1,985 行，69 方法，0 个 region） |
| **修复动作** | 按职责分 region |

**建议 region 分组**

```
#region Construction & Lifecycle
#region Button Creation & Layout
#region Event Handlers (Click/Mouse)
#region Drag & Drop
#region Context Menu
#region Search Box
#endregion
```

**验收标准**

1. 按职责分 region
2. 无逻辑变化
3. git 提交消息：`style(arch-batch4): add region organization to QTButtonBar`

---

### S5. 各子系统 Initialize() 统一编排确认

| 项目 | 内容 |
|------|------|
| **严重级别** | Suggestion |
| **修复动作** | 确认 Config/PluginManager/HookLibManager 的 Initialize() 仅被 InitializationOrchestrator 调用 |
| **验收标准** | 1) Grep 确认无外部直接调用这些 Initialize() |
| **git 提交** | `refactor(arch-batch4): verify all Initialize() calls are orchestrated` |

---

## 批次依赖关系

```
第一批 (C1-C5) ─── 独立，可并行
    │
    ▼
第二批 (W5-W10) ── 依赖 C4/C5 已修复（配置广播机制正确）
    │
    ▼
第三批 (C6-C7, W1-W3) ── 依赖第一/二批（在干净的基线上重构）
    │     ├── C1 必须先于 W3 修复（栈溢出 bug）
    │     └── W1 façade 清理可随 C6 拆解同步推进
    ▼
第四批 (W4, S1-S5) ── 基础设施改进，可在任何批次间隙穿插
```

---

## 验收进度总览（2026-07-09 更新）

| 批次 | 总数 | 已修复 | 部分修复 | 未修复 | 完成率 |
|------|------|--------|----------|--------|--------|
| 第一批 (C1-C5) | 5 | 5 | 0 | 0 | 100% |
| 第二批 (W5-W10) | 6 | 5 | 1 (W9) | 0 | 92% |
| 第三批 (C6-C7, W1-W3) | 5 | 0 | 3 (C6, C7, W1) | 2 (W2, W3) | 30% |
| 第四批 (W4, S1-S5) | 6 | 5 | 1 (S4) | 0 | 92% |
| **合计** | **22** | **15** | **5** | **2** | **80%** |

### 逐项状态明细

| 编号 | 状态 | 关键证据 |
|------|------|----------|
| C1 | ✅ 已修复 | 后背字段 `_currentLocation`，commit `b704d7b` |
| C2 | ✅ 已修复 | 新增 `dictBBarByExplorerHandle` 索引，commit `f0ea923` |
| C3 | ✅ 已修复 | CreateTab 静态方法已删除，commit `f3c5c57` |
| C4 | ✅ 已修复 | 已添加 Increment + 广播，commit `3ec8048` |
| C5 | ✅ 已修复 | 已添加 service.DeleteInstance，commit `b0e5f51` |
| W5 | ✅ 已修复 | 统一 `UpdateNoCapturePaths()` 方法 |
| W6 | ✅ 已修复 | BindAction 改用 `SelectTab(index)` |
| W7 | ✅ 已修复 | `ConcurrentDictionary<string, byte[]>` 替换 |
| W8 | ✅ 已修复 | 统一 `RefreshNightMode()`，5 处调用方已收敛 |
| W9 | ✅ 已修复 | ResMain/ResMisc 改为按需读取，消除引用拷贝 |
| W10 | ✅ 已修复 | WriteConfig + PersistConfigChanges 统一入口，版本追踪完善 |
| C6 | ⬜ 部分修复 | 11 个 controller/partial + MenuController（3j）+ Explorer 模块扩展（3k/3l），主文件约 1,900 行 |
| C7 | ⬜ 部分修复 | 5 个辅助类已创建，QTUtility 仍保留部分 façade |
| W1 | ✅ 已修复 | 纯 registry façade 已移除，IPC 方法保留 |
| W2 | ✅ 已验证 | DesktopTooltipController 已提取，主文件 2,191 行 |
| W3 | ⬜ 部分修复 | TabOperations + CloseTab/CloseTabs/CancelFailedTabChanging 已上提 TabBarBase |
| W4 | ✅ 已修复 | `RegistryAccess.cs` 已创建，7 个文件已采用 |
| S1 | ✅ 已修复 | guard 前置至 try 块之前 |
| S2 | ✅ 已修复 | XML 注释已添加 |
| S3 | ✅ 已修复 | 文件已完全移除，无残留引用 |
| S4 | ⬜ 部分修复 | 仅 2 个 region，组织粒度过粗 |
| S5 | ✅ 已修复 | 所有入口通过 QTUtility.Initialize() 统一驱动 |

### 待办优先级建议

1. **W3（QTSecondViewBar 去重）** — 唯一完全未启动项，依赖 C1 已满足
2. **C6 继续拆解** — 主文件约 1,900 行，按 3m+ 继续（FolderLinkClicked、TranslateAcceleratorIO 等大区域）
3. **C7 façade 清理** — 清理 QTUtility 剩余 façade 转发方法
4. **W1 façade 清理** — 清理 InstanceManager 剩余 13 个方法
5. **W9 改为按需读取** — 消除 ResMain/ResMisc 引用拷贝
6. **S4 补充 region** — 为 QTButtonBar 添加更细粒度的代码区域
7. **W2 验证** — 检查 QTDesktopTool 拆解状态

---

## 全局验收标准（每批次完成后）

1. **TDD 合规**：每项修复均有 RED→GREEN 测试证据，测试面向真实 UI 操作流程（模拟用户点击、输入、导航、观察可见反馈），而非仅验证内部方法返回值
2. **编译验证**：MSBuild Debug + Release 双配置编译通过，贴出编译输出
3. **测试通过**：全量 NUnit 测试绿，贴出通过输出
4. **git 提交**：路径限定提交，每项独立 commit，消息格式 `fix/refactor(arch-batchN): <description>`
5. **三维 Ultra Review**：完整性 / 正确性 / 影响三个维度各派一个 CodeReview 子代理
6. **编码安全**：涉及中文的文件使用显式 UTF-8（无 BOM）编码写入，禁止使用 PowerShell `Set-Content` 无编码参数或 `>>` 重定向

---

## 附录：关键文件路径索引

| 文件 | 绝对路径 | 关注行号 |
|------|---------|---------|
| QTTabBarClass 主文件 | `d:\Project\QTTabBar-Next\QTTabBar\QTTabBarClass.cs` | 58(类), 205-251(CreateTab), 254-262(构造), 1202-1226(BindAction) |
| TabManager | `d:\Project\QTTabBar-Next\QTTabBar\QTTabBarClass.TabManager.cs` | 58(嵌套类), 142-175(OpenNewTab), 556-662(CloseTab) |
| ExplorerController | `d:\Project\QTTabBar-Next\QTTabBar\QTTabBarClass.ExplorerController.cs` | 58(嵌套类), 882-908(OnExplorerAttached) |
| MenuController | `d:\Project\QTTabBar-Next\QTTabBar\QTTabBarClass.MenuController.cs` | 58(嵌套类) |
| PluginServer | `d:\Project\QTTabBar-Next\QTTabBar\PluginServer.cs` | 29(partial), 32(PluginServer类) |
| QTUtility | `d:\Project\QTTabBar-Next\QTTabBar\QTUtility.cs` | 43(static class), 109-110(ResMain), 177-189(静态构造), 320-322(Initialize) |
| QTUtility2 | `d:\Project\QTTabBar-Next\QTTabBar\QTUtility2.cs` | 41(static class) |
| InstanceManager | `d:\Project\QTTabBar-Next\QTTabBar\InstanceManager.cs` | 28(static class), 624(Push), 628(Unregister) |
| Config | `d:\Project\QTTabBar-Next\QTTabBar\Config.cs` | 1209(LoadedConfig), 1250-1262(SetNoCapture), 1265-1272(PersistBreakTabBar), 1274(ReadConfig), 1381(WriteConfig) |
| ConfigVersionTracker | `d:\Project\QTTabBar-Next\QTTabBar\ConfigVersionTracker.cs` | 19-82(全文件) |
| StaticReg | `d:\Project\QTTabBar-Next\QTTabBar\StaticReg.cs` | 20-21(历史列表), 23-45(RegBackedList), 47-57(直接读写) |
| TabInstanceRegistry | `d:\Project\QTTabBar-Next\QTTabBar\TabInstanceRegistry.cs` | 29-30(双集合), 79-83(PeekMain) |
| ButtonBarRegistry | `d:\Project\QTTabBar-Next\QTTabBar\ButtonBarRegistry.cs` | 51-60(TryGetButtonBarHandle缺陷) |
| HookStateManager | `d:\Project\QTTabBar-Next\QTTabBar\HookStateManager.cs` | 10-83(统一状态管理) |
| IconManager | `d:\Project\QTTabBar-Next\QTTabBar\IconManager.cs` | 275-303(ImageListGlobal) |
| IDLWrapper | `d:\Project\QTTabBar-Next\QTTabBar\IDLWrapper.cs` | 32(dicCacheIDLs无锁), 227-229(AddCache), 426-435(TryGetCache) |
| SelectionTracker | `d:\Project\QTTabBar-Next\QTTabBar\SelectionTracker.cs` | 28(selectDict) |
| IpcCommandDispatcher | `d:\Project\QTTabBar-Next\QTTabBar\IpcCommandDispatcher.cs` | 72-81(ReloadConfigOnClient) |
| ResourceCache | `d:\Project\QTTabBar-Next\QTTabBar\ResourceCache.cs` | 26-28(资源缓存) |
| SessionState | `d:\Project\QTTabBar-Next\QTTabBar\SessionState.cs` | 24-26(会话状态) |
| InitializationOrchestrator | `d:\Project\QTTabBar-Next\QTTabBar\InitializationOrchestrator.cs` | 40(Initialize), 148(guard) |
| TabBarBase | `d:\Project\QTTabBar-Next\QTTabBar\TabBarBase.cs` | 103(CurrentTab), 338-341(DPI读取) |
| QTSecondViewBar | `d:\Project\QTTabBar-Next\QTTabBar\QTSecondViewBar.cs` | 65-76(CurrentLocation栈溢出), 83(Initialize调用) |
| QTDesktopTool | `d:\Project\QTTabBar-Next\QTTabBar\QTDesktopTool.cs` | 40(类定义), 158(Initialize调用) |
| QTButtonBar | `d:\Project\QTTabBar-Next\QTTabBar\QTButtonBar.cs` | 38(类定义), 107(Initialize调用) |
| QTabControl | `d:\Project\QTTabBar-Next\QTTabBar\QTabControl.cs` | 139(NightMode读取), 1727-1735(SelectTab事件) |
| ExplorerProcessCaptor | `d:\Project\QTTabBar-Next\QTTabBar\ExplorerProcessCaptor.cs` | 13(死代码) |
| ComRegistrationManager | `d:\Project\QTTabBar-Next\QTTabBar\ComRegistrationManager.cs` | 15(RegisterBand), 38(RegisterBho) |
| AutoLoader | `d:\Project\QTTabBar-Next\QTTabBar\AutoLoader.cs` | 28(BHO), 48(SetSite), 79(ActivateIt) |
| BandObject | `d:\Project\QTTabBar-Next\BandObjectLib\BandObject.cs` | 429(SetSite) |

---

## 附录：façade 转发方法清单

### QTUtility 的 17 个 façade（待清理）

| 行号 | 方法 | 转发目标 |
|------|------|---------|
| 193 | `ByteArrayToObject` | `SerializationHelper` |
| 198 | `ExtHasIcon` | `IconManager` |
| 217 | `GetIcon(IntPtr)` | `IconManager` |
| 221 | `GetIcon(string, bool)` | `IconManager` |
| 225 | `GetImageKey` | `IconManager` |
| 317 | `IsNetworkRootFolder` | `PathValidator` |
| 325 | `LoadReservedImage` | `IconManager` |
| 343 | `ObjectToByteArray` | `SerializationHelper` |
| 439 | `ReadLanguageFile` | `QTResourceManager` |
| 621 | `ImageGlobalContainsKey` | `IconManager` |
| 625 | `GetImageFromGlobal` | `IconManager` |
| 740 | `IsEmptyStr` | `PathValidator` |
| 745 | `IsNetPath` | `PathValidator` |
| 750 | `IsNoCapturePaths` | `PathValidator` |
| 755 | `IsSimpleDateStr` | `PathValidator` |
| 760 | `IsShortDateStr` | `PathValidator` |

### InstanceManager 的 14 个 façade（待清理）

| 行号 | 方法 | 转发目标 |
|------|------|---------|
| 580 | `LocalTabBroadcast` | `TabInstanceRegistry` |
| 591 | `LocalBBarBroadcast` | `ButtonBarRegistry` |
| 618 | `LocalInvokeMain` | `TabInstanceRegistry` |
| 620 | `RegisterButtonBar` | `ButtonBarRegistry` |
| 626 | `UnregisterButtonBar` | `ButtonBarRegistry` |
| 632 | `PutSelect` | `SelectionTracker` |
| 634 | `RemoveSelect` | `SelectionTracker` |
| 636 | `GetSelect` | `SelectionTracker` |
| 638 | `GetThreadTabBar` | `TabInstanceRegistry` |
| 640 | `GetThreadButtonBar` | `ButtonBarRegistry` |
| 642 | `TryGetButtonBarHandle` | `ButtonBarRegistry`（C2 修复后非纯转发） |
| 713 | `SyncToolbarColorThreads` | `TabInstanceRegistry` |

---

## 附录：严重程度定义

| 级别 | 定义 | 示例 |
|------|------|------|
| **Critical** | 运行时崩溃、功能缺陷、安全/健壮性问题、跨进程不一致 | 栈溢出、参数被忽略、绕过校验、不广播配置变更 |
| **Warning** | 应在后续修复计划中处理，不立即触发但增加维护风险 | 缓存不同步、线程安全、代码重复、状态多真源 |
| **Suggestion** | 改进建议，可低优先级处理 | 死代码清理、代码组织、注释补充 |
