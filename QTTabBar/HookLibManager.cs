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
using System.Linq;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public static class HookLibManager {
        // Hook state fields moved to HookStateManager for unified state management
        private static int[] hookStatus = Enumerable.Repeat(-1, Enum.GetNames(typeof(Hooks)).Length).ToArray();
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void HookLibCallback(int hookId, int retcode);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate bool NewWindowCallback(IntPtr pIDL);

        [StructLayout(LayoutKind.Sequential)]
        private struct CallbackStruct {
            public HookLibCallback cbHookResult;
            public NewWindowCallback cbNewWindow;
            // todo: NewTreeView should probably also go here.
            // Using PostThreadMessage has a small chance of causing a memory leak.
        }

        private static readonly CallbackStruct callbackStruct = new CallbackStruct() {
            cbHookResult = HookResult,
            cbNewWindow = NewWindow
        };

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InitShellBrowserHookDelegate(IntPtr shellBrowser);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate int InitHookLibDelegate(CallbackStruct fpHookResult);

        public enum HookCheckPoint{
            Initial,
            ShellBrowser,
            NewWindow,
            Automation,
        }

        // Unmarked hooks exist only to set other hooks.
        private enum Hooks {
            CoCreateInstance = 0,           // Treeview Middle-click
            RegisterDragDrop,               // DragDrop into SubDirTips
            SHCreateShellFolderView,
            BrowseObject,                   // Control Panel dialog OK/Cancel buttons
            CreateViewWindow3,              // Header in all Views
            MessageSFVCB,                   // Refresh = clear text
            UiaReturnRawElementProvider,
            QueryInterface,                 // Scrolling Lag workaround
            TravelToEntry,                  // Clear Search bar = back
            OnActivateSelection,            // Recently activated files
            SetNavigationState,             // Breadcrumb Bar Middle-click
            ShowWindow,                     // New Explorer window capturing
            UpdateWindowList,               // Compatibility with SHOpenFolderAndSelectItems
            CreateWindowExW ,
            DestroyWindow,
            BeginPaint,
            FillRect,
            CreateCompatibleDC
        }

        /** Do not initialize hook.*/
        public static void Initialize_donot()
        {
            QTUtility2.log("Do not initialize hook" );
        }

        public static void Initialize_bgtool()
        {
            if (HookStateManager.Handle != IntPtr.Zero) return;
            string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "QTTabBar");
            // string filename = IntPtr.Size == 8 ? "QTHookLib64.dll" : "QTHookLib32.dll";
            string filename =  "ExplorerBgTool.dll";
            HookStateManager.SetHandle(PInvoke.LoadLibrary(Path.Combine(installPath, filename)));
            int retcode = -1;
            if (HookStateManager.Handle == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                QTUtility2.MakeErrorLog(null, "LoadLibrary error: " + error);
            }
            else
            {
                IntPtr pFunc = PInvoke.GetProcAddress(HookStateManager.Handle, "OnWindowLoad");
                if (pFunc != IntPtr.Zero)
                {
                    InitHookLibDelegate initialize = (InitHookLibDelegate)
                        Marshal.GetDelegateForFunctionPointer(pFunc, typeof(InitHookLibDelegate));
                    try
                    {
                        retcode = initialize(callbackStruct);
                    }
                    catch (Exception e)
                    {
                        QTUtility2.MakeErrorLog(e, "");
                    }

                }
            }

            if (retcode == 0)
            {
                QTUtility2.log("HookLib Initialize success");
                return;
            }
            QTUtility2.MakeErrorLog(null, "HookLib Initialize failed: " + retcode);

            MessageForm.Show(IntPtr.Zero,
                String.Format(
                    "{0}: {1} {2}",
                    QTUtility.TextResourcesDic["ErrorDialogs"][4],
                    QTUtility.TextResourcesDic["ErrorDialogs"][5],
                    QTUtility.TextResourcesDic["ErrorDialogs"][7]
                ),
                QTUtility.TextResourcesDic["ErrorDialogs"][1],
                MessageBoxIcon.Hand,
                30000, false, true
            );
        }


        public static void Initialize()
        {
            QTUtility2.flog("Win11Probe HookLibManager.Initialize.Start");
            try
            {
                if (HookStateManager.IsLoaded)
                {
                    return;
                }

                // Replace WMI query with registry query for OS detection.
                // WMI is slow and blocks startup; registry is faster.
                // Use the safe reader so a missing/restricted key degrades gracefully.
                string productName = (Config.SafeGetRegistryValue(
                        Registry.LocalMachine,
                        @"SOFTWARE\Microsoft\Windows NT\CurrentVersion",
                        "ProductName",
                        "") as string) ?? "";
                bool isServer = productName.ToLower().Contains("server");
                if(isServer) {
                    QTUtility2.log("can not hook in server by registry check");
                    HookStateManager.SetLoaded(false);
                    return;
                }

                string installPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "QTTabBar");
                string filename = IntPtr.Size == 8 ? "QTHookLib64.dll" : "QTHookLib32.dll";
                if (HookStateManager.Handle != IntPtr.Zero)
                {
                       if (!Config.Window.AutoHookWindow)
                       {
                            PInvoke.FreeLibrary(HookStateManager.Handle);
                            HookStateManager.SetHandle(IntPtr.Zero);
                       }
                    return;
                }

                if (!Config.Window.AutoHookWindow)
                {
                    HookStateManager.SetLoaded(false);
                    return;
                }

                if (!File.Exists(Path.Combine(installPath, filename)))
                {
                    QTUtility2.flog("not exists file , close auto hook " + Path.Combine(installPath, filename));
                    HookStateManager.SetLoaded(false);
                    return;
                }
                QTUtility2.flog("Win11Probe HookLibManager.Initialize.LoadLibrary");
                QTUtility2.flog("load library " + Path.Combine(installPath, filename) );
                HookStateManager.SetHandle(PInvoke.LoadLibrary(Path.Combine(installPath, filename)));
                QTUtility2.flog("load library hHookLib " + HookStateManager.Handle);
                int retcode = -1;
                if(HookStateManager.Handle == IntPtr.Zero) {
                    int error = Marshal.GetLastWin32Error();
                    QTUtility2.MakeErrorLog(null, "LoadLibrary error: " + error);
                }
                else {
                    IntPtr pFunc = PInvoke.GetProcAddress(HookStateManager.Handle, "Initialize");
                    if(pFunc != IntPtr.Zero) {
                        InitHookLibDelegate initialize = (InitHookLibDelegate) 
                            Marshal.GetDelegateForFunctionPointer(pFunc, typeof(InitHookLibDelegate));
                            QTUtility2.flog("Win11Probe HookLibManager.Initialize.InvokeNativeInitialize");
                        try {
                            retcode = initialize(callbackStruct);
                        }
                        catch(Exception e) {
                            QTUtility2.MakeErrorLog(e, "HookLibManager.Initialize: native Initialize threw");
                            HookStateManager.SetLoaded(false);
                        }
                    }
                }

                if (retcode == 0)
                {
                    HookStateManager.SetLoaded(true);
                    QTUtility2.log("HookLib Initialize success");
                    // MessageBox.Show("HookLib Initialize success");
                    return;
                }
                QTUtility2.MakeErrorLog(null, "HookLib Initialize failed: " + retcode);
                HookStateManager.SetLoaded(false);
                MessageForm.Show(IntPtr.Zero,
                    String.Format(
                        "{0}: {1} {2}",
                        QTUtility.TextResourcesDic["ErrorDialogs"][4],
                        QTUtility.TextResourcesDic["ErrorDialogs"][5],
                        QTUtility.TextResourcesDic["ErrorDialogs"][7]
                    ),
                    QTUtility.TextResourcesDic["ErrorDialogs"][1],
                    MessageBoxIcon.Hand, 
                    30000, false, true
                );
            }
            catch (DllNotFoundException dllEx)
            {
                // Hook DLL (or a dependency) could not be resolved: degrade to disabled.
                QTUtility2.MakeErrorLog(dllEx, "HookLibManager.Initialize: hook DLL not found; disabling hooks");
                HookStateManager.SetLoaded(false);
            }
            catch (System.Security.SecurityException secEx)
            {
                // Registry / security access denied during OS detection or config read.
                QTUtility2.MakeErrorLog(secEx, "HookLibManager.Initialize: registry/security access failure; disabling hooks");
                HookStateManager.SetLoaded(false);
            }
            catch (UnauthorizedAccessException uaEx)
            {
                QTUtility2.MakeErrorLog(uaEx, "HookLibManager.Initialize: unauthorized registry access; disabling hooks");
                HookStateManager.SetLoaded(false);
            }
            catch (Exception ex)
            {
                // Any other unexpected failure must not interrupt Explorer startup.
                QTUtility2.MakeErrorLog(ex, "HookLibManager.Initialize: unexpected failure; disabling hooks");
                HookStateManager.SetLoaded(false);
            }
        }


        private static void HookResult(int hookId, int retcode) {
            lock(callbackStruct.cbHookResult) {
                if (hookId <= hookStatus.Length - 1)
                {
                    hookStatus[hookId] = retcode;
                }
            }
        }

        // We need to use a callback rather than a message for window capturing,
        // since the main instance could be in another process.
        private static bool NewWindow(IntPtr pIDL) {
            byte[] IDL;
            using(IDLWrapper wrapper = new IDLWrapper(PInvoke.ILClone(pIDL))) {
                if(!Config.Window.CaptureNewWindows
                        || InstanceManager.GetTotalInstanceCount() == 0
                        || QTUtility2.IsShellPathButNotFileSystem(wrapper.Path)
                        || wrapper.Path.PathEquals(QTUtility.PATH_SEARCHFOLDER)
                        || QTUtility.NoCapturePathsList.Any(path => wrapper.Path.PathEquals(path))
                        || (Control.ModifierKeys & Keys.Control) != Keys.None) {
                    return false;
                }
                IDL = wrapper.IDL;
            }
            InstanceManager.BeginInvokeMain(tabbar => {
                QTUtility2.log("BeginInvokeMain OpenNewTabOrWindow");
                using (IDLWrapper wrapper = new IDLWrapper(IDL)) {
                    tabbar.OpenNewTabOrWindow(wrapper, true);
                }
            });
            return true;
        }
        /** do not init shell brownser hook. */
        public static void InitShellBrowserHook_old(IShellBrowser shellBrowser) { }

        public static void InitShellBrowserHook(IShellBrowser shellBrowser)
        {
            QTUtility2.flog("Win11Probe HookLibManager.InitShellBrowserHook.Start");
            lock (typeof(HookLibManager))
            {
                if(HookStateManager.ShellBrowserHooked || HookStateManager.Handle == IntPtr.Zero) return;
                IntPtr pFunc = PInvoke.GetProcAddress(HookStateManager.Handle, "InitShellBrowserHook");
                if(pFunc == IntPtr.Zero) return;
                InitShellBrowserHookDelegate initShellBrowserHook = (InitShellBrowserHookDelegate)
                        Marshal.GetDelegateForFunctionPointer(pFunc, typeof(InitShellBrowserHookDelegate));
                IntPtr pShellBrowser = Marshal.GetComInterfaceForObject(shellBrowser, typeof(IShellBrowser));
                if(pShellBrowser == IntPtr.Zero) return;
                int retcode = -1;
                try {
                    retcode = initShellBrowserHook(pShellBrowser);
                    QTUtility2.flog("Win11Probe HookLibManager.InitShellBrowserHook.NativeResult " + retcode);
                }
                catch(Exception e) {
                    QTUtility2.MakeErrorLog(e, "");
                }
                finally {
                    Marshal.Release(pShellBrowser);
                }
                if(retcode != 0) {
                    QTUtility2.MakeErrorLog(null, "InitShellBrowserHook failed: " + retcode);

                    MessageForm.Show(IntPtr.Zero,
                        String.Format(
                            "{0}: {1} {2}",
                            QTUtility.TextResourcesDic["ErrorDialogs"][4],
                            QTUtility.TextResourcesDic["ErrorDialogs"][6],
                            QTUtility.TextResourcesDic["ErrorDialogs"][7]
                        ),
                        QTUtility.TextResourcesDic["ErrorDialogs"][1],
                        MessageBoxIcon.Hand, 30000, false, true
                    );
                }
                else {
                    HookStateManager.SetShellBrowserHooked(true);
                }
            }
        }

        public static void CheckHooks() {
            if(!HookStateManager.IsLoaded || HookStateManager.Handle == IntPtr.Zero) return;
            for(int i = 0; i < hookStatus.Length; i++) {
                if(hookStatus[i] != 0) {
                    QTUtility2.flog("Hook " + ((Hooks)i) + " status: " + hookStatus[i]);
                }
            }
        }
    }
}
