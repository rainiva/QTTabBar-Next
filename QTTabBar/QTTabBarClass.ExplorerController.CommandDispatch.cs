//    Command-line capture dispatch extracted from ExplorerController.Init (arch-batch4a).

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
        internal partial class ExplorerControllerModule {
            internal sealed class CommandDispatchController {
                private readonly QTTabBarClass _owner;

                internal CommandDispatchController(QTTabBarClass owner, ExplorerControllerModule module) {
                    _owner = owner;
                }

                internal void TryHandleNewWindowCapture(string path, ref bool ensureOpenedWindow) {
                    QTLogger.log("DoFirstNavigation path: " + path + " IsNoCapturePaths:" + PathValidator.IsNoCapturePaths(path));
                    if(
                        SessionState.NoCapturePathsList.Any(ncPath => ncPath.PathEquals(path))
                         || PathValidator.IsNoCapturePaths(path)
                        ) {
                        ensureOpenedWindow = true;
                        return;
                    }
                    if(Config.Window.CaptureNewWindows &&
                        Control.ModifierKeys != Keys.Control &&
                        InstanceManager.GetTotalInstanceCount() > 0) {
                        string cmd = GetCommandLine();
                        if(!String.IsNullOrEmpty(cmd)) {
                            string lcmd = cmd.ToLower();
                            if(lcmd.Contains("/select") || lcmd.Contains(",select")) {
                                _owner.ExMCmdType = 1;
                                string selectMe = GetNameToSelectFromCommandLineArg(cmd);
                                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                InstanceManager.BeginInvokeMainCaptureNewWindow(path, 1, selectMe);
                                TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                QTLogger.log(string.Format("select cmd typed IPC cost {0} ", abs.TotalMilliseconds));
                            }
                            else if(lcmd.Contains("/factory")   ||
                                     lcmd.Contains("-embedding") ||
                                     lcmd.Contains("{75dff2b7-6936-4c06-a8bb-676a7b00b24b}")) {
                                _owner.ExMCmdType = 2;
                                TimeSpan start = new TimeSpan(DateTime.Now.Ticks);
                                InstanceManager.BeginInvokeMainCaptureNewWindow(path, 2, string.Empty);
                                TimeSpan abs = new TimeSpan(DateTime.Now.Ticks).Subtract(start).Duration();
                                QTLogger.log(string.Format("factory cmd typed IPC cost {0} ", abs.TotalMilliseconds));
                            }
                            else {
                                _owner.ExMCmdType = 3;
                                InstanceManager.BeginInvokeMainCaptureNewWindow(path, 3, string.Empty);
                                QTLogger.log("other cmd typed IPC RestoreWindow");
                            }
                        }

                        _owner.ExfNowQuitting = true;
                        if(OSDetector.IsXP) {
                            QTLogger.log("Close Explorer WindowUtils.CloseExplorer");
                            WindowUtils.CloseExplorer(_owner.ExExplorerHandle, 0);
                        }
                        else {
                            _owner.ExfHideExplorer = true;

                            if(_owner.ExMCmdType == 3 || !Config.Window.CaptureWeChatSelection) {
                                QTLogger.log("Close Explorer Explorer.Quit");
                                _owner.ExExplorer.Quit();
                            }
                        }
                        QTLogger.log("DoFirstNavigation return");
                    }
                    QTLogger.log("AddStartUpTabs ");
                    _owner.ExAddStartUpTabs(string.Empty, path);
                    QTLogger.log("AddStartUpTabs InitializeOpenedWindow");
                    ensureOpenedWindow = true;
                }

                internal static string GetCommandLine() {
                    Process cprocess = Process.GetCurrentProcess();

                    int currentProcessId2 = cprocess.Id;

                    int currentProcessId = (int)PInvoke.GetCurrentProcessId();
                    var process = Process.GetProcessById(currentProcessId);
                    QTLogger.log(" process command line 0 : " + cprocess.StartInfo.Arguments);
                    QTLogger.log(" process command line 1 : " + process.StartInfo.Arguments);


                    string result = null;
                    try {
                        var cpid = currentProcessId;
                        if(currentProcessId2 != currentProcessId) {
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
                    catch(Exception) {
                        result = "";
                    }
                    string str = Marshal.PtrToStringUni(PInvoke.GetCommandLine());
                    QTLogger.log(" process command line 2 : " + str);
                    return str;
                }

                internal static string GetNameToSelectFromCommandLineArg(string str) {
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

                internal static bool TryParseCommandlineParams(
                    string param,
                    out string path,
                    out string selection) {
                    selection = (string)null;
                    Match match = new Regex("( ?(/|,)select, ?((?<SELQ>\"[^\"/]+\")|(?<SEL>[^,/]+))| ?(/|,)root,\\s?((?<ROOTQ>\"[^\"/]+\")|(?<ROOT>[^,/]+)))+", RegexOptions.IgnoreCase).Match(param);
                    if(match.Success) {
                        var group1 = match.Groups["SEL"];
                        var group2 = match.Groups["SELQ"];
                        var group3 = match.Groups["ROOT"];
                        var group4 = match.Groups["ROOTQ"];
                        try {
                            if(group3.Success) {
                                path = group3.Value;
                                return true;
                            }
                            if(group4.Success) {
                                path = group4.Value.Trim('"');
                                return true;
                            }
                            if(group1.Success) {
                                selection = group1.Value;
                                path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                                return true;
                            }
                            if(group2.Success) {
                                selection = group2.Value.Trim('"');
                                path = !QTUtility2.IsDrive(selection) ? Path.GetDirectoryName(selection) : "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                                return true;
                            }
                        }
                        catch {
                        }
                    }
                    path = (string)null;
                    return false;
                }
            }

            private static string GetCommandLine() {
                return CommandDispatchController.GetCommandLine();
            }

            private static string GetNameToSelectFromCommandLineArg(string str) {
                return CommandDispatchController.GetNameToSelectFromCommandLineArg(str);
            }

            private static bool TryParseCommandlineParams(
                string param,
                out string path,
                out string selection) {
                return CommandDispatchController.TryParseCommandlineParams(param, out path, out selection);
            }
        }
}
