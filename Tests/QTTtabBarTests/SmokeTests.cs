using NUnit.Framework;

namespace QTTtabBarTests
{
    [TestFixture]
    public class SmokeTests
    {
        [Test]
        public void Project_CompilesAndRuns()
        {
            // Smoke test: verifies the test project can compile and execute.
            Assert.Pass("Test project is operational.");
        }

        [Test]
        public void QTTabBarLib_Namespace_IsAccessible()
        {
            // Verifies that the QTTabBarLib assembly is referenced correctly.
            var type = typeof(QTTabBarLib.QTUtility);
            Assert.IsNotNull(type, "QTUtility type should be accessible from the test project.");
        }
    }
}
