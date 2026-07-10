using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class TabInsertionPolicyTests {
        [TestCase(TabPos.Rightmost, 5, 2, 5)]
        [TestCase(TabPos.Right, 5, 2, 3)]
        [TestCase(TabPos.Left, 5, 2, 1)]
        [TestCase(TabPos.Leftmost, 5, 2, 0)]
        public void Resolve_Returns_Expected(TabPos pos, int count, int selected, int expected) {
            Assert.AreEqual(expected, TabInsertionPolicy.Resolve(pos, count, selected));
        }
    }
}
