using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ViewPerceivedTypeResolverTests {
        [TestCase(2, 1)]
        [TestCase(3, 2)]
        [TestCase(4, 3)]
        [TestCase(6, 4)]
        [TestCase(0, 0)]
        [TestCase(-1, 0)]
        [TestCase(null, 0)]
        public void FromWindowsPerceivedType_MapsKnownValues(int? input, int expectedOrdinal) {
            Assert.AreEqual(expectedOrdinal, (int)ViewPerceivedTypeResolver.FromWindowsPerceivedType(input));
        }

        [Test]
        public void IsSameOrUnderPath_MatchesExactAndNestedPaths() {
            Assert.IsTrue(ViewPerceivedTypeResolver.IsSameOrUnderPath(
                @"C:\Users\me\Pictures\2024",
                @"C:\Users\me\Pictures"));
            Assert.IsTrue(ViewPerceivedTypeResolver.IsSameOrUnderPath(
                @"C:\Users\me\Pictures",
                @"C:\Users\me\Pictures"));
            Assert.IsFalse(ViewPerceivedTypeResolver.IsSameOrUnderPath(
                @"C:\Users\me\Music",
                @"C:\Users\me\Pictures"));
        }

        [Test]
        public void Resolve_ReturnsUnknownForNullBrowser() {
            Assert.AreEqual(PerceivedType.Unknown, ViewPerceivedTypeResolver.Resolve(null));
        }
    }
}
