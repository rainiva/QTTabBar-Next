using System.Diagnostics;
using System.Linq;
using System.Management;

namespace QTTabBarLib {
    internal static class ProcessHelper {
        internal static string GetParentProcessName() {
            Process currentProcess = Process.GetCurrentProcess();
            Process process = GetParent(currentProcess);
            return process == null ? string.Empty : process.ProcessName;
        }

        internal static Process GetParent(Process process) {
            try {
                using(var query = new ManagementObjectSearcher(
                    "root\\CIMV2",
                    "SELECT ParentProcessId FROM Win32_Process WHERE ProcessId=" + process.Id)) {
                    return query
                        .Get()
                        .OfType<ManagementObject>()
                        .Select(p => Process.GetProcessById((int)(uint)p["ParentProcessId"]))
                        .FirstOrDefault();
                }
            }
            catch {
                return null;
            }
        }
    }
}
