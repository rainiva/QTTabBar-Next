using System;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerNavigationHost {
        QTabItem IExplorerNavigationHost.GetCurrentTab() => CurrentTab;
        void IExplorerNavigationHost.SelectTab(QTabItem tab) => tabControl1.SelectTab(tab);
        void IExplorerNavigationHost.ShowNavigationCanceled(string path) => ShowMessageNavCanceled(path, false);
        void IExplorerNavigationHost.OpenNewWindow(IDLWrapper target) => OpenNewWindow(target);
        void IExplorerNavigationHost.CloneTab(QTabItem tab, LogData log) => CloneTabButton(tab, log);
        void IExplorerNavigationHost.CloneTab(QTabItem tab, string path, bool select, int index) => CloneTabButton(tab, path, select, index);
        bool IExplorerNavigationHost.IsSpecialTravelPath(string path) => IsSpecialFolderNeedsToTravel(path);
        void IExplorerNavigationHost.SaveSelectedItems(QTabItem tab) => SaveSelectedItems(tab);
        void IExplorerNavigationHost.SetNavigatedByCode(bool value) => NavigatedByCode = value;
        bool IExplorerNavigationHost.NavigateToPastSpecialDirectory(int hash) => NavigateToPastSpecialDir(hash);
        bool IExplorerNavigationHost.NavigateShell(IDLWrapper target) => ShellBrowser.Navigate(target) == 0;
        QTabItem IExplorerNavigationHost.CreateAndInsertClone(QTabItem tab) {
            NowTabCloned = true;
            QTabItem clone = tab.Clone();
            AddInsertTab(clone);
            return clone;
        }
        bool IExplorerNavigationHost.HasPastSpecialEntry(int hash) => LogEntryDic.ContainsKey(hash);

        void IExplorerNavigationHost.PopulateNavigationHistoryMenu() {
            buttonNavHistoryMenu.DropDown.SuspendLayout();
            while(buttonNavHistoryMenu.DropDownItems.Count > 0) buttonNavHistoryMenu.DropDownItems[0].Dispose();
            if(CurrentTab.HistoryCount_Back + CurrentTab.HistoryCount_Forward > 1) {
                buttonNavHistoryMenu.DropDownItems.AddRange(CreateNavBtnMenuItems(true).ToArray());
                buttonNavHistoryMenu.DropDownItems.AddRange(CreateBranchMenu(true, components, tsmiBranchRoot_DropDownItemClicked).ToArray());
            }
            else {
                ToolStripMenuItem item = new ToolStripMenuItem("none") { Enabled = false };
                buttonNavHistoryMenu.DropDownItems.Add(item);
            }
            buttonNavHistoryMenu.DropDown.ResumeLayout();
        }

        bool IExplorerNavigationHost.IsBackNavigationButton(object sender) => sender == buttonBack;
    }
}
