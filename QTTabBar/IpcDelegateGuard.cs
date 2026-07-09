using System;
using System.Reflection;

namespace QTTabBarLib {
    /// <summary>
    /// Guards legacy delegate IPC (BinaryFormatter + SerializeDelegate) so only
    /// delegates whose target methods live in trusted in-process assemblies are
    /// executed. Typed QTIP messages remain the preferred path for new IPC.
    /// </summary>
    internal static class IpcDelegateGuard {
        internal static bool TryUnwrapDelegate(object deserialized, out Delegate del) {
            del = null;
            SerializeDelegate wrapper = deserialized as SerializeDelegate;
            if(wrapper == null) {
                QTLogger.MakeErrorLog("IpcDelegateGuard: IPC payload is not SerializeDelegate");
                return false;
            }
            Delegate candidate = wrapper.Delegate;
            if(candidate == null) {
                return false;
            }
            if(!IsAllowedDelegate(candidate)) {
                MethodInfo method = candidate.Method;
                string detail = method == null ? "<unknown>" : method.DeclaringType + "." + method.Name;
                QTLogger.MakeErrorLog("IpcDelegateGuard: rejected untrusted delegate " + detail);
                return false;
            }
            del = candidate;
            return true;
        }

        private static bool IsAllowedDelegate(Delegate del) {
            MethodInfo method = del.Method;
            if(method == null) {
                return false;
            }
            Type declaringType = method.DeclaringType;
            if(declaringType == null) {
                return false;
            }
            string assemblyName = declaringType.Assembly.GetName().Name;
            if(assemblyName == "QTTabBar"
                || assemblyName == "QTPluginLib"
                || assemblyName == "BandObjectLib") {
                return true;
            }
#if DEBUG
            // Unit tests serialize callbacks from the test assembly through DelToByte/ByteToDel.
            if(assemblyName == "QTTtabBarTests") {
                return true;
            }
#endif
            return false;
        }
    }
}
