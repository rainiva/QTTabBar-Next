using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class PluginsEntryWhitelistTests {
        private static readonly HashSet<string> AllowedPluginOptionsFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "QTQuick.cs",
        };

        [Test]
        public void Plugins_Options_Entry_Only_In_Approved_Files() {
            string pluginsRoot = Path.Combine(RepoRoot(), "Plugins");
            if(!Directory.Exists(pluginsRoot)) {
                Assert.Ignore("Plugins directory not present");
            }
            foreach(string file in Directory.GetFiles(pluginsRoot, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) {
                    continue;
                }
                string source = File.ReadAllText(file);
                bool usesOptionsDialogOpen = source.Contains("OptionsDialog.Open");
                bool usesIpcOpenOptions = source.Contains("ExecuteOnServerProcessOpenOptions");
                if(!usesOptionsDialogOpen && !usesIpcOpenOptions) {
                    continue;
                }
                string fileName = Path.GetFileName(file);
                Assert.IsTrue(AllowedPluginOptionsFiles.Contains(fileName),
                    "Unauthorized Options entry in plugin file " + file);
            }
        }

        [Test]
        public void Plugins_OptionsDialog_Open_Only_In_Approved_Files() {
            Plugins_Options_Entry_Only_In_Approved_Files();
        }

        [Test]
        public void Plugins_OpenOptionDialog_Does_Not_Exist() {
            string pluginsRoot = Path.Combine(RepoRoot(), "Plugins");
            if(!Directory.Exists(pluginsRoot)) {
                Assert.Ignore("Plugins directory not present");
            }
            foreach(string file in Directory.GetFiles(pluginsRoot, "*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\")) {
                    continue;
                }
                string source = File.ReadAllText(file);
                StringAssert.DoesNotContain("OpenOptionDialog", source,
                    "Plugins must not call OpenOptionDialog in " + file);
            }
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
