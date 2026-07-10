//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Reflection;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal partial class DropDownMenuReorderable {
        // Batch8 GC8b: scroll-button mechanics delegated to this controller.
        // ScrollMenu(bool,int) remains a protected facade on the menu because the
        // DropDownMenuDropTarget subclass calls it.
        private readonly ScrollController _scrollController;

        private sealed class ScrollController {
            private readonly DropDownMenuReorderable _owner;

            internal ScrollController(DropDownMenuReorderable owner) {
                _owner = owner;
            }

            internal void GetScrollButtons() {
                try {
                    if(piScrollButtonUp == null) {
                        piScrollButtonUp = typeof(ToolStripDropDownMenu).GetProperty("UpScrollButton", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    if(piScrollButtonDn == null) {
                        piScrollButtonDn = typeof(ToolStripDropDownMenu).GetProperty("DownScrollButton", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Instance);
                    }
                    _owner.upButton = (ToolStripControlHost)piScrollButtonUp.GetValue(_owner, null);
                    _owner.downButton = (ToolStripControlHost)piScrollButtonDn.GetValue(_owner, null);
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception, "DropDownMeanuDropTarget GetScrollButtons");
                }
            }

            internal void ScrollMenu(bool fUp, int count) {
                _owner.fSuppressMouseMove_Scroll = true;
                _owner.fSuppressMouseMoveOnce = true;
                _owner.HideToolTip();
                _owner.SuspendLayout();
                int num = ScrollMenuCore(fUp, count);
                if((num < count) && _owner.fVirtualMode) {
                    _owner._virtualController.ScrollMenuVirtual(fUp, count - num);
                }
                _owner.ResumeLayout();
                _owner.Refresh();
                _owner.fSuppressMouseMove_Scroll = false;
                _owner.RaiseMouseScroll();
            }

            internal int ScrollMenuCore(bool fUp, int count) {
                if(count >= 1) {
                    ToolStripControlHost host = fUp ? _owner.upButton : _owner.downButton;
                    if((host != null) && host.Visible) {
                        Control control = host.Control;
                        if((control != null) && control.Enabled) {
                            _owner.CloseChildDropDown();
                            if(miScroll == null) {
                                miScroll = typeof(ToolStripDropDownMenu).GetMethod("ScrollInternal", BindingFlags.ExactBinding | BindingFlags.NonPublic | BindingFlags.Instance, null, new Type[] { typeof(bool) }, null);
                            }
                            _owner.SuspendPaintingFlag = true;
                            try {
                                miScroll.Invoke(_owner, new object[] { fUp });
                                for(int i = 1; i < count; i++) {
                                    if(control.Enabled) {
                                        miScroll.Invoke(_owner, new object[] { fUp });
                                    }
                                    else {
                                        return i;
                                    }
                                }
                                return count;
                            }
                            finally {
                                _owner.SuspendPaintingFlag = false;
                            }
                        }
                    }
                }
                return 0;
            }
        }
    }
}
