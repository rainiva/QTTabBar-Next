using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal void HandleSysColorChangeHookMessage() {
            ThemeRefreshService.ApplySystemTheme(true);
            tabControl1.InitializeColors();
            PInvoke.SetRedraw(ExplorerHandle, true);
            PInvoke.RedrawWindow(ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
        }

        internal bool TryHandleHookCloseMessage(MSG msg, out bool suppressMessage) {
            suppressMessage = false;
            if(OSDetector.IsXP) {
                if((msg.hwnd == ExplorerHandle) && HandleCLOSE(msg.lParam)) {
                    suppressMessage = true;
                }
                return true;
            }

            string[] list = (from QTabItem item2 in tabControl1.TabPages
                             where item2.TabLocked
                             select item2.CurrentPath).ToArray();
            QTUtility.SaveLockedTabs(list);
            if(msg.hwnd == WindowUtils.GetShellTabWindowClass(ExplorerHandle)) {
                try {
                    bool flag = tabControl1.TabCount == 1;
                    string currentPath = tabControl1.SelectedTab.CurrentPath;
                    if(!Directory.Exists(currentPath) && currentPath.Length > 3) {
                        if(flag) {
                            WindowUtils.CloseExplorer(ExplorerHandle, 2);
                        }
                        else {
                            CloseTab(tabControl1.SelectedTab, true);
                        }
                    }
                }
                catch(Exception e) {
                    QTLogger.MakeErrorLog(e, "CallbackGetMsgProc WM.Close");
                }
                suppressMessage = true;
            }
            return true;
        }

        internal bool TryHandleHookCommandMessage(MSG msg, out bool suppressMessage) {
            suppressMessage = false;
            if(OSDetector.IsXP) {
                int num = ((int)((long)msg.wParam)) & 0xffff;
                if(num == 0xa021) {
                    WindowUtils.CloseExplorer(ExplorerHandle, 3);
                    suppressMessage = true;
                    return true;
                }
            }
            return false;
        }
    }
}
