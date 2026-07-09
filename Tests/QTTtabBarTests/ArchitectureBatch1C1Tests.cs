using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class ArchitectureBatch1C1Tests {
        [Test]
        public void CurrentLocation_SetAndGet_DoesNotStackOverflow() {
            using(var bar = new QTSecondViewBar()) {
                Assert.DoesNotThrow(() => bar.CurrentLocation = null);
                Assert.IsNull(bar.CurrentLocation);
            }
        }
    }
}
