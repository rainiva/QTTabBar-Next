using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5jConfigModelsTests {
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

        [Test]
        public void ConfigModels_File_Exists_With_Types_In_QTTabBarLib() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "ConfigModels.cs");
            Assert.IsTrue(File.Exists(path));
            string content = File.ReadAllText(path);
            Assert.IsTrue(content.Contains("class XmlSerializableFont"));
            Assert.IsTrue(content.Contains("enum TabPos"));
            Assert.IsTrue(content.Contains("enum BindAction"));
            Assert.IsTrue(content.Contains("class _Skin"));
            Assert.AreEqual(typeof(Config).Namespace, typeof(TabPos).Namespace);
        }

        [Test]
        public void Config_Main_File_At_Most_800_Lines() {
            string path = Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs");
            int lineCount = File.ReadAllText(path).Split('\n').Length;
            Assert.LessOrEqual(lineCount, 800);
        }

        [Test]
        public void Config_Nested_Models_Moved_Out_Of_Main_File() {
            string config = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Config.cs"));
            Assert.IsFalse(config.Contains("class _Window"));
            Assert.IsFalse(config.Contains("enum TabPos"));
            Assert.IsTrue(config.Contains("partial class Config"));
        }

        [Test]
        public void Config_Public_Static_Accessors_Still_Resolve() {
            Assert.IsNotNull(typeof(Config).GetProperty("Window", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(typeof(Config).GetProperty("Skin", BindingFlags.Public | BindingFlags.Static));
            Assert.IsNotNull(typeof(Config).GetNestedType("_Skin", BindingFlags.Public));
        }
    }
}
