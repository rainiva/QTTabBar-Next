//    Dropped files controller extracted from QTTabBarClass (arch-batch3c6s).

using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class DroppedFilesController {
            private readonly QTTabBarClass _owner;

            public DroppedFilesController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void AppendUserApps(IList<string> listDroppedPaths) {
                WindowUtils.BringExplorerToFront(_owner.ExplorerHandle);
                if(_owner.contextMenuDropped == null) {
                    ToolStripMenuItem tsmiDropped = new ToolStripMenuItem { Tag = 1 };
                    _owner.contextMenuDropped = new ContextMenuStripEx(_owner.components, false);
                    _owner.contextMenuDropped.SuspendLayout();
                    _owner.contextMenuDropped.Items.Add(tsmiDropped);
                    _owner.contextMenuDropped.Items.Add(new ToolStripMenuItem());
                    _owner.contextMenuDropped.ItemClicked += (sender, e) => {
                        if(e.ClickedItem.Tag != null)
                            AppsManager.CreateNewApp((List<string>)_owner.contextMenuDropped.Tag);
                    };
                    _owner.contextMenuDropped.ResumeLayout(false);
                }

                string strMenu = QTUtility.ResMain[21];
                strMenu += listDroppedPaths.Count > 1
                        ? listDroppedPaths.Count + QTUtility.ResMain[22]
                        : Path.GetFileName(listDroppedPaths[0]).Enquote();

                _owner.contextMenuDropped.SuspendLayout();
                _owner.contextMenuDropped.Items[0].Text = strMenu;
                _owner.contextMenuDropped.Items[1].Text = QTUtility.ResMain[23];
                _owner.contextMenuDropped.Tag = listDroppedPaths;
                _owner.contextMenuDropped.ResumeLayout();
                _owner.contextMenuDropped.Show(MousePosition);
            }
        }
    }
}
