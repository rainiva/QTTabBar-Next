using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class SecondViewBoundaryTests {
        [Test]
        public void QTSecondViewBar_Does_Not_Own_Subclass_Hook_Implementation() {
            string main = ReadQtTabBarFile("QTSecondViewBar.cs");
            string hooks = ReadQtTabBarFile("QTSecondViewBar.SubclassHooks.cs");
            StringAssert.DoesNotContain("void InstallHooks", main);
            StringAssert.DoesNotContain("void UninstallHooks", main);
            StringAssert.DoesNotContain("baseBarSubclassProc", main);
            StringAssert.DoesNotContain("rebarSubclassProc", hooks);
        }

        [Test]
        public void QTSecondViewBar_Does_Not_Own_Explorer_Event_Handlers() {
            string main = ReadQtTabBarFile("QTSecondViewBar.cs");
            StringAssert.DoesNotContain("Explorer_BeforeNavigate2", main);
            StringAssert.DoesNotContain("Explorer_NavigateComplete2", main);
        }

        [Test]
        public void SecondView_Controllers_Exist() {
            Assert.IsNotNull(Type.GetType("QTTabBarLib.SecondView.SecondViewLifecycleController, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.SecondView.SecondViewExplorerController, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.SecondView.SecondViewWindowSubclassController, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.SecondView.ISecondViewHost, QTTabBar"));
        }

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", relativePath));
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
