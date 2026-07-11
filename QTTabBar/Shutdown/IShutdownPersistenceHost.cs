using System.Collections.Generic;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IShutdownPersistenceHost {
        bool IsShown { get; }
        void UninstallHooks();
        void AddToHistory(QTabItem item);
        ITravelLogStg TravelLog { get; set; }
        ShellContextMenu ShellContextMenu { get; set; }
        ShellBrowserEx ShellBrowser { get; set; }
        Dictionary<int, ITravelLogEntry> LogEntryDic { get; }
        void SetFinalRelease();
        void CloseDWBase(uint dwReserved);
    }
}
