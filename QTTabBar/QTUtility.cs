//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano
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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using System.Xml;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;
using System.Media;
using System.Runtime.Serialization;
using System.Text;
// using NetSerializer;

namespace QTTabBarLib {
    internal static class QTUtility {
        // 1.5.6.1  edit this 
        internal static readonly Version BetaRevision = new Version(1, 0); // ���汾 beta  �ΰ汾 alpha
        internal static readonly Version CurrentVersion = new Version(1, 5, 6, 0);
        internal static readonly string BuildVerion = "build03";
        internal const int FIRST_MOUSE_ONLY_ACTION = 1000;
        internal static readonly string REG_PERSONALIZE = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        // ��ݼ����ñ�ʶ
        internal const int FLAG_KEYENABLED = 0x100000;
        internal const string IMAGEKEY_FOLDER = "folder";
        internal const string IMAGEKEY_MYNETWORK = "mynetwork";
        internal const string IMAGEKEY_NOEXT = "noext";
        internal const string IMAGEKEY_NOIMAGE = "noimage";
        internal const bool IS_DEV_VERSION = true;  // <----------------- Change me before releasing!
        internal static readonly bool IsRTL = CultureInfo.CurrentCulture.TextInfo.IsRightToLeft;
        internal static readonly bool IsWin7 = Environment.OSVersion.Version >= new Version(6, 1);
        internal static readonly bool IsWin8 = Environment.OSVersion.Version.Major == 6 &&  (Environment.OSVersion.Version.Minor == 2 || Environment.OSVersion.Version.Minor == 3);

        internal static readonly bool IsWin10 = CheckIsWin10(Environment.OSVersion.Version);

        internal static bool CheckIsWin10(Version version) {
            return (version.Major == 10 && version.Build < 22000)
                || (version.Major == 6 && version.Minor == 4);
        }

        internal static readonly bool IsWin11 = (Environment.OSVersion.Version.Major == 10 && Environment.OSVersion.Version.Build >= 22000);

        internal static readonly bool IsThanWin11 = (Environment.OSVersion.Version.Major >= 10 && Environment.OSVersion.Version.Build >= 22000);

        private static Version osVersion = Environment.OSVersion.Version;

        internal static readonly bool IsXP = Environment.OSVersion.Version.Major <= 5;

        internal static readonly string PATH_MYNETWORK = IsXP
                ? "::{208D2C60-3AEA-1069-A2D7-08002B30309D}"
                : "::{F02C1A0D-BE21-4350-88B0-7367FC96EF3C}";
        internal static readonly string PATH_SEARCHFOLDER = IsXP
                ? "::{E17D4FC0-5564-11D1-83F2-00A0C90DC849}"
                : "::{9343812E-1C37-4A49-A12E-4B2D810D956B}";
        internal const string REGUSER = RegConst.Root;
        internal static readonly char[] SEPARATOR_CHAR = new char[] { ';' };
        internal const string SEPARATOR_PATH_HASH_SESSION = "*?*?*";
		// �Ƿ�Ϊ����״̬��
        internal const bool NOW_DEBUGGING =
#if DEBUG
            true;
        
#else
            false;
#endif

        
        // TODO: almost all of these need to be either sync'd or removed.
        // TODO: we should store actual TabItems, not just strings.
        internal static readonly object syncRoot = new object();
        // ImageListGlobal 图标缓存的专用锁（P0-4 线程安全）。仅覆盖集合操作本身，
        // 严禁在锁内嵌套其他锁，以避免与 syncRoot 等锁交叉造成死锁。
        internal static readonly object imageListLock = new object();
        internal static Dictionary<string, string> DisplayNameCacheDic { get { return ResourceCache.DisplayNameCacheDic; } set { ResourceCache.DisplayNameCacheDic = value; } }
        internal static bool fExplorerPrevented;
        internal static bool fRestoreFolderTree;
        internal static bool fSingleClick;
        internal static int iIconUnderLineVal;
        internal static ImageList ImageListGlobal { get { return ResourceCache.ImageListGlobal; } set { ResourceCache.ImageListGlobal = value; } }
        internal static Dictionary<string, byte[]> ITEMIDLIST_Dic_Session { get { return SessionState.ITEMIDLIST_Dic_Session; } set { SessionState.ITEMIDLIST_Dic_Session = value; } }
        internal static List<string> NoCapturePathsList { get { return SessionState.NoCapturePathsList; } set { SessionState.NoCapturePathsList = value; } }
        internal static string[] ResMain;
        internal static string[] ResMisc;
        internal static bool RestoreFolderTree_Hide;
        // internal static SolidBrush sbAlternate;
       // internal static Font StartUpTabFont;
        internal static Dictionary<string, string[]> TextResourcesDic { get { return ResourceCache.TextResourcesDic; } set { ResourceCache.TextResourcesDic = value; } }
        internal static byte WindowAlpha { get { return System.Threading.Volatile.Read(ref SessionState.WindowAlpha); } set { System.Threading.Volatile.Write(ref SessionState.WindowAlpha, value); } }

