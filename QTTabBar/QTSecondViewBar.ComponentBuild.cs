//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2023  indiff
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.ExplorerBrowser;

namespace QTTabBarLib
{
    public sealed partial class QTSecondViewBar
    {
        private void InitializeComponent()
        {
            this.RightToLeft = RightToLeft.Yes;
            this.viewContainer = new Panel();
            this.splitContainer = new SplitContainer();
            this.controlContainer = new Panel();
            this.addressBarContainer = new SplitContainer();
            this.components = (IContainer)new Container();


            // contextMenuTab = new ContextMenuStripEx(components, false);
            // contextMenuSys = new ContextMenuStripEx(components, false);

            // 
            // viewContainer
            // 
            // 使用 Anchor 属性可以定义在调整控件的父控件大小时如何自动调整控件的大小。将控件锚定到其父控件后，可确保当调整父控件的大小时锚定的边缘与父控件的边缘的相对位置保持不变。
            this.viewContainer.Anchor = AnchorStyles.Top |
                                        AnchorStyles.Bottom |
                                        AnchorStyles.Left |
                                        AnchorStyles.Right
                ;
            this.viewContainer.BackColor = Color.Transparent;
            // this.viewContainer.BackColor = Color.Beige;
            this.viewContainer.Dock = DockStyle.Fill;
            this.viewContainer.Location = new Point(0, 0);
            this.viewContainer.Size = new Size(100, 256);
            this.viewContainer.Name = "viewContainer";
            this.viewContainer.TabIndex = 1;
            this.viewContainer.Padding = new Padding(0, 0, 0, 2);
            this.viewContainer.Margin = Padding.Empty;
            this.viewContainer.TabIndex = 0;
            // this.viewContainer.Text = "view container";

            /*if (CurrentLocation == null)
            {
                CurrentLocation = (ShellObject)KnownFolders.Computer;
                QTLogger.log("CurrentLocation init");
            }*/

            explorerBrowser = new ExplorerBrowser.WindowsForms.ExplorerBrowser();
            explorerBrowser.Dock = DockStyle.Fill;
            // explorerBrowser.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Show;
            explorerBrowser.NavigationOptions.PaneVisibility.Navigation = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.Commands = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.AdvancedQuery = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.CommandsOrganize = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.CommandsView = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.Preview = PaneVisibilityState.Hide;
            explorerBrowser.NavigationOptions.PaneVisibility.Query = PaneVisibilityState.Hide;

            viewContainer.Controls.Add(explorerBrowser);

            this.tabControl1 = new QTabControl();
            tabControl1.SuspendLayout();
            tabControl1.SetRedraw(false);
            using (IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation))
            {
                CurrentTab = new QTabItem(QTUtility2.MakePathDisplayText(wrapper.Path, false), 
                    wrapper.Path,
                    tabControl1);
                // tab.NavigatedTo(wrapper.Path, wrapper.IDL, -1, false);
                CurrentTab.ToolTipText = QTUtility2.MakePathDisplayText(wrapper.Path, true);
            }

            // CurrentTab = new QTabItem(string.Empty, string.Empty, tabControl1);
            tabControl1.TabPages.Add(CurrentTab);
            tabControl1.Dock = DockStyle.Fill;
            // tabControl1.ContextMenuStrip = contextMenuTab;
            tabControl1.RefreshOptions(true);
            tabControl1.Selecting += tabControl1_Selecting;
            tabControl1.Deselecting += tabControl1_Deselecting;
            tabControl1.SelectedIndexChanged += tabControl1_SelectedIndexChanged;
            tabControl1.MouseEnter += tabControl1_MouseEnter;
            tabControl1.MouseLeave += tabControl1_MouseLeave;
            tabControl1.MouseDown += tabControl1_MouseDown;
            tabControl1.MouseUp += tabControl1_MouseUp;
            tabControl1.MouseMove += tabControl1_MouseMove;
            tabControl1.MouseDoubleClick += tabControl1_MouseDoubleClick;
            tabControl1.CloseButtonClicked += tabControl1_CloseButtonClicked;
            tabControl1.TabIconMouseDown += tabControl1_TabIconMouseDown;


