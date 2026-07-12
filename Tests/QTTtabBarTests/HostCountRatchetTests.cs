using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace QTTtabBarTests {
    [TestFixture]
    public class HostCountRatchetTests {
        private const int RootCureHostBudget = 32;

        [Test]
        public void QTTabBarClass_Implements_At_Most_32_Host_Interfaces() {
            var hostInterfaces = new HashSet<string>(StringComparer.Ordinal);
            string root = Path.Combine(RepoRoot(), "QTTabBar");
            foreach(string file in Directory.GetFiles(root, "QTTabBarClass*.cs")) {
                string line = File.ReadAllLines(file)
                    .FirstOrDefault(text => text.Contains("partial class QTTabBarClass") && text.Contains("I") && text.Contains("Host"));
                if(line == null) {
                    continue;
                }
                foreach(Match match in Regex.Matches(line, @"I\w+Host")) {
                    hostInterfaces.Add(match.Value);
                }
            }
            Assert.LessOrEqual(hostInterfaces.Count, RootCureHostBudget,
                "QTTabBarClass Host interface count must meet root-cure R-7 budget: "
                + string.Join(", ", hostInterfaces.OrderBy(name => name)));
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
    }
}
