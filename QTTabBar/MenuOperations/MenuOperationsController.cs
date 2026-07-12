using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class MenuOperationsController {
        private readonly IMenuContext _menuContext;
        private readonly IExplorerContext _explorerContext;
        private readonly IMenuStripHost _strip;
        private readonly IMenuServicesHost _services;
        private readonly IMenuOperationsHost _host;

        public MenuOperationsController(
        IMenuContext menuContext,
        IExplorerContext explorerContext,
        IMenuStripHost strip,
        IMenuServicesHost services,
        IMenuOperationsHost host) {
        _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
        _explorerContext = explorerContext ?? throw new ArgumentNullException(nameof(explorerContext));
        _strip = strip ?? throw new ArgumentNullException(nameof(strip));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
                QTabItem item = fCurrent ? _menuContext.CurrentTab : _menuContext.ContextMenuedTab;
                List<ToolStripItem> list = new List<ToolStripItem>();
                List<LogData> branches = item.Branches;
                if(branches.Count > 0) {
                    ToolStripMenuItem item2 = new ToolStripMenuItem(ResourceCache.ResMain[0x18]);
                    item2.Tag = item;
                    item2.DropDown = new DropDownMenuBase(container, true, true);
                    item2.DropDown.ImageList = ResourceCache.ImageListGlobal;
                    item2.DropDownItemClicked += itemClickedEvent;
                    int index = -1;
                    foreach(LogData data in branches) {
                        index++;
                        if(_host.IsSpecialFolderNeedsToTravel(data.Path)) {
                            if(_services.LogEntryDic.ContainsKey(data.Hash)) {
                                goto Label_00B3;
                            }
                            continue;
                        }
                        if(!QTUtility2.PathExists(data.Path)) {
                            continue;
                        }
                    Label_00B3:
                        item2.DropDownItems.Add(MenuUtility.CreateMenuItem(new MenuItemArguments(data.Path, false, index, MenuGenre.Branch)));
                    }
                    if(item2.DropDownItems.Count > 0) {
                        list.Add(new ToolStripSeparator());
                        list.Add(item2);
                    }
                }
                return list;
        }

        public List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) {
                QTabItem item = fCurrent ? _menuContext.CurrentTab : _menuContext.ContextMenuedTab;
                List<QMenuItem> list = new List<QMenuItem>();
                string[] historyBack = item.GetHistoryBack();
                string[] historyForward = item.GetHistoryForward();
                if((historyBack.Length + historyForward.Length) > 1) {
                    for(int i = historyBack.Length - 1; i >= 0; i--) {
                        QMenuItem item2 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyBack[i], true, i, MenuGenre.Navigation));
                        if(_host.IsSpecialFolderNeedsToTravel(historyBack[i])) {
                            item2.Enabled = _services.LogEntryDic.ContainsKey(item.GetLogHash(true, i));
                        }
                        else if(!QTUtility2.PathExists(historyBack[i])) {
                            item2.Enabled = false;
                        }
                        if(item2.Enabled && (i == 0)) {
                            item2.BackColor = QTUtility2.MakeModColor(SystemColors.Highlight);
                        }
                        list.Add(item2);
                    }
                    for(int j = 0; j < historyForward.Length; j++) {
                        QMenuItem item3 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyForward[j], false, j, MenuGenre.Navigation));
                        if(_host.IsSpecialFolderNeedsToTravel(historyForward[j])) {
                            item3.Enabled = _services.LogEntryDic.ContainsKey(item.GetLogHash(false, j));
                        }
                        else if(!QTUtility2.PathExists(historyForward[j])) {
                            item3.Enabled = false;
                        }
                        list.Add(item3);
                    }
                }
                return list;
        }

        public void MenuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                string groupName = e.ClickedItem.Text;
                string currentPath = _menuContext.ContextMenuedTab.CurrentPath;
                bool addSame = Control.ModifierKeys == Keys.Control;
                Group g = GroupsManager.GetGroup(groupName);
                if(g == null) return;
                if(addSame || !g.Paths.Any(p => p.PathEquals(currentPath))) {
                    g.Paths.Add(currentPath);
                    GroupsManager.SaveGroups();
                }
        }

        public void MenuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                try {
                    string toolTipText = e.ClickedItem.ToolTipText;
                    ProcessStartInfo startInfo = new ProcessStartInfo(toolTipText);
                    startInfo.WorkingDirectory = Path.GetDirectoryName(toolTipText);
                    startInfo.ErrorDialog = true;
                    startInfo.ErrorDialogParentHandle = _explorerContext.ExplorerHandle;
                    Process.Start(startInfo);
                    StaticReg.ExecutedPathsList.Add(toolTipText);
                }
                catch {
                    SoundFeedbackService.SoundPlay();
                }
        }

        public void MenuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                using(IDLWrapper wrapper = new IDLWrapper(e.ClickedItem.ToolTipText)) {
                    e.HRESULT = _services.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : Control.MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                }
                if(e.HRESULT == 0xffff) {
                    StaticReg.ExecutedPathsList.Remove(e.ClickedItem.ToolTipText);
                    e.ClickedItem.Dispose();
                }
        }

        public void MenuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                Keys modifierKeys = Control.ModifierKeys;
                string groupName = e.ClickedItem.Text;
                if(modifierKeys == (Keys.Control | Keys.Shift)) {
                    Group g = GroupsManager.GetGroup(groupName);
                    g.Startup = !g.Startup;
                    GroupsManager.SaveGroups();
                }
                else {
                    _host.OpenGroup(groupName, modifierKeys == Keys.Control, false);
                }
        }

        public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
                GroupsManager.HandleReorder(_strip.tsmiGroups.DropDownItems.Cast<ToolStripItem>());
                QTTabBarClass.SyncTaskBarMenu();
        }

        public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if((_menuContext.ContextMenuedTab != null) && (clickedItem != null)) {
                    MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                    switch(Control.ModifierKeys) {
                        case Keys.Shift:
                            _host.CloneTabButton(_menuContext.ContextMenuedTab, null, true, -1);
                            _host.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;

                        case Keys.Control: {
                                using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                    _host.OpenNewWindow(wrapper);
                                    return;
                                }
                            }
                        default:
                            _menuContext.TabControl.SelectTab(_menuContext.ContextMenuedTab);
                            _host.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;
                    }
                }
        }

        public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _services.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : Control.MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                    }
                    if(e.HRESULT == 0xffff) {
                        StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                        e.ClickedItem.Dispose();
                    }
                }
        }

        public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
                _host.ReplaceByGroup(e.ClickedItem.Text);
        }

        public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
                QTLogger.log("QTTabBarClass FolderLinkClicked");
                MouseChord chord = QTUtility.MakeMouseChord(middle ? MouseChord.Middle : MouseChord.Left, modifierKeys);
                BindAction action;
                if(Config.Mouse.LinkActions.TryGetValue(chord, out action)) {
                    _host.DoBindAction(action, false, null, wrapper);
                    return true;
                }
                QTLogger.log("QTTabBarClass FolderLinkClicked 未获取到配置的动作");
                return false;
        }

        public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(e.ClickedItem == _strip.tsmiOption) {
                    OptionsDialog.Open();
                }
                else if(e.ClickedItem == _strip.tsmiCloseAllButCurrent) {
                    if(_menuContext.TabControl.TabCount != 1) {
                        _host.CloseAllTabsExcept(_menuContext.CurrentTab);
                    }
                }
                else if(e.ClickedItem == _strip.tsmiBrowseFolder) {
                    _host.ChooseNewDirectory();
                }
                else if(e.ClickedItem == _strip.tsmiCloseWindow) {
                    {
                        LockedTabsService.PersistFromTabs(_menuContext.TabControl.TabPages);
                    }
                    WindowUtils.CloseExplorer(_explorerContext.ExplorerHandle, 1);
                }
                else {
                    if(e.ClickedItem == _strip.tsmiLastActiv) {
                        try {
                            _menuContext.TabControl.SelectTab(_services.lstActivatedTabs[_services.lstActivatedTabs.Count - 2]);
                            return;
                        }
                        catch (Exception ex)
                        {
                            QTLogger.MakeErrorLog(ex, "tabControl1.SelectTab");
                            return;
                        }
                    }
                    if(e.ClickedItem == _strip.tsmiLockToolbar) {
                        _services.rebarController.Locked = !_strip.tsmiLockToolbar.Checked;
                    }
                    else if(e.ClickedItem == _strip.tsmiMergeWindows) {
                        _host.MergeAllWindows();
                    }
                }
        }

        // 右键（系统）菜单打开时触发
        public void contextMenuSys_Opening(object sender, CancelEventArgs e) {
                InitializeSysMenu(false);
                // 延迟加载菜单内容
                _strip.contextMenuSys.SuspendLayout();
                _strip.tsmiGroups.DropDown.SuspendLayout();
                _strip.tsmiUndoClose.DropDown.SuspendLayout();

                MenuUtility.CreateGroupItems(_strip.tsmiGroups);
                MenuUtility.CreateUndoClosedItems(_strip.tsmiUndoClose);
                if((_services.lstActivatedTabs.Count > 1) && _menuContext.TabControl.TabPages.Contains(_services.lstActivatedTabs[_services.lstActivatedTabs.Count - 2])) {
                    _strip.tsmiLastActiv.ToolTipText = _services.lstActivatedTabs[_services.lstActivatedTabs.Count - 2].CurrentPath;
                    _strip.tsmiLastActiv.Enabled = true;
                }
                else {
                    _strip.tsmiLastActiv.ToolTipText = string.Empty;
                    _strip.tsmiLastActiv.Enabled = false;
                }
                while(_strip.tsmiExecuted.DropDownItems.Count > 0) {
                    _strip.tsmiExecuted.DropDownItems[0].Dispose();
                }
                List<ToolStripItem> list = MenuUtility.CreateRecentFilesItems();
                if(list.Count > 0) {
                    _strip.tsmiExecuted.DropDown.SuspendLayout();
                    _strip.tsmiExecuted.DropDownItems.AddRange(list.ToArray());
                    _strip.tsmiExecuted.DropDown.ResumeLayout();
                }
                _strip.tsmiExecuted.Enabled = _strip.tsmiExecuted.DropDownItems.Count > 0;
                _strip.tsmiMergeWindows.Enabled = InstanceManager.GetTotalInstanceCount() > 1;
                _strip.tsmiLockToolbar.Checked = _services.rebarController.Locked;
                if((_services.lstPluginMenuItems_Sys != null) && (_services.lstPluginMenuItems_Sys.Count > 0)) {
                    foreach(ToolStripItem item in _services.lstPluginMenuItems_Sys) {
                        item.Dispose();
                    }
                    _services.lstPluginMenuItems_Sys = null;
                }
                if((_services.pluginServer != null) && (_services.pluginServer.dicFullNamesMenuRegistered_Sys.Count > 0)) {
                    _services.lstPluginMenuItems_Sys = new List<ToolStripItem>();
                    int index = _strip.contextMenuSys.Items.IndexOf(_strip.tsmiOption);
                    ToolStripSeparator separator = new ToolStripSeparator();
                    _strip.contextMenuSys.Items.Insert(index, separator);
                    foreach(string str in _services.pluginServer.dicFullNamesMenuRegistered_Sys.Keys) {
                        ToolStripMenuItem item2 = new ToolStripMenuItem(_services.pluginServer.dicFullNamesMenuRegistered_Sys[str]);
                        item2.Name = str;
                        item2.Tag = MenuType.Bar;
                        item2.Click += _services._pluginMenuController.PluginItemsClick;
                        _strip.contextMenuSys.Items.Insert(index, item2);
                        _services.lstPluginMenuItems_Sys.Add(item2);
                    }
                    _services.lstPluginMenuItems_Sys.Add(separator);
                }
                _strip.tsmiUndoClose.DropDown.ResumeLayout();
                _strip.tsmiGroups.DropDown.ResumeLayout();
                _strip.contextMenuSys.ResumeLayout();
        }

        public void InitializeSysMenu(bool fText) {
                bool flag = false;
                if(_strip.tsmiGroups == null) {
                    flag = true;
                    _strip.tsmiGroups = new ToolStripMenuItem(ResourceCache.ResMain[12]);
                    _strip.tsmiUndoClose = new ToolStripMenuItem(ResourceCache.ResMain[13]);
                    _strip.tsmiLastActiv = new ToolStripMenuItem(ResourceCache.ResMain[14]);
                    _strip.tsmiExecuted = new ToolStripMenuItem(ResourceCache.ResMain[15]);
                    _strip.tsmiBrowseFolder = new ToolStripMenuItem(ResourceCache.ResMain[0x10] + "...");
                    _strip.tsmiCloseAllButCurrent = new ToolStripMenuItem(ResourceCache.ResMain[0x11]);
                    _strip.tsmiCloseWindow = new ToolStripMenuItem(ResourceCache.ResMain[0x12]);
                    _strip.tsmiOption = new ToolStripMenuItem(ResourceCache.ResMain[0x13]);
                    _strip.tsmiLockToolbar = new ToolStripMenuItem(ResourceCache.ResMain[0x20]);
                    _strip.tsmiMergeWindows = new ToolStripMenuItem(ResourceCache.ResMain[0x21]);
                    _strip.tssep_Sys1 = new ToolStripSeparator();
                    _strip.tssep_Sys2 = new ToolStripSeparator();
                    if(_strip.contextMenuSys != null) {
                        _strip.contextMenuSys.SuspendLayout();
                        _strip.contextMenuSys.Items[0].Dispose();
                        _strip.contextMenuSys.Items.AddRange(new ToolStripItem[]
                        {
                            _strip.tsmiGroups, _strip.tsmiUndoClose, _strip.tsmiLastActiv, _strip.tsmiExecuted,
                            _strip.tssep_Sys1, _strip.tsmiBrowseFolder, _strip.tsmiCloseAllButCurrent, _strip.tsmiCloseWindow,
                            _strip.tsmiMergeWindows, _strip.tsmiLockToolbar, _strip.tssep_Sys2, _strip.tsmiOption
                        });
                    }

                    DropDownMenuReorderable reorderable = new DropDownMenuReorderable(_strip.components, true, false);
                    reorderable.ReorderFinished += MenuitemGroups_ReorderFinished;
                    reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                    reorderable.ItemMiddleClicked += DdrmrGroups_ItemMiddleClicked;
                    reorderable.ImageList = ResourceCache.ImageListGlobal;
                    _strip.tsmiGroups.DropDown = reorderable;
                    _strip.tsmiGroups.DropDownItemClicked += MenuitemGroups_DropDownItemClicked;
                    DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(_strip.components);
                    reorderable2.ReorderEnabled = false;
                    reorderable2.MessageParent = _strip.Handle;
                    reorderable2.ImageList = ResourceCache.ImageListGlobal;
                    reorderable2.ItemRightClicked += DdmrUndoClose_ItemRightClicked;
                    _strip.tsmiUndoClose.DropDown = reorderable2;
                    _strip.tsmiUndoClose.DropDownItemClicked += _strip.menuitemUndoClose_DropDownItemClicked;
                    DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(_strip.components);
                    reorderable3.MessageParent = _strip.Handle;
                    reorderable3.ItemRightClicked += MenuitemExecuted_ItemRightClicked;
                    reorderable3.ItemClicked += MenuitemExecuted_DropDownItemClicked;
                    reorderable3.ImageList = ResourceCache.ImageListGlobal;
                    _strip.tsmiExecuted.DropDown = reorderable3;
                    _strip.tssep_Sys1.Enabled = false;
                    _strip.tssep_Sys2.Enabled = false;
                    if(_strip.contextMenuSys != null) {
                        _strip.contextMenuSys.ResumeLayout(false);
                    }
                }
                if(!flag && fText) {
                    _strip.tsmiGroups.Text = ResourceCache.ResMain[12];
                    _strip.tsmiUndoClose.Text = ResourceCache.ResMain[13];
                    _strip.tsmiLastActiv.Text = ResourceCache.ResMain[14];
                    _strip.tsmiExecuted.Text = ResourceCache.ResMain[15];
                    _strip.tsmiBrowseFolder.Text = ResourceCache.ResMain[0x10] + "...";
                    _strip.tsmiCloseAllButCurrent.Text = ResourceCache.ResMain[0x11];
                    _strip.tsmiCloseWindow.Text = ResourceCache.ResMain[0x12];
                    _strip.tsmiOption.Text = ResourceCache.ResMain[0x13];
                    _strip.tsmiLockToolbar.Text = ResourceCache.ResMain[0x20];
                    _strip.tsmiMergeWindows.Text = ResourceCache.ResMain[0x21];
                }
        }

        public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(_menuContext.ContextMenuedTab != null) {
                    if(e.ClickedItem == _strip.tsmiClose) {
                        if(_menuContext.TabControl.TabCount == 1) {
                            {
                                LockedTabsService.PersistFromTabs(_menuContext.TabControl.TabPages);
                            }
                            WindowUtils.CloseExplorer(_explorerContext.ExplorerHandle, 1);
                        }
                        else {
                            _host.CloseTab(_menuContext.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _strip.tsmiCloseAllButThis) {
                        _host.CloseAllTabsExcept(_menuContext.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _strip.tsmiCloseLeft) {
                        int index = _menuContext.TabControl.TabPages.IndexOf(_menuContext.ContextMenuedTab);
                        if(index > 0) {
                            _host.CloseLeftRight(true, index);
                        }
                    }
                    else if(e.ClickedItem == _strip.tsmiCloseRight) {
                        int num2 = _menuContext.TabControl.TabPages.IndexOf(_menuContext.ContextMenuedTab);
                        if(num2 >= 0) {
                            _host.CloseLeftRight(false, num2);
                        }
                    }
                    else if(e.ClickedItem == _strip.tsmiCreateGroup) {
                        CreateGroup(_menuContext.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _strip.tsmiLockThis) {
                        LockedTabsService.ToggleTab(
                            _menuContext.ContextMenuedTab,
                            _menuContext.TabControl.TabPages.Cast<QTabItem>());
                    }
                    else if(e.ClickedItem == _strip.tsmiCloneThis) {
                        _host.CloneTabButton(_menuContext.ContextMenuedTab, null, true, -1);
                    }
                    else if(e.ClickedItem == _strip.tsmiCreateWindow) {
                        using(IDLWrapper wrapper = new IDLWrapper(_menuContext.ContextMenuedTab.CurrentIDL)) {
                            _host.OpenNewWindow(wrapper);
                        }
                        if(/*!Config.KeepOnSeparate != */ ((Control.ModifierKeys & Keys.Shift) != Keys.None)) {
                            _host.CloseTab(_menuContext.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _strip.tsmiCopy) {
                        string currentPath = _menuContext.ContextMenuedTab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        else if(currentPath.IndexOf("*?*?*") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                    }
                    else if(e.ClickedItem == _strip.tsmiProp) {
                        ShellMethods.ShowProperties(_menuContext.ContextMenuedTab.CurrentIDL);
                    }
                    else if (e.ClickedItem == _strip.tsmiOpenCmd) { // add by qwop.
                        _host.OpenCmd( null );
                    } else if (e.ClickedItem == _strip.enableApiHook)
                    {
                        _host.EnableApiHook();
                    }
                }
        }

        public void contextMenuTab_Opening(object sender, CancelEventArgs e) {
                try {
                    InitializeTabMenu(false);
                    int index = _menuContext.TabControl.TabPages.IndexOf(_menuContext.ContextMenuedTab);
                    if((index == -1) || (_menuContext.ContextMenuedTab == null)) {
                        e.Cancel = true;
                    }
                    else {
                        _menuContext.TabControl.SetContextMenuState(true);
                        _strip.contextMenuTab.SuspendLayout();
                        if(_menuContext.TabControl.TabCount == 1) {
                            _strip.tsmiTabOrder.Enabled = _strip.tsmiCloseAllButThis.Enabled = _strip.tsmiCloseLeft.Enabled = _strip.tsmiCloseRight.Enabled = false;
                        }
                        else {
                            if(index == 0) {
                                _strip.tsmiCloseLeft.Enabled = false;
                                _strip.tsmiCloseRight.Enabled = true;
                            }
                            else if(index == (_menuContext.TabControl.TabCount - 1)) {
                                _strip.tsmiCloseLeft.Enabled = true;
                                _strip.tsmiCloseRight.Enabled = false;
                            }
                            else {
                                _strip.tsmiCloseLeft.Enabled = _strip.tsmiCloseRight.Enabled = true;
                            }
                            _strip.tsmiTabOrder.Enabled = _strip.tsmiCloseAllButThis.Enabled = true;
                        }
                        _strip.tsmiClose.Enabled = !_menuContext.ContextMenuedTab.TabLocked;
                        _strip.tsmiLockThis.Text = _menuContext.ContextMenuedTab.TabLocked ? ResourceCache.ResMain[20] : ResourceCache.ResMain[6];
                        if(GroupsManager.GroupCount > 0) {
                            _strip.tsmiAddToGroup.DropDown.SuspendLayout();
                            _strip.tsmiAddToGroup.Enabled = true;
                            while(_strip.tsmiAddToGroup.DropDownItems.Count > 0) {
                                _strip.tsmiAddToGroup.DropDownItems[0].Dispose();
                            }
                            foreach(Group g in GroupsManager.Groups.Where(g => g.Paths.Count > 0)) {
                                _strip.tsmiAddToGroup.DropDownItems.Add(new ToolStripMenuItem(g.Name) {
                                    ImageKey = IconManager.GetImageKey(g.Paths[0], null)
                                });
                            }
                            _strip.tsmiAddToGroup.DropDown.ResumeLayout();
                        }
                        else {
                            _strip.tsmiAddToGroup.Enabled = false;
                        }
                        _strip.tsmiHistory.DropDown.SuspendLayout();
                        while(_strip.tsmiHistory.DropDownItems.Count > 0) {
                            _strip.tsmiHistory.DropDownItems[0].Dispose();
                        }
                        if((_menuContext.ContextMenuedTab.HistoryCount_Back + _menuContext.ContextMenuedTab.HistoryCount_Forward) > 1) {
                            _strip.tsmiHistory.DropDownItems.AddRange(CreateNavBtnMenuItems(false).ToArray());
                            _strip.tsmiHistory.DropDownItems.AddRange(CreateBranchMenu(false, _strip.components, _strip.tsmiBranchRoot_DropDownItemClicked).ToArray());
                            _strip.tsmiHistory.Enabled = true;
                        }
                        else {
                            _strip.tsmiHistory.Enabled = false;
                        }
                        _strip.tsmiHistory.DropDown.ResumeLayout();
                        _strip.contextMenuTab.Items.Remove(_strip.menuTextBoxTabAlias);
                        if(!Config.Tabs.RenameAmbTabs) {
                            _strip.contextMenuTab.Items.Insert(12, _strip.menuTextBoxTabAlias);
                            if(_menuContext.ContextMenuedTab.Comment.Length > 0) {
                                _strip.menuTextBoxTabAlias.Text = _menuContext.ContextMenuedTab.Comment;
                                _strip.menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
                            }
                            else {
                                _strip.menuTextBoxTabAlias.Text = ResourceCache.ResMain[0x1b];
                                _strip.menuTextBoxTabAlias.ForeColor = SystemColors.GrayText;
                            }
                            _strip.menuTextBoxTabAlias.Enabled = !_menuContext.TabControl.AutoSubText;
                        }
                        if(_strip.tsmiTabOrder.DropDownItems.Count == 0) {
                            ((ToolStripDropDownMenu)_strip.tsmiTabOrder.DropDown).ShowImageMargin = false;
                            ToolStripMenuItem item2 = new ToolStripMenuItem(ResourceCache.ResMain[0x1d]);
                            ToolStripMenuItem item3 = new ToolStripMenuItem(ResourceCache.ResMain[30]);
                            ToolStripMenuItem item4 = new ToolStripMenuItem(ResourceCache.ResMain[0x1f]);
                            ToolStripSeparator separator = new ToolStripSeparator();
                            ToolStripMenuItem item5 = new ToolStripMenuItem(ResourceCache.ResMain[0x22]);
                            item2.Name = "Name";
                            item3.Name = "Drive";
                            item4.Name = "Active";
                            separator.Enabled = false;
                            item5.Name = "Rev";
                            _strip.tsmiTabOrder.DropDownItems.Add(item2);
                            _strip.tsmiTabOrder.DropDownItems.Add(item3);
                            _strip.tsmiTabOrder.DropDownItems.Add(item4);
                            _strip.tsmiTabOrder.DropDownItems.Add(separator);
                            _strip.tsmiTabOrder.DropDownItems.Add(item5);
                            _strip.tsmiTabOrder.DropDownItemClicked += _strip.menuitemTabOrder_DropDownItemClicked;
                        }
                        if((_services.lstPluginMenuItems_Tab != null) && (_services.lstPluginMenuItems_Tab.Count > 0)) {
                            foreach(ToolStripItem item6 in _services.lstPluginMenuItems_Tab) {
                                item6.Dispose();
                            }
                            _services.lstPluginMenuItems_Tab = null;
                        }
                        if((_services.pluginServer != null) && (_services.pluginServer.dicFullNamesMenuRegistered_Tab.Count > 0)) {
                            _services.lstPluginMenuItems_Tab = new List<ToolStripItem>();
                            int num2 = _strip.contextMenuTab.Items.IndexOf(_strip.tsmiProp);
                            ToolStripSeparator separator2 = new ToolStripSeparator();
                            _strip.contextMenuTab.Items.Insert(num2, separator2);
                            foreach(string str3 in _services.pluginServer.dicFullNamesMenuRegistered_Tab.Keys) {
                                ToolStripMenuItem item7 = new ToolStripMenuItem(_services.pluginServer.dicFullNamesMenuRegistered_Tab[str3]);
                                item7.Name = str3;
                                item7.Tag = MenuType.Tab;
                                item7.Click += _services._pluginMenuController.PluginItemsClick;
                                _strip.contextMenuTab.Items.Insert(num2, item7);
                                _services.lstPluginMenuItems_Tab.Add(item7);
                            }
                            _services.lstPluginMenuItems_Tab.Add(separator2);
                        }
                        _strip.contextMenuTab.ResumeLayout();
                    }
                }
                catch (Exception ex) { QTLogger.MakeErrorLog(ex); }
        }

        // 创建标签分组
        public void CreateGroup(QTabItem contextMenuedTab) {
                _host.NowModalDialogShown = true;
                using(CreateNewGroupForm form = new CreateNewGroupForm(contextMenuedTab.CurrentPath, _menuContext.TabControl.TabPages)) {
                    // Application.EnableVisualStyles();
                    //  Application.SetCompatibleTextRenderingDefault(false);
                    // Application.Run(form);
                   form.TopMost = true;
                   form.ShowDialog();
                }
                _host.NowModalDialogShown = false;
        }

        public void InitializeTabMenu(bool fText) {
                try {
                    bool flag = false;
                    if(_strip.tsmiClose == null) {
                        flag = true;
                        _strip.tsmiClose = new ToolStripMenuItem(ResourceCache.ResMain[0]);
                        _strip.tsmiCloseRight = new ToolStripMenuItem(ResourceCache.ResMain[1]);
                        _strip.tsmiCloseLeft = new ToolStripMenuItem(ResourceCache.ResMain[2]);
                        _strip.tsmiCloseAllButThis = new ToolStripMenuItem(ResourceCache.ResMain[3]);
                        _strip.tsmiAddToGroup = new ToolStripMenuItem(ResourceCache.ResMain[4]);
                        _strip.tsmiCreateGroup = new ToolStripMenuItem(ResourceCache.ResMain[5] + "...");
                        _strip.tsmiLockThis = new ToolStripMenuItem(ResourceCache.ResMain[6]);
                        _strip.tsmiCloneThis = new ToolStripMenuItem(ResourceCache.ResMain[7]);
                        _strip.tsmiCreateWindow = new ToolStripMenuItem(ResourceCache.ResMain[8]);
                        _strip.tsmiCopy = new ToolStripMenuItem(ResourceCache.ResMain[9]);
                        _strip.tsmiProp = new ToolStripMenuItem(ResourceCache.ResMain[10]);
                        _strip.tsmiHistory = new ToolStripMenuItem(ResourceCache.ResMain[11]);
                        _strip.tsmiTabOrder = new ToolStripMenuItem(ResourceCache.ResMain[0x1c]);

                        int len = ResourceCache.ResMain.Length;
                        _strip.tsmiOpenCmd = new ToolStripMenuItem(ResourceCache.ResMain[len - 1]);
                        _strip.enableApiHook = new ToolStripMenuItem("Enable Image Hook");

                        _strip.menuTextBoxTabAlias = new ToolStripTextBox();
                        _strip.tssep_Tab1 = new ToolStripSeparator();
                        _strip.tssep_Tab2 = new ToolStripSeparator();
                        _strip.tssep_Tab3 = new ToolStripSeparator();
                        _strip.contextMenuTab.SuspendLayout();
                        _strip.contextMenuTab.Items[0].Dispose();
                        _strip.contextMenuTab.Items.AddRange(new ToolStripItem[] {
                            _strip.tsmiClose, _strip.tsmiCloseRight, _strip.tsmiCloseLeft, _strip.tsmiCloseAllButThis,
                            _strip.tssep_Tab1, _strip.tsmiAddToGroup, _strip.tsmiCreateGroup, _strip.tssep_Tab2, _strip.tsmiLockThis,
                            _strip.tsmiCloneThis, _strip.tsmiCreateWindow, _strip.tsmiCopy, _strip.tsmiTabOrder, _strip.tssep_Tab3, _strip.tsmiProp,
                            _strip.tsmiHistory,
                            _strip.tsmiOpenCmd,
                        });

                        _strip.tsmiAddToGroup.DragDrop += (sender, e) => {
                            _host.NowTabDragging = true;
                            var dataObject = e.Data;
                            QTLogger.log("e.Data: " + dataObject);
                            _host.NowTabDragging = false;
                        };

                        _strip.tsmiAddToGroup.DropDownItemClicked += MenuitemAddToGroup_DropDownItemClicked;
                        (_strip.tsmiAddToGroup.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _strip.tsmiHistory.DropDown = new DropDownMenuBase(_strip.components, true, true, true);
                        _strip.tsmiHistory.DropDownItemClicked += MenuitemHistory_DropDownItemClicked;
                        (_strip.tsmiHistory.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _strip.menuTextBoxTabAlias.Text = _strip.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                        _strip.menuTextBoxTabAlias.GotFocus += _strip.menuTextBoxTabAlias_GotFocus;
                        _strip.menuTextBoxTabAlias.LostFocus += _strip.menuTextBoxTabAlias_LostFocus;
                        _strip.menuTextBoxTabAlias.KeyPress += _strip.menuTextBoxTabAlias_KeyPress;
                        _strip.tsmiTabOrder.DropDown = new ContextMenuStripEx(_strip.components, false);
                        _strip.tssep_Tab1.Enabled = false;
                        _strip.tssep_Tab2.Enabled = false;
                        _strip.tssep_Tab3.Enabled = false;
                        _strip.contextMenuTab.ResumeLayout(false);
                    }
                    if(!flag && fText) {
                        _strip.tsmiClose.Text = ResourceCache.ResMain[0];
                        _strip.tsmiCloseRight.Text = ResourceCache.ResMain[1];
                        _strip.tsmiCloseLeft.Text = ResourceCache.ResMain[2];
                        _strip.tsmiCloseAllButThis.Text = ResourceCache.ResMain[3];
                        _strip.tsmiAddToGroup.Text = ResourceCache.ResMain[4];
                        _strip.tsmiCreateGroup.Text = ResourceCache.ResMain[5] + "...";
                        _strip.tsmiLockThis.Text = ResourceCache.ResMain[6];
                        _strip.tsmiCloneThis.Text = ResourceCache.ResMain[7];
                        _strip.tsmiCreateWindow.Text = ResourceCache.ResMain[8];
                        _strip.tsmiCopy.Text = ResourceCache.ResMain[9];
                        _strip.tsmiProp.Text = ResourceCache.ResMain[10];
                        _strip.tsmiHistory.Text = ResourceCache.ResMain[11];
                        _strip.tsmiTabOrder.Text = ResourceCache.ResMain[0x1c];
                        _strip.menuTextBoxTabAlias.Text = _strip.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                    }
                }
                catch(Exception e) {
                    QTLogger.MakeErrorLog(e);
                }
        }
    }
}
