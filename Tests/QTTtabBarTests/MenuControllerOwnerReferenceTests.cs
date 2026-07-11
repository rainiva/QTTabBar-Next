using System.IO;
using System.Linq;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class MenuControllerOwnerReferenceTests {
        [Test]
        public void MenuController_Files_ShouldHaveZeroOwnerReferences() {
            string root = FindRepoRoot();
            string dir = Path.Combine(root, "QTTabBar");
            string text = File.ReadAllText(Path.Combine(dir, "QTTabBarClass.ShellHosts.cs"));
            int idx = 0;
            int total = 0;
            while((idx = text.IndexOf("_owner.", idx, System.StringComparison.Ordinal)) >= 0) {
                total++;
                idx += "_owner.".Length;
            }
            Assert.AreEqual(0, total, "ShellHosts merged file must have zero _owner. references");
        }

        private static string FindRepoRoot() {
            var dir = new System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(System.IO.File.Exists(System.IO.Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
