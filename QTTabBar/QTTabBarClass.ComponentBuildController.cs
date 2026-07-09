//    UI component build controller extracted from QTTabBarClass (arch-batch3c6r).

using System;
using System.Drawing;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ComponentBuildController {
            private readonly QTTabBarClass _owner;

            public ComponentBuildController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void Build() {
                _owner.buttonNavHistoryMenu = new ToolStripDropDownButton();
                _owner.tabControl1 = new QTabControl();
                _owner.CurrentTab = new QTabItem(string.Empty, string.Empty, _owner.tabControl1);
                _owner.contextMenuTab = new ContextMenuStripEx(_owner.components, false);
                _owner.contextMenuSys = new ContextMenuStripEx(_owner.components, false);
                _owner.tabControl1.SuspendLayout();
                _owner.contextMenuSys.SuspendLayout();
                _owner.contextMenuTab.SuspendLayout();
                _owner.SuspendLayout();

                _owner._explorerControllerModule = new ExplorerControllerModule(_owner);
                bool flag = Config.Tabs.ShowNavButtons;
                if(flag) {
                    _owner._explorerControllerModule.InitializeNavBtns(false);
                }

                _owner.buttonNavHistoryMenu.AutoSize = false;
                _owner.buttonNavHistoryMenu.DisplayStyle = ToolStripItemDisplayStyle.None;
                _owner.buttonNavHistoryMenu.Enabled = false;
                _owner.buttonNavHistoryMenu.Size = new Size(13, 0x15);
                _owner.buttonNavHistoryMenu.DropDown = new DropDownMenuBase(_owner.components, true, true, true);
                _owner.buttonNavHistoryMenu.DropDown.ItemClicked += _owner._explorerControllerModule.NavigationButton_DropDownMenu_ItemClicked;
                _owner.buttonNavHistoryMenu.DropDownOpening += _owner._explorerControllerModule.NavigationButtons_DropDownOpening;
                _owner.buttonNavHistoryMenu.DropDown.ImageList = QTUtility.ImageListGlobal;

                _owner.tabControl1.SetRedraw(false);
                _owner.tabControl1.TabPages.Add(_owner.CurrentTab);
                _owner.tabControl1.Dock = DockStyle.Fill;
                _owner.tabControl1.ContextMenuStrip = _owner.contextMenuTab;
                _owner.tabControl1.RefreshOptions(true);
                _owner._tabManager = new TabManager(_owner);
                _owner._menuController = new MenuController(_owner);
                _owner._dragDropController = new DragDropController(_owner);
                _owner._hookInputController = new HookInputController(_owner);
                _owner._fileToolsController = new FileToolsController(_owner);
                _owner._bindActionController = new BindActionController(_owner);
                _owner._shellCommandController = new ShellCommandController(_owner);
                _owner._listViewInputController = new ListViewInputController(_owner);
                _owner._keyboardAcceleratorController = new KeyboardAcceleratorController(_owner);
                _owner._shellUiController = new ShellUiController(_owner);
                _owner._buttonBarClickController = new ButtonBarClickController(_owner);
                _owner._bandInfoController = new BandInfoController(_owner);
                _owner._bandLifecycleController = new BandLifecycleController(_owner);
                _owner._shellNavigationController = new ShellNavigationController(_owner);
                _owner._tabTooltipController = new TabTooltipController(_owner);
                _owner._windowManagementController = new WindowManagementController(_owner);
                _owner._bandWindowController = new BandWindowController(_owner);
                _owner._droppedFilesController = new DroppedFilesController(_owner);
                _owner._folderTreeController = new FolderTreeController(_owner);
                _owner._viewModeController = new ViewModeController(_owner);
                _owner._pluginMenuController = new PluginMenuController(_owner);
                _owner._shutdownController = new ShutdownController(_owner);
                _owner.tabControl1.RowCountChanged += _owner.tabControl1_RowCountChanged;
                _owner.tabControl1.Deselecting += _owner.tabControl1_Deselecting;
                _owner.tabControl1.Selecting += _owner.tabControl1_Selecting;
                _owner.tabControl1.SelectedIndexChanged += _owner.tabControl1_SelectedIndexChanged;
                _owner.tabControl1.GotFocus += _owner.Controls_GotFocus;
                _owner.tabControl1.MouseEnter += _owner.tabControl1_MouseEnter;
                _owner.tabControl1.MouseLeave += _owner.tabControl1_MouseLeave;
                _owner.tabControl1.MouseDown += _owner.tabControl1_MouseDown;
                _owner.tabControl1.MouseUp += _owner.tabControl1_MouseUp;
                _owner.tabControl1.MouseMove += _owner.tabControl1_MouseMove;
                _owner.tabControl1.MouseDoubleClick += _owner.tabControl1_MouseDoubleClick;
                _owner.tabControl1.ItemDrag += _owner._tabManager.tabControl1_ItemDrag;
                _owner.tabControl1.PointedTabChanged += _owner._tabManager.tabControl1_PointedTabChanged;
                _owner.tabControl1.TabCountChanged += _owner._tabManager.tabControl1_TabCountChanged;
                _owner.tabControl1.CloseButtonClicked += _owner.tabControl1_CloseButtonClicked;
                _owner.tabControl1.TabIconMouseDown += _owner.tabControl1_TabIconMouseDown;
                _owner.tabControl1.PlusButtonClicked += _owner.tabControl1_PlusButtonClicked;

                _owner.contextMenuTab.Items.Add(new ToolStripMenuItem());
                _owner.contextMenuTab.ShowImageMargin = false;
                _owner.contextMenuTab.ItemClicked += _owner._menuController.contextMenuTab_ItemClicked;
                _owner.contextMenuTab.Opening += _owner._menuController.contextMenuTab_Opening;
                _owner.contextMenuTab.Closed += _owner._tabManager.contextMenuTab_Closed;
                _owner.contextMenuSys.Items.Add(new ToolStripMenuItem());
                _owner.contextMenuSys.ShowImageMargin = false;
                _owner.contextMenuSys.ItemClicked += _owner._menuController.contextMenuSys_ItemClicked;
                _owner.contextMenuSys.Opening += _owner._menuController.contextMenuSys_Opening;
                _owner.Controls.Add(_owner.tabControl1);
                if(flag) {
                    _owner.Controls.Add(_owner.toolStrip);
                }
                int scaledHeight = ComputeBandHeight(1, Config.Skin.TabHeight, _owner.GetBandDpiScale());
                _owner.MinSize = new Size(150, scaledHeight);
                _owner.Height = scaledHeight;
                _owner.BandHeight = scaledHeight;
                _owner.ContextMenuStrip = _owner.contextMenuSys;
                _owner.MouseDoubleClick += _owner.QTTabBarClass_MouseDoubleClick;
                _owner.MouseUp += _owner.QTTabBarClass_MouseUp;
                _owner.tabControl1.ResumeLayout(false);
                _owner.contextMenuSys.ResumeLayout(false);
                _owner.contextMenuTab.ResumeLayout(false);
                if(flag) {
                    _owner.toolStrip.ResumeLayout(false);
                    _owner.toolStrip.PerformLayout();
                }
                _owner.ResumeLayout(false);
            }
        }
    }
}
