using System.IO;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        public void tabControl1_ItemDrag(object sender, ItemDragEventArgs e) {
            QTabItem item = (QTabItem)e.Item;
            string currentPath = item.CurrentPath;
            if(Directory.Exists(currentPath)) {
                ShellMethods.DoDragDrop(currentPath, this);
            }
        }
    }
}
