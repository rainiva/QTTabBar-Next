using System.Collections.Generic;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : ITabOperationsHost {
        private TabOperations _tabOperations;
        private TabOperations TabOperationHandler => _tabOperations ?? (_tabOperations = new TabOperations(this));
        void ITabOperationsHost.AddStartUpTabs(string group, string path) => TabOperationHandler.AddStartUpTabs(group, path);
        void ITabOperationsHost.ChooseNewDirectory() => TabOperationHandler.ChooseNewDirectory();
        void ITabOperationsHost.OpenNewTabOrWindow(IDLWrapper target, bool pulse) => TabOperationHandler.OpenNewTabOrWindow(target, pulse);
        void ITabOperationsHost.OpenNewWindow(IDLWrapper target) => TabOperationHandler.OpenNewWindow(target);
        void ITabOperationsHost.OpenGroup(string group, bool force, bool disable) => TabOperationHandler.OpenGroup(group, force, disable);
        void ITabOperationsHost.OpenDroppedFolder(IList<string> paths) => TabOperationHandler.OpenDroppedFolder(paths);
        void ITabOperationsHost.CloneCurrentTab(bool select) => TabOperationHandler.CloneCurrentTab(select);
        void ITabOperationsHost.CloneTabButton(QTabItem tab, LogData log) => TabOperationHandler.CloneTabButton(tab, log);
        QTabItem ITabOperationsHost.CloneTabButton(QTabItem tab, string url, bool select, int index) => TabOperationHandler.CloneTabButton(tab, url, select, index);
        void ITabOperationsHost.CloseLeftRight(bool left, int index) => TabOperationHandler.CloseLeftRight(left, index);
        void ITabOperationsHost.ReplaceByGroup(string group) => TabOperationHandler.ReplaceByGroup(group);
    }
}
