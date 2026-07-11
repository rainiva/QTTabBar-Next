using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch4PluginServerTopLevelTests {
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

        /// <summary>
        /// PluginServer must be a top-level type in QTTabBarLib namespace,
        /// not nested inside QTTabBarClass.
        /// </summary>
        [Test]
        public void PluginServer_ShouldBe_TopLevel_Type() {
            Assembly asm = typeof(QTTabBarLib.QTTabBarClass).Assembly;
            Type t = asm.GetType("QTTabBarLib.PluginServer");
            Assert.IsNotNull(t,
                "PluginServer must be a top-level type (QTTabBarLib.PluginServer). " +
                "Currently it is nested as QTTabBarClass.PluginServer.");
            Assert.IsNull(t.DeclaringType,
                "PluginServer must not be nested inside any type. " +
                $"Currently nested in: {t.DeclaringType?.FullName}");
        }

        /// <summary>
        /// PluginServer must be declared as 'sealed' (not just 'partial')
        /// at namespace level, since it has no derivatives.
        /// </summary>
        [Test]
        public void PluginServer_ShouldBe_Sealed() {
            Assembly asm = typeof(QTTabBarLib.QTTabBarClass).Assembly;
            Type t = asm.GetType("QTTabBarLib.PluginServer");
            Assert.IsNotNull(t, "PluginServer must exist as top-level type first");
            Assert.IsTrue(t.IsSealed,
                "PluginServer should be sealed. Top-level controllers must be sealed.");
        }

        /// <summary>
        /// No file in the QTTabBar directory should contain
        /// 'QTTabBarClass.PluginServer' as a type reference.
        /// After extraction, all references should use 'PluginServer' directly.
        /// </summary>
        [Test]
        public void No_QTTabBarClass_PluginServer_TypeReference_Remains() {
            string root = FindRepoRoot();
            string dir = Path.Combine(root, "QTTabBar");
            string[] files = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
            int violations = 0;
            foreach(string file in files) {
                string content = File.ReadAllText(file);
                // Allow comments containing the old reference, but not code references
                int idx = 0;
                while((idx = content.IndexOf("QTTabBarClass.PluginServer", idx, StringComparison.Ordinal)) >= 0) {
                    // Check if it's in a comment line
                    int lineStart = content.LastIndexOf('\n', idx) + 1;
                    string linePrefix = content.Substring(lineStart, idx - lineStart).Trim();
                    if(!linePrefix.StartsWith("//") && !linePrefix.StartsWith("*")) {
                        violations++;
                    }
                    idx += "QTTabBarClass.PluginServer".Length;
                }
            }
            Assert.AreEqual(0, violations,
                $"Found {violations} code references to 'QTTabBarClass.PluginServer'. " +
                "All should be changed to 'PluginServer' after top-level extraction.");
        }
    }
}
