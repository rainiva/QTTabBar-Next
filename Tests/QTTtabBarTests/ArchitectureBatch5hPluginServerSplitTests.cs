using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5hPluginServerSplitTests {
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

        private static string ReadPluginFile(string name) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", name));
        }

        private static string ReadCombined() {
            return ReadPluginFile("PluginServer.cs") +
                   ReadPluginFile("PluginServer.Commands.cs") +
                   ReadPluginFile("PluginServer.TabAccess.cs") +
                   ReadPluginFile("PluginServer.Lifetime.cs");
        }

        private static int CountLines(string name) {
            return ReadPluginFile(name).Split('\n').Length;
        }

        [Test]
        public void PluginServer_Is_Partial_With_Command_TabAccess_And_Lifetime_Files() {
            Assert.IsTrue(ReadPluginFile("PluginServer.cs").Contains("partial class PluginServer"));
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "PluginServer.Commands.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "PluginServer.TabAccess.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(FindRepoRoot(), "QTTabBar", "PluginServer.Lifetime.cs")));
        }

        [Test]
        public void PluginServer_Main_File_Under_300_Lines() {
            Assert.LessOrEqual(CountLines("PluginServer.cs"), 300);
        }

        [Test]
        public void PluginServer_Commands_Holds_ExecuteCommand_Switch() {
            string commands = ReadPluginFile("PluginServer.Commands.cs");
            Assert.IsTrue(commands.Contains("ExecuteCommand"));
            Assert.IsTrue(commands.Contains("switch(command)"));
        }

        [Test]
        public void PluginServer_TabAccess_Holds_TabWrapper_And_Tab_Operations() {
            string tabAccess = ReadPluginFile("PluginServer.TabAccess.cs");
            Assert.IsTrue(tabAccess.Contains("class TabWrapper"));
            Assert.IsTrue(tabAccess.Contains("CreateTab("));
            Assert.IsTrue(tabAccess.Contains("GetTabs("));
        }

        [Test]
        public void PluginServer_Lifetime_Holds_Load_And_Dispose() {
            string lifetime = ReadPluginFile("PluginServer.Lifetime.cs");
            Assert.IsTrue(lifetime.Contains("Plugin Load("));
            Assert.IsTrue(lifetime.Contains("void Dispose()"));
            Assert.IsTrue(lifetime.Contains("LoadStartupPlugins"));
        }

        [Test]
        public void Combined_PluginServer_Preserves_Public_Api() {
            string combined = ReadCombined();
            Assert.IsTrue(combined.Contains("ExecuteCommand"));
            Assert.IsTrue(combined.Contains("class TabWrapper"));
            Assert.IsTrue(combined.Contains("RefreshPlugins"));
        }
    }
}
