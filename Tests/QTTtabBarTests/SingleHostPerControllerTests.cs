using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class SingleHostPerControllerTests {
        private static readonly HashSet<string> ExcludedContextTypes = new HashSet<string>(StringComparer.Ordinal) {
            "IMenuContext",
            "IExplorerContext",
            "ITabContext",
        };

        private static readonly HashSet<string> MultiHostWhitelist = new HashSet<string>(StringComparer.Ordinal) {
        };

        private static readonly Regex ConstructorPattern = new Regex(
            @"(?:public|internal)\s+\w+Controller\s*\((?<params>[^)]*)\)",
            RegexOptions.Compiled);

        [Test]
        public void Navigation_And_Shell_Controllers_Accept_At_Most_One_IHost_Parameter() {
            string root = Path.Combine(FindRepoRoot(), "QTTabBar");
            var violations = new List<string>();

            foreach(string file in Directory.GetFiles(root, "*Controller*.cs", SearchOption.AllDirectories)) {
                if(file.Contains("\\obj\\") || file.Contains("\\bin\\") || file.Contains("\\Tests\\")) {
                    continue;
                }
                string fileName = Path.GetFileNameWithoutExtension(file);
                if(MultiHostWhitelist.Contains(fileName)) {
                    continue;
                }

                string source = File.ReadAllText(file);
                foreach(Match match in ConstructorPattern.Matches(source)) {
                    string[] parameters = match.Groups["params"].Value
                        .Split(',')
                        .Select(parameter => parameter.Trim())
                        .Where(parameter => parameter.Length > 0)
                        .ToArray();
                    int hostCount = parameters.Count(IsRoleHostParameter);
                    if(hostCount > 1) {
                        violations.Add(fileName + ".ctor("
                            + string.Join(", ", parameters.Select(ExtractTypeName))
                            + ")");
                    }
                }
            }

            Assert.IsEmpty(violations,
                "Wave 18 Q4: each controller ctor may accept at most one I*Host (excluding context types). Violations:\n"
                + string.Join("\n", violations));
        }

        private static bool IsRoleHostParameter(string parameter) {
            string typeName = ExtractTypeName(parameter);
            return typeName.EndsWith("Host", StringComparison.Ordinal)
                && !ExcludedContextTypes.Contains(typeName);
        }

        private static string ExtractTypeName(string parameter) {
            string[] parts = parameter.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length == 0 ? parameter : parts[0].Trim();
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
