using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5bIpcTests {
        private const int HeaderLength = 6;

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

        [Test]
        public void IpcCommand_Batch5b_EnumValues_Are_Frozen() {
            Assert.AreEqual(7, (byte)IpcCommand.SyncSearchBoxWidth);
            Assert.AreEqual(8, (byte)IpcCommand.RestoreMainWindow);
            Assert.AreEqual(9, (byte)IpcCommand.OpenGroup);
        }

        [Test]
        public void EncodeSyncSearchBoxWidth_RoundTrips() {
            byte[] buffer = IpcCommandMessage.EncodeSyncSearchBoxWidth(420);
            Assert.AreEqual(HeaderLength + 4, buffer.Length);
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.SyncSearchBoxWidth, cmd);
            int width;
            Assert.IsTrue(IpcCommandMessage.TryDecodeSyncSearchBoxWidth(payload, out width));
            Assert.AreEqual(420, width);
        }

        [Test]
        public void EncodeRestoreMainWindow_Is_Header_Only() {
            byte[] buffer = IpcCommandMessage.EncodeRestoreMainWindow();
            Assert.AreEqual(HeaderLength, buffer.Length);
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.RestoreMainWindow, cmd);
            Assert.AreEqual(0, payload.Length);
        }

        [Test]
        public void EncodeOpenGroup_RoundTrips_Utf8() {
            byte[] buffer = IpcCommandMessage.EncodeOpenGroup("MyGroup");
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.OpenGroup, cmd);
            string group;
            Assert.IsTrue(IpcCommandMessage.TryDecodeOpenGroup(payload, out group));
            Assert.AreEqual("MyGroup", group);
        }

        [Test]
        public void Dispatcher_Batch5b_Commands_Create_Client_Actions() {
            Type dispatcher = typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.IpcCommandDispatcher");
            MethodInfo m = dispatcher.GetMethod(
                "TryCreateClientAction",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            foreach(byte[] buffer in new[] {
                IpcCommandMessage.EncodeSyncSearchBoxWidth(100),
                IpcCommandMessage.EncodeRestoreMainWindow(),
                IpcCommandMessage.EncodeOpenGroup("g1"),
            }) {
                object[] args = new object[] { buffer, null };
                Assert.IsTrue((bool)m.Invoke(null, args), "Expected client action for " + buffer[5]);
                Assert.IsNotNull(args[1]);
            }
        }

        [Test]
        public void SearchBox_Resize_Uses_Typed_SyncSearchBoxWidth() {
            string content = ReadQtTabBarFile("QTButtonBar.BandLifecycle.cs");
            int idx = content.IndexOf("searchBox_ResizeComplete", StringComparison.Ordinal);
            Assert.GreaterOrEqual(idx, 0);
            int end = Math.Min(content.Length, idx + 500);
            string body = content.Substring(idx, end - idx);
            Assert.IsTrue(body.Contains("BroadcastSyncSearchBoxWidth"),
                "search box resize should use typed SyncSearchBoxWidth broadcast");
            Assert.IsFalse(body.Contains("ButtonBarBroadcast(bbar =>"),
                "search box resize should not use delegate ButtonBarBroadcast");
        }

        [Test]
        public void OpenGroup_Uses_Typed_BeginInvokeMainOpenGroup() {
            string content = ReadQtTabBarFile("QTDesktopTool.OpenNavigation.cs");
            Assert.IsTrue(content.Contains("BeginInvokeMainOpenGroup"),
                "OpenGroup should use typed BeginInvokeMainOpenGroup");
            Assert.IsFalse(content.Contains("BeginInvokeMain(tabbar => tabbar.OpenGroup"),
                "OpenGroup should not use delegate BeginInvokeMain");
        }
    }
}
