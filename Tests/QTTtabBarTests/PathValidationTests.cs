using System;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class PathValidationTests {

        [Test]
        public void IsValidExecutablePath_Rejects_EmptyString() {
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(""));
        }

        [Test]
        public void IsValidExecutablePath_Rejects_Null() {
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(null));
        }

        [Test]
        public void IsValidExecutablePath_Rejects_RelativePath() {
            Assert.IsFalse(QTUtility2.IsValidExecutablePath("notepad.exe"));
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(@"folder\app.exe"));
        }

        [Test]
        public void IsValidExecutablePath_Rejects_UrlScheme() {
            Assert.IsFalse(QTUtility2.IsValidExecutablePath("file:///malicious"));
            Assert.IsFalse(QTUtility2.IsValidExecutablePath("http://evil.com/payload.exe"));
        }

        [Test]
        public void IsValidExecutablePath_Accepts_AbsoluteExePath() {
            Assert.IsTrue(QTUtility2.IsValidExecutablePath(@"C:\Windows\explorer.exe"));
            Assert.IsTrue(QTUtility2.IsValidExecutablePath(@"C:\Windows\System32\notepad.exe"));
        }

        [Test]
        public void IsValidExecutablePath_Accepts_BatAndCmd() {
            Assert.IsTrue(QTUtility2.IsValidExecutablePath(@"C:\scripts\build.bat"));
            Assert.IsTrue(QTUtility2.IsValidExecutablePath(@"C:\scripts\deploy.cmd"));
        }

        [Test]
        public void IsValidExecutablePath_Rejects_NonExecutableExtensions() {
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(@"C:\Windows\readme.txt"));
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(@"C:\data\document.pdf"));
            Assert.IsFalse(QTUtility2.IsValidExecutablePath(@"C:\Windows\explorer.dll"));
        }
    }
}
