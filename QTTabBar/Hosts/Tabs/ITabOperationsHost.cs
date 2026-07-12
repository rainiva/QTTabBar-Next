using System.Collections.Generic;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface ITabOperationsHost {
        void AddStartUpTabs(string openingGroup, string openingPath);
        void ChooseNewDirectory();
        void OpenNewTabOrWindow(IDLWrapper target, bool needsPulse);
        void OpenNewWindow(IDLWrapper target);
        void OpenGroup(string groupName, bool forceNewWindow, bool disableOverrides);
        void OpenDroppedFolder(IList<string> paths);
        void CloneCurrentTab(bool select);
        void CloneTabButton(QTabItem tab, LogData log);
        QTabItem CloneTabButton(QTabItem tab, string optionUrl, bool select, int index);
        void CloseLeftRight(bool left, int index);
        void ReplaceByGroup(string groupName);
    }
}
