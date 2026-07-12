using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class SessionStoreBypassGuardTests {
        private static readonly HashSet<string> AllowedTabsOnLastClosedWindowFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "WindowSessionPersistence.cs",
        };

        private static readonly HashSet<string> AllowedRecentlyClosedRegistryFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "WindowSessionPersistence.cs",
        };

        private static readonly HashSet<string> AllowedLockedTabsPersistFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "LockedTabsService.cs",
        };

        [Test]
        public void Production_Code_Does_Not_Read_TabsOnLastClosedWindow_Outside_WindowSessionPersistence() {
            AssertScanOnlyAllowed(
                "GetValue(\"TabsOnLastClosedWindow\"",
                AllowedTabsOnLastClosedWindowFiles,
                "TabsOnLastClosedWindow registry read");
        }

        [Test]
        public void Production_Code_Does_Not_Write_TabsOnLastClosedWindow_Outside_WindowSessionPersistence() {
            AssertScanOnlyAllowed(
                "SetValue(\"TabsOnLastClosedWindow\"",
                AllowedTabsOnLastClosedWindowFiles,
                "TabsOnLastClosedWindow registry write");
        }

        [Test]
        public void Production_Code_Does_Not_Inline_RecentlyClosed_Registry_Reads() {
            AssertScanOnlyAllowed(
                "CreateSubKey(\"RecentlyClosed\")",
                AllowedRecentlyClosedRegistryFiles,
                "RecentlyClosed registry read/write");
        }

        [Test]
        public void Production_Code_Does_Not_Inline_RecentFiles_Registry_Reads() {
            AssertScanOnlyAllowed(
                "CreateSubKey(\"RecentFiles\")",
                AllowedRecentlyClosedRegistryFiles,
                "RecentFiles registry read/write");
        }

        [Test]
        public void Production_Code_Does_Not_Persist_LockedTabs_Outside_LockedTabsService() {
            AssertScanOnlyAllowed(
                "WriteRegBinary(",
                AllowedLockedTabsPersistFiles,
                "locked tabs registry write",
                source => source.Contains("WriteRegBinary(") && source.Contains("\"TabsLocked\""));
        }

        [Test]
        public void RestoreTabsOnInitialize_Uses_WindowSessionPersistence_Read_Api() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "TabBarBase.TabRestoration.cs"));
            StringAssert.Contains("WindowSessionPersistence.LoadTabsOnLastClosedWindow", source);
            StringAssert.DoesNotContain("GetValue(\"TabsOnLastClosedWindow\"", source);
        }

        private static void AssertScanOnlyAllowed(
            string needle,
            HashSet<string> allowedFileNames,
            string violationKind,
            Func<string, bool> predicate = null) {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            var violations = new List<string>();
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(predicate != null ? !predicate(source) : !source.Contains(needle)) {
                    continue;
                }
                string fileName = Path.GetFileName(relativePath);
                if(!allowedFileNames.Contains(fileName)) {
                    violations.Add(relativePath);
                }
            }
            CollectionAssert.IsEmpty(violations,
                "Unauthorized " + violationKind + " outside canonical session store API: "
                + string.Join(", ", violations));
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
