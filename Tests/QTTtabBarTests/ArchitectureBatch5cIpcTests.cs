using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5cIpcTests {
        [Test]
        public void IpcCommand_Batch5c_EnumValues_Are_Frozen() {
            Assert.AreEqual(10, (byte)IpcCommand.OpenNewTabFromIdl);
            Assert.AreEqual(11, (byte)IpcCommand.CaptureNewWindow);
        }

        [Test]
        public void EncodeOpenNewTabOrWindowFromIdl_RoundTrips() {
            byte[] idl = { 0x01, 0x02, 0x03, 0x04 };
            byte[] buffer = IpcCommandMessage.EncodeOpenNewTabOrWindowFromIdl(idl);
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.OpenNewTabFromIdl, cmd);
            byte[] decoded;
            Assert.IsTrue(IpcCommandMessage.TryDecodeOpenNewTabOrWindowFromIdl(payload, out decoded));
            CollectionAssert.AreEqual(idl, decoded);
        }

        [Test]
        public void EncodeOpenNewTabSequence_RoundTrips() {
            byte[][] idls = { new byte[] { 1, 2 }, new byte[] { 3, 4, 5 } };
            byte[] buffer = IpcCommandMessage.EncodeOpenNewTabSequence(idls);
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.OpenNewTabFromIdl, cmd);
            byte[][] decoded;
            Assert.IsTrue(IpcCommandMessage.TryDecodeOpenNewTabSequence(payload, out decoded));
            Assert.AreEqual(2, decoded.Length);
            CollectionAssert.AreEqual(idls[0], decoded[0]);
            CollectionAssert.AreEqual(idls[1], decoded[1]);
        }

        [Test]
        public void EncodeCaptureNewWindow_RoundTrips() {
            byte[] buffer = IpcCommandMessage.EncodeCaptureNewWindow(@"C:\Test", 1, "file.txt");
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.CaptureNewWindow, cmd);
            string path;
            int cmdType;
            string selectName;
            Assert.IsTrue(IpcCommandMessage.TryDecodeCaptureNewWindow(payload, out path, out cmdType, out selectName));
            Assert.AreEqual(@"C:\Test", path);
            Assert.AreEqual(1, cmdType);
            Assert.AreEqual("file.txt", selectName);
        }

        [Test]
        public void CommandDispatch_Does_Not_Use_BeginInvokeMain() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.ExplorerController.CommandDispatch.cs"));
            Assert.IsFalse(content.Contains("BeginInvokeMain(tabbar"),
                "CommandDispatch should not use delegate BeginInvokeMain");
            Assert.IsTrue(content.Contains("BeginInvokeMainCaptureNewWindow"),
                "CommandDispatch should call typed capture helper");
        }

        [Test]
        public void HookLibManager_Does_Not_Use_BeginInvokeMain() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "HookLibManager.cs"));
            Assert.IsFalse(content.Contains("BeginInvokeMain("),
                "HookLibManager should use typed OpenNewTabFromIdl IPC");
        }

        [Test]
        public void DesktopTool_OpenFolders_Does_Not_Use_BeginInvokeMain() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTDesktopTool.OpenNavigation.cs"));
            Assert.IsFalse(content.Contains("BeginInvokeMain(tabbar =>"),
                "OpenFolders should use typed OpenNewTab sequence IPC");
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