        // �Ƿ�Ϊ����ģʽ
        internal static bool InNightMode;

        // {
        //     get { return getNightMode(); }
        //     set { InNightMode = value;  }
        // }


        ///////////////////////// ���� by indiff ////////////////////////////////////
        internal static bool SingleClickMode { get; private set; }

        internal static bool ShowInfoTip { get; private set; }
        /**
         * ˢ��״̬
         */
        public static void RefreshShellStateValues()
        {
            // try
            // {
                /*bool flag1 = false;
                bool flag2 = true;
                using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer", false))
                {
                    if (registryKey != null)
                    {
                        var value = registryKey.GetValue("ShellState");
                        if (value != null)
                        {
                            if (value.GetType().BaseType == typeof(Array))
                            {
                                byte[] numArray = (byte[]) value;
                                if (numArray.Length > 3)
                                    flag1 = ((int)numArray[4] & 32) == 0;
                            }
                        }
                        
                        using (RegistryKey rk = registryKey.OpenSubKey("Advanced", false))
                        {
                            if (rk != null)
                                flag2 = QTUtility2.GetValueSafe<int>(rk, "ShowInfoTip", 1) != 0;
                        }
                    }
                }
                SingleClickMode = flag1;
                ShowInfoTip = flag2;*/
                InNightMode = true; // getNightMode();
            // }
            // catch (Exception ex)
            // {
            //     QTUtility2.MakeErrorLog(ex, "QTUtility.RefreshShellStateValues" );
            // }
        }
        ///////////////////////// ���� by indiff ////////////////////////////////////



        /// <summary>
        /// ִֻ��һ��
        /// </summary>
        static QTUtility() {
            // I'm tempted to just return for everything except "explorer"
            // Maybe I should...
            String processName = Process.GetCurrentProcess().ProcessName.ToLower();
            if(processName == "iexplore" || processName == "regasm" || processName == "gacutil") {
                QTUtility2.log("QTUtility return :" + processName);
                return;
            }

            // Register a callback for AssemblyResolve in order to load embedded assemblies.
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) => {
                var requestedName = new AssemblyName(args.Name);
                foreach(var loaded in AppDomain.CurrentDomain.GetAssemblies()) {
                    if(string.Equals(loaded.GetName().Name, requestedName.Name, StringComparison.OrdinalIgnoreCase)) {
                        return loaded;
                    }
                }
                String resourceName = "QTTabBarLib.Resources." + requestedName.Name + ".dll";
                using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                    if(stream == null) return null;
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    QTUtility2.Close(stream);
                    return Assembly.Load(assemblyData);
                }
            };

