//    Plugin tab access extracted from PluginServer (arch-batch5h).

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed partial class PluginServer {
        public bool CreateTab(Address address, int index, bool fLocked, bool fSelect) {
            using(IDLWrapper wrapper = new IDLWrapper(address)) {
                address.ITEMIDLIST = wrapper.IDL;
                address.Path = wrapper.Path;
            }
            if((address.ITEMIDLIST == null) || (address.ITEMIDLIST.Length <= 0)) {
                return false;
            }
            QTabItem tab = new QTabItem(QTUtility2.MakePathDisplayText(address.Path, false), address.Path, _tabHost.tabControl1);
            tab.NavigatedTo(address.Path, address.ITEMIDLIST, -1, false);
            tab.ToolTipText = QTUtility2.MakePathDisplayText(address.Path, true);
            tab.TabLocked = fLocked;
            if(index < 0) {
                _tabHost.AddInsertTab(tab);
            }
            else {
                if(index > _tabHost.tabControl1.TabCount) {
                    index = _tabHost.tabControl1.TabCount;
                }
                _tabHost.tabControl1.TabPages.Insert(index, tab);
            }
            if(fSelect) {
                _tabHost.tabControl1.SelectTab(tab);
            }
            return true;
        }

        public bool CreateWindow(Address address) {
            using(IDLWrapper wrapper = new IDLWrapper(address)) {
                if(wrapper.Available) {
                    _tabHost.OpenNewWindow(wrapper);
                    return true;
                }
            }
            return false;
        }

        public ITab[] GetTabs() {
            return (from QTabItem item in _tabHost.tabControl1.TabPages
                    select (ITab)(new TabWrapper(item, _tabHost))).ToArray();
        }

        public ITab HitTest(Point pnt) {
            QTabItem tabMouseOn = _tabHost.tabControl1.GetTabMouseOn();
            return tabMouseOn != null ? new TabWrapper(tabMouseOn, _tabHost) : null;
        }

        public bool TryGetSelection(out Address[] adSelectedItems) {
            string str;
            return _tabHost.ShellBrowser.TryGetSelection(out adSelectedItems, out str, false);
        }

        public bool TrySetSelection(Address[] itemsToSelect, bool fDeselectOthers) {
            return _tabHost.ShellBrowser.TrySetSelection(itemsToSelect, null, fDeselectOthers);
        }

        public void UpdateItem(IBarButton barItem, bool fEnabled, bool fRefreshImage) {
            string pid = InstanceToFullName(barItem, false);
            if(pid.Length > 0) {
                QTTabBarClass.TryCallButtonBar(bbar => bbar.UpdatePluginItem(pid, barItem, fEnabled, fRefreshImage));
            }
        }

        public ITab SelectedTab {
            get {
                return _tabHost.CurrentTab != null ? new TabWrapper(_tabHost.CurrentTab, _tabHost) : null;
            }
            set {
                TabWrapper wrapper = value as TabWrapper;
                if((wrapper.Tab != null) && _tabHost.tabControl1.TabPages.Contains(wrapper.Tab)) {
                    _tabHost.tabControl1.SelectTab(wrapper.Tab);
                }
            }
        }

        internal sealed class TabWrapper : ITab {
            private QTabItem tab;
            private IPluginServerTabHost _tabHost;

            public TabWrapper(QTabItem tab, IPluginServerTabHost tabHost) {
                this.tab = tab;
                this._tabHost = tabHost;
                this.tab.Closed += tab_Closed;
            }

            public bool Browse(Address address) {
                if(tab != null) {
                    _tabHost.tabControl1.SelectTab(tab);
                    using(IDLWrapper wrapper = new IDLWrapper(address)) {
                        return _tabHost.ShellBrowser.Navigate(wrapper) == 0;
                    }
                }
                return false;
            }

            public bool Browse(bool fBack) {
                if(tab != null) {
                    _tabHost.tabControl1.SelectTab(tab);
                    return _tabHost.NavigateCurrentTab(fBack);
                }
                return false;
            }

            public void Clone(int index, bool fSelect) {
                if(tab != null) {
                    _tabHost.CloneTabButton(tab, null, fSelect, index);
                }
            }

            public bool Close() {
                return (((tab != null) && (_tabHost.tabControl1.TabCount > 1)) && _tabHost.CloseTab(tab, true));
            }

            public Address[] GetBraches() {
                if(tab == null) {
                    return null;
                }
                return (from data in tab.Branches
                        where data.IDL != null || !string.IsNullOrEmpty(data.Path)
                        select new Address(data.IDL, data.Path)).ToArray();
            }

            public Address[] GetHistory(bool fBack) {
                if(tab == null) {
                    return null;
                }
                IEnumerable<LogData> logs = tab.GetLogs(fBack);
                return logs.Select(data => new Address(data.IDL, data.Path)).ToArray();
            }

            public bool Insert(int index) {
                if(((tab != null) && (-1 < index)) && (index < (_tabHost.tabControl1.TabCount + 1))) {
                    int indexSource = _tabHost.tabControl1.TabPages.IndexOf(tab);
                    if(indexSource > -1) {
                        _tabHost.tabControl1.TabPages.Relocate(indexSource, index);
                        return true;
                    }
                }
                return false;
            }

            private void tab_Closed(object sender, EventArgs e) {
                tab.Closed -= tab_Closed;
                tab = null;
                _tabHost = null;
            }

            public Address Address {
                get {
                    if(tab == null) {
                        return new Address();
                    }
                    Address address = new Address(tab.CurrentIDL, tab.CurrentPath);
                    if((address.ITEMIDLIST == null) && !string.IsNullOrEmpty(address.Path)) {
                        IDLWrapper wrapper;
                        IntPtr pidl = PInvoke.ILCreateFromPath(address.Path);
                        if(pidl != IntPtr.Zero) {
                            address = new Address(pidl, tab.CurrentPath);
                            PInvoke.CoTaskMemFree(pidl);
                            return address;
                        }
                        if(!IDLWrapper.TryGetCache(address.Path, out wrapper)) {
                            return address;
                        }
                        using(wrapper) {
                            address.ITEMIDLIST = wrapper.IDL;
                        }
                    }
                    return address;
                }
            }

            public int Index {
                get {
                    if(tab != null) {
                        return _tabHost.tabControl1.TabPages.IndexOf(tab);
                    }
                    return -1;
                }
            }

            public bool Locked {
                get {
                    return ((tab != null) && tab.TabLocked);
                }
                set {
                    if(tab != null) {
                        tab.TabLocked = value;
                        _tabHost.tabControl1.Refresh();
                    }
                }
            }

            public bool Selected {
                get {
                    return ((tab != null) && (_tabHost.CurrentTab == tab));
                }
                set {
                    if((tab != null) && value) {
                        _tabHost.tabControl1.SelectTab(tab);
                    }
                }
            }

            public string SubText {
                get {
                    if(tab != null) {
                        return tab.Comment;
                    }
                    return string.Empty;
                }
                set {
                    if((tab != null) && (value != null)) {
                        tab.Comment = value;
                        tab.RefreshRectangle();
                        _tabHost.tabControl1.Refresh();
                    }
                }
            }

            public QTabItem Tab {
                get {
                    return tab;
                }
            }

            public string Text {
                get {
                    return tab != null ? tab.Text : string.Empty;
                }
                set {
                    if(tab != null && !string.IsNullOrEmpty(value)) {
                        tab.Text = value;
                    }
                }
            }
        }
    }
}
