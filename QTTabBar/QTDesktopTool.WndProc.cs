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
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTTabBarLib.Interop;
using IShellBrowser = QTTabBarLib.Interop.IShellBrowser;
using MSG = BandObjectLib.MSG;
using Timer = System.Windows.Forms.Timer;


namespace QTTabBarLib {
    public sealed partial class QTDesktopTool : BandObject, IDeskBand2 {
        #region ---------- Overriding Methods ----------

        public override int SetSite(object pUnkSite) {
            if(BandObjectSite != null) {
                QTLogger.log("ReleaseComObject BandObjectSite");
                Marshal.ReleaseComObject(BandObjectSite);
            }
            BandObjectSite = (IInputObjectSite)pUnkSite;
            // 测试DPI兼容 indiff
            PInvoke.SetProcessDPIAware();

            Application.EnableVisualStyles();

            ReadSetting();
            InitializeComponent();
            InstallDesktopHook();

            TitleMenuItem.DrawBackground = tsmiVSTitle.Checked;
            return 0;
        }

        public override void CloseDW(uint dwReserved) {
            // called when the user disables the Desktop Tool
            // this seems not to be called on log off / shut down...

            if(iContextMenu2 != null) {
                QTLogger.log("ReleaseComObject iContextMenu2");
                Marshal.ReleaseComObject(iContextMenu2);
                iContextMenu2 = null;
            }

            // dispose controls in the thread they're created.
            if(thumbnailTooltip != null) {
                thumbnailTooltip.Invoke(d => d.Dispose());
                thumbnailTooltip = null;
            }
            if(subDirTip != null) {
                subDirTip.Invoke(d => d.Dispose());
                subDirTip = null;
            }

            // unhook, unsubclass
            if(hHook_MsgDesktop != IntPtr.Zero) {
                PInvoke.UnhookWindowsHookEx(hHook_MsgDesktop);
                hHook_MsgDesktop = IntPtr.Zero;
            }

            if(hHook_MsgShell_TrayWnd != IntPtr.Zero) {
                PInvoke.UnhookWindowsHookEx(hHook_MsgShell_TrayWnd);
                hHook_MsgShell_TrayWnd = IntPtr.Zero;
            }

            if(hHook_KeyDesktop != IntPtr.Zero) {
                PInvoke.UnhookWindowsHookEx(hHook_KeyDesktop);
                hHook_KeyDesktop = IntPtr.Zero;
            }

            if(shellViewListener != null) {
                shellViewListener.ReleaseHandle();
                shellViewListener = null;
            }

            // #3 反订阅：InitializeComponent 中为各菜单/下拉挂接的 ItemClicked 等事件无对应 -=，
            // 在关闭点统一反订阅，避免菜单对 this 的事件引用长期驻留。
            if(contextMenu != null) {
                contextMenu.ItemClicked -= dropDowns_ItemClicked;
                contextMenu.Closing -= contextMenu_Closing;
                contextMenu.ReorderFinished -= contextMenu_ReorderFinished;
                contextMenu.ItemRightClicked -= dropDowns_ItemRightClicked;
            }
            if(ddmrGroups != null) {
                ddmrGroups.ReorderFinished -= dropDowns_ReorderFinished;
                ddmrGroups.ItemClicked -= dropDowns_ItemClicked;
                ddmrGroups.ItemRightClicked -= dropDowns_ItemRightClicked;
            }
            if(ddmrHistory != null) {
                ddmrHistory.ItemClicked -= dropDowns_ItemClicked;
                ddmrHistory.ItemRightClicked -= dropDowns_ItemRightClicked;
            }
            if(ddmrUserapps != null) {
                ddmrUserapps.ReorderFinished -= dropDowns_ReorderFinished;
                ddmrUserapps.ItemClicked -= dropDowns_ItemClicked;
                ddmrUserapps.ItemRightClicked -= dropDowns_ItemRightClicked;
            }
            if(ddmrRecentFile != null) {
                ddmrRecentFile.ItemClicked -= dropDowns_ItemClicked;
                ddmrRecentFile.ItemRightClicked -= dropDowns_ItemRightClicked;
            }
            if(tsmiExperimental != null) {
                tsmiExperimental.DropDownItemClicked -= tsmiExperimental_DropDownItemClicked;
                tsmiExperimental.DropDownOpening -= tsmiExperimental_DropDownOpening;
            }
            if(contextMenuForSetting != null) {
                contextMenuForSetting.ItemClicked -= contextMenuForSetting_ItemClicked;
            }
            MouseClick -= desktopTool_MouseClick;
            MouseDoubleClick -= desktopTool_MouseDoubleClick;

            base.CloseDW(dwReserved);
        }

        public override void GetClassID(out Guid pClassID) {
            pClassID = typeof(QTDesktopTool).GUID;
        }

        protected override void WndProc(ref Message m) {
            const int MA_NOACTIVATEANDEAT = 4;
            const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

            switch(m.Msg) {
                case WM.INITMENUPOPUP:
                case WM.DRAWITEM:
                case WM.MEASUREITEM:

                    // these messages are forwarded to draw sub items in 'Send to" of shell context menu.
                    if(iContextMenu2 != null) {
                        iContextMenu2.TryHandleMenuMsg(m.Msg, m.WParam, m.LParam);
                        return;
                    }
                    break;


                case WM.MOUSEACTIVATE:
                    if(Config.Desktop.OneClickMenu) {
                        if(m.LParam.HiWord() == WM.LBUTTONDOWN) {
                            if(contextMenu.Visible) {
                                contextMenu.Close(ToolStripDropDownCloseReason.AppClicked);
                                m.Result = (IntPtr)MA_NOACTIVATEANDEAT;
                                return;
                            }
                        }
                    }
                    break;

                case WM_DWMCOMPOSITIONCHANGED:
                    Invalidate();
                    break;

                // TODO: UpdateConfig should call this through invoke, if it's even necessary.
                /*
                case MC.QTDT_REFRESH_TEXTRES:
                    RefreshStringResources();
                    break;*/
            }

            base.WndProc(ref m);
        }

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            // background
            if(VisualStyleRenderer.IsSupported) {
                if(bgRenderer == null) {
                    bgRenderer = new VisualStyleRenderer(VisualStyleElement.Taskbar.BackgroundTop.Normal);
                }
                bgRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);
            }
            else {
                base.OnPaintBackground(e);
            }

            // strings
            if(!fNowMouseHovering) return;
            Color clr = VisualStyleRenderer.IsSupported ? SystemColors.Window : SystemColors.WindowText;

            stringFormat = stringFormat ?? new StringFormat {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };
            
            using(SolidBrush sb = new SolidBrush(Color.FromArgb(128, clr))) {
                e.Graphics.DrawString(TEXT_TOOLBAR, Font, sb,
                        new Rectangle(0, 5, e.ClipRectangle.Width - 1, e.ClipRectangle.Height - 6),
                        stringFormat);
            }

            using(Pen p = new Pen(Color.FromArgb(128, clr))) {
                e.Graphics.DrawRectangle(p,
                        new Rectangle(0, 2, e.ClipRectangle.Width - 1, e.ClipRectangle.Height - 3));
            }
        }

        protected override void OnMouseEnter(EventArgs e) {
            fNowMouseHovering = true;
            base.OnMouseEnter(e);
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e) {
            fNowMouseHovering = false;
            base.OnMouseLeave(e);
            Invalidate();
        }

        #endregion
    }
}
