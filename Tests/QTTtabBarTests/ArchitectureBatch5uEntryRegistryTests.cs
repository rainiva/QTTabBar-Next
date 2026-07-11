using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5uEntryRegistryTests {
        private static readonly string[] QtUtilityInitializeWhitelist = {
            "QTUtility.cs",
            "QTTabBarClass.cs",
            "OptionsDialog/OptionsDialog.xaml.cs",
        };

        private static readonly string[] RootRegistryOffendersMustUseRegistryAccess = {
            "QTButtonBar.cs",
            "FileHashComputerForm.cs",
            "IDLWrapper.cs",
        };

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        [Test]
        public void Only_Whitelisted_Files_Call_QTUtility_Initialize() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            var offenders = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories)
                .Where(path => !path.Contains("\\obj\\") && !path.Contains("\\bin\\"))
                .Select(path => new {
                    Relative = path.Substring(root.Length + 1).Replace('\\', '/'),
                    Content = File.ReadAllText(path),
                })
                .Where(file => Regex.IsMatch(file.Content, @"QTUtility\.Initialize\s*\(\s*\)"))
                .Where(file => !QtUtilityInitializeWhitelist.Contains(file.Relative, StringComparer.OrdinalIgnoreCase))
                .Select(file => file.Relative)
                .ToArray();
            Assert.IsEmpty(offenders,
                "QTUtility.Initialize should only appear in the whitelisted entry files");
        }

        [Test]
        public void Groups_And_Apps_Use_Separate_Ipc_Not_ConfigManager() {
            string dispatcher = ReadQtTabBarFile("IpcCommandDispatcher.cs");
            Assert.IsTrue(dispatcher.Contains("IpcCommand.ReloadGroups"),
                "ReloadGroups IPC command should be handled");
            Assert.IsTrue(dispatcher.Contains("IpcCommand.ReloadApps"),
                "ReloadApps IPC command should be handled");
            string groups = ReadQtTabBarFile("GroupsManager.cs");
            string apps = ReadQtTabBarFile("AppsManager.cs");
            Assert.IsFalse(groups.Contains("ConfigManager.PersistConfig"),
                "GroupsManager should not persist through ConfigManager.PersistConfig");
            Assert.IsFalse(apps.Contains("ConfigManager.PersistConfig"),
                "AppsManager should not persist through ConfigManager.PersistConfig");
        }

        [Test]
        public void Root_Registry_Writes_Go_Through_RegistryAccess() {
            foreach(string relative in RootRegistryOffendersMustUseRegistryAccess) {
                string content = ReadQtTabBarFile(relative);
                Assert.IsFalse(
                    content.Contains("Registry.CurrentUser.CreateSubKey(RegConst.Root)"),
                    relative + " should not write the root key directly");
                Assert.IsTrue(
                    content.Contains("RegistryAccess.OpenRootCreate")
                        || content.Contains("RegistryAccess.OpenRoot(true)"),
                    relative + " should route root registry writes through RegistryAccess");
            }
        }
    }
}
