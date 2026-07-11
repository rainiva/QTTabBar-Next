using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IButtonBarItemFactoryHost {
        void BeginItemCreation();
        void CompleteItemCreation();
    }

    internal interface IButtonBarSpecialItemHost {
        void AddSeparator();
        void AddPluginItem(int buttonIndex);
    }

    internal interface IButtonBarStandardItemHost {
        void AddStandardItem(int buttonIndex);
        void AddNavigationDropDown();
    }

    internal sealed class ButtonBarItemFactory {
        internal static bool CreateItems(
                IButtonBarItemFactoryHost lifecycle,
                IButtonBarSpecialItemHost specialItems,
                IButtonBarStandardItemHost standardItems) {
            lifecycle.BeginItemCreation();

            foreach(int index in Config.BBar.ButtonIndexes) {
                if(index == QTButtonBar.BII_SEPARATOR) {
                    specialItems.AddSeparator();
                    continue;
                }

                if(index >= QTButtonBar.INTERNAL_BUTTON_COUNT) {
                    if(index.HiWord() > 0) {
                        specialItems.AddPluginItem(index);
                    }
                    continue;
                }

                standardItems.AddStandardItem(index);
                if((index == QTButtonBar.BII_NAVIGATION_BACK &&
                    Array.IndexOf(Config.BBar.ButtonIndexes, QTButtonBar.BII_NAVIGATION_FWRD) == -1) ||
                    index == QTButtonBar.BII_NAVIGATION_FWRD) {
                    standardItems.AddNavigationDropDown();
                }
            }

            if(Config.BBar.ButtonIndexes.Length == 0) {
                specialItems.AddSeparator();
            }

            lifecycle.CompleteItemCreation();
            return true;
        }
    }

    internal sealed class ButtonBarItemState {
        internal IContainer Components;
        internal DropDownMenuReorderable GroupMenu;
        internal DropDownMenuReorderable RecentlyClosedMenu;
        internal DropDownMenuReorderable UserAppsMenu;
        internal readonly List<ToolStripItem> PluginCustomItems = new List<ToolStripItem>();
        internal DropDownMenuBase NavigationMenu;
        internal ToolStripSearchBox SearchBox;
        internal Timer SearchTimer;
        internal Timer RearrangeTimer;
        internal ToolStripClasses ToolStrip;
    }

    internal sealed class ButtonBarItemLifecycleCallbacks {
        internal Action ManageImages;
        internal Action<bool> RefreshSearchBox;
        internal Action ClearToolStripItems;
        internal Action<int> SetHeight;
        internal Action UnloadPlugins;
        internal Action RefreshExplorer;
        internal Action RefreshButtons;
    }

    internal sealed class ButtonBarDropDownCallbacks {
        internal Func<IntPtr> ExplorerHandle;
        internal EventHandler DropDownOpening;
        internal ToolStripItemClickedEventHandler DropDownItemClicked;
        internal ToolStripDropDownClosedEventHandler DropDownClosed;
        internal ItemRightClickedEventHandler ItemRightClicked;
        internal ItemRightClickedEventHandler GroupItemMiddleClicked;
        internal MenuReorderedEventHandler ReorderFinished;
        internal ToolStripItemClickedEventHandler CopyItemClicked;
        internal CancelEventHandler CopyOpening;
        internal EventHandler TrackBarValueChanged;
    }

    internal sealed class ButtonBarSearchCallbacks {
        internal Func<int> SearchBoxWidth;
        internal CancelEventHandler SearchErasingText;
        internal EventHandler SearchResizeComplete;
        internal EventHandler SearchTextChanged;
        internal KeyPressEventHandler SearchKeyPress;
        internal EventHandler SearchGotFocus;
        internal EventHandler SearchTimerTick;
        internal EventHandler RearrangeTimerTick;
    }

    internal sealed class ButtonBarPluginCallbacks {
        internal EventHandler PluginButtonClick;
        internal EventHandler PluginDropDownOpening;
        internal ToolStripItemClickedEventHandler PluginDropDownItemClicked;
        internal ItemRightClickedEventHandler PluginDropDownItemRightClicked;
    }

    internal sealed class ButtonBarItemController {
        #region Button Creation & Layout
        private static readonly object ImageLock = new object();
        private static readonly Size LargeButtonSize = new Size(24, 24);
        private static readonly Size SmallButtonSize = new Size(16, 16);
        private static readonly ImageStrip LargeImages = new ImageStrip(LargeButtonSize);
        private static readonly ImageStrip SmallImages = new ImageStrip(SmallButtonSize);
        private readonly ButtonBarItemState _state;
        private readonly ButtonBarItemLifecycleCallbacks _lifecycle;
        private readonly ButtonBarDropDownCallbacks _dropDown;
        private readonly ButtonBarSearchCallbacks _search;
        private readonly ButtonBarPluginCallbacks _plugins;

        internal ButtonBarItemController(ButtonBarItemState state, ButtonBarItemLifecycleCallbacks lifecycle,
                ButtonBarDropDownCallbacks dropDown, ButtonBarSearchCallbacks search, ButtonBarPluginCallbacks plugins) {
            _state = state;
            _lifecycle = lifecycle; _dropDown = dropDown; _search = search; _plugins = plugins;
        }

        internal static void ManageImages() { ButtonBarImageLoader.Manage(LargeImages, SmallImages); }
        internal static bool LoadExternalImage(string path) { return ButtonBarImageLoader.LoadExternalImage(path, LargeImages, SmallImages); }
        internal static bool LoadExternalImage(string path, out Bitmap large, out Bitmap small) { return ButtonBarImageLoader.LoadExternalImage(path, out large, out small); }

        internal bool CreateItems() {
            _lifecycle.ManageImages();
            _state.ToolStrip.SuspendLayout();
            if(_lifecycle.RefreshExplorer != null) _lifecycle.RefreshExplorer();
            _lifecycle.RefreshSearchBox(false);
            DisposeSearchBox();
            _lifecycle.ClearToolStripItems();
            _state.ToolStrip.ShowItemToolTips = true;
            _lifecycle.SetHeight(Config.BBar.LargeButtons ? 48 : 36);
            _lifecycle.UnloadPlugins();
            ButtonBarItemFactory.CreateItems(new Lifecycle(this), new Special(this), new Standard(this));
            return true;
        }

        private void DisposeSearchBox() {
            if(_state.SearchBox == null) return;
            _state.SearchBox.Dispose();
            _state.SearchTimer.Dispose();
            _state.RearrangeTimer.Dispose();
            _state.SearchBox = null;
            _state.SearchTimer = null;
            _state.RearrangeTimer = null;
        }

        private ToolStripDropDownButton CreateDropDownButton(int index) {
            ToolStripDropDownButton button = new ToolStripDropDownButton();
            if(Config.Skin.UseRebarBGColor) button.BackColor = Config.Skin.RebarColor;
            switch(index) {
                case QTButtonBar.BII_NAVIGATION_DROPDOWN:
                    if(_state.NavigationMenu == null) {
                        _state.NavigationMenu = new DropDownMenuBase(_state.Components, true, true, true);
                        _state.NavigationMenu.ImageList = ResourceCache.ImageListGlobal;
                        _state.NavigationMenu.ItemClicked += _dropDown.DropDownItemClicked;
                        _state.NavigationMenu.Closed += _dropDown.DropDownClosed;
                    }
                    button.DropDown = _state.NavigationMenu; button.Tag = index; button.AutoToolTip = false; break;
                case QTButtonBar.BII_GROUP:
                    if(_state.GroupMenu == null) {
                        _state.GroupMenu = new DropDownMenuReorderable(_state.Components, true, false);
                        _state.GroupMenu.ImageList = ResourceCache.ImageListGlobal; _state.GroupMenu.ReorderEnabled = !Config.BBar.LockDropDownButtons;
                        _state.GroupMenu.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked; _state.GroupMenu.ItemMiddleClicked += _dropDown.GroupItemMiddleClicked;
                        _state.GroupMenu.ReorderFinished += _dropDown.ReorderFinished; _state.GroupMenu.ItemClicked += _dropDown.DropDownItemClicked; _state.GroupMenu.Closed += _dropDown.DropDownClosed;
                    }
                    button.DropDown = _state.GroupMenu; button.Enabled = GroupsManager.GroupCount > 0; break;
                case QTButtonBar.BII_RECENTTAB:
                    if(_state.RecentlyClosedMenu == null) {
                        _state.RecentlyClosedMenu = new DropDownMenuReorderable(_state.Components, true, false);
                        _state.RecentlyClosedMenu.ImageList = ResourceCache.ImageListGlobal; _state.RecentlyClosedMenu.ReorderEnabled = false;
                        _state.RecentlyClosedMenu.MessageParent = _dropDown.ExplorerHandle(); _state.RecentlyClosedMenu.ItemRightClicked += _dropDown.ItemRightClicked;
                        _state.RecentlyClosedMenu.ItemClicked += _dropDown.DropDownItemClicked; _state.RecentlyClosedMenu.Closed += _dropDown.DropDownClosed;
                    }
                    button.DropDown = _state.RecentlyClosedMenu; button.Enabled = StaticReg.ClosedTabHistoryList.Count > 0; break;
                case QTButtonBar.BII_APPLICATIONLAUNCHER:
                    if(_state.UserAppsMenu == null) {
                        _state.UserAppsMenu = new DropDownMenuReorderable(_state.Components);
                        _state.UserAppsMenu.ImageList = ResourceCache.ImageListGlobal; _state.UserAppsMenu.ReorderEnabled = !Config.BBar.LockDropDownButtons;
                        _state.UserAppsMenu.MessageParent = _dropDown.ExplorerHandle(); _state.UserAppsMenu.ItemRightClicked += _dropDown.ItemRightClicked;
                        _state.UserAppsMenu.ReorderFinished += _dropDown.ReorderFinished; _state.UserAppsMenu.ItemClicked += _dropDown.DropDownItemClicked; _state.UserAppsMenu.Closed += _dropDown.DropDownClosed;
                    }
                    button.DropDown = _state.UserAppsMenu; button.Enabled = AppsManager.UserApps.Any(); break;
            }
            button.DropDownOpening += _dropDown.DropDownOpening;
            return button;
        }

        private void AddStandardItem(int index) {
            ToolStripItem item;
            switch(index) {
                case QTButtonBar.BII_GROUP: case QTButtonBar.BII_RECENTTAB: case QTButtonBar.BII_APPLICATIONLAUNCHER:
                    item = CreateDropDownButton(index); break;
                case QTButtonBar.BII_MISCTOOL:
                    string[] names = ResourceCache.TextResourcesDic["ButtonBar_Misc"];
                    DropDownMenuBase menu = new DropDownMenuBase(_state.Components) { ShowCheckMargin = !OSDetector.IsXP, ShowImageMargin = false };
                    menu.Items.AddRange(new ToolStripItem[] { new ToolStripMenuItem(names[0]), new ToolStripMenuItem(names[1]), new ToolStripMenuItem(names[2]), new ToolStripMenuItem(names[3]), new ToolStripMenuItem(names[4]), new ToolStripMenuItem(names[6]) });
                    menu.ItemClicked += _dropDown.CopyItemClicked; menu.Opening += _dropDown.CopyOpening; item = new ToolStripDropDownButton { DropDown = menu }; break;
                case QTButtonBar.BII_TOPMOST: item = new ToolStripButton { CheckOnClick = true }; break;
                case QTButtonBar.BII_WINDOWOPACITY:
                    ToolStripTrackBar opacity = new ToolStripTrackBar { Tag = index, ToolTipText = ResourceCache.TextResourcesDic["ButtonBar_BtnName"][19], ForeColor = Config.Skin.ToolBarTextColor };
                    if(Config.Skin.UseRebarBGColor) opacity.BackColor = Config.Skin.RebarColor;
                    int key, flags; byte alpha;
                    if(PInvoke.GetLayeredWindowAttributes(_dropDown.ExplorerHandle(), out key, out alpha, out flags)) opacity.SetValueWithoutEvent(alpha);
                    opacity.ValueChanged += _dropDown.TrackBarValueChanged; _state.ToolStrip.Items.Add(opacity); return;
                case QTButtonBar.BII_FILTERBAR:
                    _state.SearchBox = new ToolStripSearchBox(Config.BBar.LargeButtons, Config.BBar.LockSearchBarWidth, ResourceCache.TextResourcesDic["ButtonBar_BtnName"][18], _search.SearchBoxWidth()) { ToolTipText = ResourceCache.TextResourcesDic["ButtonBar_BtnName"][20], Tag = index };
                    _state.SearchBox.ErasingText += _search.SearchErasingText; _state.SearchBox.ResizeComplete += _search.SearchResizeComplete; _state.SearchBox.TextChanged += _search.SearchTextChanged; _state.SearchBox.KeyPress += _search.SearchKeyPress; _state.SearchBox.GotFocus += _search.SearchGotFocus;
                    _state.ToolStrip.Items.Add(_state.SearchBox); _state.SearchTimer = new Timer(_state.Components) { Interval = 250 }; _state.SearchTimer.Tick += _search.SearchTimerTick; _state.RearrangeTimer = new Timer(_state.Components) { Interval = 300 }; _state.RearrangeTimer.Tick += _search.RearrangeTimerTick; return;
                default: item = new ToolStripButton(); break;
            }
            Image image;
            lock(ImageLock) image = (Config.BBar.LargeButtons ? LargeImages[index - 1] : SmallImages[index - 1]).Clone(new Rectangle(Point.Empty, Config.BBar.LargeButtons ? LargeButtonSize : SmallButtonSize), PixelFormat.Format32bppArgb);
            ButtonBarStandardItemAppearance.Apply(item, index, image);
            _state.ToolStrip.Items.Add(item);
        }

        private void AddPluginItem(int index) {
            ButtonBarPluginItemFactory.Create(index, _state.Components, _state.ToolStrip, _state.PluginCustomItems, _dropDown.ExplorerHandle(), _plugins.PluginButtonClick, _plugins.PluginDropDownOpening, _plugins.PluginDropDownItemClicked, _plugins.PluginDropDownItemRightClicked);
        }

        private sealed class Lifecycle : IButtonBarItemFactoryHost {
            private readonly ButtonBarItemController _owner; internal Lifecycle(ButtonBarItemController owner) { _owner = owner; }
            public void BeginItemCreation() { }
            public void CompleteItemCreation() { QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar(); if(tabBar != null) tabBar.rebarController.RefreshHeight(); _owner._lifecycle.RefreshButtons(); _owner._state.ToolStrip.ResumeLayout(); _owner._state.ToolStrip.RaiseOnResize(); }
        }
        private sealed class Special : IButtonBarSpecialItemHost {
            private readonly ButtonBarItemController _owner; internal Special(ButtonBarItemController owner) { _owner = owner; }
            public void AddSeparator() { _owner._state.ToolStrip.Items.Add(new ToolStripSeparator { Tag = QTButtonBar.BII_SEPARATOR }); }
            public void AddPluginItem(int index) { _owner.AddPluginItem(index); }
        }
        private sealed class Standard : IButtonBarStandardItemHost {
            private readonly ButtonBarItemController _owner; internal Standard(ButtonBarItemController owner) { _owner = owner; }
            public void AddStandardItem(int index) { _owner.AddStandardItem(index); }
            public void AddNavigationDropDown() { _owner._state.ToolStrip.Items.Add(_owner.CreateDropDownButton(QTButtonBar.BII_NAVIGATION_DROPDOWN)); }
        }
        #endregion
    }

    internal sealed class ButtonBarItemActivator {
        internal static bool TryActivate(ToolStrip toolStrip, int index) {
            ToolStripItem item = toolStrip.Items.Cast<ToolStripItem>()
                    .FirstOrDefault(candidate => candidate.Tag != null && (int)candidate.Tag == index);
            if(item == null) {
                return false;
            }

            ToolStripDropDownItem dropDownItem = item as ToolStripDropDownItem;
            if(dropDownItem != null) {
                dropDownItem.ShowDropDown();
            }
            else {
                item.PerformClick();
            }
            return true;
        }
    }

    internal sealed class ButtonBarPluginItemFactory {
        internal static void Create(int buttonIndex, IContainer components, ToolStripClasses toolStrip,
                System.Collections.Generic.List<ToolStripItem> customItems, IntPtr explorerHandle,
                EventHandler buttonClick, EventHandler dropDownOpening,
                ToolStripItemClickedEventHandler dropDownItemClicked, ItemRightClickedEventHandler dropDownItemRightClicked) {
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            if(tabbar == null || tabbar.pluginServer == null) return;
            QTTabBarClass.PluginServer pluginServer = tabbar.pluginServer;
            string pluginID = Config.BBar.ActivePluginIDs[buttonIndex.HiWord() - 1];
            try {
                bool showText = Config.BBar.ShowButtonLabels;
                PluginInformation pi = PluginManager.PluginInformations.FirstOrDefault(info => info.PluginID == pluginID);
                if(pi == null || !pi.Enabled) return;
                Plugin plugin;
                pluginServer.TryGetPlugin(pluginID, out plugin);
                if(plugin == null) plugin = pluginServer.Load(pi, null);
                if(plugin == null) return;
                ToolStripItem itemToAdd = null;
                IBarDropButton dropButton = plugin.Instance as IBarDropButton;
                if(dropButton != null) {
                    dropButton.InitializeItem();
                    DropDownMenuReorderable reorder = new DropDownMenuReorderable(components);
                    reorder.ItemClicked += dropDownItemClicked;
                    reorder.ItemRightClicked += dropDownItemRightClicked;
                    if(dropButton.IsSplitButton) {
                        ToolStripSplitButton split = new ToolStripSplitButton(dropButton.Text) {
                            ImageScaling = ToolStripItemImageScaling.None, DropDownButtonWidth = Config.BBar.LargeButtons ? 14 : 11,
                            DisplayStyle = showText ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
                            ToolTipText = dropButton.Text, Image = dropButton.GetImage(Config.BBar.LargeButtons), DropDown = reorder
                        };
                        split.DropDownOpening += dropDownOpening;
                        split.ButtonClick += buttonClick;
                        itemToAdd = split;
                    }
                    else {
                        ToolStripDropDownButton dropDown = new ToolStripDropDownButton(dropButton.Text) {
                            ImageScaling = ToolStripItemImageScaling.None,
                            DisplayStyle = showText ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
                            ToolTipText = dropButton.Text, Image = dropButton.GetImage(Config.BBar.LargeButtons), DropDown = reorder
                        };
                        dropDown.DropDownOpening += dropDownOpening;
                        itemToAdd = dropDown;
                    }
                }
                else {
                    IBarButton button = plugin.Instance as IBarButton;
                    if(button != null) {
                        button.InitializeItem();
                        itemToAdd = new ToolStripButton(button.Text) {
                            ImageScaling = ToolStripItemImageScaling.None,
                            DisplayStyle = showText ? ToolStripItemDisplayStyle.ImageAndText : ToolStripItemDisplayStyle.Image,
                            ToolTipText = button.Text, Image = button.GetImage(Config.BBar.LargeButtons)
                        };
                        itemToAdd.Click += buttonClick;
                    }
                    else {
                        IBarCustomItem custom = plugin.Instance as IBarCustomItem;
                        if(custom != null) {
                            itemToAdd = custom.CreateItem(Config.BBar.LargeButtons, showText ? DisplayStyle.ShowTextLabel : DisplayStyle.NoLabel);
                            if(itemToAdd != null) { itemToAdd.ImageScaling = ToolStripItemImageScaling.None; customItems.Add(itemToAdd); }
                        }
                        else {
                            IBarMultipleCustomItems multiple = plugin.Instance as IBarMultipleCustomItems;
                            if(multiple != null) {
                                if(buttonIndex.LoWord() >= multiple.Count) return;
                                if(!plugin.BackgroundButtonEnabled) multiple.Initialize(multiple.Count.RangeSelect(i => i).ToArray());
                                itemToAdd = multiple.CreateItem(Config.BBar.LargeButtons, showText ? DisplayStyle.ShowTextLabel : DisplayStyle.NoLabel, buttonIndex.LoWord());
                                if(itemToAdd != null) customItems.Add(itemToAdd);
                            }
                        }
                    }
                }
                if(itemToAdd != null) {
                    itemToAdd.ForeColor = Config.Skin.ToolBarTextColor;
                    if(Config.Skin.UseRebarBGColor) itemToAdd.BackColor = Config.Skin.RebarColor;
                    itemToAdd.Tag = buttonIndex;
                    toolStrip.Items.Add(itemToAdd);
                    if(pi.PluginType == PluginType.Background || pi.PluginType == PluginType.BackgroundMultiple) plugin.BackgroundButtonEnabled = true;
                }
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, explorerHandle, pluginID, "Loading plugin button.");
            }
        }
    }

    internal sealed class ButtonBarDropDownInteractionController {
        internal static void HandleItemRightClick(object sender, ItemRightClickedEventArgs e,
                DropDownMenuReorderable recentlyClosed, ShellContextMenu shellContextMenu) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(clickedItem == null) return;
            bool canRemove = sender == recentlyClosed;
            using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                e.HRESULT = shellContextMenu.Open(wrapper, e.IsKey ? e.Point : Control.MousePosition,
                        ((DropDownMenuReorderable)sender).Handle, canRemove);
            }
            if(canRemove && e.HRESULT == 0xffff) {
                StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                clickedItem.Dispose();
            }
        }

        internal static void HandleGroupMiddleClick(ItemRightClickedEventArgs e) {
            TabInstanceRegistry.GetThreadTabBar().ReplaceByGroup(e.ClickedItem.Text);
        }

        internal static void HandleItemClick(ToolStripItem ownerItem, ToolStripItem clickedItem, Keys modifierKeys, DropDownMenuReorderable groups) {
            if(ownerItem == null || ownerItem.Tag == null || clickedItem == null) return;
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            QMenuItem menuItem = clickedItem as QMenuItem;
            if(tabBar == null || menuItem == null) return;
            switch((int)ownerItem.Tag) {
                case QTButtonBar.BII_NAVIGATION_DROPDOWN:
                    MenuItemArguments arguments = menuItem.MenuItemArguments;
                    if(modifierKeys == Keys.Control) { using(IDLWrapper item = new IDLWrapper(arguments.Path)) tabBar.OpenNewWindow(item); }
                    else { if(modifierKeys == Keys.Shift) tabBar.CloneCurrentTab(); tabBar.NavigateToHistory(arguments.Path, arguments.IsBack, arguments.Index); }
                    break;
                case QTButtonBar.BII_GROUP:
                    groups.Close();
                    if(modifierKeys == (Keys.Control | Keys.Shift)) { Group group = GroupsManager.GetGroup(clickedItem.Text); group.Startup = !group.Startup; GroupsManager.SaveGroups(); }
                    else tabBar.OpenGroup(clickedItem.Text, modifierKeys == Keys.Control);
                    break;
                case QTButtonBar.BII_RECENTTAB:
                    using(IDLWrapper item = new IDLWrapper(menuItem.Path)) tabBar.OpenNewTabOrWindow(item);
                    break;
                case QTButtonBar.BII_APPLICATIONLAUNCHER:
                    if(menuItem.Target == MenuTarget.File) AppsManager.Execute(menuItem.MenuItemArguments.App, menuItem.MenuItemArguments.ShellBrowser);
                    break;
            }
        }
    }

    internal sealed class ButtonBarStandardItemAppearance {
        internal static void Apply(ToolStripItem item, int index, Image image) {
            item.DisplayStyle = Config.BBar.ShowButtonLabels
                    ? ToolStripItemDisplayStyle.ImageAndText
                    : ToolStripItemDisplayStyle.Image;
            item.ForeColor = Config.Skin.ToolBarTextColor;
            if(Config.Skin.UseRebarBGColor) {
                item.BackColor = Config.Skin.RebarColor;
            }
            item.ImageScaling = ToolStripItemImageScaling.None;
            item.Text = item.ToolTipText = ResourceCache.TextResourcesDic["ButtonBar_BtnName"][index];
            item.Image = image;
            item.Tag = index;
        }
    }

    internal sealed class ButtonBarDropDownPopulationController {
        internal static void Populate(
                ToolStripClasses toolStrip,
                ToolStripDropDownItem button,
                Action<ToolStripDropDownItem> addHistoryItems,
                Action addUserAppItems) {
            toolStrip.HideToolTip();
            button.DropDown.SuspendLayout();
            switch((int)button.Tag) {
                case QTButtonBar.BII_NAVIGATION_DROPDOWN:
                    addHistoryItems(button);
                    break;

                case QTButtonBar.BII_GROUP:
                    MenuUtility.CreateGroupItems(button);
                    break;

                case QTButtonBar.BII_RECENTTAB:
                    MenuUtility.CreateUndoClosedItems(button);
                    break;

                case QTButtonBar.BII_APPLICATIONLAUNCHER:
                    addUserAppItems();
                    break;
            }
            button.DropDown.ResumeLayout();
        }
    }

    internal interface IButtonBarStatusTextHost {
        bool HasSearchResults();
        int GetSearchResultCount();
        int GetCurrentShellItemCount();
        void SetSearchResultCount(int count);
        void SetSearchStatusText(int visibleResultCount);
    }

    internal sealed class ButtonBarStatusTextController {
        internal static bool Refresh(IButtonBarStatusTextHost host) {
            if(!host.HasSearchResults()) {
                return false;
            }

            int currentCount = host.GetSearchResultCount();
            int newCount = host.GetCurrentShellItemCount();
            if(newCount >= currentCount) {
                return false;
            }

            host.SetSearchResultCount(newCount);
            host.SetSearchStatusText(newCount);
            return true;
        }
    }
}
