//    Tab selection guard handlers shared by QTTabBarClass and QTSecondViewBar (arch-batch3w3b).

using QTPlugin;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        public void tabControl1_Deselecting(object sender, QTabCancelEventArgs e) {
            if(e.TabPageIndex != -1) {
                SaveSelectedItems(e.TabPage);
            }
        }

        protected void SaveSelectedItems(QTabItem tab) {
            Address[] addressArray;
            string str;
            if(((tab != null) && !string.IsNullOrEmpty(CurrentAddress)) &&
                ShellBrowser.TryGetSelection(out addressArray, out str, false, ShellBrowser)) {
                if(addressArray != null && addressArray.Length > 0) {
                    QTUtility2.log("SaveSelectedItems addressArray " + addressArray[0].Path);
                }
                tab.SetSelectedItemsAt(CurrentAddress, addressArray, str);
            }
        }

        public void tabControl1_Selecting(object sender, QTabCancelEventArgs e) {
            if(NowTabsAddingRemoving) {
                QTUtility2.log("tabControl1_Selecting");
                e.Cancel = true;
            }
        }
    }
}
