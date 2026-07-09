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
            string[] savedMain = QTUtility.ResMain;
            string[] savedMisc = QTUtility.ResMisc;
            Dictionary<string, string[]> savedDic = QTUtility.TextResourcesDic;
            try {
                var replacement = new Dictionary<string, string[]>(savedDic ?? new Dictionary<string, string[]>());
                string[] newMain = { "replacement-main" };
                string[] newMisc = { "replacement-misc" };
                replacement["TabBar_Menu"] = newMain;
                replacement["Misc_Strings"] = newMisc;

                QTUtility.TextResourcesDic = replacement;

                Assert.AreSame(newMain, QTUtility.ResMain,
                    "ResMain should point at the new TabBar_Menu array after TextResourcesDic replacement");
                Assert.AreSame(newMisc, QTUtility.ResMisc,
                    "ResMisc should point at the new Misc_Strings array after TextResourcesDic replacement");
            }
            finally {
                QTUtility.TextResourcesDic = savedDic;
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
