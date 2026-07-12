using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class WindowCaptureSessionTests {
        private static readonly HashSet<string> AllowedCreateWindowGroupWriteFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "Session\\WindowCaptureSession.cs",
            "Session/WindowCaptureSession.cs",
        };

        [Test]
        public void Production_Code_Does_Not_Write_StaticReg_CreateWindowGroup_Directly() {
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fullPath = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
                string source = File.ReadAllText(fullPath);
                if(!source.Contains("StaticReg.CreateWindowGroup =")) {
                    continue;
                }
                Assert.IsTrue(AllowedCreateWindowGroupWriteFiles.Contains(relativePath),
                    "Unauthorized StaticReg.CreateWindowGroup write in " + relativePath);
            }
        }

        [Test]
        public void WindowCaptureSession_Exposes_Group_Queue_Api() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "Session", "WindowCaptureSession.cs"));
            StringAssert.Contains("EnqueueGroup", source);
            StringAssert.Contains("TryDequeueGroup", source);
            StringAssert.Contains("ClearGroup", source);
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
