using System.IO;
using System.Linq;

namespace QTTtabBarTests {
    internal static class IpcSourceTestHelper {
        public static string ReadCombined(string repoRoot) {
            string dir = Path.Combine(repoRoot, "QTTabBar");
            string instanceManager = File.ReadAllText(Path.Combine(dir, "InstanceManager.cs"));
            string ipcDir = Path.Combine(dir, "Ipc");
            string instancesDir = Path.Combine(dir, "Instances");
            if(!Directory.Exists(ipcDir)) {
                return instanceManager;
            }
            string ipcSources = string.Concat(Directory.GetFiles(ipcDir, "*.cs", SearchOption.TopDirectoryOnly)
                .OrderBy(path => path)
                .Select(File.ReadAllText));
            string instanceSources = Directory.Exists(instancesDir)
                ? string.Concat(Directory.GetFiles(instancesDir, "*.cs", SearchOption.TopDirectoryOnly)
                    .OrderBy(path => path)
                    .Select(File.ReadAllText))
                : string.Empty;
            return instanceManager + ipcSources + instanceSources;
        }
    }
}
