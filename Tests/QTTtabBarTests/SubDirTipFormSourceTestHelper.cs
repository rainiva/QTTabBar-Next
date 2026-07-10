using System.IO;

namespace QTTtabBarTests {
    // SubDirTipForm was split into partial files during the Batch7 god-class
    // teardown (ShellMenuGenerator, and later Thumbnail/DragDrop controllers).
    // Architecture source-scan tests must read the whole class, not just the
    // primary file, mirroring the ExtendedListViewCommon / PluginServer splits.
    internal static class SubDirTipFormSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            string combined = File.ReadAllText(Path.Combine(dir, "SubDirTipForm.cs"));
            foreach(string partial in new[] {
                "SubDirTipForm.ShellMenuGenerator.cs",
                "SubDirTipForm.ThumbnailController.cs",
                "SubDirTipForm.DragDropController.cs",
            }) {
                string path = Path.Combine(dir, partial);
                if(File.Exists(path)) {
                    combined += File.ReadAllText(path);
                }
            }
            return combined;
        }
    }
}
