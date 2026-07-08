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



        /// <summary>

        /// Core-framework ISerializable types that must never round-trip via BinaryFormatter

        /// (gadget chains). Collections such as Dictionary/List are intentionally allowed.

        /// </summary>

        private static bool IsBlockedCoreFrameworkISerializable(string typeName, Type candidate)

        {

            if (candidate == null || !typeof(ISerializable).IsAssignableFrom(candidate))

                return false;



            if (typeof(Exception).IsAssignableFrom(candidate))

                return true;



            if (string.IsNullOrEmpty(typeName))

                return false;



            if (typeName.StartsWith("System.Security.Principal.", StringComparison.Ordinal))

                return true;

            if (typeName.StartsWith("System.Security.Claims.", StringComparison.Ordinal))

                return true;



            return false;

        }



        public override Type BindToType(string assemblyName, string typeName)

        {

            String exeAssembly = Assembly.GetExecutingAssembly().FullName;



            // Legacy FMDServiceProxy namespace migration (narrow whitelist).

            if (typeName != null && typeName.StartsWith("QTTabBarLib.Entities.", StringComparison.Ordinal))

            {

                string migrated = typeName.Replace("Entities", "FMDService");

                Type migratedType = Type.GetType(

                    "FMDServiceProxy." + migrated + ", FMDServiceProxy, Version=1.6.0.0, Culture=neutral, PublicKeyToken=null");

                if (migratedType != null) {

                    return migratedType;

                }

                QTUtility2.MakeErrorLog(null,

                    "PreMergeToMergedDeserializationBinder: FMDServiceProxy migration failed for " + typeName);

            }



            if (IsExplicitlyAllowedQTTabBarType(typeName))

            {

                return Type.GetType(String.Format("{0}, {1}", typeName, exeAssembly));

            }



            // Known-safe framework serialization holders: let BinaryFormatter bind them.

            if (IsAllowedFrameworkInfra(typeName))

            {

                return null;

            }



            Type candidate = ResolveCandidate(assemblyName, typeName);

            if (candidate != null

                && candidate.IsSerializable

                && !candidate.IsDefined(typeof(CompilerGeneratedAttribute), false))

            {

                if (IsQTTabBarAssemblyType(assemblyName, typeName))

                {

                    QTUtility2.MakeErrorLog("PreMergeToMergedDeserializationBinder rejected non-whitelisted type: [" + assemblyName + "] " + typeName);

                    throw new SerializationException(

                        "Blocked non-whitelisted serializable type during deserialization: " + typeName);

                }



                // Core framework assemblies: block known ISerializable gadget types while still

                // allowing collections (Dictionary/List) and System.Drawing / WinForms types.

                if (IsCoreFrameworkAssembly(assemblyName)

                    && IsBlockedCoreFrameworkISerializable(typeName, candidate))

                {

                    throw new SerializationException(

                        "Blocked non-whitelisted ISerializable framework type during deserialization: " + typeName);

                }



                if (!IsTrustedFrameworkAssembly(assemblyName))

                {

                    throw new SerializationException(

                        "Blocked non-whitelisted serializable type from external assembly during deserialization: " + typeName);

                }

            }



            // Remaining framework [Serializable] types use default binding.

            return null;

        }



        private static bool IsCoreFrameworkAssembly(string assemblyName)

        {

            if (string.IsNullOrEmpty(assemblyName)) return true;



            string simpleName = assemblyName.Split(',')[0].Trim();

            return string.Equals(simpleName, "mscorlib", StringComparison.OrdinalIgnoreCase)

                || string.Equals(simpleName, "System", StringComparison.OrdinalIgnoreCase)

                || string.Equals(simpleName, "System.Core", StringComparison.OrdinalIgnoreCase);

        }



        private static bool IsTrustedFrameworkAssembly(string assemblyName)

        {

            if (string.IsNullOrEmpty(assemblyName)) return true;



            string simpleName = assemblyName.Split(',')[0].Trim();

            return IsCoreFrameworkAssembly(assemblyName)

                || string.Equals(simpleName, "System.Drawing", StringComparison.OrdinalIgnoreCase)

                || string.Equals(simpleName, "System.Windows.Forms", StringComparison.OrdinalIgnoreCase);

        }



        private static bool IsExplicitlyAllowedQTTabBarType(string typeName)

        {

            if (string.IsNullOrEmpty(typeName)) return false;



            if (string.Equals(typeName, "QTTabBarLib.SerializeDelegate", StringComparison.Ordinal)) return true;

            if (typeName.StartsWith("QTTabBarLib.SerializeDelegate+", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.XmlSerializableFont", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.Config", StringComparison.Ordinal)) return true;

            if (typeName.StartsWith("QTTabBarLib.Config+", StringComparison.Ordinal)) return true;



            // Top-level config enums referenced by Config nested types (DeepClone path).

            if (string.Equals(typeName, "QTTabBarLib.TabPos", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.StretchMode", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.MouseTarget", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.MouseChord", StringComparison.Ordinal)) return true;

            if (string.Equals(typeName, "QTTabBarLib.BindAction", StringComparison.Ordinal)) return true;



            return false;

        }



        private static bool IsQTTabBarAssemblyType(string assemblyName, string typeName)

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