            // contextMenuTab.Items.Add(new ToolStripMenuItem());
            // contextMenuTab.ShowImageMargin = false;
            // contextMenuSys.Items.Add(new ToolStripMenuItem());
            // contextMenuSys.ShowImageMargin = false;


            // contextMenuTab.ItemClicked += contextMenuTab_ItemClicked;
            // contextMenuTab.Opening += contextMenuTab_Opening;
            // contextMenuTab.Closed += contextMenuTab_Closed;
            //
            // contextMenuSys.ItemClicked += contextMenuSys_ItemClicked;
            // contextMenuSys.Opening += contextMenuSys_Opening;

            this.tabControl1.Text = "QTTabBar TabControl";

            tabControl1.PlusButtonClicked += tabControl1_PlusButtonClicked;

            
            // 标签组件容器
            this.controlContainer.BackColor = System.Drawing.Color.Transparent;
            // this.controlContainer.Dock = DockStyle.Bottom;
            // this.controlContainer.Dock = DockStyle.Top;
            this.controlContainer.Dock = DockStyle.Left;
            this.controlContainer.Location = new Point(0, 0);
            // this.controlContainer.Location = new Point(20, 100);
            // this.controlContainer.Location = new Point(0, 256);
            // this.controlContainer.Size = new Size(256, CalcBandHeight(1));
            this.controlContainer.Size = new Size(100, Config.Skin.TabHeight );
            this.controlContainer.Margin = Padding.Empty;
            // this.controlContainer.Padding = Padding.Empty;
            this.controlContainer.Padding = new Padding(0, 2, 5, 0);
            // this.controlContainer.Padding = this.ControlContainerPadding;
            this.controlContainer.TabIndex = 1;
            this.controlContainer.Text = "ControlContainer";
            this.controlContainer.Visible = true;

            // 添加标签栏
            this.controlContainer.Controls.Add(this.tabControl1);

            // 获取或设置控件绑定到的容器的边缘并确定控件如何随其父级一起调整大小。（即指控件挂靠的方向）
            // this.controlContainer.Anchor = AnchorStyles.Top |
            //                                AnchorStyles.Bottom |
            //                                AnchorStyles.Left |
            //                                AnchorStyles.Right;

                // !this.GetObjectForExtraViewBar<bool>(Config.Bool(Scts.ExtraViewNoTab2nd), Config.Bool(Scts.ExtraViewNoTab3rd));

            // explorerBrowser.NavigationPending += new EventHandler<NavigationPendingEventArgs>(explorerBrowser_NavigationPending);
            // explorerBrowser.NavigationComplete += explorerBrowser_NavigationComplete;
            // explorerBrowser.NavigationFailed +=  new EventHandler<NavigationFailedEventArgs>(explorerBrowser_NavigationFailed);

            this.splitContainer.BackColor = Color.Transparent;
            // this.splitContainer.BackColor = this.IsVertical ? this.VerticalExplorerBarBackgroundColor : System.Drawing.Color.Transparent;
            this.splitContainer.Dock = DockStyle.Fill;
            this.splitContainer.FixedPanel = FixedPanel.Panel1;
            this.splitContainer.Location = new Point(0, 0);
            this.splitContainer.Margin = Padding.Empty;
            // this.splitContainer.Panel1.Padding = new Padding(2, 0, 0, 0);

            this.splitContainer.Panel1Collapsed = true;
            this.splitContainer.Panel1.Padding = Padding.Empty;
                // new Padding(0, Graphic.SelectValueByScaling<int>(ExplorerManager.WindowScaling, 16, 11, 6), 0, 0);

            // 添加 视图容器、标签容器
            // this.splitContainer.Panel2.BackColor = Color.Transparent;
            this.splitContainer.Panel2.Controls.Add((Control)this.viewContainer);
            this.splitContainer.Panel2.Controls.Add((Control)this.controlContainer);
            // this.splitContainer.Panel2.Padding = new Padding(2, 0, 0, 0);
            this.splitContainer.Panel2.Padding = Padding.Empty;
                // this.IsVertical ? Graphic.Translate(new Padding(2, 0, 0, 0)) : Padding.Empty;
            this.splitContainer.Size = new Size(256, 256);
            this.splitContainer.Margin = Padding.Empty;
            // this.splitContainer.SplitterDistance = 48;
            // this.splitContainer.SplitterDistance = 10;
            
