using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch3W3jTests {
        [Test]
        public void SecondViewBar_Has_No_Dead_Hook_Procs() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
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
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("BaseBarSubclassProc"),
                "SecondView should keep active WindowSubclass hooks after controller extraction");
            Assert.IsTrue(content.Contains("InstallHooks"),
                "SecondView should keep InstallHooks for WindowSubclass after controller extraction");
        }

        [Test]
        public void SecondViewBar_SubclassProc_Handles_SysColorChange() {
            string content = SecondViewBarSourceTestHelper.ReadCombined(FindRepoRoot());
            Assert.IsTrue(content.Contains("case WM.SYSCOLORCHANGE:"),
                "SecondView BaseBarSubclassProc should handle SYSCOLORCHANGE after review fix");
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
