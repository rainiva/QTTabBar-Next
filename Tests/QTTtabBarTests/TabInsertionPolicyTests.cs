using System.Linq;
using System.Threading.Tasks;
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

        [TestCase(TabPos.Left, 0, -1, 0)]
        [TestCase(TabPos.Right, 0, -1, 0)]
        [TestCase(TabPos.Left, 5, 0, 0)]
        [TestCase(TabPos.Right, 5, 4, 5)]
        [TestCase(TabPos.Left, 5, 99, 4)]
        [TestCase(TabPos.Right, 5, 99, 5)]
        public void Resolve_Clamps_To_A_Valid_Insertion_Index(TabPos pos, int count, int selected, int expected) {
            int index = TabInsertionPolicy.Resolve(pos, count, selected);

            Assert.AreEqual(expected, index);
            Assert.GreaterOrEqual(index, 0);
            Assert.LessOrEqual(index, count);
        }

        [Test]
        public void Resolve_Is_Deterministic_Across_Parallel_Restore_Decisions() {
            var results = new int[1000];

            Parallel.For(0, results.Length, i => {
                results[i] = TabInsertionPolicy.Resolve(TabPos.Rightmost, 5, i % 6);
            });

            CollectionAssert.AreEqual(new int[results.Length].Select(_ => 5), results);
        }
    }
}
