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
        private readonly IMenuOperationsFacadeHost _facade;

        public MenuOperationsController(
        IMenuContext menuContext,
        IExplorerContext explorerContext,
        IMenuOperationsFacadeHost facade) {
        _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
        _explorerContext = explorerContext ?? throw new ArgumentNullException(nameof(explorerContext));
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
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
                        if(_facade.IsSpecialFolderNeedsToTravel(data.Path)) {
                            if(_facade.LogEntryDic.ContainsKey(data.Hash)) {
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
                        if(_facade.IsSpecialFolderNeedsToTravel(historyBack[i])) {
                            item2.Enabled = _facade.LogEntryDic.ContainsKey(item.GetLogHash(true, i));
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
                        if(_facade.IsSpecialFolderNeedsToTravel(historyForward[j])) {
                            item3.Enabled = _facade.LogEntryDic.ContainsKey(item.GetLogHash(false, j));
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
                    e.HRESULT = _facade.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : Control.MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
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
                    _facade.OpenGroup(groupName, modifierKeys == Keys.Control, false);
                }
        }

        public void MenuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
                GroupsManager.HandleReorder(_facade.tsmiGroups.DropDownItems.Cast<ToolStripItem>());
                QTTabBarClass.SyncTaskBarMenu();
        }

        public void MenuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if((_menuContext.ContextMenuedTab != null) && (clickedItem != null)) {
                    MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                    switch(Control.ModifierKeys) {
                        case Keys.Shift:
                            _facade.CloneTabButton(_menuContext.ContextMenuedTab, null, true, -1);
                            _facade.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;

                        case Keys.Control: {
                                using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                    _facade.OpenNewWindow(wrapper);
                                    return;
                                }
                            }
                        default:
                            _menuContext.TabControl.SelectTab(_menuContext.ContextMenuedTab);
                            _facade.NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                            return;
                    }
                }
        }

        public void DdmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
                QMenuItem clickedItem = e.ClickedItem as QMenuItem;
                if(clickedItem != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        e.HRESULT = _facade.shellContextMenu.Open(wrapper, e.IsKey ? e.Point : Control.MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                    }
                    if(e.HRESULT == 0xffff) {
                        StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                        e.ClickedItem.Dispose();
                    }
                }
        }

        public void DdrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
                _facade.ReplaceByGroup(e.ClickedItem.Text);
        }

        public bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
                QTLogger.log("QTTabBarClass FolderLinkClicked");
                MouseChord chord = QTUtility.MakeMouseChord(middle ? MouseChord.Middle : MouseChord.Left, modifierKeys);
                BindAction action;
                if(Config.Mouse.LinkActions.TryGetValue(chord, out action)) {
                    _facade.DoBindAction(action, false, null, wrapper);
                    return true;
                }
                QTLogger.log("QTTabBarClass FolderLinkClicked 未获取到配置的动作");
                return false;
        }

        public void contextMenuSys_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(e.ClickedItem == _facade.tsmiOption) {
                    OptionsDialog.Open();
                }
                else if(e.ClickedItem == _facade.tsmiCloseAllButCurrent) {
                    if(_menuContext.TabControl.TabCount != 1) {
                        _facade.CloseAllTabsExcept(_menuContext.CurrentTab);
                    }
                }
                else if(e.ClickedItem == _facade.tsmiBrowseFolder) {
                    _facade.ChooseNewDirectory();
                }
                else if(e.ClickedItem == _facade.tsmiCloseWindow) {
                    {
                        LockedTabsService.PersistFromTabs(_menuContext.TabControl.TabPages);
                    }
                    WindowUtils.CloseExplorer(_explorerContext.ExplorerHandle, 1);
                }
                else {
                    if(e.ClickedItem == _facade.tsmiLastActiv) {
                        try {
                            _menuContext.TabControl.SelectTab(_facade.lstActivatedTabs[_facade.lstActivatedTabs.Count - 2]);
                            return;
                        }
                        catch (Exception ex)
                        {
                            QTLogger.MakeErrorLog(ex, "tabControl1.SelectTab");
                            return;
                        }
                    }
                    if(e.ClickedItem == _facade.tsmiLockToolbar) {
                        _facade.rebarController.Locked = !_facade.tsmiLockToolbar.Checked;
                    }
                    else if(e.ClickedItem == _facade.tsmiMergeWindows) {
                        _facade.MergeAllWindows();
                    }
                }
        }

        // 右键（系统）菜单打开时触发
        public void contextMenuSys_Opening(object sender, CancelEventArgs e) {
                InitializeSysMenu(false);
                // 延迟加载菜单内容
                _facade.contextMenuSys.SuspendLayout();
                _facade.tsmiGroups.DropDown.SuspendLayout();
                _facade.tsmiUndoClose.DropDown.SuspendLayout();

                MenuUtility.CreateGroupItems(_facade.tsmiGroups);
                MenuUtility.CreateUndoClosedItems(_facade.tsmiUndoClose);
                if((_facade.lstActivatedTabs.Count > 1) && _menuContext.TabControl.TabPages.Contains(_facade.lstActivatedTabs[_facade.lstActivatedTabs.Count - 2])) {
                    _facade.tsmiLastActiv.ToolTipText = _facade.lstActivatedTabs[_facade.lstActivatedTabs.Count - 2].CurrentPath;
                    _facade.tsmiLastActiv.Enabled = true;
                }
                else {
                    _facade.tsmiLastActiv.ToolTipText = string.Empty;
                    _facade.tsmiLastActiv.Enabled = false;
                }
                while(_facade.tsmiExecuted.DropDownItems.Count > 0) {
                    _facade.tsmiExecuted.DropDownItems[0].Dispose();
                }
                List<ToolStripItem> list = MenuUtility.CreateRecentFilesItems();
                if(list.Count > 0) {
                    _facade.tsmiExecuted.DropDown.SuspendLayout();
                    _facade.tsmiExecuted.DropDownItems.AddRange(list.ToArray());
                    _facade.tsmiExecuted.DropDown.ResumeLayout();
                }
                _facade.tsmiExecuted.Enabled = _facade.tsmiExecuted.DropDownItems.Count > 0;
                _facade.tsmiMergeWindows.Enabled = InstanceManager.GetTotalInstanceCount() > 1;
                _facade.tsmiLockToolbar.Checked = _facade.rebarController.Locked;
                if((_facade.lstPluginMenuItems_Sys != null) && (_facade.lstPluginMenuItems_Sys.Count > 0)) {
                    foreach(ToolStripItem item in _facade.lstPluginMenuItems_Sys) {
                        item.Dispose();
                    }
                    _facade.lstPluginMenuItems_Sys = null;
                }
                if((_facade.pluginServer != null) && (_facade.pluginServer.dicFullNamesMenuRegistered_Sys.Count > 0)) {
                    _facade.lstPluginMenuItems_Sys = new List<ToolStripItem>();
                    int index = _facade.contextMenuSys.Items.IndexOf(_facade.tsmiOption);
                    ToolStripSeparator separator = new ToolStripSeparator();
                    _facade.contextMenuSys.Items.Insert(index, separator);
                    foreach(string str in _facade.pluginServer.dicFullNamesMenuRegistered_Sys.Keys) {
                        ToolStripMenuItem item2 = new ToolStripMenuItem(_facade.pluginServer.dicFullNamesMenuRegistered_Sys[str]);
                        item2.Name = str;
                        item2.Tag = MenuType.Bar;
                        item2.Click += _facade._pluginMenuController.PluginItemsClick;
                        _facade.contextMenuSys.Items.Insert(index, item2);
                        _facade.lstPluginMenuItems_Sys.Add(item2);
                    }
                    _facade.lstPluginMenuItems_Sys.Add(separator);
                }
                _facade.tsmiUndoClose.DropDown.ResumeLayout();
                _facade.tsmiGroups.DropDown.ResumeLayout();
                _facade.contextMenuSys.ResumeLayout();
        }

        public void InitializeSysMenu(bool fText) {
                bool flag = false;
                if(_facade.tsmiGroups == null) {
                    flag = true;
                    _facade.tsmiGroups = new ToolStripMenuItem(ResourceCache.ResMain[12]);
                    _facade.tsmiUndoClose = new ToolStripMenuItem(ResourceCache.ResMain[13]);
                    _facade.tsmiLastActiv = new ToolStripMenuItem(ResourceCache.ResMain[14]);
                    _facade.tsmiExecuted = new ToolStripMenuItem(ResourceCache.ResMain[15]);
                    _facade.tsmiBrowseFolder = new ToolStripMenuItem(ResourceCache.ResMain[0x10] + "...");
                    _facade.tsmiCloseAllButCurrent = new ToolStripMenuItem(ResourceCache.ResMain[0x11]);
                    _facade.tsmiCloseWindow = new ToolStripMenuItem(ResourceCache.ResMain[0x12]);
                    _facade.tsmiOption = new ToolStripMenuItem(ResourceCache.ResMain[0x13]);
                    _facade.tsmiLockToolbar = new ToolStripMenuItem(ResourceCache.ResMain[0x20]);
                    _facade.tsmiMergeWindows = new ToolStripMenuItem(ResourceCache.ResMain[0x21]);
                    _facade.tssep_Sys1 = new ToolStripSeparator();
                    _facade.tssep_Sys2 = new ToolStripSeparator();
                    if(_facade.contextMenuSys != null) {
                        _facade.contextMenuSys.SuspendLayout();
                        _facade.contextMenuSys.Items[0].Dispose();
                        _facade.contextMenuSys.Items.AddRange(new ToolStripItem[]
                        {
                            _facade.tsmiGroups, _facade.tsmiUndoClose, _facade.tsmiLastActiv, _facade.tsmiExecuted,
                            _facade.tssep_Sys1, _facade.tsmiBrowseFolder, _facade.tsmiCloseAllButCurrent, _facade.tsmiCloseWindow,
                            _facade.tsmiMergeWindows, _facade.tsmiLockToolbar, _facade.tssep_Sys2, _facade.tsmiOption
                        });
                    }

                    DropDownMenuReorderable reorderable = new DropDownMenuReorderable(_facade.components, true, false);
                    reorderable.ReorderFinished += MenuitemGroups_ReorderFinished;
                    reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                    reorderable.ItemMiddleClicked += DdrmrGroups_ItemMiddleClicked;
                    reorderable.ImageList = ResourceCache.ImageListGlobal;
                    _facade.tsmiGroups.DropDown = reorderable;
                    _facade.tsmiGroups.DropDownItemClicked += MenuitemGroups_DropDownItemClicked;
                    DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(_facade.components);
                    reorderable2.ReorderEnabled = false;
                    reorderable2.MessageParent = _facade.Handle;
                    reorderable2.ImageList = ResourceCache.ImageListGlobal;
                    reorderable2.ItemRightClicked += DdmrUndoClose_ItemRightClicked;
                    _facade.tsmiUndoClose.DropDown = reorderable2;
                    _facade.tsmiUndoClose.DropDownItemClicked += _facade.menuitemUndoClose_DropDownItemClicked;
                    DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(_facade.components);
                    reorderable3.MessageParent = _facade.Handle;
                    reorderable3.ItemRightClicked += MenuitemExecuted_ItemRightClicked;
                    reorderable3.ItemClicked += MenuitemExecuted_DropDownItemClicked;
                    reorderable3.ImageList = ResourceCache.ImageListGlobal;
                    _facade.tsmiExecuted.DropDown = reorderable3;
                    _facade.tssep_Sys1.Enabled = false;
                    _facade.tssep_Sys2.Enabled = false;
                    if(_facade.contextMenuSys != null) {
                        _facade.contextMenuSys.ResumeLayout(false);
                    }
                }
                if(!flag && fText) {
                    _facade.tsmiGroups.Text = ResourceCache.ResMain[12];
                    _facade.tsmiUndoClose.Text = ResourceCache.ResMain[13];
                    _facade.tsmiLastActiv.Text = ResourceCache.ResMain[14];
                    _facade.tsmiExecuted.Text = ResourceCache.ResMain[15];
                    _facade.tsmiBrowseFolder.Text = ResourceCache.ResMain[0x10] + "...";
                    _facade.tsmiCloseAllButCurrent.Text = ResourceCache.ResMain[0x11];
                    _facade.tsmiCloseWindow.Text = ResourceCache.ResMain[0x12];
                    _facade.tsmiOption.Text = ResourceCache.ResMain[0x13];
                    _facade.tsmiLockToolbar.Text = ResourceCache.ResMain[0x20];
                    _facade.tsmiMergeWindows.Text = ResourceCache.ResMain[0x21];
                }
        }

        public void contextMenuTab_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
                if(_menuContext.ContextMenuedTab != null) {
                    if(e.ClickedItem == _facade.tsmiClose) {
                        if(_menuContext.TabControl.TabCount == 1) {
                            {
                                LockedTabsService.PersistFromTabs(_menuContext.TabControl.TabPages);
                            }
                            WindowUtils.CloseExplorer(_explorerContext.ExplorerHandle, 1);
                        }
                        else {
                            _facade.CloseTab(_menuContext.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _facade.tsmiCloseAllButThis) {
                        _facade.CloseAllTabsExcept(_menuContext.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _facade.tsmiCloseLeft) {
                        int index = _menuContext.TabControl.TabPages.IndexOf(_menuContext.ContextMenuedTab);
                        if(index > 0) {
                            _facade.CloseLeftRight(true, index);
                        }
                    }
                    else if(e.ClickedItem == _facade.tsmiCloseRight) {
                        int num2 = _menuContext.TabControl.TabPages.IndexOf(_menuContext.ContextMenuedTab);
                        if(num2 >= 0) {
                            _facade.CloseLeftRight(false, num2);
                        }
                    }
                    else if(e.ClickedItem == _facade.tsmiCreateGroup) {
                        CreateGroup(_menuContext.ContextMenuedTab);
                    }
                    else if(e.ClickedItem == _facade.tsmiLockThis) {
                        LockedTabsService.ToggleTab(
                            _menuContext.ContextMenuedTab,
                            _menuContext.TabControl.TabPages.Cast<QTabItem>());
                    }
                    else if(e.ClickedItem == _facade.tsmiCloneThis) {
                        _facade.CloneTabButton(_menuContext.ContextMenuedTab, null, true, -1);
                    }
                    else if(e.ClickedItem == _facade.tsmiCreateWindow) {
                        using(IDLWrapper wrapper = new IDLWrapper(_menuContext.ContextMenuedTab.CurrentIDL)) {
                            _facade.OpenNewWindow(wrapper);
                        }
                        if(/*!Config.KeepOnSeparate != */ ((Control.ModifierKeys & Keys.Shift) != Keys.None)) {
                            _facade.CloseTab(_menuContext.ContextMenuedTab);
                        }
                    }
                    else if(e.ClickedItem == _facade.tsmiCopy) {
                        string currentPath = _menuContext.ContextMenuedTab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        else if(currentPath.IndexOf("*?*?*") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                    }
                    else if(e.ClickedItem == _facade.tsmiProp) {
                        ShellMethods.ShowProperties(_menuContext.ContextMenuedTab.CurrentIDL);
                    }
                    else if (e.ClickedItem == _facade.tsmiOpenCmd) { // add by qwop.
                        _facade.OpenCmd( null );
                    } else if (e.ClickedItem == _facade.enableApiHook)
                    {
                        _facade.EnableApiHook();
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
                        _facade.contextMenuTab.SuspendLayout();
                        if(_menuContext.TabControl.TabCount == 1) {
                            _facade.tsmiTabOrder.Enabled = _facade.tsmiCloseAllButThis.Enabled = _facade.tsmiCloseLeft.Enabled = _facade.tsmiCloseRight.Enabled = false;
                        }
                        else {
                            if(index == 0) {
                                _facade.tsmiCloseLeft.Enabled = false;
                                _facade.tsmiCloseRight.Enabled = true;
                            }
                            else if(index == (_menuContext.TabControl.TabCount - 1)) {
                                _facade.tsmiCloseLeft.Enabled = true;
                                _facade.tsmiCloseRight.Enabled = false;
                            }
                            else {
                                _facade.tsmiCloseLeft.Enabled = _facade.tsmiCloseRight.Enabled = true;
                            }
                            _facade.tsmiTabOrder.Enabled = _facade.tsmiCloseAllButThis.Enabled = true;
                        }
                        _facade.tsmiClose.Enabled = !_menuContext.ContextMenuedTab.TabLocked;
                        _facade.tsmiLockThis.Text = _menuContext.ContextMenuedTab.TabLocked ? ResourceCache.ResMain[20] : ResourceCache.ResMain[6];
                        if(GroupsManager.GroupCount > 0) {
                            _facade.tsmiAddToGroup.DropDown.SuspendLayout();
                            _facade.tsmiAddToGroup.Enabled = true;
                            while(_facade.tsmiAddToGroup.DropDownItems.Count > 0) {
                                _facade.tsmiAddToGroup.DropDownItems[0].Dispose();
                            }
                            foreach(Group g in GroupsManager.Groups.Where(g => g.Paths.Count > 0)) {
                                _facade.tsmiAddToGroup.DropDownItems.Add(new ToolStripMenuItem(g.Name) {
                                    ImageKey = IconManager.GetImageKey(g.Paths[0], null)
                                });
                            }
                            _facade.tsmiAddToGroup.DropDown.ResumeLayout();
                        }
                        else {
                            _facade.tsmiAddToGroup.Enabled = false;
                        }
                        _facade.tsmiHistory.DropDown.SuspendLayout();
                        while(_facade.tsmiHistory.DropDownItems.Count > 0) {
                            _facade.tsmiHistory.DropDownItems[0].Dispose();
                        }
                        if((_menuContext.ContextMenuedTab.HistoryCount_Back + _menuContext.ContextMenuedTab.HistoryCount_Forward) > 1) {
                            _facade.tsmiHistory.DropDownItems.AddRange(CreateNavBtnMenuItems(false).ToArray());
                            _facade.tsmiHistory.DropDownItems.AddRange(CreateBranchMenu(false, _facade.components, _facade.tsmiBranchRoot_DropDownItemClicked).ToArray());
                            _facade.tsmiHistory.Enabled = true;
                        }
                        else {
                            _facade.tsmiHistory.Enabled = false;
                        }
                        _facade.tsmiHistory.DropDown.ResumeLayout();
                        _facade.contextMenuTab.Items.Remove(_facade.menuTextBoxTabAlias);
                        if(!Config.Tabs.RenameAmbTabs) {
                            _facade.contextMenuTab.Items.Insert(12, _facade.menuTextBoxTabAlias);
                            if(_menuContext.ContextMenuedTab.Comment.Length > 0) {
                                _facade.menuTextBoxTabAlias.Text = _menuContext.ContextMenuedTab.Comment;
                                _facade.menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
                            }
                            else {
                                _facade.menuTextBoxTabAlias.Text = ResourceCache.ResMain[0x1b];
                                _facade.menuTextBoxTabAlias.ForeColor = SystemColors.GrayText;
                            }
                            _facade.menuTextBoxTabAlias.Enabled = !_menuContext.TabControl.AutoSubText;
                        }
                        if(_facade.tsmiTabOrder.DropDownItems.Count == 0) {
                            ((ToolStripDropDownMenu)_facade.tsmiTabOrder.DropDown).ShowImageMargin = false;
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
                            _facade.tsmiTabOrder.DropDownItems.Add(item2);
                            _facade.tsmiTabOrder.DropDownItems.Add(item3);
                            _facade.tsmiTabOrder.DropDownItems.Add(item4);
                            _facade.tsmiTabOrder.DropDownItems.Add(separator);
                            _facade.tsmiTabOrder.DropDownItems.Add(item5);
                            _facade.tsmiTabOrder.DropDownItemClicked += _facade.menuitemTabOrder_DropDownItemClicked;
                        }
                        if((_facade.lstPluginMenuItems_Tab != null) && (_facade.lstPluginMenuItems_Tab.Count > 0)) {
                            foreach(ToolStripItem item6 in _facade.lstPluginMenuItems_Tab) {
                                item6.Dispose();
                            }
                            _facade.lstPluginMenuItems_Tab = null;
                        }
                        if((_facade.pluginServer != null) && (_facade.pluginServer.dicFullNamesMenuRegistered_Tab.Count > 0)) {
                            _facade.lstPluginMenuItems_Tab = new List<ToolStripItem>();
                            int num2 = _facade.contextMenuTab.Items.IndexOf(_facade.tsmiProp);
                            ToolStripSeparator separator2 = new ToolStripSeparator();
                            _facade.contextMenuTab.Items.Insert(num2, separator2);
                            foreach(string str3 in _facade.pluginServer.dicFullNamesMenuRegistered_Tab.Keys) {
                                ToolStripMenuItem item7 = new ToolStripMenuItem(_facade.pluginServer.dicFullNamesMenuRegistered_Tab[str3]);
                                item7.Name = str3;
                                item7.Tag = MenuType.Tab;
                                item7.Click += _facade._pluginMenuController.PluginItemsClick;
                                _facade.contextMenuTab.Items.Insert(num2, item7);
                                _facade.lstPluginMenuItems_Tab.Add(item7);
                            }
                            _facade.lstPluginMenuItems_Tab.Add(separator2);
                        }
                        _facade.contextMenuTab.ResumeLayout();
                    }
                }
                catch (Exception ex) { QTLogger.MakeErrorLog(ex); }
        }

        // 创建标签分组
        public void CreateGroup(QTabItem contextMenuedTab) {
                _facade.NowModalDialogShown = true;
                using(CreateNewGroupForm form = new CreateNewGroupForm(contextMenuedTab.CurrentPath, _menuContext.TabControl.TabPages)) {
                    // Application.EnableVisualStyles();
                    //  Application.SetCompatibleTextRenderingDefault(false);
                    // Application.Run(form);
                   form.TopMost = true;
                   form.ShowDialog();
                }
                _facade.NowModalDialogShown = false;
        }

        public void InitializeTabMenu(bool fText) {
                try {
                    bool flag = false;
                    if(_facade.tsmiClose == null) {
                        flag = true;
                        _facade.tsmiClose = new ToolStripMenuItem(ResourceCache.ResMain[0]);
                        _facade.tsmiCloseRight = new ToolStripMenuItem(ResourceCache.ResMain[1]);
                        _facade.tsmiCloseLeft = new ToolStripMenuItem(ResourceCache.ResMain[2]);
                        _facade.tsmiCloseAllButThis = new ToolStripMenuItem(ResourceCache.ResMain[3]);
                        _facade.tsmiAddToGroup = new ToolStripMenuItem(ResourceCache.ResMain[4]);
                        _facade.tsmiCreateGroup = new ToolStripMenuItem(ResourceCache.ResMain[5] + "...");
                        _facade.tsmiLockThis = new ToolStripMenuItem(ResourceCache.ResMain[6]);
                        _facade.tsmiCloneThis = new ToolStripMenuItem(ResourceCache.ResMain[7]);
                        _facade.tsmiCreateWindow = new ToolStripMenuItem(ResourceCache.ResMain[8]);
                        _facade.tsmiCopy = new ToolStripMenuItem(ResourceCache.ResMain[9]);
                        _facade.tsmiProp = new ToolStripMenuItem(ResourceCache.ResMain[10]);
                        _facade.tsmiHistory = new ToolStripMenuItem(ResourceCache.ResMain[11]);
                        _facade.tsmiTabOrder = new ToolStripMenuItem(ResourceCache.ResMain[0x1c]);

                        int len = ResourceCache.ResMain.Length;
                        _facade.tsmiOpenCmd = new ToolStripMenuItem(ResourceCache.ResMain[len - 1]);
                        _facade.enableApiHook = new ToolStripMenuItem("Enable Image Hook");

                        _facade.menuTextBoxTabAlias = new ToolStripTextBox();
                        _facade.tssep_Tab1 = new ToolStripSeparator();
                        _facade.tssep_Tab2 = new ToolStripSeparator();
                        _facade.tssep_Tab3 = new ToolStripSeparator();
                        _facade.contextMenuTab.SuspendLayout();
                        _facade.contextMenuTab.Items[0].Dispose();
                        _facade.contextMenuTab.Items.AddRange(new ToolStripItem[] {
                            _facade.tsmiClose, _facade.tsmiCloseRight, _facade.tsmiCloseLeft, _facade.tsmiCloseAllButThis,
                            _facade.tssep_Tab1, _facade.tsmiAddToGroup, _facade.tsmiCreateGroup, _facade.tssep_Tab2, _facade.tsmiLockThis,
                            _facade.tsmiCloneThis, _facade.tsmiCreateWindow, _facade.tsmiCopy, _facade.tsmiTabOrder, _facade.tssep_Tab3, _facade.tsmiProp,
                            _facade.tsmiHistory,
                            _facade.tsmiOpenCmd,
                        });

                        _facade.tsmiAddToGroup.DragDrop += (sender, e) => {
                            _facade.NowTabDragging = true;
                            var dataObject = e.Data;
                            QTLogger.log("e.Data: " + dataObject);
                            _facade.NowTabDragging = false;
                        };

                        _facade.tsmiAddToGroup.DropDownItemClicked += MenuitemAddToGroup_DropDownItemClicked;
                        (_facade.tsmiAddToGroup.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _facade.tsmiHistory.DropDown = new DropDownMenuBase(_facade.components, true, true, true);
                        _facade.tsmiHistory.DropDownItemClicked += MenuitemHistory_DropDownItemClicked;
                        (_facade.tsmiHistory.DropDown).ImageList = ResourceCache.ImageListGlobal;
                        _facade.menuTextBoxTabAlias.Text = _facade.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                        _facade.menuTextBoxTabAlias.GotFocus += _facade.menuTextBoxTabAlias_GotFocus;
                        _facade.menuTextBoxTabAlias.LostFocus += _facade.menuTextBoxTabAlias_LostFocus;
                        _facade.menuTextBoxTabAlias.KeyPress += _facade.menuTextBoxTabAlias_KeyPress;
                        _facade.tsmiTabOrder.DropDown = new ContextMenuStripEx(_facade.components, false);
                        _facade.tssep_Tab1.Enabled = false;
                        _facade.tssep_Tab2.Enabled = false;
                        _facade.tssep_Tab3.Enabled = false;
                        _facade.contextMenuTab.ResumeLayout(false);
                    }
                    if(!flag && fText) {
                        _facade.tsmiClose.Text = ResourceCache.ResMain[0];
                        _facade.tsmiCloseRight.Text = ResourceCache.ResMain[1];
                        _facade.tsmiCloseLeft.Text = ResourceCache.ResMain[2];
                        _facade.tsmiCloseAllButThis.Text = ResourceCache.ResMain[3];
                        _facade.tsmiAddToGroup.Text = ResourceCache.ResMain[4];
                        _facade.tsmiCreateGroup.Text = ResourceCache.ResMain[5] + "...";
                        _facade.tsmiLockThis.Text = ResourceCache.ResMain[6];
                        _facade.tsmiCloneThis.Text = ResourceCache.ResMain[7];
                        _facade.tsmiCreateWindow.Text = ResourceCache.ResMain[8];
                        _facade.tsmiCopy.Text = ResourceCache.ResMain[9];
                        _facade.tsmiProp.Text = ResourceCache.ResMain[10];
                        _facade.tsmiHistory.Text = ResourceCache.ResMain[11];
                        _facade.tsmiTabOrder.Text = ResourceCache.ResMain[0x1c];
                        _facade.menuTextBoxTabAlias.Text = _facade.menuTextBoxTabAlias.ToolTipText = ResourceCache.ResMain[0x1b];
                    }
                }
                catch(Exception e) {
                    QTLogger.MakeErrorLog(e);
                }
        }
    }
}
