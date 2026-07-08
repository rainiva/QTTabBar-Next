using System;
using System.IO;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// 任务 #15:插件与 native DLL 加载安全加固。
    /// 默认保守策略:校验失败仅告警仍继续加载; Config.Security 可 opt-in 阻断。
    ///
    /// 分层测试豁免说明:
    /// HookLibManager / PluginManager 的实际加载走的是 Win32 LoadLibrary 与
    /// Assembly.Load,依赖真实 Explorer 宿主进程、已注册的安装目录与原生 DLL,
    /// 无法在纯 NUnit 单元测试环境里端到端复现。因此这里对底层安全逻辑采用
    /// **针对性单元测试**,把两条可确定化的安全决策抽取为纯函数后断言:
    ///   1) 受信任路径解析:给定受信任目录与候选路径,优先解析出受信任目录中的 DLL;
    ///   2) 来源/签名校验:给定未签名/不在受信任目录的程序集路径,校验返回"不受信任",
    ///      但加载流程不抛出、不中断(通过返回值 ShouldContinueLoading 验证"告警但继续")。
    /// </summary>
    [TestFixture]
    public class PluginNativeLoadSecurityTests {

        private string tempRoot;

        [SetUp]
        public void SetUp() {
            if(ConfigManager.LoadedConfig == null) {
                ConfigManager.Initialize();
            }
            tempRoot = Path.Combine(Path.GetTempPath(),
                "QTTabBarSecTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
        }

        [TearDown]
        public void TearDown() {
            try {
                if(tempRoot != null && Directory.Exists(tempRoot)) {
                    Directory.Delete(tempRoot, true);
                }
            }
            catch {
                // 清理失败不影响测试结论
            }
        }

        // ---------------- HookLibManager: 受信任路径优先解析 ----------------

        [Test]
        public void ResolveTrustedLibraryPath_PrefersTrustedDir_WhenBothContainFile() {
            // 受信任(安装)目录与旧的用户可写目录都存在同名 DLL 时,必须解析到受信任目录,
            // 以降低 DLL 劫持面。
            const string fileName = "ExplorerBgTool.dll";
            string trustedDir = Path.Combine(tempRoot, "trusted");
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(legacyDir);
            File.WriteAllText(Path.Combine(trustedDir, fileName), "trusted");
            File.WriteAllText(Path.Combine(legacyDir, fileName), "legacy");

            string resolved = HookLibManager.ResolveTrustedLibraryPath(fileName, trustedDir, legacyDir);

            Assert.AreEqual(Path.Combine(trustedDir, fileName), resolved,
                "两个目录都含同名 DLL 时应优先解析受信任安装目录的路径。");
        }

        [Test]
        public void ResolveTrustedLibraryPath_FallsBackToLegacy_WhenTrustedMissing() {
            // 受信任目录不含该 DLL、旧目录含有时,回退到旧路径(实现内部应记告警)。
            const string fileName = "ExplorerBgTool.dll";
            string trustedDir = Path.Combine(tempRoot, "trusted");
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(legacyDir);
            File.WriteAllText(Path.Combine(legacyDir, fileName), "legacy");

            string resolved = HookLibManager.ResolveTrustedLibraryPath(fileName, trustedDir, legacyDir);

            Assert.AreEqual(Path.Combine(legacyDir, fileName), resolved,
                "受信任目录缺失该 DLL 时应回退到旧路径。");
        }

        [Test]
        public void ResolveTrustedLibraryPath_ReturnsLegacyCandidate_WhenNeitherExists() {
            // 两处都没有该 DLL 时,返回旧路径候选,保持既有"文件不存在则降级"处理不变。
            const string fileName = "ExplorerBgTool.dll";
            string trustedDir = Path.Combine(tempRoot, "trusted");
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(legacyDir);

            string resolved = HookLibManager.ResolveTrustedLibraryPath(fileName, trustedDir, legacyDir);

            Assert.AreEqual(Path.Combine(legacyDir, fileName), resolved,
                "两处都不存在时应返回旧路径候选以复用既有降级逻辑。");
        }

        // ---------------- PluginManager: 来源/签名校验(告警不阻断) ----------------

        [Test]
        public void IsWithinTrustedPluginDirectory_ReturnsTrue_ForPathInsideTrustedDir() {
            string trustedDir = Path.Combine(tempRoot, "plugins");
            Directory.CreateDirectory(trustedDir);
            string pluginPath = Path.Combine(trustedDir, "Sample.dll");
            File.WriteAllText(pluginPath, "x");

            bool inside = PluginManager.IsWithinTrustedPluginDirectory(
                pluginPath, new[] { trustedDir });

            Assert.IsTrue(inside, "位于受信任插件目录内的程序集应被判定为受信任目录。");
        }

        [Test]
        public void IsWithinTrustedPluginDirectory_ReturnsFalse_ForPathOutsideTrustedDir() {
            string trustedDir = Path.Combine(tempRoot, "plugins");
            string otherDir = Path.Combine(tempRoot, "downloads");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(otherDir);
            string pluginPath = Path.Combine(otherDir, "Evil.dll");
            File.WriteAllText(pluginPath, "x");

            bool inside = PluginManager.IsWithinTrustedPluginDirectory(
                pluginPath, new[] { trustedDir });

            Assert.IsFalse(inside, "受信任目录之外的程序集不应被判定为受信任目录。");
        }

        [Test]
        public void HasTrustedSignature_ReturnsFalse_ForUnsignedGarbageFile_WithoutThrowing() {
            // 非托管/损坏的 dll 文件既无 Authenticode 也无强名称,校验必须返回 false 且不抛异常。
            string bogus = Path.Combine(tempRoot, "unsigned.dll");
            File.WriteAllText(bogus, "this is not a real assembly");

            bool signed = false;
            Assert.DoesNotThrow(() => {
                signed = PluginManager.HasTrustedSignature(bogus);
            }, "对无效程序集的签名校验不应抛出异常。");
            Assert.IsFalse(signed, "未签名/无效文件应返回 false。");
        }

        [Test]
        public void ValidatePluginSource_UnsignedUntrusted_ReturnsNotTrusted_ButContinues() {
            // 核心保守策略验证:未签名且不在受信任目录 → 校验返回"不受信任",
            // 但加载流程必须继续(ShouldContinueLoading == true),且不抛异常。
            string trustedDir = Path.Combine(tempRoot, "plugins");
            string otherDir = Path.Combine(tempRoot, "downloads");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(otherDir);
            string bogus = Path.Combine(otherDir, "unsigned.dll");
            File.WriteAllText(bogus, "not a real assembly");

            PluginSourceValidation result = default(PluginSourceValidation);
            Assert.DoesNotThrow(() => {
                result = PluginManager.ValidatePluginSource(bogus, new[] { trustedDir });
            }, "来源校验流程不应因未签名程序集而抛出。");

            Assert.IsFalse(result.IsTrusted,
                "未签名且不在受信任目录的插件应被判定为不受信任。");
            Assert.IsTrue(result.ShouldContinueLoading,
                "保守策略:校验失败仅告警,加载流程必须继续。");
        }

        [Test]
        public void IsWithinTrustedPluginDirectory_ReturnsFalse_ForProgramDataPath() {
            string trustedDir = Path.Combine(tempRoot, "install");
            string programDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "QTTabBar");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(programDataDir);
            string pluginPath = Path.Combine(programDataDir, "Legacy.dll");
            File.WriteAllText(pluginPath, "x");

            bool insideTrusted = PluginManager.IsWithinTrustedPluginDirectory(
                pluginPath, PluginManager.GetTrustedPluginDirectories());
            bool insideLegacy = PluginManager.IsWithinTrustedPluginDirectory(
                pluginPath, PluginManager.GetLegacyPluginDirectories());

            Assert.IsFalse(insideTrusted, "ProgramData 路径 alone 不应算受信任安装目录。");
            Assert.IsTrue(insideLegacy, "ProgramData 应归类为 legacy 目录。");
        }

        [Test]
        public void ValidatePluginSource_Blocks_WhenBlockUntrustedPluginsEnabled() {
            string trustedDir = Path.Combine(tempRoot, "plugins");
            string otherDir = Path.Combine(tempRoot, "downloads");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(otherDir);
            string bogus = Path.Combine(otherDir, "unsigned.dll");
            File.WriteAllText(bogus, "not a real assembly");

            bool prior = Config.Security.BlockUntrustedPlugins;
            try {
                ConfigManager.LoadedConfig.security.BlockUntrustedPlugins = true;
                PluginSourceValidation result = PluginManager.ValidatePluginSource(
                    bogus, new[] { trustedDir });
                Assert.IsFalse(result.IsTrusted);
                Assert.IsFalse(result.ShouldContinueLoading,
                    "BlockUntrustedPlugins=true 时必须阻断不受信任插件。");
            }
            finally {
                ConfigManager.LoadedConfig.security.BlockUntrustedPlugins = prior;
            }
        }

        [Test]
        public void ResolveTrustedLibraryPath_BlocksLegacy_WhenConfigured() {
            const string fileName = "ExplorerBgTool.dll";
            string trustedDir = Path.Combine(tempRoot, "trusted");
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(legacyDir);
            File.WriteAllText(Path.Combine(legacyDir, fileName), "legacy");

            string resolved = HookLibManager.ResolveTrustedLibraryPath(
                fileName, trustedDir, legacyDir, blockLegacyPath: true);

            Assert.AreEqual(Path.Combine(trustedDir, fileName), resolved,
                "BlockLegacyHookDllPath 时不应回退到 legacy 路径。");
        }

        [Test]
        public void ResolveTrustedLibraryPath_BlockLegacy_ReturnsNull_WhenNeitherExistsAndNoTrustedDir() {
            const string fileName = "ExplorerBgTool.dll";
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(legacyDir);

            string resolved = HookLibManager.ResolveTrustedLibraryPath(
                fileName, null, legacyDir, blockLegacyPath: true);

            Assert.IsNull(resolved,
                "blockLegacyPath 且无受信任目录时不应返回 legacy 候选路径。");
        }

        [Test]
        public void ResolveTrustedLibraryPath_BlockLegacy_ReturnsTrustedCandidate_WhenNeitherExists() {
            const string fileName = "ExplorerBgTool.dll";
            string trustedDir = Path.Combine(tempRoot, "trusted");
            string legacyDir = Path.Combine(tempRoot, "legacy");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(legacyDir);

            string resolved = HookLibManager.ResolveTrustedLibraryPath(
                fileName, trustedDir, legacyDir, blockLegacyPath: true);

            Assert.AreEqual(Path.Combine(trustedDir, fileName), resolved,
                "blockLegacyPath 且两处均不存在时应返回受信任路径候选。");
        }

        [Test]
        public void HasTrustedSignature_ReturnsTrue_ForSameStrongNameAssembly() {
            string assemblyPath = typeof(PluginManager).Assembly.Location;
            Assert.IsFalse(string.IsNullOrEmpty(assemblyPath));
            Assert.IsTrue(File.Exists(assemblyPath));

            bool signed = PluginManager.HasTrustedSignature(assemblyPath);

            Assert.IsTrue(signed,
                "与主程序集相同强名称的程序集应通过 HasTrustedSignature。");
        }

        [Test]
        public void LoadAssembly_ReturnsNull_WhenBlockUntrustedPluginsEnabled() {
            string trustedDir = Path.Combine(tempRoot, "plugins");
            string otherDir = Path.Combine(tempRoot, "downloads");
            Directory.CreateDirectory(trustedDir);
            Directory.CreateDirectory(otherDir);
            string bogus = Path.Combine(otherDir, "unsigned.dll");
            File.WriteAllText(bogus, "not a real assembly");

            bool prior = Config.Security.BlockUntrustedPlugins;
            try {
                ConfigManager.LoadedConfig.security.BlockUntrustedPlugins = true;
                PluginAssembly result = PluginManager.LoadAssembly(bogus);
                Assert.IsNull(result,
                    "BlockUntrustedPlugins=true 时 LoadAssembly 必须返回 null。");
            }
            finally {
                ConfigManager.LoadedConfig.security.BlockUntrustedPlugins = prior;
            }
        }
    }
}
