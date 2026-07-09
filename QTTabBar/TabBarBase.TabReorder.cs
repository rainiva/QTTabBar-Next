using System.Windows.Forms;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal void ReorderTab(int index, bool fDescending) {
            tabControl1.SetRedraw(false);
            try {
                if(index == 3) {
                    if(tabControl1.TabCount > 1) {
                        int indexSource = 0;
                        for(int i = tabControl1.TabCount - 1; indexSource < i; i--) {
                            tabControl1.TabPages.Relocate(indexSource, i);
                            tabControl1.TabPages.Relocate(i - 1, indexSource);
                            indexSource++;
                        }
                    }
                }
                else {
                    int num3 = fDescending ? -1 : 1;
                    for(int j = 0; j < (tabControl1.TabCount - 1); j++) {
                        for(int k = tabControl1.TabCount - 1; k > j; k--) {
                            string strA;
                            string strB;
                            if(index == 0) {
                                strA = tabControl1.TabPages[j].Text;
                                strB = tabControl1.TabPages[k].Text;
                            }
                            else if(index == 1) {
                                strA = tabControl1.TabPages[j].CurrentPath;
                                strB = tabControl1.TabPages[k].CurrentPath;
                            }
                            else {
                                int num6 = lstActivatedTabs.IndexOf(tabControl1.TabPages[j]);
                                int num7 = lstActivatedTabs.IndexOf(tabControl1.TabPages[k]);
                                if(((num6 - num7) * num3) < 0) {
                                    tabControl1.TabPages.Relocate(j, k);
                                }
                                continue;
                            }
                            if((string.Compare(strA, strB) * num3) > 0) {
                                tabControl1.TabPages.Relocate(j, k);
                            }
                        }
                    }
                }
            }
            finally {
                tabControl1.SetRedraw(true);
            }
            TryCallButtonBar(bbar => bbar.RefreshButtons());
        }
    }
}
