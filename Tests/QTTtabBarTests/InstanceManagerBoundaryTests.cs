using System;
using System.IO;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class InstanceManagerBoundaryTests {
        [Test]
        public void InstanceManager_Does_Not_Own_Transport_And_Lifecycle_Implementation() {
            string source = ReadQtTabBarFile("InstanceManager.cs");
            StringAssert.DoesNotContain("class DuplexClient", source);
            StringAssert.DoesNotContain("class CommService", source);
            StringAssert.DoesNotContain("SameUserAuthorizationManager", source);
        }

        [Test]
        public void InstanceManager_Does_Not_Own_Instance_Registry_Fields() {
            string source = ReadQtTabBarFile("InstanceManager.cs");
            StringAssert.DoesNotContain("StackDictionary<IntPtr, ICommClient> sdInstances", source);
            StringAssert.DoesNotContain("List<ICommClient> callbacks", source);
        }

        [Test]
        public void InstanceManager_Does_Not_Own_TrayIcon_Field() {
            string source = ReadQtTabBarFile("InstanceManager.cs");
            StringAssert.DoesNotContain("TrayIcon trayIcon", source);
        }

        [Test]
        public void Extracted_Ipc_And_Instance_Types_Exist() {
            Assert.IsNotNull(Type.GetType("QTTabBarLib.Ipc.IpcCommandGateway, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.Ipc.NamedPipeTransport, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.Ipc.IpcServerLifecycle, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.Instances.ExplorerInstanceRegistry, QTTabBar"));
            Assert.IsNotNull(Type.GetType("QTTabBarLib.Tray.TrayIconGateway, QTTabBar"));
        }

        private static string ReadQtTabBarFile(string relativePath) {
            return File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", relativePath));
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
