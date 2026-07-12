using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class TabCreationBypassGuardTests {
        [Test]
        public void RestoreTabsOnInitialize_Does_Not_Use_Unvalidated_CreateNewTabAt() {
            string source = ReadQtTabBarFile("TabBarBase.TabRestoration.cs");
            StringAssert.Contains("TryCreateTabCore", source);
            StringAssert.DoesNotContain("CreateNewTabAt(wrapper2", source);
        }

        [Test]
        public void AddStartUpTabs_Does_Not_Directly_New_QTabItem() {
            string source = ReadQtTabBarFile("TabOperations/TabOperationsController.cs");
            string body = ExtractMethodBody(source, "public void AddStartUpTabs");
            StringAssert.DoesNotContain("new QTabItem", body);
            StringAssert.Contains("TryCreateTabAtPosition", body);
        }

        [Test]
        public void CreateNewTabAt_Delegates_To_TryCreateTabCore() {
            string source = ReadQtTabBarFile("TabBarBase.TabOperations.cs");
            string body = ExtractMethodBody(source, "internal QTabItem CreateNewTabAt");
            StringAssert.Contains("TryCreateTabCore", body);
            StringAssert.DoesNotContain("new QTabItem", body);
        }

        [Test]
        public void OpenGroup_Does_Not_Use_CreateNewTab() {
            string source = ReadQtTabBarFile("TabOperations/TabOperationsController.cs");
            string body = ExtractMethodBody(source, "public void OpenGroup");
            StringAssert.DoesNotContain("CreateNewTab(", body);
            StringAssert.Contains("TryCreateTabAtPosition", body);
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

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", relativePath));
        }

        private static string ExtractMethodBody(string content, string methodSignature) {
            int methodIndex = content.IndexOf(methodSignature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodSignature);
            return ExtractBlockFromBrace(content, content.IndexOf('{', methodIndex));
        }

        private static string ExtractNestedMethodBody(string content, string nestedClassName, string methodName) {
            int classIndex = content.IndexOf("class " + nestedClassName, StringComparison.Ordinal);
            Assert.GreaterOrEqual(classIndex, 0, "Missing nested class " + nestedClassName);
            int methodIndex = content.IndexOf("void " + methodName + "(", classIndex, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + methodName);
            return ExtractBlockFromBrace(content, content.IndexOf('{', methodIndex));
        }

        private static string ExtractBlockFromBrace(string content, int brace) {
            int depth = 0;
            for(int i = brace; i < content.Length; i++) {
                if(content[i] == '{') {
                    depth++;
                }
                else if(content[i] == '}') {
                    depth--;
                    if(depth == 0) {
                        return content.Substring(brace, i - brace + 1);
                    }
                }
            }
            throw new InvalidOperationException("Unbalanced braces.");
        }
    }
}
