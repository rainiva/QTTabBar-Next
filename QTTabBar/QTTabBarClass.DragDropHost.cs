using System.Collections.Generic;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IDragDropHost {
        QTabControl IDragDropHost.TabControl { get { return tabControl1; } }
        QTabItem IDragDropHost.CurrentDragDropTab { get { return tabForDD; } }
        bool IDragDropHost.ToggleTabMenu { set { fToggleTabMenu = value; } }
        void IDragDropHost.HideDragDropToolTip() { HideToolTipForDD(); }
        void IDragDropHost.HideTabSubDirTipMenu() { HideSubDirTip_Tab_Menu(); }
        void IDragDropHost.ShowDragDropToolTip(QTabItem tab, int state, int keyState) {
            ShowToolTipForDD(tab, state, keyState);
        }
        void IDragDropHost.OpenDroppedFolder(IList<string> droppedPaths) { OpenDroppedFolder(droppedPaths); }
    }
}
