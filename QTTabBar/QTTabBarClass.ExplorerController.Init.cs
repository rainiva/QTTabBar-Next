//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
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
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using QTTabBarLib.Common;
using Control = System.Windows.Forms.Control;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using ToolTip = System.Windows.Forms.ToolTip;
using System.Management;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class ExplorerControllerModule {
            #region OnExplorerAttached

            public void OnExplorerAttachedCore() {
                QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.Start");
                _owner.ExplorerHandle = (IntPtr)_owner.Explorer.HWND;
                try {
                    object obj2;
                    object obj3;
                    _IServiceProvider bandObjectSite = (_IServiceProvider)_owner.BandObjectSite;
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.IShellBrowser");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_IShellBrowser, ExplorerGUIDs.IID_IUnknown, out obj2);
                    _owner.ShellBrowser = new ShellBrowserEx((IShellBrowser)obj2);
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.InitShellBrowserHook");
                    HookLibManager.InitShellBrowserHook(_owner.ShellBrowser.GetIShellBrowser());
                    if(Config.Tweaks.ForceSysListView) {
                        _owner.ShellBrowser.SetUsingListView(true);
                    }
                    QTLogger.flog("Win11Probe QTTabBarClass.OnExplorerAttached.QueryService.ITravelLogStg");
                    bandObjectSite.QueryService(ExplorerGUIDs.IID_ITravelLogStg, ExplorerGUIDs.IID_ITravelLogStg, out obj3);
                    _owner.TravelLog = (ITravelLogStg)obj3;
                }
                catch(COMException exception) {
                    QTLogger.MakeErrorLog(exception);
                }

                _owner.Explorer.BeforeNavigate2 += Explorer_BeforeNavigate2;
                _owner.Explorer.NavigateComplete2 += Explorer_NavigateComplete2;
                QTLogger.log("QTTabBarClass set BeforeNavigate2 NavigateComplete2");
            }

            #endregion

            #region DoFirstNavigation / InitializeInstallation

            public void DoFirstNavigation(bool before, string path) {
                bool ensureOpenedWindow = false;
                try {
                    if(StaticReg.CreateWindowPaths.Count > 0 || StaticReg.CreateWindowIDLs.Count > 0) {
                        QTLogger.log("DoFirstNavigation StaticReg.CreateWindowPaths.Count " + StaticReg.CreateWindowPaths.Count + " StaticReg.CreateWindowIDLs.Count:" + StaticReg.CreateWindowIDLs.Count);
                        foreach (string tpath in StaticReg.CreateWindowPaths.Where(str2 => !str2.PathEquals(path))) {
                            using(IDLWrapper wrapper = new IDLWrapper(tpath)) {
                                if(wrapper.Available) {
                                    _owner.CreateNewTab(wrapper);
                                }
                            }
                        }
                        foreach(byte[] idl in StaticReg.CreateWindowIDLs) {
                            using(IDLWrapper wrapper2 = new IDLWrapper(idl)) {
                                _owner.OpenNewTab(wrapper2, true);
                            }
                        }
                        QTUtility2.InitializeTemporaryPaths();
                        _owner.AddStartUpTabs(string.Empty, path);
                        ensureOpenedWindow = true;
                    }
                    else if(StaticReg.CreateWindowGroup.Length != 0) {
                        QTLogger.log("DoFirstNavigation StaticReg.CreateWindowGroup.Length " + StaticReg.CreateWindowGroup.Length);
                        string createWindowTMPGroup = StaticReg.CreateWindowGroup;
                        StaticReg.CreateWindowGroup = string.Empty;
                        _owner.CurrentTab.CurrentPath = path;
                        _owner.NowOpenedByGroupOpener = true;
                        _owner.OpenGroup(createWindowTMPGroup, false);
                        _owner.AddStartUpTabs(createWindowTMPGroup, path);
                        ensureOpenedWindow = true;
                    }
                    else if(!Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture)
                    {
                        QTLogger.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                        StaticReg.SkipNextCapture = false;
                        _owner.AddStartUpTabs(string.Empty, path);
                        ensureOpenedWindow = true;
                    }
                    else if(path.StartsWith(QTUtility.ResMisc[0]) ||
                            (path.EndsWith(QTUtility.ResMisc[0]) && QTUtility2.IsShellPathButNotFileSystem(path)) ||
                            path.PathEquals(OSDetector.PATH_SEARCHFOLDER)) {
                        QTLogger.log("DoFirstNavigation !Config.Window.CaptureNewWindows || StaticReg.SkipNextCapture");
                        ensureOpenedWindow = true;
                    }
                    else {
                        QTLogger.log("DoFirstNavigation path: " + path + " IsNoCapturePaths:" + PathValidator.IsNoCapturePaths(path));
                        if(
                            QTUtility.NoCapturePathsList.Any(ncPath => ncPath.PathEquals(path))
                             || PathValidator.IsNoCapturePaths( path )
                            ) {
                            ensureOpenedWindow = true;
                            return;
                        }
                        if (Config.Window.CaptureNewWindows &&
                            Control.ModifierKeys != Keys.Control &&
                            InstanceManager.GetTotalInstanceCount() > 0) {
                            string cmd = GetCommandLine();
                            if (!String.IsNullOrEmpty(cmd))
                            {
                                string lcmd = cmd.ToLower();
                                if (lcmd.Contains("/select") || lcmd.Contains(",select"))
                                {
                                    _owner.mCmdType = 1;
                                    string selectMe = GetNameToSelectFromCommandLineArg(cmd);
                                    TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                    InstanceManager.BeginInvokeMain(tabbar =>
                                    {
                                        tabbar.OpenNewTab(path);
                                        if (selectMe != "")
                                        {
                                            tabbar.ShellBrowser.TrySetSelection(
                                                  new Address[] { new Address(selectMe) }, null, true);
                                        }

                                        tabbar.RestoreWindow();
                                        TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                        QTLogger.log(string.Format("select cmd BeginInvokeMain cost {0} ", abs.TotalMilliseconds));
                                    });
                                }
                                else if (lcmd.Contains("/factory")   ||
                                         lcmd.Contains("-embedding") ||
                                         lcmd.Contains("{75dff2b7-6936-4c06-a8bb-676a7b00b24b}"))
                                {
                                    _owner.mCmdType = 2;
                                    TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                    InstanceManager.BeginInvokeMain(tabbar =>
                                    {
                                        tabbar.OpenNewTab(path);
                                        tabbar.RestoreWindow();
                                        if (Config.Window.CaptureWeChatSelection)
                                        {
                                            tabbar.Wait4Select();
                                        }
                                        TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                        QTLogger.log(string.Format("factory cmd BeginInvokeMain cost {0} ", abs.TotalMilliseconds));
                                    });
                                }
                                else
                                {
                                    _owner.mCmdType = 3;
                                    InstanceManager.BeginInvokeMain(tabbar =>
                                    {
                                        tabbar.OpenNewTab(path);
                                        QTLogger.log("other cmd BeginInvokeMain RestoreWindow");
                                        tabbar.RestoreWindow();
                                    });
                                }
                            }

                            _owner.fNowQuitting = true;
                            if (OSDetector.IsXP)
                            {
                                QTLogger.log("Close Explorer WindowUtils.CloseExplorer");
                                WindowUtils.CloseExplorer(_owner.ExplorerHandle, 0);
                            }
                            else
                            {
                                _owner.fHideExplorer = true;

                                if (_owner.mCmdType == 3 || !Config.Window.CaptureWeChatSelection)
                                {
                                    QTLogger.log("Close Explorer Explorer.Quit");
                                    _owner.Explorer.Quit();
                                }
                            }
                            QTLogger.log("DoFirstNavigation return");
                        }
                        QTLogger.log("AddStartUpTabs ");
                        _owner.AddStartUpTabs(string.Empty, path);
                        QTLogger.log("AddStartUpTabs InitializeOpenedWindow");
                        ensureOpenedWindow = true;
                    }
                }
                finally {
                    if(ensureOpenedWindow) {
                        InitializeOpenedWindow();
                    }
                }
            }

            public void InitializeInstallation() {
                InitializeOpenedWindow();
                object locationURL = _owner.Explorer.LocationURL;
                if(_owner.ShellBrowser != null) {
                    using(IDLWrapper wrapper = _owner.ShellBrowser.GetShellPath()) {
                        if(wrapper.Available) {
                            locationURL = wrapper.Path;
                        }
                    }
                }
                QTLogger.log("QTTabBarClass InitializeInstallation  pDisp :" + null + " locationURL :" + (string)locationURL);
                Explorer_NavigateComplete2(null, ref locationURL);
            }

            public void InitializeNavBtns(bool fSync) {
                _owner.toolStrip = new ToolStripClasses();
                _owner.buttonBack = new ToolStripButton();
                _owner.buttonForward = new ToolStripButton();
                _owner.toolStrip.SuspendLayout();
                if(!IconManager.ImageGlobalContainsKey("navBack")) {
                    IconManager.AddImageToGlobal("navBack", Resources_Image.imgNavBack);
                }
                if(!IconManager.ImageGlobalContainsKey("navFrwd")) {
                    IconManager.AddImageToGlobal("navFrwd", Resources_Image.imgNavFwd);
                }
                _owner.toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                _owner.toolStrip.AutoSize = false;
                _owner.toolStrip.CanOverflow = false;
                _owner.toolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
                _owner.toolStrip.GripStyle = ToolStripGripStyle.Hidden;
                _owner.toolStrip.Items.AddRange(new ToolStripItem[] { _owner.buttonBack, _owner.buttonForward, _owner.buttonNavHistoryMenu });
                _owner.toolStrip.Renderer = new ToolbarRenderer();
                _owner.toolStrip.Width = 0x3f;
                _owner.toolStrip.TabStop = false;
                _owner.toolStrip.BackColor = QTUtility.InNightMode ? Color.Black : Color.WhiteSmoke;

                _owner.buttonBack.AutoSize = false;
                _owner.buttonBack.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.buttonBack.Enabled = fSync ? ((_owner.navBtnsFlag & 1) != 0) : false;
                _owner.buttonBack.Image = IconManager.GetImageFromGlobal("navBack");
                _owner.buttonBack.Size = new Size(0x15, 0x15);
                _owner.buttonBack.Click += NavigationButtons_Click;
                _owner.buttonForward.AutoSize = false;
                _owner.buttonForward.DisplayStyle = ToolStripItemDisplayStyle.Image;
                _owner.buttonForward.Enabled = fSync ? ((_owner.navBtnsFlag & 2) != 0) : false;
                _owner.buttonForward.Image = IconManager.GetImageFromGlobal("navFrwd");
                _owner.buttonForward.Size = new Size(0x15, 0x15);
                _owner.buttonForward.Click += NavigationButtons_Click;
            }

            public void InitializeOpenedWindow() {
                if(_owner.fOpenedWindowInitialized) {
                    return;
                }
                _owner.fOpenedWindowInitialized = true;
                _owner.IsShown = true;
                InstanceManager.PushTabBarInstance(_owner);
                InstanceManager.SetMainUIControl(_owner);
                QTLogger.log("QTTabBarClass InitializeOpenedWindow  InstallHooks");
                InstallHooks();

                QTLogger.log("QTTabBarClass  PluginServer ");
                _owner.pluginServer = new PluginServer(_owner);

                QTLogger.log("QTTabBarClass TryCallButtonBar ");
                if(!QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems())) {
                    Timer timer = new Timer { Interval = 2000 };
                    timer.Tick += (sender, args) => {
                        QTLogger.log("QTTabBarClass timer.Tick TryCallButtonBar ");
                        QTTabBarClass.TryCallButtonBar(bbar => bbar.CreateItems());
                        timer.Stop();
                    };
                    timer.Start();
                }
                if(QTUtility.WindowAlpha < 0xff) {
                    QTLogger.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                    PInvoke.SetWindowLongPtr(_owner.ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(_owner.ExplorerHandle, -20), 0x80000));
                    PInvoke.SetLayeredWindowAttributes(_owner.ExplorerHandle, 0, QTUtility.WindowAlpha, 2);
                }

                QTLogger.log("QTTabBarClass ListViewMonitor ");
                _owner.listViewManager = new ListViewMonitor(_owner.ShellBrowser, _owner.ExplorerHandle, _owner.Handle);
                _owner.listViewManager.ListViewChanged += _owner._listViewInputController.OnListViewMonitorChanged;
                _owner.listViewManager.Initialize();

                IntPtr hwndBreadcrumbBar = WindowUtils.FindChildWindow(_owner.ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "Breadcrumb Parent");
                if(hwndBreadcrumbBar != IntPtr.Zero) {
                    hwndBreadcrumbBar = PInvoke.FindWindowEx(hwndBreadcrumbBar, IntPtr.Zero, "ToolbarWindow32", null);
                    if(hwndBreadcrumbBar != IntPtr.Zero) {
                        _owner.breadcrumbBar = new BreadcrumbBar(hwndBreadcrumbBar);
                        QTLogger.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                        _owner.breadcrumbBar.ItemClicked += (wrapper, modifierKeys, middle) => _owner._menuController.FolderLinkClicked(wrapper, modifierKeys, middle);
                    }
                }
            }

            public void InstallHooks() {
                _owner._hookInputController.Install(PInvoke.GetCurrentThreadId());
                _owner.explorerController = new NativeWindowController(_owner.ExplorerHandle);
                _owner.explorerController.MessageCaptured += explorerController_MessageCaptured;
                if(_owner.ReBarHandle != IntPtr.Zero) {
                    _owner.rebarController = new RebarController(_owner, _owner.ReBarHandle, _owner.BandObjectSite as IOleCommandTarget);
                }
                if(!OSDetector.IsXP) {
                    _owner.TravelToolBarHandle = _owner.GetTravelToolBarWindow32();
                    if(_owner.TravelToolBarHandle != IntPtr.Zero) {
                        _owner.travelBtnController = new NativeWindowController(_owner.TravelToolBarHandle);
                        _owner.travelBtnController.MessageCaptured += TravelToolbarMessageCaptured;
                    }
                }
                _owner.dropTargetWrapper = new DropTargetWrapper(_owner);
                _owner.dropTargetWrapper.DragFileEnter += _owner.dropTargetWrapper_DragFileEnter;
                _owner.dropTargetWrapper.DragFileOver += _owner.dropTargetWrapper_DragFileOver;
                _owner.dropTargetWrapper.DragFileLeave += _owner.dropTargetWrapper_DragFileLeave;
                _owner.dropTargetWrapper.DragFileDrop += _owner.dropTargetWrapper_DragFileDrop;
            }

            public bool TravelToolbarMessageCaptured(ref Message m) {
                if(_owner.CurrentTab == null) {
                    QTLogger.log("QTTabBarClass travelBtnController_MessageCaptured CurrentTab == null");
                    return false;
                }
                switch(m.Msg) {
                    case WM.LBUTTONDOWN:
                    case WM.LBUTTONUP: {
                            Point pt = QTUtility2.PointFromLPARAM(m.LParam);
                            int num = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x445, IntPtr.Zero, ref pt);
                            bool flag = _owner.CurrentTab.HistoryCount_Back > 1;
                            bool flag2 = _owner.CurrentTab.HistoryCount_Forward > 0;
                            if(m.Msg != 0x202) {
                                PInvoke.SetCapture(_owner.travelBtnController.Handle);
                                if(((flag && (num == 0)) || (flag2 && (num == 1))) || ((flag || flag2) && (num == 2))) {
                                    int num5 = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x412, (IntPtr)(0x100 + num), IntPtr.Zero);
                                    int num6 = num5 | 2;
                                    PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x411, (IntPtr)(0x100 + num), (IntPtr)num6);
                                }
                                if((num == 2) && (flag || flag2)) {
                                    RECT rect;
                                    IntPtr hWnd = PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x423, IntPtr.Zero, IntPtr.Zero);
                                    if(hWnd != IntPtr.Zero) {
                                        PInvoke.SendMessage(hWnd, 0x41c, IntPtr.Zero, IntPtr.Zero);
                                    }
                                    PInvoke.GetWindowRect(_owner.travelBtnController.Handle, out rect);
                                    NavigationButtons_DropDownOpening(_owner.buttonNavHistoryMenu, new EventArgs());
                                    _owner.buttonNavHistoryMenu.DropDown.Show(new Point(rect.left - 2, rect.bottom + 1));
                                }
                                break;
                            }
                            PInvoke.ReleaseCapture();
                            for(int i = 0; i < 3; i++) {
                                int num3 = (int)PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x412, (IntPtr)(0x100 + i), IntPtr.Zero);
                                int num4 = num3 & -3;
                                PInvoke.SendMessage(_owner.travelBtnController.Handle, 0x411, (IntPtr)(0x100 + i), (IntPtr)num4);
                            }
                            if((num == 0) && flag) {
                                NavigateCurrentTab(true);
                            }
                            else if((num == 1) && flag2) {
                                NavigateCurrentTab(false);
                            }
                            break;
                        }
                    case WM.LBUTTONDBLCLK:
                        m.Result = IntPtr.Zero;
                        return true;

                    case WM.USER+1:
                        if(((((int)((long)m.LParam)) >> 0x10) & 0xffff) == 1) {
                            return false;
                        }
                        m.Result = (IntPtr)1;
                        return true;

                    case WM.MOUSEACTIVATE:
                        if(_owner.buttonNavHistoryMenu.DropDown.Visible) {
                            m.Result = (IntPtr)4;
                            _owner.buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
                            return true;
                        }
                        return false;

                    case WM.NOTIFY: {
                            NMHDR nmhdr = (NMHDR)Marshal.PtrToStructure(m.LParam, typeof(NMHDR));
                            if(nmhdr.code != -530) {
                                return false;
                            }
                            NMTTDISPINFO nmttdispinfo = (NMTTDISPINFO)Marshal.PtrToStructure(m.LParam, typeof(NMTTDISPINFO));
                            string str;
                            if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x100)) {
                                str = MakeTravelBtnTooltipText(true);
                                if(str.Length > 0x4f) {
                                    str = "Back";
                                }
                            }
                            else if(nmttdispinfo.hdr.idFrom == ((IntPtr)0x101)) {
                                str = MakeTravelBtnTooltipText(false);
                                if(str.Length > 0x4f) {
                                    str = "Forward";
                                }
                            }
                            else {
                                return false;
                            }
                            nmttdispinfo.szText = str;
                            Marshal.StructureToPtr(nmttdispinfo, m.LParam, false);
                            m.Result = IntPtr.Zero;
                            return true;
                        }
                    default:
                        return false;
                }
                m.Result = IntPtr.Zero;
                return true;
            }

            private string MakeTravelBtnTooltipText(bool fBack) {
                string path = string.Empty;
                if(fBack) {
                    string[] historyBack = _owner.CurrentTab.GetHistoryBack();
                    if(historyBack.Length > 1) {
                        path = historyBack[1];
                    }
                }
                else {
                    string[] historyForward = _owner.CurrentTab.GetHistoryForward();
                    if(historyForward.Length > 0) {
                        path = historyForward[0];
                    }
                }
                if(path.Length > 0) {
                    string str2 = QTUtility2.MakePathDisplayText(path, false);
                    if(!string.IsNullOrEmpty(str2)) {
                        return str2;
                    }
                }
                return path;
            }

            #endregion

            #region Helpers

            private ITravelLogEntry GetCurrentLogEntry() {
                IEnumTravelLogEntry ppenum = null;
                ITravelLogEntry rgElt = null;
                ITravelLogEntry entry3;
                try {
                    if(_owner.TravelLog.EnumEntries(1, out ppenum) == 0) {
                        ppenum.Next(1, out rgElt, 0);
                    }
                    entry3 = rgElt;
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                    entry3 = null;
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                }
                return entry3;
            }

            private static string GetCommandLine()
            {
                Process cprocess = Process.GetCurrentProcess();

                int currentProcessId2 = cprocess.Id;

                int currentProcessId = (int)PInvoke.GetCurrentProcessId();
                var process = Process.GetProcessById( currentProcessId );
                QTLogger.log(" process command line 0 : " + cprocess.StartInfo.Arguments);
                QTLogger.log(" process command line 1 : " + process.StartInfo.Arguments);


                string result = null;
                try
                {
                    var cpid = currentProcessId;
                    if (currentProcessId2 != currentProcessId)
                    {
                        cpid = currentProcessId2;
                    }
                    string wmiQuery = string.Format("select CommandLine from Win32_Process where ProcessID ={0}", cpid);
                    using(ManagementObjectSearcher managementObjectSearcher =
                        new ManagementObjectSearcher(wmiQuery)) {
                        ManagementObjectCollection managementObjectCollection = managementObjectSearcher.Get();

                        foreach(ManagementObject managementObject in managementObjectCollection.Cast<ManagementObject>()) {
                            result = managementObject["CommandLine"] == null ? "" : managementObject["CommandLine"].ToString();
                        }
                    }
                    QTLogger.log(" process command line 3 : " + result);
                }
                catch (Exception ex)
                {
                    result = "";
                }
                string str = Marshal.PtrToStringUni(PInvoke.GetCommandLine());
                QTLogger.log(" process command line 2 : " + str);
                return str;
            }

            private static string GetNameToSelectFromCommandLineArg(string str) {
                QTLogger.log("GetNameToSelectFromCommandLineArg :" + str);
                if(!string.IsNullOrEmpty(str)) {
                    int index = str.IndexOf("/select,", StringComparison.CurrentCultureIgnoreCase);
                    if(index == -1) {
                        index = str.IndexOf(",select,", StringComparison.CurrentCultureIgnoreCase);
                    }
                    if(index != -1) {
                        index += 8;
                        if(str.Length < index) {
                            return string.Empty;
                        }
                        string path = str.Substring(index).Split(new char[] { ',' })[0].Trim().Trim(new char[] { ' ', '"' });
                        try {
                            if(File.Exists(path) || Directory.Exists(path)) {
                                return Path.GetFileName(path);
                            }
                        }
                        catch {
                        }
                    }
                }
                return string.Empty;
            }

            private static bool TryParseCommandlineParams(
                string param,
                out string path,
                out string selection)
            {
                selection = (string)null;
                Match match = new Regex("( ?(/|,)select, ?((?<SELQ>\"[^\"/]+\")|(?<SEL>[^,/]+))| ?(/|,)root,\\s?((?<ROOTQ>\"[^\"/]+\")|(?<ROOT>[^,/]+)))+", RegexOptions.IgnoreCase).Match(param);
                if (match.Success) {
                    var group1 = match.Groups["SEL"];
                    var group2 = match.Groups["SELQ"];
                    var group3 = match.Groups["ROOT"];
                    var group4 = match.Groups["ROOTQ"];
                    try
                    {
                        if (group3.Success)
                        {
                            path = group3.Value;
                            return true;
                        }
                        if (group4.Success)
                        {
                            path = group4.Value.Trim('"');
                            return true;
                        }
                        if (group1.Success)
                        {
                            selection = group1.Value;
                            path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                            return true;
                        }
                        if (group2.Success)
                        {
                            selection = group2.Value.Trim('"');
                            path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                            return true;
                        }
                    }
                    catch
                    {
                    }
                }
                path = (string)null;
                return false;
            }

            #endregion
        }
    }
}
