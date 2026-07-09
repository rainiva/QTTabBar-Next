using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        /// <summary>
        /// Handles bind actions that only require TabBarBase APIs (shared by main and second view bars).
        /// Returns true when the action was handled.
        /// </summary>
        internal bool TryDoBindActionCore(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
            if(fRepeat && !(
                    action == BindAction.GoBack ||
                    action == BindAction.GoForward ||
                    action == BindAction.TransparencyPlus ||
                    action == BindAction.TransparencyMinus)) {
                return false;
            }

            if(tab == null) {
                tab = CurrentTab;
            }

            switch(action) {
                case BindAction.NextTab:
                    if(tabControl1.SelectedIndex == tabControl1.TabCount - 1) {
                        tabControl1.SelectTab(0);
                    }
                    else {
                        tabControl1.SelectTab(tabControl1.SelectedIndex + 1);
                    }
                    return true;

                case BindAction.PreviousTab:
                    if(tabControl1.SelectedIndex == 0) {
                        tabControl1.SelectTab(tabControl1.TabCount - 1);
                    }
                    else {
                        tabControl1.SelectTab(tabControl1.SelectedIndex - 1);
                    }
                    return true;

                case BindAction.FirstTab:
                    tabControl1.SelectTab(0);
                    return true;

                case BindAction.LastTab:
                    tabControl1.SelectTab(tabControl1.TabCount - 1);
                    return true;

                case BindAction.CloseCurrent:
                case BindAction.CloseTab:
                    NowTabDragging = false;
                    if(tab != null && !tab.TabLocked) {
                        if(tabControl1.TabCount > 1) {
                            CloseTab(tab);
                        }
                        else {
                            WindowUtils.CloseExplorer(ExplorerHandle, 1);
                        }
                    }
                    return true;

                case BindAction.CloseAllButCurrent:
                case BindAction.CloseAllButThis:
                    CloseAllTabsExcept(tab);
                    return true;

                case BindAction.CloseLeft:
                case BindAction.CloseLeftTab:
                    CloseLeftRight(true, tab != null ? tab.Index : -1);
                    return true;

                case BindAction.CloseRight:
                case BindAction.CloseRightTab:
                    CloseLeftRight(false, tab != null ? tab.Index : -1);
                    return true;

                case BindAction.CloseWindow: {
                        string[] list = (from QTabItem item2 in tabControl1.TabPages
                                         where item2.TabLocked
                                         select item2.CurrentPath).ToArray();
                        QTUtility.SaveLockedTabs(list);
                        WindowUtils.CloseExplorer(ExplorerHandle, 1);
                    }
                    return true;

                case BindAction.CloneCurrent:
                case BindAction.CloneTab:
                    CloneTabButtonCore(tab, null, true, -1);
                    return true;

                case BindAction.LockCurrent:
                case BindAction.LockTab:
                    if(tab != null) {
                        tab.TabLocked = !tab.TabLocked;
                    }
                    return true;

                case BindAction.LockAll: {
                        bool lockState = tabControl1.TabPages.Any(t => t.TabLocked);
                        tabControl1.TabPages.ForEach(t => t.TabLocked = !lockState);
                    }
                    return true;

                case BindAction.SwitchToLastActivated:
                    if(lstActivatedTabs.Count > 1 &&
                       tabControl1.TabPages.Contains(lstActivatedTabs[lstActivatedTabs.Count - 2])) {
                        try {
                            tabControl1.SelectTab(lstActivatedTabs[lstActivatedTabs.Count - 2]);
                        }
                        catch(System.ArgumentException) {
                        }
                    }
                    return true;

                case BindAction.CopyTabPath:
                    if(tab != null) {
                        string currentPath = tab.CurrentPath;
                        if(currentPath.IndexOf("???") != -1) {
                            currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                        }
                        QTUtility2.SetStringClipboard(currentPath);
                    }
                    return true;

                case BindAction.TabProperties:
                    if(tab != null) {
                        ShellMethods.ShowProperties(tab.CurrentIDL);
                    }
                    return true;

                case BindAction.NewTab:
                    using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                        OpenNewTab(wrapper, false, true);
                    }
                    return true;

                case BindAction.FocusTabBar:
                    tabControl1.Focus();
                    tabControl1.FocusNextTab(false, true, false);
                    return true;
            }

            return false;
        }
    }
}
