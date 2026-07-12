using System.Linq;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class StructuralGovernanceScannerTests {
        [Test]
        public void Partial_Count_Includes_Declarations_Outside_TypeNamed_Files() {
            Assert.AreEqual(6,
                SourceMetrics.PartialDeclarationCount("QTTabBarLib.QTTabBarClass"));
        }

        [Test]
        public void QtTabBarClass_Partials_Reside_Only_In_Approved_Files() {
            CollectionAssert.AreEquivalent(new[] {
                "QTTabBarClass.cs",
                "QTTabBarClass.ComponentBuildController.cs",
                "QTTabBarClass.ExplorerHosts.cs",
                "QTTabBarClass.ExplorerIntegration.cs",
                "QTTabBarClass.MenuOperationsHost.cs",
                "QTTabBarClass.ShellHosts.cs"
            }, SourceMetrics.FilesDeclaringPartial("QTTabBarLib.QTTabBarClass"));
        }

        [Test]
        public void PluginServer_Does_Not_Declare_QTTabBarClass_Partial() {
            string source = SourceMetrics.ReadRepoFile("QTTabBar/PluginServer.cs");
            StringAssert.DoesNotContain("partial class QTTabBarClass", source);
        }

        [Test]
        public void SourceFiles_Includes_PluginServer_TabAccess() {
            CollectionAssert.Contains(SourceMetrics.SourceFiles().ToList(),
                "PluginServer.TabAccess.cs");
        }
    }
}
