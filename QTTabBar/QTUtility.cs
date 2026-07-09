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
        // 1.5.6.3  edit this 
        internal static readonly Version BetaRevision = new Version(1, 0); // ���汾 beta  �ΰ汾 alpha
        internal static readonly Version CurrentVersion = new Version(1, 5, 6, 7);
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
        private static Version osVersion = OSDetector.OsVersion;
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
        internal static string[] ResMain =>
            TextResourcesDic != null && TextResourcesDic.TryGetValue("TabBar_Menu", out string[] main) ? main : null;
        internal static string[] ResMisc =>
            TextResourcesDic != null && TextResourcesDic.TryGetValue("Misc_Strings", out string[] misc) ? misc : null;
        internal static bool RestoreFolderTree_Hide;
        // internal static SolidBrush sbAlternate;
       // internal static Font StartUpTabFont;
        internal static Dictionary<string, string[]> TextResourcesDic {
            get { return ResourceCache.TextResourcesDic; }
            set { ResourceCache.TextResourcesDic = value; }
        }
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
            //     QTLogger.MakeErrorLog(ex, "QTUtility.RefreshShellStateValues" );
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
                QTLogger.log("QTUtility return :" + processName);
                return;
            }

            EmbeddedAssemblyLoader.EnsureRegistered();

            InitializationOrchestrator.Initialize();
        }


        public static void GetHiddenFileSettings(out bool fShowHidden, out bool fShowSystem) {
            const uint SSF_SHOWALLOBJECTS   = 0x00001;
            const uint SSF_SHOWSUPERHIDDEN  = 0x40000;
            SHELLSTATE ss = new SHELLSTATE();
            PInvoke.SHGetSetSettings(ref ss, SSF_SHOWALLOBJECTS | SSF_SHOWSUPERHIDDEN, false);
            fShowHidden = ss.fShowAllObjects != 0;
            fShowSystem = ss.fShowSuperHidden != 0;
        }

        public static IEnumerable<KeyValuePair<string, string>> GetResourceStrings(this ResourceManager res) {
            var dict = res.GetResourceSet(CultureInfo.CurrentUICulture, true, true);
            var e = dict.GetEnumerator();
            while(e.MoveNext()) {
                yield return new KeyValuePair<string, string>((string)e.Key, (string)e.Value);
            }
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
                QTLogger.MakeErrorLog(exception);
            }
        }

        public static TabBarOption GetTabBarOption() {
            return null; // TODO
        }

        /// <summary>
        /// Triggers the static constructor, which calls InitializationOrchestrator.Initialize().
        /// Entry points must call this method rather than InitializationOrchestrator directly so the
        /// static constructor runs first and initialization stays idempotent.
        /// </summary>
        public static void Initialize() {
            // Intentionally empty — triggers static constructor
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
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    string[] collection = RegistryHelper.ReadRegBinary<string>("TabsLocked", key);
                    if((collection != null) && (collection.Length != 0)) {
                        StaticReg.LockedTabsToRestoreList.Assign(collection);
                    }
                    else {
                        StaticReg.LockedTabsToRestoreList.Assign(Array.Empty<string>());
                    }
                }
            }
        }

        public static void SaveLockedTabs(string[] paths) {
            StaticReg.LockedTabsToRestoreList.Assign(paths ?? Array.Empty<string>());
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null) {
                    RegistryHelper.WriteRegBinary(paths, "TabsLocked", key);
                }
            }
        }

        /**
         * �ǲ��� path ���Ե�
         */
        public static void SaveClosing(List<string> closingPaths) {
            if (null == closingPaths || closingPaths.Count == 0)
            {
                return;
            }
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                if(key != null)
                {
                    string newCloseList =
                        string.Join(";", closingPaths.Where(p => !PathValidator.IsNoCapturePaths(p)).ToArray())
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
        
        public static void SetTabBarOption(TabBarOption tabBarOption, QTTabBarClass tabBar) {
            // TODO
        }

        public static void RefreshNightMode() {
            InNightMode = getNightMode();
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
                        QTLogger.log("can not get reg for personailize");
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
