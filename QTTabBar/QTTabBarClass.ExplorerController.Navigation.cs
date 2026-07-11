using System;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal partial class ExplorerController {
        internal void NavigateBranchCurrent(int index) => NavigationController.NavigateBranchCurrent(index);
        public void NavigateBranches(QTabItem tab, int index) => NavigationController.NavigateBranches(tab, index);
        public bool NavigateCurrentTab(bool back) => NavigationController.NavigateCurrentTab(back);
        public void NavigateToFirstOrLast(bool back) => NavigationController.NavigateToFirstOrLast(back);
        internal void NavigateToHistory(string path, bool back, int steps) => NavigationController.NavigateToHistory(path, back, steps);
        public bool NavigateToIndex(bool back, int index) => NavigationController.NavigateToIndex(back, index);
        public void NavigationButton_DropDownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e) => NavigationController.NavigationButton_DropDownMenu_ItemClicked(sender, e);
        public void NavigationButtons_Click(object sender, EventArgs e) => NavigationController.NavigationButtons_Click(sender, e);
        public void NavigationButtons_DropDownOpening(object sender, EventArgs e) => NavigationController.NavigationButtons_DropDownOpening(sender, e);
    }
}
