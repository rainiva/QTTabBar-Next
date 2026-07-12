using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class DroppedFilesController {
        private readonly IFileDropToolsHost _host;

        public DroppedFilesController(IFileDropToolsHost host) {
            _host = host;
        }

        public void AppendUserApps(IList<string> listDroppedPaths) {
            WindowUtils.BringExplorerToFront(_host.ExplorerHandle);
            if(_host.DroppedFilesMenu == null) {
                ToolStripMenuItem tsmiDropped = new ToolStripMenuItem { Tag = 1 };
                _host.DroppedFilesMenu = new ContextMenuStripEx(_host.Components, false);
                _host.DroppedFilesMenu.SuspendLayout();
                _host.DroppedFilesMenu.Items.Add(tsmiDropped);
                _host.DroppedFilesMenu.Items.Add(new ToolStripMenuItem());
                _host.DroppedFilesMenu.ItemClicked += (sender, e) => {
                    if(e.ClickedItem.Tag != null)
                        AppsManager.CreateNewApp((List<string>)_host.DroppedFilesMenu.Tag);
                };
                _host.DroppedFilesMenu.ResumeLayout(false);
            }

            string strMenu = ResourceCache.ResMain[21];
            strMenu += listDroppedPaths.Count > 1
                    ? listDroppedPaths.Count + ResourceCache.ResMain[22]
                    : Path.GetFileName(listDroppedPaths[0]).Enquote();

            _host.DroppedFilesMenu.SuspendLayout();
            _host.DroppedFilesMenu.Items[0].Text = strMenu;
            _host.DroppedFilesMenu.Items[1].Text = ResourceCache.ResMain[23];
            _host.DroppedFilesMenu.Tag = listDroppedPaths;
            _host.DroppedFilesMenu.ResumeLayout();
            _host.DroppedFilesMenu.Show(Cursor.Position);
        }
    }
}
