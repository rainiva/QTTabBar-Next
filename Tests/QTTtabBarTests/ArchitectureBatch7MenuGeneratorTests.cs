using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    // Batch7 GC7a: extract the shell context-menu building logic
    // (CreateMenu / CreateMenuFromIDL / CreateParentMenu / CreateDirectoryItem
    // and their QueryVirtualMenu wiring) out of the SubDirTipForm god class
    // into a nested ShellMenuGenerator controller that reaches back via _owner.
    [TestFixture]
    public class ArchitectureBatch7MenuGeneratorTests {

        private static Type GetFormType() {
            var t = typeof(QTUtility).Assembly.GetType("QTTabBarLib.SubDirTipForm");
            Assert.IsNotNull(t, "SubDirTipForm type must exist");
            return t;
        }

        private static Type GetGeneratorType() {
            return GetFormType().GetNestedType("ShellMenuGenerator",
                BindingFlags.NonPublic | BindingFlags.Public);
        }

        [Test]
        public void ShellMenuGenerator_NestedType_Is_Extracted() {
            Assert.IsNotNull(GetGeneratorType(),
                "ShellMenuGenerator nested controller must be extracted from SubDirTipForm");
        }

        [Test]
        public void ShellMenuGenerator_Holds_Owner_BackReference() {
            var ctrl = GetGeneratorType();
            Assert.IsNotNull(ctrl, "ShellMenuGenerator must exist");
            var ownerField = ctrl.GetField("_owner", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(ownerField, "ShellMenuGenerator must hold a private readonly _owner back-reference");
            Assert.AreEqual(GetFormType(), ownerField.FieldType,
                "_owner must be typed as SubDirTipForm");
        }

        [Test]
        public void Menu_Building_Methods_Moved_Into_Controller() {
            var ctrl = GetGeneratorType();
            Assert.IsNotNull(ctrl, "ShellMenuGenerator must exist");
            const BindingFlags any = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            foreach(string name in new[] { "CreateMenu", "CreateMenuFromIDL", "CreateParentMenu", "CreateDirectoryItem" }) {
                Assert.IsNotNull(ctrl.GetMethod(name, any),
                    name + " should be moved into ShellMenuGenerator");
            }
        }

        [Test]
        public void SubDirTipForm_Delegates_Menu_Building_To_Controller() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "SubDirTipForm.cs"));
            Assert.IsTrue(content.Contains("ShellMenuGenerator"),
                "SubDirTipForm should declare and use the extracted ShellMenuGenerator");
            Assert.IsFalse(content.Contains("private List<QMenuItem> CreateMenu("),
                "the CreateMenu body should no longer live directly in SubDirTipForm.cs");
            Assert.IsFalse(content.Contains("private List<QMenuItem> CreateMenuFromIDL("),
                "the CreateMenuFromIDL body should no longer live directly in SubDirTipForm.cs");
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
