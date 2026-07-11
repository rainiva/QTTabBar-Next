using SHDocVw;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerWindowMessageHost {
        int IExplorerWindowMessageHost.SequentialCloseCount { get => iSequential_WM_CLOSE; set => iSequential_WM_CLOSE = value; }
        WebBrowser IExplorerWindowMessageHost.Explorer => Explorer;
        bool IExplorerWindowMessageHost.NeedsNewWindowPulse { get => fNeedsNewWindowPulse; set => fNeedsNewWindowPulse = value; }

        int IExplorerWindowMessageHost.GetMessageCode(ExplorerMessageKind kind) {
            switch(kind) {
                case ExplorerMessageKind.BrowseObject: return WM_BROWSEOBJECT;
                case ExplorerMessageKind.HeaderInAllViews: return WM_HEADERINALLVIEWS;
                case ExplorerMessageKind.ShowHideBars: return WM_SHOWHIDEBARS;
                case ExplorerMessageKind.CheckPulse: return WM_CHECKPULSE;
                case ExplorerMessageKind.SelectFile: return WM_SELECTFILE;
                default: return 0;
            }
        }

        bool IExplorerWindowMessageHost.TryCloseCurrentTab() {
            return CloseTab(CurrentTab, true) && tabControl1.TabCount == 0;
        }

        void IExplorerWindowMessageHost.CloseExplorer(int reason) {
            WindowUtils.CloseExplorer(ExplorerHandle, reason);
        }
    }
}
