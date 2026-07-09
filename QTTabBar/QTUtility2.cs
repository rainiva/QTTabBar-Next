//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2020  Quizo, Paul Accisano, Indiff
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
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using BandObjectLib;
using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using QTTabBarLib.Interop;
using SHDocVw;

namespace QTTabBarLib {
    public static class QTUtility2 {
        private const int THRESHOLD_ELLIPSIS = 40;
                // �ж��Ƿ�������־��������Ϊfalse�� ��������. Ĭ���ǹرյģ��ڳ���ѡ�����������������
        public static bool ENABLE_LOGGER { get { return Logger.ENABLE_LOGGER; } set { Logger.ENABLE_LOGGER = value; } }

        public static string ExplorerPath
        {
            get
            {
                // Environment.SpecialFolder.CommonApplicationData + 1 => Environment.SpecialFolder.Windows
                return Environment.GetFolderPath((Environment.SpecialFolder.CommonApplicationData + 1)) + Path.DirectorySeparatorChar + "explorer.exe";
            }
        }

        public static string Enquote(this string s) {
            return "\"" + s + "\"";
        }

        public static int GET_X_LPARAM(IntPtr lParam) {
            return ((int)lParam).LoWord();
        }

        public static int GET_Y_LPARAM(IntPtr lParam) {
            return ((int)lParam).HiWord();
        }

        public static int HiWord(this int i) {
            return (short)((i >> 0x10) & 0xffff);
        }

        public static int LoWord(this int i) {
            return (short)(i & 0xffff);
        }

        public static int HiWord(this IntPtr i) {
            return ((((int)(long)i) >> 16) & 0xFFFF);
        }

        public static int LoWord(this IntPtr i) {
            return ((int)(long)i & 0xFFFF);
        }

        public static IEnumerable<T> RangeSelect<T>(this int i, Converter<int, T> converter) {
            for(int j = 0; j < i; j++) {
                yield return converter(j);
            }
        }

