//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022 indiff  Quizo, Paul Accisano
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BandObjectLib;
using Microsoft.Win32;
using SHDocVw;

namespace QTTabBarLib {

    [Guid("D2BF470E-ED1C-487F-A777-2BD8835EB6CE"), ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public class AutoLoader : IObjectWithSite {
        private IWebBrowser2 explorer;       
        private const string BHOKEYNAME = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Browser Helper Objects\";
        private const int E_FAIL = unchecked((int)0x80004005);

        [ComRegisterFunction]
        public static void Register(Type t) {
            string name = t.GUID.ToString("B");
            ComRegistrationManager.RegisterBand(name, "QTTabBar AutoLoader", "QTTabBar AutoLoader", "QTTabBar AutoLoader");
            ComRegistrationManager.RegisterBho(name);
            QTLogger.flog( "AutoLoader 注册表 QTTabBar 自动加载(安装)");
        }

        [ComUnregisterFunction]
        public static void Unregister(Type t) {
            ComRegistrationManager.UnregisterBho(t.GUID.ToString("B"));
            QTLogger.flog("AutoLoader 注册表 QTTabBar 自动加载(卸载)");
        }

        public int SetSite(object site) {
            // SetProcessDPIAware是Vista以上才有的函数，这样直接调用会使得程序不兼容XP
            // PInvoke.SetProcessDPIAware();
            // QTLogger.log("QTUtility AutoLoader SetSite SetProcessDPIAware 不兼容XP");
            QTLogger.flog("Win11Probe AutoLoader.SetSite");
            QTLogger.log("SetSite");
            explorer = site as IWebBrowser2;
            // QTLogger.flog("QTTabBar AutoLoader SetSite ");
            /*if(explorer == null || Process.GetCurrentProcess().ProcessName == "iexplore") {
                QTLogger.log("QTTabBar AutoLoader SetSite Throw Exception ");
                // QTLogger.flog("QTTabBar AutoLoader SetSite Throw Exception ");
                // 基于指定的 IErrorInfo 接口，用特定失败 HRESULT 引发异常
                Marshal.ThrowExceptionForHR(E_FAIL);
            }
            else {*/

            if (explorer != null && Process.GetCurrentProcess().ProcessName.ToLower() != "iexplore")
            {
                QTLogger.log("QTTabBar AutoLoader SetSite ActivateIt ");
                // QTLogger.flog("QTTabBar AutoLoader SetSite ActivateIt ");
                ActivateIt();
            }

            return 0;
        }

        public int GetSite(ref Guid guid, out object ppvSite) {
            ppvSite = explorer;
            return 0;
        }

        private void ActivateIt() {
            if(!FirstLoadActivationService.IsFirstInstallActivationPending()) {
                return;
            }

            object pvaTabBar = new Guid("{d2bf470e-ed1c-487f-a333-2bd8835eb6ce}").ToString("B");
            object pvaButtonBar = new Guid("{d2bf470e-ed1c-487f-a666-2bd8835eb6ce}").ToString("B");
            object pvarShow = true;
            object pvarSize = null;
            bool activated = false;
            try {
                QTLogger.flog("Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.TabBar");
                explorer.ShowBrowserBar(pvaTabBar, pvarShow, pvarSize);
                QTLogger.log("QTTabBar AutoLoader 显示标签");

                QTLogger.flog("Win11Probe AutoLoader.ActivateIt.ShowBrowserBar.ButtonBar");
                explorer.ShowBrowserBar(pvaButtonBar, pvarShow, pvarSize);
                QTLogger.log("QTTabBar AutoLoader 显示工具栏");
                activated = true;
            }
            catch(COMException e) {
                QTLogger.MakeErrorLog(e, "ActivateIt");
                MessageForm.Show(
                    IntPtr.Zero,
                    QTUtility.TextResourcesDic["ErrorDialogs"][2],
                    QTUtility.TextResourcesDic["ErrorDialogs"][3],
                    MessageBoxIcon.Warning,
                    30000,
                    false,
                    true
                );
            }

            if(activated) {
                FirstLoadActivationService.MarkActivationComplete();
            }
        }
    }
}
