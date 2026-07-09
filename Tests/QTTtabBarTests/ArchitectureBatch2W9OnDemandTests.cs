using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W9OnDemandTests {
        [Test]
        public void ResMain_And_ResMisc_Are_OnDemand_Properties_On_ResourceCache() {
            Assert.IsNotNull(typeof(ResourceCache).GetProperty(
                "ResMain",
                BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNotNull(typeof(ResourceCache).GetProperty(
                "ResMisc",
                BindingFlags.NonPublic | BindingFlags.Static));
            Assert.IsNull(typeof(QTUtility).GetProperty(
                "ResMain",
                BindingFlags.NonPublic | BindingFlags.Static),
                "QTUtility should not expose ResMain after C7q");
        }

        [Test]
        public void TextResourcesDic_Assignment_ResMain_ResMisc_Reflect_New_Values_OnDemand() {
            ConfigManager.Initialize();
            Dictionary<string, string[]> savedDic = ResourceCache.TextResourcesDic;
            try {
                var replacement = new Dictionary<string, string[]>(savedDic ?? new Dictionary<string, string[]>());
                string[] newMain = { "replacement-main" };
                string[] newMisc = { "replacement-misc" };
                replacement["TabBar_Menu"] = newMain;
                replacement["Misc_Strings"] = newMisc;

                ResourceCache.TextResourcesDic = replacement;

                CollectionAssert.AreEqual(newMain, ResourceCache.ResMain,
                    "ResMain should read TabBar_Menu from the current TextResourcesDic");
                CollectionAssert.AreEqual(newMisc, ResourceCache.ResMisc,
                    "ResMisc should read Misc_Strings from the current TextResourcesDic");
            }
            finally {
                ResourceCache.TextResourcesDic = savedDic;
                QTResourceManager.ValidateTextResources();
            }
        }
    }
}
