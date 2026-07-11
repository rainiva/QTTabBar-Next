using System;
using System.IO;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch5PartialMergeTests {
        [Test]
        public void QTTabBarClass_PartialDeclarations_ShouldBeAtMost4() {
            int count = SourceMetrics.PartialDeclarationCount("QTTabBarClass");
            Assert.LessOrEqual(count, 4,
                $"QTTabBarClass should have at most 4 partial declarations, but has {count}. " +
                "Merge host/controller files into grouped files.");
        }
    }
}