        public static string GetDriveDisplayText(string path) {
            if((path.Length != 3) || !path.EndsWith(@":\")) {
                return String.Empty;
            }
            switch(PInvoke.GetDriveType(path)) {
                case 0:
                case 1:
                case 4:
                    return path;
            }
            return ShellMethods.GetDisplayName(path);
        }

        public static void InitializeTemporaryPaths() {
            StaticReg.CreateWindowPaths.Clear();
            StaticReg.CreateWindowIDLs.Clear();
        }

        internal static IEnumerable<T> Interleave<T>(this IEnumerable<T> first, IEnumerable<T> second) {
            using(var enumerator1 = first.GetEnumerator())
            using(var enumerator2 = second.GetEnumerator()) {
                while(enumerator1.MoveNext()) {
                    yield return enumerator1.Current;
                    if(enumerator2.MoveNext()) {
                        yield return enumerator2.Current;
                    }
                }
                while(enumerator2.MoveNext()) {
                    yield return enumerator2.Current;
                }
            }
        }

        internal static void Invoke<T>(this T control, Action<T> action) where T : Control {
            control.Invoke(action, control);
        }

        internal static void Invoke(this Control control, Action action) {
            control.Invoke(action);
        }

        public static bool IsExecutable(string ext) {
            const string EXTS = ".COM|.EXE|.BAT|.CMD|.VBS|.VBE|.JS|.JSE|.WSF|.WSH|.MSC|.LNK";
            return ext != null && ext.Length > 2 && -1 != EXTS.IndexOf(ext.ToUpper());
        }

        public static bool IsNetworkPath(string path) {
            if(path.StartsWith("::")) {
                return false;
            }
            else if(path.StartsWith(@"\\")) {
                return true;
            }
            try {
                if(Path.IsPathRooted(path)) {
                    DriveInfo drive;
                    try {
                        drive = new DriveInfo(Path.GetPathRoot(path));
                    }
                    catch {
                        return false;
                    }
                    if(drive.DriveType == DriveType.Network) {
                        return true;
                    }
                }
            }
            catch {
            }
            return false;
        }

        public static bool IsShellPathButNotFileSystem(string path) {
            path = path.ToLower();
            return ((!path.StartsWith("http://") && !path.StartsWith("ftp://")) && !Path.IsPathRooted(path));
        }

        public static bool IsValidPathChar(char ch) {
            return (((((ch != '"') && (ch != '<')) && ((ch != '>') && (ch != '|'))) && (ch != '*')) && (ch != '?'));
        }

        public static int Make_INT(int x, int y) {
            return ((x & 0xffff) | ((y & 0xffff) << 0x10));
        }

        public static IntPtr Make_LPARAM(int x, int y) {
            return (IntPtr)((x & 0xffff) | ((y & 0xffff) << 0x10));
        }

        public static IntPtr Make_LPARAM(Point pt) {
            return (IntPtr)((pt.X & 0xffff) | ((pt.Y & 0xffff) << 0x10));
        }
        
        public static Color MakeColor(int colorref) {
            return Color.FromArgb(colorref & 0xff, (colorref >> 8) & 0xff, (colorref >> 0x10) & 0xff);
        }

        public static int MakeCOLORREF(Color clr) {
            return ((clr.R | (clr.G << 8)) | (clr.B << 0x10));
        }

        /*
        public static object lockObject = new object();
        //��д���������ļ�д��Ȩ�ޣ�ÿ���߳����εȴ��ϸ�д�����
        static ReaderWriterLockSlim LogWriteLock = new ReaderWriterLockSlim();
        */


        /*
        ������Mutex
        ���壺
            private static readonly Mutex mutex = new Mutex();
            ʹ�ã�
            mutex.WaitOne();
            mutex.ReleaseMutex();
            ���ã�������ס���������ݣ�����ֹ�����߳̽���ô���飬ֱ���ô����������ɣ��ͷŸ�����
         * Mutex�����ǿ���ϵͳ����ģ������ǿ��Կ�Խ���̵ġ�
         */
        
        public static void Close(TextReader sr)
        {
            if (sr == null)
            {
                sr.Close();
                sr.Dispose();
            }
        }

        public static void Close(Stream stream)
        {
            if (stream == null)
            {
                stream.Close();
                stream.Dispose();
            }
        }

        public static void Close(TextWriter sw)
        {
            if (sw == null)
            {
                sw.Close();
                sw.Dispose();
            }
        }

        public static string MakeKeyString(Keys key) {
            if(key == Keys.None) {
                return " - ";
            }
            string str = String.Empty;
            if((key & Keys.Control) == Keys.Control) {
                str = "Ctrl + ";
            }
            if((key & Keys.Shift) == Keys.Shift) {
                str = str + "Shift + ";
            }
            if((key & Keys.Alt) == Keys.Alt) {
                str = str + "Alt + ";
            }
            return (str + ((key & Keys.KeyCode)));
        }

        public static Color MakeModColor(Color clr) {
            float num = 0.875f;
            return Color.FromArgb(((int)((0xff - clr.R) * num)) + clr.R, 
                ((int)((0xff - clr.G) * num)) + clr.G,
                ((int)((0xff - clr.B) * num)) + clr.B);
        }

        public static string MakeNameEllipsis(string name) {
            bool dummy;
            return MakeNameEllipsis(name, out dummy);
        }

        public static string MakeNameEllipsis(string name, out bool fTruncated) {
            fTruncated = false;
            if(name.Length > 40) {
                name = name.Substring(0, 0x25) + "...";
                fTruncated = true;
            }
            return name;
        }

        public static string MakePathDisplayText(string path, bool fToolTip) {
            int index = path.IndexOf("???");
            int length = path.IndexOf("*?*?*");
            if((index != -1) && IsShellPathButNotFileSystem(path)) {
                return path.Substring(0, index);
            }
            if(fToolTip && !path.StartsWith("::")) {
                if(length != -1) {
                    return path.Substring(0, length);
                }
                return path;
            }
            if(((path.Length == 3) && path.EndsWith(@":\")) || (path.StartsWith("::") || (length != -1))) {
                string driveDisplayText;
                lock(QTUtility.syncRoot) {
                    if(QTUtility.DisplayNameCacheDic.TryGetValue(path, out driveDisplayText)) {
                        return driveDisplayText;
                    }
                }
                if(path.Length == 3) {
                    driveDisplayText = GetDriveDisplayText(path);
                }
                else if(length != -1) {
                    string str2 = path.Substring(0, length);
                    if(path.StartsWith("::")) {
                        driveDisplayText = ShellMethods.GetDisplayName(str2);
                    }
                    else {
                        driveDisplayText = str2;
                    }
                }
                else {
                    driveDisplayText = ShellMethods.GetDisplayName(path);
                }
                if(!String.IsNullOrEmpty(driveDisplayText)) {
                    lock(QTUtility.syncRoot) {
                        QTUtility.DisplayNameCacheDic[path] = driveDisplayText;
                    }
                    return driveDisplayText;
                }
                return path;
            }
            if(!path.StartsWith("http://") && !path.StartsWith("ftp://")) {
                try {
                    string fileName = Path.GetFileName(path);
                    if(fileName.Length > 0x40) {
                        fileName = fileName.Substring(0, 0x3d) + "...";
                    }
                    return fileName;
                }
                catch (Exception e)
                {
                    QTLogger.MakeErrorLog(e, "check http ftp");
                }
            }
            return path;
        }

        public static string MakeRootName(string path) {
            if((path == null) || (path.Length <= 2)) {
                return path;
            }
            if(path.StartsWith(@"\\")) {
                int index = path.IndexOf(@"\", 2);
                return index != -1 ? path.Substring(0, index) : path;
            }
            return path.Substring(0, 3);
        }

        public static string MakeVersionString() {
            // qwop comment  ���� .net framework �İ汾��
            if(QTUtility.IS_DEV_VERSION) {
                return "DevBuild: " + AssemblyInfoHelper.GetLinkerTimestamp() + " (" + Environment.Version + ")";
            }
            else {
                string str = QTUtility.CurrentVersion.ToString();
                if(QTUtility.BetaRevision.Major > 0) {
                    str = str + " Beta " + QTUtility.BetaRevision.Major;
                }
                else if(QTUtility.BetaRevision.Minor > 0) {
                    str = str + " Alpha " + QTUtility.BetaRevision.Minor;
                }
                return str;
                /* */
            } 
        }

        public static bool PathEquals(this string str1, string str2) {
            return String.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsValidExecutablePath(string path) {
            if(String.IsNullOrEmpty(path)) return false;
            if(!Path.IsPathRooted(path)) return false;
            string ext = Path.GetExtension(path).ToLower();
            return ext == ".exe" || ext == ".bat" || ext == ".cmd" || ext == ".msi";
        }

        public static bool PathExists(string path) {
            if(String.IsNullOrEmpty(path)) {
                return false;
            }
            path = path.ToLower();
            if(path.StartsWith("::") || path.StartsWith(@"\\") || path.StartsWith("http://") || path.StartsWith("ftp://") || path.Contains("???")) {
                return true;
            }
            if(Path.IsPathRooted(path)) {
                DriveInfo drive;
                try {
                    drive = new DriveInfo(Path.GetPathRoot(path));
                }
                catch (Exception e)
                {
                    QTLogger.MakeErrorLog(e, "new DriveInfo");
                    return false;
                }
                switch(drive.DriveType) {
                    case DriveType.Unknown:
                    case DriveType.NoRootDirectory:
                        return false;
                    case DriveType.Network:
                        return true;
                }
            }
            if(Directory.Exists(path)) {
                return true;
            }
            if(File.Exists(path)) {
                string ext = Path.GetExtension(path).ToLower();
                return (IconManager.ExtIsCompressed(ext) || (!OSDetector.IsXP && (ext == ".search-ms")));
            }
            if(OSDetector.IsXP || ((!path.Contains(@".zip\") && !path.Contains(@".cab\")) && !path.Contains(@".lzh\"))) {
                return !Path.IsPathRooted(path);
            }
            string str2 = String.Empty;
            if(path.Contains(@".zip\")) {
                str2 = @".zip\";
            }
            else if(path.Contains(@".cab\")) {
                str2 = @".cab\";
            }
            else if(path.Contains(@".lzh\")) {
                str2 = @".lzh\";
            }
            return File.Exists(path.Substring(0, path.IndexOf(str2) + 4));
        }

        public static bool PathStartsWith(this string str1, string str2) {
            return str1.StartsWith(str2, StringComparison.OrdinalIgnoreCase);
        }

        public static Point PointFromLPARAM(IntPtr lParam) {
            return new Point(
                (short)(((int)lParam) & 0xffff),
                (short)((((int)lParam) >> 0x10) & 0xffff));
        }

        public static string SanitizePathString(string path) {
            if(path == null) {
                return null;
            }
            path = path.Trim();
            StringBuilder builder = new StringBuilder(path.Length);
            foreach(char ch in path) {
                if(IsValidPathChar(ch) && (ch > '\x001f')) {
                    builder.Append(ch);
                }
            }
            return builder.ToString();
        }

        public static IntPtr SendCOPYDATASTRUCT(IntPtr hWnd, IntPtr wParam, string strMsg, IntPtr dwData) {
            if(String.IsNullOrEmpty(strMsg)) {
                strMsg = "null";
            }
            using(SafePtr hglobal = new SafePtr(strMsg)) {
                COPYDATASTRUCT structure = new COPYDATASTRUCT {
                    lpData = hglobal,
                    cbData = (strMsg.Length + 1)*2,
                    dwData = dwData
                };
                return PInvoke.SendMessage(hWnd, WM.COPYDATA, wParam, ref structure);
            }
        }



        /**
         * �����ַ�����������
         */
        internal static void SetStringClipboard(string str) {
            try {
                Clipboard.SetDataObject(str, true);
                QTUtility.AsteriskPlay();
            }
            catch (Exception e)
            {
                QTLogger.MakeErrorLog(e, "SetStringClipboard");
                QTUtility.SoundPlay();
            }
        }

        /**
         * �����ַ�����������
         */
        internal static string GetStringClipboard()
        {
            try
            {
                if (Clipboard.ContainsText(TextDataFormat.Text))
                {
                    string clipboardText = Clipboard.GetText(TextDataFormat.Text);
                    QTUtility.AsteriskPlay();
                    return clipboardText;
                }
            }
            catch
            {
                QTUtility.SoundPlay();
            }
            return "";
        }
        // �ַ���ͨ���ָ�������
        public static string StringJoin<T>(this IEnumerable<T> list, string separator) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach(T t in list) {
                if(first) first = false;
                else sb.Append(separator);
                sb.Append(t.ToString());
            }
            return sb.ToString();
        }
        // �ַ���ͨ���ָ�������
        public static string StringJoin(this IEnumerable list, string separator) {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach(object t in list) {
                if(first) first = false;
                else sb.Append(separator);
                sb.Append(t.ToString());
            }
            return sb.ToString();
        }

        public static string Replace(this string s, Regex regex, string replaceWith) {
            if (regex!=null) {
                s = regex.Replace(s, replaceWith);
            }
            return s;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="pIDL"></param>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool TargetIsInNoCapture(IntPtr pIDL, string path) {
            if(pIDL != IntPtr.Zero) {
                path = ShellMethods.GetPath(pIDL);
            }
            return !String.IsNullOrEmpty(path) && QTUtility.NoCapturePathsList.Any(path2 => path.PathEquals(path2));
        }

        // [MethodImpl(MethodImplOptions.InternalCall)]
        public static int Round(float f)
        {
            return (int)((double)f + ((double)f > 0.0 ? 0.5 : -0.5));
        }

        public static bool IsDrive(string path)
        {
            if (path != null)
            {
                path = ExpandEnvironmentVariables(path);
                if (path != null)
                {
                    path = path.ToLower();
                    if (path.Length == 2)
                        return 'a' <= path[0] && path[0] <= 'z' && path[1] == ':';
                    if (path.Length == 3 && 'a' <= path[0] && path[0] <= 'z' && path[1] == ':')
                        return path[2] == '\\';
                }
            }
            return false;
        }

        private static string ExpandEnvironmentVariables(string str)
        {
            try
            {
                if (str != null)
                    return Environment.ExpandEnvironmentVariables(str);
            }
            catch
            {
            }
            return str;
        }


        /// <summary>
        ///     A .NET framework 3.5 way to mimic the FX4 "Has Flag" method.
        /// </summary>
        /// <param name="variable">The tested enum</param>
        /// <param name="value">The value to test</param>
        /// <returns>True if the flag is set, otherwise false</returns>
        public static bool HasFlag(Enum variable, Enum value)
        {
            // check if from the same type.
            if (variable.GetType() != value.GetType())
                throw new ArgumentException("The checked flag is not from the same type as the checked variable.");

            Convert.ToUInt64(value);
            ulong num = Convert.ToUInt64(value);
            ulong num2 = Convert.ToUInt64(variable);

            return (num2 & num) == num;
        }

        public static string File2Text(string cTxtPath)
        {
            string value = "";
            using (StreamReader streamReader = new StreamReader(cTxtPath))
            {
                value = streamReader.ReadToEnd();
            }
            return value;
        }

        /// <summary>
        ///    ���ڵ�����Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public static void debugMessage(Message msg)
        {
            
            // if (msg.LParam != null && msg.LParam.ToString().Equals(File2Text(@"c:\1.txt")))
                if (msg.WParam != null && msg.WParam.ToString().Equals(File2Text(@"c:\1.txt")))
            {
                var name = Enum.GetName(typeof(MsgEnum), msg.Msg);
                if (
                    "WM_TIMER".Equals(name) ||
                    "RB_GETBANDBORDERS".Equals(name) ||
                    "WM_NCCALCSIZE".Equals(name) ||
                    "WM_IME_SETCONTEXT".Equals(name) ||
                    "WM_SHOWWINDOW".Equals(name)
                )
                {
                    // ignore 
                    return;
                }

                QTLogger.log("check msg\t " + Enum.GetName(typeof(MsgEnum), msg.Msg) + " msg int " + msg.Msg +
                               "\tw\t" + msg.WParam + "\tl\t" + msg.LParam);
            }
        }

        /// <summary>
        ///    ���ڵ�����Ϣ
        /// </summary>
        /// <param name="msg"></param>
        public static void debugMessage(MSG msg)
        {
            // if (msg.lParam != null && msg.lParam.ToString().Equals(File2Text(@"c:\1.txt")))
            if (msg.wParam != null && msg.wParam.ToString().Equals(File2Text(@"c:\1.txt")))
            {
                var name = Enum.GetName(typeof(MsgEnum), msg.message);
                if ( 
                    "WM_TIMER".Equals(name) ||
                    "RB_GETBANDBORDERS".Equals(name) ||
                    "WM_NCCALCSIZE".Equals(name) ||
                    "WM_IME_SETCONTEXT".Equals(name) ||
                    "WM_SHOWWINDOW".Equals(name)
                    )
                {
                    // ignore 
                    return;
                }
                QTLogger.log("check msg\t " + name + " msg int " + msg.message +
                    "\tw\t" + msg.wParam + "\tl\t" + msg.lParam);
            }
        }

        public static void KillCurrentProcess()
        {
            // TASKKILL /T /PID 1230 /PID 1241 /PID 1253 
            // Process process = Process.GetCurrentProcess();
            // process.Kill();

            // Process.Start("TASKKILL /F /T /PID " + process.Id);
            /*string MyDosComLine1;
            MyDosComLine1 = "TASKKILL /F /T /PID " + process.Id;//���ظ�Ŀ¼����
            Process myProcess = new Process();
            myProcess.StartInfo.FileName = "cmd.exe ";//��DOS����ƽ̨ 
            myProcess.StartInfo.UseShellExecute = false;
            myProcess.StartInfo.CreateNoWindow = true;//�Ƿ���ʾDOS���ڣ�true��������;
            myProcess.StartInfo.RedirectStandardInput = true;
            myProcess.StartInfo.RedirectStandardOutput = true;
            myProcess.StartInfo.RedirectStandardError = true;
            myProcess.Start();
            StreamWriter sIn = myProcess.StandardInput;//��׼������ 
            sIn.AutoFlush = true;
            StreamReader sOut = myProcess.StandardOutput;//��׼������
            StreamReader sErr = myProcess.StandardError;//��׼������ 
            sIn.Write(MyDosComLine1 + Environment.NewLine);//��һ��DOS����
            QTLogger.log("write dos command: " + MyDosComLine1);
            sIn.Write("exit" + Environment.NewLine);//������DOS����˳�DOS����
            if (myProcess.HasExited == false)
            {
                myProcess.Kill();
            }
            else
            {
            }
            sIn.Close();
            sOut.Close();
            sErr.Close();
            myProcess.Close();*/
        }

        public static void Wait4SelectFiles(SHDocVw.WebBrowser explorer)
        {
            if (explorer != null)
            {
                explorer.Quit();
            }
        }

        public static bool IsEmpty(string text)
        {
            return null == text || text.Trim().Length == 0;
        }

        public static bool IsNotEmpty(string text)
        {
            return !IsEmpty(text);
        }

        public static int CurrentProcessId()
        {
            return Process.GetCurrentProcess().Id;
        }
    }
}
