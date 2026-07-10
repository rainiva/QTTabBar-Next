using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using System.Text;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5dIpcTests {
        [Test]
        public void IpcCommand_Batch5d_EnumValues_Are_Frozen() {
            Assert.AreEqual(12, (byte)IpcCommand.MergeTabs);
            Assert.AreEqual(13, (byte)IpcCommand.OpenNewTabOrWindowFromPath);
            Assert.AreEqual(14, (byte)IpcCommand.OpenPluginOptions);
        }

        [Test]
        public void EncodeMergeTabs_RoundTrips_Json() {
            var payloads = new[] {
                new MergeTabPayload { Path = @"C:\A", Text = "A", Locked = true, ImageKey = "folder" },
                new MergeTabPayload { Path = @"C:\B", Text = "B", Locked = false, ImageKey = "" },
            };
            byte[] buffer = IpcCommandMessage.EncodeMergeTabs(payloads);
            IpcCommand cmd;
            byte[] decodedPayload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out decodedPayload));
            Assert.AreEqual(IpcCommand.MergeTabs, cmd);
            MergeTabPayload[] roundTrip;
            Assert.IsTrue(IpcCommandMessage.TryDecodeMergeTabs(decodedPayload, out roundTrip));
            Assert.AreEqual(2, roundTrip.Length);
            Assert.AreEqual(@"C:\A", roundTrip[0].Path);
            Assert.IsTrue(roundTrip[0].Locked);
        }

        [Test]
        public void MergeAllWindows_Does_Not_Use_Delegate_Ipc() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar",
                "QTTabBarClass.WindowManagementHost.cs"));
            Assert.IsFalse(content.Contains("TabBarBroadcast"),
                "MergeAllWindows should not use TabBarBroadcast");
            Assert.IsFalse(content.Contains("InvokeMain("),
                "MergeAllWindows should not use InvokeMain");
            Assert.IsTrue(content.Contains("BeginInvokeMainMergeTabs"),
                "MergeAllWindows should use typed merge IPC");
        }

        [Test]
        public void Production_Code_Has_No_Delegate_Ipc_Call_Sites() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
            foreach(string file in files) {
                string name = Path.GetFileName(file);
                if(name == "InstanceManager.cs" || name == "TabInstanceRegistry.cs") {
                    continue;
                }
                string content = File.ReadAllText(file);
                Assert.IsFalse(content.Contains("TabBarBroadcast("), name + " must not call TabBarBroadcast");
                Assert.IsFalse(content.Contains("ButtonBarBroadcast("), name + " must not call ButtonBarBroadcast");
                Assert.IsFalse(ContainsInstanceInvokeMain(content), name + " must not call InvokeMain");
                Assert.IsFalse(content.Contains("BeginInvokeMain(tabbar"), name + " must not call delegate BeginInvokeMain");
            }
        }

        private static bool ContainsInstanceInvokeMain(string content) {
            int index = 0;
            while((index = content.IndexOf("InvokeMain(", index, StringComparison.Ordinal)) >= 0) {
                if(index < "Local".Length || content.Substring(index - "Local".Length, "Local".Length) != "Local") {
                    return true;
                }
                index += "InvokeMain(".Length;
            }
            return false;
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
