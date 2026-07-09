using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch1C3Tests {
        [Test]
        public void CreateTab_Static_Method_Removed_As_Dead_Code() {
            MethodInfo method = typeof(QTTabBarClass).GetMethod(
                "CreateTab",
                BindingFlags.Public | BindingFlags.Static);
            Assert.IsNull(method, "Static QTTabBarClass.CreateTab should be removed when no callers exist");
        }
    }
}
