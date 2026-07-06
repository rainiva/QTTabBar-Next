//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano, Indiff
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
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32.SafeHandles;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Extracted from QTUtility2 — handles all logging functionality.
    /// QTUtility2 retains facade methods for backward compatibility.
    /// </summary>
    internal static class Logger {
        private static bool fConsoleAllocated;
        public static bool ENABLE_LOGGER = false;
        private static Dictionary<int, DateTime> dictTime = new Dictionary<int, DateTime>();
        private static string[] IGNORES = { "ReleaseComObject" };
        private static readonly Mutex M_MUTEX = new Mutex();

        public static void AllocDebugConsole() {
            if(fConsoleAllocated) {
                return;
            }
            const int STD_OUTPUT_HANDLE = -11;
            const int MY_CODE_PAGE = 437;
            PInvoke.AllocConsole();
            IntPtr stdHandle = PInvoke.GetStdHandle(STD_OUTPUT_HANDLE);
            SafeFileHandle safeFileHandle = new SafeFileHandle(stdHandle, true);
            FileStream fileStream = new FileStream(safeFileHandle, FileAccess.Write);
            Encoding encoding = Encoding.GetEncoding(MY_CODE_PAGE);
            StreamWriter standardOutput = new StreamWriter(fileStream, encoding);
            standardOutput.AutoFlush = true;
            Console.SetOut(standardOutput);
            fConsoleAllocated = true;
        }

        /// <summary>
        /// Force log (always writes, regardless of ENABLE_LOGGER).
        /// </summary>
        public static void flog(string optional) {
            StackTrace trace = new StackTrace();
            Dictionary<String, String> dic = new Dictionary<String, String>();
            if(trace != null) {
                StackFrame frame = trace.GetFrame(1);
                if(frame != null) {
                    MethodBase method = frame.GetMethod();
                    if(method != null) {
                        dic.Add("methodName", method.Name);
                        if(method.ReflectedType != null) {
                            String className = method.ReflectedType.Name;
                            dic.Add("className", className);
                        }
                    }
                }
            }
            log("flog", optional, dic);
        }

        public static void log(string optional) {
            if(ENABLE_LOGGER) {
                StackTrace trace = new StackTrace();
                Dictionary<String, String> dic = new Dictionary<String, String>();
                if(trace != null) {
                    StackFrame frame = trace.GetFrame(1);
                    if(frame != null) {
                        MethodBase method = frame.GetMethod();
                        if(method != null) {
                            dic.Add("methodName", method.Name);
                            if(method.ReflectedType != null) {
                                String className = method.ReflectedType.Name;
                                dic.Add("className", className);
                            }
                        }
                    }
                }
                log("log", optional, dic);
            }
        }

        public static void log2(string optional) {
            if(ENABLE_LOGGER) {
                log("log", optional);
            }
        }

        public static void err(string optional) {
            if(ENABLE_LOGGER) {
                StackTrace trace = new StackTrace();
                Dictionary<String, String> dic = new Dictionary<String, String>();
                if(trace != null) {
                    StackFrame frame = trace.GetFrame(1);
                    if(frame != null) {
                        MethodBase method = frame.GetMethod();
                        if(method != null) {
                            dic.Add("methodName", method.Name);
                            if(method.ReflectedType != null) {
                                String className = method.ReflectedType.Name;
                                dic.Add("className", className);
                            }
                        }
                    }
                }
                log("err", optional, dic);
            }
        }

        public static void log(string level, string optional, Dictionary<String, String> dic = null) {
            // ignore
            if(null != IGNORES && IGNORES.Length > 0) {
                foreach(var ignore in IGNORES) {
                    var lower1 = ignore.ToLower();
                    var lower2 = optional.ToLower();
                    if(lower2.Contains(lower1)) {
                        return;
                    }
                }
            }

            string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appdataQT = Path.Combine(appdata, "QTTabBar");
            if(!Directory.Exists(appdataQT)) {
                Directory.CreateDirectory(appdataQT);
            }

            Process process = Process.GetCurrentProcess();
            var cThreadId = Thread.CurrentThread.ManagedThreadId;
            var currentThreadId = AppDomain.GetCurrentThreadId();
            if(null == cThreadId) {
                cThreadId = currentThreadId;
            }

            var useTime = "";
            if(null != cThreadId) {
                if(null != dictTime) {
                    if(!dictTime.ContainsKey(cThreadId)) {
                        dictTime[cThreadId] = DateTime.Now;
                    }
                    var oldTime = dictTime[cThreadId];
                    if(null != oldTime) {
                        useTime = "" + ((DateTime.Now - oldTime).TotalMilliseconds) + "ms";
                        dictTime[cThreadId] = DateTime.Now;
                    }
                }
                else {
                    dictTime[cThreadId] = DateTime.Now;
                }
            }

            string path = Path.Combine(appdataQT, "QTTabBarException.log");
            var line = new StringBuilder();
            line
                .Append("[")
                .Append(level)
                .Append("]");

            if(null != dic && dic.Count > 0 && dic.ContainsKey("methodName") && dic.ContainsKey("className")) {
                if(dic.ContainsKey("className")) {
                    var className = dic["className"];
                    if(!string.IsNullOrEmpty(className)) {
                        line
                            .Append("\tC:")
                            .Append(className);
                    }
                }

                if(dic.ContainsKey("methodName")) {
                    var methodName = dic["methodName"];
                    if(!string.IsNullOrEmpty(methodName)) {
                        line
                            .Append("\tM:")
                            .Append(methodName);
                    }
                }
            }
            if(process != null) {
                line
                    .Append("\tP:")
                    .Append(process.Id);
            }
            if(cThreadId != null) {
                line
                    .Append("\tT:")
                    .Append(cThreadId);
            }
            else if(currentThreadId != null) {
                line
                    .Append("\tT:")
                    .Append(currentThreadId);
            }

            if(!string.IsNullOrEmpty(useTime)) {
                line
                    .Append("\tcost:")
                    .Append(useTime);
            }
            line
                .Append("\t")
                .Append(DateTime.Now.ToString())
                .Append("\t")
                .Append(optional);
            writeStr(path, line);
        }

        public static void MakeErrorLog(Exception ex, string optional = null) {
            try {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appdataQT = Path.Combine(appdata, "QTTabBar");
                if(!Directory.Exists(appdataQT)) {
                    Directory.CreateDirectory(appdataQT);
                }
                string path = Path.Combine(appdataQT, "QTTabBarException.log");
                var line = new StringBuilder();
                line.AppendLine(DateTime.Now.ToString());
                line.AppendLine(".NET Version: " + Environment.Version);
                line.AppendLine("OS Version: " + Environment.OSVersion.Version +
                                " Major: " + Environment.OSVersion.Version.Major +
                                " Bit: " + getEnv()
                                );
                line.AppendLine("QT Version: " + QTUtility2.MakeVersionString());
                if(!String.IsNullOrEmpty(optional)) {
                    line.AppendLine("Additional Info: " + optional);
                }
                if(ex == null) {
                    line.AppendLine("Exception: None");
                    if(Environment.StackTrace != null) {
                        line.AppendLine(Environment.StackTrace);
                    }
                }
                else {
                    line.AppendFormat("\nMessage ---\n{0}", ex.Message);
                    line.AppendFormat(
                        "\nHelpLink ---\n{0}", ex.HelpLink);
                    line.AppendFormat("\nSource ---\n{0}", ex.Source);
                    line.AppendFormat(
                        "\nStackTrace ---\n{0}", ex.StackTrace);
                    line.AppendFormat(
                        "\nTargetSite ---\n{0}", ex.TargetSite);

                    if(ex.InnerException != null) {
                        line.AppendLine("****************InnerException");
                        line.AppendFormat("\n  InnerMessage ---\n{0}", ex.InnerException.Message);
                        line.AppendFormat(
                            "\n InnerHelpLink ---\n{0}", ex.InnerException.HelpLink);
                        line.AppendFormat("\n  InnerSource ---\n{0}", ex.InnerException.Source);
                        line.AppendFormat(
                            "\n InnerStackTrace ---\n{0}", ex.InnerException.StackTrace);
                        line.AppendFormat(
                            "\n InnerTargetSite ---\n{0}", ex.InnerException.TargetSite);
                    }
                }
                line.AppendLine("--------------");
                line.AppendLine();

                writeStr(path, line);
            }
            catch(Exception ex2) {
                System.Diagnostics.Debug.WriteLine("MakeErrorLog failed: " + ex2.Message);
            }
        }

        public static void MakeErrorLog(string optional = null) {
            try {
                string appdata = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string appdataQT = Path.Combine(appdata, "QTTabBar");
                if(!Directory.Exists(appdataQT)) {
                    Directory.CreateDirectory(appdataQT);
                }
                string path = Path.Combine(appdataQT, "QTTabBarException.log");
                using(StreamWriter writer = new StreamWriter(path, true)) {
                    if(!String.IsNullOrEmpty(optional)) {
                        writer.WriteLine("Additional Info: " + optional);
                    }
                    writer.WriteLine("--------------");
                    writer.WriteLine();
                    QTUtility2.Close(writer);
                }
                SystemSounds.Exclamation.Play();
            }
            catch {
            }
        }

        private static string getEnv() {
            if(4 == IntPtr.Size) {
                return "32";
            }
            else if(8 == IntPtr.Size) {
                return "64";
            }
            return "unknown";
        }

        private static void writeStr(string path, StringBuilder formatLogLine) {
            try {
                M_MUTEX.WaitOne();
                if(File.Exists(path)) {
                    using(FileStream fs = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite)) {
                        using(StreamWriter sr = new StreamWriter(fs)) {
                            sr.WriteLine(formatLogLine);
                        }
                    }
                }
                else {
                    using(FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite)) {
                        using(StreamWriter sr = new StreamWriter(fs)) {
                            sr.WriteLine(formatLogLine);
                        }
                    }
                }
            }
            finally {
                M_MUTEX.ReleaseMutex();
            }
        }
    }
}
