using System.Drawing;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : ITabBarCompositionHost {
        void ITabBarCompositionHost.BuildTabBarComponents() {
            buttonNavHistoryMenu = new ToolStripDropDownButton();
            tabControl1 = new QTabControl();
            CurrentTab = new QTabItem(string.Empty, string.Empty, tabControl1);
            contextMenuTab = new ContextMenuStripEx(components, false);
            contextMenuSys = new ContextMenuStripEx(components, false);
            tabControl1.SuspendLayout();
            contextMenuSys.SuspendLayout();
            contextMenuTab.SuspendLayout();
            SuspendLayout();

            _explorerControllerModule = new ExplorerController((IExplorerIntegrationHost)this);
            bool showNavigationButtons = Config.Tabs.ShowNavButtons;
            if(showNavigationButtons) {
                _explorerControllerModule.InitializeNavBtns(false);
            }

            buttonNavHistoryMenu.AutoSize = false;
            buttonNavHistoryMenu.DisplayStyle = ToolStripItemDisplayStyle.None;
            buttonNavHistoryMenu.Enabled = false;
            buttonNavHistoryMenu.Size = new Size(13, 0x15);
            buttonNavHistoryMenu.DropDown = new DropDownMenuBase(components, true, true, true);
            buttonNavHistoryMenu.DropDown.ItemClicked += _explorerControllerModule.NavigationButton_DropDownMenu_ItemClicked;
            buttonNavHistoryMenu.DropDownOpening += _explorerControllerModule.NavigationButtons_DropDownOpening;
            buttonNavHistoryMenu.DropDown.ImageList = ResourceCache.ImageListGlobal;

            tabControl1.SetRedraw(false);
            tabControl1.TabPages.Add(CurrentTab);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.ContextMenuStrip = contextMenuTab;
            tabControl1.RefreshOptions(true);
            _tabManager = new TabManager((ITabOperationsHost)this);
            _menuController = new MenuController((IMenuInteractionHost)this, (IMenuLifecycleHost)this);
            _dragDropController = new DragDropController((IDragDropHost)this);
            _hookInputController = new HookInputController((IHookInputHost)this);
            _fileToolsController = new FileToolsController((IFileToolsHost)this);
            _bindActionController = new BindActionController(this);
            _shellCommandController = new ShellCommandController((IShellCommandHost)this);
            _listViewInputController = new ListViewInputController((IListViewInputHost)this);
            _keyboardAcceleratorController = new KeyboardAcceleratorController((IQTTabBarBandHost)this);
            _shellUiController = new ShellUiController((IShellUiHost)this);
            _buttonBarClickController = new ButtonBarClickController((IButtonBarCommandHost)this);
            _bandInfoController = new BandInfoController((IQTTabBarBandHost)this);
            _bandLifecycleController = new BandLifecycleController((IQTTabBarBandHost)this);
            _shellNavigationController = new ShellNavigationController((IShellNavigationHost)this);
            _tabTooltipController = new TabTooltipController((ISubDirTipHost)this);
            _windowManagementController = new WindowManagementController((IWindowManagementHost)this);
            _bandWindowController = new BandWindowController((IQTTabBarBandHost)this);
            _droppedFilesController = new DroppedFilesController((IDroppedFilesHost)this);
            _folderTreeController = new FolderTreeController((IFolderTreeHost)this);
            _viewModeController = new ViewModeController((IViewModeHost)this);
            _pluginMenuController = new PluginMenuController((IPluginMenuHost)this);
            _shutdownController = new ShutdownController((IShutdownResourceHost)this, (IShutdownPersistenceHost)this);

            tabControl1.RowCountChanged += tabControl1_RowCountChanged;
            tabControl1.Deselecting += tabControl1_Deselecting;
            tabControl1.Selecting += tabControl1_Selecting;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            tabControl1.GotFocus += Controls_GotFocus;
            tabControl1.MouseEnter += tabControl1_MouseEnter;
            tabControl1.MouseLeave += tabControl1_MouseLeave;
            tabControl1.MouseDown += tabControl1_MouseDown;
            tabControl1.MouseUp += tabControl1_MouseUp;
            tabControl1.MouseMove += tabControl1_MouseMove;
            tabControl1.MouseDoubleClick += tabControl1_MouseDoubleClick;
            tabControl1.ItemDrag += tabControl1_ItemDrag;
            tabControl1.PointedTabChanged += tabControl1_PointedTabChanged;
            tabControl1.TabCountChanged += tabControl1_TabCountChanged;
            tabControl1.CloseButtonClicked += tabControl1_CloseButtonClicked;
            tabControl1.TabIconMouseDown += tabControl1_TabIconMouseDown;
            tabControl1.PlusButtonClicked += tabControl1_PlusButtonClicked;

            contextMenuTab.Items.Add(new ToolStripMenuItem());
            contextMenuTab.ShowImageMargin = false;
            contextMenuTab.ItemClicked += _menuController.contextMenuTab_ItemClicked;
            contextMenuTab.Opening += _menuController.contextMenuTab_Opening;
            contextMenuTab.Closed += contextMenuTab_Closed;
            contextMenuSys.Items.Add(new ToolStripMenuItem());
            contextMenuSys.ShowImageMargin = false;
            contextMenuSys.ItemClicked += _menuController.contextMenuSys_ItemClicked;
            contextMenuSys.Opening += _menuController.contextMenuSys_Opening;
            Controls.Add(tabControl1);
            if(showNavigationButtons) {
                Controls.Add(toolStrip);
            }
            int scaledHeight = ComputeBandHeight(1, Config.Skin.TabHeight, GetBandDpiScale());
            MinSize = new Size(150, scaledHeight);
            Height = scaledHeight;
            BandHeight = scaledHeight;
            ContextMenuStrip = contextMenuSys;
            MouseDoubleClick += QTTabBarClass_MouseDoubleClick;
            MouseUp += QTTabBarClass_MouseUp;
            tabControl1.ResumeLayout(false);
            contextMenuSys.ResumeLayout(false);
            contextMenuTab.ResumeLayout(false);
            if(showNavigationButtons) {
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            ResumeLayout(false);
        }
    }
}
