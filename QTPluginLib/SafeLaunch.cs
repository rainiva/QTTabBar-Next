using System;
using System.Diagnostics;
using System.IO;

namespace QTPluginLib {
    /// <summary>
    /// Shared safe process-launch helpers for core and plugin assemblies.
    /// </summary>
    public static class SafeLaunch {
        private static readonly char[] ShellMetacharacters = { '&', '|', '^', '\r', '\n' };

        public static bool IsAllowedLaunchTarget(string target) {
            if(string.IsNullOrWhiteSpace(target)) {
                return false;
            }

            target = target.Trim();
            if(IsWebUrl(target)) {
                return true;
            }

            if(target.IndexOfAny(ShellMetacharacters) >= 0) {
                return false;
            }

            if(!Path.IsPathRooted(target)) {
                return false;
            }

            string extension = Path.GetExtension(target);
            if(!string.IsNullOrEmpty(extension)) {
                extension = extension.ToLowerInvariant();
                if(extension == ".exe" || extension == ".bat" || extension == ".cmd" || extension == ".msi") {
                    return true;
                }
            }

            try {
                if(Directory.Exists(target) || File.Exists(target)) {
                    return true;
                }
            }
            catch {
                return false;
            }

            return false;
        }

        public static ProcessStartInfo CreateCmdInDirectory(string directory) {
            if(string.IsNullOrWhiteSpace(directory)) {
                throw new ArgumentException("Directory is required.", nameof(directory));
            }
            if(directory.IndexOfAny(ShellMetacharacters) >= 0) {
                throw new ArgumentException("Directory contains invalid shell metacharacters.", nameof(directory));
            }

            return new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = "/k",
                WorkingDirectory = directory,
                WindowStyle = ProcessWindowStyle.Normal,
                UseShellExecute = false,
            };
        }

        public static bool TryStart(string target, out Process process, Action<ProcessStartInfo> configure = null) {
            process = null;
            if(!IsAllowedLaunchTarget(target)) {
                return false;
            }

            ProcessStartInfo startInfo = new ProcessStartInfo(target);
            if(IsWebUrl(target)) {
                startInfo.UseShellExecute = true;
            }
            else {
                string directory = Path.GetDirectoryName(target);
                if(!string.IsNullOrEmpty(directory)) {
                    startInfo.WorkingDirectory = directory;
                }
            }

            configure?.Invoke(startInfo);
            try {
                process = Process.Start(startInfo);
                return process != null;
            }
            catch {
                process = null;
                return false;
            }
        }

        public static bool TryStart(string target, Action<ProcessStartInfo> configure = null) {
            Process process;
            bool started = TryStart(target, out process, configure);
            process?.Dispose();
            return started;
        }

        private static bool IsWebUrl(string target) {
            return target.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                   || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                   || target.StartsWith("ftp://", StringComparison.OrdinalIgnoreCase);
        }
    }
}
