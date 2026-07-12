using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using static QTTtabBarTests.StructuralGovernanceBaselineTests;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigBypassGuardTests {
        private static readonly HashSet<string> AllowedRegistryConfigWriterFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            "RegistryConfigWriter.cs",
            "RegistryConfigWindowWriter.cs",
        };

        [Test]
        public void Production_Code_Does_Not_Call_WriteConfig_Outside_ConfigManager() {
            var violations = new List<string>();
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fileName = Path.GetFileName(relativePath);
                if(fileName.StartsWith("ConfigManager", StringComparison.OrdinalIgnoreCase)) {
                    continue;
                }
                string source = RemoveComments(File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", relativePath)));
                if(source.Contains("WriteConfig(")) {
                    violations.Add(relativePath);
                }
            }
            CollectionAssert.IsEmpty(violations,
                "WriteConfig must remain internal to ConfigManager/RegistryConfigWriter: "
                + string.Join(", ", violations));
        }

        [Test]
        public void Production_Code_Does_Not_Write_Config_Registry_Outside_Writers() {
            var violations = new List<string>();
            foreach(string relativePath in SourceMetrics.SourceFiles()) {
                string fileName = Path.GetFileName(relativePath);
                if(AllowedRegistryConfigWriterFiles.Contains(fileName) || fileName == "ConfigMetadataCache.cs") {
                    continue;
                }
                string source = RemoveComments(File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", relativePath)));
                if(source.Contains("RegConst.Config") && Regex.IsMatch(source, @"\bkey\.SetValue\s*\(")) {
                    violations.Add(relativePath);
                }
            }
            CollectionAssert.IsEmpty(violations,
                "Config registry writes must go through RegistryConfigWriter: "
                + string.Join(", ", violations));
        }

        [Test]
        public void ConfigManager_Exposes_Canonical_Commit_Entries() {
            StringAssert.Contains("CommitSnapshot", ReadConfigManagerSource());
            StringAssert.Contains("MutateAndCommit", ReadConfigManagerSource());
            StringAssert.Contains("MutateWindowAndCommit", ReadConfigManagerSource());
        }

        private static string ReadConfigManagerSource() {
            return File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "ConfigManager.cs"));
        }

        private static string RemoveComments(string source) {
            var sb = new StringBuilder(source.Length);
            bool inLineComment = false;
            bool inBlockComment = false;
            bool inString = false;
            for(int i = 0; i < source.Length; i++) {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';
                if(inLineComment) {
                    if(c == '\n') {
                        inLineComment = false;
                        sb.Append(c);
                    }
                }
                else if(inBlockComment) {
                    if(c == '*' && next == '/') {
                        inBlockComment = false;
                        i++;
                    }
                }
                else if(inString) {
                    sb.Append(c);
                    if(c == '\\' && next != '\0') {
                        sb.Append(next);
                        i++;
                    }
                    else if(c == '"') {
                        inString = false;
                    }
                }
                else {
                    if(c == '/' && next == '/') {
                        inLineComment = true;
                        i++;
                    }
                    else if(c == '/' && next == '*') {
                        inBlockComment = true;
                        i++;
                    }
                    else {
                        if(c == '"') inString = true;
                        sb.Append(c);
                    }
                }
            }
            return sb.ToString();
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
