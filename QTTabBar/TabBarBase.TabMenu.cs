using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        protected ToolStripTextBox menuTextBoxTabAlias;

        internal int TabIndexForNewTab() {
            return TabInsertionPolicy.Resolve(Config.Tabs.NewTabPosition, tabControl1.TabPages.Count, tabControl1.SelectedIndex);
        }

        internal void menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if(e.ClickedItem.Name == "Name") {
                ReorderTab(0, false);
            }
            else if(e.ClickedItem.Name == "Drive") {
                ReorderTab(1, false);
            }
            else if(e.ClickedItem.Name == "Active") {
                ReorderTab(2, false);
            }
            else if(e.ClickedItem.Name == "Rev") {
                ReorderTab(3, false);
            }
        }

        internal void menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
            if(Control.ModifierKeys != Keys.Control) {
                OpenNewTab(clickedItem.Path);
            }
            else {
                using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                    OnOpenNewWindowFromMenu(wrapper);
                }
            }
        }

        protected virtual void OnOpenNewWindowFromMenu(IDLWrapper idlw) {
        }

        internal void menuTextBoxTabAlias_GotFocus(object sender, EventArgs e) {
            menuTextBoxTabAlias.ForeColor = SystemColors.WindowText;
            if(menuTextBoxTabAlias.TextBox.ImeMode != ImeMode.On) {
                menuTextBoxTabAlias.TextBox.ImeMode = ImeMode.On;
            }
            if(menuTextBoxTabAlias.Text == ResourceCache.ResMain[0x1b]) {
                menuTextBoxTabAlias.Text = string.Empty;
            }
        }

        internal void menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e) {
            if(e.KeyChar == '\r') {
                e.Handled = true;
                contextMenuTab.Close(ToolStripDropDownCloseReason.ItemClicked);
            }
        }

        internal void menuTextBoxTabAlias_LostFocus(object sender, EventArgs e) {
            string text = menuTextBoxTabAlias.Text;
            if(text.Length == 0) {
                menuTextBoxTabAlias.Text = ResourceCache.ResMain[0x1b];
            }
            if((text != ResourceCache.ResMain[0x1b]) && (ContextMenuedTab != null)) {
                ContextMenuedTab.Comment = text;
                ContextMenuedTab.RefreshRectangle();
                tabControl1.Refresh();
            }
            menuTextBoxTabAlias.TextBox.SelectionStart = 0;
        }

        internal void contextMenuTab_Closed(object sender, ToolStripDropDownClosedEventArgs e) {
            tabControl1.SetContextMenuState(false);
            if(ContextMenuedTab != CurrentTab) {
                tabControl1.Refresh();
            }
        }

        internal void Add2Group(QTabItem contextMenuedTab) {
            NowModalDialogShown = true;
            if(contextMenuedTab != null) {
                string groupName = contextMenuedTab.Text;
                string currentPath = contextMenuedTab.CurrentPath;
                Group g = GroupsManager.GetGroup(groupName);
                if(g == null) return;
                if(!g.Paths.Any(p => p.PathEquals(currentPath))) {
                    g.Paths.Add(currentPath);
                    GroupsManager.SaveGroups();
                }
            }
            NowModalDialogShown = false;
        }

        internal void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QTLogger.log("QTTabBarClass tsmiBranchRoot_DropDownItemClicked");
            QTabItem tag = (QTabItem)((ToolStripMenuItem)sender).Tag;
            if(tag != null) {
                OnNavigateBranches(tag, ((QMenuItem)e.ClickedItem).MenuItemArguments.Index);
            }
        }

        protected virtual void OnNavigateBranches(QTabItem tab, int index) {
        }
    }
}
