using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class DragDropController {
        private readonly IDragDropHost _host;
        private bool _drivesContained;
        private string _draggingDrive;
        private string _draggingStartPath;

        public DragDropController(IDragDropHost host) {
            _host = host;
        }

        public int DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
            _host.HideDragDropToolTip();
            hwnd = _host.TabControl.Handle;
            idlReal = null;
            QTabItem tabMouseOn = _host.TabControl.GetTabMouseOn();
            if(tabMouseOn == null || !Config.Tabs.DragOverTabOpensSDT) return 1;
            if(tabMouseOn.CurrentIDL != null && tabMouseOn.CurrentIDL.Length > 0) {
                idlReal = tabMouseOn.CurrentIDL;
                return 0;
            }
            return -1;
        }

        public DragDropEffects DragFileEnter(IntPtr hDrop, Point point, int keyState) {
            if(!Config.Tabs.DragOverTabOpensSDT) return DragDropEffects.Copy;

            int result = HandleDragEnter(hDrop, out _draggingDrive, out _draggingStartPath);
            _drivesContained = result == 2;
            if(result == -1) return DragDropEffects.None;
            if(_host.TabControl.GetTabMouseOn() == null) return DragDropEffects.Copy;
            switch(result) {
                case 0: return DropTargetWrapper.MakeEffect(keyState, 0);
                case 1: return DropTargetWrapper.MakeEffect(keyState, 1);
                default: return DragDropEffects.None;
            }
        }

        public void DragFileLeave(object sender, EventArgs e) {
            _host.HideDragDropToolTip();
            _draggingDrive = null;
            _draggingStartPath = null;
            _host.TabControl.Refresh();
        }

        public void DragFileOver(object sender, DragEventArgs e) {
            QTLogger.log("QTTabBarClass dropTargetWrapper_DragFileOver");
            e.Effect = DragDropEffects.None;
            QTabItem mouseOnTab = _host.TabControl.GetTabMouseOn();
            bool sameTab = mouseOnTab == _host.CurrentDragDropTab;
            if(!sameTab) {
                _host.TabControl.Refresh();
                _host.HideTabSubDirTipMenu();
                _host.ToggleTabMenu = false;
            }
            if(mouseOnTab == null) {
                e.Effect = DragDropEffects.Copy;
            }
            else if(mouseOnTab.CurrentPath.Length > 2) {
                if(_drivesContained || _draggingStartPath.PathEquals(mouseOnTab.CurrentPath)) {
                    _host.HideDragDropToolTip();
                    _host.ShowDragDropToolTip(mouseOnTab, -1, e.KeyState);
                }
                else {
                    using(IDLWrapper wrapper = new IDLWrapper(mouseOnTab.CurrentIDL, !sameTab)) {
                        if(wrapper.Available && wrapper.IsDropTarget) {
                            string root = mouseOnTab.CurrentPath.Substring(0, 3);
                            int state = _draggingDrive != null && _draggingDrive.Equals(root, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                            _host.ShowDragDropToolTip(mouseOnTab, state, e.KeyState);
                            e.Effect = Config.Tabs.DragOverTabOpensSDT ? DropTargetWrapper.MakeEffect(e.KeyState, state) : DragDropEffects.Copy;
                        }
                        else {
                            _host.HideDragDropToolTip();
                        }
                    }
                }
            }
            else {
                _host.HideDragDropToolTip();
            }
        }

        public void HandleFileDrop(IntPtr hDrop) {
            _host.HideDragDropToolTip();
            int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
            if(capacity < 1) return;

            var paths = new List<string>(capacity);
            for(int i = 0; i < capacity; i++) {
                var fileName = new StringBuilder(260);
                PInvoke.DragQueryFile(hDrop, (uint)i, fileName, fileName.Capacity);
                paths.Add(fileName.ToString());
            }
            _host.OpenDroppedFolder(paths);
        }

        internal static int HandleDragEnter(IntPtr hDrop, out string draggingDrive, out string draggingStartPath) {
            QTLogger.log("QTTabBarClass HandleDragEnter IsFolder hDrop " + hDrop +
                " out string strDraggingDrive, out string strDraggingStartPath");
            draggingDrive = draggingStartPath = null;
            int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
            if(capacity < 1) return -1;

            var paths = new List<string>(capacity);
            for(int i = 0; i < capacity; i++) {
                var fileName = new StringBuilder(260);
                PInvoke.DragQueryFile(hDrop, (uint)i, fileName, fileName.Capacity);
                if(fileName.Length > 0) paths.Add(fileName.ToString());
            }
            if(paths.Count == 0) return -1;
            if(paths[0].Length < 4) return 2;

            bool sameRoot = true;
            string root = QTUtility2.MakeRootName(paths[0]);
            foreach(string path in paths) {
                if(!File.Exists(path) && !Directory.Exists(path)) return -1;
                if(path.Length <= 3) return 2;
                if(!QTUtility2.MakeRootName(path).PathEquals(root)) sameRoot = false;
            }
            if(!sameRoot) return 1;
            draggingDrive = root;
            draggingStartPath = Path.GetDirectoryName(paths[0]);
            return 0;
        }
    }
}
