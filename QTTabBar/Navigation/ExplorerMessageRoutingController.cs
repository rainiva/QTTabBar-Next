using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerMessageRoutingController {
        private readonly IExplorerMessageRoutingHost _host;

        internal ExplorerMessageRoutingController(IExplorerMessageRoutingHost host) {
            _host = host;
        }

        internal bool Route(ref Message message) {
            switch(message.Msg) {
                case WM.SETTINGCHANGE:
                    if(OSDetector.IsXP) ShellStateService.GetShellClickMode();
                    if(Marshal.PtrToStringUni(message.LParam) == "Environment") QTTabBarClass.SyncTaskBarMenu();
                    return false;
                case WM.NCLBUTTONDOWN:
                case WM.NCRBUTTONDOWN:
                case WM.NCMBUTTONDOWN:
                case WM.NCXBUTTONDOWN:
                    _host.HideTabSwitcher();
                    return false;
                case WM.MOVE:
                case WM.SIZE:
                    _host.HideViewTips(false);
                    return false;
                case WM.ACTIVATE:
                    if((((int)message.WParam) & 0xffff) > 0) _host.ActivateExplorerInstance();
                    else _host.HandleExplorerDeactivated();
                    return false;
                case WM.CLOSE:
                    return _host.TryHandleClose(message.LParam);
                case WM.SYSCOMMAND:
                    return HandleSystemCommand(ref message);
                case WM.POWERBROADCAST:
                    return false;
                case WM.DEVICECHANGE:
                    if((int)message.WParam == 0x8004) HandleDeviceChange(message.LParam);
                    return false;
                case WM.PARENTNOTIFY:
                    switch(((int)message.WParam) & 0xffff) {
                        case WM.LBUTTONDOWN:
                        case WM.RBUTTONDOWN:
                        case WM.MBUTTONDOWN:
                        case WM.XBUTTONDOWN:
                            _host.HideTabSwitcher();
                            break;
                    }
                    return false;
                case WM.APPCOMMAND:
                    return HandleAppCommand(message.LParam);
            }
            return false;
        }

        private bool HandleSystemCommand(ref Message message) {
            int command = ((int)message.WParam) & 0xfff0;
            if(command == 0xf020) {
                _host.NotifyExplorerState(ExplorerWindowActions.Minimized);
                if(Config.Window.TrayOnMinimize) {
                    _host.MinimizeToTray();
                    return true;
                }
                return false;
            }
            if(command == 0xf030) {
                _host.NotifyExplorerState(ExplorerWindowActions.Maximized);
                return false;
            }
            if(command == 0xf120) {
                _host.NotifyExplorerState(ExplorerWindowActions.Restored);
                return false;
            }
            if(Config.Window.TrayOnClose && ((int)message.WParam == 0xf060 || (int)message.WParam == 0xf063) && Control.ModifierKeys != Keys.Shift) {
                _host.MinimizeToTray();
                return true;
            }
            if(!OSDetector.IsXP || ((int)message.WParam != 0xf060 && (int)message.WParam != 0xf063)) return false;
            _host.CloseExplorer(3);
            return true;
        }

        private void HandleDeviceChange(IntPtr lParam) {
            DEV_BROADCAST_HDR header = (DEV_BROADCAST_HDR)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_HDR));
            if(header.dbch_devicetype != 2) return;
            DEV_BROADCAST_VOLUME volume = (DEV_BROADCAST_VOLUME)Marshal.PtrToStructure(lParam, typeof(DEV_BROADCAST_VOLUME));
            uint mask = volume.dbcv_unitmask;
            ushort drive = 0;
            while(drive < 0x1a) {
                if((mask & 1) != 0) break;
                mask >>= 1;
                drive++;
            }
            _host.CloseTabsForDisconnectedDrive(((char)(drive + 0x41)) + @":\");
        }

        private bool HandleAppCommand(IntPtr lParam) {
            const int BrowserBackward = 1;
            const int BrowserForward = 2;
            const int Close = 31;
            const int MouseFlag = 0x8000;
            const int FlagMask = 0xF000;
            int command = ((((int)(long)lParam) >> 16) & 0xFFFF) & ~FlagMask;
            int device = ((((int)(long)lParam) >> 16) & 0xFFFF) & FlagMask;
            bool process = device != MouseFlag;
            BindAction action;
            switch(command) {
                case BrowserBackward:
                    QTLogger.log("APPCOMMAND_BROWSER_BACKWARD");
                    if(process && Config.Mouse.GlobalMouseActions.TryGetValue(QTUtility.MakeMouseChord(MouseChord.X1, Control.ModifierKeys), out action)) _host.ExecuteBindAction(action);
                    return true;
                case BrowserForward:
                    QTLogger.log("APPCOMMAND_BROWSER_FORWARD");
                    if(process && Config.Mouse.GlobalMouseActions.TryGetValue(QTUtility.MakeMouseChord(MouseChord.X2, Control.ModifierKeys), out action)) _host.ExecuteBindAction(action);
                    return true;
                case Close:
                    QTLogger.log("APPCOMMAND_CLOSE");
                    _host.CloseExplorer(0);
                    return true;
            }
            return false;
        }
    }
}
