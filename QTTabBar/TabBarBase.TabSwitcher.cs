using System.Collections.Generic;
using System.Windows.Forms;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal TabSwitchForm tabSwitcher;

        public bool ShowTabSwitcher(bool fShift, bool fRepeat) {
            if(listView != null) {
                listView.HideSubDirTip();
                listView.HideThumbnailTooltip();
            }
            if(tabControl1.TabCount < 2) {
                return false;
            }
            if(tabSwitcher == null) {
                tabSwitcher = new TabSwitchForm();
                tabSwitcher.Switched += tabSwitcher_Switched;
            }
            if(!tabSwitcher.IsShown) {
                List<PathData> lstPaths = new List<PathData>();
                string str = Config.Tabs.RenameAmbTabs ? " @ " : " : ";
                foreach(QTabItem item in tabControl1.TabPages) {
                    string strDisplay = item.Text;
                    if(!string.IsNullOrEmpty(item.Comment)) {
                        strDisplay += str + item.Comment;
                    }
                    lstPaths.Add(new PathData(strDisplay, item.CurrentPath, item.ImageKey));
                }
                tabSwitcher.ShowSwitcher(ExplorerHandle, tabControl1.SelectedIndex, lstPaths);
            }
            int index = tabSwitcher.Switch(fShift);
            if(!fRepeat || tabControl1.TabCount < 13) {
                tabControl1.SetPseudoHotIndex(index);
            }
            return true;
        }

        public void HideTabSwitcher(bool fSwitch) {
            if((tabSwitcher != null) && tabSwitcher.IsShown) {
                tabSwitcher.HideSwitcher(fSwitch);
                tabControl1.SetPseudoHotIndex(-1);
            }
        }

        public void tabSwitcher_Switched(object sender, ItemCheckEventArgs e) {
            tabControl1.SelectTab(e.Index);
        }
    }
}
