namespace QTTabBarLib {
    internal sealed class ButtonBarClickController {
        private readonly IButtonBarCommandHost _host;

        public ButtonBarClickController(IButtonBarCommandHost host) { _host = host; }

        public void ProcessButtonBarClick(int buttonID) {
            switch(buttonID) {
                case QTButtonBar.BII_NAVIGATION_BACK:
                    _host.NavigateCurrentTab(true);
                    break;
                case QTButtonBar.BII_NAVIGATION_FWRD:
                    _host.NavigateCurrentTab(true);
                    break;
                case QTButtonBar.BII_NEWWINDOW:
                    _host.OpenNewWindowForCurrentTab();
                    break;
                case QTButtonBar.BII_CLONE:
                    QTLogger.log("QTTabBarLib.QTTabBarClass.CloneCurrentTab 复制标签");
                    _host.CloneCurrentTab();
                    break;
                case QTButtonBar.BII_LOCK:
                    _host.ToggleCurrentTabLock();
                    break;
                case QTButtonBar.BII_TOPMOST:
                    _host.ToggleTopMost();
                    break;
                case QTButtonBar.BII_CLOSE_CURRENT:
                    _host.CloseCurrentTab(Config.Window.CloseBtnClosesSingleTab);
                    break;
                case QTButtonBar.BII_CLOSE_ALLBUTCURRENT:
                    _host.CloseAllTabsExceptCurrent();
                    break;
                case QTButtonBar.BII_CLOSE_WINDOW:
                    _host.CloseExplorerWindow();
                    break;
                case QTButtonBar.BII_CLOSE_LEFT:
                    _host.CloseLeftRight(true);
                    break;
                case QTButtonBar.BII_CLOSE_RIGHT:
                    _host.CloseLeftRight(false);
                    break;
                case QTButtonBar.BII_GOUPONELEVEL:
                    QTLogger.log("QTButtonBar.BII_GOUPONELEVEL UpOneLevel");
                    _host.UpOneLevel();
                    break;
                case QTButtonBar.BII_REFRESH_SHELLBROWSER:
                    _host.RefreshExplorer();
                    break;
                case QTButtonBar.BII_SHELLSEARCH:
                    _host.ShowSearchBar();
                    break;
                case QTButtonBar.BII_OPTION:
                    OptionsDialog.Open();
                    break;
            }
        }
    }
}
