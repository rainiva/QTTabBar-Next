# Task 9：输入责任簇窄 Host 设计

## 决策

Task 9 不使用单一 `ITabInputHost`。现有十个输入、键盘、鼠标、拖放和文件夹树源文件直接使用 62 个 owner 成员；将它们汇入一个接口会形成新的上帝接口，并且无法满足接口成员不超过 12 个的治理目标。

改为按职责建立五个内部 host：

- `IDragDropHost`：标签命中、拖放提示和已放入路径处理。
- `IDroppedFilesHost`：Explorer 前台激活和应用菜单的生命周期。
- `IFolderTreeHost`：UI 调度、Explorer 重绘和文件夹树可见性。
- `IListViewInputHost`：列表视图事件、选择同步和插件选择通知。
- `IHookInputHost`：Win32 hook、键盘、鼠标、Explorer 消息和导航操作。

每个 controller 只接受所属 host；禁止 controller 再接收 `QTTabBarClass`，禁止 host 暴露未被该责任簇使用的 owner 字段。

## 迁移顺序

1. `DragDropController`：最少的八项依赖，验证拖入标签和放入文件夹。
2. `DroppedFilesController`：三个依赖，验证应用菜单。
3. `FolderTreeController`：四个依赖，验证显示/隐藏与重绘。
4. `ListViewInputController`：保留现有三个 partial 文件，收敛列表事件和选择行为。
5. `HookInputController`：保留现有三个 partial 文件，最后收敛 hook、键盘和鼠标操作。

每一簇独立 RED、GREEN、编译、特征测试和提交；不得以聚合接口代替职责边界。

## 验收

- 五个外层 controller 都是程序集顶层类型。
- 每个构造函数只接收其窄 host。
- 不保留 `QTTabBarClass` nested controller 定义或旧编译项。
- `TopLevelInputControllerTests`、输入/键盘/拖放/Explorer 回归测试、Debug 与 Release 构建通过。
- 真实 Explorer 操作覆盖键盘切换、列表中键、文件拖入和文件夹树显示/隐藏。
