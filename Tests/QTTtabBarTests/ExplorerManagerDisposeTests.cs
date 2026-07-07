using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ExplorerManagerDisposeTests {

        [Test]
        public void Dispose_DoesNotThrow() {
            using (ExplorerManager manager = new ExplorerManager()) {
                Assert.DoesNotThrow(() => manager.Dispose());
            }
        }
    }
}
