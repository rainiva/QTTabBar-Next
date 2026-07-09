using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal List<string> CloseAllTabsExcept(QTabItem leaveThisOne, bool leaveLocked = true) {
            List<QTabItem> tabs = tabControl1.TabPages.Where(item =>
                !(leaveLocked && item.TabLocked) && item != leaveThisOne).ToList();
            List<string> paths = tabs.Select(tab => tab.CurrentPath).ToList();
            CloseTabs(tabs, !leaveLocked);
            return paths;
        }

        internal void CloseLeftRight(bool fLeft, int index) {
            if(index == -1) {
                index = tabControl1.SelectedIndex;
            }
            if(fLeft ? (index <= 0) : (index >= (tabControl1.TabCount - 1))) {
                return;
            }
            CloseTabs(fLeft
                    ? tabControl1.TabPages.Take(index).ToList()
                    : tabControl1.TabPages.Skip(index + 1).ToList());
        }

        internal bool HandleCLOSE(IntPtr lParam) {
            bool flag = Config.Window.CloseBtnClosesSingleTab;
            bool flag2 = Config.Window.CloseBtnClosesUnlocked;
            List<string> closingPaths = new List<string>();
            int num = (int)lParam;
            switch(num) {
                case 1:
                    closingPaths = CloseAllTabsExcept(null, flag2);
                    if(tabControl1.TabCount > 0) {
                        return true;
                    }
                    break;

                case 2:
                    return false;

                default: {
                        bool flag3 = QTUtility2.PathExists(CurrentTab.CurrentPath);
                        if((OSDetector.IsXP && flag3) && (num == 0)) {
                            return true;
                        }
                        if(!flag3) {
                            CloseTab(CurrentTab, true);
                            return (tabControl1.TabCount > 0);
                        }
                        if(flag2 && !flag) {
                            closingPaths = CloseAllTabsExcept(null);
                            if(tabControl1.TabCount > 0) {
                                return true;
                            }
                            QTUtility.SaveClosing(closingPaths);
                            return false;
                        }
                        Keys modifierKeys = Control.ModifierKeys;
                        if((modifierKeys == (Keys.Control | Keys.Shift)) || !flag) {
                            foreach(QTabItem item2 in tabControl1.TabPages) {
                                closingPaths.Add(item2.CurrentPath);
                                AddToHistory(item2);
                            }
                            QTUtility.SaveClosing(closingPaths);
                            return false;
                        }
                        if(modifierKeys == Keys.Control) {
                            closingPaths = CloseAllTabsExcept(null);
                        }
                        else {
                            closingPaths.Add(CurrentTab.CurrentPath);
                            CloseTab(CurrentTab, false);
                        }
                        if(tabControl1.TabCount > 0) {
                            return true;
                        }
                        QTUtility.SaveClosing(closingPaths);
                        return false;
                    }
            }
            QTUtility.SaveClosing(closingPaths);
            return false;
        }
    }
}
