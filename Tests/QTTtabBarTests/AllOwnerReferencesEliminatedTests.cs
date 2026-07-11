using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class AllOwnerReferencesEliminatedTests {
        /// <summary>
        /// Scans every QTTabBarClass*.cs file in the QTTabBar directory
        /// and asserts that the token "_owner." appears zero times.
        /// This is the final gate for Batch 3 — all back-references
        /// must be eliminated, not just in MenuController files.
        /// </summary>
        [Test]
        public void All_QTTabBarClass_Files_ShouldHaveZeroOwnerReferences() {
            string root = FindRepoRoot();
            string dir = Path.Combine(root, "QTTabBar");
            string[] files = Directory.GetFiles(dir, "QTTabBarClass*.cs", SearchOption.TopDirectoryOnly);

            int total = 0;
            foreach(string file in files) {
                string text = File.ReadAllText(file);
                int idx = 0;
                int count = 0;
                while((idx = text.IndexOf("_owner.", idx, StringComparison.Ordinal)) >= 0) {
                    count++;
                    idx += "_owner.".Length;
                }
                if(count > 0) {
                    Console.WriteLine($"  {Path.GetFileName(file)}: {count} _owner. references");
                }
                total += count;
            }

            Assert.AreEqual(0, total,
                $"All QTTabBarClass*.cs files must have zero _owner. references, found {total}");
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
