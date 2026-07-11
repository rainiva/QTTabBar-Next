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
            string[] files = {
                "QTTabBarClass.MenuController.cs",
                "QTTabBarClass.MenuController.TabMenu.cs",
                "QTTabBarClass.MenuController.SysMenu.cs",
                "QTTabBarClass.MenuController.DropDownHandlers.cs"
            };

            int total = 0;
            foreach(string file in files) {
                string path = Path.Combine(dir, file);
                if(File.Exists(path)) {
                    string text = File.ReadAllText(path);
                    int idx = 0;
                    int count = 0;
                    while((idx = text.IndexOf("_owner.", idx, System.StringComparison.Ordinal)) >= 0) {
                        count++;
                        idx += "_owner.".Length;
                    }
                    if(count > 0) {
                        Assert.Fail($"{file} still has {count} _owner. references");
                    }
                    total += count;
                }
            }
            Assert.AreEqual(0, total, "MenuController files must have zero _owner. references");
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
