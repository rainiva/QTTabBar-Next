using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using QTPlugin;
using QTTabBarLib;

namespace QTTtabBarTests {
    internal static class TabCreationTestFixtures {
        internal static IEnumerable<string> InvalidTargets {
            get {
                yield return @"Z:\QTTabBar_Nonexistent_Drive";
                string deadLink = Path.Combine(Path.GetTempPath(), "qttb-dead-link-" + Guid.NewGuid().ToString("N") + ".lnk");
                File.WriteAllBytes(deadLink, new byte[] { 0 });
                yield return deadLink;
                string filePath = Path.Combine(Path.GetTempPath(), "qttb-not-a-folder-" + Guid.NewGuid().ToString("N") + ".txt");
                File.WriteAllText(filePath, "x");
                yield return filePath;
            }
        }

        internal sealed class TabCreationTestBar : TabBarBase, IDisposable {
            public QTabControl TabControl => tabControl1;

            public static TabCreationTestBar Create() {
                var bar = (TabCreationTestBar)FormatterServices.GetUninitializedObject(typeof(TabCreationTestBar));
                bar.tabControl1 = new QTabControl();
                return bar;
            }

            protected override bool IsTabSubFolderMenuVisible => false;

            protected override int CalcBandHeight(int count) => 30;

            public new void Dispose() {
                tabControl1?.Dispose();
            }
        }
    }
}
