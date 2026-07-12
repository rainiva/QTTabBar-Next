using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Common;
using QTTabBarLib.Interop;
using Timer = System.Windows.Forms.Timer;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
using IShellView = QTTabBarLib.Interop.IShellView;

namespace QTTabBarLib {
    internal sealed class ShellCommandController {
        private readonly IMenuContext _menuContext;
        private readonly IShellCommandHost _host;
        private const int WS_SHOWNORMAL = 1;

        public ShellCommandController(IMenuContext menuContext, IShellCommandHost host) {
            _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
            _host = host ?? throw new ArgumentNullException(nameof(host));
        }

        public void CreateNewFile() {
            IShellView shellView = null;
            IntPtr pIDL = IntPtr.Zero;
            try {
                string path = _host.SelectedTabPath;
                if(String.IsNullOrEmpty(path) || !Directory.Exists(path)) { SoundFeedbackService.SoundPlay(); return; }
                int i = 2;
                string name = QTUtility.DefaultNewFileName();
                string ext = ".txt";
                string pathNew = path + "\\" + name + ext;
                while(Directory.Exists(pathNew) || File.Exists(pathNew)) { pathNew = path + "\\" + name + " (" + i + ")" + ext; i++; }
                using(File.Create(pathNew)) { }
                if(0 == _host.ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView)) {
                    shellView.Refresh();
                    pIDL = PInvoke.ILCreateFromPath(pathNew);
                    if(pIDL != IntPtr.Zero) {
                        IntPtr pIDLRltv = PInvoke.ILFindLastID(pIDL);
                        if(pIDLRltv != IntPtr.Zero) { shellView.SelectItem(pIDLRltv, SVSIF.SELECT | SVSIF.DESELECTOTHERS | SVSIF.ENSUREVISIBLE | SVSIF.EDIT); return; }
                    }
                }
            }
            catch { }
            finally {
                if(_host.ShellBrowser.GetIShellBrowser() != null) { QTLogger.log("ReleaseComObject ShellBrowser.GetIShellBrowser()"); Marshal.ReleaseComObject(_host.ShellBrowser.GetIShellBrowser()); }
                if(pIDL != IntPtr.Zero) Marshal.FreeCoTaskMem(pIDL);
            }
            SoundFeedbackService.SoundPlay();
        }

        public void OpenCmd(QTabItem tab) {
            if(_host.ShellBrowser.GetIShellBrowser() != null) {
                string currentPath = tab != null ? tab.CurrentPath : _menuContext.ContextMenuedTab.CurrentPath;
                if(currentPath.IndexOf("???") != -1) currentPath = currentPath.Substring(0, currentPath.IndexOf("???"));
                else if(currentPath.IndexOf("*?*?*") != -1) currentPath = currentPath.Substring(0, currentPath.IndexOf("*?*?*"));
                if(Directory.Exists(currentPath)) CmdPath(currentPath);
                else if(QTUtility2.PathExists("C:\\")) CmdPath("C:\\");
                else if(QTUtility2.PathExists("D:\\")) CmdPath("D:\\");
                else if(QTUtility2.PathExists("E:\\")) CmdPath("E:\\");
                else if(QTUtility2.PathExists("F:\\")) CmdPath("F:\\");
            }
        }

        public void Wait4Select() {
            if(!Config.Window.CaptureWeChatSelection) return;
            QTLogger.log("Wait4Select");
            int count = 1;
            Timer timer = new Timer { Interval = 1000 };
            timer.Tick += (sender, args) => {
                try {
                    count++;
                    if(count >= 10) timer.Stop();
                    string selectionPath = RegistryUtil.ReadSelection(_host.CurrentTab.CurrentPath);
                    QTLogger.log(" ReadSelection key " + _host.CurrentTab.CurrentPath + " path " + selectionPath);
                    if(QTUtility2.IsNotEmpty(selectionPath)) {
                        QTLogger.log("find mainShellView ");
                        IShellView mainShellView = null;
                        bool selected = false;
                        if(0 == _host.ShellBrowser.GetIShellBrowser().QueryActiveShellView(out mainShellView)) {
                            mainShellView.Refresh();
                            QTLogger.log("Refresh ");
                            using(IDLWrapper wrapper = new IDLWrapper(selectionPath)) {
                                if(wrapper.Available && wrapper.PIDL != IntPtr.Zero) {
                                    QTLogger.log("wrapper " + wrapper.Path);
                                    IntPtr pIDLRltv = PInvoke.ILFindLastID(wrapper.PIDL);
                                    if(pIDLRltv != IntPtr.Zero) { QTLogger.log("SelectItem " + pIDLRltv); mainShellView.SelectItem(pIDLRltv, SVSIF.SELECT | SVSIF.DESELECTOTHERS | SVSIF.ENSUREVISIBLE); selected = true; }
                                }
                            }
                        }
                        if(selected) timer.Stop();
                    }
                }
                catch(Exception e) { QTLogger.MakeErrorLog(e, "读取微信或者qq打开后的选中文件"); }
            };
            timer.Start();
        }

        private void Wait4SelectedQuit() {
            int count = 1;
            Timer timer = new Timer { Interval = 1000 };
            timer.Tick += (sender, args) => {
                count++;
                if(count >= 10) { QTLogger.log("CloseExplorer QTTabBarClass start"); timer.Stop(); _host.QuitExplorer(); QTLogger.log("CloseExplorer QTTabBarClass  end"); }
                QTLogger.log("QTTabBarClass timer.Tick TryGetSelection ");
                try {
                    QTabItem tabItem = _host.TabControl.TabPages[0];
                    IShellView shellView = null;
                    List<string> select = SelectionTracker.GetSelect(tabItem.CurrentPath);
                    if(select != null) { timer.Stop(); _host.QuitExplorer(); }
                    if(0 == _host.ShellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView)) {
                        Guid iid = new Guid("{0000010e-0000-0000-C000-000000000046}");
                        object ppv;
                        shellView.GetItemObject((uint)SVSIF.SELECT, ref iid, out ppv);
                        if(ppv != null) {
                            IDataObject pDataObject = (IDataObject)ppv;
                            ShellObjectCollection shellObjectCollection = ShellObjectCollection.FromDataObject(pDataObject);
                            if(shellObjectCollection.Count > 0) {
                                List<string> list = new List<string>();
                                foreach(ShellObject so in shellObjectCollection) {
                                    QTLogger.log("add so.Name " + so.Name + " so.ParsingName " + so.ParsingName);
                                    list.Add(so.ParsingName);
                                    SelectionTracker.PutSelect(tabItem.CurrentPath, list);
                                    timer.Stop();
                                    _host.QuitExplorer();
                                }
                            }
                        }
                    }
                }
                catch { }
            };
            timer.Start();
        }

        private void CmdPath(string currentPath) {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.Arguments = "/k cd " + currentPath;
            process.StartInfo.WorkingDirectory = currentPath;
            process.Start();
            ShowWindowAsync(process.MainWindowHandle, WS_SHOWNORMAL);
            SetForegroundWindow(process.MainWindowHandle);
            SetFocus(process.MainWindowHandle);
        }
        [DllImport("User32.dll")] private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        [DllImport("User32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("User32.dll")] private static extern bool SetFocus(IntPtr hWnd);
    }
}
