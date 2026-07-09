using System;
using System.IO;
using System.Reflection;

namespace QTTabBarLib {
    internal static class EmbeddedAssemblyLoader {
        private static bool registered;

        internal static int EnsureInitialized() {
            EnsureRegistered();
            return 0;
        }

        internal static void EnsureRegistered() {
            if(registered) {
                return;
            }
            registered = true;
            AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
        }

        private static Assembly OnAssemblyResolve(object sender, ResolveEventArgs args) {
            var requestedName = new AssemblyName(args.Name);
            foreach(var loaded in AppDomain.CurrentDomain.GetAssemblies()) {
                if(string.Equals(loaded.GetName().Name, requestedName.Name, StringComparison.OrdinalIgnoreCase)) {
                    return loaded;
                }
            }

            string resourceName = "QTTabBarLib.Resources." + requestedName.Name + ".dll";
            using(var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName)) {
                if(stream != null) {
                    byte[] assemblyData = new byte[stream.Length];
                    stream.Read(assemblyData, 0, assemblyData.Length);
                    return Assembly.Load(assemblyData);
                }
            }

            string installDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "QTTabBar");
            string installPath = Path.Combine(installDir, requestedName.Name + ".dll");
            if(File.Exists(installPath)) {
                return Assembly.LoadFrom(installPath);
            }

            return null;
        }
    }
}
