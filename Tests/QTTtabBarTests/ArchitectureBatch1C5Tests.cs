using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ArchitectureBatch1C5Tests {
        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type TabInstanceRegistryType {
            get { return typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.TabInstanceRegistry"); }
        }

        [TearDown]
        public void TearDown() {
            try {
                MethodInfo unregister = TabInstanceRegistryType.GetMethod(
                    "UnregisterTabBar", AnyStatic, null,
                    new[] { typeof(IntPtr).MakeByRefType() }, null);
                unregister?.Invoke(null, new object[] { IntPtr.Zero });
            }
            catch { /* best effort */ }
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

        [Test]
        public void UnregisterTabBar_Source_Invokes_DeleteInstance() {
            string content = File.ReadAllText(Path.Combine(FindRepoRoot(), "QTTabBar", "Ipc", "IpcCommandGateway.cs"));
            int methodIndex = content.IndexOf("bool UnregisterTabBar()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0);
            int brace = content.IndexOf('{', methodIndex);
            int nextMethod = content.IndexOf("\n        internal static ", brace + 1, StringComparison.Ordinal);
            string body = nextMethod > 0
                ? content.Substring(brace, nextMethod - brace)
                : content.Substring(brace, Math.Min(500, content.Length - brace));
            Assert.IsTrue(body.Contains("DeleteInstance"));
        }

        [Test]
        public void TabInstanceRegistry_UnregisterTabBar_Returns_Removed_Handle() {
            MethodInfo unregister = TabInstanceRegistryType.GetMethod(
                "UnregisterTabBar", AnyStatic, null,
                new[] { typeof(IntPtr).MakeByRefType() }, null);
            Assert.IsNotNull(unregister);

            MethodInfo push = TabInstanceRegistryType.GetMethod("PushTabBarInstance", AnyStatic);
            Assert.IsNotNull(push);

            using(var tabBar = new QTTabBarClass()) {
                tabBar.CreateControl();
                IntPtr expectedHandle = tabBar.Handle;
                Assert.AreNotEqual(IntPtr.Zero, expectedHandle);
                push.Invoke(null, new object[] { tabBar });
                object[] args = { IntPtr.Zero };
                Assert.IsTrue((bool)unregister.Invoke(null, args));
                Assert.AreEqual(expectedHandle, (IntPtr)args[0]);
            }
        }
    }
}
