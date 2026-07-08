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
    /**
     sealed Ӧ����ĳ����ʱ��sealed ���η�����ֹ������̳��Ը��ࡣ�������ʾ���У��� B �̳����� A����û������Լ̳����� B��
     class A {}
     sealed class B : A {}
     */
    [ComVisible(true), Guid("d2bf470e-ed1c-487f-a333-2bd8835eb6ce")]
    public partial class QTTabBarClass : TabBarBase
    {
       
        private VisualStyleRenderer bgRenderer;
        private BreadcrumbBar breadcrumbBar;
        
        
        private IContainer components;
        private MenuController _menuController;
        private TabManager _tabManager;
        private ExplorerControllerModule _explorerControllerModule;
        private ContextMenuStripEx contextMenuDropped;
        private QTabItem ContextMenuedTab;

        
        
        private Cursor curTabCloning;
        private Cursor curTabDrag;
        private Rectangle DraggingDestRect;
        private QTabItem DraggingTab;
        private DropTargetWrapper dropTargetWrapper;
        private NativeWindowController explorerController;
        
        private bool fHideExplorer;
        private bool fDrivesContainedDD;
        private static bool fInitialized;
        private readonly bool fIsFirstLoad;
        private volatile bool FirstNavigationCompleted;
        private bool fAutoNavigating;
        
        private bool fNeedsNewWindowPulse;
        private bool fNowQuitting;
        private bool fNowRestoring;
        private bool fNowTravelByTree;
        private bool fToggleTabMenu;
        private IntPtr hHook_Key;
        private IntPtr hHook_Mouse;
        private IntPtr hHook_Msg;
        private HookProc hookProc_GetMsg;
        private HookProc hookProc_Key;
        private HookProc hookProc_Mouse;
        private ShellContextMenu shellContextMenu = new ShellContextMenu();
        private int iModKeyStateDD;
        private const int INTERVAL_SELCTTAB = 5000;
        private const int INTERVAL_SHOWMENU = 0x4b0;
        private int iSequential_WM_CLOSE;
        private bool IsShown;
        private byte[] lastAttemptedBrowseObjectIDL;
        private byte[] lastCompletedBrowseObjectIDL;
        
        private static FileHashComputerForm md5Form;
        private ToolStripTextBox menuTextBoxTabAlias;


        
        private string strDraggingDrive;
        private string strDraggingStartPath;
        private SubDirTipForm subDirTip_Tab;
        public QTabControl tabControl1;
        private QTabItem tabForDD;
        private TabSwitchForm tabSwitcher;
        private Timer timerOnTab;
        private Timer timerSelectionChanged;
        
        private ToolTip toolTipForDD;
        private NativeWindowController travelBtnController;
        
        
        private TreeViewWrapper treeViewWrapper;
        /*// ���ӵ�����
        private ToolStripMenuItem tsmiAddToGroup;
        private ToolStripMenuItem tsmiBrowseFolder;
        private ToolStripMenuItem tsmiCloneThis;
        private ToolStripMenuItem tsmiClose;
        private ToolStripMenuItem tsmiCloseAllButCurrent;
        private ToolStripMenuItem tsmiCloseAllButThis;
        private ToolStripMenuItem tsmiCloseLeft;
        private ToolStripMenuItem tsmiCloseRight;
        private ToolStripMenuItem tsmiCloseWindow;
        private ToolStripMenuItem tsmiCopy;
        private ToolStripMenuItem tsmiCreateGroup;
        private ToolStripMenuItem tsmiCreateWindow;
        private ToolStripMenuItem tsmiExecuted;
        private ToolStripMenuItem tsmiGroups;
        private ToolStripMenuItem tsmiHistory;
        private ToolStripMenuItem tsmiLastActiv;
        private ToolStripMenuItem tsmiLockThis;
        private ToolStripMenuItem tsmiLockToolbar;
        private ToolStripMenuItem tsmiMergeWindows;
        private ToolStripMenuItem tsmiOption;
        private ToolStripMenuItem tsmiProp;
        private ToolStripMenuItem tsmiTabOrder;
        private ToolStripMenuItem tsmiUndoClose;

        /*add by qwop 2012.07.13#1#
        private ToolStripMenuItem tsmiOpenCmd;
        private ToolStripMenuItem enableApiHook;
        /*add by qwop 2012.07.13#1#

        private ToolStripSeparator tssep_Sys1;
        private ToolStripSeparator tssep_Sys2;
        private ToolStripSeparator tssep_Tab1;
        private ToolStripSeparator tssep_Tab2;
        private ToolStripSeparator tssep_Tab3;*/
        private readonly int WM_NEWTREECONTROL = PInvoke.RegisterWindowMessage("QTTabBar_NewTreeControl");
        private readonly int WM_BROWSEOBJECT = PInvoke.RegisterWindowMessage("QTTabBar_BrowseObject");
        private readonly int WM_HEADERINALLVIEWS = PInvoke.RegisterWindowMessage("QTTabBar_HeaderInAllViews");
        private readonly int WM_LISTREFRESHED = PInvoke.RegisterWindowMessage("QTTabBar_ListRefreshed");
        private readonly int WM_SHOWHIDEBARS = PInvoke.RegisterWindowMessage("QTTabBar_ShowHideBars");
        private readonly int WM_CHECKPULSE = PInvoke.RegisterWindowMessage("QTTabBar_CheckPulse");
        private readonly int WM_SELECTFILE = PInvoke.RegisterWindowMessage("QTTabBar_SelectFile");

        
        internal bool CanNavigateBackward { get { return ((navBtnsFlag & 1) != 0); } }
        internal bool CanNavigateForward { get { return ((navBtnsFlag & 2) != 0); } }
        internal int TabCount { get { return tabControl1.TabCount; } }

        internal int SelectedTabIndex {
            get {
                return tabControl1.TabPages.IndexOf(CurrentTab);
            }
            set {
                if(0 <= value && value < tabControl1.TabPages.Count) {
                    tabControl1.SelectTab(value);
                }
            }
        }


        #region qwop �Զ�����
        private static QTTabBarClass lstTabBar;
        public static void OpenOptionDialog()
        {
            OptionsDialog.Open();
        }

        public static QTTabBarClass GetThreadTabBar()
        {
            return InstanceManager.GetThreadTabBar(); 
        }

        /// <summary>
        /// ����һ����ǰ�̵߳� ·����ǩ
        /// </summary>
        /// <param name="address">·��</param>
        /// <param name="index">λ��</param>
        /// <param name="fLocked">�Ƿ�����</param>
        /// <param name="fSelect">�Ƿ�ѡ��</param>
        /// <returns></returns>
        public static bool CreateTab(QTTabBarClass tabBar, Address address, int index, bool fLocked, bool fSelect)
        {
            if (null == tabBar) {
                tabBar = GetThreadTabBar();    
            }
            
            if (null == tabBar)
            {
                if (lstTabBar == null)
                {
                    return false;
                }
                tabBar = lstTabBar;
            } 
            
            using (IDLWrapper wrapper = new IDLWrapper(address))
            {
                address.ITEMIDLIST = wrapper.IDL;
                address.Path = wrapper.Path;
            }
            if ((address.ITEMIDLIST == null) || (address.ITEMIDLIST.Length <= 0))
            {
                return false;
            }

            QTabItem tab = new QTabItem(QTUtility2.MakePathDisplayText(address.Path, false), address.Path, tabBar.tabControl1);
            tab.NavigatedTo(address.Path, address.ITEMIDLIST, -1, false);
            tab.ToolTipText = QTUtility2.MakePathDisplayText(address.Path, true);
            tab.TabLocked = fLocked;
            if (index < 0)
            {
                tabBar.AddInsertTab(tab);
            }
            else
            {
                if (index > tabBar.tabControl1.TabCount)
                {
                    index = tabBar.tabControl1.TabCount;
                }
                tabBar.tabControl1.TabPages.Insert(index, tab);
            }
            if (fSelect)
            {
                tabBar.tabControl1.SelectTab(tab);
            }
            return true;
        }
        #endregion

        public QTTabBarClass() {
            // Trigger QTUtility's static constructor as the single driver of the
            // business initialization sequence. QTUtility.Initialize() is an empty
            // method whose only purpose is to fire that static ctor, which in turn
            // calls InitializationOrchestrator.Initialize() exactly once. Calling the
            // orchestrator directly here would let its mid-sequence QTUtility.* access
            // trigger the static ctor and re-enter the orchestrator (Monitor is
            // reentrant), running the non-idempotent steps twice.
            QTUtility.Initialize();
            // QTUtility2.AllocDebugConsole();
            // Application.SetCompatibleTextRenderingDefault(false);
            // Application.DoEvents();
            /*try
            {
                ConfigurationManager.AppSettings.Set("EnableWindowsFormsHighDpiAutoResizing", "true");
            }
            catch (Exception) { /* Ignora l'eccezione #1# }*/
            try {
                string installDateString;
                DateTime installDate;
                string minDate = DateTime.MinValue.ToString();
                using(RegistryKey key = Registry.LocalMachine.OpenSubKey(RegConst.Root)) {
                    installDateString = key == null ? minDate : (string)key.GetValue("InstallDate", minDate);
                    // ʱ���ʽ������ ���ܻᵼ�³�ʼ��ʧ��
                    if (QTUtility.IsSimpleDateStr(installDateString))  // �����ж������Ƿ�����ȷ��ʽ
                    {
                        try
                        {
                            QTUtility2.log("installDateString " + installDateString);
                            installDate = DateTime.Parse(installDateString);
                        }
                        catch (Exception e)
                        {
                            installDate = DateTime.ParseExact(installDateString, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture);
                            // ignore exception 
                        }

                        using (RegistryKey key2 = Registry.CurrentUser.CreateSubKey(RegConst.Root))
                        {
                            DateTime lastActivation;
                            // DateTime lastActivation = DateTime.Parse((string)key.GetValue("ActivationDate", minDate));
                            var value = (string)key2.GetValue("ActivationDate", minDate);
                            try
                            {
                                QTUtility2.log("ActivationDate " + value);
                                lastActivation = DateTime.Parse(value);
                            }
                            catch (Exception e)
                            {
                                lastActivation = DateTime.ParseExact(value, "yyyy/MM/dd HH:mm:ss", CultureInfo.CurrentCulture);
                                // ignore exception 
                            }

                            fIsFirstLoad = installDate.CompareTo(lastActivation) > 0;
                            // ʱ���ʽ������ ���ܻᵼ�³�ʼ��ʧ��
                            if (fIsFirstLoad)
                                key.SetValue("ActivationDate", installDateString);
                        }
                    } 
                    /*else if (QTUtility.IsShortDateStr(installDateString))
                    {

                    }*/
                }
                
            }
            catch (Exception e ){
                QTUtility2.MakeErrorLog(e, "QTTabBarClass ���캯����ʼ����װʱ��");
            }
            if(!fInitialized) {
                InitializeStaticFields();
            }
            // ��ʼ���߶�
            BandHeight = Config.Skin.TabHeight + BandHeightSpace;
            // BandHeight = Config.Skin.TabHeight + 10;
            InitializeComponent();
            lstActivatedTabs.Add(CurrentTab);

            // reocrd the last qttabbarclass instance, add by indiff .
            lstTabBar = this;
            
            // Ĭ�ϻ�ȡ�Ƿ�������־
            QTUtility2.ENABLE_LOGGER = Config.Misc.EnableLog;
        }

        private void AddInsertTab(QTabItem tab) {
            _tabManager.AddInsertTab(tab);
        }
        private void AddStartUpTabs(string openingGRP, string openingPath) {
            _tabManager.AddStartUpTabs(openingGRP, openingPath);
        }
       

        /**
         * �����ק����ļ��������������Ӧ�ó���˵��ĵ����˵�����
         */
        private void AppendUserApps(IList<string> listDroppedPaths) {
            WindowUtils.BringExplorerToFront(ExplorerHandle);
            if(contextMenuDropped == null) {
                ToolStripMenuItem tsmiDropped = new ToolStripMenuItem { Tag = 1 };
                contextMenuDropped = new ContextMenuStripEx(components, false);
                contextMenuDropped.SuspendLayout();
                contextMenuDropped.Items.Add(tsmiDropped);
                contextMenuDropped.Items.Add(new ToolStripMenuItem());
                contextMenuDropped.ItemClicked += (sender, e) => {
                    if(e.ClickedItem.Tag != null)
                        AppsManager.CreateNewApp((List<string>)contextMenuDropped.Tag);
                };
                contextMenuDropped.ResumeLayout(false);
            }

            string strMenu = QTUtility.ResMain[21];
            strMenu += listDroppedPaths.Count > 1
                    ? listDroppedPaths.Count + QTUtility.ResMain[22] // "items"  ������Ӧ�ó���˵�
                    : Path.GetFileName(listDroppedPaths[0]).Enquote();

            contextMenuDropped.SuspendLayout();
            contextMenuDropped.Items[0].Text = strMenu;
            contextMenuDropped.Items[1].Text = QTUtility.ResMain[23];			// Cancel
            contextMenuDropped.Tag = listDroppedPaths;
            contextMenuDropped.ResumeLayout();
            contextMenuDropped.Show(MousePosition);
        }

        // Async callback for FolderTree thread completion
        private void AsyncComplete_FolderTree(IAsyncResult ar) {
            AsyncResult result = (AsyncResult)ar;
            ((WaitTimeoutCallback)result.AsyncDelegate).EndInvoke(ar);
            if(IsHandleCreated) {
                Invoke(new FormMethodInvoker(CallbackFolderTree), new object[] { result.AsyncState });
            }
        }

        // BeforeNavigate moved to ExplorerControllerModule (Batch 13)

        private void CallbackFolderTree(object obj) {
            bool fShow = (bool)obj;
            ShowFolderTree(fShow);
            if(fShow) {
                PInvoke.SetRedraw(ExplorerHandle, true);
                PInvoke.RedrawWindow(ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
            }
        }

        private IntPtr CallbackGetMsgProc(int nCode, IntPtr wParam, IntPtr lParam) {
            if(nCode >= 0) {
                MSG msg = (MSG)Marshal.PtrToStructure(lParam, typeof(MSG));
                // QTUtility2.debugMessage(msg);
                try {
                    if(QTUtility.IsXP) {
                        if(msg.message == WM.CLOSE) {
                            if(iSequential_WM_CLOSE > 0) {
                                Marshal.StructureToPtr(new MSG(), lParam, false);
                                return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                            }
                            iSequential_WM_CLOSE++;
                        }
                        else {
                            iSequential_WM_CLOSE = 0;
                        }
                    }

                    if(msg.message == WM_NEWTREECONTROL)
                    {
                        QTUtility2.log("CallbackGetMsgProc WM_NEWTREECONTROL");
                        object obj = Marshal.GetObjectForIUnknown(msg.wParam);
                        try {
                            if(obj != null) {
                                IOleWindow window = obj as IOleWindow;
                                if(window != null) {
                                    IntPtr hwnd;
                                    window.GetWindow(out hwnd);
                                    if(hwnd != IntPtr.Zero && PInvoke.IsChild(ExplorerHandle, hwnd)) {
                                        hwnd = WindowUtils.FindChildWindow(hwnd,
                                                child => PInvoke.GetClassName(child) == "SysTreeView32");
                                        if(hwnd != IntPtr.Zero) {
                                            INameSpaceTreeControl control = obj as INameSpaceTreeControl;
                                            if(control != null) {
                                                if(treeViewWrapper != null) {
                                                    treeViewWrapper.Dispose();
                                                }
                                                treeViewWrapper = new TreeViewWrapper(hwnd, control);
                                                treeViewWrapper.TreeViewClicked += FolderLinkClicked;
                                                QTUtility2.log("CallbackGetMsgProc regedit TreeViewClicked");
                                                obj = null; // Release the object only if we didn't get this far.
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        finally {
                            if(obj != null) {
                                QTUtility2.log("ReleaseComObject obj");
                                Marshal.ReleaseComObject(obj);
                            }
                        }
                        return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);   
                    }
                    else if(msg.message == WM_LISTREFRESHED) {
                        HandleF5();
                        return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                    }
                    else if (msg.message == WM_SELECTFILE)
                    {
                        QTUtility2.log(" select file 1 " + " wparam " + wParam + " lparam " + lParam );
                        // lParam: point to an array of ITEMIDLISTs;
                        /*IShellView shellView = null;
                        if (0 == ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView))
                        {
                            // new Guid("{0000010e-0000-0000-C000-000000000046}")
                            var iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                            object ppv;
                            shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                            if (ppv != null)
                            {
                                System.Runtime.InteropServices.ComTypes.IDataObject pDataObject = (System.Runtime.InteropServices.ComTypes.IDataObject)ppv;
                                // var iid = new Guid(ShellIIDGuid.IShellItemArray);
                                var shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                                if (shellObjectCollection.Count>0)
                                {
                                    foreach (ShellObject so in shellObjectCollection)
                                    {
                                        QTUtility2.log("so.Name " + so.Name);
                                    }
                                }
                            }
                        }*/
                        // IntPtr pIDL = Marshal.ReadIntPtr(lParam);
                        // SFVCB_SELECTINFO sel = (SFVCB_SELECTINFO)Marshal.PtrToStructure(lParam, typeof(SFVCB_SELECTINFO));
                        // QTUtility2.log("SFVCB_SELECTINFO UOldState " + sel.UOldState);
                        // QTUtility2.log("SFVCB_SELECTINFO UNewState " + sel.UNewState);
                        // QTUtility2.log("SFVCB_SELECTINFO Pid1 " + sel.Pid1);
                        return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
                    }

                    switch(msg.message) {
                        /* TODO: Handle FolderView clicks on XP.
                        case WM.LBUTTONDOWN:
                        case WM.LBUTTONUP:
                            if((QTUtility.IsXP && !Config.NoMidClickTree) && ((((int)((long)msg.wParam)) & 4) != 0)) {
                                HandleLBUTTON_Tree(msg, msg.message == 0x201);
                            }
                            break;

                        
                        */
                        case WM.MBUTTONUP:
                            if (!Explorer.Busy) // && !Config.NoMidClickTree
                            {
                                QTUtility2.log("CallbackGetMsgProc MBUTTONUP NoMidClickTree");
                                /*
                                object obj = Marshal.GetObjectForIUnknown(msg.wParam);
                                try {
                                    if(obj != null) {
                                        IOleWindow window = obj as IOleWindow;
                                        if(window != null) {
                                            IntPtr hwnd;
                                            window.GetWindow(out hwnd);
                                            if(hwnd != IntPtr.Zero && PInvoke.IsChild(ExplorerHandle, hwnd)) {
                                                hwnd = WindowUtils.FindChildWindow(hwnd,
                                                        child => PInvoke.GetClassName(child) == "SysTreeView32");
                                                if(hwnd != IntPtr.Zero) {
                                                    INameSpaceTreeControl control = obj as INameSpaceTreeControl;
                                                    if(control != null) {
                                                        if(treeViewWrapper != null) {
                                                            treeViewWrapper.Dispose();
                                                        }
                                                        treeViewWrapper = new TreeViewWrapper(hwnd, control);
                                                        treeViewWrapper.TreeViewClicked += FolderLinkClicked;
                                                        QTUtility2.log("CallbackGetMsgProc regedit TreeViewClicked");
                                                        obj = null; // Release the object only if we didn't get this far.
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                                finally {
                                    if(obj != null) {
                                        Marshal.ReleaseComObject(obj);
                                    }
                                }
                                */
                                Handle_MButtonUp_Tree(msg);
                            }
                            break;
                        case WM.SYSCOLORCHANGE:
                            QTUtility.InNightMode = QTUtility.getNightMode();
                            QTUtility2.log("SYSCOLORCHANGE SwitchNighMode");
                            Config.Skin.SwitchNighMode(QTUtility.InNightMode ); // ����ر��Զ���ɫ�򲻽��б�ɫ
                            ConfigManager.UpdateConfig(true);
                            this.tabControl1.InitializeColors();
                            PInvoke.SetRedraw(ExplorerHandle, true);
                            PInvoke.RedrawWindow(ExplorerHandle, IntPtr.Zero, IntPtr.Zero, 0x289);
                            break;

                        case WM.CLOSE:  // �رմ���
                            if(QTUtility.IsXP) {
                                if((msg.hwnd == ExplorerHandle) && HandleCLOSE(msg.lParam)) {
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                                break;
                            }

                            
                            using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root))
                            {
                                string[] list = (from QTabItem item2 in tabControl1.TabPages
                                                 where item2.TabLocked
                                                 select item2.CurrentPath).ToArray();
                               // MessageBox.Show(String.Join(",", list));
                                QTUtility2.WriteRegBinary(list, "TabsLocked", key);
                            }
                            if(msg.hwnd == WindowUtils.GetShellTabWindowClass(ExplorerHandle)) { // �����ǩ�� handle ����Դ��������ƥ��
                                try {
                                    bool flag = tabControl1.TabCount == 1;
                                    string currentPath = tabControl1.SelectedTab.CurrentPath;
                                    if(!Directory.Exists(currentPath) && // �����ǰ·��Ŀ¼������
                                       currentPath.Length > 3 
                                       /* && currentPath.Substring(1, 2) == @":\" */ ) {
                                        if(flag) {
                                            WindowUtils.CloseExplorer(ExplorerHandle, 2);
                                        }
                                        else {
                                            CloseTab(tabControl1.SelectedTab, true);
                                        }
                                    }
                                }
                                catch(Exception e ) {
                                    QTUtility2.MakeErrorLog(e, "CallbackGetMsgProc WM.Close");
                                }
                                Marshal.StructureToPtr(new MSG(), lParam, false);
                            }
                            break;

                        case WM.COMMAND:
                            if(QTUtility.IsXP) {
                                int num = ((int)((long)msg.wParam)) & 0xffff;
                                if(num == 0xa021) {
                                    WindowUtils.CloseExplorer(ExplorerHandle, 3);
                                    Marshal.StructureToPtr(new MSG(), lParam, false);
                                }
                            }
                            break;

                        /*case 48648:  // not work
                            QTUtility2.log("48648");
                            break;*/
                    }
                }
                catch(Exception ex) {
                    QTUtility2.MakeErrorLog(ex, String.Format("Message: {0:x4}", msg.message));
                }
            }
            return PInvoke.CallNextHookEx(hHook_Msg, nCode, wParam, lParam);
        }


       

        /**
         * ע���ݼ��Ļص�
         */
        private IntPtr CallbackKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam) {
            const uint KB_TRANSITION_FLAG = 0x80000000;
            const uint KB_PREVIOUS_STATE_FLAG = 0x40000000;
            if(nCode < 0 || NowModalDialogShown) {
                return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
            }

            try {
                uint flags = (uint)((long)lParam);
                bool isKeyPress = (flags & KB_TRANSITION_FLAG) == 0;
                bool isRepeat = (flags & KB_PREVIOUS_STATE_FLAG) != 0;
                Keys key = (Keys)((int)wParam);

                if(key == Keys.ShiftKey) {
                    if(isKeyPress || !isRepeat) {
                        listView.HandleShiftKey();
                    }
                }

                if(isKeyPress) {
                    if(HandleKEYDOWN(key, isRepeat)) {
                        return new IntPtr(1);
                    }
                }
                else {
                    listView.HideThumbnailTooltip(3);
                    if(NowTabDragging && DraggingTab != null) {
                        Cursor = Cursors.Default;
                    }

                    switch(key) {
                        case Keys.ControlKey:
                            if(Config.Keys.UseTabSwitcher) {
                                HideTabSwitcher(true);
                            }
                            break;

                        case Keys.Menu: // Alt key
                            if(Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                                tabControl1.ShowCloseButton(false);
                            }
                            break;

                        case Keys.Tab:
                            if(Config.Keys.UseTabSwitcher && tabSwitcher != null && tabSwitcher.IsShown) {
                                tabControl1.SetPseudoHotIndex(tabSwitcher.SelectedIndex);
                            }
                            break;
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex,
                        String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
            }
            return PInvoke.CallNextHookEx(hHook_Key, nCode, wParam, lParam);
        }

        private IntPtr CallbackMouseProc(int nCode, IntPtr wParam, IntPtr lParam) {
            try {
                if(nCode >= 0 && !NowModalDialogShown) {
                    IntPtr ptr = (IntPtr)1;
                    switch(((int)wParam)) {
                        case WM.MOUSEWHEEL:
                            if(!HandleMOUSEWHEEL(lParam)) {
                                break;
                            }
                            return ptr;

                        /*case WM.MBUTTONDOWN: // add by indiff
                            QTUtility2.log("CallbackMouseProc Handle_MButtonUp_Tree");
                            if (Handle_MButtonUp_Tree(ExplorerHandle,lParam))
                            {
                                break;
                            }
                            return ptr;*/

                        case WM.XBUTTONDOWN:
                             
                        case WM.XBUTTONUP:
                            MouseButtons mouseButtons = MouseButtons;
                            Keys modifierKeys = ModifierKeys;
                            MouseChord chord = mouseButtons == MouseButtons.XButton1
                                    ? MouseChord.X1
                                    : mouseButtons == MouseButtons.XButton2 ? MouseChord.X2 : MouseChord.None;
                            if(chord == MouseChord.None) break;
                            chord = QTUtility.MakeMouseChord(chord, modifierKeys);
                            BindAction action;
                            if(!Config.Mouse.GlobalMouseActions.TryGetValue(chord, out action)) {
                                break;
                            }
                            if(((int)wParam) == WM.XBUTTONUP && !Explorer.Busy) {
                                QTUtility2.log("QTTabBarClass WM.XBUTTONUP " + action);
                                DoBindAction(action);
                            }
                            return ptr;
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, String.Format("LParam: {0:x4}, WParam: {1:x4}", (long)lParam, (long)wParam));
            }
            return PInvoke.CallNextHookEx(hHook_Mouse, nCode, wParam, lParam);
        }

        private void CallbackMultiPath(object obj) {
            object[] objArray = (object[])obj;
            string[] collection = (string[])objArray[0];
            int num = (int)objArray[1];
            switch(num) {
                case 0:
                    foreach(string str in collection) {
                        OpenNewTab(str, true);
                    }
                    break;

                case 1: {
                        bool flag = true;
                        foreach(string str2 in collection) {
                            OpenNewTab(str2, !flag);
                            flag = false;
                        }
                        break;
                    }
                default:
                    StaticReg.CreateWindowPaths.Assign(collection);
                    using(IDLWrapper wrapper = new IDLWrapper(collection[0])) {
                        OpenNewWindow(wrapper);
                    }
                    break;
            }
            if(num == 1) {
                InstanceManager.RemoveFromTrayIcon(Handle);
                WindowUtils.BringExplorerToFront(ExplorerHandle);
            }
        }

        // CancelFailedNavigation moved to ExplorerControllerModule (Batch 13)

        

        private void ChangeViewMode(bool fUp) {
            FVM orig = ShellBrowser.ViewMode;
            FVM mode = orig;
            switch(mode) {
                case FVM.ICON:
                    mode = fUp ? FVM.TILE : FVM.LIST;
                    break;

                case FVM.LIST:
                    mode = fUp ? FVM.ICON : FVM.DETAILS;
                    break;

                case FVM.DETAILS:
                    if(fUp) {
                        mode = FVM.LIST;
                    }
                    break;

                case FVM.THUMBNAIL:
                    mode = fUp ? FVM.THUMBSTRIP : FVM.TILE;
                    break;

                case FVM.TILE:
                    mode = fUp ? FVM.THUMBNAIL : FVM.ICON;
                    break;

                case FVM.THUMBSTRIP:
                    if(!fUp) {
                        mode = FVM.THUMBNAIL;
                    }
                    break;
            }
            if(mode != orig) {
                ShellBrowser.ViewMode = mode;
            }
        }

        private static bool CheckProcessID(IntPtr hwnd1, IntPtr hwnd2) {
            uint num;
            uint num2;
            PInvoke.GetWindowThreadProcessId(hwnd1, out num);
            PInvoke.GetWindowThreadProcessId(hwnd2, out num2);
            return ((num == num2) && (num != 0));
        }

        private void ChooseNewDirectory() {
            _tabManager.ChooseNewDirectory();
        }
        // ClearTravelLogs moved to ExplorerControllerModule (Batch 13)

        internal void CloneCurrentTab(bool fSelect = true) {
            _tabManager.CloneCurrentTab(fSelect);
        }
        private void CloneTabButton(QTabItem tab, LogData log) {
            _tabManager.CloneTabButton(tab, log);
        }
        private QTabItem CloneTabButton(QTabItem tab, string optionURL, bool fSelect, int index) {
            return _tabManager.CloneTabButton(tab, optionURL, fSelect, index);
        }
        private List<string> CloseAllTabsExcept(QTabItem leaveThisOne, bool leaveLocked = true) {
            return _tabManager.CloseAllTabsExcept(leaveThisOne, leaveLocked);
        }
        /**
         *�����رմ����¼� by indiff
         */
        public override void CloseDW(uint dwReserved) {
            try {
                /*string[] list1 = (from ITab tab in pluginServer.GetTabs()
                                 where tab.Locked
                                 select tab.Address.Path).ToArray();
                MessageBox.Show(String.Join(",", list1));
               

                MessageBox.Show("�رմ���:" + tabControl1.TabPages.Count );
                string[] list = (from QTabItem item2 in tabControl1.TabPages
                                 where item2.TabLocked
                                 select item2.CurrentPath).ToArray();
                MessageBox.Show(String.Join(",", list));
 */
                string[] list = (from QTabItem item2 in tabControl1.TabPages
                                 where item2.TabLocked
                                 select item2.CurrentPath).ToArray();
                if(treeViewWrapper != null) {
                    treeViewWrapper.Dispose();
                    treeViewWrapper = null;
                }
                if(listViewManager != null) {
                    listViewManager.Dispose();
                    listViewManager = null;
                }
                if(subDirTip_Tab != null) {
                    subDirTip_Tab.Dispose();
                    subDirTip_Tab = null;
                }
                if(IsShown) {
                    if(pluginServer != null) {
                        pluginServer.Dispose();
                        pluginServer = null;
                    }
                    if(hHook_Key != IntPtr.Zero) {
                        PInvoke.UnhookWindowsHookEx(hHook_Key);
                        hHook_Key = IntPtr.Zero;
                    }
                    if(hHook_Mouse != IntPtr.Zero) {
                        PInvoke.UnhookWindowsHookEx(hHook_Mouse);
                        hHook_Mouse = IntPtr.Zero;
                    }
                    if(hHook_Msg != IntPtr.Zero) {
                        PInvoke.UnhookWindowsHookEx(hHook_Msg);
                        hHook_Msg = IntPtr.Zero;
                    }
                    if(explorerController != null) {
                        explorerController.ReleaseHandle();
                        explorerController = null;
                    }
                    if(rebarController != null) {
                        rebarController.Dispose();
                        rebarController = null;
                    }
                    if(!QTUtility.IsXP && (travelBtnController != null)) {
                        travelBtnController.ReleaseHandle();
                        travelBtnController = null;
                    }

                    // �Ƴ����½�ͼ��
                    if (null != Handle)
                    {
                        InstanceManager.RemoveFromTrayIcon(Handle);
                    }

                    // TODO: check this
                    using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                        if(Config.Misc.KeepHistory) {
                            foreach(QTabItem item in tabControl1.TabPages) {
                                // ������� �ڴ������ھ��֮ǰ�������ڿؼ��ϵ��� Invoke �� BeginInvoke��
                                AddToHistory(item);
                            }
                            QTUtility.SaveRecentlyClosed(key);
                        }
                        if(Config.Misc.KeepRecentFiles) {
                            QTUtility.SaveRecentFiles(key);
                        }

                        /*foreach (QTabItem item in tabControl1.TabPages)
                        {
                            if ( item.TabLocked ) {
                                MessageBox.Show(item.CurrentPath);
                            }
                        }*/

                        // �ر���Ϣȥ��д��������ǩ�ĵ���
                        if (list != null && list.Length > 0)
                        {
                            QTUtility2.WriteRegBinary(list, "TabsLocked", key);
                        }

                        InstanceManager.UnregisterTabBar();
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000))) {
                            QTUtility.WindowAlpha = 0xff;
                        }
                        else {
                            byte num;
                            int num2;
                            int num3;
                            if(PInvoke.GetLayeredWindowAttributes(ExplorerHandle, out num2, out num, out num3)) {
                                QTUtility.WindowAlpha = num;
                            }
                            else {
                                QTUtility.WindowAlpha = 0xff;
                            }
                        }
                        key.SetValue("WindowAlpha", QTUtility.WindowAlpha);
                        IDLWrapper.SaveCache(key);
                    }
                    if((md5Form != null) && !md5Form.InvokeRequired) {
                        md5Form.SaveMD5FormStat();
                        md5Form.Dispose();
                        md5Form = null;
                    }
                    Cursor = Cursors.Default;
                    if((curTabDrag != null) && (curTabDrag != Cursors.Default)) {
                        PInvoke.DestroyIcon(curTabDrag.Handle);
                        GC.SuppressFinalize(curTabDrag);
                        curTabDrag = null;
                    }
                    if((curTabCloning != null) && (curTabCloning != Cursors.Default)) {
                        PInvoke.DestroyIcon(curTabCloning.Handle);
                        GC.SuppressFinalize(curTabCloning);
                        curTabCloning = null;
                    }
                    if(dropTargetWrapper != null) {
                        dropTargetWrapper.Dispose();
                        dropTargetWrapper = null;
                    }
                    OptionsDialog.ForceClose();
                    if(tabSwitcher != null) {
                        tabSwitcher.Dispose();
                        tabSwitcher = null;
                    }
                }
                if(TravelLog != null) {
                    QTUtility2.log("ReleaseComObject TravelLog");
                    Marshal.FinalReleaseComObject(TravelLog);
                    TravelLog = null;
                }
                if(shellContextMenu != null) {
                    shellContextMenu.Dispose();
                    shellContextMenu = null;
                }
                if(ShellBrowser != null) {
                    ShellBrowser.Dispose();
                    ShellBrowser = null;
                }
                foreach(ITravelLogEntry entry in LogEntryDic.Values) {
                    if(entry != null) {
                        QTUtility2.log("ReleaseComObject entry");
                        Marshal.FinalReleaseComObject(entry);
                    }
                }
                LogEntryDic.Clear();
                fFinalRelease = true;
                base.CloseDW(dwReserved);
            }
            catch(Exception exception2) {
                QTUtility2.MakeErrorLog(exception2, "tabbar closing");
            }
        }

        private void CloseLeftRight(bool fLeft, int index) {
            _tabManager.CloseLeftRight(fLeft, index);
        }


      

        


        /**
         * ������������ API Hook
         */
        private void EnableApiHook()
        {
            // Create and enable the API hooks.
            // Startup one-time hook initialization is handled idempotently by
            // InitializationOrchestrator.Initialize(). This method, however, is the
            // user-triggered context-menu entry ("Enable API Hook"), so it must keep
            // its own independent call to re-enable/reload the API hooks on demand.
            HookLibManager.Initialize();
            QTUtility2.log("QTUtility ������������ API hooks");
        }


        private void Controls_GotFocus(object sender, EventArgs e) {
            OnGotFocus(e);
        }

        internal List<ToolStripItem> CreateBranchMenu(bool fCurrent, IContainer container, ToolStripItemClickedEventHandler itemClickedEvent) {
            QTabItem item = fCurrent ? CurrentTab : ContextMenuedTab;
            List<ToolStripItem> list = new List<ToolStripItem>();
            List<LogData> branches = item.Branches;
            if(branches.Count > 0) {
                ToolStripMenuItem item2 = new ToolStripMenuItem(QTUtility.ResMain[0x18]);
                item2.Tag = item;
                item2.DropDown = new DropDownMenuBase(container, true, true);
                item2.DropDown.ImageList = QTUtility.ImageListGlobal;
                item2.DropDownItemClicked += itemClickedEvent;
                int index = -1;
                foreach(LogData data in branches) {
                    index++;
                    if(IsSpecialFolderNeedsToTravel(data.Path)) {
                        if(LogEntryDic.ContainsKey(data.Hash)) {
                            goto Label_00B3;
                        }
                        continue;
                    }
                    if(!QTUtility2.PathExists(data.Path)) {
                        continue;
                    }
                Label_00B3:
                    item2.DropDownItems.Add(MenuUtility.CreateMenuItem(new MenuItemArguments(data.Path, false, index, MenuGenre.Branch)));
                }
                if(item2.DropDownItems.Count > 0) {
                    list.Add(new ToolStripSeparator());
                    list.Add(item2);
                }
            }
            return list;
        }

        private static Cursor CreateCursor(Bitmap bmpColor) {
            Cursor cursor;
            using(bmpColor) {
                using(Bitmap bitmap = new Bitmap(0x20, 0x20)) {
                    ICONINFO piconinfo = new ICONINFO();
                    piconinfo.fIcon = false;
                    piconinfo.hbmColor = bmpColor.GetHbitmap();
                    piconinfo.hbmMask = bitmap.GetHbitmap();
                    try {
                        cursor = new Cursor(PInvoke.CreateIconIndirect(ref piconinfo));
                    }
                    catch {
                        cursor = Cursors.Default;
                    }
                }
            }
            return cursor;
        }



        // ���ӵ���ǩ�鹦��
        private void Add2Group(QTabItem contextMenuedTab) {
            _tabManager.Add2Group(contextMenuedTab);
        }
        internal List<QMenuItem> CreateNavBtnMenuItems(bool fCurrent) {
            QTabItem item = fCurrent ? CurrentTab : ContextMenuedTab;
            List<QMenuItem> list = new List<QMenuItem>();
            string[] historyBack = item.GetHistoryBack();
            string[] historyForward = item.GetHistoryForward();
            if((historyBack.Length + historyForward.Length) > 1) {
                for(int i = historyBack.Length - 1; i >= 0; i--) {
                    QMenuItem item2 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyBack[i], true, i, MenuGenre.Navigation));
                    if(IsSpecialFolderNeedsToTravel(historyBack[i])) {
                        item2.Enabled = LogEntryDic.ContainsKey(item.GetLogHash(true, i));
                    }
                    else if(!QTUtility2.PathExists(historyBack[i])) {
                        item2.Enabled = false;
                    }
                    if(item2.Enabled && (i == 0)) {
                        item2.BackColor = QTUtility2.MakeModColor(SystemColors.Highlight);
                    }
                    list.Add(item2);
                }
                for(int j = 0; j < historyForward.Length; j++) {
                    QMenuItem item3 = MenuUtility.CreateMenuItem(new MenuItemArguments(historyForward[j], false, j, MenuGenre.Navigation));
                    if(IsSpecialFolderNeedsToTravel(historyForward[j])) {
                        item3.Enabled = LogEntryDic.ContainsKey(item.GetLogHash(false, j));
                    }
                    else if(!QTUtility2.PathExists(historyForward[j])) {
                        item3.Enabled = false;
                    }
                    list.Add(item3);
                }
            }
            return list;
        }
        
        // �����µ�tabҳ
        private QTabItem CreateNewTab(IDLWrapper idlw) {
            return _tabManager.CreateNewTab(idlw);
        }
        // ���� tab ͼƬ
        internal static Bitmap[] CreateTabImage() {
            if(File.Exists(Config.Skin.TabImageFile)) {
                try {
                    Bitmap[] bitmapArray = new Bitmap[3];
                    using(Bitmap bitmap = new Bitmap(Config.Skin.TabImageFile)) {
                        int height = bitmap.Height / 3;
                        bitmapArray[0] = bitmap.Clone(new Rectangle(0, 0, bitmap.Width, height), PixelFormat.Format32bppArgb);
                        bitmapArray[1] = bitmap.Clone(new Rectangle(0, height, bitmap.Width, height), PixelFormat.Format32bppArgb);
                        bitmapArray[2] = bitmap.Clone(new Rectangle(0, height * 2, bitmap.Width, height), PixelFormat.Format32bppArgb);
                    }
                    if(Path.GetExtension(Config.Skin.TabImageFile).PathEquals(".bmp")) {
                        bitmapArray[0].MakeTransparent(Color.Magenta);
                        bitmapArray[1].MakeTransparent(Color.Magenta);
                        bitmapArray[2].MakeTransparent(Color.Magenta);
                    }
                    return bitmapArray;
                }
                catch {
                }
            }
            return null;
        }

        // todo: handle links
        private static IEnumerable<string> CreateTMPPathsToOpenNew(Address[] addresses, string pathExclude) {
            List<string> list = new List<string>();
            QTUtility2.InitializeTemporaryPaths();
            for(int i = 0; i < addresses.Length; i++) {
                try {
                    using(IDLWrapper wrapper = new IDLWrapper(addresses[i].ITEMIDLIST)) {
                        if(wrapper.Available && wrapper.HasPath) {
                            string path = wrapper.Path;
                            if(path.Length > 0 && !path.PathEquals(pathExclude) && 
                                    !QTUtility2.IsShellPathButNotFileSystem(path) && 
                                    wrapper.IsFolder && !wrapper.IsLinkToDeadFolder) {
                                list.Add(path);
                            }
                        }
                    }
                }
                catch {
                }
            }
            return list;
        }

        private void ddmrUndoClose_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(clickedItem != null) {
                using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                    e.HRESULT = shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
                }
                if(e.HRESULT == 0xffff) {
                    StaticReg.ClosedTabHistoryList.Remove(clickedItem.Path);
                    e.ClickedItem.Dispose();
                }
            }
        }

        private void ddrmrGroups_ItemMiddleClicked(object sender, ItemRightClickedEventArgs e) {
            ReplaceByGroup(e.ClickedItem.Text);
        }

        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool DoBindAction(BindAction action, bool fRepeat = false, QTabItem tab = null, IDLWrapper item = null) {
            if(fRepeat && !(
                    action == BindAction.GoBack ||
                    action == BindAction.GoForward ||
                    action == BindAction.TransparencyPlus ||
                    action == BindAction.TransparencyMinus)) {
                return false;
            }

            if(tab == null) tab = CurrentTab;

            // IntPtr ptr;
            switch(action) {
                case BindAction.GoBack:
                    NavigateCurrentTab(true);
                    break;

                case BindAction.GoForward:
                    NavigateCurrentTab(false);
                    break;

                case BindAction.GoFirst:
                    NavigateToFirstOrLast(true);
                    break;

                case BindAction.GoLast:
                    NavigateToFirstOrLast(false);
                    break;

                case BindAction.NextTab:
                    if(tabControl1.SelectedIndex == tabControl1.TabCount - 1) {
                        tabControl1.SelectedIndex = 0;
                    }
                    else {
                        tabControl1.SelectedIndex++;
                    }
                    break;

                case BindAction.PreviousTab:
                    if(tabControl1.SelectedIndex == 0) {
                        tabControl1.SelectedIndex = tabControl1.TabCount - 1;
                    }
                    else {
                        tabControl1.SelectedIndex--;
                    }
                    break;

                case BindAction.FirstTab:
                    tabControl1.SelectedIndex = 0;
                    break;

                case BindAction.LastTab:
                    tabControl1.SelectedIndex = tabControl1.TabCount - 1;
                    break;

                case BindAction.CloseCurrent:
                case BindAction.CloseTab:
                    NowTabDragging = false;
                    if(!tab.TabLocked) {
                        if(tabControl1.TabCount > 1) {
                            CloseTab(tab);
                        }
                        else {
                            WindowUtils.CloseExplorer(ExplorerHandle, 1);
                        }
                    }
                    break;

                case BindAction.CloseAllButCurrent: // �ر�����
                case BindAction.CloseAllButThis:
                    CloseAllTabsExcept(tab);
                    break;

                case BindAction.CloseLeft: // �����
                case BindAction.CloseLeftTab:
                    CloseLeftRight(true, tab.Index);
                    break;

                case BindAction.CloseRight: // �ر��ұ�
                case BindAction.CloseRightTab:
                    CloseLeftRight(false, tab.Index);
                    break;

                case BindAction.CloseWindow: // �رմ��� indiff
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root))
                    {
                        string[] list = (from QTabItem item2 in tabControl1.TabPages
                                         where item2.TabLocked
                                         select item2.CurrentPath).ToArray();

                        //MessageBox.Show(String.Join(",", list));
                        QTUtility2.WriteRegBinary(list, "TabsLocked", key);
                    }
                    WindowUtils.CloseExplorer(ExplorerHandle, 1);
                    break;

                case BindAction.RestoreLastClosed:
                    RestoreLastClosed();
                    break;

                case BindAction.CloneCurrent: // ���Ƶ�ǰ
                case BindAction.CloneTab:
                    CloneTabButton(tab, null, true, -1);
                    break;

                case BindAction.TearOffCurrent: //
                case BindAction.TearOffTab:
                    if(tabControl1.TabCount > 1) {
                        using(IDLWrapper wrapper = new IDLWrapper(tab.CurrentIDL)) {
                            OpenNewWindow(wrapper);
                        }
                        CloseTab(tab);
                    }
                    break;

                case BindAction.LockCurrent: // �رձ�ǩ
                case BindAction.LockTab:
                    tab.TabLocked = !tab.TabLocked;
                    break;

                case BindAction.LockAll: // ��������
                    bool lockState = tabControl1.TabPages.Any(t => t.TabLocked);
                    tabControl1.TabPages.ForEach(t => t.TabLocked = !lockState);
                    break;
                case BindAction.BrowseFolder: // ���ļ���
                    ChooseNewDirectory();
                    break;

                case BindAction.CreateNewGroup: // �����·���
                    _menuController.CreateGroup(tab);
                    break;
                
                // case BindAction.AddToGroup: // ��������ǩ��
                //     Add2Group(tab);
                //     break;

                case BindAction.ShowOptions: // ��ʾѡ��
                    OptionsDialog.Open();
                    break;

                case BindAction.ShowToolbarMenu: // hmm.
                    Rectangle tabRect = tabControl1.GetTabRect(tabControl1.TabCount - 1, true);
                    contextMenuSys.Show(PointToScreen(new Point(tabRect.Right + 10, tabRect.Bottom - 10)));
                    break;

                case BindAction.ShowTabMenuCurrent:
                    if(tab.Index != -1) {
                        ContextMenuedTab = tab;
                        Rectangle rect = tabControl1.GetTabRect(tab.Index, true);
                        contextMenuTab.Show(PointToScreen(new Point(rect.Right + 10, rect.Bottom - 10)));
                    }
                    break;

                case BindAction.ShowTabMenu:
                    ContextMenuedTab = tab;
                    contextMenuTab.Show(MousePosition);
                    break;

                case BindAction.ShowGroupMenu:
                    TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_GROUP));
                    break;

                case BindAction.ShowRecentTabsMenu:
                    TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_RECENTTAB));
                    break;

                case BindAction.ShowUserAppsMenu:
                    TryCallButtonBar(bbar => bbar.ClickItem(QTButtonBar.BII_APPLICATIONLAUNCHER));
                    break;

                case BindAction.CopySelectedPaths:
                    if(listView.SubDirTipMenuIsShowing() || (subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing)) {
                        return false;
                    }
                    DoFileTools(0);
                    break;

                case BindAction.CopySelectedNames:
                    if(listView.SubDirTipMenuIsShowing() || (subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing)) {
                        return false;
                    }
                    DoFileTools(1);
                    break;

                case BindAction.CopyCurrentFolderPath:
                    DoFileTools(2);
                    break;

                case BindAction.CopyCurrentFolderName:
                    DoFileTools(3);
                    break;

                case BindAction.ChecksumSelected:
                    DoFileTools(4);
                    break;

                case BindAction.ToggleTopMost:
                    ToggleTopMost(); // todo: move v to < ?
                    TryCallButtonBar(bbar => bbar.RefreshButtons());
                    break;

                case BindAction.TransparencyPlus:
                case BindAction.TransparencyMinus: {
                        // TODO!!!
                        int num9;
                        int num10;
                        byte num11;
                        if(0x80000 != ((int)PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000))) {
                            if(action == BindAction.TransparencyPlus) {
                                return true;
                            }
                            PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
                            PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, 0xff, 2);
                        }
                        if(PInvoke.GetLayeredWindowAttributes(ExplorerHandle, out num9, out num11, out num10)) {
                           // IntPtr ptr3;
                            if(action == BindAction.TransparencyPlus) {
                                if(num11 > 0xf3) {
                                    num11 = 0xff;
                                }
                                else {
                                    num11 = (byte)(num11 + 12);
                                }
                            }
                            else if(num11 < 0x20) {
                                num11 = 20;
                            }
                            else {
                                num11 = (byte)(num11 - 12);
                            }
                            PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, num11, 2);
                            // IM!
                            //if(InstanceManager.TryGetButtonBarHandle(ExplorerHandle, out ptr3)) {
                            //    QTUtility2.SendCOPYDATASTRUCT(ptr3, (IntPtr)7, "track", (IntPtr)num11);
                            //}
                            if(num11 == 0xff) {
                                PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0xfff7ffff));
                            }
                        }
                    }
                    break;

                case BindAction.FocusFileList:
                    listView.SetFocus();
                    break;

                case BindAction.FocusSearchBarReal:
                    if(QTUtility.IsXP) return false;
                    // todo, I don't think this works
                    PInvoke.SetFocus(GetSearchBand_Edit());
                    break;

                case BindAction.FocusSearchBarBBar:
                    TryCallButtonBar(bbar => { return bbar.FocusSearchBox(); });
                    break;

                case BindAction.ShowSDTSelected:
                    if(!Config.Tips.ShowSubDirTips) return false;
                    listView.ShowAndClickSubDirTip();
                    break;

                case BindAction.SendToTray:
                    MinimizeToTray();
                    break;

                case BindAction.FocusTabBar:
                    tabControl1.Focus();
                    tabControl1.FocusNextTab(false, true, false);
                    break;

                case BindAction.NewTab:
                    using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                        OpenNewTab(wrapper, false, true);    
                    }
                    break;

                case BindAction.NewWindow:
                    using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                        OpenNewWindow(wrapper);
                    }
                    break;

                // TODO all the blank ones
                case BindAction.NewFolder:
                    break;
                case BindAction.NewFile:
                    createNewFile();
                    break;

                case BindAction.SwitchToLastActivated:
                    if(lstActivatedTabs.Count > 1 && tabControl1.TabPages.Contains(lstActivatedTabs[lstActivatedTabs.Count - 2])) {
                        try {
                            tabControl1.SelectTab(lstActivatedTabs[lstActivatedTabs.Count - 2]);
                        }
                        catch(ArgumentException) {
                        }
                    }
                    break;

                case BindAction.MergeWindows:
                    MergeAllWindows();
                    break;

                case BindAction.ShowRecentFilesMenu:
                    break;
                case BindAction.SortTabsByName:
                    break;
                case BindAction.SortTabsByPath:
                    break;
                case BindAction.SortTabsByActive:
                    break;

                case BindAction.UpOneLevelTab:
                    // QTUtility2.log("QTTabBarClass UpOneLevelTab");
                case BindAction.UpOneLevel:
                    QTUtility2.log("QTTabBarClass case UpOneLevel");
                    UpOneLevel(); // Hmm...
                    break;

                case BindAction.Refresh:
                    break;
                case BindAction.Paste:
                    break;
                case BindAction.Maximize:
                    break;
                case BindAction.Minimize:
                    break;

                case BindAction.CopyTabPath:
                    // ���Ʊ�ǩ��·��·��
                    string currentPath = tab.CurrentPath;
                    if(currentPath.IndexOf("???") != -1) {
                        currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                    }
                    QTUtility2.SetStringClipboard(currentPath);
                    break;

                case BindAction.TabProperties:
                    ShellMethods.ShowProperties(tab.CurrentIDL);
                    break;

                case BindAction.ShowTabSubfolderMenu:
                    break;

                case BindAction.ItemOpenInNewTab:
                case BindAction.ItemOpenInNewTabNoSel:
                case BindAction.ItemOpenInNewWindow:
                    if(item.Available && item.HasPath && item.IsReadyIfDrive && !item.IsLinkToDeadFolder) {
                        using(IDLWrapper linkWrapper = item.ResolveTargetIfLink()) {
                            IDLWrapper actualItem = linkWrapper ?? item;
                            if(actualItem.IsFolder) {
                                if(action == BindAction.ItemOpenInNewWindow) {
                                    OpenNewWindow(actualItem);
                                }
                                else {
                                    OpenNewTab(actualItem, action == BindAction.ItemOpenInNewTabNoSel);
                                }
                            }
                        }
                    }
                    break;

                case BindAction.ItemCut:
                case BindAction.ItemCopy:      
                case BindAction.ItemDelete:
                    break;

                case BindAction.ItemProperties:
                    ShellMethods.ShowProperties(item.IDL);
                    break;

                case BindAction.CopyItemPath:
                case BindAction.CopyItemName:
                case BindAction.ChecksumItem:
                    break;
                /***** add by qwop start ***/
                case BindAction.OpenCmd:  // ��������ʾ��
                    OpenCmd( tab ); // add by qwop...
                    break;
                case BindAction.ItemsOpenInNewTabNoSel: // ��ѡ�е��ļ��� ����ǩҳ(������)
                    Address[] addressArray;
                    if ( ShellBrowser.TryGetSelection(out addressArray, false  )) {
                        foreach (Address address in addressArray)
                        {
                            if (address.Path != null && Directory.Exists(address.Path) ) {
                                OpenNewTab(address.Path, action == BindAction.ItemsOpenInNewTabNoSel);
                            }
                        }
                    }                   
                    break;
                /***** add by qwop end   ***/
            }
            return true;
        }

        /// <summary>
        /// �������ļ� add by indiff
        /// </summary>
        private void createNewFile()
        {

            // Create new file
            IShellView shellView = null;
            IntPtr pIDL = IntPtr.Zero;

            try
            {
                string path = pluginServer.SelectedTab.Address.Path;

                if (String.IsNullOrEmpty(path) || !Directory.Exists(path))
                {
                    QTUtility.SoundPlay();
                    return;
                }

                // make new name

                int i = 2;
                // string name = "�½��ı��ĵ�";
                string name = QTUtility.DefaultNewFileName();
                string ext =  ".txt";
                string pathNew = path + "\\" + name + ext;

                while ( Directory.Exists(pathNew) || File.Exists(pathNew) )
                {
                    pathNew = path + "\\" + name + " (" + i + ")" + ext;
                    i++;
                }

                using (File.Create(pathNew))
                {
                }

                // Select and put into rename mode.
                if (0 == ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView))
                {
                    shellView.Refresh();

                    pIDL = PInvoke.ILCreateFromPath(pathNew);
                    if (pIDL != IntPtr.Zero)
                    {
                        IntPtr pIDLRltv = PInvoke.ILFindLastID(pIDL);
                        if (pIDLRltv != IntPtr.Zero) {
                            // ѡ���ļ�
                             shellView.SelectItem(pIDLRltv, SVSIF.SELECT | SVSIF.DESELECTOTHERS | SVSIF.ENSUREVISIBLE | SVSIF.EDIT );
                           //  ShellBrowser.GetIShellBrowser().SelectItem(pIDLRltv, SVSI_SELECT | SVSI_DESELECTOTHERS | SVSI_ENSUREVISIBLE | SVSI_EDIT);
                            return;
                        }
                    }
                }
            }
            catch
            {
            }
            finally
            {
                if (ShellBrowser.GetIShellBrowser() != null)
                {
                    QTUtility2.log("ReleaseComObject ShellBrowser.GetIShellBrowser()");
                    Marshal.ReleaseComObject(ShellBrowser.GetIShellBrowser());
                }
                    

                if (pIDL != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pIDL);
            }

            QTUtility.SoundPlay();
       
        }
        /***** add by qwop end   ***/

        /// <summary>
        /// �ú��������ɲ�ͬ�̲߳����Ĵ��ڵ���ʾ״̬��
        /// </summary>
        /// <param name="hWnd">���ھ��</param>
        /// <param name="cmdShow">ָ�����������ʾ���鿴����ֵ�б��������ShowWlndow������˵�����֡�</param>
        /// <returns>�������ԭ���ɼ�������ֵΪ���㣻�������ԭ�������أ�����ֵΪ�㡣</returns>
        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        /// <summary>
        /// �ú���������ָ�����ڵ��߳����õ�ǰ̨�����Ҽ���ô��ڡ���������ת��ô��ڣ���Ϊ�û��ĸ��ֿ��ӵļǺš�ϵͳ������ǰ̨���ڵ��̷߳����Ȩ���Ը��������̡߳�
        /// </summary>
        /// <param name="hWnd">�������������ǰ̨�Ĵ��ھ����</param>
        /// <returns>�������������ǰ̨������ֵΪ���㣻�������δ������ǰ̨������ֵΪ�㡣</returns>
        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("User32.dll")]
        private static extern bool SetFocus(IntPtr hWnd);

        private const int WS_SHOWNORMAL = 1;

        private void cmdPath(string currentPath)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/k cd " + currentPath;
            process.StartInfo.WorkingDirectory = currentPath;
            process.Start();

            ShowWindowAsync(process.MainWindowHandle, WS_SHOWNORMAL); //��ʾ������ע�͵�
            SetForegroundWindow(process.MainWindowHandle);            //�ŵ�ǰ��
            SetFocus(process.MainWindowHandle);
        }

        /************************************************************************/
        /* �򿪵�ǰĿ¼�� ������ʾ��                                            */
        /************************************************************************/
        private void OpenCmd( QTabItem tab )
        {
            /*            if ( tab == null ) {
                            return ;
                        }*/
            if (ShellBrowser.GetIShellBrowser() != null) { 
                string currentPath = "";
                if (tab != null)
                {
                    currentPath = tab.CurrentPath;
                }
                else
                {
                    currentPath = ContextMenuedTab.CurrentPath;
                }
            
                if (currentPath.IndexOf("???") != -1)
                {
                    currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                }
                else if (currentPath.IndexOf("*?*?*") != -1)
                {
                    currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                }
                // �ж��ļ��Ƿ����
                if (Directory.Exists(currentPath))
                {
                    /*
                    ProcessStartInfo startInfo = new ProcessStartInfo("cmd");
                    startInfo.WindowStyle = ProcessWindowStyle.Normal;
                    startInfo.Verb = "runas";
                    startInfo.CreateNoWindow = false;
                
                
                    startInfo.WorkingDirectory = currentPath;
                    // ���ӻ�ȡ����
                    Process instance = Process.Start(startInfo );
                    //  instance.WaitForInputIdle();
                    
                    if (!instance.WaitForInputIdle(10000)) // 10 s timout 
                    {
                        throw new ApplicationException("Process takes too much time to start");
                    }
                    */

                    cmdPath(currentPath);

                } // end for open cmd.
                else { 
                    // �Ҳ���·�����ϵͳ��
                    if (QTUtility2.PathExists("C:\\"))
                    {
                        cmdPath("C:\\");
                    } else if (QTUtility2.PathExists("D:\\"))
                    {
                        cmdPath("D:\\");
                    }
                    else if (QTUtility2.PathExists("E:\\"))
                    {
                        cmdPath("E:\\");
                    }
                    else if (QTUtility2.PathExists("F:\\"))
                    {
                        cmdPath("F:\\");
                    }
                }
            }
        }

        // todo: clean, enum.
        internal bool DoFileTools(int index) {
            try {
                Address[] addressArray;
                List<string> list;
                int num;
                string displayName = string.Empty;
                switch(index) {
                    case 0:
                    case 1:
                    case 4:
                        string str2;
                        if(!ShellBrowser.TryGetSelection(out addressArray, out str2, index == 1)) {
                            goto Label_019C;
                        }
                        list = new List<string>();
                        num = 0;
                        goto Label_00A1;

                    case 2:
                    case 3: {
                            using(IDLWrapper wrapper = ShellBrowser.GetShellPath()) {
                                if(wrapper.Available) {
                                    displayName = ShellMethods.GetDisplayName(wrapper.PIDL, index == 3);
                                }
                                goto Label_019C;
                            }
                        }
                    case 5:
                        foreach(QTabItem item in tabControl1.TabPages) {
                            string currentPath = item.CurrentPath;
                            int length = currentPath.IndexOf("???");
                            if(length != -1) {
                                currentPath = currentPath.Substring(0, length);
                            }
                            int num3 = currentPath.IndexOf("*?*?*");
                            if(num3 != -1) {
                                currentPath = currentPath.Substring(0, num3);
                            }
                            displayName = displayName + ((displayName.Length == 0) ? string.Empty : "\r\n") + currentPath;
                        }
                        goto Label_019C;

                    default:
                        goto Label_019C;
                }
            Label_004B:
                if(addressArray[num].Path != null) {
                    if(index != 4) {
                        displayName = displayName + ((displayName.Length == 0) ? string.Empty : "\r\n") + addressArray[num].Path;
                    }
                    else {
                        list.Add(addressArray[num].Path);
                    }
                }
                num++;
            Label_00A1:
                if(num < addressArray.Length) {
                    goto Label_004B;
                }
                if(index == 4) {
                    ShowMD5(list.ToArray());
                    return true;
                }
            Label_019C:
                if(displayName.Length > 0) {
                    QTUtility2.SetStringClipboard(displayName);
                    return true;
                }
            }
            catch (Exception e){
                QTUtility2.MakeErrorLog( e, "DoFileTools");
            }
            if(index == 4) {
                // .net4 ��������ǿ����������е�
                /*QTUtility2.log("md5 out tabbar.OpenNewTab");
                InstanceManager.BeginInvokeMain(tabbar =>
                {
                    QTUtility2.log("md5 inner tabbar.OpenNewTab");
                    tabbar.OpenNewTab("c:/");
                    tabbar.RestoreWindow();
                });*/

                ShowMD5(null);
                return true;
            }
            return false;
        }

        // ����ķ�ʽ  select 1 / factory 2 / other 3
        private int mCmdType = 0;

        // This function is either called by BeforeNavigate2 (on XP and Vista)
        // �˺�����BeforeNavigate2����(on XP and Vista)
        // or NavigateComplete2 (on 7)
        // DoFirstNavigation moved to ExplorerControllerModule (Batch 13)

        /**
         * ��ʱ����� 1 �뷽ʽ����ȡ΢�Ż���qq�򿪺��ѡ���ļ�
         */
        private void Wait4Select()
        {
            if (!Config.Window.CaptureWeChatSelection)
            {
                return;
            }

            QTUtility2.log("Wait4Select");
            int count = 1;
            // �Թر� ��ʱ����� 1 ��
            Timer timer = new Timer { Interval = 1000 };
            timer.Tick += (sender, args) =>
            {
                try
                {
                    count++;
                    // ��������10�� �Թر� 
                    if (count >= 10)
                    {
                        timer.Stop();
                    }
                    string SelectionPath = RegistryUtil.ReadSelection(CurrentTab.CurrentPath);
                    QTUtility2.log(
                        " ReadSelection key " +
                        CurrentTab.CurrentPath +
                        " path " + SelectionPath
                    );
                    if (QTUtility2.IsNotEmpty(SelectionPath) )
                    {
                        QTUtility2.log("find mainShellView ");
                        IShellView mainShellView = null;
                        bool selected = false;
                        if (0 == ShellBrowser.GetIShellBrowser().QueryActiveShellView(out mainShellView))
                        {
                            mainShellView.Refresh();
                            QTUtility2.log("Refresh ");
                            using (IDLWrapper wrapper = new IDLWrapper(SelectionPath))
                            {
                                if (wrapper.Available && wrapper.PIDL != IntPtr.Zero)
                                {
                                    QTUtility2.log("wrapper " + wrapper.Path);
                                    IntPtr pIDLRltv = PInvoke.ILFindLastID(wrapper.PIDL);
                                    if (pIDLRltv != IntPtr.Zero)
                                    {
                                        QTUtility2.log("SelectItem " + pIDLRltv);
                                        // ѡ���ļ�
                                        mainShellView.SelectItem(pIDLRltv, SVSIF.SELECT |
                                                                           SVSIF.DESELECTOTHERS |
                                                                           SVSIF.ENSUREVISIBLE
                                            // | SVSIF.EDIT
                                        );
                                        selected = true;
                                    }
                                }
                            }
                        }
                        if (selected)
                        {
                            timer.Stop();
                            // InstanceManager.RemoveSelect(CurrentTab.CurrentPath );
                            // InstanceManager.selectDict.Remove(CurrentTab.CurrentPath);
                        }
                    }
                }
                catch (Exception e)
                {
                    QTUtility2.MakeErrorLog(e, "��ȡ΢�Ż���qq�򿪺��ѡ���ļ�");
                }
            };
            timer.Start();
        }


        private void Wait4SelectedQuit()
        {
            int count = 1;
            Timer timer = new Timer { Interval = 1000 };
            timer.Tick += (sender, args) =>
            {
                count++;
                if (count >= 10)
                {
                    QTUtility2.log("CloseExplorer QTTabBarClass start");
                    timer.Stop();
                    Explorer.Quit();
                    WindowUtils.CloseExplorer(ExplorerHandle, 0);
                    QTUtility2.log("CloseExplorer QTTabBarClass  end");
                }

                QTUtility2.log("QTTabBarClass timer.Tick TryGetSelection ");
                try
                {
                    var tabItem = tabControl1.TabPages[0];
                    var tabItemComment = tabItem.Text;
                    IShellView shellView = null;
                    List<string> select = InstanceManager.GetSelect(tabItem.CurrentPath);
                    if (select != null)
                    {
                        timer.Stop();
                        Explorer.Quit();
                        WindowUtils.CloseExplorer(ExplorerHandle, 0);
                    }

                    if (0 == ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView))
                    {
                        var iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                        object ppv;
                        shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                        if (ppv != null)
                        {
                            IDataObject pDataObject = (IDataObject)ppv;
                            var shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                            if (shellObjectCollection.Count > 0)
                            {
                                List<string> list = new List<string>();
                                foreach (ShellObject so in shellObjectCollection)
                                {
                                    QTUtility2.log("add so.Name " + so.Name + " so.ParsingName " + so.ParsingName);
                                    list.Add(so.ParsingName);

                                    InstanceManager.PutSelect(tabItem.CurrentPath, list);
                                    // InstanceManager.selectDict.Add(tabItem.CurrentPath, list);
                                    timer.Stop();
                                    Explorer.Quit();
                                    WindowUtils.CloseExplorer(ExplorerHandle, 0);

                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            };
            timer.Start();
        }

        /*
                      Address[] addressArray;
                      if (ShellBrowser.TryGetSelection(out addressArray, false))
                      {
                          foreach (Address address in addressArray)
                          {
                              if (address.Path != null && Directory.Exists(address.Path))
                              {
                                  QTUtility2.log("TryGetSelection " + address.Path);
                                  // OpenNewTab(address.Path, action == BindAction.ItemsOpenInNewTabNoSel);
                              }
                          }
                      } 
                    */


        // TryParseCommandlineParams moved to ExplorerControllerModule (Batch 13)

        private int dropTargetWrapper_DragFileDrop(out IntPtr hwnd, out byte[] idlReal) {
            HideToolTipForDD();
            hwnd = tabControl1.Handle;
            idlReal = null;
            QTabItem tabMouseOn = tabControl1.GetTabMouseOn();
            if((tabMouseOn == null) || !Config.Tabs.DragOverTabOpensSDT) {
                return 1;
            }
            if((tabMouseOn.CurrentIDL != null) && (tabMouseOn.CurrentIDL.Length > 0)) {
                idlReal = tabMouseOn.CurrentIDL;
                return 0;
            }
            return -1;
        }

        private DragDropEffects dropTargetWrapper_DragFileEnter(IntPtr hDrop, Point pnt, int grfKeyState) {
            if(Config.Tabs.DragOverTabOpensSDT) {
                int num = HandleDragEnter(hDrop, out strDraggingDrive, out strDraggingStartPath);
                fDrivesContainedDD = num == 2;
                if(num == -1) {
                    return DragDropEffects.None;
                }
                if(tabControl1.GetTabMouseOn() == null) {
                    return DragDropEffects.Copy;
                }
                switch(num) {
                    case 0:
                        return DropTargetWrapper.MakeEffect(grfKeyState, 0);

                    case 1:
                        return DropTargetWrapper.MakeEffect(grfKeyState, 1);

                    case 2:
                        return DragDropEffects.None;
                }
            }
            return DragDropEffects.Copy;
        }

        private void dropTargetWrapper_DragFileLeave(object sender, EventArgs e) {
            HideToolTipForDD();
            strDraggingDrive = null;
            strDraggingStartPath = null;
            tabControl1.Refresh();
        }

        private void dropTargetWrapper_DragFileOver(object sender, DragEventArgs e) {
            QTUtility2.log("QTTabBarClass dropTargetWrapper_DragFileOver");
            e.Effect = DragDropEffects.None;
            QTabItem mouseOnTab = tabControl1.GetTabMouseOn(); // ��������ı�ǩ 
            bool flag = true;
            if(mouseOnTab != tabForDD) {
                tabControl1.Refresh();
                HideSubDirTip_Tab_Menu();
                fToggleTabMenu = false;
                flag = false;
            }
            if(mouseOnTab == null) {
                e.Effect = DragDropEffects.Copy;
            }
            else if(mouseOnTab.CurrentPath.Length > 2) {
                if(fDrivesContainedDD || strDraggingStartPath.PathEquals(mouseOnTab.CurrentPath)) {
                    if(toolTipForDD != null) {
                        toolTipForDD.Hide(tabControl1);
                    }
                    ShowToolTipForDD(mouseOnTab, -1, e.KeyState); // ��ʾtip ��ʾ��Ϣ
                }
                else {
                    using(IDLWrapper wrapper = new IDLWrapper(mouseOnTab.CurrentIDL, !flag)) {
                        if(wrapper.Available && wrapper.IsDropTarget) {
                            string b = mouseOnTab.CurrentPath.Substring(0, 3);
                            int num = strDraggingDrive != null && strDraggingDrive.Equals(b, StringComparison.OrdinalIgnoreCase)
                                    ? 0 : 1;
                            ShowToolTipForDD(mouseOnTab, num, e.KeyState);
                            e.Effect = Config.Tabs.DragOverTabOpensSDT
                                    ? DropTargetWrapper.MakeEffect(e.KeyState, num)
                                    : DragDropEffects.Copy;
                        }
                        else {
                            HideToolTipForDD();
                        }
                    }
                }
            }
            else {
                HideToolTipForDD();
            }
        }

        // Explorer_BeforeNavigate2 moved to ExplorerControllerModule (Batch 13)



        private void Explorer_NavigateComplete2(object pDisp, ref object URL) {
            _explorerControllerModule.Explorer_NavigateComplete2(pDisp, ref URL);
        }


        // ��Ϣ����
        // explorerController_MessageCaptured moved to ExplorerControllerModule (Batch 13)

        public override void GetBandInfo(uint dwBandID, uint dwViewMode, ref DESKBANDINFO dbi) {
            if((dbi.dwMask & DBIM.ACTUAL) != (0)) {
                dbi.ptActual.X = Size.Width;
                dbi.ptActual.Y = BandHeight;
            }
            if((dbi.dwMask & DBIM.INTEGRAL) != (0)) {
                dbi.ptIntegral.X = -1;
                dbi.ptIntegral.Y = 10;
            }
            if((dbi.dwMask & DBIM.MAXSIZE) != (0)) {
                dbi.ptMaxSize.X = -1;
                dbi.ptMaxSize.Y = BandHeight;
            }
            if((dbi.dwMask & DBIM.MINSIZE) != (0)) {
                dbi.ptMinSize.X = MinSize.Width;
                dbi.ptMinSize.Y = BandHeight;
            }
            if((dbi.dwMask & DBIM.MODEFLAGS) != (0)) {
                dbi.dwModeFlags = DBIMF.NORMAL;
            }
            if((dbi.dwMask & DBIM.BKCOLOR) != (0)) {
                dbi.dwMask &= ~DBIM.BKCOLOR;
            }
            if((dbi.dwMask & DBIM.TITLE) != (0)) {
                dbi.wszTitle = null;
            }
        }

        // GetCurrentLogEntry moved to ExplorerControllerModule (Batch 13)

        internal IDLWrapper GetCurrentPIDL() {
            IDLWrapper wrapper = ShellBrowser.GetShellPath();
            if(!wrapper.Available) {
                wrapper.Dispose();
                wrapper = new IDLWrapper(ShellMethods.ShellGetPath2(ExplorerHandle));
                if(!wrapper.Available) {
                    wrapper.Dispose();
                    wrapper = new IDLWrapper(lastCompletedBrowseObjectIDL);
                }
            }
            return wrapper;
        }

        private Cursor GetCursor(bool fDragging) {
            return _tabManager.GetCursor(fDragging);
        }
        /**
         * new �Ƿ��������أ�
         */
        // GetCommandLine moved to ExplorerControllerModule (Batch 13)

        // GetNameToSelectFromCommandLineArg moved to ExplorerControllerModule (Batch 13)

        private IntPtr GetSearchBand_Edit() {
            IntPtr hwndSearchBand = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "UniversalSearchBand");
            if(hwndSearchBand != IntPtr.Zero) {
                hwndSearchBand = WindowUtils.FindChildWindow(hwndSearchBand, hwnd =>
                        PInvoke.GetClassName(hwnd) == "Edit" && ((int)PInvoke.GetWindowLongPtr(hwnd, -16) & 0x10000000) != 0);
            }
            return hwndSearchBand;
        }

        public ShellBrowserEx GetShellBrowser() {
            return ShellBrowser;
        }

        private IntPtr GetTravelToolBarWindow32() {
            IntPtr hwndTravelBand = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "TravelBand");
            return hwndTravelBand != IntPtr.Zero 
                    ? PInvoke.FindWindowEx(hwndTravelBand, IntPtr.Zero, "ToolbarWindow32", null) 
                    : IntPtr.Zero;
        }

        internal struct TVITEM
        {
            public int mask;
            public IntPtr hItem;
            public int state;
            public int stateMask;
            public IntPtr pszText;
            public int cchTextMax;
            public int iImage;
            public int iSelectedImage;
            public int cChildren;
            public IntPtr lParam;
        }

        #region �м������Դ��������ർ��
        unsafe private void Handle_MButtonUp_Tree(MSG msg) 
        {
            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree msg");
            // IntPtr ptr;
            // if (ShellBrowser.IsFolderTreeVisible(out ptr)   && msg.hwnd == ptr)
            if ( !Explorer.Busy && msg.hwnd != null)
            {
                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree hwnd  " + msg.hwnd);
                TVHITTESTINFO structure = new TVHITTESTINFO { pt = QTUtility2.PointFromLPARAM(msg.lParam) };
                // HitTest
                IntPtr wParam = PInvoke.SendMessage(msg.hwnd, 0x1111, IntPtr.Zero, ref structure);
                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree wParam  " + wParam);
                if (wParam != IntPtr.Zero)
                {
                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  start " + 4362);
                    Stack<IntPtr> numStack = new Stack<IntPtr>();
                    do
                    {
                        numStack.Push(wParam);
                    }
                    while ((wParam = PInvoke.SendMessage(msg.hwnd, 4362, (IntPtr)3, wParam)) != IntPtr.Zero);
                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  end " + 4362);
                    TVITEM* ptrTvitem = stackalloc TVITEM[1];
                    ptrTvitem->mask = 4;
                    int num1 = 0;
                    IntPtr pidl = IntPtr.Zero;
                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Count " + numStack.Count);
                    while (numStack.Count > 0)
                    {
                        // var hItem = numStack.Pop();
                        ptrTvitem->hItem = numStack.Pop();
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop hItem " + ptrTvitem->hItem);
                        bool flag1 = num1 == 2 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{031E4825-7B94-4DC3-B131-E946B44C8DD5}";
                        bool flag2 = num1 == 1 && numStack.Count == 0 && ShellMethods.GetPath(pidl) == "::{679F85CB-0220-4080-B29B-5540CC05AAB6}";
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop flag1 " + flag1 + " flag2 " + flag2);
                        if (!(IntPtr.Zero != PInvoke.SendMessage(msg.hwnd, 4414, (void*)null, (void*)ptrTvitem)))
                        {
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree  while return1 " );
                            return;
                        }

                        if (!(ptrTvitem->lParam != IntPtr.Zero))
                        {
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree  while return2 ");
                            return;
                        }
                        IntPtr num2 = IntPtr.Zero;
                        try
                        {
                            num2 = PInvoke.ILCombine(pidl, *(IntPtr*)*(IntPtr*)(void*)ptrTvitem->lParam);
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree numStack.Pop ILCombine  " + num2 + " pidl " + pidl);
                            if (pidl != IntPtr.Zero)
                            {
                                PInvoke.CoTaskMemFree(pidl);
                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree CoTaskMemFree pidl " + pidl);
                            }
                        }
                        catch (Exception e)
                        {
                            QTUtility2.MakeErrorLog(e, "QTTabBarClass Handle_MButtonUp_Tree Exception");
                        }
                       
                            

                        if (flag1 | flag2)
                        {
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree ShellMethods.GetPath " + num2);
                            string path = ShellMethods.GetPath(num2);
                            if (!string.IsNullOrEmpty(path))
                            {

                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree path  " + path);
                                uint theOut = 0;
                                PInvoke.SHParseDisplayName(path, IntPtr.Zero, out pidl, 0, out theOut);
                                if (num2 != IntPtr.Zero)
                                    PInvoke.CoTaskMemFree(num2);
                            }
                            else
                            {
                                pidl = num2;
                            }
                        }
                        else
                        {
                            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree flag1 " + flag1 + " flag2" + flag2);
                            pidl = num2;
                        }
                        ++num1;
                        /*IShellFolder ppv = null;
                        IntPtr ptr1;
                        if (PInvoke.SHBindToParent(pidl, ExplorerGUIDs.IID_IShellFolder, out ppv, out ptr1) ==
                            0)
                        {
                           // IntPtr num2 = PInvoke.ILCombine(pidl, ptr1);
                        }*/
                    }

                    if (pidl != null && pidl != IntPtr.Zero) {
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree pidl " + pidl);
                        MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
                        BindAction action;
                        
                        Keys modKeys = ModifierKeys;
                        // QTUtility2.InitializeTemporaryPaths();
                        bool fBlockSelecting = modKeys == Keys.Shift;
                        bool fCtrl = modKeys == Keys.Control;
                        using (IDLWrapper wrapper = new IDLWrapper(pidl))
                        {
                            if (!wrapper.Available) return;
                            if (wrapper.IsFolder && wrapper.IsReadyIfDrive)
                            {
                                // blockSelecting
                                NavigatedByCode = true;
                                fNowTravelByTree = false; // �ᵼ�¶࿪һ����ǩ by indiff
                               // OpenNewTab(wrapper, true);

                               if (Config.Mouse.ItemActions.TryGetValue(chord, out action))
                               {
                                   if (action == BindAction.ItemOpenInNewTab)
                                   {
                                       OpenNewTab(wrapper, false);
                                       QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                   }
                                   else if (action == BindAction.ItemOpenInNewTabNoSel)
                                   {
                                       OpenNewTab(wrapper, true);
                                       QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
                                   }
                               }
                               else
                               {
                                   if (fCtrl)
                                   {
                                       OpenNewTab(wrapper, true);
                                   }
                                   else
                                   {
                                       OpenNewTab(wrapper, false);
                                   }
                               }
                               QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree IsFolder IsReadyIfDrive " + wrapper.Path);
                            } else if (wrapper.IsLink){
                                if (wrapper.IsLinkToDeadFolder)
                                {
                                    QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree wrapper.IsLinkToDeadFolder");
                                    return;
                                }
                                using (IDLWrapper idlwTarget = new IDLWrapper(ShellMethods.GetLinkTargetIDL(wrapper.Path)))
                                {
                                    if (idlwTarget.IsFolder && idlwTarget.IsReadyIfDrive)
                                    {
                                        NavigatedByCode = true;
                                        fNowTravelByTree = false; // �ᵼ�¶࿪һ����ǩ by indiff
                                        if (Config.Mouse.ItemActions.TryGetValue(chord, out action))
                                        {
                                            if (action == BindAction.ItemOpenInNewTab)
                                            {
                                                OpenNewTab(wrapper, false);
                                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTab");
                                            }
                                            else if (action == BindAction.ItemOpenInNewTabNoSel)
                                            {
                                                OpenNewTab(wrapper, true);
                                                QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree BindAction.ItemOpenInNewTabNoSel");
                                            }
                                        }
                                        else
                                        {
                                            if (fCtrl)
                                            {
                                                // StaticReg.CreateWindowIDLs.Add(wrapper.IDL);
                                                OpenNewTab(wrapper, true);
                                            }
                                            else
                                            {
                                                OpenNewTab(wrapper, false);
                                            }
                                        }
                                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree IsLink GetLinkTargetIDL" + wrapper.Path);
                                    }
                                }
                            }
                        }
                    }
                    /*QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree wParam " + wParam);
                    int num = (int)PInvoke.SendMessage(msg.hwnd, 0x1127, wParam, (IntPtr)2);
                    if ((num & 2) == 0)
                    {
                        NavigatedByCode = fNowTravelByTree = true;
                        PInvoke.SendMessage(msg.hwnd, 0x110b, (IntPtr)9, wParam);
                        QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree SendMessage  " + wParam);
                    }*/
                }
            }
            else
            {
                QTUtility2.log("QTTabBarClass Explorer.Busy msg.hwnd " + msg.hwnd);
            }
        }
        #endregion

        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        /*
        public static string GetTreeItemText(IntPtr treeViewHwnd, IntPtr hItem)
        {
            string itemText;

            uint pid;
            uint MY_MAXLVITEMTEXT = 1024;
            PInvoke.GetWindowThreadProcessId(treeViewHwnd, out pid);


            IntPtr process = PInvoke.OpenProcess(ProcessAccessFlags.VirtualMemoryOperation |
                                                 ProcessAccessFlags.VirtualMemoryRead |
                                                 ProcessAccessFlags.VirtualMemoryWrite | 
                                                 ProcessAccessFlags.QueryInformation, false, pid);
            if (process == IntPtr.Zero)
                throw new Exception("Could not open handle to owning process of TreeView", new Win32Exception());

            try
            {
                uint tviSize = Marshal.SizeOf(typeof(TVITEM));
                
                uint textSize = MY_MAXLVITEMTEXT;
                bool isUnicode = IsWindowUnicode(treeViewHwnd);
                if (isUnicode)
                    textSize *= 2;

                IntPtr tviPtr = PInvoke.VirtualAllocEx(process, IntPtr.Zero, tviSize + textSize, AllocationType.Commit, MemoryProtection.ReadWrite);
                if (tviPtr == IntPtr.Zero)
                    throw new Exception("Could not allocate memory in owning process of TreeView", new Win32Exception());

                try
                {
                    IntPtr textPtr = IntPtr.Add(tviPtr, tviSize);

                    TVITEM tvi = new TVITEM();
                    tvi.mask = TVIF_TEXT;
                    tvi.hItem = hItem;
                    tvi.cchTextMax = MY_MAXLVITEMTEXT;
                    tvi.pszText = textPtr;

                    IntPtr ptr = Marshal.AllocHGlobal(tviSize);
                    try
                    {
                        Marshal.StructureToPtr(tvi, ptr, false);
                        if (!PInvoke.WriteProcessMemory(process, tviPtr, ptr,(int) tviSize, IntPtr.Zero))
                            throw new Exception("Could not write to memory in owning process of TreeView", new Win32Exception());
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }

                    if (SendMessage(treeViewHwnd, isUnicode ? TVM_GETITEMW : TVM_GETITEMA, 0, tviPtr) != 1)
                        throw new Exception("Could not get item data from TreeView");

                    ptr = Marshal.AllocHGlobal(textSize);
                    try
                    {
                        int bytesRead;
                        if (!PInvoke.ReadProcessMemory(process, textPtr, ptr, textSize, out bytesRead))
                            throw new Exception("Could not read from memory in owning process of TreeView", new Win32Exception());

                        if (isUnicode)
                            itemText = Marshal.PtrToStringUni(ptr, bytesRead / 2);
                        else
                            itemText = Marshal.PtrToStringAnsi(ptr, bytesRead);
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(ptr);
                    }
                }
                finally
                {
                    PInvoke.VirtualFreeEx(process, tviPtr, 0, FreeType.Release);
                }
            }
            finally
            {
                PInvoke.CloseHandle(process);
            }

            //char[] arr = itemText.ToCharArray(); //<== use this array to look at the bytes in debug mode

            return itemText;
        } */

        private bool Handle_MButtonUp_Tree(IntPtr hwnd, IntPtr lParam)
        {
            QTUtility2.log("QTTabBarClass Handle_MButtonUp_Tree");
            IntPtr ptr;
            if(ShellBrowser.IsFolderTreeVisible(out ptr) && hwnd == ptr) {
                TVHITTESTINFO structure = new TVHITTESTINFO {pt = QTUtility2.PointFromLPARAM(lParam)};
                QTUtility2.log("QTTabBarClass structure " + structure);
                IntPtr wParam = PInvoke.SendMessage(ptr, 0x1111, IntPtr.Zero, ref structure);
                if(wParam != IntPtr.Zero) {
                    int num = (int)PInvoke.SendMessage(ptr, 0x1127, wParam, (IntPtr)2);
                    if((num & 2) == 0) {
                        NavigatedByCode = fNowTravelByTree = true;
                        PInvoke.SendMessage(ptr, 0x110b, (IntPtr)9, wParam);
                        return true;
                    }
                }
            }
            return false;
        }
        /**
         * �����رղ���
         */
        private bool HandleCLOSE(IntPtr lParam) {
            bool flag = Config.Window.CloseBtnClosesSingleTab;
            bool flag2 = Config.Window.CloseBtnClosesUnlocked;
            List<string> closingPaths = new List<string>();
            int num = (int)lParam;
            switch(num) {
                case 1:
                    closingPaths = CloseAllTabsExcept(null, flag2);
                    if(tabControl1.TabCount > 0) {
                        return true;
                    }
                    break;
                    
                case 2:
                    return false;

                default: {
                        bool flag3 = QTUtility2.PathExists(CurrentTab.CurrentPath);
                        if((QTUtility.IsXP && flag3) && (num == 0)) {
                            return true;
                        }
                        if(!flag3) {
                            CloseTab(CurrentTab, true);
                            return (tabControl1.TabCount > 0);
                        }
                        if(flag2 && !flag) {
                            closingPaths = CloseAllTabsExcept(null);
                            if(tabControl1.TabCount > 0) {
                                return true;
                            }
                            QTUtility.SaveClosing(closingPaths);
                            return false;
                        }
                        Keys modifierKeys = ModifierKeys;
                        if((modifierKeys == (Keys.Control | Keys.Shift)) || !flag) {
                            foreach(QTabItem item2 in tabControl1.TabPages) {
                                closingPaths.Add(item2.CurrentPath);
                                AddToHistory(item2);
                            }
                            QTUtility.SaveClosing(closingPaths);
                            return false;
                        }
                        if(modifierKeys == Keys.Control) {
                            closingPaths = CloseAllTabsExcept(null);
                        }
                        else {
                            closingPaths.Add(CurrentTab.CurrentPath);
                            CloseTab(CurrentTab, false);
                        }
                        if(tabControl1.TabCount > 0) {
                            return true;
                        }
                        QTUtility.SaveClosing(closingPaths);
                        return false;
                    }
            }
            QTUtility.SaveClosing(closingPaths);
            return false;
        }

        internal static int HandleDragEnter(IntPtr hDrop, out string strDraggingDrive, out string strDraggingStartPath) {
            QTUtility2.log("QTTabBarClass HandleDragEnter IsFolder hDrop " + hDrop + 
                           " out string strDraggingDrive, out string strDraggingStartPath" );
            strDraggingDrive = (strDraggingStartPath = null);
            int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
            if(capacity < 1) {
                return -1;
            }
            List<string> list = new List<string>(capacity);
            for(int i = 0; i < capacity; i++) {
                StringBuilder lpszFile = new StringBuilder(260);
                PInvoke.DragQueryFile(hDrop, (uint)i, lpszFile, lpszFile.Capacity);
                if(lpszFile.Length > 0) {
                    list.Add(lpszFile.ToString());
                }
            }
            if(list.Count <= 0) {
                return -1;
            }
            if(list[0].Length < 4) {
                return 2;
            }
            bool flag = true;
            string b = QTUtility2.MakeRootName(list[0]);
            foreach(string str2 in list) {
                if(File.Exists(str2) || Directory.Exists(str2)) {
                    if(str2.Length <= 3) {
                        return 2;
                    }
                    if(!QTUtility2.MakeRootName(str2).PathEquals(b)) {
                        flag = false;
                    }
                    continue;
                }
                return -1;
            }
            if(flag) {
                strDraggingDrive = b;
                strDraggingStartPath = Path.GetDirectoryName(list[0]);
                return 0;
            }
            return 1;
        }

        private static void HandleF5() {
            TryCallButtonBar(bbar => { return bbar.RefreshSearchBox(false); });
        }

        private void HandleFileDrop(IntPtr hDrop) {
            HideToolTipForDD();
            int capacity = (int)PInvoke.DragQueryFile(hDrop, uint.MaxValue, null, 0);
            if(capacity >= 1) {
                // ��ȡ��ק���ļ��б�
                List<string> listDroppedPaths = new List<string>(capacity);
                for(int i = 0; i < capacity; i++) {
                    StringBuilder lpszFile = new StringBuilder(260);
                    PInvoke.DragQueryFile(hDrop, (uint)i, lpszFile, lpszFile.Capacity);
                    listDroppedPaths.Add(lpszFile.ToString());
                }
                OpenDroppedFolder(listDroppedPaths);
            }
        }

        private bool HandleKEYDOWN(Keys key, bool fRepeat) {
            Keys mkey = key | ModifierKeys;

            switch(key) {
                case Keys.Enter:
                    return false;

                case Keys.Menu:
                    if(!fRepeat && Config.Tabs.ShowCloseButtons && Config.Tabs.CloseBtnsWithAlt) {
                        tabControl1.ShowCloseButton(true);
                    }
                    return false;

                case Keys.ControlKey:
                    if(!fRepeat && NowTabDragging && DraggingTab != null && tabControl1.GetTabMouseOn() == null) {
                        Cursor = GetCursor(false);
                    }
                    break;

                case Keys.Tab:
                    if(Config.Keys.UseTabSwitcher && (mkey & Keys.Control) != Keys.None) {
                        return ShowTabSwitcher((mkey & Keys.Shift) != Keys.None, fRepeat);
                    }
                    break;
            }

            switch(mkey) {
                case Keys.Back:
                    if(!QTUtility.IsXP) {
                        if(listView.HasFocus()) {
                            if(!fRepeat) {
                                if(Config.Tweaks.BackspaceUpLevel) {
                                    QTUtility2.log("QTTabBarClass BackspaceUpLevel UpOneLevel");
                                    UpOneLevel();
                                }
                                else {
                                    NavigateCurrentTab(true);
                                }
                            }
                            return true;
                        }
                    }
                    return false;

                case Keys.Alt | Keys.Left:
                    NavigateCurrentTab(true);
                    return true;

                case Keys.Alt | Keys.Right:
                    NavigateCurrentTab(false);
                    return true;

                case Keys.Alt | Keys.F4:  // ��ݼ���ʽ�رմ���
                    if(!fRepeat) {
                        using (RegistryKey key1 = Registry.CurrentUser.CreateSubKey(RegConst.Root))
                        {
                            string[] list = (from QTabItem item2 in tabControl1.TabPages
                                             where item2.TabLocked
                                             select item2.CurrentPath).ToArray();
                           //  MessageBox.Show(String.Join(",", list));
                            QTUtility2.WriteRegBinary(list, "TabsLocked", key1);
                        }
                        WindowUtils.CloseExplorer(ExplorerHandle, 1);
                    }
                    return true;

                case Keys.F2:
                    if(!Config.Tweaks.F2Selection) {
                        listView.HandleF2();
                    }
                    return false;

            }

            // Ctrl+number = switch to tab #n
            if(((Keys.Control | Keys.NumPad0) <= mkey && mkey <= (Keys.Control | Keys.NumPad9)) ||
                    ((Keys.Control | Keys.D0) <= mkey && mkey <= (Keys.Control | Keys.D9))) {
                int digit;
                if(mkey >= (Keys.Control | Keys.NumPad0)) {
                    digit = (mkey - (Keys.Control | Keys.NumPad0));
                }
                else {
                    digit = (mkey - (Keys.Control | Keys.D0));
                }
                if(digit == 0) {
                    digit = 10;
                }
                if(tabControl1.TabCount >= digit) {
                    tabControl1.SelectTab(digit - 1);
                }
                return true;
            }

            // Check for hotkeys
            int imkey = (int)mkey | QTUtility.FLAG_KEYENABLED;
            for(int i = 0; i < Config.Keys.Shortcuts.Length; ++i) {
                if(Config.Keys.Shortcuts[i] == imkey) {
                    QTUtility2.log("QTTabBarClass imkey " + (BindAction)i);
                    return DoBindAction((BindAction)i);
                }
            }

            // Check for plugin hotkeys.
            foreach(var pair in Config.Keys.PluginShortcuts) {
                int idx = Array.IndexOf(pair.Value, imkey);
                if(idx == -1) continue;
                Plugin plugin;
                if(!pluginServer.TryGetPlugin(pair.Key, out plugin)) return false;
                try {
                    plugin.Instance.OnShortcutKeyPressed(idx);
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception,
                            ExplorerHandle, plugin.PluginInformation.Name,
                            "On shortcut key pressed. Index is " + idx);
                }
                return true;
            }

            // todo: apps and groups should use hash tables.
            if(!fRepeat) {
                // Check for app hotkeys
                foreach(UserApp app in AppsManager.UserApps.Where(a => a.ShortcutKey == mkey)) {
                    AppsManager.Execute(app, ShellBrowser);
                    return true;
                }

                // Check for group hotkey
                foreach(Group g in GroupsManager.Groups.Where(g => g.ShortcutKey == mkey)) {
                    OpenGroup(g.Name, false);
                    return true;
                }
            }

            // This is important I guess?  Not sure
            if(mkey == (Keys.Control | Keys.W)) return true;

            return false;
        }

        // TODO
        private void HandleLBUTTON_Tree(MSG msg, bool fMouseDown) {
            IntPtr ptr;
            if(ShellBrowser.IsFolderTreeVisible(out ptr) && msg.hwnd == ptr) {
                TVHITTESTINFO structure = new TVHITTESTINFO {pt = QTUtility2.PointFromLPARAM(msg.lParam)};
                IntPtr wParam = PInvoke.SendMessage(ptr, 0x1111, IntPtr.Zero, ref structure);
                if(wParam != IntPtr.Zero) {
                    bool flag;
                    if(fMouseDown) {
                        flag = (((structure.flags != 1) && (structure.flags != 0x10)) && ((structure.flags & 2) == 0)) && ((structure.flags & 4) == 0);
                    }
                    else {
                        flag = ((structure.flags & 2) != 0) || ((structure.flags & 4) != 0);
                    }
                    if(flag) {
                        int num = (int)PInvoke.SendMessage(ptr, 0x1127, wParam, (IntPtr)2);
                        if((num & 2) == 0) {
                            NavigatedByCode = fNowTravelByTree = true;
                        }
                    }
                }
            }
        }        

        private bool HandleMOUSEWHEEL(IntPtr lParam) {
            if(!IsHandleCreated) {
                return false;
            }
            MOUSEHOOKSTRUCTEX mousehookstructex = (MOUSEHOOKSTRUCTEX)Marshal.PtrToStructure(lParam, typeof(MOUSEHOOKSTRUCTEX));
            int y = mousehookstructex.mouseData >> 0x10;
            IntPtr handle = PInvoke.WindowFromPoint(mousehookstructex.mhs.pt);
            Control control = FromHandle(handle);
            bool flag = false;
            if(control != null) {
                IntPtr ptr2;
                DropDownMenuReorderable reorderable = control as DropDownMenuReorderable;
                if(reorderable != null) {
                    if(reorderable.CanScroll) {
                        PInvoke.SendMessage(handle, WM.MOUSEWHEEL, QTUtility2.Make_LPARAM(0, y), QTUtility2.Make_LPARAM(mousehookstructex.mhs.pt));
                    }
                    return true;
                }
                flag = (control == tabControl1) || (handle == Handle);
                if(!flag && InstanceManager.TryGetButtonBarHandle(ExplorerHandle, out ptr2)) {
                    flag = (handle == ptr2) || (handle == listView.Handle); // TODO make sure this didn't break
                }
            }
            if(!flag) {
                Keys modifierKeys = ModifierKeys;
                if((QTUtility.IsXP && modifierKeys == Keys.Control) ||
                        (Config.Tweaks.HorizontalScroll && modifierKeys == Keys.Shift)) {
                    if(listView.MouseIsOverListView()) {
                        switch(modifierKeys) {
                            case Keys.Shift:
                                listView.ScrollHorizontal(y);
                                return true;

                            case Keys.Control:
                                ChangeViewMode(y > 0);
                                return true;
                        }
                    }
                }
                return false;
            }
            if(((tabControl1.TabCount < 2) || (ExplorerHandle != PInvoke.GetForegroundWindow())) || Explorer.Busy) {
                return false;
            }
            int selectedIndex = tabControl1.SelectedIndex;
            if(y < 0) {
                if(selectedIndex == (tabControl1.TabCount - 1)) {
                    tabControl1.SelectedIndex = 0;
                }
                else {
                    tabControl1.SelectedIndex = selectedIndex + 1;
                }
            }
            else if(selectedIndex < 1) {
                tabControl1.SelectedIndex = tabControl1.TabCount - 1;
            }
            else {
                tabControl1.SelectedIndex = selectedIndex - 1;
            }
            return true;
        }

        // todo: clean this crap up...
        private bool HandleItemActivate(Keys modKeys, bool fEnqExec) {
            IntPtr zero = IntPtr.Zero;
            IntPtr ppidl = IntPtr.Zero;
            try {
                Address[] addressArray;
                IDLWrapper wrapper1;
                bool fOpenFirstInTab;
                string str;
                if(ShellBrowser.TryGetSelection(out addressArray, out str, false) && (addressArray.Length > 0)) {
                    List<Address> list = new List<Address>(addressArray);
                    wrapper1 = new IDLWrapper(list[0]);
                    list.RemoveAt(0);
                    addressArray = list.ToArray();
                    fOpenFirstInTab = (addressArray.Length > 0) || (modKeys == Keys.Shift);
                }
                else {
                    return false;
                }
                using(IDLWrapper wrapper = wrapper1) {
                    if((wrapper.Available && wrapper.HasPath) && wrapper.IsReadyIfDrive) {
                        if(wrapper.IsFolder) {
                            if(modKeys == Keys.Control) {
                                if(!wrapper.IsLinkToDeadFolder) {
                                    StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addressArray, wrapper.Path));
                                    OpenNewWindow(wrapper);
                                }
                                else {
                                    QTUtility.SoundPlay();
                                }
                            }
                            else if(modKeys == (Keys.Alt | Keys.Control | Keys.Shift)) {
                                DirectoryInfo info = new DirectoryInfo(wrapper.Path);
                                if(info.Exists) {
                                    DirectoryInfo[] directories = info.GetDirectories();
                                    if((directories.Length + tabControl1.TabCount) < 0x41) {
                                        tabControl1.SetRedraw(false);
                                        foreach(DirectoryInfo info2 in directories) {
                                            if(info2.Name != "System Volume Information") {
                                                using(IDLWrapper wrapper2 = new IDLWrapper(info2.FullName)) {
                                                    if(wrapper2.Available && (!wrapper2.IsLink || Directory.Exists(ShellMethods.GetLinkTargetPath(info2.FullName)))) {
                                                        // MessageBox.Show("Open New Tab");
                                                        OpenNewTab(wrapper2, true);
                                                    }
                                                }
                                            }
                                        }
                                        tabControl1.SetRedraw(true);
                                    }
                                    else {
                                        QTUtility.SoundPlay();
                                    }
                                }
                            }
                            else {
                                if(addressArray.Length > 1) {
                                    tabControl1.SetRedraw(false);
                                }
                                try {
                                    if(fOpenFirstInTab) {
                                        OpenNewTab(wrapper, (modKeys & Keys.Shift) == Keys.Shift);
                                    }
                                    else if(!wrapper.IsFileSystemFile) {
                                        ShellBrowser.Navigate(wrapper);
                                    }
                                    else {
                                        return false;
                                    }
                                    for(int i = 0; i < addressArray.Length; i++) {
                                        using(IDLWrapper wrapper3 = new IDLWrapper(addressArray[i].ITEMIDLIST)) {
                                            if(((wrapper3.Available && wrapper3.HasPath) && (wrapper3.IsReadyIfDrive && wrapper3.IsFolder)) && !wrapper3.IsLinkToDeadFolder) {
                                                string path = wrapper3.Path;
                                                if(((path != wrapper.Path) && (path.Length > 0)) && !QTUtility2.IsShellPathButNotFileSystem(path)) {
                                                    OpenNewTab(wrapper3, true);
                                                }
                                            }
                                        }
                                    }
                                }
                                finally {
                                    if(addressArray.Length > 1) {
                                        tabControl1.SetRedraw(true);
                                    }
                                }
                            }
                            return true;
                        }
                        if(wrapper.IsLink) {
                            using(IDLWrapper wrapper4 = new IDLWrapper(ShellMethods.GetLinkTargetIDL(wrapper.Path))) {
                                if(((wrapper4.Available && wrapper4.HasPath) && (wrapper4.IsReadyIfDrive && wrapper4.IsFolder)) && !wrapper.IsLinkToDeadFolder) {
                                    if(modKeys == Keys.Control) {
                                        StaticReg.CreateWindowPaths.AddRange(CreateTMPPathsToOpenNew(addressArray, wrapper.Path));
                                        OpenNewWindow(wrapper4);
                                    }
                                    else {
                                        if(fOpenFirstInTab) {
                                            OpenNewTab(wrapper4, (modKeys & Keys.Shift) == Keys.Shift);
                                        }
                                        else {
                                            ShellBrowser.Navigate(wrapper4);
                                        }
                                        for(int j = 0; j < addressArray.Length; j++) {
                                            using(IDLWrapper wrapper5 = new IDLWrapper(addressArray[j].ITEMIDLIST)) {
                                                if(((wrapper5.Available && wrapper5.HasPath) && (wrapper5.IsReadyIfDrive && wrapper5.IsFolder)) && !wrapper5.IsLinkToDeadFolder) {
                                                    string str3 = wrapper5.Path;
                                                    if(((str3 != wrapper4.Path) && (str3.Length > 0)) && !QTUtility2.IsShellPathButNotFileSystem(str3)) {
                                                        OpenNewTab(wrapper5, true);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    return true;
                                }
                            }
                        }
                        if(fEnqExec) {
                            List<string> list2 = new List<string>();
                            list2.Add(wrapper.Path);
                            foreach(Address address in addressArray) {
                                using(IDLWrapper wrapper6 = new IDLWrapper(address.ITEMIDLIST)) {
                                    if(wrapper6.IsFolder) {
                                        return true;
                                    }
                                    if(wrapper6.HasPath && !wrapper6.IsLinkToDeadFolder) {
                                        list2.Add(wrapper6.Path);
                                    }
                                }
                            }
                            foreach(string str4 in list2) {
                                StaticReg.ExecutedPathsList.Add(str4);
                            }
                        }
                    }
                }
            }
            catch(Exception exception) {
                QTUtility2.MakeErrorLog(exception);
            }
            finally {
                if(zero != IntPtr.Zero) {
                    PInvoke.CoTaskMemFree(zero);
                }
                if(ppidl != IntPtr.Zero) {
                    PInvoke.CoTaskMemFree(ppidl);
                }
            }
            return false;
        }

        private void HideSubDirTip_Tab_Menu() {
            _tabManager.HideSubDirTip_Tab_Menu();
        }
        private void HideTabSwitcher(bool fSwitch) {
            _tabManager.HideTabSwitcher(fSwitch);
        }
        private void HideToolTipForDD() {
            _tabManager.HideToolTipForDD();
        }
        private void InitializeComponent() {
            // // AutoScaleMode.Dpi  / by indiff dpi
            // AutoScaleMode = AutoScaleMode.Dpi;
            components = new Container();
            /*
             �ṩ������ ToolStripDropDown��ToolStripDropDownButton �� ToolStripMenuItem �ؼ�ʱ����ʾ ToolStripSplitButton �Ŀؼ��Ļ������ܡ�
             ��ť������
             */
            buttonNavHistoryMenu = new ToolStripDropDownButton();
            // ���ڷ��ñ�ǩ��
            tabControl1 = new QTabControl();
            // ��ǰ�ı�ǩ
            CurrentTab = new QTabItem(string.Empty, string.Empty, tabControl1);
            contextMenuTab = new ContextMenuStripEx(components, false);
            contextMenuSys = new ContextMenuStripEx(components, false);
            tabControl1.SuspendLayout();
            contextMenuSys.SuspendLayout();
            contextMenuTab.SuspendLayout();
            SuspendLayout();

            // �ж��Ƿ���ʾ��ť������
            _explorerControllerModule = new ExplorerControllerModule(this);
            bool flag = Config.Tabs.ShowNavButtons;
            if (flag)
            {
                InitializeNavBtns(false);
            }

            buttonNavHistoryMenu.AutoSize = false;
            buttonNavHistoryMenu.DisplayStyle = ToolStripItemDisplayStyle.None;
            buttonNavHistoryMenu.Enabled = false;
            buttonNavHistoryMenu.Size = new Size(13, 0x15);
            buttonNavHistoryMenu.DropDown = new DropDownMenuBase(components, true, true, true);
            buttonNavHistoryMenu.DropDown.ItemClicked += _explorerControllerModule.NavigationButton_DropDownMenu_ItemClicked;
            buttonNavHistoryMenu.DropDownOpening += _explorerControllerModule.NavigationButtons_DropDownOpening;
            buttonNavHistoryMenu.DropDown.ImageList = QTUtility.ImageListGlobal;
            
            
            tabControl1.SetRedraw(false);
            // ���ӵ�ǰ��ǩ
            tabControl1.TabPages.Add(CurrentTab);
            tabControl1.Dock = DockStyle.Fill;
            tabControl1.ContextMenuStrip = contextMenuTab;
            tabControl1.RefreshOptions(true);
            _tabManager = new TabManager(this);
            _menuController = new MenuController(this);
            tabControl1.RowCountChanged += tabControl1_RowCountChanged;
            tabControl1.Deselecting += _tabManager.tabControl1_Deselecting;
            tabControl1.Selecting += _tabManager.tabControl1_Selecting;
            tabControl1.SelectedIndexChanged += _tabManager.tabControl1_SelectedIndexChanged;
            tabControl1.GotFocus += Controls_GotFocus;
            tabControl1.MouseEnter += _tabManager.tabControl1_MouseEnter;
            tabControl1.MouseLeave += _tabManager.tabControl1_MouseLeave;
            tabControl1.MouseDown += _tabManager.tabControl1_MouseDown;
            tabControl1.MouseUp += _tabManager.tabControl1_MouseUp;
            tabControl1.MouseMove += _tabManager.tabControl1_MouseMove;
            tabControl1.MouseDoubleClick += _tabManager.tabControl1_MouseDoubleClick;
            tabControl1.ItemDrag += _tabManager.tabControl1_ItemDrag;
            tabControl1.PointedTabChanged += _tabManager.tabControl1_PointedTabChanged;
            tabControl1.TabCountChanged += _tabManager.tabControl1_TabCountChanged;
            tabControl1.CloseButtonClicked += _tabManager.tabControl1_CloseButtonClicked;
            tabControl1.TabIconMouseDown += _tabManager.tabControl1_TabIconMouseDown;
            // ע����ɫ������ť�ĵ���¼�
            tabControl1.PlusButtonClicked += _tabManager.tabControl1_PlusButtonClicked;
            
            contextMenuTab.Items.Add(new ToolStripMenuItem());
            contextMenuTab.ShowImageMargin = false;
            contextMenuTab.ItemClicked += _menuController.contextMenuTab_ItemClicked;
            contextMenuTab.Opening += _menuController.contextMenuTab_Opening;
            contextMenuTab.Closed += _tabManager.contextMenuTab_Closed;
            contextMenuSys.Items.Add(new ToolStripMenuItem());
            contextMenuSys.ShowImageMargin = false;
            contextMenuSys.ItemClicked += _menuController.contextMenuSys_ItemClicked;
            contextMenuSys.Opening += _menuController.contextMenuSys_Opening;
            Controls.Add(tabControl1);
            if(flag) {
                Controls.Add(toolStrip);
            }
            MinSize = new Size(150, Config.Skin.TabHeight + 2);
            Height = Config.Skin.TabHeight + 2;
            ContextMenuStrip = contextMenuSys;
            // ע�����˫���¼�
            MouseDoubleClick += _tabManager.QTTabBarClass_MouseDoubleClick;
            MouseUp += _tabManager.QTTabBarClass_MouseUp;
            tabControl1.ResumeLayout(false);
            contextMenuSys.ResumeLayout(false);
            contextMenuTab.ResumeLayout(false);
            if(flag) {
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            ResumeLayout(false);
        }

        private void InitializeInstallation() {
            _explorerControllerModule.InitializeInstallation();
        }
        /**
         * ��ʼ��������
         */
        private void InitializeNavBtns(bool fSync) {
            toolStrip = new ToolStripClasses();
            buttonBack = new ToolStripButton();
            buttonForward = new ToolStripButton();
            toolStrip.SuspendLayout();
            if(!QTUtility.ImageGlobalContainsKey("navBack")) {
                QTUtility.AddImageToGlobal("navBack", Resources_Image.imgNavBack);
            }
            if(!QTUtility.ImageGlobalContainsKey("navFrwd")) {
                QTUtility.AddImageToGlobal("navFrwd", Resources_Image.imgNavFwd);
            }
            toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
            toolStrip.AutoSize = false;
            toolStrip.CanOverflow = false;
            toolStrip.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.Items.AddRange(new ToolStripItem[] { buttonBack, buttonForward, buttonNavHistoryMenu });
            toolStrip.Renderer = new ToolbarRenderer();
            toolStrip.Width = 0x3f;
            toolStrip.TabStop = false;
            
            // dark mode ?  by indiff ����ı���ɫ
            toolStrip.BackColor = QTUtility.InNightMode ? Color.Black : Color.WhiteSmoke;
            /*if (QTUtility.InNightMode)
            {
                toolStrip.BackColor = Color.Black;
            }
            else
            {
                toolStrip.BackColor = SystemColors.Window;
            }*/

            buttonBack.AutoSize = false;
            buttonBack.DisplayStyle = ToolStripItemDisplayStyle.Image;
            buttonBack.Enabled = fSync ? ((navBtnsFlag & 1) != 0) : false;
            buttonBack.Image = QTUtility.GetImageFromGlobal("navBack");
            buttonBack.Size = new Size(0x15, 0x15);
            buttonBack.Click += _explorerControllerModule.NavigationButtons_Click;
            buttonForward.AutoSize = false;
            buttonForward.DisplayStyle = ToolStripItemDisplayStyle.Image;
            buttonForward.Enabled = fSync ? ((navBtnsFlag & 2) != 0) : false;
            buttonForward.Image = QTUtility.GetImageFromGlobal("navFrwd");
            buttonForward.Size = new Size(0x15, 0x15);
            buttonForward.Click += _explorerControllerModule.NavigationButtons_Click;
        }
        /**
         * ��ʼ���Ѿ��򿪵Ĵ���
         */
        private void InitializeOpenedWindow() {
            IsShown = true;
            InstanceManager.PushTabBarInstance(this);
            // P0-5: register this tab bar as the main UI control so IPC callbacks
            // are marshaled onto its (UI) thread.
            InstanceManager.SetMainUIControl(this);
            //  ��װ����
            QTUtility2.log("QTTabBarClass InitializeOpenedWindow  InstallHooks");
            InstallHooks();

            // ��������췽��
            QTUtility2.log("QTTabBarClass  PluginServer ");
            pluginServer = new PluginServer(this);
            
            // ����������
            QTUtility2.log("QTTabBarClass TryCallButtonBar ");
            if (!TryCallButtonBar(bbar => { return bbar.CreateItems(); }))
            {
                // Try again in 2 seconds
                Timer timer = new Timer { Interval = 2000 };
                timer.Tick += (sender, args) => {
                    QTUtility2.log("QTTabBarClass timer.Tick TryCallButtonBar ");
                    TryCallButtonBar(bbar => {return bbar.CreateItems();});
                    timer.Stop();
                };
                timer.Start();
            }
            if(QTUtility.WindowAlpha < 0xff) {
                QTUtility2.log("QTTabBarClass SetWindowLongPtr SetLayeredWindowAttributes");
                PInvoke.SetWindowLongPtr(ExplorerHandle, -20, PInvoke.Ptr_OP_OR(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 0x80000));
                PInvoke.SetLayeredWindowAttributes(ExplorerHandle, 0, QTUtility.WindowAlpha, 2);
            }

            QTUtility2.log("QTTabBarClass ListViewMonitor ");
            listViewManager = new ListViewMonitor(ShellBrowser, ExplorerHandle, Handle);
            listViewManager.ListViewChanged += ListViewMonitor_ListViewChanged;
            listViewManager.Initialize();

            IntPtr hwndBreadcrumbBar = WindowUtils.FindChildWindow(ExplorerHandle, hwnd => PInvoke.GetClassName(hwnd) == "Breadcrumb Parent");
            if(hwndBreadcrumbBar != IntPtr.Zero) {
                hwndBreadcrumbBar = PInvoke.FindWindowEx(hwndBreadcrumbBar, IntPtr.Zero, "ToolbarWindow32", null);
                if(hwndBreadcrumbBar != IntPtr.Zero) {
                    breadcrumbBar = new BreadcrumbBar(hwndBreadcrumbBar);
                    QTUtility2.log("QTTabBarClass BreadcrumbBar set FolderLinkClicked ");
                    breadcrumbBar.ItemClicked += FolderLinkClicked;
                }
            }
            // SysTreeView32
        }

        private static void InitializeStaticFields() {
            fInitialized = true;
            // ����DPI���� indiff
            PInvoke.SetProcessDPIAware();
            Application.EnableVisualStyles();
        }

        private void InitializeSysMenu(bool fText) {
            bool flag = false;
            if(tsmiGroups == null) {
                flag = true;
                // ��ʼ��ϵͳ������
                // ��ǩ��
                tsmiGroups = new ToolStripMenuItem(QTUtility.ResMain[12]);
                // ����ر�
                tsmiUndoClose = new ToolStripMenuItem(QTUtility.ResMain[13]);
                // ����ر�
                tsmiLastActiv = new ToolStripMenuItem(QTUtility.ResMain[14]);
                tsmiExecuted = new ToolStripMenuItem(QTUtility.ResMain[15]);
                tsmiBrowseFolder = new ToolStripMenuItem(QTUtility.ResMain[0x10] + "...");
                tsmiCloseAllButCurrent = new ToolStripMenuItem(QTUtility.ResMain[0x11]);
                tsmiCloseWindow = new ToolStripMenuItem(QTUtility.ResMain[0x12]);
                tsmiOption = new ToolStripMenuItem(QTUtility.ResMain[0x13]);
                tsmiLockToolbar = new ToolStripMenuItem(QTUtility.ResMain[0x20]);
                tsmiMergeWindows = new ToolStripMenuItem(QTUtility.ResMain[0x21]);
                tssep_Sys1 = new ToolStripSeparator();
                tssep_Sys2 = new ToolStripSeparator();
                if (contextMenuSys != null)
                {
                    contextMenuSys.SuspendLayout();
                    contextMenuSys.Items[0].Dispose();
                    contextMenuSys.Items.AddRange(new ToolStripItem[]
                    {
                        tsmiGroups, tsmiUndoClose, tsmiLastActiv, tsmiExecuted, 
                        tssep_Sys1, tsmiBrowseFolder, tsmiCloseAllButCurrent, tsmiCloseWindow, 
                        tsmiMergeWindows, tsmiLockToolbar, tssep_Sys2, tsmiOption
                    });
                }

                DropDownMenuReorderable reorderable = new DropDownMenuReorderable(components, true, false);
                reorderable.ReorderFinished += menuitemGroups_ReorderFinished;
                reorderable.ItemRightClicked += MenuUtility.GroupMenu_ItemRightClicked;
                reorderable.ItemMiddleClicked += ddrmrGroups_ItemMiddleClicked;
                reorderable.ImageList = QTUtility.ImageListGlobal;
                tsmiGroups.DropDown = reorderable;
                tsmiGroups.DropDownItemClicked += menuitemGroups_DropDownItemClicked;
                DropDownMenuReorderable reorderable2 = new DropDownMenuReorderable(components);
                reorderable2.ReorderEnabled = false;
                reorderable2.MessageParent = Handle;
                reorderable2.ImageList = QTUtility.ImageListGlobal;
                reorderable2.ItemRightClicked += ddmrUndoClose_ItemRightClicked;
                tsmiUndoClose.DropDown = reorderable2;
                tsmiUndoClose.DropDownItemClicked += _tabManager.menuitemUndoClose_DropDownItemClicked;
                DropDownMenuReorderable reorderable3 = new DropDownMenuReorderable(components);
                reorderable3.MessageParent = Handle;
                reorderable3.ItemRightClicked += menuitemExecuted_ItemRightClicked;
                reorderable3.ItemClicked += menuitemExecuted_DropDownItemClicked;
                reorderable3.ImageList = QTUtility.ImageListGlobal;
                tsmiExecuted.DropDown = reorderable3;
                tssep_Sys1.Enabled = false;
                tssep_Sys2.Enabled = false;
                if (contextMenuSys != null)
                {
                    contextMenuSys.ResumeLayout(false);
                }
            }
            if(!flag && fText) {
                tsmiGroups.Text = QTUtility.ResMain[12];
                tsmiUndoClose.Text = QTUtility.ResMain[13];
                tsmiLastActiv.Text = QTUtility.ResMain[14];
                tsmiExecuted.Text = QTUtility.ResMain[15];
                tsmiBrowseFolder.Text = QTUtility.ResMain[0x10] + "...";
                tsmiCloseAllButCurrent.Text = QTUtility.ResMain[0x11];
                tsmiCloseWindow.Text = QTUtility.ResMain[0x12];
                tsmiOption.Text = QTUtility.ResMain[0x13];
                tsmiLockToolbar.Text = QTUtility.ResMain[0x20];
                tsmiMergeWindows.Text = QTUtility.ResMain[0x21];
            }
        }

        // ��ʼ����ǩ���Ҽ��˵�
        private void InitializeTabMenu(bool fText) {
            try
            {
               // MessageBox.Show( "InitializeTabMenu");
                bool flag = false;
                if (tsmiClose == null)
                {
                    flag = true;
                    tsmiClose = new ToolStripMenuItem(QTUtility.ResMain[0]);
                    tsmiCloseRight = new ToolStripMenuItem(QTUtility.ResMain[1]);
                    tsmiCloseLeft = new ToolStripMenuItem(QTUtility.ResMain[2]);
                    tsmiCloseAllButThis = new ToolStripMenuItem(QTUtility.ResMain[3]);
                    tsmiAddToGroup = new ToolStripMenuItem(QTUtility.ResMain[4]);
                    tsmiCreateGroup = new ToolStripMenuItem(QTUtility.ResMain[5] + "...");
                    tsmiLockThis = new ToolStripMenuItem(QTUtility.ResMain[6]);
                    tsmiCloneThis = new ToolStripMenuItem(QTUtility.ResMain[7]);
                    tsmiCreateWindow = new ToolStripMenuItem(QTUtility.ResMain[8]);
                    tsmiCopy = new ToolStripMenuItem(QTUtility.ResMain[9]);
                    tsmiProp = new ToolStripMenuItem(QTUtility.ResMain[10]);
                    tsmiHistory = new ToolStripMenuItem(QTUtility.ResMain[11]);
                    tsmiTabOrder = new ToolStripMenuItem(QTUtility.ResMain[0x1c]);

                    /** add by qwop 2012-07-13.*/
                    int len = QTUtility.ResMain.Length;
                    tsmiOpenCmd = new ToolStripMenuItem(QTUtility.ResMain[len - 1]);
                    enableApiHook = new ToolStripMenuItem("Enable Image Hook");
                    /** add by qwop 2012-07-13.*/


                    menuTextBoxTabAlias = new ToolStripTextBox();
                    tssep_Tab1 = new ToolStripSeparator();
                    tssep_Tab2 = new ToolStripSeparator();
                    tssep_Tab3 = new ToolStripSeparator();
                    contextMenuTab.SuspendLayout();
                    contextMenuTab.Items[0].Dispose();
                    contextMenuTab.Items.AddRange(new ToolStripItem[] { 
                        tsmiClose, tsmiCloseRight, tsmiCloseLeft, tsmiCloseAllButThis, 
                        tssep_Tab1, tsmiAddToGroup, tsmiCreateGroup, tssep_Tab2, tsmiLockThis, 
                        tsmiCloneThis, tsmiCreateWindow, tsmiCopy, tsmiTabOrder, tssep_Tab3, tsmiProp,
                        tsmiHistory,
                        tsmiOpenCmd,
                        // enableApiHook
                    });

                    // ���ñ�ǩ����϶��¼� by indiff group drag
                    tsmiAddToGroup.DragDrop += (sender, e) =>
                    {
                        NowTabDragging = true;
                        var dataObject = e.Data;
                        QTUtility2.log("e.Data: " + dataObject);

                        NowTabDragging = false;
                    };

                    // ���ñ�ǩ��ĵ���¼� comment by indiff
                    tsmiAddToGroup.DropDownItemClicked += menuitemAddToGroup_DropDownItemClicked;
                    (tsmiAddToGroup.DropDown).ImageList = QTUtility.ImageListGlobal;
                    tsmiHistory.DropDown = new DropDownMenuBase(components, true, true, true);
                    tsmiHistory.DropDownItemClicked += menuitemHistory_DropDownItemClicked;
                    (tsmiHistory.DropDown).ImageList = QTUtility.ImageListGlobal;
                    menuTextBoxTabAlias.Text = menuTextBoxTabAlias.ToolTipText = QTUtility.ResMain[0x1b];
                    menuTextBoxTabAlias.GotFocus += _tabManager.menuTextBoxTabAlias_GotFocus;
                    menuTextBoxTabAlias.LostFocus += _tabManager.menuTextBoxTabAlias_LostFocus;
                    menuTextBoxTabAlias.KeyPress += _tabManager.menuTextBoxTabAlias_KeyPress;
                    tsmiTabOrder.DropDown = new ContextMenuStripEx(components, false);
                    tssep_Tab1.Enabled = false;
                    tssep_Tab2.Enabled = false;
                    tssep_Tab3.Enabled = false;
                    contextMenuTab.ResumeLayout(false);
                }
                if (!flag && fText)
                {
                    tsmiClose.Text = QTUtility.ResMain[0];
                    tsmiCloseRight.Text = QTUtility.ResMain[1];
                    tsmiCloseLeft.Text = QTUtility.ResMain[2];
                    tsmiCloseAllButThis.Text = QTUtility.ResMain[3];
                    tsmiAddToGroup.Text = QTUtility.ResMain[4];
                    tsmiCreateGroup.Text = QTUtility.ResMain[5] + "...";
                    tsmiLockThis.Text = QTUtility.ResMain[6];
                    tsmiCloneThis.Text = QTUtility.ResMain[7];
                    tsmiCreateWindow.Text = QTUtility.ResMain[8];
                    tsmiCopy.Text = QTUtility.ResMain[9];
                    tsmiProp.Text = QTUtility.ResMain[10];
                    tsmiHistory.Text = QTUtility.ResMain[11];
                    tsmiTabOrder.Text = QTUtility.ResMain[0x1c];
                    menuTextBoxTabAlias.Text = menuTextBoxTabAlias.ToolTipText = QTUtility.ResMain[0x1b];
                }
            }
            catch (Exception e) {
                QTUtility2.MakeErrorLog(e);
            }
        }

        // ��װ����
        private void InstallHooks() {
            hookProc_Key = new HookProc(CallbackKeyboardProc);
            hookProc_Mouse = new HookProc(CallbackMouseProc);
            hookProc_GetMsg = new HookProc(CallbackGetMsgProc);
            int currentThreadId = PInvoke.GetCurrentThreadId();
            hHook_Key = PInvoke.SetWindowsHookEx(2, hookProc_Key, IntPtr.Zero, currentThreadId);
            hHook_Mouse = PInvoke.SetWindowsHookEx(7, hookProc_Mouse, IntPtr.Zero, currentThreadId);
            hHook_Msg = PInvoke.SetWindowsHookEx(3, hookProc_GetMsg, IntPtr.Zero, currentThreadId);
            explorerController = new NativeWindowController(ExplorerHandle);
            explorerController.MessageCaptured += _explorerControllerModule.explorerController_MessageCaptured;
            if(ReBarHandle != IntPtr.Zero) {
                rebarController = new RebarController(this, ReBarHandle, BandObjectSite as IOleCommandTarget);
            }
            if(!QTUtility.IsXP) {
                TravelToolBarHandle = GetTravelToolBarWindow32();
                if(TravelToolBarHandle != IntPtr.Zero) {
                    travelBtnController = new NativeWindowController(TravelToolBarHandle);
                    travelBtnController.MessageCaptured += travelBtnController_MessageCaptured;
                }
            }
            dropTargetWrapper = new DropTargetWrapper(this);
            dropTargetWrapper.DragFileEnter += dropTargetWrapper_DragFileEnter;
            dropTargetWrapper.DragFileOver += dropTargetWrapper_DragFileOver;
            dropTargetWrapper.DragFileLeave += dropTargetWrapper_DragFileLeave;
            dropTargetWrapper.DragFileDrop += dropTargetWrapper_DragFileDrop;
        }



        

        private void ListView_ItemCountChanged(int count) {
            TryCallButtonBar(bbar => { return bbar.RefreshStatusText(); });
        }

        private bool ListView_SelectionActivated(Keys modKeys) {
            QTUtility2.log("ListView_SelectionActivated");
            if(timerSelectionChanged != null) {
                timerSelectionChanged.Enabled = false;
            }
            int num = ShellBrowser.GetSelectedCount();
            bool fEnqExec = Config.Misc.KeepRecentFiles;
            return (fEnqExec || num != 1 || (modKeys != Keys.None && modKeys != Keys.Alt)) &&
                    HandleItemActivate(modKeys, fEnqExec);
        }

        /// <summary>
        /// ��ͼѡ���¼�����
        /// ���ӻ�ȡѡ���ļ�����
        /// </summary>
        private void ListView_SelectionChanged(/*object sender, SelectionChangedEventArgs e*/)
        {
            // QTUtility2.log("ListView_SelectionChanged e.AddedItems " + e.AddedItems);
            // QTUtility2.log("ListView_SelectionChanged e.OriginalSource " + e.OriginalSource);
            if(pluginServer != null && pluginServer.SelectionChangedAttached) {
                if(timerSelectionChanged == null) {
                    timerSelectionChanged = new Timer(components);
                    timerSelectionChanged.Interval = 250;
                    timerSelectionChanged.Tick += timerSelectionChanged_Tick;
                    timerSelectionChanged.Enabled = true; // moved by indiff
                }
                else {
                    timerSelectionChanged.Enabled = false;
                }
                // timerSelectionChanged.Enabled = true;
            }

            // ��ȡѡ���ļ�����
            try
            {
                // var selectedCount = ShellBrowser.GetSelectedCount();
                var tabText = tabControl1.TabPages[0].Text;
                QTUtility2.log("ListView_SelectionChanged this.TabCount " + this.TabCount +
                               " fHideExplorer " + fHideExplorer +
                               // " selectedCount " + selectedCount +
                               " mCmdType " + mCmdType +
                               " tabItem Text " + tabText
                );

                // other cmd ��ʽ�ᱨ��  �Ҳ���Ԫ�ء� (�쳣���� HRESULT:0x80070490)
                if (this.TabCount == 1 && // ��ǰ���񵽵��½�����ֻ��һ������
                    fHideExplorer && // ��Ҫ�����˳��Ĵ���
                    (mCmdType == 2) && // factory ���� // || mCmdType == 3
                    Config.Window.CaptureWeChatSelection && // �Ƿ������˽��в���΢��ѡ�е��ļ�
                    QTUtility2.IsEmpty(tabText) // ��ǩ�ı��ǿյ�
                   )
                {
                    try
                    {
                        // var tabItem = tabControl1.TabPages[0];
                        IShellView shellView = null;
                        if (0 == ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView))
                        {
                            var iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                            object ppv;
                            // QTTabBarLib.Common.HResult hr = shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                            var hr = shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                            if (hr == QTTabBarLib.Common.HResult.Ok)
                            {
                                if (ppv != null)
                                {
                                    IDataObject pDataObject = (IDataObject)ppv;
                                    var shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                                    if (shellObjectCollection.Count > 0)
                                    {
                                        string key = "";
                                        foreach (ShellObject so in shellObjectCollection)
                                        {
                                            FileInfo info = new FileInfo(so.ParsingName);
                                            if (info.Exists)
                                            {
                                                key = info.Directory.FullName;

                                                if (QTUtility2.IsNotEmpty(key))
                                                {
                                                    RegistryUtil.WriteSelection(key, so.ParsingName);
                                                    QTUtility2.log(
                                                        " WriteSelection " +
                                                        key +
                                                        " path " + so.ParsingName
                                                    );
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }

                                // release com object
                                if (hr != QTTabBarLib.Common.HResult.Ok && null != ppv)
                                {
                                    Marshal.ReleaseComObject(ppv);
                                }
                            }
                        }
                    }
                    finally
                    {
                        try
                        {
                            if (fHideExplorer)
                            {
                                Explorer.Quit();
                                WindowUtils.CloseExplorer(ExplorerHandle, 0);
                            }
                        }
                        catch (Exception e)
                        {
                            QTUtility2.MakeErrorLog(e, "�رմ���");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                QTUtility2.MakeErrorLog(e, "��ȡѡ���ļ�����");
            }
            
            if (
                this.TabCount == 1 &&
                fHideExplorer && // ��Ҫ�����˳��Ĵ���
                (mCmdType == 3) && // other cmd ���� vscode ��visual studio �򿪵Ķ��ǵ����̣� ��һ�Թرյ�ǰ��ǩ
                QTUtility2.IsEmpty(tabControl1.TabPages[0].Text)) // ��ǩ�ı��ǿյ�)
            {
                try
                {
                    // string focushed;
                    // CurrentTab.GetSelectedItemsAt(CurrentAddress, out focushed);
                    // ����ִ�� quit�� ��Ϊ��ͬһ������
                    QTUtility2.log("other cmd close windows  " );
                    // WindowUtils.CloseExplorer(ExplorerHandle, 0);
                    // WindowUtils.CloseExplorer(ExplorerHandle, 1);
                    // WindowUtils.CloseExplorer(ExplorerHandle, 2);
                    WindowUtils.CloseExplorer(ExplorerHandle, 2, true);
                    // WindowUtils.CloseExplorer(ExplorerHandle, 3);
                    // MergeAllWindows();
                }
                finally
                {
                }
            }


            /*if (
                this.TabCount == 1 &&
                fHideExplorer && // ��Ҫ�����˳��Ĵ���
                (mCmdType == 3) && // other cmd ����
                QTUtility2.IsEmpty(tabControl1.TabPages[0].Text)) // ��ǩ�ı��ǿյ�)
            {
                try
                {
                }
                finally
                {
                    try
                    {
                        Explorer.Quit();
                        WindowUtils.CloseExplorer(ExplorerHandle, 0);
                    }
                    catch (Exception e)
                    {
                        QTUtility2.MakeErrorLog(e, "�رմ���");
                    }
                }
            }*/
        }

        private bool ListView_MiddleClick(Point pt) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Middle, ModifierKeys);
            BindAction action;
            if(Config.Mouse.MarginActions.TryGetValue(chord, out action)) {
                QTUtility2.log("ListView_MiddleClick " + action);
                if(listView.PointIsBackground(pt, false)) {
                    return DoBindAction(action);
                }
            }
            if(Config.Mouse.ItemActions.TryGetValue(chord, out action)) {
                int index = listView.HitTest(pt, false);
                if(index <= -1) {
                    return false;
                }
                using(IDLWrapper wrapper = ShellBrowser.GetItem(index)) {
                    QTUtility2.log("QTTabBarClass ListView_MiddleClick " + action);
                    return DoBindAction(action, false, null, wrapper);
                }
            }
            return false;
        }

        private bool ListView_MouseActivate(ref int result) {
            // The purpose of this is to prevent accidentally
            // renaming an item when clicking out of a SubDirTip menu.
            bool ret = false;
            if(listView.SubDirTipMenuIsShowing() || (subDirTip_Tab != null && subDirTip_Tab.MenuIsShowing)) {
                if(ShellBrowser.GetSelectedCount() == 1 && listView.HotItemIsSelected()) {
                    result = 2;
                    listView.HideSubDirTipMenu();
                    HideSubDirTip_Tab_Menu();
                    listView.SetFocus();
                    ret = true;
                }
            }
            listView.RefreshSubDirTip(true);
            return ret;
        }

        private bool ListView_DoubleClick(Point pt) {
            MouseChord chord = QTUtility.MakeMouseChord(MouseChord.Double, ModifierKeys);
            BindAction action;
            if(Config.Mouse.MarginActions.TryGetValue(chord, out action) && listView.PointIsBackground(pt, false)) {
                QTUtility2.log("ListView_DoubleClick " + action);
                DoBindAction(action);
                return true;
            }
            return false;
        }

        private void ListView_EndLabelEdit(LVITEM item) {
            if(item.pszText == IntPtr.Zero) return;
            using(IDLWrapper wrapper = ShellBrowser.GetItem(item.iItem)) {
                if(wrapper.DisplayName != Marshal.PtrToStringUni(item.pszText)) {
                    HandleF5();
                }
            }
        }

        private void ListViewMonitor_ListViewChanged(object sender, EventArgs args) {
            if (listViewManager != null) // �޸���ָ������ by indiff
            {
                listView = listViewManager.CurrentListView;
                ExtendedListViewCommon elvc = listView as ExtendedListViewCommon;
                if (elvc != null)
                {
                    elvc.ItemCountChanged += ListView_ItemCountChanged;
                    elvc.SelectionActivated += ListView_SelectionActivated;
                    elvc.SelectionChanged += ListView_SelectionChanged;
                    elvc.MiddleClick += ListView_MiddleClick;
                    elvc.DoubleClick += ListView_DoubleClick;
                    elvc.EndLabelEdit += ListView_EndLabelEdit;
                    elvc.MouseActivate += ListView_MouseActivate;
                    elvc.SubDirTip_MenuItemClicked += subDirTip_MenuItemClicked;
                    elvc.SubDirTip_MenuItemRightClicked += subDirTip_MenuItemRightClicked;
                    elvc.SubDirTip_MultipleMenuItemsClicked += subDirTip_MultipleMenuItemsClicked;
                    elvc.SubDirTip_MultipleMenuItemsRightClicked += subDirTip_MultipleMenuItemsRightClicked;
                    elvc.RefreshViewWatermark(true);
                }
            }
            HandleF5();
        }
          
        private string MakeTravelBtnTooltipText(bool fBack) {
            string path = string.Empty;
            if(fBack) {
                string[] historyBack = CurrentTab.GetHistoryBack();
                if(historyBack.Length > 1) {
                    path = historyBack[1];
                }
            }
            else {
                string[] historyForward = CurrentTab.GetHistoryForward();
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
        /**
         * ��������ǩ���¼�
         */
        private void menuitemAddToGroup_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            // TODO we should be using tags I think
            string groupName = e.ClickedItem.Text;
            string currentPath = ContextMenuedTab.CurrentPath;
            bool addSame = ModifierKeys == Keys.Control;
            Group g = GroupsManager.GetGroup(groupName);
            if(g == null) return;
            if(addSame || !g.Paths.Any(p => p.PathEquals(currentPath))) {
                g.Paths.Add(currentPath);
                GroupsManager.SaveGroups();
            }
        }

        private void menuitemExecuted_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            try {
                string toolTipText = e.ClickedItem.ToolTipText;
                ProcessStartInfo startInfo = new ProcessStartInfo(toolTipText);
                startInfo.WorkingDirectory = Path.GetDirectoryName(toolTipText);
                startInfo.ErrorDialog = true;
                startInfo.ErrorDialogParentHandle = ExplorerHandle;
                Process.Start(startInfo);
                StaticReg.ExecutedPathsList.Add(toolTipText);
            }
            catch {
                QTUtility.SoundPlay();
            }
        }

        private void menuitemExecuted_ItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            using(IDLWrapper wrapper = new IDLWrapper(e.ClickedItem.ToolTipText)) {
                e.HRESULT = shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((DropDownMenuReorderable)sender).Handle, true);
            }
            if(e.HRESULT == 0xffff) {
                StaticReg.ExecutedPathsList.Remove(e.ClickedItem.ToolTipText);
                e.ClickedItem.Dispose();
            }
        }

        private void menuitemGroups_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            Keys modifierKeys = ModifierKeys;
            string groupName = e.ClickedItem.Text;
            if(modifierKeys == (Keys.Control | Keys.Shift)) {
                Group g = GroupsManager.GetGroup(groupName);
                g.Startup = !g.Startup;
                GroupsManager.SaveGroups();
            }
            else {
                OpenGroup(groupName, modifierKeys == Keys.Control);
            }
        }

        private void menuitemGroups_ReorderFinished(object sender, ToolStripItemClickedEventArgs e) {
            GroupsManager.HandleReorder(tsmiGroups.DropDownItems.Cast<ToolStripItem>());
            SyncTaskBarMenu();
        }

        private void menuitemHistory_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if((ContextMenuedTab != null) && (clickedItem != null)) {
                MenuItemArguments menuItemArguments = clickedItem.MenuItemArguments;
                switch(ModifierKeys) {
                    case Keys.Shift:
                        CloneTabButton(ContextMenuedTab, null, true, -1);
                        NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                        return;

                    case Keys.Control: {
                            using(IDLWrapper wrapper = new IDLWrapper(menuItemArguments.Path)) {
                                OpenNewWindow(wrapper);
                                return;
                            }
                        }
                    default:
                        tabControl1.SelectTab(ContextMenuedTab);
                        NavigateToHistory(menuItemArguments.Path, menuItemArguments.IsBack, menuItemArguments.Index);
                        return;
                }
            }
        }

        private void menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            _tabManager.menuitemTabOrder_DropDownItemClicked(sender, e);
        }
        private void MergeAllWindows() {
            InstanceManager.PushTabBarInstance(this);
            InstanceManager.TabBarBroadcast(tabbar => {
                QTabItem[] tabs = tabbar.tabControl1.TabPages.Select(tab => tab.Clone(true)).ToArray();
                InstanceManager.InvokeMain(main => {
                    try {
                        main.tabControl1.SetRedraw(false);
                        foreach(QTabItem tab in tabs) {
                            tab.ResetOwner(main.tabControl1);
                            tab.ImageKey = tab.ImageKey; // this is so dumb, but unfortunately necessary
                            // todo: make it less dumb.
                        }
                        QTabItem.CheckSubTexts(main.tabControl1);
                        TryCallButtonBar(bbar => bbar.RefreshButtons());
                    }
                    finally {
                        main.tabControl1.SetRedraw(true);
                    }
                });
                WindowUtils.CloseExplorer(tabbar.ExplorerHandle, 2, true);
            }, false);
        }

        private void MinimizeToTray() {
            InstanceManager.AddToTrayIcon(Handle, ExplorerHandle, CurrentAddress,
                tabControl1.TabPages.Select(t => t.Text).ToArray(),
                tabControl1.TabPages.Select(t => t.CurrentPath).ToArray());
        }

        // NavigateBackToTheFuture moved to ExplorerControllerModule (Batch 13)

        internal void NavigateBranchCurrent(int index) {
            _explorerControllerModule.NavigateBranchCurrent(index);
        }

        private void NavigateBranches(QTabItem tab, int index) {
            _explorerControllerModule.NavigateBranches(tab, index);
        }

        private bool NavigateCurrentTab(bool fBack) {
            return _explorerControllerModule.NavigateCurrentTab(fBack);
        }

        private void NavigateToFirstOrLast(bool fBack) {
            _explorerControllerModule.NavigateToFirstOrLast(fBack);
        }

        internal void NavigateToHistory(string displayPath, bool fBack, int steps) {
            _explorerControllerModule.NavigateToHistory(displayPath, fBack, steps);
        }

        private bool NavigateToIndex(bool fBack, int index) {
            return _explorerControllerModule.NavigateToIndex(fBack, index);
        }

        

        // NavigationButton_DropDownMenu_ItemClicked moved to ExplorerControllerModule (Batch 13)

        // NavigationButtons_Click moved to ExplorerControllerModule (Batch 13)

        // NavigationButtons_DropDownOpening moved to ExplorerControllerModule (Batch 13)

        private void OnAwake() {
        }

        internal void OnMouseDoubleClick() {
            OnMouseDoubleClick(new MouseEventArgs(MouseButtons.Left, 0, 0, 0, 0));
        }

        protected override void OnExplorerAttached() {
            _explorerControllerModule.OnExplorerAttachedCore();
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if(VisualStyleRenderer.IsSupported) {
                if(bgRenderer == null) {
                    bgRenderer = new VisualStyleRenderer(VisualStyleElement.Rebar.Band.Normal);
                }
                bgRenderer.DrawParentBackground(e.Graphics, e.ClipRectangle, this);
            }
            else {
                if(ReBarHandle != IntPtr.Zero) {
                    int colorref = (int)PInvoke.SendMessage(ReBarHandle, 0x414, IntPtr.Zero, IntPtr.Zero);
                    using(SolidBrush brush = new SolidBrush(QTUtility2.MakeColor(colorref))) {
                        e.Graphics.FillRectangle(brush, e.ClipRectangle);
                        return;
                    }
                }
                base.OnPaintBackground(e);
            }
        }

        private void OpenDroppedFolder(IList<string> listDroppedPaths) {
            _tabManager.OpenDroppedFolder(listDroppedPaths);
        }
        // todo: CLEANNNNNNNNN
        public void OpenGroup(string groupName, bool fForceNewWindow, bool fDisableOverrides = false) {
            _tabManager.OpenGroup(groupName, fForceNewWindow, fDisableOverrides);
        }
        private bool OpenNewTab(string path, bool blockSelecting = false, bool fForceNew = false) {
            return _tabManager.OpenNewTab(path, blockSelecting, fForceNew);
        }
        internal bool OpenNewTab(IDLWrapper idlwGiven, bool blockSelecting = false, bool fForceNew = false) {
            return _tabManager.OpenNewTab(idlwGiven, blockSelecting, fForceNew);
        }
        internal void OpenNewTabOrWindow(IDLWrapper idlw, bool fNeedsPulse = false) {
            _tabManager.OpenNewTabOrWindow(idlw, fNeedsPulse);
        }
        internal void OpenNewWindow(IDLWrapper idlwGiven) {
            _tabManager.OpenNewWindow(idlwGiven);
        }
        private void pluginitems_Click(object sender, EventArgs e) {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            string name = item.Name;
            MenuType tag = (MenuType)item.Tag;
            foreach(Plugin plugin in pluginServer.Plugins.Where(plugin => plugin.PluginInformation.PluginID == name)) {
                try {
                    if(tag == MenuType.Tab) {
                        if(ContextMenuedTab != null) {
                            plugin.Instance.OnMenuItemClick(tag, item.Text, new PluginServer.TabWrapper(ContextMenuedTab, this));
                        }
                    }
                    else {
                        plugin.Instance.OnMenuItemClick(tag, item.Text, null);
                    }
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, ExplorerHandle, plugin.PluginInformation.Name, "On menu item \"" + item.Text + "\"clicked.");
                }
                break;
            }
        }

        // I don't like this.  It seems wrong to have this here instead of in the button bar class.
        // todo: consider moving all this to the button bar and just making the necessary methods internal.
        internal void ProcessButtonBarClick(int buttonID) {
            switch(buttonID) {
                case QTButtonBar.BII_NAVIGATION_BACK: // ��������
                    NavigateCurrentTab(true);
                    break;

                case QTButtonBar.BII_NAVIGATION_FWRD: // ����ǰ��
                    NavigateCurrentTab(true);
                    break;

                case QTButtonBar.BII_NEWWINDOW:// �´���
                    using(IDLWrapper wrapper4 = new IDLWrapper(CurrentTab.CurrentIDL)) {
                        OpenNewWindow(wrapper4);
                    }
                    break;

                case QTButtonBar.BII_CLONE:// ���Ʊ�ǩ
                    QTUtility2.log("QTTabBarLib.QTTabBarClass.CloneCurrentTab ���Ʊ�ǩ");
                    CloneCurrentTab();
                    break;

                case QTButtonBar.BII_LOCK: // ������ť
                    CurrentTab.TabLocked = !CurrentTab.TabLocked;
                    // CurrentTab.CurrentPath
                    if (CurrentTab.TabLocked)
                    {
                        StaticReg.LockedTabsToRestoreList.Add(CurrentTab.CurrentPath);
                    }
                    break;
                case QTButtonBar.BII_TOPMOST: // �ö�
                    ToggleTopMost();
                    break;

                case QTButtonBar.BII_CLOSE_CURRENT:// �رյ�ǰ
                    if(Config.Window.CloseBtnClosesSingleTab) {
                        CloseTab(CurrentTab);
                        return;
                    }
                    CloseTab(CurrentTab, false);
                    if(tabControl1.TabCount == 0) {
                        WindowUtils.CloseExplorer(ExplorerHandle, 2);
                    }
                    break;

                case QTButtonBar.BII_CLOSE_ALLBUTCURRENT: // �ر�����
                    if(tabControl1.TabCount > 1) {
                        CloseAllTabsExcept(CurrentTab);
                    }
                    break;

                case QTButtonBar.BII_CLOSE_WINDOW: // �ش���
                    using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root))
                    {
                        string[] list = (from QTabItem item2 in tabControl1.TabPages
                                         where item2.TabLocked
                                         select item2.CurrentPath).ToArray();

                        // MessageBox.Show(String.Join(",", list));
                        QTUtility2.WriteRegBinary(list, "TabsLocked", key);
                    }
                    WindowUtils.CloseExplorer(ExplorerHandle, 1);
                    break;

                case QTButtonBar.BII_CLOSE_LEFT: // �ر����
                    CloseLeftRight(true, -1);
                    break;

                case QTButtonBar.BII_CLOSE_RIGHT: // �ر��Ҳ�
                    CloseLeftRight(false, -1);
                    break;

                case QTButtonBar.BII_GOUPONELEVEL: // ��ת��һ��
                    QTUtility2.log("QTButtonBar.BII_GOUPONELEVEL UpOneLevel");
                    UpOneLevel();
                    break;

                case QTButtonBar.BII_REFRESH_SHELLBROWSER: // ˢ��
                    Explorer.Refresh();
                    break;

                case QTButtonBar.BII_SHELLSEARCH: // ��ʾ������
                    ShowSearchBar(true);
                    break;
                
                // add by qwop.
                case QTButtonBar.BII_OPTION:
                    OptionsDialog.Open();
                    break;
            }
        }

        /// <summary>
        /// ˢ����������
        /// </summary>
        internal void RefreshOptions() {
            QTUtility2.log(  "QTTabBarClass RefreshOptions" );
            SuspendLayout();
            tabControl1.SuspendLayout();
            tabControl1.RefreshOptions(false);
            if(Config.Tabs.ShowNavButtons) {
                if(toolStrip == null) {
                    InitializeNavBtns(true);
                    buttonNavHistoryMenu.Enabled = navBtnsFlag != 0;
                    Controls.Add(toolStrip);
                }
                else {
                    toolStrip.SuspendLayout();
                }
                toolStrip.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
                toolStrip.ResumeLayout(false);
                toolStrip.PerformLayout();
            }
            else if(toolStrip != null) {
                toolStrip.Dock = DockStyle.None;
            }
            int iType = 0;
            if(Config.Tabs.MultipleTabRows) {
                iType = Config.Tabs.ActiveTabOnBottomRow ? 1 : 2;
            }
            SetBarRows(tabControl1.SetTabRowType(iType));
            rebarController.RefreshBG();
            foreach(QTabItem item in tabControl1.TabPages) {
                item.RefreshRectangle();
            }
            ShellBrowser.SetUsingListView(Config.Tweaks.ForceSysListView);
            tabControl1.ResumeLayout();
            ResumeLayout(true);
            TryCallButtonBar(bbar => { return bbar.CreateItems(); });
            AbstractListView lv = GetListView();
            if(lv != null) {
                lv.RefreshViewWatermark(true);
            }
        }

        [ComRegisterFunction]
        private static void Register(Type t) {
            string name = t.GUID.ToString("B");
            ComRegistrationManager.RegisterBand(name, "QTTabBar", "QTTabBar", "QTTabBar");
            ComRegistrationManager.RegisterToolbar(name, "QTTabBar");
        }

        private void ReorderTab(int index, bool fDescending) {
            _tabManager.ReorderTab(index, fDescending);
        }
        internal void ReplaceByGroup(string groupName) {
            _tabManager.ReplaceByGroup(groupName);
        }
        private void RestoreLastClosed() {
            _tabManager.RestoreLastClosed();
        }
        // �ָ���ǩ
        private void RestoreTabsOnInitialize(int iIndex, string openingPath) {
            _tabManager.RestoreTabsOnInitialize(iIndex, openingPath);
        }

       

        protected override bool ShouldHaveBreak() {
            bool breakBar = true;
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                if(key != null) {
                    breakBar = ((int)key.GetValue("BreakTabBar", 1) == 1);
                }
            }
            return breakBar;
        }

        internal void ShowContextMenu(bool fByKey) {
            contextMenuSys.Show(fByKey ? PointToScreen(Point.Empty) : MousePosition);
        }

        public override void ShowDW(bool fShow) {
            base.ShowDW(fShow);
            if((fShow && !FirstNavigationCompleted) && ((Explorer != null) && (Explorer.ReadyState == tagREADYSTATE.READYSTATE_COMPLETE))) {
                InitializeInstallation();
            }
            if(!fShow) {
                using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                    key.SetValue("BreakTabBar", BandHasBreak() ? 1 : 0);
                }
            }
        }
        // ��ʾĿ¼��
        private void ShowFolderTree(bool fShow) {
            if(QTUtility.IsXP &&
               (fShow != ShellBrowser.IsFolderTreeVisible())) {
                object pvaClsid = "{EFA24E64-B078-11d0-89E4-00C04FC9E26E}";
                object pvarShow = fShow;
                object pvarSize = null;
                Explorer.ShowBrowserBar(ref pvaClsid, ref pvarShow, ref pvarSize);
            }
        }
        
        // ��ʾ·����MD5
        internal static void ShowMD5(string[] paths) {
            if(md5Form == null) {
                md5Form = new FileHashComputerForm();
            }
            List<string> list = new List<string>();
            if(paths != null) {
                list.AddRange(paths.Where(File.Exists));
            }
            string[] strArray = null;
            if(list.Count > 0) {
                strArray = list.ToArray();
            }
            if(md5Form.InvokeRequired) {
                md5Form.Invoke(new FormMethodInvoker(ShowMD5FormCore), new object[] { strArray });
            }
            else {
                ShowMD5FormCore(strArray);
            }
        }

        private static void ShowMD5FormCore(object paths) {
            md5Form.ShowFileHashForm((string[])paths);
        }

      

        private void ShowSearchBar(bool fShow) {
            QTUtility2.log(  "QTTabBarClass ShowSearchBar fShow: " + fShow);
            if(!QTUtility.IsXP) {
                if(!fShow) {
                    return;
                }
                using(IDLWrapper wrapper = new IDLWrapper(QTUtility.PATH_SEARCHFOLDER)) {
                    if(wrapper.Available) {
                        ShellBrowser.Navigate(wrapper, SBSP.NEWBROWSER);
                    }
                    return;
                }
            }
            object pvaClsid = "{C4EE31F3-4768-11D2-BE5C-00A0C9A83DA1}";
            object pvarShow = fShow;
            object pvarSize = null;
            Explorer.ShowBrowserBar(ref pvaClsid, ref pvarShow, ref pvarSize);
        }

        public AbstractListView GetListView() {
            return listView;
        }
        
        // ��ʾ��Ŀ¼��ʾ��Ϣ
        private void ShowSubdirTip_Tab(QTabItem tab, bool fShow, int offsetX, bool fKey, bool fParent) {
            _tabManager.ShowSubdirTip_Tab(tab, fShow, offsetX, fKey, fParent);
        }
        private bool ShowTabSwitcher(bool fShift, bool fRepeat) {
            return _tabManager.ShowTabSwitcher(fShift, fRepeat);
        }
        /**
         * ��ʾ������Ϣ
         *  shift ��ʾ��ϸ��Ϣ
         */
        private void ShowToolTipForDD(QTabItem tab, int iState, int grfKeyState) {
            _tabManager.ShowToolTipForDD(tab, iState, grfKeyState);
        }
        private void subDirTip_MenuItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            QMenuItem clickedItem = (QMenuItem)e.ClickedItem;
            if(clickedItem.Target == MenuTarget.Folder) {
                if(clickedItem.IDLData != null) {
                    using(IDLWrapper wrapper = new IDLWrapper(clickedItem.IDLData)) {
                        ShellBrowser.Navigate(wrapper);
                    }
                    return;
                }
                string targetPath = clickedItem.TargetPath;
                Keys modifierKeys = ModifierKeys;
                bool flag = (subDirTip_Tab != null) && (sender == subDirTip_Tab);
                if((modifierKeys & Keys.Control) == Keys.Control) {
                    using(IDLWrapper wrapper2 = new IDLWrapper(targetPath)) {
                        OpenNewWindow(wrapper2);
                        return;
                    }
                }
                if((modifierKeys & Keys.Shift) == Keys.Shift) {
                    using(IDLWrapper wrapper3 = new IDLWrapper(targetPath)) {
                        OpenNewTab(wrapper3, false, true);
                        return;
                    }
                }
                if((!flag || (ContextMenuedTab == CurrentTab)) && CurrentTab.TabLocked)
                {
                    QTUtility2.log("Clone Tab Button1");
                    CloneTabButton(CurrentTab, targetPath, true, TabIndex());
                    return;
                }
                if(flag && (ContextMenuedTab != CurrentTab)) {
                    if(ContextMenuedTab != null) {
                        if(ContextMenuedTab.TabLocked) {
                            // QTUtility2.log("Clone Tab Button2");
                            var index = TabIndex();
                            // ����߼���һ����ǩ
                            // tabControl1.TabPages.IndexOf(ContextMenuedTab) + 1
                            CloneTabButton(
                                ContextMenuedTab, 
                                targetPath, 
                                true,
                                index
                            );
                            return;
                        }

                        NowTabCloned = targetPath == CurrentAddress;
                        ContextMenuedTab.NavigatedTo(targetPath, null, 1, false);
                        // ContextMenuedTab.NavigatedTo(targetPath, null, -1, false);
                        tabControl1.SelectTab(ContextMenuedTab);
                        QTUtility2.log("NavigatedTo SelectTab");
                    }
                    return;
                }
                using(IDLWrapper wrapper4 = new IDLWrapper(targetPath)) {
                    ShellBrowser.Navigate(wrapper4);
                    QTUtility2.log("ShellBrowser.Navigate");
                    return;
                }
            }
            try {
                Process.Start(new ProcessStartInfo(clickedItem.Path) {
                    WorkingDirectory = Path.GetDirectoryName(clickedItem.Path) ?? "",
                    ErrorDialog = true,
                    ErrorDialogParentHandle = ExplorerHandle
                });
                QTUtility2.log("Process.Start");
                if(Config.Misc.KeepRecentFiles) {
                    StaticReg.ExecutedPathsList.Add(clickedItem.Path);
                    QTUtility2.log("StaticReg.ExecutedPathsList.Add");
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "ExecuteItem");
            }
        }

        // �޸�Ԥ��Ŀ¼��ת����ȷ�ı�ǩλ��
        private int TabIndex() {
            return _tabManager.TabIndex();
        }
        private void subDirTip_MenuItemRightClicked(object sender, ItemRightClickedEventArgs e) {
            QMenuItem clickedItem = e.ClickedItem as QMenuItem;
            if(clickedItem != null) {
                using(IDLWrapper wrapper = new IDLWrapper(clickedItem.Path)) {
                    e.HRESULT = shellContextMenu.Open(wrapper, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle, false);
                }
            }
        }

        private void subDirTip_MultipleMenuItemsClicked(object sender, EventArgs e) {
            List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
            if((ModifierKeys & Keys.Control) == Keys.Control) {
                QTUtility2.InitializeTemporaryPaths();
                StaticReg.CreateWindowPaths.AddRange(executedDirectories);
                using(IDLWrapper wrapper = new IDLWrapper(executedDirectories[0])) {
                    OpenNewWindow(wrapper);
                    return;
                }
            }
            bool flag = true;
            foreach(string str in executedDirectories) {
                OpenNewTab(str, !flag);
                flag = false;
            }
        }

        private void subDirTip_MultipleMenuItemsRightClicked(object sender, ItemRightClickedEventArgs e) {
            List<string> executedDirectories = ((SubDirTipForm)sender).ExecutedDirectories;
            // TODO: Replace ExecutedDirectories with ExecutedIDLs.
            List<byte[]> executedIDLs = executedDirectories.Select(path => {
                using(IDLWrapper wrapper = new IDLWrapper(path)) {
                    return wrapper.IDL;
                }
            }).ToList();
            e.HRESULT = shellContextMenu.Open(executedIDLs, e.IsKey ? e.Point : MousePosition, ((SubDirTipForm)sender).Handle);
        }

        internal static void SyncTaskBarMenu() {
            // todo
            /*
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                IntPtr hWnd = QTUtility2.ReadRegHandle("TaskBarHandle", key);
                if((hWnd != IntPtr.Zero) && PInvoke.IsWindow(hWnd)) {
                    QTUtility2.SendCOPYDATASTRUCT(hWnd, (IntPtr)3, string.Empty, IntPtr.Zero);
                }
            }*/
        }

        

        



        /**
         * bug ��ֻ��һ����ǩ��ʱ�򣬵����ǩ�հ״�ʶ��Ϊ��ǩ
         */
        // ����ڱ�ǩ�ϲ���

       



       

        private void timerSelectionChanged_Tick(object sender, EventArgs e) {
            try {
                timerSelectionChanged.Enabled = false;
                if((pluginServer != null) && (CurrentTab != null)) {
                    pluginServer.OnSelectionChanged(tabControl1.SelectedIndex, CurrentTab.CurrentIDL, CurrentTab.CurrentPath);
                    // timerSelectionChanged.Enabled = true;
                }
            }
            catch( Exception e1 ) {
                QTUtility2.MakeErrorLog( e1, "QTTabBarClass timerSelectionChanged_Tick");
            }
        }

        // ���ô����ö�����
        private void ToggleTopMost() {
            QTUtility2.log("QTTabBarClass ToggleTopMost");
            if(PInvoke.Ptr_OP_AND(PInvoke.GetWindowLongPtr(ExplorerHandle, -20), 8) != IntPtr.Zero) {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-2), 0, 0, 0, 0, 3);
                NowTopMost = false;
            }
            else {
                PInvoke.SetWindowPos(ExplorerHandle, (IntPtr)(-1), 0, 0, 0, 0, 3);
                NowTopMost = true;
            }
        }

        public override int TranslateAcceleratorIO(ref MSG msg) {
            if(msg.message == WM.KEYDOWN) {
                Keys wParam = (Keys)((int)((long)msg.wParam));
                bool flag = (((int)((long)msg.lParam)) & 0x40000000) != 0;
                switch(wParam) {
                    case Keys.Delete: {
                            if(!tabControl1.Focused || ((subDirTip_Tab != null) && subDirTip_Tab.MenuIsShowing)) {
                                break;
                            }
                            int focusedTabIndex = tabControl1.GetFocusedTabIndex();
                            if((-1 < focusedTabIndex) && (focusedTabIndex < tabControl1.TabCount)) {
                                bool flag3 = focusedTabIndex == (tabControl1.TabCount - 1);
                                if(CloseTab(tabControl1.TabPages[focusedTabIndex]) && flag3) {
                                    tabControl1.FocusNextTab(true, false, false);
                                }
                            }
                            return 0;
                        }
                    case Keys.Apps:
                        if(!flag) {
                            int index = tabControl1.GetFocusedTabIndex();
                            if((-1 >= index) || (index >= tabControl1.TabCount)) {
                                break;
                            }
                            ContextMenuedTab = tabControl1.TabPages[index];
                            Rectangle tabRect = tabControl1.GetTabRect(index, true);
                            contextMenuTab.Show(PointToScreen(new Point(tabRect.Right + 10, tabRect.Bottom - 10)));
                        }
                        return 0;

                    case Keys.F6:
                    case Keys.Tab:
                    case Keys.Left:
                    case Keys.Right: {
                            if(!tabControl1.Focused || ((subDirTip_Tab != null) && subDirTip_Tab.MenuIsShowing)) {
                                break;
                            }
                            bool fBack = (ModifierKeys == Keys.Shift) || (wParam == Keys.Left);
                            if(!tabControl1.FocusNextTab(fBack, false, false)) {
                                break;
                            }
                            return 0;
                        }
                    case Keys.Back:
                        return 0;

                    case Keys.Return:
                    case Keys.Space:
                        if(!flag && !tabControl1.SelectFocusedTab()) {
                            break;
                        }
                        listView.SetFocus();
                        return 0;

                    case Keys.Escape:
                        if(tabControl1.Focused && ((subDirTip_Tab == null) || !subDirTip_Tab.MenuIsShowing)) {
                            listView.SetFocus();
                        }
                        break;

                    case Keys.End:
                    case Keys.Home:
                        if((!tabControl1.Focused || ((subDirTip_Tab != null) && subDirTip_Tab.MenuIsShowing)) || !tabControl1.FocusNextTab(wParam == Keys.Home, false, true)) {
                            break;
                        }
                        return 0;

                    case Keys.Up:
                    case Keys.Down:
                        if(((!Config.Tabs.ShowSubDirTipOnTab || !tabControl1.Focused) || ((subDirTip_Tab != null) && subDirTip_Tab.MenuIsShowing)) || (!flag && !tabControl1.PerformFocusedFolderIconClick(wParam == Keys.Up))) {
                            break;
                        }
                        return 0;
                }
            }
            return base.TranslateAcceleratorIO(ref msg);
        }

        private bool travelBtnController_MessageCaptured(ref Message m) {
            if(CurrentTab == null) {
                QTUtility2.log("QTTabBarClass travelBtnController_MessageCaptured CurrentTab == null");
                return false;
            }
            switch(m.Msg) {
                case WM.LBUTTONDOWN:
                case WM.LBUTTONUP: {
                        Point pt = QTUtility2.PointFromLPARAM(m.LParam);
                        int num = (int)PInvoke.SendMessage(travelBtnController.Handle, 0x445, IntPtr.Zero, ref pt);
                        bool flag = CurrentTab.HistoryCount_Back > 1;
                        bool flag2 = CurrentTab.HistoryCount_Forward > 0;
                        if(m.Msg != 0x202) {
                            PInvoke.SetCapture(travelBtnController.Handle);
                            if(((flag && (num == 0)) || (flag2 && (num == 1))) || ((flag || flag2) && (num == 2))) {
                                int num5 = (int)PInvoke.SendMessage(travelBtnController.Handle, 0x412, (IntPtr)(0x100 + num), IntPtr.Zero);
                                int num6 = num5 | 2;
                                PInvoke.SendMessage(travelBtnController.Handle, 0x411, (IntPtr)(0x100 + num), (IntPtr)num6);
                            }
                            if((num == 2) && (flag || flag2)) {
                                RECT rect;
                                IntPtr hWnd = PInvoke.SendMessage(travelBtnController.Handle, 0x423, IntPtr.Zero, IntPtr.Zero);
                                if(hWnd != IntPtr.Zero) {
                                    PInvoke.SendMessage(hWnd, 0x41c, IntPtr.Zero, IntPtr.Zero);
                                }
                                PInvoke.GetWindowRect(travelBtnController.Handle, out rect);
                                _explorerControllerModule.NavigationButtons_DropDownOpening(buttonNavHistoryMenu, new EventArgs());
                                buttonNavHistoryMenu.DropDown.Show(new Point(rect.left - 2, rect.bottom + 1));
                            }
                            break;
                        }
                        PInvoke.ReleaseCapture();
                        for(int i = 0; i < 3; i++) {
                            int num3 = (int)PInvoke.SendMessage(travelBtnController.Handle, 0x412, (IntPtr)(0x100 + i), IntPtr.Zero);
                            int num4 = num3 & -3;
                            PInvoke.SendMessage(travelBtnController.Handle, 0x411, (IntPtr)(0x100 + i), (IntPtr)num4);
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
                    if(buttonNavHistoryMenu.DropDown.Visible) {
                        m.Result = (IntPtr)4;
                        buttonNavHistoryMenu.DropDown.Close(ToolStripDropDownCloseReason.AppClicked);
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

        private bool FolderLinkClicked(IDLWrapper wrapper, Keys modifierKeys, bool middle) {
            QTUtility2.log("QTTabBarClass FolderLinkClicked");
            MouseChord chord = QTUtility.MakeMouseChord(middle ? MouseChord.Middle : MouseChord.Left, modifierKeys);
            BindAction action;
            if(Config.Mouse.LinkActions.TryGetValue(chord, out action)) {
                DoBindAction(action, false, null, wrapper);
                return true;
            }
            else {
                QTUtility2.log("QTTabBarClass FolderLinkClicked δ��ȡ�����õĶ���");
                return false;
            }
        }

        public static bool TryCallButtonBar(Func<QTButtonBar, bool> func) {
            QTButtonBar bbar = InstanceManager.GetThreadButtonBar();
            return bbar != null && func(bbar);
        }

        internal bool TryGetSelection(out Address[] adSelectedItems, out string pathFocused, bool fDisplayName) {
            return ShellBrowser.TryGetSelection(out adSelectedItems, out pathFocused, fDisplayName);
        }

       

        private void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            _tabManager.tsmiBranchRoot_DropDownItemClicked(sender, e);
        }
        public override void UIActivateIO(int fActivate, ref MSG Msg) {
            QTUtility2.log("QTTabBarClass UIActivateIO");
            if(fActivate != 0) {
                tabControl1.Focus();
                tabControl1.FocusNextTab(ModifierKeys == Keys.Shift, true, false);
            }
        }

        [ComUnregisterFunction]
        private static void Unregister(Type t) {
            QTUtility2.log("QTTabBarClass Unregister");
            string name = t.GUID.ToString("B");
            ComRegistrationManager.UnregisterAll(name);
            // Also clean up the hardcoded CLSID
            try {
                using(RegistryKey key2 = Registry.ClassesRoot.OpenSubKey("CLSID", true)) {
                    if(key2 != null) {
                        key2.DeleteSubKeyTree("{D2BF470E-ED1C-487F-A444-2BD8835EB6CE}", false);
                    }
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "Unregister.CLSID2");
            }
            
            return;
            // TODO: Make the following code optional in the Uninstaller.
#if false
            try {
                using(RegistryKey key3 = Registry.Users) {
                    try {
                        foreach(string str2 in key3.GetSubKeyNames()) {
                            bool flag = true;
                            try {
                                using(RegistryKey key4 = key3.OpenSubKey(str2 + @"\Software\Quizo", true)) {
                                    if(key4 != null) {
                                        try {
                                            key4.DeleteSubKeyTree("QTTabBar");
                                            string[] subKeyNames = key4.GetSubKeyNames();
                                            flag = (subKeyNames != null) && (subKeyNames.Length > 0);
                                        }
                                        catch {
                                        }
                                    }
                                }
                            }
                            catch {
                            }
                            try {
                                if(!flag) {
                                    using(RegistryKey key5 = key3.OpenSubKey(str2 + @"\Software", true)) {
                                        if(key5 != null) {
                                            key5.DeleteSubKeyTree("Quizo");
                                        }
                                    }
                                }
                            }
                            catch {
                            }
                        }
                    }
                    catch {
                    }
                }
            }
            catch {
            }
#endif
        }

        private void UpOneLevel()
        {
            // ������һ��Ŀ¼
            if(CurrentTab.TabLocked) {
                QTabItem tab = CurrentTab.Clone();
                AddInsertTab(tab);
                tabControl1.SelectTab(tab);
            }
            if(!QTUtility.IsXP) {
                PInvoke.SendMessage(WindowUtils.GetShellTabWindowClass(ExplorerHandle), 0x111, (IntPtr)0xa022, IntPtr.Zero);
            }
            else {
                PInvoke.SendMessage(ExplorerHandle, 0x111, (IntPtr)0xa022, IntPtr.Zero);
            }
        }

        internal static void WaitTimeout(int msec) {
            Thread.Sleep(msec);
        }
        /**
         * ��Ϣ��� by indiff
         */
        protected override void WndProc(ref Message m) {
            try {
                switch(m.Msg) {
                    case WM.APP + 1: // todo: what sends this?
                        NowModalDialogShown = m.WParam != IntPtr.Zero;
                        return;

                    case WM.DROPFILES:  // �϶��ļ�
                        HandleFileDrop(m.WParam);
                        break;

                    case WM.DRAWITEM:
                    case WM.MEASUREITEM:
                    case WM.INITMENUPOPUP:
                        if(m.HWnd == Handle && shellContextMenu.TryHandleMenuMsg(m.Msg, m.WParam, m.LParam)) {
                            return;
                        }
                        break;
                }
                base.WndProc(ref m);
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, String.Format("Message: {0:x4}", m.Msg));
            }
        }

        // todo: This seems like it should go after every new tab creation, no?
        private void RestoreWindow()
        {
            bool fIsIconic = PInvoke.IsIconic(ExplorerHandle);
            InstanceManager.RemoveFromTrayIcon(Handle);
            WindowUtils.BringExplorerToFront(ExplorerHandle);
            if(fIsIconic) {
                foreach(QTabItem item2 in tabControl1.TabPages) {
                    item2.RefreshRectangle();
                }
                tabControl1.Refresh();
            }
        }

        protected override void OnDpiChanged(int oldDpi, int newDpi)
        {
            QTUtility2.log("QTTabBarClass OnDpiChanged");
            int iType = 0;
            if (Config.Tabs.MultipleTabRows)
            {
                iType = Config.Tabs.ActiveTabOnBottomRow ? 1 : 2;
            }
            // this.SetBarRows(this.tabControl.RowCount);
            SetBarRows(tabControl1.SetTabRowType(iType));
        }



        #region ��ǩ���¼���
        // Fields moved to TabBarBase: rebarController, CurrentAddress, CurrentTab, BandHeight,
        // BandHeightSpace, ShellBrowser, lstActivatedTabs, ExplorerHandle, LogEntryDic,
        // listView, listViewManager, TravelLog, pluginServer, NavigatedByCode,
        // NowTabsAddingRemoving, NowInTravelLog, NowModalDialogShown, NowTabCloned,
        // NowTabCreated, fNavigatedByTabSelection, CurrentTravelLogIndex, navBtnsFlag,
        // toolStrip, buttonBack, buttonForward, buttonNavHistoryMenu, TravelToolBarHandle

        protected List<ToolStripItem> lstPluginMenuItems_Sys;
        protected List<ToolStripItem> lstPluginMenuItems_Tab;

        protected bool NowOpenedByGroupOpener;
        protected bool NowTabDragging;
        protected bool NowTopMost;

        public bool HideExplorer
        {
            get
            {
                return fHideExplorer;
            }
        }

        /**
         * ���ӵ���ʷĿ¼
         */
        // AddToHistory and TryCallButtonBar moved to TabBarBase

        // CloseTab
        protected bool CloseTab(QTabItem closingTab, bool fCritical, bool fSkipSync = false) {
            return _tabManager.CloseTab(closingTab, fCritical, fSkipSync);
        }
        protected void CloseTabs(IEnumerable<QTabItem> tabs, bool fCritical = false) {
            _tabManager.CloseTabs(tabs, fCritical);
        }
        // TODO: Optional params
        protected bool CloseTab(QTabItem closingTab) {
            return _tabManager.CloseTab(closingTab);
        }
        // ShowMessageNavCanceled moved to TabBarBase

        protected void CancelFailedTabChanging(string newPath) {
            _tabManager.CancelFailedTabChanging(newPath);
        }
        // NavigateToPastSpecialDir moved to TabBarBase

        /**
        * TODO config to refresh  when tab control selected index changed
        * ���л���ǩ��ʱ�������Ƿ����ˢ��
        * �����쳣���
        * System.NullReferenceException: δ�������������õ������ʵ����
          �� QTTabBarLib.Interop.IShellBrowser.BrowseObject(IntPtr pidl, SBSP wFlags)
          �� QTTabBarLib.ShellBrowserEx.Navigate(IDLWrapper idlw, SBSP flags)
          �� QTTabBarLib.QTTabBarClass.tabControl1_SelectedIndexChanged(Object sender, EventArgs e)
        */
        // SyncTravelState, SyncToolbarTravelButton, IsSpecialFolderNeedsToTravel,
        // IsSearchResultFolder, tabControl1_RowCountChanged, SetBarRows moved to TabBarBase

        /**
         * ����ѡ����
         */
        protected void SaveSelectedItems(QTabItem tab) {
            _tabManager.SaveSelectedItems(tab);
        }
        

        // ����+�Ű�ť�������±�ǩ�¼�
        private void openDefault() {
            _tabManager.openDefault();
        }
        #endregion
    }
}
