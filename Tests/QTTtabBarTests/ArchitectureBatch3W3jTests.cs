using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3jTests {
        [Test]
        public void SecondViewBar_Has_No_Dead_Hook_Procs() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            string[] removed = {
                "CallbackKeyboardProc",
                "CallbackMouseProc",
                "CallbackGetMsgProc",
                "OpenDefaultLocation",
                "hHook_Key",
                "hHook_Mouse",
                "hHook_Msg",
            };
            foreach(string symbol in removed) {
                Assert.IsFalse(content.Contains(symbol),
                    "QTSecondViewBar should remove dead hook symbol " + symbol + " after W3j");
            }
        }

        [Test]
        public void SecondViewBar_Still_Uses_WindowSubclass_Hooks() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs"));
            Assert.IsTrue(content.Contains("baseBarSubclassProc"),
                "QTSecondViewBar should keep active WindowSubclass hooks after W3j");
            Assert.IsTrue(content.Contains("InstallHooks"),
                "QTSecondViewBar should keep InstallHooks for WindowSubclass after W3j");
        }

        [Test]
        public void SecondViewBar_Line_Count_Reduced_After_Dead_Hook_Removal() {
            int lines = File.ReadAllLines(Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs")).Length;
            Assert.LessOrEqual(lines, 1350,
                "QTSecondViewBar should shrink materially after removing dead hook procs (W3j)");
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
