using System;
using System.Drawing;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IBindActionHost, IBindActionUiHost {

        // --- IBindActionHost ---

        bool IBindActionHost.TryDoBindActionCore(BindAction action, bool fRepeat, QTabItem tab, IDLWrapper item) {
            return TryDoBindActionCore(action, fRepeat, tab, item);
        }

        QTabItem IBindActionHost.CurrentTab {
            get { return CurrentTab; }
        }

        QTabItem IBindActionHost.ContextMenuedTab {
            get { return ContextMenuedTab; }
            set { ContextMenuedTab = value; }
        }

        QTabControl IBindActionHost.TabControl {
            get { return tabControl1; }
        }

        void IBindActionHost.NavigateCurrentTab(bool back) {
            NavigateCurrentTab(back);
        }

        void IBindActionHost.NavigateToFirstOrLast(bool first) {
            NavigateToFirstOrLast(first);
        }

        void IBindActionHost.RestoreLastClosed() {
            RestoreLastClosed();
        }

        void IBindActionHost.OpenNewWindow(IDLWrapper idl) {
            OpenNewWindow(idl);
        }

        void IBindActionHost.CloseTab(QTabItem tab) {
            CloseTab(tab);
        }

        void IBindActionHost.OpenNewTab(IDLWrapper idl, bool fBlockSelect) {
            OpenNewTab(idl, fBlockSelect);
        }

        void IBindActionHost.OpenNewTab(string path, bool fBlockSelect) {
            OpenNewTab(path, fBlockSelect);
        }

        void IBindActionHost.UpOneLevel() {
            UpOneLevel();
        }

        ShellBrowserEx IBindActionHost.ShellBrowser {
            get { return ShellBrowser; }
        }

        void IBindActionHost.OpenCmd(QTabItem tab) {
            OpenCmd(tab);
        }

        // --- IBindActionUiHost ---

        void IBindActionUiHost.ChooseNewDirectory() {
            ChooseNewDirectory();
        }

        void IBindActionUiHost.CreateGroup(QTabItem tab) {
            _menuController.CreateGroup(tab);
        }

        ContextMenuStripEx IBindActionUiHost.ContextMenuSys {
            get { return contextMenuSys; }
        }

        Point IBindActionUiHost.PointToScreen(Point point) {
            return PointToScreen(point);
        }

        ContextMenuStripEx IBindActionUiHost.ContextMenuTab {
            get { return contextMenuTab; }
        }

        AbstractListView IBindActionUiHost.ListView {
            get { return listView; }
        }

        SubDirTipForm IBindActionUiHost.SubDirTipTab {
            get { return subDirTip_Tab; }
        }

        void IBindActionUiHost.DoFileTools(int index) {
            DoFileTools(index);
        }

        void IBindActionUiHost.ToggleTopMost() {
            ToggleTopMost();
        }

        IntPtr IBindActionUiHost.ExplorerHandle {
            get { return ExplorerHandle; }
        }

        IntPtr IBindActionUiHost.GetSearchBandEdit() {
            return GetSearchBand_Edit();
        }

        void IBindActionUiHost.MinimizeToTray() {
            MinimizeToTray();
        }

        void IBindActionUiHost.CreateNewFile() {
            createNewFile();
        }

        void IBindActionUiHost.MergeAllWindows() {
            MergeAllWindows();
        }
    }
}
