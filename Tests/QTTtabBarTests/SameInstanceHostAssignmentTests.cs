using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class SameInstanceHostAssignmentTests {
        private static readonly Regex DuplicateHostAssignment = new Regex(
            @"=\s*(\w+)\s*;\s*\n\s*_\w+\s*=\s*\(\s*I\w+Host\s*\)\s*\1\s*;",
            RegexOptions.Compiled);

        [Test]
        public void Navigation_Controller_Ctors_Do_Not_Assign_Same_Parameter_To_Multiple_Host_Fields() {
            string navigationDir = Path.Combine(FindRepoRoot(), "QTTabBar", "Navigation");
            var violations = new List<string>();

            foreach(string file in Directory.GetFiles(navigationDir, "*.cs", SearchOption.AllDirectories)) {
                string source = File.ReadAllText(file);
                if(!source.Contains("Controller")) {
                    continue;
                }
                foreach(Match match in DuplicateHostAssignment.Matches(source)) {
                    violations.Add(Path.GetFileName(file) + " assigns host parameter '" + match.Groups[1].Value + "' twice");
                }
            }

            Assert.IsEmpty(violations,
                "Wave 18 Q4: controller ctors must not assign the same host expression to multiple fields:\n"
                + string.Join("\n", violations));
        }

        private static string FindRepoRoot() {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while(dir != null) {
                if(File.Exists(Path.Combine(dir.FullName, "QTTabBar Rebirth.sln"))) {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            throw new InvalidOperationException("Repository root not found.");
        }
    }
}
