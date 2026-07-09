namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal QTabItem CloneTabButtonCore(QTabItem tab, string optionURL, bool fSelect, int index) {
            QTLogger.log("TabBarBase.CloneTabButtonCore optionURL " + optionURL +
                           " fSelect " + fSelect +
                           " index " + index);
            NowTabCloned = fSelect;
            QTabItem item = tab.Clone();
            if(index < 0) {
                AddInsertTab(item);
            }
            else if((-1 < index) && (index < (tabControl1.TabCount + 1))) {
                tabControl1.TabPages.Insert(index, item);
            }
            else {
                AddInsertTab(item);
            }
            if(optionURL != null) {
                using(IDLWrapper wrapper = new IDLWrapper(optionURL)) {
                    item.NavigatedTo(optionURL, wrapper.IDL, -1, false);
                }
            }
            if(fSelect) {
                tabControl1.SelectTab(item);
            }
            else {
                item.RefreshRectangle();
                tabControl1.Refresh();
            }
            return item;
        }
    }
}
