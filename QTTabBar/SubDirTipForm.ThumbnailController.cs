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
using System.IO;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class SubDirTipForm {

        // Batch7 GC7b: thumbnail-tooltip hover behaviour (show / hide preview,
        // key-driven tooltip timer) delegated to this controller.
        private readonly ThumbnailController _thumbnailController;

        private sealed class ThumbnailController {
            private readonly SubDirTipForm _owner;

            internal ThumbnailController(SubDirTipForm owner) {
                _owner = owner;
            }

            internal void HideThumbnailTooltip() {
                if(((_owner.thumbnailTip != null) && _owner.thumbnailTip.IsShowing) && _owner.thumbnailTip.HideToolTip()) {
                    _owner.iThumbnailIndex = -1;
                }
            }

            internal void HideThumbnailTooltip(bool fKey) {
                if(_owner.thumbnailTip != null) {
                    if(fKey) {
                        _owner.thumbnailTip.IsShownByKey = false;
                    }
                    HideThumbnailTooltip();
                }
            }

            internal bool ShowThumbnailTooltip(ToolStripMenuItemEx tsmi, bool fKey) {
                if((_owner.menuIsShowing && (_owner.draggingPath == null)) && !_owner.fSuppressThumbnail) {
                    if((!Config.Tips.SubDirTipsPreview ^ (ModifierKeys == Keys.Shift)) && ThumbnailTooltipForm.ExtIsSupported(Path.GetExtension(tsmi.ThumbnailPath).ToLower())) {
                        if(_owner.iThumbnailIndex == tsmi.ThumbnailIndex) {
                            return false;
                        }
                        if(_owner.thumbnailTip == null) {
                            _owner.thumbnailTip = new ThumbnailTooltipForm();
                        }
                        if(_owner.thumbnailTip.IsShownByKey && !fKey) {
                            _owner.thumbnailTip.IsShownByKey = false;
                            return false;
                        }
                        _owner.thumbnailTip.IsShownByKey = fKey;
                        _owner.iThumbnailIndex = tsmi.ThumbnailIndex;
                        if(_owner.thumbnailTip.ShowToolTip(tsmi.ThumbnailPath, tsmi.Owner.RectangleToScreen(tsmi.Bounds))) {
                            tsmi.ToolTipText = null;
                            return true;
                        }
                    }
                    if(tsmi.ToolTipText == null) {
                        string originalTitle = tsmi.OriginalTitle;
                        string shellInfoTipText = ShellMethods.GetShellInfoTipText(tsmi.Path, false);
                        if(shellInfoTipText != null) {
                            if(originalTitle == null) {
                                originalTitle = shellInfoTipText;
                            }
                            else {
                                originalTitle = originalTitle + "\r\n" + shellInfoTipText;
                            }
                        }
                        tsmi.ToolTipText = originalTitle;
                    }
                    HideThumbnailTooltip(fKey);
                }
                return false;
            }

            internal void timerToolTipByKey_Tick(object sender, EventArgs e) {
                try {
                    _owner.timerToolTipByKey.Enabled = false;
                    ToolStripMenuItemEx tag = _owner.timerToolTipByKey.Tag as ToolStripMenuItemEx;
                    if(((tag != null) && !tag.IsDisposed) && _owner.menuIsShowing) {
                        DropDownMenuReorderable owner = tag.Owner as DropDownMenuReorderable;
                        if(((owner != null) && owner.Visible) && (!owner.IsDisposed && !owner.Disposing)) {
                            owner.UpdateToolTipByKey(tag);
                            _owner.iToolTipIndex = tag.ThumbnailIndex;
                        }
                    }
                    _owner.timerToolTipByKey.Tag = null;
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
            }
        }
    }
}
