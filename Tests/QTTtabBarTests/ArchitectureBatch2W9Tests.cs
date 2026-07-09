using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ArchitectureBatch2W9Tests {
        [Test]
        public void TextResourcesDic_Assignment_Refreshes_ResMain_And_ResMisc() {
            ConfigManager.Initialize();
            string[] savedMain = ResourceCache.ResMain;
            string[] savedMisc = ResourceCache.ResMisc;
            Dictionary<string, string[]> savedDic = ResourceCache.TextResourcesDic;
            try {
                var replacement = new Dictionary<string, string[]>(savedDic ?? new Dictionary<string, string[]>());
                string[] newMain = { "replacement-main" };
                string[] newMisc = { "replacement-misc" };
                replacement["TabBar_Menu"] = newMain;
                replacement["Misc_Strings"] = newMisc;

                ResourceCache.TextResourcesDic = replacement;

                Assert.AreSame(newMain, ResourceCache.ResMain,
                    "ResMain should point at the new TabBar_Menu array after TextResourcesDic replacement");
                Assert.AreSame(newMisc, ResourceCache.ResMisc,
                    "ResMisc should point at the new Misc_Strings array after TextResourcesDic replacement");
            }
            finally {
                ResourceCache.TextResourcesDic = savedDic;
                MethodInfo validate = typeof(QTUtility).GetMethod(
                    "ValidateTextResources",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    Type.EmptyTypes,
                    null);
                validate?.Invoke(null, null);
            }
        }
    }
}
