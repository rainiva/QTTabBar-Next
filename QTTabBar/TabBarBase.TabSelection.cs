using System;
using System.Collections.Generic;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        public void tabControl1_SelectedIndexChanged(object sender, EventArgs e) {
            QTLogger.log("tabControl1_SelectedIndexChanged");
            QTabItem selectedTab = tabControl1.SelectedTab;
            string currentPath = selectedTab.CurrentPath;
            if(IsSpecialFolderNeedsToTravel(currentPath) &&
               LogEntryDic.ContainsKey(selectedTab.GetLogHash(true, 0))) {
                NavigatedByCode = true;
                CurrentTab = selectedTab;
                UpdateActivatedTabs();
                fNavigatedByTabSelection = NavigateToPastSpecialDir(CurrentTab.GetLogHash(true, 0));
                NotifyTabChanged(selectedTab);
                FocusListViewIfTabControlFocused();
            }
            else {
                IDLWrapper idlw = null;
                if((selectedTab.CurrentIDL != null) && (selectedTab.CurrentIDL.Length > 0)) {
                    idlw = new IDLWrapper(selectedTab.CurrentIDL);
                }
                if((idlw == null) || !idlw.Available) {
                    idlw = new IDLWrapper(selectedTab.CurrentPath);
                }
                using(idlw) {
                    if(!idlw.Available) {
                        CancelFailedTabChanging(currentPath);
                        return;
                    }
                    CurrentTab = selectedTab;
                    UpdateActivatedTabs();
                    if(((currentPath != CurrentAddress) ||
                        (OSDetector.IsXP && (currentPath == OSDetector.PATH_SEARCHFOLDER))) ||
                       NowTabCloned) {
                        NavigatedByCode = true;
                        fNavigatedByTabSelection = true;
                        NowTabCloned = false;
                        if(!TryNavigateOnTabSelect(idlw, currentPath)) {
                            CancelFailedTabChanging(currentPath);
                            return;
                        }
                    }
                    else {
                        SyncTravelState();
                    }
                }
                FocusListViewIfTabControlFocused();
                NotifyTabChanged(CurrentTab);
            }
        }

        protected virtual bool TryNavigateOnTabSelect(IDLWrapper idlw, string currentPath) {
            return ShellBrowser != null && ShellBrowser.Navigate(idlw) == 0;
        }

        private void UpdateActivatedTabs() {
            while(lstActivatedTabs.Remove(CurrentTab)) {
            }
            lstActivatedTabs.Add(CurrentTab);
            if(lstActivatedTabs.Count > 15) {
                lstActivatedTabs.RemoveAt(0);
            }
        }

        private void FocusListViewIfTabControlFocused() {
            if(tabControl1 == null || listView == null) {
                return;
            }
            try {
                if(tabControl1.Focused) {
                    listView.SetFocus();
                }
            }
            catch(NullReferenceException) {
            }
        }

        private void NotifyTabChanged(QTabItem tab) {
            if(pluginServer != null) {
                pluginServer.OnTabChanged(tabControl1.SelectedIndex, tab.CurrentIDL, tab.CurrentPath);
            }
        }
    }
}
