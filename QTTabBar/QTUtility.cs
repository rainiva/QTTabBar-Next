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
        internal static bool fExplorerPrevented;
        internal static bool fRestoreFolderTree;
        internal static bool RestoreFolderTree_Hide;
        // internal static SolidBrush sbAlternate;
       // internal static Font StartUpTabFont;

        /// <summary>
        /// ִֻ��һ��
        /// </summary>
        static QTUtility() {
            if(ShouldSkipProcessInitialization()) {
                QTLogger.log("QTUtility return :" + Process.GetCurrentProcess().ProcessName.ToLower());
                return;
            }

            EmbeddedAssemblyLoader.EnsureRegistered();
            Initialize();
        }


        /// <summary>
        /// Ensures initialization has run. The static constructor calls this on first type load;
        /// explicit entry points may call again to retry after InitializationOrchestrator failure.
        /// </summary>
        public static void Initialize() {
            if(ShouldSkipProcessInitialization()) {
                return;
            }
            EmbeddedAssemblyLoader.EnsureRegistered();
            InitializationOrchestrator.Initialize();
        }

        private static bool ShouldSkipProcessInitialization() {
            string processName = Process.GetCurrentProcess().ProcessName.ToLower();
            return processName == "iexplore" || processName == "regasm" || processName == "gacutil";
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

        /// <summary>
        /// Ensures initialization has run. The static constructor calls this on first type load;
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
