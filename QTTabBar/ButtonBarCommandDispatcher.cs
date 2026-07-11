using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal interface IButtonBarCommandSurface {
        void RaiseButtonBarGotFocus(EventArgs e);
        void DispatchButtonBarCommand(int buttonId);
        void HandleButtonBarEmptyAreaDoubleClick();
        void ApplyButtonBarOpacity(int opacity);
    }

    internal sealed class ButtonBarCommandDispatcher {
        private readonly ToolStripClasses _toolStrip;
        private readonly IButtonBarCommandSurface _surface;

        public ButtonBarCommandDispatcher(ToolStripClasses toolStrip, IButtonBarCommandSurface surface) {
            _toolStrip = toolStrip;
            _surface = surface;
        }

        public void OnGotFocus(bool isHandleCreated, EventArgs e) {
            if(isHandleCreated) {
                _surface.RaiseButtonBarGotFocus(e);
            }
        }

        public void OnItemClicked(ToolStripItemClickedEventArgs e) {
            if(e.ClickedItem == null || e.ClickedItem.Tag == null) return;
            _surface.DispatchButtonBarCommand((int)e.ClickedItem.Tag);
        }

        public void OnMouseActivated() {
            Point point = _toolStrip.PointToClient(Control.MousePosition);
            ToolStripItem itemAt = _toolStrip.GetItemAt(point);
            if(itemAt == null || !itemAt.Enabled) return;

            ToolStripSplitButton splitButton = itemAt as ToolStripSplitButton;
            if(splitButton != null) {
                if((itemAt.Bounds.X + splitButton.ButtonBounds.Width + splitButton.SplitterBounds.Width) < point.X) {
                    splitButton.ShowDropDown();
                }
                else {
                    splitButton.PerformButtonClick();
                }
                return;
            }

            ToolStripDropDownItem dropDownItem = itemAt as ToolStripDropDownItem;
            if(dropDownItem != null) {
                dropDownItem.ShowDropDown();
                return;
            }
            itemAt.PerformClick();
        }

        public void OnMouseDoubleClick(MouseEventArgs e) {
            if(_toolStrip.GetItemAt(e.Location) == null) {
                _surface.HandleButtonBarEmptyAreaDoubleClick();
            }
        }

        public void OnPreviewKeyDown(PreviewKeyDownEventArgs e) {
            switch(e.KeyCode) {
                case Keys.Left:
                case Keys.Right:
                case Keys.F6:
                case Keys.Tab:
                    e.IsInputKey = true;
                    DropDownMenuBase.ExitMenuMode();
                    break;
            }
        }

        public void OnOpacityChanged(ToolStripTrackBar trackBar) {
            _surface.ApplyButtonBarOpacity(trackBar.Value);
        }

        public int TranslateAcceleratorIO(ref MSG msg, ToolStripSearchBox searchBox) {
            if(msg.message != WM.KEYDOWN) return 1;

            Keys wParam = (Keys)((int)((long)msg.wParam));
            switch(wParam) {
                case Keys.Left:
                case Keys.Right:
                case Keys.Tab:
                case Keys.F6: {
                        if((wParam == Keys.Right || wParam == Keys.Left) && _toolStrip.Items.OfType<ToolStripControlHost>()
                                .Any(item => item.Visible && item.Enabled && item.Selected)) {
                            return 1;
                        }
                        bool previous = Control.ModifierKeys == Keys.Shift || wParam == Keys.Left;
                        if(previous && _toolStrip.OverflowButton.Selected) {
                            for(int j = _toolStrip.Items.Count - 1; j > -1; j--) {
                                ToolStripItem item = _toolStrip.Items[j];
                                if(item.Visible && item.Enabled) {
                                    item.Select();
                                    return 0;
                                }
                            }
                        }
                        for(int i = 0; i < _toolStrip.Items.Count; i++) {
                            if(!_toolStrip.Items[i].Selected) continue;
                            ToolStripItem start = _toolStrip.Items[i];
                            if(start is ToolStripControlHost) _toolStrip.Select();
                            while((start = _toolStrip.GetNextItem(start, previous ? ArrowDirection.Left : ArrowDirection.Right)) != null) {
                                int index = _toolStrip.Items.IndexOf(start);
                                if(previous && (index > i || start is ToolStripOverflowButton)) return 1;
                                if(!previous && index < i) {
                                    if(_toolStrip.OverflowButton.Visible) {
                                        _toolStrip.OverflowButton.Select();
                                        return 0;
                                    }
                                    return 1;
                                }
                                ToolStripControlHost host = start as ToolStripControlHost;
                                if(host != null) {
                                    host.Control.Select();
                                    return 0;
                                }
                                if(start.Enabled) {
                                    start.Select();
                                    return 0;
                                }
                            }
                            return 1;
                        }
                        break;
                    }
                case Keys.Down:
                case Keys.Space:
                case Keys.Return:
                    if(_toolStrip.OverflowButton.Selected) {
                        _toolStrip.OverflowButton.ShowDropDown();
                        return 0;
                    }
                    foreach(ToolStripItem item in _toolStrip.Items) {
                        if(!item.Selected) continue;
                        ToolStripDropDownItem dropDownItem = item as ToolStripDropDownItem;
                        if(dropDownItem != null) {
                            dropDownItem.ShowDropDown();
                        }
                        else {
                            if(item is ToolStripSearchBox && (wParam == Keys.Return || wParam == Keys.Space)) return 1;
                            if(wParam != Keys.Down) item.PerformClick();
                        }
                        return 0;
                    }
                    break;
                case Keys.Back:
                    if(_toolStrip.Items.OfType<ToolStripControlHost>().Any(item => item.Selected)) {
                        PInvoke.SendMessage(msg.hwnd, WM.CHAR, msg.wParam, msg.lParam);
                        return 0;
                    }
                    break;
                case Keys.A:
                case Keys.C:
                case Keys.V:
                case Keys.X:
                case Keys.Z:
                    if(Control.ModifierKeys == Keys.Control && searchBox != null && searchBox.Selected) {
                        PInvoke.TranslateMessage(ref msg);
                        if(wParam == Keys.A) searchBox.TextBox.SelectAll();
                        return 0;
                    }
                    break;
                case Keys.Delete:
                    if(_toolStrip.Items.OfType<ToolStripControlHost>().Any(item => item.Selected)) {
                        PInvoke.SendMessage(msg.hwnd, WM.KEYDOWN, msg.wParam, msg.lParam);
                        return 0;
                    }
                    break;
            }
            return 1;
        }

        public void UIActivateIO(int fActivate, Keys modifierKeys) {
            if(fActivate == 0) return;
            _toolStrip.Focus();
            if(_toolStrip.Items.Count == 0) return;

            if(modifierKeys != Keys.Shift) {
                for(int i = 0; i < _toolStrip.Items.Count; i++) {
                    if(SelectItem(_toolStrip.Items[i])) return;
                }
                return;
            }
            if(_toolStrip.OverflowButton.Visible) {
                _toolStrip.OverflowButton.Select();
                return;
            }
            for(int i = _toolStrip.Items.Count - 1; i > -1; i--) {
                if(SelectItem(_toolStrip.Items[i])) return;
            }
        }

        private static bool SelectItem(ToolStripItem item) {
            if(!item.Enabled || !item.Visible || item is ToolStripSeparator) return false;
            ToolStripControlHost host = item as ToolStripControlHost;
            if(host != null) {
                host.Control.Select();
            }
            else {
                item.Select();
            }
            return true;
        }
    }
}
