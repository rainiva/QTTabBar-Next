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
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.Win32;

namespace QTTabBarLib {
    /// <summary>
    /// Unified initialization orchestrator. Converges the multiple entry points
    /// that previously triggered (or duplicated) the business initialization
    /// sequence (QTUtility static constructor, QTTabBarClass ctor / EnableApiHook,
    /// OptionsDialog preview path). The sequence below is migrated verbatim from
    /// the former QTUtility static constructor; only a double-checked idempotent
    /// guard is added so the sequence runs exactly once.
    /// </summary>
    internal static class InitializationOrchestrator {

        private static volatile bool _initialized;
        private static readonly object _lock = new object();

        public static void Initialize() {
            if(_initialized) return;
            lock(_lock) {
                if(_initialized) return;
                _initialized = true;
            try {
                QTLogger.log("QTUtility RefreshShellStateValues");
                // RefreshShellStateValues();

                // Load the config
                ConfigManager.Initialize();
                QTLogger.log("QTUtility ��������");
                
                // Initialize the instance manager
                InstanceManager.Initialize();
                QTLogger.log("QTUtility ��ʼ��InstanceManager");

                // Create and enable the API hooks
                HookLibManager.Initialize();
                QTLogger.log("QTUtility ������������ API hooks");

                // Create the global imagelist
                QTUtility.ImageListGlobal = new ImageList { ColorDepth = ColorDepth.Depth32Bit };
                IconManager.AddImageToGlobal("folder", IconManager.GetIcon(string.Empty, false));
                QTLogger.log("QTUtility ����ȫ���ļ���ͼƬ�б�");

                // Load groups/apps
                GroupsManager.LoadGroups();
                QTLogger.log("QTUtility ���ط������");
                
                AppsManager.LoadApps();
                QTLogger.log("QTUtility ����ȫ���ļ���ͼƬ�б�");

                if(Config.Lang.UseLangFile && File.Exists(Config.Lang.LangFile)) {
                    QTUtility.TextResourcesDic = QTResourceManager.ReadLanguageFile(Config.Lang.LangFile);
                }
                QTResourceManager.ValidateTextResources();

                using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                    if(key != null) {
                        using(RegistryKey key2 = key.CreateSubKey("RecentlyClosed")) {
                            if(key2 != null) {
                                List<string> collection = key2.GetValueNames()
                                        .Select(str4 => (string)key2.GetValue(str4)).ToList();
                                StaticReg.ClosedTabHistoryList = new UniqueList<string>(collection, Config.Misc.TabHistoryCount);
                            }
                        }
                        using(RegistryKey key3 = key.CreateSubKey("RecentFiles")) {
                            if(key3 != null) {
                                List<string> list2 = key3.GetValueNames().Select(str5 =>
                                        (string)key3.GetValue(str5)).ToList();
                                StaticReg.ExecutedPathsList = new UniqueList<string>(list2, Config.Misc.FileHistoryCount);
                            }
                        }
                        QTUtility.RefreshLockedTabsList();
                        if(!byte.TryParse((string)key.GetValue("WindowAlpha", "255"), out SessionState.WindowAlpha)) {
                            SessionState.WindowAlpha = 0xff;
                        }
                    }
                }

               

                // ���ò�����������
                /*QTLogger.log("QTUtility ���غ��Ե�·�� ������� ��������");
                string[] theNoCaptures = { "::{26EE0668-A00A-44D7-9371-BEB064C98683}",
                                           "::{26EE0668-A00A-44D7-9371-BEB064C98683}\0",
                                           "::{7007ACC7-3202-11D1-AAD2-00805FC1270E}" };
                foreach (var item in theNoCaptures)
                {
                    if (!NoCapturePathsList.Contains(item))
                    {
                        NoCapturePathsList.Add(item);
                    } 
                }*/
                
                // default add ::{20D04FE0-3AEA-1069-A2D8-08002B30309D};::{26EE0668-A00A-44D7-9371-BEB064C98683}
                /*
                NoCapturePathsList.Add("::{26EE0668-A00A-44D7-9371-BEB064C98683}");
                NoCapturePathsList.Add("::{26EE0668-A00A-44D7-9371-BEB064C98683}\0");

                NoCapturePathsList.Add("::{7007ACC7-3202-11D1-AAD2-00805FC1270E}");// ��������
                */

                // ������� ::{26EE0668-A00A-44D7-9371-BEB064C98683} ::{26EE0668-A00A-44D7-9371-BEB064C98683}\0
              
               // NoCapturePathsList.Add("::{20D04FE0-3AEA-1069-A2D8-08002B30309D}"); // �ҵĵ���
              //  NoCapturePathsList.Add("::{21EC2020-3AEA-1069-A2DD-08002B30309D}"); // ���п������
               // NoCapturePathsList.Add("::{26EE0668-A00A-44D7-9371-BEB064C98683}\\0\\::{ED834ED6-4B5A-4BFE-8F11-A626DCB6A921}");
                
                // ����վ      NoCapturePathsList.Add("::{645FF040-5081-101B-9F08-00AA002F954E}");
                /*
                                               ����վ �C {645FF040-5081-101B-9F08-00AA002F954E}
                               ������� �C {21EC2020-3AEA-1069-A2DD-08002B30309D}
                               ���� �C {2559A1F3-21D7-11D4-BDAF-00C04F60B9F0}
                               ���� �C {2559A1F0-21D7-11D4-BDAF-00C04F60B9F0}
                               Internet Explorer �C {871C5380-42A0-1069-A2EA-08002B30309D}
                               �������� �C {D20EA4E1-3957-11D2-A40B-0C5020524153}
                               �������� �C {7007ACC7-3202-11D1-AAD2-00805FC1270E}
                               ��ӡ���ʹ��� �C {2227A280-3AEA-1069-A2DE-08002B30309D}
                                               */
                // ���ò�����������
                QTUtility.GetShellClickMode();
                QTLogger.log("QTUtility Get Shell Click Mode");

                // Initialize plugins
                PluginManager.Initialize();
                QTLogger.log("QTUtility �������в��");
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
            }
            }
        }
    }
}
