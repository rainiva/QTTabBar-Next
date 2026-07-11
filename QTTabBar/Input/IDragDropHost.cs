using System.Collections.Generic;

namespace QTTabBarLib {
    internal interface IDragDropHost {
        QTabControl TabControl { get; }
        QTabItem CurrentDragDropTab { get; }
        bool ToggleTabMenu { set; }
        void HideDragDropToolTip();
        void HideTabSubDirTipMenu();
        void ShowDragDropToolTip(QTabItem tab, int state, int keyState);
        void OpenDroppedFolder(IList<string> droppedPaths);
    }
}
