using System;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class WhitelistMonotonicityTests {
        private const int ContextMenuedTabWhitelistLineBudget = 3;
        private const int CurrentTabAssignmentWhitelistFileBudget = 3;

        private const int TabCreationNewQTabItemWhitelistBudget = 5;
        private const int TabCreationInsertWhitelistBudget = 2;

        [Test]
        public void ContextMenuedTab_Whitelist_Does_Not_Grow() {
            int count = CountHashSetEntries("ContextMenuedTab_WritePathTests.cs", "AllowedDirectWriteFiles");
            Assert.LessOrEqual(count, ContextMenuedTabWhitelistLineBudget,
                "ContextMenuedTab whitelist must not grow during root cure");
        }

        [Test]
        public void TabCreation_Whitelist_File_Count_Does_Not_Grow() {
            int newTabCount = CountHashSetEntries("TabCreationWhitelistTests.cs", "AllowedFiles");
            int insertCount = CountHashSetEntries("TabCreationWhitelistTests.cs", "AllowedInsertFiles");
            Assert.LessOrEqual(newTabCount, TabCreationNewQTabItemWhitelistBudget,
                "Tab creation new QTabItem whitelist must not grow during root cure");
            Assert.LessOrEqual(insertCount, TabCreationInsertWhitelistBudget,
                "Tab creation TabPages.Insert whitelist must not grow during root cure");
        }

        [Test]
        public void CurrentTab_Assignment_Whitelist_Does_Not_Grow() {
            int count = CountHashSetEntries("CurrentTab_SingleWriterTests.cs", "AllowedCurrentTabAssignmentFiles");
            Assert.LessOrEqual(count, CurrentTabAssignmentWhitelistFileBudget,
                "CurrentTab assignment whitelist must not grow during root cure");
        }

        private static int CountHashSetEntries(string testFileName, string hashSetFieldName) {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "Tests", "QTTtabBarTests", testFileName));
            int start = source.IndexOf(hashSetFieldName, StringComparison.Ordinal);
            Assert.GreaterOrEqual(start, 0, "Missing whitelist field " + hashSetFieldName + " in " + testFileName);
            int openBrace = source.IndexOf('{', start);
            int closeBrace = source.IndexOf("};", openBrace, StringComparison.Ordinal);
            string block = source.Substring(openBrace, closeBrace - openBrace);
            return Regex.Matches(block, @"""[^""]+""").Count;
        }

        private static string RepoRoot() {
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
