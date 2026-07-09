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
using System.Windows.Forms;

namespace QTTabBarLib {
    public sealed class NativeWindowController : NativeWindow {
        internal event MessageEventHandler MessageCaptured;
        // private bool fNoMoreImmersiveColorSet;

        public NativeWindowController(IntPtr hwnd) {
            AssignHandle(hwnd);
        }

        protected override void WndProc(ref Message m) {
            bool consumed = false;
            if(MessageCaptured != null)
            {
                // QTLogger.log("msg\t" + Enum.GetName(typeof(MsgEnum), m.Msg) + "\tw\t" + m.WParam + "\tl\t" + m.LParam);
                /*switch (m.Msg)
                {
                    case 26:
                        QTLogger.log("NativeWindowController WndProc 26");
                        string str = string.Empty;
                        try
                        {
                            if (m.LParam != IntPtr.Zero)
                                str = Marshal.PtrToStringUni(m.LParam);
                        }
                        catch
                        {
                        }
                        if (!(str == "Environment"))
                        {
                            if (str == "ImmersiveColorSet")
                            {
                                if (!this.fNoMoreImmersiveColorSet)
                                {
                                    QTUtility.RefreshShellStateValues();
                                    ShellColors.Refresh();
                                    TabInstanceRegistry.SyncToolbarColorThreads();
                                    this.fNoMoreImmersiveColorSet = true;
                                    ActionDelayer.Add((Action)(() => this.fNoMoreImmersiveColorSet = false), 3000);
                                }
                            }
                            else
                            {
                                QTUtility.RefreshShellStateValues();
                            }
                        }
                        else
                        {
                           //  LauncherManager.SetDirty();
                        }
                            

                        if ((int)(long)m.WParam == 20)
                        {
                           //  CompatibleView.UpdateDesktopFocusedColor();
                        }
                            
                        m.Result = IntPtr.Zero;
                        break;
                }*/
                try {
                    consumed = MessageCaptured(ref m);
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex, String.Format(m.ToString()));
                }
            }
            if(!consumed) {
                base.WndProc(ref m);
            }
        }

        // #4 句柄释放评估结论：
        // 这些 NativeWindowController 子类化的是 Explorer 自身的窗口（SysListView32 /
        // SHELLDLL_DefView / ShellTabWindowClass / Edit 等），这些窗口同时还被 QTHookLib
        // 原生子类化。若在此强行调用 base.ReleaseHandle()（经 SetWindowLong 恢复窗口过程），
        // 可能与原生子类化的过程链相互破坏，因此保留“不调用 base”的原设计。真正的子类化
        // 解除由窗口销毁时的 WM_NCDESTROY 经 base.WndProc 自动完成，不会长期泄漏。
        // 这里负责托管侧清理：断开事件委托链，并清零保存的外部句柄，避免残留已释放的死
        // 句柄；可安全重复调用（幂等）。
        public override void ReleaseHandle() {
            MessageCaptured = null;
            OptionalHandle = IntPtr.Zero;
        }

        public IntPtr OptionalHandle { get; set; }

        internal delegate bool MessageEventHandler(ref Message msg);
    }
}