            // this.splitContainer.SplitterWidth = 6;
            // this.splitContainer.SplitterWidth = Graphic.ScaleBy(ExplorerManager.WindowScaling, 6);
            // this.splitContainer.SplitterMoving += new SplitterCancelEventHandler(this.splitContainer_SplitterMoving);
            // this.splitContainer.SplitterMoved += new SplitterEventHandler(this.splitContainer_SplitterMoved);
            this.splitContainer.TabIndex = 0;
            
            this.Controls.Add((Control)this.splitContainer);
            this.MinSize = new Size(16, 64);
            this.MaxSize = new Size(this.MaxSize.Width, -1);
            this.Margin = this.Padding = Padding.Empty;
            this.Font = Graphic.CreateDefaultFont();

            contextMenuTab = new ContextMenuStripEx(components, false);
            contextMenuSys = new ContextMenuStripEx(components, false);
            tabControl1.SuspendLayout();
            contextMenuSys.SuspendLayout();
            contextMenuTab.SuspendLayout();
            SuspendLayout();

            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel1.PerformLayout();
            this.splitContainer.Panel2.ResumeLayout(false);
            this.splitContainer.Panel2.PerformLayout();
            this.splitContainer.ResumeLayout(false);
            this.viewContainer.ResumeLayout(false);
            this.controlContainer.ResumeLayout(false);
            this.tabControl1.ResumeLayout(false);
            this.ResumeLayout(false);
        }

        public Padding ControlContainerPadding
        {
            get
            {
                int left = QTUtility.LaterThan7 ? 1 : 0;
                // Config.Get(Scts.ExtraViewTabBarPos3rd) == 1
                return this.IsVertical && true ?
                    (!QTUtility.RightToLeft ? new Padding(left, 2, 5, 0) :
                        new Padding(5, 2, 0, 0)) 
                    :
                    (!QTUtility.RightToLeft ? new Padding(left, 1, 5, 0) : 
                        new Padding(5, 1, 0, 0));

            }
        }

        /*protected  int CalcBandHeight(int count)
        {
            return count * 
                (Graphic.ScaleBy(ExplorerManager.WindowScaling, Config.Skin.TabHeight) - 3) + 
                3 + 
                this.ControlContainerPadding.Vertical;
        }*/

   
        private void InitializeContextMenus()
        {
            this.contextMenuTab = new ContextMenuStripEx(this.components, true);
            // this.contextMenuTab.ImageList = ;
            this.contextMenuTab.Items.Add((ToolStripItem)new ToolStripMenuItem("测试"));
            /*this.contextMenuTab.ItemClicked += new ToolStripItemClickedEventHandler(this.contextMenuTab_ItemClicked);
            this.contextMenuTab.Opening += new CancelEventHandler(this.contextMenuTab_Opening);
            this.contextMenuTab.Closed += new ToolStripDropDownClosedEventHandler(this.contextMenuTab_Closed);
            this.tabControl.TabRightClick += new EventHandler<QEventArgs>(this.tabControl_TabRightClick);
            this.tabControl.TabBarRightClick += new EventHandler<QEventArgs>(this.tabControl_TabBarRightClick);
            this.contextMenuBar = new DropDownMenuEx(this.components, false, false, false);
            this.contextMenuBar.ExplorerHandle = this.Handle;
            this.contextMenuBar.ImageList = Graphic.ImageList;
            this.contextMenuBar.Items.Add((ToolStripItem)new ToolStripMenuItem());
            this.contextMenuBar.UsePrefix = true;
            this.contextMenuBar.ItemClicked += new ToolStripItemClickedEventHandler(this.contextMenuBar_ItemClicked);
            this.contextMenuBar.Opening += new CancelEventHandler(this.contextMenuBar_Opening);
            this.contextMenuBar.Closed += new ToolStripDropDownClosedEventHandler(this.contextMenuBar_Closed);*/
        }
    }
}
