using System.Collections.Generic;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class TabManager {
        private readonly ITabOperationsHost _host;
        public TabManager(ITabOperationsHost host) { _host = host; }
        public void AddStartUpTabs(string group, string path) => _host.AddStartUpTabs(group, path);
        public void ChooseNewDirectory() => _host.ChooseNewDirectory();
        internal void OpenNewTabOrWindow(IDLWrapper target, bool needsPulse = false) => _host.OpenNewTabOrWindow(target, needsPulse);
        internal void OpenNewWindow(IDLWrapper target) => _host.OpenNewWindow(target);
        public void OpenGroup(string group, bool forceNewWindow, bool disableOverrides = false) => _host.OpenGroup(group, forceNewWindow, disableOverrides);
        public void OpenDroppedFolder(IList<string> paths) => _host.OpenDroppedFolder(paths);
        internal void CloneCurrentTab(bool select = true) => _host.CloneCurrentTab(select);
        public void CloneTabButton(QTabItem tab, LogData log) => _host.CloneTabButton(tab, log);
        public QTabItem CloneTabButton(QTabItem tab, string url, bool select, int index) => _host.CloneTabButton(tab, url, select, index);
        public void CloseLeftRight(bool left, int index) => _host.CloseLeftRight(left, index);
        internal void ReplaceByGroup(string group) => _host.ReplaceByGroup(group);
    }
}
