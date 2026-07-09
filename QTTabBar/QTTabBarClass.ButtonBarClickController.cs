//    Button bar click controller extracted from QTTabBarClass (arch-batch3c6o).

using System.Linq;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ButtonBarClickController {
            private readonly QTTabBarClass _owner;

            public ButtonBarClickController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void ProcessButtonBarClick(int buttonID) {
                switch(buttonID) {
                    case QTButtonBar.BII_NAVIGATION_BACK:
                        _owner.NavigateCurrentTab(true);
                        break;

                    case QTButtonBar.BII_NAVIGATION_FWRD:
                        _owner.NavigateCurrentTab(true);
                        break;

                    case QTButtonBar.BII_NEWWINDOW:
                        using(IDLWrapper wrapper4 = new IDLWrapper(_owner.CurrentTab.CurrentIDL)) {
                            _owner.OpenNewWindow(wrapper4);
                        }
                        break;

                    case QTButtonBar.BII_CLONE:
                        QTLogger.log("QTTabBarLib.QTTabBarClass.CloneCurrentTab 复制标签");
                        _owner.CloneCurrentTab();
                        break;

                    case QTButtonBar.BII_LOCK:
                        LockedTabsService.ToggleTab(
                            _owner.CurrentTab,
                            _owner.tabControl1.TabPages.Cast<QTabItem>());
                        break;

                    case QTButtonBar.BII_TOPMOST:
                        _owner.ToggleTopMost();
                        break;

                    case QTButtonBar.BII_CLOSE_CURRENT:
                        if(Config.Window.CloseBtnClosesSingleTab) {
                            _owner.CloseTab(_owner.CurrentTab);
                            return;
                        }
                        _owner.CloseTab(_owner.CurrentTab, false);
                        if(_owner.tabControl1.TabCount == 0) {
                            WindowUtils.CloseExplorer(_owner.ExplorerHandle, 2);
                        }
                        break;

                    case QTButtonBar.BII_CLOSE_ALLBUTCURRENT:
                        if(_owner.tabControl1.TabCount > 1) {
                            _owner.CloseAllTabsExcept(_owner.CurrentTab);
                        }
                        break;

                    case QTButtonBar.BII_CLOSE_WINDOW: {
                            LockedTabsService.PersistFromTabs(_owner.tabControl1.TabPages);
                        }
                        WindowUtils.CloseExplorer(_owner.ExplorerHandle, 1);
                        break;

                    case QTButtonBar.BII_CLOSE_LEFT:
                        _owner.CloseLeftRight(true, -1);
                        break;

                    case QTButtonBar.BII_CLOSE_RIGHT:
                        _owner.CloseLeftRight(false, -1);
                        break;

                    case QTButtonBar.BII_GOUPONELEVEL:
                        QTLogger.log("QTButtonBar.BII_GOUPONELEVEL UpOneLevel");
                        _owner.UpOneLevel();
                        break;

                    case QTButtonBar.BII_REFRESH_SHELLBROWSER:
                        _owner.Explorer.Refresh();
                        break;

                    case QTButtonBar.BII_SHELLSEARCH:
                        _owner.ShowSearchBar(true);
                        break;

                    case QTButtonBar.BII_OPTION:
                        OptionsDialog.Open();
                        break;
                }
            }
        }
    }
}
