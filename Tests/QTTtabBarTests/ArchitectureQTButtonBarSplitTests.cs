using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureQTButtonBarSplitTests {
        [Test]
        public void QTButtonBar_Is_Partial_And_Main_File_Under_500_Lines() {
            string repo = FindRepoRoot();
            string main = File.ReadAllText(Path.Combine(repo, "QTTabBar", "QTButtonBar.cs"));
            Assert.IsTrue(main.Contains("partial class QTButtonBar"));
            int lineCount = File.ReadAllLines(Path.Combine(repo, "QTTabBar", "QTButtonBar.cs")).Length;
            Assert.LessOrEqual(lineCount, 500);
        }

        [Test]
        public void QTButtonBar_Partial_Files_Exist() {
            string dir = Path.Combine(FindRepoRoot(), "QTTabBar");
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTButtonBar.CreateItems.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTButtonBar.BandLifecycle.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTButtonBar.ItemClick.cs")));
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new System.InvalidOperationException("Repository root not found.");
        }
    }
}
