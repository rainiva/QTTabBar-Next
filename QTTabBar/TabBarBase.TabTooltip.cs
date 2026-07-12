using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;
using ToolTip = System.Windows.Forms.ToolTip;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal SubDirTipForm subDirTip_Tab;
        protected QTabItem tabForDD;
        protected Timer timerOnTab;
        protected ToolTip toolTipForDD;
        protected int iModKeyStateDD;
        protected bool fToggleTabMenu;

        private const int INTERVAL_SELCTTAB = 5000;
        private const int INTERVAL_SHOWMENU = 0x4b0;

        internal void HideSubDirTip_Tab_Menu() {
            if(subDirTip_Tab != null) {
                subDirTip_Tab.HideMenu();
            }
        }

        internal void HideToolTipForDD() {
            tabForDD = null;
            iModKeyStateDD = 0;
            if(toolTipForDD != null) {
                toolTipForDD.Hide(tabControl1);
            }
            if(timerOnTab != null) {
                timerOnTab.Enabled = false;
            }
        }

        internal void ShowSubdirTip_Tab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
            try {
                if(fShow) {
                    if(Explorer.Busy || string.IsNullOrEmpty(tab.CurrentPath)) {
                        tabControl1.SetSubDirTipShown(false);
                    }
                    else if(PathValidator.IsNetPath(tab.CurrentPath)) {
                        tabControl1.SetSubDirTipShown(false);
                    }
                    else {
                        string currentPath = tab.CurrentPath;
                        if(fParent || ShellMethods.TryMakeSubDirTipPath(ref currentPath)) {
                            if(subDirTip_Tab == null) {
                                subDirTip_Tab = new SubDirTipForm(Handle, true, listView);
                                WireSubDirTipTabEvents(subDirTip_Tab);
                            }
                            SetContextMenuedTab(tab);
                            Point pnt = tabControl1.PointToScreen(new Point(tab.TabBounds.X + offsetX, fParent ? tab.TabBounds.Top : (tab.TabBounds.Bottom - 3)));
                            if(tab != CurrentTab) {
                                pnt.X += 2;
                            }
                            tabControl1.SetSubDirTipShown(subDirTip_Tab.ShowMenuWithoutShowForm(currentPath, pnt, fParent));
                        }
                        else {
                            tabControl1.SetSubDirTipShown(false);
                            HideSubDirTip_Tab_Menu();
                        }
                    }
                }
                else {
                    HideSubDirTip_Tab_Menu();
                }
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception, "tabsubdir");
            }
        }

        internal virtual void WireSubDirTipTabEvents(SubDirTipForm form) {
            form.MenuClosed += subDirTip_Tab_MenuClosed;
        }

        internal void ShowToolTipForDD(QTabItem tab, int iState, int grfKeyState) {
            if(((tabForDD == null) || (tabForDD != tab)) || (iModKeyStateDD != grfKeyState)) {
                tabForDD = tab;
                iModKeyStateDD = grfKeyState;
                if(timerOnTab == null) {
                    timerOnTab = new Timer(components);
                    timerOnTab.Tick += timerOnTab_Tick;
                }
                timerOnTab.Enabled = false;
                timerOnTab.Interval = Config.Tabs.DragOverTabOpensSDT ? INTERVAL_SHOWMENU : INTERVAL_SELCTTAB;
                timerOnTab.Enabled = true;
                if(Config.Tabs.DragOverTabOpensSDT && (iState != -1)) {
                    Rectangle tabRect = tabControl1.GetTabRect(tab);
                    Point lpPoints = new Point(tabRect.X + ((tabRect.Width * 3) / 4), tabRect.Bottom + 0x10);
                    string[] strArray = ResourceCache.TextResourcesDic["DragDropToolTip"];
                    string str;
                    switch((grfKeyState & 12)) {
                        case 4:
                            str = strArray[1];
                            break;

                        case 8:
                            str = strArray[0];
                            break;

                        case 12:
                            str = strArray[2];
                            break;

                        default:
                            if(iState == 1) {
                                str = strArray[0];
                            }
                            else {
                                str = strArray[1];
                            }
                            break;
                    }
                    if(toolTipForDD == null) {
                        toolTipForDD = new ToolTip(components);
                        toolTipForDD.UseAnimation = toolTipForDD.UseFading = false;
                    }
                    toolTipForDD.ToolTipTitle = str;
                    if(PInvoke.GetForegroundWindow() != ExplorerHandle) {
                        Type type = typeof(ToolTip);
                        const BindingFlags bindingAttr = BindingFlags.NonPublic | BindingFlags.Instance;
                        MethodInfo method = type.GetMethod("SetTrackPosition", bindingAttr);
                        MethodInfo info2 = type.GetMethod("SetTool", bindingAttr);
                        PInvoke.MapWindowPoints(tabControl1.Handle, IntPtr.Zero, ref lpPoints, 1);
                        method.Invoke(toolTipForDD, new object[] { lpPoints.X, lpPoints.Y });
                        info2.Invoke(toolTipForDD, new object[] { tabControl1, tab.CurrentPath, 2, lpPoints });
                    }
                    else {
                        toolTipForDD.Active = true;
                        toolTipForDD.Show(tab.CurrentPath, tabControl1, lpPoints);
                    }
                }
            }
        }

        internal void subDirTip_Tab_MenuClosed(object sender, EventArgs e) {
            tabControl1.SetSubDirTipShown(false);
            tabControl1.RefreshFolderImage();
        }

        internal void timerOnTab_Tick(object sender, EventArgs e) {
            timerOnTab.Enabled = false;
            QTabItem tabMouseOn = tabControl1.GetTabMouseOn();
            if(((tabMouseOn != null) && (tabMouseOn == tabForDD)) && tabControl1.TabPages.Contains(tabMouseOn)) {
                if(Config.Tabs.DragOverTabOpensSDT) {
                    WindowUtils.BringExplorerToFront(ExplorerHandle);
                    ShowSubdirTip_Tab(tabMouseOn, true, tabControl1.TabOffset, false, fToggleTabMenu);
                    fToggleTabMenu = !fToggleTabMenu;
                    timerOnTab.Enabled = true;
                    if(toolTipForDD != null) {
                        toolTipForDD.Active = false;
                    }
                }
                else {
                    tabControl1.SelectTab(tabMouseOn);
                }
            }
        }
    }
}
