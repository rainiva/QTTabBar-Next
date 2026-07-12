using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class OptionsEntryWhitelistTests {
        private static readonly HashSet<string> AllowedOptionsOpenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "OptionsDialog.xaml.cs",
            "OptionsDialogEntry.cs",
            "MenuOperationsController.cs",
            "BindAction/BindActionController.cs",
            "PluginServer.Commands.cs",
            "QTButtonBar.cs",
            "ButtonBarClickController.cs",
            "IpcCommandGateway.cs",
            "IpcCommandDispatcher.cs",
        };

        [Test]
        public void OptionsDialog_Open_Only_In_Approved_QTTabBar_Files() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!source.Contains("OptionsDialog.Open")) {
                    continue;
                }
                string fileName = Path.GetFileName(relativePath);
                Assert.IsTrue(IsAllowedOptionsOpenPath(relativePath),
                    "Unauthorized OptionsDialog.Open in " + relativePath);
            }
        }

        private static bool IsAllowedOptionsOpenPath(string relativePath) {
            string normalized = relativePath.Replace('\\', '/');
            if(AllowedOptionsOpenFiles.Contains(normalized)) {
                return true;
            }
            return AllowedOptionsOpenFiles.Contains(Path.GetFileName(normalized));
        }

        [Test]
        public void OpenOptionDialog_Does_Not_Exist_In_Production_Code() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                StringAssert.DoesNotContain("OpenOptionDialog", source,
                    "OpenOptionDialog must be deleted from " + relativePath);
            }
        }

        [Test]
        public void New_OptionsDialog_Only_In_OptionsDialog_xaml_cs() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!source.Contains("new OptionsDialog")) {
                    continue;
                }
                Assert.AreEqual("OptionsDialog/OptionsDialog.xaml.cs", relativePath.Replace('\\', '/'),
                    "Unauthorized new OptionsDialog in " + relativePath);
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
