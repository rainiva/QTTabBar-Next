using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        protected virtual void PerformBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
        }

        protected virtual QTabItem CloneTabButtonForMouse(QTabItem tab, string optionURL, bool fSelect, int index) {
            return null;
        }

        protected virtual void ShowSubdirTipForTab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
        }

        protected Cursor GetTabDragCursor(bool fDragging) {
            return fDragging ?
                curTabDrag ?? (curTabDrag = CreateDragCursor(Resources_Image.imgCurTabDrag)) :
                curTabCloning ?? (curTabCloning = CreateDragCursor(Resources_Image.imgCurTabCloning));
        }

        protected static Cursor CreateDragCursor(Bitmap bmpColor) {
            Cursor cursor;
            using(bmpColor) {
                using(Bitmap bitmap = new Bitmap(0x20, 0x20)) {
                    ICONINFO piconinfo = new ICONINFO();
                    piconinfo.fIcon = false;
                    piconinfo.hbmColor = bmpColor.GetHbitmap();
                    piconinfo.hbmMask = bitmap.GetHbitmap();
                    try {
                        cursor = new Cursor(PInvoke.CreateIconIndirect(ref piconinfo));
                    }
                    catch {
                        cursor = Cursors.Default;
                    }
                }
            }
            return cursor;
        }

        public void tabControl1_CloseButtonClicked(object sender, QTabCancelEventArgs e) {
            if(NowTabDragging) {
                Cursor = Cursors.Default;
                NowTabDragging = false;
                DraggingTab = null;
                DraggingDestRect = Rectangle.Empty;
                TryCallButtonBar(bbar => bbar.RefreshButtons());
                e.Cancel = true;
            }
            else if(!Explorer.Busy) {
                if(tabControl1.TabCount > 1) {
                    e.Cancel = !CloseTab(e.TabPage);
                }
                else {
                    LockedTabsService.PersistFromTabs(tabControl1.TabPages);
                    WindowUtils.CloseExplorer(ExplorerHandle, 1);
                }
            }
        }

        public void tabControl1_MouseDoubleClick(object sender, MouseEventArgs e) {
            if((Control.ModifierKeys != Keys.Control) && (e.Button == MouseButtons.Left)) {
                QTabItem tabMouseOn = tabControl1.GetTabMouseOn();
                if(tabMouseOn != null) {
                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, Control.ModifierKeys);
                    BindAction action;
                    if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                        QTLogger.log("TabBarBase tabControl1_MouseDoubleClick " + action);
                        PerformBindAction(action, false, DraggingTab);
                    }
                }
                else {
                    OnMouseDoubleClick(e);
                }
            }
        }

        public void tabControl1_MouseDown(object sender, MouseEventArgs e) {
            QTabItem tabMouseOn = tabControl1.GetTabMouseOn();
            DraggingTab = null;
            if(tabMouseOn != null) {
                if(e.Button == MouseButtons.Left) {
                    NowTabDragging = true;
                    DraggingTab = tabMouseOn;
                }
                else if(e.Button == MouseButtons.Right) {
                    SetContextMenuedTab(tabMouseOn);
                }
            }
        }

        public void tabControl1_MouseEnter(object sender, EventArgs e) {
            if(pluginServer != null) {
                pluginServer.OnMouseEnter();
            }
        }

        public void tabControl1_MouseLeave(object sender, EventArgs e) {
            if(pluginServer != null) {
                pluginServer.OnMouseLeave();
            }
        }

        public void tabControl1_MouseMove(object sender, MouseEventArgs e) {
            RECT rect;
            if((tabControl1.Capture && (((e.X < 0) || (e.Y < 0)) || ((e.X > tabControl1.Width) || (e.Y > tabControl1.Height)))) && (PInvoke.GetWindowRect(ReBarHandle, out rect) && !PInvoke.PtInRect(ref rect, tabControl1.PointToScreen(e.Location)))) {
                Cursor = Cursors.Default;
                tabControl1.Capture = false;
            }
            else if((NowTabDragging && (DraggingTab != null)) && ((Control.ModifierKeys & Keys.Shift) != Keys.Shift)) {
                if(Explorer.Busy || (Control.MouseButtons != MouseButtons.Left)) {
                    NowTabDragging = false;
                }
                else {
                    int num;
                    QTabItem tabMouseOn = tabControl1.GetTabMouseOn(out num);
                    int index = tabControl1.TabPages.IndexOf(DraggingTab);
                    if((num > (tabControl1.TabCount - 1)) || (num < 0)) {
                        if((num == -1) && (Control.ModifierKeys == Keys.Control)) {
                            Cursor = GetTabDragCursor(false);
                            DraggingDestRect = new Rectangle(1, 0, 0, 0);
                        }
                        else {
                            Cursor = Cursors.Default;
                        }
                    }
                    else if((index <= (tabControl1.TabCount - 1)) && (index >= 0)) {
                        Rectangle tabRect = tabControl1.GetTabRect(num, false);
                        Rectangle rectangle2 = tabControl1.GetTabRect(index, false);
                        if(tabMouseOn != null) {
                            if(tabMouseOn != DraggingTab) {
                                if(!DraggingDestRect.Contains(tabControl1.PointToClient(MousePosition))) {
                                    Cursor = GetTabDragCursor(true);
                                    bool flag = tabMouseOn.Row != DraggingTab.Row;
                                    bool flag2 = tabControl1.SelectedTab != DraggingTab;
                                    tabControl1.TabPages.Relocate(index, num);
                                    if(num < index) {
                                        DraggingDestRect = new Rectangle(tabRect.X + rectangle2.Width, tabRect.Y, tabRect.Width - rectangle2.Width, tabRect.Height);
                                    }
                                    else {
                                        DraggingDestRect = new Rectangle(tabRect.X, tabRect.Y, tabRect.Width - rectangle2.Width, tabRect.Height);
                                    }
                                    if((flag && !flag2) && !Config.Tabs.MultipleTabRows) {
                                        Rectangle rectangle3 = tabControl1.GetTabRect(num, false);
                                        Point p = new Point(rectangle3.X + (rectangle3.Width / 2), rectangle3.Y + (Config.Skin.TabHeight / 2));
                                        Cursor.Position = tabControl1.PointToScreen(p);
                                    }
                                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                                }
                            }
                            else if((curTabCloning != null) && (Cursor == curTabCloning)) {
                                Cursor = GetTabDragCursor(true);
                            }
                        }
                    }
                }
            }
        }

        public void tabControl1_MouseUp(object sender, MouseEventArgs e) {
            if(tabControl1 == null || tabControl1.IsDisposed) {
                return;
            }
            QTabItem tabMouseOn = tabControl1.GetTabMouseOn();
            if(NowTabDragging && e.Button == MouseButtons.Left) {
                Keys modifierKeys = Control.ModifierKeys;
                if(tabMouseOn == null) {
                    if(DraggingTab != null && (modifierKeys == Keys.Control || modifierKeys == (Keys.Control | Keys.Shift))) {
                        bool cloning = false;
                        Point pt = tabControl1.PointToScreen(e.Location);
                        if(!OSDetector.IsXP) {
                            RECT rect;
                            PInvoke.GetWindowRect(ReBarHandle, out rect);
                            cloning = PInvoke.PtInRect(ref rect, pt);
                        }
                        else {
                            RECT rect2;
                            IntPtr ptr;
                            if(ButtonBarRegistry.TryGetButtonBarHandle(ExplorerHandle, out ptr) && PInvoke.IsWindowVisible(ptr)) {
                                PInvoke.GetWindowRect(ptr, out rect2);
                                if(PInvoke.PtInRect(ref rect2, pt)) {
                                    cloning = true;
                                }
                            }
                            PInvoke.GetWindowRect(Handle, out rect2);
                            if(PInvoke.PtInRect(ref rect2, pt)) {
                                cloning = true;
                            }
                        }
                        if(cloning) {
                            CloneTabButtonForMouse(DraggingTab, null, false, tabControl1.TabCount);
                        }
                    }
                }
                else if(tabMouseOn == DraggingTab && DraggingDestRect == Rectangle.Empty) {
                    MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Left, Control.ModifierKeys);
                    BindAction action;
                    if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                        QTLogger.log("TabBarBase DraggingTab " + action);
                        PerformBindAction(action, false, DraggingTab);
                    }
                }
                NowTabDragging = false;
                DraggingTab = null;
                DraggingDestRect = Rectangle.Empty;
                TryCallButtonBar(bbar => bbar.RefreshButtons());
            }
            else if(e.Button == MouseButtons.Middle && !Explorer.Busy && tabMouseOn != null) {
                DraggingTab = null;
                NowTabDragging = false;
                MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, Control.ModifierKeys);
                BindAction action;
                if(Config.Mouse.TabActions.TryGetValue(chord, out action)) {
                    QTLogger.log("TabBarBase MouseButtons.Middle " + action);
                    PerformBindAction(action, false, tabMouseOn);
                }
            }
            else if(tabMouseOn == null) {
                NowTabDragging = false;
                if(DraggingTab == null) {
                    OnMouseUp(e);
                }
                DraggingTab = null;
            }
            Cursor = Cursors.Default;
        }

        public void tabControl1_TabIconMouseDown(object sender, QTabCancelEventArgs e) {
            ShowSubdirTipForTab(e.TabPage, e.Action == TabControlAction.Selecting, e.TabPageIndex, false, e.Cancel);
        }

        public void QTTabBarClass_MouseDoubleClick(object sender, MouseEventArgs e) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, Control.ModifierKeys);
            BindAction action;
            if(Config.Mouse.BarActions.TryGetValue(chord, out action)) {
                QTLogger.log("TabBarBase_MouseDoubleClick " + action);
                PerformBindAction(action);
            }
        }

        public void QTTabBarClass_MouseUp(object sender, MouseEventArgs e) {
            MouseChord chord;
            if(e.Button == MouseButtons.Left) {
                chord = QTUtility.MakeMouseChord(MouseChord.Left, Control.ModifierKeys);
            }
            else if(e.Button == MouseButtons.Middle) {
                chord = QTUtility.MakeMouseChord(MouseChord.Middle, Control.ModifierKeys);
            }
            else {
                return;
            }
            BindAction action;
            if(Config.Mouse.BarActions.TryGetValue(chord, out action)) {
                QTLogger.log("TabBarBase_MouseUp " + action);
                PerformBindAction(action);
            }
        }
    }
}
