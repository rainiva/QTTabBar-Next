using System;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5PartialMergeTests {
        [Test]
        public void QTTabBarClass_PartialDeclarations_ShouldBeAtMost6() {
            int count = SourceMetrics.PartialDeclarationCount("QTTabBarClass");
            Assert.LessOrEqual(count, 6,
                $"QTTabBarClass should have at most 6 partial declarations after Wave 18, but has {count}.");
        }
    }
}
