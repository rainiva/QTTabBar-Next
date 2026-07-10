using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class CanonicalEntryAndComIdentityTests {
        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private static string RelativePath(string fullPath) {
            return fullPath.Substring(RepoRoot().Length + 1).Replace('\\', '/');
        }

        private static bool IsBuildArtifact(string path) {
            return path.Contains("\\obj\\") || path.Contains("\\bin\\") || path.Contains("/obj/") || path.Contains("/bin/");
        }

        [Test]
        public void Production_Has_No_Options_Preview_Entry() {
            string[] forbidden = {
                "ShowStandalonePreview",
                "OptionsFluentPoC",
                "FluentOptionsPoC",
                "QTCommandBar",
                "FluentPoCTestHost"
            };
            var offenders = new List<string>();

            string csproj = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "QTTabBar.csproj"));
            foreach(string f in forbidden) {
                if(csproj.Contains(f)) offenders.Add("QTTabBar.csproj: " + f);
            }

            foreach(string file in Directory.GetFiles(Path.Combine(RepoRoot(), "QTTabBar"), "*.cs", SearchOption.AllDirectories)) {
                if(IsBuildArtifact(file)) continue;
                string content = File.ReadAllText(file);
                foreach(string f in forbidden) {
                    if(content.Contains(f)) offenders.Add(RelativePath(file) + ": " + f);
                }
            }

            CollectionAssert.IsEmpty(offenders, "Retired options preview / COM entry points still present in production");
        }

        [Test]
        public void Retired_QTCommandBar_Source_Is_Removed() {
            Assert.IsFalse(File.Exists(Path.Combine(RepoRoot(), "QTTabBar", "QTCommandBar.cs")),
                "QTCommandBar.cs should not exist");
        }

        [Test]
        public void Retired_FluentPoC_Host_Is_Removed() {
            Assert.IsFalse(Directory.Exists(Path.Combine(RepoRoot(), "Tools", "FluentPoCTestHost")),
                "Tools/FluentPoCTestHost directory should not exist");
        }

        [Test]
        public void Compiled_COM_Coclass_Guids_Are_Unique() {
            var duplicates = ComGuidScanner.FindDuplicateCompiledCoclassGuids();
            CollectionAssert.IsEmpty(duplicates, "Duplicate [Guid] values found in coclass definitions");
        }
    }

    internal static class ComGuidScanner {
        public static IEnumerable<string> FindDuplicateCompiledCoclassGuids() {
            string root = typeof(CanonicalEntryAndComIdentityTests).Assembly
                .GetTypes()
                .First(t => t.Namespace == "QTTtabBarTests")
                .Assembly
                .Location;
            // Not used: use repo source scan instead.
            return FindDuplicateCoclassGuidsFromSource();
        }

        private static string RepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }

        private static bool IsBuildArtifact(string path) {
            return path.Contains("\\obj\\") || path.Contains("\\bin\\") || path.Contains("/obj/") || path.Contains("/bin/");
        }

        private static IEnumerable<string> FindDuplicateCoclassGuidsFromSource() {
            var guidPattern = new Regex(@"\[\s*Guid\s*\(\s*""([^""]+)""\s*\)\s*\]");
            var comClassPattern = new Regex(@"\[\s*ComVisible\s*\(\s*true\s*\)\s*\]|\[\s*ClassInterface\s*\(");
            var guids = new List<string>();
            foreach(string file in Directory.GetFiles(Path.Combine(RepoRoot(), "QTTabBar"), "*.cs", SearchOption.AllDirectories)) {
                if(IsBuildArtifact(file)) continue;
                string content = File.ReadAllText(file);
                if(!comClassPattern.IsMatch(content)) continue;
                foreach(Match m in guidPattern.Matches(content)) {
                    guids.Add(m.Groups[1].Value);
                }
            }
            return guids.GroupBy(g => g)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
        }
    }
}
