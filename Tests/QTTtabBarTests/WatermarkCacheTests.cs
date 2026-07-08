using System;

using System.Threading;

using NUnit.Framework;

using QTTabBarLib;



namespace QTTtabBarTests {

    [TestFixture]

    public class WatermarkCacheTests {

        [Test]

        public void ResourceCache_DoesNotCacheNull() {

            int factoryCalls = 0;

            var cache = new ResourceCache<string, string>(key => {

                Interlocked.Increment(ref factoryCalls);

                return null;

            });



            Assert.IsNull(cache["missing"]);

            Assert.IsNull(cache["missing"]);

            Assert.AreEqual(2, factoryCalls, "null 结果不应被缓存,每次访问应重新调用 factory。");

        }



        [Test]

        public void ResourceCache_GetOrAdd_ReturnsSameInstance() {

            var cache = new ResourceCache<string, string>(key => "value-" + key);



            string first = cache["a"];

            string second = cache["a"];



            Assert.AreSame(first, second);

        }



        [Test]

        public void ClearWatermarkCache_DoesNotThrow() {

            Assert.DoesNotThrow(() => ExplorerManager.ClearWatermarkCache());

        }

    }

}


