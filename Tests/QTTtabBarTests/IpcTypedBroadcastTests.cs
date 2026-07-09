using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class IpcTypedBroadcastTests {
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

        [Test]
        public void IpcCommand_RefreshButtonBars_EnumValue_Is_Six() {
            Assert.AreEqual(6, (byte)IpcCommand.RefreshButtonBars);
        }

        [Test]
        public void EncodeRefreshButtonBars_Produces_QTIP_Header_Only() {
            byte[] buffer = IpcCommandMessage.EncodeRefreshButtonBars();
            Assert.AreEqual(HeaderLength, buffer.Length);
            Assert.AreEqual((byte)'Q', buffer[0]);
            Assert.AreEqual((byte)'T', buffer[1]);
            Assert.AreEqual((byte)'I', buffer[2]);
            Assert.AreEqual((byte)'P', buffer[3]);
            Assert.AreEqual((byte)1, buffer[4]);
            Assert.AreEqual((byte)IpcCommand.RefreshButtonBars, buffer[5]);

            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.RefreshButtonBars, cmd);
            Assert.AreEqual(0, payload.Length);
        }

        [Test]
        public void Dispatcher_RefreshButtonBars_Creates_Client_Action() {
            byte[] buffer = IpcCommandMessage.EncodeRefreshButtonBars();
            Type dispatcher = typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.IpcCommandDispatcher");
            MethodInfo m = dispatcher.GetMethod(
                "TryCreateClientAction",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            object[] args = new object[] { buffer, null };
            Assert.IsTrue((bool)m.Invoke(null, args));
            Assert.IsNotNull(args[1], "RefreshButtonBars should produce a client work action");
        }

        [Test]
        public void InstanceManager_Exposes_BroadcastRefreshButtonBars() {
            MethodInfo method = typeof(InstanceManager).GetMethod(
                "BroadcastRefreshButtonBars",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(method, "InstanceManager.BroadcastRefreshButtonBars should exist");
        }

        [Test]
        public void TabBarBase_RefreshButtons_Uses_Typed_Broadcast() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "TabBarBase.cs"));
            int methodIndex = content.IndexOf("AddToHistory(QTabItem closingTab)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int searchEnd = Math.Min(content.Length, methodIndex + 700);
            string body = content.Substring(methodIndex, searchEnd - methodIndex);
            Assert.IsTrue(body.Contains("BroadcastRefreshButtonBars"),
                "AddToHistory should refresh button bars via typed IPC broadcast");
            Assert.IsFalse(body.Contains("ButtonBarBroadcast(bbar => bbar.RefreshButtons()"),
                "RefreshButtons should no longer use delegate ButtonBarBroadcast");
        }
    }
}
