using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class SessionPersistenceBoundaryTests {
        [Test]
        public void Structural_Governance_Documents_Session_Store_Boundaries() {
            string doc = GovernanceDoc();
            StringAssert.Contains("ConfigManager.LoadedConfig", doc);
            StringAssert.Contains("WindowSessionPersistence", doc);
            StringAssert.Contains("LockedTabsService", doc);
            StringAssert.Contains("StaticReg", doc);
            StringAssert.Contains("TabsOnLastClosedWindow", doc);
            StringAssert.Contains("MutateWindowAndCommit", doc);
        }

        [Test]
        public void Structural_Governance_Documents_Tab_Creation_Bootstrap_Exceptions() {
            string doc = GovernanceDoc();
            StringAssert.Contains("TryCreateTabCore", doc);
            StringAssert.Contains("bootstrap", doc.ToLowerInvariant());
            StringAssert.Contains("QTTabBarClass.ComponentBuildController.cs", doc);
            StringAssert.Contains("QTSecondViewBar.ComponentBuild.cs", doc);
            StringAssert.Contains("TabBarBase.TabOperations.cs", doc);
        }

        [Test]
        public void Structural_Governance_Documents_Measured_Hotspot_Baselines() {
            string doc = GovernanceDoc();
            StringAssert.Contains("5986", doc);
            StringAssert.Contains("6800", doc);
            StringAssert.Contains("Hosts", doc);
            StringAssert.Contains("2090", doc);
            StringAssert.Contains("FamilyLinesRecursive", doc);
            StringAssert.Contains("≤1058", doc);
            StringAssert.Contains("≤1293", doc);
            StringAssert.Contains("≤900", doc);
            StringAssert.Contains("≤40", doc);
            Assert.IsFalse(doc.Contains("nested controller = 0"),
                "Governance doc must not claim nested controller count is already zero");
            Assert.IsFalse(doc.Contains("InstanceManager ≤813") || doc.Contains("InstanceManager <=813"),
                "Governance doc must not keep the pre-split InstanceManager ≤813 budget");
        }

        [Test]
        public void Progress_Log_Records_Wave6_To_8_Explorer_Acceptance_Matrix() {
            string progress = File.ReadAllText(Path.Combine(RepoRoot(), "progress.md"));
            StringAssert.Contains("Wave 6–8 Explorer", progress);
            StringAssert.Contains("会话恢复含无效路径", progress);
            StringAssert.Contains("启动组", progress);
            StringAssert.Contains("NeverOpenSame", progress);
            StringAssert.Contains("跨进程 IPC", progress);
            StringAssert.Contains("SecondView", progress);
        }

        private static string GovernanceDoc() {
            return File.ReadAllText(Path.Combine(RepoRoot(), "docs", "architecture", "structural-governance.md"));
        }

        [Test]
        public void Structural_Governance_Documents_Session_Read_Write_Matrix() {
            string doc = GovernanceDoc();
            StringAssert.Contains("Read API", doc);
            StringAssert.Contains("Write API", doc);
            StringAssert.Contains("LoadTabsOnLastClosedWindow", doc);
            StringAssert.Contains("WindowCaptureSession", doc);
        }

        [Test]
        public void Composition_Context_Implementations_Avoid_Ex_Facades() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "Composition", "ExplorerContext.cs"));
            StringAssert.DoesNotContain(".ExExplorerHandle", source);
            StringAssert.DoesNotContain(".ExShellBrowser", source);
            StringAssert.DoesNotContain(".ExCurrentTab", source);
        }

        [Test]
        public void IMenuContext_Exposes_ContextMenuedTab() {
            Type menuContext = typeof(QTTabBarLib.QTTabBarClass).Assembly.GetType("QTTabBarLib.IMenuContext", true);
            Assert.IsNotNull(menuContext.GetProperty("ContextMenuedTab"));
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
