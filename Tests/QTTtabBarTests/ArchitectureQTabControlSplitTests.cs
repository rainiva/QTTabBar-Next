using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureQTabControlSplitTests {
        [Test]
        public void QTabControl_Is_Partial_And_Main_File_Under_800_Lines() {
            string main = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTabControl.cs"));
            Assert.IsTrue(main.Contains("partial class QTabControl"),
                "QTabControl should be declared partial");
            int lineCount = File.ReadAllLines(Path.Combine(FindRepoRoot(), "QTTabBar", "QTabControl.cs")).Length;
            Assert.LessOrEqual(lineCount, 800, "QTabControl main partial should be <= 800 lines");
        }

        [Test]
        public void QTabControl_Partial_Files_Exist() {
            string dir = Path.Combine(FindRepoRoot(), "QTTabBar");
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTabControl.LayoutPainting.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTabControl.MouseInput.cs")));
            Assert.IsTrue(File.Exists(Path.Combine(dir, "QTabControl.SelectionScroll.cs")));
        }

        [Test]
        public void QTabControl_Relocate_Uses_SelectTab_In_Combined_Source() {
            string content = QTabControlSourceTestHelper.ReadCombined(FindRepoRoot());
            int relocateStart = content.IndexOf("void Relocate(", StringComparison.Ordinal);
            Assert.GreaterOrEqual(relocateStart, 0);
            int relocateEnd = content.IndexOf("Owner.Refresh();", relocateStart, StringComparison.Ordinal);
            string relocateBody = content.Substring(relocateStart, relocateEnd - relocateStart);
            Assert.IsTrue(relocateBody.Contains("SelectTab("));
            Assert.IsFalse(relocateBody.Contains("Owner.SelectedIndex ="));
        }

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
    }
}