            InitializationOrchestrator.Initialize();
        }


        public static object ByteArrayToObject(byte[] arrBytes) {
            return SerializationHelper.ByteArrayToObject(arrBytes);
        }

        private readonly static string[] strIconExt = new string[] { ".exe", ".lnk", ".ico", ".url", ".sln" };
        public static bool ExtHasIcon(string ext) {
            return strIconExt.Contains(ext);
        }

        private readonly static string[] strCompressedExt = new string[] { ".zip", ".lzh", ".cab" };

        public static bool ExtIsCompressed(string ext) {
            return strCompressedExt.Contains(ext);
        }

        public static void GetHiddenFileSettings(out bool fShowHidden, out bool fShowSystem) {
            const uint SSF_SHOWALLOBJECTS   = 0x00001;
            const uint SSF_SHOWSUPERHIDDEN  = 0x40000;
            SHELLSTATE ss = new SHELLSTATE();
            PInvoke.SHGetSetSettings(ref ss, SSF_SHOWALLOBJECTS | SSF_SHOWSUPERHIDDEN, false);
            fShowHidden = ss.fShowAllObjects != 0;
            fShowSystem = ss.fShowSuperHidden != 0;
        }

        public static Icon GetIcon(IntPtr pIDL) {
            SHFILEINFO psfi = new SHFILEINFO();
            if((IntPtr.Zero != PInvoke.SHGetFileInfo(pIDL, 0, ref psfi, Marshal.SizeOf(psfi), 0x109)) && (psfi.hIcon != IntPtr.Zero)) {
                Icon icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                PInvoke.DestroyIcon(psfi.hIcon);
                return icon;
            }
            return Resources_Image.icoEmpty;
        }

        public static Icon GetIcon(string path, bool fExtension) {
            Icon icon;
            SHFILEINFO psfi = new SHFILEINFO();
            if(fExtension) {
                if(path.Length == 0) {
                    path = ".*";
                }
                if((IntPtr.Zero != PInvoke.SHGetFileInfo("*" + path, 0x80, ref psfi, Marshal.SizeOf(psfi), 0x111)) && (psfi.hIcon != IntPtr.Zero)) {
                    icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                    PInvoke.DestroyIcon(psfi.hIcon);
                    return icon;
                }
                return Resources_Image.icoEmpty;
            }
            if(path.Length == 0) {
                if((IntPtr.Zero != PInvoke.SHGetFileInfo("dummy", 0x10, ref psfi, Marshal.SizeOf(psfi), 0x111)) && (psfi.hIcon != IntPtr.Zero)) {
                    icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                    PInvoke.DestroyIcon(psfi.hIcon);
                    return icon;
                }
                return Resources_Image.icoEmpty;
            }
            if(!IsXP && path.StartsWith("::")) {
                IntPtr pszPath = PInvoke.ILCreateFromPath(path);
                if(pszPath != IntPtr.Zero) {
                    if((IntPtr.Zero != PInvoke.SHGetFileInfo(pszPath, 0, ref psfi, Marshal.SizeOf(psfi), 0x109)) && (psfi.hIcon != IntPtr.Zero)) {
                        icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                        PInvoke.DestroyIcon(psfi.hIcon);
                        PInvoke.CoTaskMemFree(pszPath);
                        return icon;
                    }
                    PInvoke.CoTaskMemFree(pszPath);
                }
            }
            else if((IntPtr.Zero != PInvoke.SHGetFileInfo(path, 0, ref psfi, Marshal.SizeOf(psfi), 0x101)) && (psfi.hIcon != IntPtr.Zero)) {
                icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                PInvoke.DestroyIcon(psfi.hIcon);
                return icon;
            }
            return Resources_Image.icoEmpty;
        }

        public static string GetImageKey(string path, string ext) {
            if(!string.IsNullOrEmpty(path)) {
                if(QTUtility2.IsNetworkPath(path)) {
                    if(ext != null) {
                        ext = ext.ToLower();
                        if(ext.Length == 0) {
                            SetImageKey("noext", path);
                            return "noext";
                        }
                        if(!ImageGlobalContainsKey(ext)) {
                            AddImageToGlobal(ext, GetIcon(ext, true));
                        }
                        return ext;
                    }
                    if(IsNetworkRootFolder(path)) {
                        SetImageKey(path, path);
                        return path;
                    }
                    SetImageKey("mynetwork", PATH_MYNETWORK);
                    return "mynetwork";
                }
                if(path.StartsWith("::")) {
                    SetImageKey(path, path);
                    return path;
                }
                if(ext != null) {
                    ext = ext.ToLower();
                    if(ext.Length == 0) {
                        SetImageKey("noext", path);
                        return "noext";
                    }
                    if(ExtHasIcon(ext)) {
                        SetImageKey(path, path);
                        return path;
                    }
                    SetImageKey(ext, path);
                    return ext;
                }
                if(path.Contains("*?*?*")) {
                    byte[] buffer;
                    if(ImageGlobalContainsKey(path)) {
                        return path;
                    }
                    // 先在 syncRoot 下取出缓存数据并释放该锁，随后再加 imageListLock 写入，
                    // 避免两把锁嵌套。
                    bool found;
                    lock(syncRoot) {
                        found = ITEMIDLIST_Dic_Session.TryGetValue(path, out buffer);
                    }
                    if(found) {
                        using(IDLWrapper w = new IDLWrapper(buffer)) {
                            if(w.Available) {
                                AddImageToGlobal(path, GetIcon(w.PIDL));
                                return path;
                            }
                        }
                    }
                    return "noimage";
                }
                if(QTUtility2.IsShellPathButNotFileSystem(path)) {
                    IDLWrapper wrapper;
                    if(ImageGlobalContainsKey(path)) {
                        return path;
                    }
                    if(IDLWrapper.TryGetCache(path, out wrapper)) {
                        using(wrapper) {
                            if(wrapper.Available) {
                                AddImageToGlobal(path, GetIcon(wrapper.PIDL));
                                return path;
                            }
                        }
                    }
                    return "noimage";
                }
                if(path.StartsWith("ftp://") || path.StartsWith("http://")) {
                    return "folder";
                }
                try {
                    DirectoryInfo info = new DirectoryInfo(path);
                    if(info.Exists) {
                        FileAttributes attributes = info.Attributes;
                        if(((attributes & FileAttributes.System) != 0) || ((attributes & FileAttributes.ReadOnly) != 0)) {
                            SetImageKey(path, path);
                            return path;
                        }
                        return "folder";
                    }
                    if(File.Exists(path)) {
                        ext = Path.GetExtension(path).ToLower();
                        if(ext.Length == 0) {
                            SetImageKey("noext", path);
                            return "noext";
                        }
                        if(ExtHasIcon(ext)) {
                            SetImageKey(path, path);
                            return path;
                        }
                        SetImageKey(ext, path);
                        return ext;
                    }
                    if(path.ToLower().Contains(@".zip\")) {
                        return "folder";
                    }
                }
                catch {
                }
            }
            return "noimage";
        }

        public static DateTime GetLinkerTimestamp() {
            string filePath = System.Reflection.Assembly.GetCallingAssembly().Location;
            const int c_PeHeaderOffset = 60;
            const int c_LinkerTimestampOffset = 8;
            byte[] buf = new byte[2048];
            Stream stream = null;

            try {
                stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                stream.Read(buf, 0, 2048);
            }
            finally {
                QTUtility2.Close(stream);
            }

            int offset = BitConverter.ToInt32(buf, c_PeHeaderOffset);
            int secondsSince1970 = BitConverter.ToInt32(buf, offset + c_LinkerTimestampOffset);
            DateTime dt = new DateTime(1970, 1, 1, 0, 0, 0);
            dt = dt.AddSeconds(secondsSince1970);
            dt = dt.AddHours(TimeZone.CurrentTimeZone.GetUtcOffset(dt).Hours);
            return dt;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetResourceStrings(this ResourceManager res) {
            var dict = res.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            var e = dict.GetEnumerator();
            while(e.MoveNext()) {
                yield return new KeyValuePair<string, string>((string)e.Key, (string)e.Value);
            }
        }

        public static T[] GetSettingValue<T>(T[] inputValues, T[] defaultValues, bool fClone) {
            if((inputValues == null) || (inputValues.Length == 0)) {
                if(!fClone) {
                    return defaultValues;
                }
                return (T[])defaultValues.Clone();
            }
            int length = defaultValues.Length;
            int num2 = inputValues.Length;
            T[] localArray = new T[length];
            for(int i = 0; i < length; i++) {
                if(i < num2) {
                    localArray[i] = inputValues[i];
                }
                else {
                    localArray[i] = defaultValues[i];
                }
            }
            return localArray;
        }

        public static void GetShellClickMode() {
            const string lpSubKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
            iIconUnderLineVal = 0;
            int lpcbData = 4;
            try {
                IntPtr ptr;
                if(PInvoke.RegOpenKeyEx((IntPtr)(-2147483647), lpSubKey, 0, 0x20019, out ptr) == 0) {
                    using(SafePtr lpData = new SafePtr(4)) {
                        int num2;
                        if(PInvoke.RegQueryValueEx(ptr, "IconUnderline", IntPtr.Zero, out num2, lpData, ref lpcbData) == 0) {
                            byte[] destination = new byte[4];
                            Marshal.Copy(lpData, destination, 0, 4);
                            iIconUnderLineVal = destination[0];
                        }                        
                    }
                    PInvoke.RegCloseKey(ptr);
                }
                using(RegistryKey key = Registry.CurrentUser.OpenSubKey(lpSubKey, false)) {
                    byte[] buffer2 = (byte[])key.GetValue("ShellState");
                    fSingleClick = false;
                    if((buffer2 != null) && (buffer2.Length > 3)) {
                        byte num3 = buffer2[4];
                        fSingleClick = (num3 & 0x20) == 0;
                    }
                }
            }
            catch(Exception exception) {
                QTUtility2.MakeErrorLog(exception);
            }
        }

        public static TabBarOption GetTabBarOption() {
            return null; // TODO
        }

        private static bool IsNetworkRootFolder(string path) {
            string str = path.Substring(2);
            int index = str.IndexOf(Path.DirectorySeparatorChar);
            if(index != -1) {
                string str2 = str.Substring(index + 1);
                if(str2.Length > 0) {
                    return (str2.IndexOf(Path.DirectorySeparatorChar) == -1);
                }
            }
            return false;
        }

        public static void Initialize() {
            // This method exists just to cause the static constructor to fire, if it hasn't already.
        }

        public static void LoadReservedImage(ImageReservationKey irk) {
            if(ImageGlobalContainsKey(irk.ImageKey)) {
                return;
            }
            switch(irk.ImageType) {
                case 0:
                    if(irk.ImageKey != "noimage") {
                        if(irk.ImageKey == "noext") {
                            AddImageToGlobal("noext", GetIcon(string.Empty, true));
                            return;
                        }
                        return;
                    }
                    return;

                case 1:
                    AddImageToGlobal(irk.ImageKey, GetIcon(irk.ImageKey, true));
                    return;

                case 2:
                case 4:
                    AddImageToGlobal(irk.ImageKey, GetIcon(irk.ImageKey, false));
                    return;

                case 3:
                    return;

                case 5:
                    // 先在 syncRoot 下取缓存并释放，避免与 imageListLock 嵌套。
                    byte[] buffer;
                    bool found5;
                    lock(syncRoot) {
                        found5 = ITEMIDLIST_Dic_Session.TryGetValue(irk.ImageKey, out buffer);
                    }
                    if(found5) {
                        using(IDLWrapper w = new IDLWrapper(buffer)) {
                            if(w.Available) {
                                AddImageToGlobal(irk.ImageKey, GetIcon(w.PIDL));
                            }
                        }
                    }
                    return;

                case 6:
                    IDLWrapper wrapper;
                    if(IDLWrapper.TryGetCache(irk.ImageKey, out wrapper)) {
                        using(wrapper) {
                            if(wrapper.Available) {
                                AddImageToGlobal(irk.ImageKey, GetIcon(wrapper.PIDL));
                            }
                        }
                    }
                    return;
            }
        }

        public static MouseChord MakeMouseChord(MouseChord button, Keys modifiers) {
            if((modifiers & Keys.Shift) != 0) button |= MouseChord.Shift;
            if((modifiers & Keys.Control) != 0) button |= MouseChord.Ctrl;
            if((modifiers & Keys.Alt) != 0) button |= MouseChord.Alt;
            return button;
        }

        // private static Serializer ser;

        public static IEnumerable<Type> GetSubclasses(Type type)
        {
            return type.Assembly.GetTypes().Where(t => t.IsSubclassOf(type));
        }

        public static byte[] ObjectToByteArray(SerializeDelegate obj) {
            return SerializationHelper.ObjectToByteArray(obj);
        }

        private static Regex singleLinebreakAtStart = new Regex(@"^(\r\n)?");
        private static bool IsWindows10
        {
            get
            {
                if (QTUtility.osVersion.Major >= 10)
                    return true;
                return QTUtility.osVersion.Major == 6 && QTUtility.osVersion.Minor == 4;
            }
        }

        public static bool LaterThan7
        {
            get
            {
                if (QTUtility.osVersion.Major > 6)
                    return true;
                return QTUtility.osVersion.Major == 6 && QTUtility.osVersion.Minor > 1;
            }
        }

        public static bool LaterThan8_1
        {
            get
            {
                return IsWindows10AndLater || IsWindows8_1;
            }
        }

        public static bool IsWindows8_1
        {
            get
            {
                return QTUtility.osVersion.Major == 6 && QTUtility.osVersion.Minor == 3;
            }
        }

        public static bool IsWindows10AndLater
        {
            get
            {
                if (QTUtility.osVersion.Major >= 10)
                    return true;
                return QTUtility.osVersion.Major == 6 && QTUtility.osVersion.Minor == 4;
            }
        }
        public static bool LaterThan10Beta17666 
        {
            get
            {
                if (QTUtility.IsWindows10AndLater)
                    return true;
                return QTUtility.IsWindows10 && QTUtility.osVersion.Build >= 17666;
            }
        }

        private static bool rtl = CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft;

        public static bool RightToLeft
        {
            get
            {
                return rtl;
            }
        }

        public static bool IsWindows7
        {
            get
            {
                return osVersion.Major == 6 && osVersion.Minor == 1;
            }
        }

        public static bool IsJapanese
        {
            get { return CultureInfo.CurrentUICulture.Name == "ja-JP"; }
        }

        public static bool IsChinese
        {
            get { return CultureInfo.CurrentUICulture.Name == "zh-CN"; }
        }

        public static string DefaultFontName
        {
            get
            {
                return IsJapanese && IsWindows10AndLater ? "Yu Gothic UI" : "Arial";
            }
        }

        public static Dictionary<string, string[]> ReadLanguageFile(string path) {
          //  const string linebreak = "\r\n";
          //  const string linebreakLiteral = @"\r\n";

            //We have to remove the first linebreak in the XML element's value, before we can split 
            //on the linebreak. It's there in the XML, when the XML is created using the editor.
            //Other linebreaks should be left in place, even if the line is empty, in order to preserve
            //the relative places of the other substrings.
            //The simplest way to do this is with a regular expression.

            try {
               /* var dictionary = XElement.Load(path).Elements().ToDictionary(
                    element => element.Name.ToString(),
                    element => {
                        string[] substrings =
                            ((string)element)
                            .Replace(singleLinebreakAtStart, "")
                            .Split(new[] { linebreak }, StringSplitOptions.None)
                            .Select(
                                s => s.Replace(linebreakLiteral, linebreak)
                            )
                            .ToArray();
                        return substrings;
                    }
                );*/
                const string newValue = "\r\n";
                const string oldValue = @"\r\n";
                Dictionary<string, string[]> dictionary = new Dictionary<string, string[]>();

                using (XmlTextReader reader = new XmlTextReader(path))
                {
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element || reader.Name == "root") continue;
                        string[] str = reader.ReadString().Split(new string[] { newValue }, StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < str.Length; i++)
                        {
                            str[i] = str[i].Replace(oldValue, newValue);
                        }
                        dictionary[reader.Name] = str;
                    }
                    reader.Close();
                }
                return dictionary;
            } catch (XmlException xmlException) {
                string msg = String.Join("\r\n", new[] {
                    "Invalid language file.",
                    "",
                    xmlException.SourceUri,
                    "Line: " + xmlException.LineNumber,
                    "Position: " + xmlException.LinePosition,
                    "Detail: " + xmlException.Message
                });
                MessageBox.Show(msg);
                return null;
            } catch (Exception exception) {
                QTUtility2.MakeErrorLog(exception);
                return null;
            }
        }
       // private string QTTabBar = @"Software\QTTabBar\Config\Misc";
     

        
        public static void AsteriskPlay()
        {
            if (Config.Misc.SoundBox) {
                SystemSounds.Asterisk.Play();
            }
        }


        public static void SoundPlay()
        {
            if (Config.Misc.SoundBox)
            {
                SystemSounds.Hand.Play();
            }
        }

        public static void RefreshLockedTabsList() {
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                if(key != null) {
                    string[] collection = QTUtility2.ReadRegBinary<string>("TabsLocked", key);
                    if((collection != null) && (collection.Length != 0)) {
                        StaticReg.LockedTabsToRestoreList.Assign(collection);
                    }
                    else {
                        StaticReg.LockedTabsToRestoreList.Clear();
                    }
                }
            }
        }

        public static ImageReservationKey ReserveImageKey(QMenuItem qmi, string path, string ext) {
            ImageReservationKey key = null;
            if(string.IsNullOrEmpty(path)) {
                return new ImageReservationKey("noimage", 0);
            }
            if(!string.IsNullOrEmpty(ext)) {
                ext = ext.ToLower();
                if(ExtHasIcon(ext) && !QTUtility2.IsNetworkPath(path)) {
                    return new ImageReservationKey(path, 2);
                }
                return new ImageReservationKey(ext, 1);
            }
            if(QTUtility2.IsNetworkPath(path)) {
                if(IsNetworkRootFolder(path)) {
                    return new ImageReservationKey(path, 4);
                }
                return new ImageReservationKey("folder", 3);
            }
            if(path.StartsWith("::")) {
                return new ImageReservationKey(path, 4);
            }
            if(path.Contains("*?*?*")) {
                return new ImageReservationKey(path, 5);
            }
            if(QTUtility2.IsShellPathButNotFileSystem(path)) {
                return new ImageReservationKey(path, 6);
            }
            if(path.StartsWith("ftp://") || path.StartsWith("http://")) {
                return new ImageReservationKey("folder", 3);
            }
            try {
                if(qmi.Exists) {
                    if(qmi.Target == MenuTarget.Folder) {
                        if(qmi.HasIcon) {
                            return new ImageReservationKey(path, 4);
                        }
                        return new ImageReservationKey("folder", 3);
                    }
                    if(qmi.Target == MenuTarget.File) {
                        ext = Path.GetExtension(path).ToLower();
                        if(ext.Length == 0) {
                            return new ImageReservationKey("noext", 0);
                        }
                        if(ExtHasIcon(ext)) {
                            return new ImageReservationKey(path, 2);
                        }
                        return new ImageReservationKey(ext, 1);
                    }
                }
                DirectoryInfo info = new DirectoryInfo(path);
                if(info.Exists) {
                    FileAttributes attributes = info.Attributes;
                    if(((attributes & FileAttributes.System) != 0) || ((attributes & FileAttributes.ReadOnly) != 0)) {
                        return new ImageReservationKey(path, 4);
                    }
                    return new ImageReservationKey("folder", 3);
                }
                if(!File.Exists(path)) {
                    return new ImageReservationKey("noimage", 0);
                }
                ext = Path.GetExtension(path).ToLower();
                if(ext.Length == 0) {
                    return new ImageReservationKey("noext", 0);
                }
                if(ExtHasIcon(ext)) {
                    return new ImageReservationKey(path, 2);
                }
                key = new ImageReservationKey(ext, 1);
            }
            catch {
            }
            return key;
        }

        /**
         * �ǲ��� path ���Ե�
         */
        public static void SaveClosing(List<string> closingPaths) {
            if (null == closingPaths || closingPaths.Count == 0)
            {
                return;
            }
            using(RegistryKey key = Registry.CurrentUser.CreateSubKey(RegConst.Root)) {
                if(key != null)
                {
                    string newCloseList =
                        string.Join(";", closingPaths.Where(p => !IsNoCapturePaths(p)).ToArray())
                        ;
                    key.SetValue("TabsOnLastClosedWindow", newCloseList);
                }
            }
        }

        public static void SaveRecentFiles(RegistryKey rkUser) {
            if(rkUser != null) {
                using(RegistryKey key = rkUser.CreateSubKey("RecentFiles")) {
                    if(key != null) {
                        foreach(string str in key.GetValueNames()) {
                            key.DeleteValue(str, false);
                        }
                        for(int i = 0; i < StaticReg.ExecutedPathsList.Count; i++) {
                            key.SetValue(i.ToString(), StaticReg.ExecutedPathsList[i]);
                        }
                    }
                }
            }
        }

        public static void SaveRecentlyClosed(RegistryKey rkUser) {
            if(rkUser != null) {
                using(RegistryKey key = rkUser.CreateSubKey("RecentlyClosed")) {
                    if(key != null) {
                        foreach(string str in key.GetValueNames()) {
                            key.DeleteValue(str, false);
                        }
                        for(int i = 0; i < StaticReg.ClosedTabHistoryList.Count; i++) {
                            key.SetValue(i.ToString(), StaticReg.ClosedTabHistoryList[i]);
                        }
                    }
                }
            }
        }
        
        // �ж�ͼƬ�б�����Ϊ��
        private static void SetImageKey(string key, string itemPath) {
            // 快速路径：已存在则无需提取图标（加锁读取以避免与并发写入产生竞态）。
            if(ImageGlobalContainsKey(key)) {
                return;
            }
            // 图标提取（可能较慢）在锁外进行，统一经 AddImageToGlobal 入口写入。
            AddImageToGlobal(key, GetIcon(itemPath, false));
        }

        // ���� ImageListGlobal ͼ�껺��ķ��ʣ�P0-4 �̰߳�ȫ��ͳһ��ڣ���
        // 所有对 ImageListGlobal.Images 的 Add / ContainsKey / 索引访问均应经由
        // 下列方法，以 imageListLock 保护集合操作。锁范围保持短小，仅覆盖集合操作。
        internal static void AddImageToGlobal(string key, Image image) {
            lock(imageListLock) {
                if(!ImageListGlobal.Images.ContainsKey(key)) {
                    ImageListGlobal.Images.Add(key, image);
                }
            }
        }

        internal static void AddImageToGlobal(string key, Icon icon) {
            lock(imageListLock) {
                if(!ImageListGlobal.Images.ContainsKey(key)) {
                    ImageListGlobal.Images.Add(key, icon);
                }
            }
        }

        internal static bool ImageGlobalContainsKey(string key) {
            lock(imageListLock) {
                return ImageListGlobal != null
                    && ImageListGlobal.Images != null
                    && ImageListGlobal.Images.ContainsKey(key);
            }
        }

        internal static Image GetImageFromGlobal(string key) {
            lock(imageListLock) {
                return ImageListGlobal.Images[key];
            }
        }

        public static void SetTabBarOption(TabBarOption tabBarOption, QTTabBarClass tabBar) {
            // TODO
        }

        public static void ValidateMinMax(ref int value, int min, int max) {
            value = ValidateMinMax(value, min, max);
        }

        // �ж��Ƿ�Ϊ����ģʽ  Environment.OSVersion.Version.Major
        public static bool getNightMode()
        {
            // if (Environment.OSVersion.Version.Major > 9)  {
                /*using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize"))
                {
                    if (rk != null)
                        return QTUtility2.GetValueSafe<int>(rk, "AppsUseLightTheme", 1) == 0;
                }*/

                using (var envKey = Registry.CurrentUser.OpenSubKey(REG_PERSONALIZE, true))
                {
                    if (envKey == null)
                    {
                        QTUtility2.log("can not get reg for personailize");
                        return false;
                    }
                    object value = envKey.GetValue("AppsUseLightTheme");
                    if (value != null)
                    {
                        string useTheme = value.ToString();
                        if ("1".Equals(useTheme))
                        {
                            // the light
                            return false;
                        }
                        else
                        {
                            // the dark mode
                            return true;
                        }
                    }
                    else
                    {
                        // default is light
                        return false;
                    }
                }
            // }
           return true;
        }



        public static int ValidateMinMax(int value, int min, int max) {
            int a = Math.Min(min, max);
            int b = Math.Max(min, max);
            if(value < a) {
                value = a;
            }
            else if(value > b) {
                value = b;
            }
            return value;
        }

        public static void ValidateTextResources() {
            Dictionary<string, string[]> dict = ResourceCache.TextResourcesDic;
            ValidateTextResources(ref dict);
            lock(syncRoot) {
                ResourceCache.TextResourcesDic = dict;
            }
            ResMain = TextResourcesDic["TabBar_Menu"];
            ResMisc = TextResourcesDic["Misc_Strings"];
            Resx.UpdateAll();
        }
    
        public static void ValidateTextResources(ref Dictionary<string, string[]> dict)
        {
            // MessageBox.Show("Config.Lang.UseLangFile:" + Config.Lang.UseLangFile + ",dict == null:" + (dict == null));
            // ��Ҫ���˵ĵ��� url
            string[] urlKeys = { "SiteURL", "PayPalURL" };
            
            // dict �ļ��
            if (dict == null)
            {
                dict = new Dictionary<string, string[]>();
            }

            // ������������,�ڴ˿�������������
            IEnumerable<KeyValuePair<string, string>> keyValuePairs = null;
            switch (Config.Lang.BuiltInLangSelectedIndex)
            {
                case 0: keyValuePairs = Resources_String.ResourceManager.GetResourceStrings(); break;
                case 1: keyValuePairs = Resource_String_zh_CN.ResourceManager.GetResourceStrings(); break;
                case 2: keyValuePairs = Resources_String_de_DE.ResourceManager.GetResourceStrings(); break;
                case 3: keyValuePairs = Resources_String_pt_BR.ResourceManager.GetResourceStrings(); break;
                case 4: keyValuePairs = Resources_String_es_ES.ResourceManager.GetResourceStrings(); break;
                case 5: keyValuePairs = Resources_String_fr_FR.ResourceManager.GetResourceStrings(); break;
                case 6: keyValuePairs = Resources_String_tr_TR.ResourceManager.GetResourceStrings(); break;
                case 7: keyValuePairs = Resources_String_ru_RU.ResourceManager.GetResourceStrings(); break;
            }

            // �������Ϊ�գ� ���ȡĬ�ϵ�Ӧ������
            if (null == keyValuePairs)
            {
                keyValuePairs = Resources_String.ResourceManager.GetResourceStrings();
            }

            // �ж��Ƿ�δʹ����������,����ǵĻ�����ֱ�ӱ��� ��������
            if ( !Config.Lang.UseLangFile )
            {
                foreach (var pair in keyValuePairs)
                {
                    dict[pair.Key] = pair.Value.Split(SEPARATOR_CHAR);
                }
            }
            else // �����ⲿ�����ļ�
            {
                // ������������
                foreach (var pair in keyValuePairs)
                {
                    if (urlKeys.Contains(pair.Key)) continue;
                    // �ֺŷָ��ַ������������ʽ
                    string[] buildinValue = pair.Value.Split(SEPARATOR_CHAR);
                    string[] res;
                    dict.TryGetValue(pair.Key, out res);
                    if (res == null) // ����� dict ��δ��ȡ����Ӧ�� ֵ�� ��� �������Ը��ǵ�.
                    {
                        dict[pair.Key] = buildinValue;
                    }
                    else if (res.Length < buildinValue.Length)// �����ȡ�����������������Ե���Ŀ��һ��
                    {
                        int len = res.Length;
                        Array.Resize(ref res, buildinValue.Length);
                        Array.Copy(buildinValue, len, res, len, buildinValue.Length - len);
                        dict[pair.Key] = res;
                    }
                }
            }
        }


        public static bool isChinese()
        {
            var uiCulture = System.Globalization.CultureInfo.InstalledUICulture.Name;
            var lUiCulture = uiCulture.ToLower();
            if (uiCulture.Equals("zh-CN") || lUiCulture.Equals("zh") || lUiCulture.Equals("cn"))
            {
                return true;
            }
            else if (uiCulture.Equals("de_DE") || lUiCulture.Equals("de"))
            {
            }
            else if (uiCulture.Equals("pt_BR") || lUiCulture.Equals("br") || lUiCulture.Equals("pt"))
            {
            }
            else if (uiCulture.Equals("es_ES") || lUiCulture.Equals("es"))
            {
            }
            else if (uiCulture.Equals("fr_FR") || lUiCulture.Equals("fr"))
            {
            }
            else if (uiCulture.Equals("tr_TR") || lUiCulture.Equals("tr"))
            {
            }
            else
            {
            }

            return false;
        }

        internal static string DefaultNewFileName()
        {
            return isChinese() ? "�½��ı��ĵ�" : "newDocument";
        }


        public static bool IsEmptyStr(string strs)
        {
            return strs == null || strs.Trim().Length == 0;
        }

        public static bool IsNetPath(string path)
        {
            return !IsEmptyStr(path) && path.StartsWith(@"\\");
        }

        public static bool IsNoCapturePaths(string path)
        {
            // �������
            string controlPanel = "::{26EE0668-A00A-44D7-9371-BEB064C98683}";
            string print = @"::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{2227A280-3AEA-1069-A2DE-08002B30309D}";// ��ӡ��
            return !IsEmptyStr(path) && (
                path.StartsWith(controlPanel) ||
                path.StartsWith(print) 
                );
        }

        public static bool IsSimpleDateStr(string input)
        {
            if (IsEmptyStr(input))
            {
                return false;
            }
            string pattern = @"\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}";
            return Regex.IsMatch(input, pattern);
        }

        public static bool IsShortDateStr(string input)
        {
            if (IsEmptyStr(input))
            {
                return false;
            }
            string pattern = @"\d{1,2}/\d{1,2}/\d{1,2}\s��[һ|��|��|��|��|��|��]\s\d{1,2}:\d{1,2}:\d{1,2}";
            return Regex.IsMatch(input, pattern);
        }

        // c# ��ȡ��ǰ���̵ĸ�����
        public static string GetParentProcessName()
        {
            Process currentProcess = Process.GetCurrentProcess();
            var process = GetParent( currentProcess );
            if (process == null)
            {
                return "";
            }
            else
            {
                return process.ProcessName;
            }
        }

        /// <summary>
        /// ��ȡ�����̡�����������ܷ���null
        /// </summary>
        /// <param name="process"></param>
        /// <returns></returns>
        public static Process GetParent(Process process)
        {
            try
            {
                //using (var query = new ManagementObjectSearcher("SELECT * FROM Win32_Process WHERE ProcessId=" + process.Id))
                using (var query = new ManagementObjectSearcher("root\\CIMV2", "SELECT ParentProcessId FROM Win32_Process WHERE ProcessId=" + process.Id))
                {
                    return query
                        .Get()
                        .OfType<ManagementObject>()
                        .Select(p => Process.GetProcessById((int)(uint)p["ParentProcessId"]))
                        .FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }

     

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        /*public static Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = PInvoke.NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);
            if (status != 0)
                throw new Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }*/
    }
}
