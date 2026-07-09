using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ArchitectureBatch1C2Tests {
        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type ButtonBarRegistryType {
            get { return typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.ButtonBarRegistry"); }
        }

        [TearDown]
        public void TearDown() {
            try {
                ButtonBarRegistryType.GetMethod("UnregisterButtonBar", AnyStatic)?.Invoke(null, null);
            }
            catch { /* best effort */ }
        }

        [Test]
        public void TryGetButtonBarHandle_LooksUp_By_Explorer_Handle_Not_Current_Thread_Only() {
            var tryGet = ButtonBarRegistryType.GetMethod(
                "TryGetButtonBarHandle",
                AnyStatic,
                null,
                new[] { typeof(IntPtr), typeof(IntPtr).MakeByRefType() },
                null);
            Assert.IsNotNull(tryGet);

            var register = ButtonBarRegistryType.GetMethod("RegisterButtonBar", AnyStatic);
            Assert.IsNotNull(register);

            IntPtr registeredExplorer = new IntPtr(0x1234);
            IntPtr otherExplorer = new IntPtr(0x5678);

            using(var bbar = new QTButtonBar()) {
                bbar.CreateControl();
                typeof(QTButtonBar).GetField("ExplorerHandle", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(bbar, registeredExplorer);
                register.Invoke(null, new object[] { bbar });

                object[] args = { otherExplorer, IntPtr.Zero };
                Assert.IsFalse((bool)tryGet.Invoke(null, args));

                args = new object[] { registeredExplorer, IntPtr.Zero };
                Assert.IsTrue((bool)tryGet.Invoke(null, args));
                Assert.AreEqual(bbar.Handle, (IntPtr)args[1]);
            }
        }
    }
}
