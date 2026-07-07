using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using QTPluginLib;

namespace QTTtabBarTests {
    /// <summary>
    /// TDD: safe process launch helpers used by core and plugins.
    /// </summary>
    [TestFixture]
    public class SafeLaunchTests {

        [Test]
        public void IsAllowedLaunchTarget_Rejects_NullOrEmpty() {
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget(null));
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget(""));
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget("   "));
        }

        [Test]
        public void IsAllowedLaunchTarget_Rejects_RelativeExecutable() {
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget("notepad.exe"));
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget(@"folder\app.exe"));
        }

        [Test]
        public void IsAllowedLaunchTarget_Rejects_ShellMetacharacters() {
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget(@"C:\test & calc.exe"));
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget(@"C:\test|calc.exe"));
            Assert.IsFalse(SafeLaunch.IsAllowedLaunchTarget("C:\test^calc.exe"));
        }

        [Test]
        public void IsAllowedLaunchTarget_Accepts_HttpAndHttpsUrls() {
            Assert.IsTrue(SafeLaunch.IsAllowedLaunchTarget("https://github.com/indiff/qttabbar/releases"));
            Assert.IsTrue(SafeLaunch.IsAllowedLaunchTarget("http://example.com/page"));
            Assert.IsTrue(SafeLaunch.IsAllowedLaunchTarget("ftp://files.example.com/readme.txt"));
        }

        [Test]
        public void IsAllowedLaunchTarget_Accepts_RootedExecutablePath() {
            Assert.IsTrue(SafeLaunch.IsAllowedLaunchTarget(@"C:\Windows\System32\notepad.exe"));
            Assert.IsTrue(SafeLaunch.IsAllowedLaunchTarget(@"C:\scripts\build.bat"));
        }

        [Test]
        public void CreateCmdInDirectory_DoesNotEmbedPathInArguments() {
            string dir = @"C:\Users\test & evil";
            ProcessStartInfo psi = SafeLaunch.CreateCmdInDirectory(dir);

            Assert.AreEqual("cmd.exe", psi.FileName);
            Assert.AreEqual("/k", psi.Arguments);
            Assert.AreEqual(dir, psi.WorkingDirectory);
            Assert.IsFalse(psi.Arguments.Contains("&"),
                "Working directory must not be concatenated into cmd arguments.");
        }

        [Test]
        public void CreateCmdInDirectory_Rejects_InvalidDirectory() {
            Assert.Throws<ArgumentException>(() => SafeLaunch.CreateCmdInDirectory(null));
            Assert.Throws<ArgumentException>(() => SafeLaunch.CreateCmdInDirectory(""));
            Assert.Throws<ArgumentException>(() => SafeLaunch.CreateCmdInDirectory(@"C:\missing & evil"));
        }

        [Test]
        public void TryStart_Rejects_DisallowedTarget_WithoutThrowing() {
            bool started = SafeLaunch.TryStart("relative.exe", out Process process);
            Assert.IsFalse(started);
            Assert.IsNull(process);
        }
    }
}
