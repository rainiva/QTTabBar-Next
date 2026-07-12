using System;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerNavigationController {
        private readonly ITabContext _tabContext;
        private readonly IExplorerNavigationHost _host;
        private readonly Action<string, bool, int> _cancelFailedNavigation;

        internal ExplorerNavigationController(
            ITabContext tabContext,
            IExplorerNavigationHost host,
            Action<string, bool, int> cancelFailedNavigation) {
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
            _host = host;
            _cancelFailedNavigation = cancelFailedNavigation;
        }

        internal void NavigateBranchCurrent(int index) => NavigateBranches(_tabContext.CurrentTab, index);

        internal void NavigateBranches(QTabItem tab, int index) {
            LogData log = tab.Branches[index];
            if(Control.ModifierKeys == Keys.Control) {
                using(IDLWrapper wrapper = new IDLWrapper(log.IDL)) {
                    if(!wrapper.Available) _host.ShowNavigationCanceled(log.Path);
                    else _host.OpenNewWindow(wrapper);
                }
            }
            else if(Control.ModifierKeys == Keys.Shift) _host.CloneTab(tab, log);
            else {
                _host.SelectTab(tab);
                if(_host.IsSpecialTravelPath(log.Path)) {
                    _host.SaveSelectedItems(_tabContext.CurrentTab);
                    _host.SetNavigatedByCode(true);
                    _host.NavigateToPastSpecialDirectory(log.Hash);
                }
                else {
                    _host.SetNavigatedByCode(false);
                    using(IDLWrapper wrapper = new IDLWrapper(log.IDL)) {
                        if(!wrapper.Available) _host.ShowNavigationCanceled(log.Path);
                        else {
                            _host.SaveSelectedItems(_tabContext.CurrentTab);
                            _host.NavigateShell(wrapper);
                        }
                    }
                }
            }
        }

        internal bool NavigateCurrentTab(bool back) {
            QTabItem current = _tabContext.CurrentTab;
            string currentPath = current.CurrentPath;
            LogData data = back ? current.GoBackward() : current.GoForward();
            if(string.IsNullOrEmpty(data.Path)) return false;
            if(current.TabLocked && !data.Path.Contains("*?*?*") && !currentPath.Contains("*?*?*")) {
                try {
                    QTabItem tab = _host.CreateAndInsertClone(current);
                    if(back) current.GoForward(); else current.GoBackward();
                    _host.SelectTab(tab);
                }
                catch(Exception exception) { QTLogger.MakeErrorLog(exception); }
                return true;
            }
            if(_host.IsSpecialTravelPath(data.Path) && _host.HasPastSpecialEntry(data.Hash)) {
                _host.SaveSelectedItems(current);
                _host.SetNavigatedByCode(true);
                return _host.NavigateToPastSpecialDirectory(data.Hash);
            }
            using(IDLWrapper wrapper = new IDLWrapper(data.IDL)) {
                if(!wrapper.Available) {
                    _cancelFailedNavigation(data.Path, back, 1);
                    return false;
                }
                _host.SaveSelectedItems(current);
                _host.SetNavigatedByCode(true);
                return _host.NavigateShell(wrapper);
            }
        }

        internal void NavigateToFirstOrLast(bool back) {
            string[] history = back ? _tabContext.CurrentTab.GetHistoryBack() : _tabContext.CurrentTab.GetHistoryForward();
            if(history.Length > (back ? 1 : 0)) NavigateToHistory(history[history.Length - 1], back, history.Length - 1);
        }

        internal void NavigateToHistory(string displayPath, bool back, int steps) {
            QTabItem current = _tabContext.CurrentTab;
            LogData data = new LogData();
            int rollback = back ? steps : steps + 1;
            for(int i = 0; i < rollback; i++) data = back ? current.GoBackward() : current.GoForward();
            if(string.IsNullOrEmpty(data.Path)) _cancelFailedNavigation("( Unknown Path )", back, rollback);
            else if(current.TabLocked) {
                QTabItem tab = _host.CreateAndInsertClone(current);
                for(int i = 0; i < rollback; i++) { if(back) current.GoForward(); else current.GoBackward(); }
                _host.SelectTab(tab);
            }
            else if(_host.IsSpecialTravelPath(displayPath)) {
                _host.SaveSelectedItems(current);
                _host.SetNavigatedByCode(true);
                _host.NavigateToPastSpecialDirectory(data.Hash);
            }
            else {
                using(IDLWrapper wrapper = new IDLWrapper(data.IDL)) {
                    if(!wrapper.Available) _cancelFailedNavigation(displayPath, back, rollback);
                    else {
                        _host.SaveSelectedItems(current);
                        _host.SetNavigatedByCode(true);
                        _host.NavigateShell(wrapper);
                    }
                }
            }
        }

        internal bool NavigateToIndex(bool back, int index) {
            if(index == 0) return false;
            string[] history = back ? _tabContext.CurrentTab.GetHistoryBack() : _tabContext.CurrentTab.GetHistoryForward();
            if((back && history.Length - 1 < index) || (!back && history.Length < index)) return false;
            NavigateToHistory(back ? history[index] : history[index - 1], back, back ? index : index - 1);
            return true;
        }

        internal void NavigationButton_DropDownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QMenuItem item = e.ClickedItem as QMenuItem;
            if(item == null) return;
            MenuItemArguments args = item.MenuItemArguments;
            if(Control.ModifierKeys == Keys.Shift) {
                _host.CloneTab(_tabContext.CurrentTab, null, true, -1);
                NavigateToHistory(args.Path, args.IsBack, args.Index);
            }
            else if(Control.ModifierKeys == Keys.Control) {
                using(IDLWrapper wrapper = new IDLWrapper(args.Path)) _host.OpenNewWindow(wrapper);
            }
            else NavigateToHistory(args.Path, args.IsBack, args.Index);
        }

        internal void NavigationButtons_Click(object sender, EventArgs e) => NavigateCurrentTab(_host.IsBackNavigationButton(sender));
        internal void NavigationButtons_DropDownOpening(object sender, EventArgs e) => _host.PopulateNavigationHistoryMenu();
    }
}
