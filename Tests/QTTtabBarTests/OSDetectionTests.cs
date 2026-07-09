using NUnit.Framework;
using System;
using QTTabBarLib;

namespace QTTtabBarTests
{
    [TestFixture]
    public class OSDetectionTests
    {
        [Test]
        public void CheckIsWin10_False_OnWin11_Build22000()
        {
            // Windows 11 has Build >= 22000, should NOT be detected as Win10
            Assert.IsFalse(OSDetector.CheckIsWin10(new Version(10, 0, 22000, 0)));
        }

        [Test]
        public void CheckIsWin10_False_OnWin11_Build22631()
        {
            // Windows 11 23H2
            Assert.IsFalse(OSDetector.CheckIsWin10(new Version(10, 0, 22631, 0)));
        }

        [Test]
        public void CheckIsWin10_True_OnWin10_Build19041()
        {
            // Windows 10 2004
            Assert.IsTrue(OSDetector.CheckIsWin10(new Version(10, 0, 19041, 0)));
        }

        [Test]
        public void CheckIsWin10_True_OnWin10_Build10240()
        {
            // Windows 10 RTM
            Assert.IsTrue(OSDetector.CheckIsWin10(new Version(10, 0, 10240, 0)));
        }

        [Test]
        public void CheckIsWin10_False_OnWin7()
        {
            // Windows 7: Major=6, Minor=1
            Assert.IsFalse(OSDetector.CheckIsWin10(new Version(6, 1, 7601, 0)));
        }

        [Test]
        public void CheckIsWin10_False_OnWin8()
        {
            // Windows 8: Major=6, Minor=2
            Assert.IsFalse(OSDetector.CheckIsWin10(new Version(6, 2, 9200, 0)));
        }

        [Test]
        public void CheckIsWin10_True_OnWin10_Threshold_Compat()
        {
            // Windows 10 Threshold reports as 6.4 in some compat scenarios
            Assert.IsTrue(OSDetector.CheckIsWin10(new Version(6, 4, 0, 0)));
        }

        [Test]
        public void IsWin10_And_IsWin11_Are_Mutually_Exclusive()
        {
            // On any OS, IsWin10 and IsWin11 should not both be true
            if (OSDetector.IsWin11)
            {
                Assert.IsFalse(OSDetector.IsWin10,
                    "IsWin10 should be false when IsWin11 is true (Build >= 22000)");
            }
        }
    }
}
