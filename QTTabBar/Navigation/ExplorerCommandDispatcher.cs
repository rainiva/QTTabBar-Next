using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerCommandDispatcher {
        private readonly IExplorerWindowCaptureHost _host;

        internal ExplorerCommandDispatcher(IExplorerWindowCaptureHost host) {
            _host = host;
        }

        internal void TryHandleNewWindowCapture(string path, ref bool ensureOpenedWindow) {
            QTLogger.log("DoFirstNavigation path: " + path + " IsNoCapturePaths:" + PathValidator.IsNoCapturePaths(path));
            if(SessionState.NoCapturePathsList.Any(ncPath => ncPath.PathEquals(path)) || PathValidator.IsNoCapturePaths(path)) {
                ensureOpenedWindow = true;
                return;
            }
            if(Config.Window.CaptureNewWindows && Control.ModifierKeys != Keys.Control && InstanceManager.GetTotalInstanceCount() > 0) {
                string cmd = GetCommandLine();
                if(!String.IsNullOrEmpty(cmd)) {
                    string lcmd = cmd.ToLower();
                    if(lcmd.Contains("/select") || lcmd.Contains(",select")) {
                        _host.CommandMode = 1;
                        string selectMe = GetNameToSelectFromCommandLineArg(cmd);
                        TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                        InstanceManager.BeginInvokeMainCaptureNewWindow(path, 1, selectMe);
                        TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                        QTLogger.log(string.Format("select cmd typed IPC cost {0} ", elapsed.TotalMilliseconds));
                    }
                    else if(lcmd.Contains("/factory") || lcmd.Contains("-embedding") || lcmd.Contains("{75dff2b7-6936-4c06-a8bb-676a7b00b24b}")) {
                        _host.CommandMode = 2;
                        TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                        InstanceManager.BeginInvokeMainCaptureNewWindow(path, 2, string.Empty);
                        TimeSpan elapsed = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                        QTLogger.log(string.Format("factory cmd typed IPC cost {0} ", elapsed.TotalMilliseconds));
                    }
                    else {
                        _host.CommandMode = 3;
                        InstanceManager.BeginInvokeMainCaptureNewWindow(path, 3, string.Empty);
                        QTLogger.log("other cmd typed IPC RestoreWindow");
                    }
                }

                _host.IsQuitting = true;
                if(OSDetector.IsXP) {
                    QTLogger.log("Close Explorer WindowUtils.CloseExplorer");
                    WindowUtils.CloseExplorer(_host.ExplorerHandle, 0);
                }
                else {
                    _host.HideExplorer = true;
                    if(_host.CommandMode == 3 || !Config.Window.CaptureWeChatSelection) {
                        QTLogger.log("Close Explorer Explorer.Quit");
                        _host.Explorer.Quit();
                    }
                }
                QTLogger.log("DoFirstNavigation return");
            }
            QTLogger.log("AddStartUpTabs ");
            _host.AddStartupTabs(string.Empty, path);
            QTLogger.log("AddStartUpTabs InitializeOpenedWindow");
            ensureOpenedWindow = true;
        }

        internal static string GetCommandLine() {
            Process current = Process.GetCurrentProcess();
            int currentProcessId = (int)PInvoke.GetCurrentProcessId();
            Process process = Process.GetProcessById(currentProcessId);
            QTLogger.log(" process command line 0 : " + current.StartInfo.Arguments);
            QTLogger.log(" process command line 1 : " + process.StartInfo.Arguments);
            string result = null;
            try {
                int processId = current.Id != currentProcessId ? current.Id : currentProcessId;
                string query = string.Format("select CommandLine from Win32_Process where ProcessID ={0}", processId);
                using(ManagementObjectSearcher searcher = new ManagementObjectSearcher(query)) {
                    foreach(ManagementObject item in searcher.Get().Cast<ManagementObject>()) {
                        result = item["CommandLine"] == null ? "" : item["CommandLine"].ToString();
                    }
                }
                QTLogger.log(" process command line 3 : " + result);
            }
            catch(Exception) {
                result = "";
            }
            string commandLine = Marshal.PtrToStringUni(PInvoke.GetCommandLine());
            QTLogger.log(" process command line 2 : " + commandLine);
            return commandLine;
        }

        internal static string GetNameToSelectFromCommandLineArg(string value) {
            QTLogger.log("GetNameToSelectFromCommandLineArg :" + value);
            if(!string.IsNullOrEmpty(value)) {
                int index = value.IndexOf("/select,", StringComparison.CurrentCultureIgnoreCase);
                if(index == -1) index = value.IndexOf(",select,", StringComparison.CurrentCultureIgnoreCase);
                if(index != -1) {
                    index += 8;
                    if(value.Length < index) return string.Empty;
                    string path = value.Substring(index).Split(new[] { ',' })[0].Trim().Trim(new[] { ' ', '"' });
                    try {
                        if(File.Exists(path) || Directory.Exists(path)) return Path.GetFileName(path);
                    }
                    catch { }
                }
            }
            return string.Empty;
        }

        internal static bool TryParseCommandlineParams(string param, out string path, out string selection) {
            selection = null;
            Match match = new Regex("( ?(/|,)select, ?((?<SELQ>\"[^\"/]+\")|(?<SEL>[^,/]+))| ?(/|,)root,\\s?((?<ROOTQ>\"[^\"/]+\")|(?<ROOT>[^,/]+)))+", RegexOptions.IgnoreCase).Match(param);
            if(match.Success) {
                try {
                    if(match.Groups["ROOT"].Success) { path = match.Groups["ROOT"].Value; return true; }
                    if(match.Groups["ROOTQ"].Success) { path = match.Groups["ROOTQ"].Value.Trim('"'); return true; }
                    if(match.Groups["SEL"].Success) {
                        selection = match.Groups["SEL"].Value;
                        path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                        return true;
                    }
                    if(match.Groups["SELQ"].Success) {
                        selection = match.Groups["SELQ"].Value.Trim('"');
                        path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                        return true;
                    }
                }
                catch { }
            }
            path = null;
            return false;
        }
    }
}
