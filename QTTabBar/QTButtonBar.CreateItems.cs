//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed partial class QTButtonBar : BandObject {
        #region Button Creation & Layout

        private ToolStripDropDownButton CreateDropDownButton(int index) {
            ToolStripDropDownButton button = new ToolStripDropDownButton();
            if (Config.Skin.UseRebarBGColor)  // 判断是否填充颜色？
            {
                button.BackColor = Config.Skin.RebarColor;
                // button.ForeColor = Color.White;
            }

            /*if (QTUtility.InNightMode)
            {
                button.BackColor = Color.Black;
                button.ForeColor = Color.White;
            }
            else
            {
                button.BackColor = SystemColors.ButtonFace;
                button.ForeColor = Color.Black;
            }*/
           
            switch(index) {
                case -1:
                    if(NavDropDown == null) {
                        NavDropDown = new DropDownMenuBase(components, true, true, true);
                        NavDropDown.ImageList = QTUtility.ImageListGlobal;
                        NavDropDown.ItemClicked += dropDownButtons_DropDown_ItemClicked;
                        NavDropDown.Closed += dropDownButtons_DropDown_Closed;
                    }
                    button.DropDown = NavDropDown;
                    button.Tag = -1;
                    button.AutoToolTip = false;
                    break;

                case 3:
                    if(ddmrGroupButton == null) {
                        ddmrGroupButton = new DropDownMenuReorderable(components, true, false);
                        ddmrGroupButton.ImageList = QTUtility.ImageListGlobal;
                        ddmrGroupButton.ReorderEnabled = !Config.BBar.LockDropDownButtons;
                        ddmrGroupButton.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                        ddmrGroupButton.ItemMiddleClicked += ddmrGroupButton_ItemMiddleClicked;
                        ddmrGroupButton.ReorderFinished += dropDownButtons_DropDown_ReorderFinished;
                        ddmrGroupButton.ItemClicked += dropDownButtons_DropDown_ItemClicked;
                        ddmrGroupButton.Closed += dropDownButtons_DropDown_Closed;
                    }
                    button.DropDown = ddmrGroupButton;
                    button.Enabled = GroupsManager.GroupCount > 0;
                    break;

                case 4:
                    if(ddmrRecentlyClosed == null) {
                        ddmrRecentlyClosed = new DropDownMenuReorderable(components, true, false);
                        ddmrRecentlyClosed.ImageList = QTUtility.ImageListGlobal;
                        ddmrRecentlyClosed.ReorderEnabled = false;
                        ddmrRecentlyClosed.MessageParent = Handle;
                        ddmrRecentlyClosed.ItemRightClicked += ddmr45_ItemRightClicked;
                        ddmrRecentlyClosed.ItemClicked += dropDownButtons_DropDown_ItemClicked;
                        ddmrRecentlyClosed.Closed += dropDownButtons_DropDown_Closed;
                    }
                    button.DropDown = ddmrRecentlyClosed;
                    button.Enabled = StaticReg.ClosedTabHistoryList.Count > 0;
                    break;

                case 5:
                    if(ddmrUserAppButton == null) {
                        ddmrUserAppButton = new DropDownMenuReorderable(components);
                        ddmrUserAppButton.ImageList = QTUtility.ImageListGlobal;
                        ddmrUserAppButton.ReorderEnabled = !Config.BBar.LockDropDownButtons;
                        ddmrUserAppButton.MessageParent = Handle;
                        ddmrUserAppButton.ItemRightClicked += ddmr45_ItemRightClicked;
                        ddmrUserAppButton.ReorderFinished += dropDownButtons_DropDown_ReorderFinished;
                        ddmrUserAppButton.ItemClicked += dropDownButtons_DropDown_ItemClicked;
                        ddmrUserAppButton.Closed += dropDownButtons_DropDown_Closed;
                    }
                    button.DropDown = ddmrUserAppButton;
                    button.Enabled = AppsManager.UserApps.Any();
                    break;
            }
            button.DropDownOpening += dropDownButtons_DropDownOpening;
            return button;
        }
		
		// 控制图片多线程锁
        private static object imgLock2 = new object();
		
        internal bool CreateItems()
        {
            // 工具栏按钮标签文字
            string[] ButtonItemsDisplayName = QTUtility.TextResourcesDic["ButtonBar_BtnName"];
            ManageImageList();
            toolStrip.SuspendLayout();
            if(iSearchResultCount != -1) {
                Explorer.Refresh();
            }
            // 搜索框
            RefreshSearchBox(false);
            if(searchBox != null) {
                searchBox.Dispose();
                timerSerachBox_Search.Dispose();
                timerSearchBox_Rearrange.Dispose();
                searchBox = null;
                timerSerachBox_Search = null;
                timerSearchBox_Rearrange = null;
            }
            // 清除提示组件
            ClearToolStripItems();
            toolStrip.ShowItemToolTips = true;
            // 设置按钮的高度
            Height = Config.BBar.LargeButtons ? BARHEIGHT_LARGE_LARGE : BARHEIGHT_LARGE_SMALL;
            // 是否显示按钮标签
            bool showButtonLabels = Config.BBar.ShowButtonLabels;
            UnloadPluginsOnCreation();
            foreach(int index in Config.BBar.ButtonIndexes) {
                ToolStripItem item;
                switch(index) {
                    case BII_SEPARATOR: // 分割
                        toolStrip.Items.Add(new ToolStripSeparator {Tag = 0});
                        continue;

                    case BII_GROUP:  // 添加到分组
                    case BII_RECENTTAB: // 最近关闭
                    case BII_APPLICATIONLAUNCHER: // 应用程序
                        item = CreateDropDownButton(index);
                        break;

                    case BII_MISCTOOL: // 复制工具的
                        string[] strArray = QTUtility.TextResourcesDic["ButtonBar_Misc"];
                        DropDownMenuBase base2 = new DropDownMenuBase(components) {
                                ShowCheckMargin = !OSDetector.IsXP,
                                ShowImageMargin = false
                        };
                        base2.Items.AddRange(new ToolStripItem[] {
                                new ToolStripMenuItem(strArray[0]),
                                new ToolStripMenuItem(strArray[1]),
                                new ToolStripMenuItem(strArray[2]),
                                new ToolStripMenuItem(strArray[3]),
                                new ToolStripMenuItem(strArray[4]),
                                new ToolStripMenuItem(strArray[6])
                                // 可以添加复制工具
                        });
                        base2.ItemClicked += copyButton_DropDownItemClicked;
                        base2.Opening += copyButton_Opening;
                        item = new ToolStripDropDownButton {DropDown = base2};
                        break;

                    case BII_TOPMOST: // 置顶
                        item = new ToolStripButton {CheckOnClick = true};
                        break;

                    case BII_WINDOWOPACITY:  // 窗口透明度
                        ToolStripTrackBar bar = new ToolStripTrackBar {
                            Tag = index,
                            ToolTipText = ButtonItemsDisplayName[19]  // 半透明
                        };
                        /*if (QTUtility.InNightMode)
                        {
                            bar.BackColor = Color.Black;
                            bar.ForeColor = Color.White;
                        }
                        else
                        {
                            bar.BackColor = SystemColors.ButtonFace;
                            bar.ForeColor = Color.Black;
                        }*/
                        bar.ForeColor = Config.Skin.ToolBarTextColor; // 适配半透明文本颜色
                        if (Config.Skin.UseRebarBGColor)
                        {
                            bar.BackColor = Config.Skin.RebarColor; // 适配填充颜色
                        }

                        int crKey, dwFlg;
                        byte bAlpha;
                        if(PInvoke.GetLayeredWindowAttributes(ExplorerHandle, out crKey, out bAlpha, out dwFlg)) {
                            bar.SetValueWithoutEvent(bAlpha);
                        }
                        bar.ValueChanged += trackBar_ValueChanged;
                        toolStrip.Items.Add(bar);
                        continue;

                    case BII_FILTERBAR: // 搜索框
                        searchBox = new ToolStripSearchBox(
                                Config.BBar.LargeButtons, 
                                Config.BBar.LockSearchBarWidth,
                                ButtonItemsDisplayName[0x12], 
                                SearchBoxWidth) {
                            ToolTipText = ButtonItemsDisplayName[20], 
                            Tag = index
                        };
                        searchBox.ErasingText += searchBox_ErasingText;
                        searchBox.ResizeComplete += searchBox_ResizeComplete;
                        searchBox.TextChanged += searchBox_TextChanged;
                        searchBox.KeyPress += searchBox_KeyPress;
                        searchBox.GotFocus += searchBox_GotFocus;
                        toolStrip.Items.Add(searchBox);
                        timerSerachBox_Search = new Timer(components) {Interval = 250};
                        timerSerachBox_Search.Tick += timerSerachBox_Search_Tick;
                        timerSearchBox_Rearrange = new Timer(components) {Interval = 300};
                        timerSearchBox_Rearrange.Tick += timerSearchBox_Rearrange_Tick;
                        continue;

                    default:
                        if(index >= INTERNAL_BUTTON_COUNT) {
                            if(index.HiWord() > 0) CreatePluginItem(index);
                            continue;
                        }
                        item = new ToolStripButton();
                        break;
                }
                item.DisplayStyle = showButtonLabels
                        ? ToolStripItemDisplayStyle.ImageAndText
                        : ToolStripItemDisplayStyle.Image;
                // 工具栏颜色  by indiff dark mode
                item.ForeColor = Config.Skin.ToolBarTextColor; // 适配颜色文本颜色
                if (Config.Skin.UseRebarBGColor)
                {
                    item.BackColor = Config.Skin.RebarColor; // 适配填充颜色
                }
                
                /*
                if (QTUtility.InNightMode)
                {
                    item.BackColor = Color.Black;
                }
                else
                {
                    item.BackColor = SystemColors.Window;
                }*/
                /*if (QTUtility.InNightMode)
                {
                    item.BackColor = Config.Skin.TabShadActiveColor;
                }*/
                item.ImageScaling = ToolStripItemImageScaling.None;
                item.Text = item.ToolTipText = ButtonItemsDisplayName[index];
                /*
                 ************** 异常文本 **************
                   System.InvalidOperationException: 对象当前正在其他地方使用。
                   在 System.Drawing.Bitmap.Clone(Rectangle rect, PixelFormat format)
                   在 QTTabBarLib.QTButtonBar.CreateItems()
                   在 QTTabBarLib.QTTabBarClass.RefreshOptions()
                 */
                lock (imgLock2) // by indiff
                {
                    item.Image =
                        (Config.BBar.LargeButtons ? imageStrip_Large[index - 1] : imageStrip_Small[index - 1])
                         .Clone(
                            new Rectangle(Point.Empty, Config.BBar.LargeButtons ? sizeLargeButton : sizeSmallButton), PixelFormat.Format32bppArgb);
                }
               

                item.Tag = index;
                toolStrip.Items.Add(item);

                // 添加最后一个
                if((index == BII_NAVIGATION_BACK && 
                    Array.IndexOf(Config.BBar.ButtonIndexes, BII_NAVIGATION_FWRD) == -1) ||
                    index == BII_NAVIGATION_FWRD) {
                    // 导航下拉列表
                    toolStrip.Items.Add(CreateDropDownButton(BII_NAVIGATION_DROPDOWN));
                }
            }
            if(Config.BBar.ButtonIndexes.Length == 0) {
                toolStrip.Items.Add(new ToolStripSeparator {Tag = 0});
            }

            // todo: check
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar != null) {
                tabBar.rebarController.RefreshHeight();
            }
            RefreshButtons();
            toolStrip.ResumeLayout();
            toolStrip.RaiseOnResize();
            return true;
        }

        private void CreatePluginItem(int buttonIndex) {
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar == null) return;
            QTTabBarClass.PluginServer pluginServer = tabbar.pluginServer;
            if(pluginServer == null) return;
            string pluginID = Config.BBar.ActivePluginIDs[buttonIndex.HiWord() - 1];
            try {
                bool showText = Config.BBar.ShowButtonLabels;
                PluginInformation pi = PluginManager.PluginInformations.FirstOrDefault(info => info.PluginID == pluginID);
                if(pi == null || !pi.Enabled) return;
                Plugin plugin;
                pluginServer.TryGetPlugin(pluginID, out plugin);
                if(plugin == null) {
                    plugin = pluginServer.Load(pi, null);
                }
                if(plugin == null) return;

                ToolStripItem itemToAdd = null;
                if(plugin.Instance is IBarDropButton) {
                    IBarDropButton instance = (IBarDropButton)plugin.Instance;
                    instance.InitializeItem();
                    if(instance.IsSplitButton) {
                        itemToAdd = new ToolStripSplitButton(instance.Text) {
                                ImageScaling = ToolStripItemImageScaling.None,
                                DropDownButtonWidth = Config.BBar.LargeButtons ? 14 : 11,
                                DisplayStyle = showText
                                        ? ToolStripItemDisplayStyle.ImageAndText
                                        : ToolStripItemDisplayStyle.Image,
                                ToolTipText = instance.Text,
                                Image = instance.GetImage(Config.BBar.LargeButtons)
                        };

                        


                        DropDownMenuReorderable reorder = new DropDownMenuReorderable(components);
                        reorder.ItemClicked += pluginDropDown_ItemClicked;
                        reorder.ItemRightClicked += pluginDropDown_ItemRightClicked;
                        ((ToolStripSplitButton)itemToAdd).DropDown = reorder;
                        ((ToolStripSplitButton)itemToAdd).DropDownOpening += pluginDropDown_DropDownOpening;
                        ((ToolStripSplitButton)itemToAdd).ButtonClick += pluginButton_ButtonClick;
                    }
                    else {
                        itemToAdd = new ToolStripDropDownButton(instance.Text) {
                                ImageScaling = ToolStripItemImageScaling.None,
                                DisplayStyle = showText
                                        ? ToolStripItemDisplayStyle.ImageAndText
                                        : ToolStripItemDisplayStyle.Image,
                                ToolTipText = instance.Text,
                                Image = instance.GetImage(Config.BBar.LargeButtons)
                        };
                        DropDownMenuReorderable reorder = new DropDownMenuReorderable(components);
                        reorder.ItemClicked += pluginDropDown_ItemClicked;
                        reorder.ItemRightClicked += pluginDropDown_ItemRightClicked;
                        ((ToolStripDropDownButton)itemToAdd).DropDown = reorder;
                        ((ToolStripDropDownButton)itemToAdd).DropDownOpening += pluginDropDown_DropDownOpening;
                    }
                }
                else if(plugin.Instance is IBarButton) {
                    IBarButton instance = (IBarButton)plugin.Instance;
                    instance.InitializeItem();
                    itemToAdd = new ToolStripButton(instance.Text) {
                            ImageScaling = ToolStripItemImageScaling.None,
                            DisplayStyle = showText
                                    ? ToolStripItemDisplayStyle.ImageAndText
                                    : ToolStripItemDisplayStyle.Image,
                            ToolTipText = instance.Text,
                            Image = instance.GetImage(Config.BBar.LargeButtons)
                    };
                    itemToAdd.Click += pluginButton_ButtonClick;
                }
                else if(plugin.Instance is IBarCustomItem) {
                    IBarCustomItem instance = (IBarCustomItem)plugin.Instance;
                    DisplayStyle displayStyle = showText ? DisplayStyle.ShowTextLabel : DisplayStyle.NoLabel;
                    itemToAdd = instance.CreateItem(Config.BBar.LargeButtons, displayStyle);
                    if(itemToAdd != null) {
                        itemToAdd.ImageScaling = ToolStripItemImageScaling.None;
                        lstPluginCustomItem.Add(itemToAdd);
                    }
                }
                else if(plugin.Instance is IBarMultipleCustomItems) {
                    IBarMultipleCustomItems instance = (IBarMultipleCustomItems)plugin.Instance;
                    if(buttonIndex.LoWord() >= instance.Count) return;
                    if(!plugin.BackgroundButtonEnabled) {
                        // This is to maintain backwards compatibility.
                        instance.Initialize(instance.Count.RangeSelect(i => i).ToArray());
                    }
                    DisplayStyle style = showText ? DisplayStyle.ShowTextLabel : DisplayStyle.NoLabel;
                    itemToAdd = instance.CreateItem(Config.BBar.LargeButtons, style, buttonIndex.LoWord());
                    if(itemToAdd != null) {
                        lstPluginCustomItem.Add(itemToAdd);
                    }
                }

                if(itemToAdd != null) {
                    // 工具栏颜色  by indiff dark mode
                    itemToAdd.ForeColor = Config.Skin.ToolBarTextColor; // 适配插件文本颜色
                    // item.BackColor = Config.Skin.TabShadActiveColor;
                    // itemToAdd.BackColor = Config.Skin.TabShadActiveColor;

                    if (Config.Skin.UseRebarBGColor)
                    {
                        itemToAdd.BackColor = Config.Skin.RebarColor; // 适配插件填充颜色
                    }

                    itemToAdd.Tag = buttonIndex;
                    toolStrip.Items.Add(itemToAdd);
                    if(pi.PluginType == PluginType.Background || pi.PluginType == PluginType.BackgroundMultiple) {
                        plugin.BackgroundButtonEnabled = true;
                    }
                }
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, ExplorerHandle, pluginID, "Loading plugin button.");
            }
        }

        private void ddmr45_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(clickedItem != null) {
                bool fCanRemove = sender == ddmrRecentlyClosed;
                using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                    e.HRESULT = shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, fCanRemove);
                }
                if(fCanRemove && (e.HRESULT == 0xffff)) {
                    StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                    clickedItem.Dispose();
                }
            }
        }

        private static void ddmrGroupButton_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
            TabInstanceRegistry.GetThreadTabBar().ReplaceByGroup(e.ClickedItem.Text);
        }

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        internal bool ClickItem(int index) {
            ToolStripItem item = toolStrip.Items.Cast<ToolStripItem>()
                    .FirstOrDefault(item2 => item2.Tag != null && (int)item2.Tag == index);
            if(item == null) {
                return false;
            }
            else if(item is ToolStripDropDownItem) {
                ((ToolStripDropDownItem)item).ShowDropDown();
            }
            else {
                item.PerformClick();
            }
            return true;
        }

        private static void dropDownButtons_DropDown_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
            DropDownMenuBase.ExitMenuMode();
        }

        private void dropDownButtons_DropDown_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            ToolStripItem ownerItem = ((ToolStripDropDown)sender).OwnerItem;
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(ownerItem == null || ownerItem.Tag == null || clickedItem == null) return;
            int tag = (int)ownerItem.Tag;
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            Keys modifierKeys = ModifierKeys;
            switch(tag) {
                case BII_NAVIGATION_DROPDOWN:
                    MenuItemArguments mia = clickedItem.MenuItemArguments;
                    if(modifierKeys == Keys.Control) {
                        using(IDLWrapper wrapper = new IDLWrapper(mia.Path)) {
                            tabbar.OpenNewWindow(wrapper);
                        }
                    }
                    else {
                        if(modifierKeys == Keys.Shift) tabbar.CloneCurrentTab();  
                        tabbar.NavigateToHistory(mia.Path, mia.IsBack, mia.Index);
                    }
                    return;

                case BII_GROUP:
                    ddmrGroupButton.Close();
                    if(ModifierKeys == (Keys.Control | Keys.Shift)) {
                        Group g = GroupsManager.GetGroup(e.ClickedItem.Text);
                        g.Startup = !g.Startup;
                        GroupsManager.SaveGroups();
                    }
                    else {
                        tabbar.OpenGroup(e.ClickedItem.Text, modifierKeys == Keys.Control);
                    }
                    return;

                case BII_RECENTTAB: // 最近标签
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                        tabbar.OpenNewTabOrWindow(wrapper);
                    }
                    return;

                case BII_APPLICATIONLAUNCHER:  // 启动应用
                    if(clickedItem.Target == MenuTarget.File) {
                        AppsManager.Execute(clickedItem.MenuItemArguments.App, clickedItem.MenuItemArguments.ShellBrowser);
                    }
                    return;
            }
        }

        private void dropDownButtons_DropDown_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
            DropDownMenuReorderable reorderable = (DropDownMenuReorderable)sender;
            switch(((int)reorderable.OwnerItem.Tag)) {
                case 3:
                    GroupsManager.HandleReorder(reorderable.Items.Cast<ToolStripItem>());
                    break;

                case 5:
                    AppsManager.HandleReorder(reorderable.Items.Cast<ToolStripItem>());
                    break;
            }
            QTTabBarClass.SyncTaskBarMenu();
        }

        private void dropDownButtons_DropDownOpening(object sender, EventArgs e) {
            toolStrip.HideToolTip();
            ToolStripDropDownItem button = (ToolStripDropDownItem)sender;
            button.DropDown.SuspendLayout();
            switch(((int)button.Tag)) {
                case -1:
                    AddHistoryItems(button);
                    break;

                case 3:
                    MenuUtility.CreateGroupItems(button);
                    break;

                case 4:
                    MenuUtility.CreateUndoClosedItems(button);
                    break;

                case 5:
                    AddUserAppItems();
                    break;
            }
            button.DropDown.ResumeLayout();
        }

        #endregion
    }
}