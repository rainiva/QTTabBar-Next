using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Batch 1: idempotent guards + implicit entry convergence (architecture fix plan).
    /// </summary>
    [TestFixture]
    public class ArchitectureBatch1Tests {

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found from test base directory.");
        }

        private static bool HasVolatileBoolGuard(Type type) {
            return type
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static)
                .Any(f => f.FieldType == typeof(bool)
                    && f.GetRequiredCustomModifiers()
                        .Any(m => m == typeof(System.Runtime.CompilerServices.IsVolatile)));
        }

        [Test]
        public void InstanceManager_Has_Volatile_Bool_Initialized_Guard() {
            Assert.IsTrue(HasVolatileBoolGuard(typeof(InstanceManager)),
                "InstanceManager should have a private static volatile bool _initialized guard field");
        }

        [Test]
        public void PluginManager_Has_Volatile_Bool_Initialized_Guard() {
            Assert.IsTrue(HasVolatileBoolGuard(typeof(PluginManager)),
                "PluginManager should have a private static volatile bool _initialized guard field");
        }

        [Test]
        public void InstanceManager_Double_Initialize_Keeps_Same_ServiceHost_Reference() {
            var initMethod = typeof(InstanceManager).GetMethod("Initialize",
                BindingFlags.Public | BindingFlags.Static,
                null, new[] { typeof(bool) }, null);
            Assert.IsNotNull(initMethod, "InstanceManager.Initialize(bool) should exist");

            var serviceHostField = typeof(InstanceManager).GetField("serviceHost",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(serviceHostField, "InstanceManager.serviceHost field should exist");

            initMethod.Invoke(null, new object[] { true });
            object firstHost = serviceHostField.GetValue(null);

            initMethod.Invoke(null, new object[] { true });
            object secondHost = serviceHostField.GetValue(null);

            Assert.AreSame(firstHost, secondHost,
                "Second Initialize() call should not replace serviceHost (idempotent guard)");
        }

        [Test]
        public void InstanceManager_SafeReinitialize_Closes_Old_ServiceHost() {
            var safeReinitMethod = typeof(InstanceManager).GetMethod("SafeReinitialize",
                BindingFlags.NonPublic | BindingFlags.Static);
            var serviceHostField = typeof(InstanceManager).GetField("serviceHost",
                BindingFlags.NonPublic | BindingFlags.Static);
            var initializedField = typeof(InstanceManager).GetField("_initialized",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.IsNotNull(safeReinitMethod);
            Assert.IsNotNull(serviceHostField);
            Assert.IsNotNull(initializedField);

            Type commServiceType = typeof(InstanceManager).GetNestedType("CommService",
                BindingFlags.NonPublic);
            Assert.IsNotNull(commServiceType, "CommService nested type should exist for host injection");

            string address = "net.pipe://localhost/QTTabBarBatch1Test" + Guid.NewGuid().ToString("N");
            ServiceHost oldHost = new ServiceHost(commServiceType, new Uri[] { new Uri(address) });
            try { oldHost.Open(); }
            catch { /* opening may fail in isolated test host; Close still applies */ }

            serviceHostField.SetValue(null, oldHost);
            initializedField.SetValue(null, true);

            safeReinitMethod.Invoke(null, null);

            Assert.AreEqual(CommunicationState.Closed, oldHost.State,
                "SafeReinitialize should close the previous ServiceHost before re-initializing");
        }

        [Test]
        public void QTButtonBar_Constructor_Calls_QTUtility_Initialize() {
            string sourcePath = Path.Combine(FindRepoRoot(), "QTTabBar", "QTButtonBar.cs");
            string content = File.ReadAllText(sourcePath);
            int ctorIndex = content.IndexOf("public QTButtonBar()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(ctorIndex, 0, "QTButtonBar constructor should exist");

            int braceIndex = content.IndexOf('{', ctorIndex);
            int nextCtor = content.IndexOf("public ", braceIndex + 1);
            string ctorBody = nextCtor > 0
                ? content.Substring(braceIndex, nextCtor - braceIndex)
                : content.Substring(braceIndex, Math.Min(500, content.Length - braceIndex));

            Assert.IsTrue(ctorBody.Contains("QTUtility.Initialize()"),
                "QTButtonBar constructor should call QTUtility.Initialize() explicitly");
        }

        [Test]
        public void QTSecondViewBar_Constructor_Calls_QTUtility_Initialize() {
            string sourcePath = Path.Combine(FindRepoRoot(), "QTTabBar", "QTSecondViewBar.cs");
            string content = File.ReadAllText(sourcePath);
            int ctorIndex = content.IndexOf("public QTSecondViewBar()", StringComparison.Ordinal);
            Assert.GreaterOrEqual(ctorIndex, 0, "QTSecondViewBar constructor should exist");

            int braceIndex = content.IndexOf('{', ctorIndex);
            int nextMethod = content.IndexOf("private void ", braceIndex + 1);
            string ctorBody = nextMethod > 0
                ? content.Substring(braceIndex, nextMethod - braceIndex)
                : content.Substring(braceIndex, Math.Min(800, content.Length - braceIndex));

            Assert.IsTrue(ctorBody.Contains("QTUtility.Initialize()"),
                "QTSecondViewBar constructor should call QTUtility.Initialize() explicitly");
        }

        [Test]
        public void InitializationOrchestrator_Sets_Initialized_On_Exception() {
            string sourcePath = Path.Combine(FindRepoRoot(), "QTTabBar", "InitializationOrchestrator.cs");
            string content = File.ReadAllText(sourcePath);
            int lockIndex = content.IndexOf("lock(_lock)", StringComparison.Ordinal);
            Assert.GreaterOrEqual(lockIndex, 0, "InitializationOrchestrator should guard with lock(_lock)");

            int tryIndex = content.IndexOf("try {", lockIndex, StringComparison.Ordinal);
            Assert.GreaterOrEqual(tryIndex, 0, "InitializationOrchestrator should wrap init body in try");

            string guardSection = content.Substring(lockIndex, tryIndex - lockIndex);
            Assert.IsTrue(guardSection.Contains("_initialized = true"),
                "InitializationOrchestrator should set _initialized before the init try block to prevent infinite retry");
        }
    }
}
