using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace QTTabBarLib
{
    internal class PreMergeToMergedDeserializationBinder : SerializationBinder
    {
        // mscorlib serialization-infrastructure types that must be allowed so a
        // legitimate SerializeDelegate can round-trip. They are known-safe and are
        // resolved by BinaryFormatter's default binding (return null == use default).
        private static readonly string[] AllowedFrameworkInfra = new[]
        {
            "System.UnitySerializationHolder",
            "System.DelegateSerializationHolder",
            "System.DelegateSerializationHolder+DelegateEntry",
            "System.Reflection.MemberInfoSerializationHolder"
        };

        public override Type BindToType(string assemblyName, string typeName)
        {
            String exeAssembly = Assembly.GetExecutingAssembly().FullName;

            QTUtility2.log("PreMergeToMergedDeserializationBinder exeAssembly:" + exeAssembly + " typeName: " + typeName);

            // Legacy FMDServiceProxy namespace migration (whitelisted).
            if (typeName != null && typeName.Contains("Entities."))
            {
                string migrated = typeName.Replace("Entities", "FMDService");
                return Type.GetType("FMDServiceProxy." + migrated + ", FMDServiceProxy, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null");
            }

            // QTTabBarLib types (SerializeDelegate, the nested AnonymousClassWrapper,
            // config classes, etc.) are resolved against the executing assembly, keeping
            // the original pre-merge -> merged behaviour intact.
            if (IsQTTabBarType(assemblyName, typeName))
            {
                return Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));
            }

            // Known-safe framework serialization holders: let BinaryFormatter bind them.
            if (IsAllowedFrameworkInfra(typeName))
            {
                return null;
            }

            // Anything else is NOT whitelisted. Returning null is not enough because
            // BinaryFormatter falls back to default binding and would still materialise
            // the type. Serializable types are the real deserialization-gadget attack
            // surface, so reject them hard by throwing. Non-serializable types cannot be
            // instantiated by BinaryFormatter anyway, so returning null is safe there.
            QTUtility2.MakeErrorLog("PreMergeToMergedDeserializationBinder rejected non-whitelisted type: [" + assemblyName + "] " + typeName);

            Type candidate = ResolveCandidate(assemblyName, typeName);
            if (candidate != null
                && candidate.IsSerializable
                && !candidate.IsDefined(typeof(CompilerGeneratedAttribute), false))
            {
                throw new SerializationException(
                    "Blocked non-whitelisted serializable type during deserialization: " + typeName);
            }

            return null;
        }

        private static bool IsQTTabBarType(string assemblyName, string typeName)
        {
            if (!string.IsNullOrEmpty(typeName) &&
                typeName.StartsWith("QTTabBarLib.", StringComparison.Ordinal))
            {
                return true;
            }
            return !string.IsNullOrEmpty(assemblyName) &&
                   assemblyName.StartsWith("QTTabBar,", StringComparison.Ordinal);
        }

        private static bool IsAllowedFrameworkInfra(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return false;
            foreach (string allowed in AllowedFrameworkInfra)
            {
                if (string.Equals(typeName, allowed, StringComparison.Ordinal)) return true;
            }
            return false;
        }

        private static Type ResolveCandidate(string assemblyName, string typeName)
        {
            try
            {
                if (!string.IsNullOrEmpty(assemblyName))
                {
                    Type t = Type.GetType(typeName + ", " + assemblyName, false);
                    if (t != null) return t;
                }
                return Type.GetType(typeName, false);
            }
            catch
            {
                return null;
            }
        }
    }

}
