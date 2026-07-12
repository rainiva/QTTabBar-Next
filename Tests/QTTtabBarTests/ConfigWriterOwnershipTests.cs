using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigWriterOwnershipTests {
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

        private static IEnumerable<string> CSharpFiles(string subDir) {
            return Directory.GetFiles(Path.Combine(RepoRoot(), subDir), "*.cs", SearchOption.AllDirectories);
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

        private static string RelativePath(string fullPath) {
            return fullPath.Substring(RepoRoot().Length + 1).Replace('\\', '/');
        }

        [Test]
        public void LoadedConfig_Assignment_Only_Inside_ConfigManager() {
            var offenders = new List<string>();
            var assignment = new Regex(@"\bLoadedConfig\s*=[^=]");
            foreach(string file in CSharpFiles("QTTabBar")) {
                string rel = RelativePath(file);
                if(rel == "QTTabBar/ConfigManager.cs" || rel == "QTTabBar/ConfigManager.ReadConfig.cs") {
                    continue;
                }
                string body = RemoveComments(File.ReadAllText(file));
                if(assignment.IsMatch(body)) {
                    offenders.Add(rel);
                }
            }
            CollectionAssert.IsEmpty(offenders, "LoadedConfig assignment found outside ConfigManager");
        }

        [Test]
        public void Direct_Config_Category_Assignment_Only_Inside_ConfigManager() {
            var offenders = new List<string>();
            var assignment = new Regex(
                @"\bConfig\.(Desktop|Window|Tabs|Tweaks|Tips|Misc|Skin|BBar|Mouse|Keys|Plugin|Lang|Security)\." +
                @"[A-Za-z_][A-Za-z0-9_]*\s*=[^=]");
            foreach(string file in CSharpFiles("QTTabBar")) {
                string rel = RelativePath(file);
                if(rel == "QTTabBar/ConfigManager.cs") {
                    continue;
                }
                string body = RemoveComments(File.ReadAllText(file));
                if(assignment.IsMatch(body)) {
                    offenders.Add(rel);
                }
            }
            CollectionAssert.IsEmpty(offenders, "Direct Config category assignment found outside ConfigManager");
        }

        [Test]
        public void ConfigManager_CommitSnapshot_Is_Public_Canonical_Entry() {
            MethodInfo commit = typeof(ConfigManager).GetMethod("CommitSnapshot", BindingFlags.Public | BindingFlags.Static);
            Assert.IsNotNull(commit, "CommitSnapshot must be public static");
            Assert.AreEqual(typeof(void), commit.ReturnType);
        }

        [Test]
        public void Partial_Window_Methods_Use_MutateWindowAndCommit() {
            string content = File.ReadAllText(Path.Combine(RepoRoot(), "QTTabBar", "ConfigManager.cs"));
            AssertMethodUsesMutateWindowAndCommit(content, "void PersistBreakTabBar(");
            AssertMethodUsesMutateWindowAndCommit(content, "void PersistWindowAlpha(");
            AssertMethodUsesMutateWindowAndCommit(content, "void SetNoCapturePathsAndBroadcast(");
        }

        private static void AssertMethodUsesMutateWindowAndCommit(string content, string signature) {
            int methodIndex = content.IndexOf(signature, StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodIndex, 0, "Missing method " + signature);
            int brace = content.IndexOf('{', methodIndex);
            int depth = 0;
            for(int i = brace; i < content.Length; i++) {
                if(content[i] == '{') depth++;
                else if(content[i] == '}') {
                    depth--;
                    if(depth == 0) {
                        string body = content.Substring(brace, i - brace + 1);
                        StringAssert.Contains("MutateWindowAndCommit", body);
                        Assert.IsFalse(body.Contains("Config.Window."));
                        Assert.IsFalse(body.Contains("Registry.CurrentUser.CreateSubKey"));
                        return;
                    }
                }
            }
            Assert.Fail("Could not extract body for " + signature);
        }
    }
}
