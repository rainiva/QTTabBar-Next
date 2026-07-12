using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigCommitConcurrencyTests {
        [SetUp]
        public void SetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
        }

        [Test]
        public void Clone_Transaction_Test_Uses_A_Recording_Writer() {
            string source = File.ReadAllText(Path.Combine(RepoRoot(), "Tests", "QTTtabBarTests",
                "ConfigCommitTransactionTests.cs"));
            int methodStart = source.IndexOf("public void CommitSnapshot_Clones_Candidate_Before_Publishing()",
                StringComparison.Ordinal);
            Assert.GreaterOrEqual(methodStart, 0);
            int nextMethod = source.IndexOf("[Test]", methodStart + 1, StringComparison.Ordinal);
            string method = nextMethod < 0 ? source.Substring(methodStart) : source.Substring(methodStart, nextMethod - methodStart);

            StringAssert.Contains("ConfigTestScope.WithRecordingWriter()", method,
                "This regression test must never invoke the real RegistryConfigWriter.");
        }

        [Test]
        public void MutateAndCommit_Preserves_Both_Concurrent_Mutations() {
            var writer = new FirstWriteBarrierWriter();
            using(ConfigTestScope.WithWriter(writer)) {
                Task first = Task.Run(() => ConfigManager.MutateAndCommit(config => {
                    config.desktop.FirstItem = 101;
                }, ConfigCommitScope.All, false));

                Assert.IsTrue(writer.FirstWriteEntered.Wait(TimeSpan.FromSeconds(5)),
                    "The first commit did not reach the controlled writer.");

                Task second = Task.Run(() => ConfigManager.MutateAndCommit(config => {
                    writer.SecondMutationEntered.Set();
                    config.desktop.SecondItem = 202;
                }, ConfigCommitScope.All, false));

                Assert.IsTrue(Task.WaitAll(new[] { first, second }, TimeSpan.FromSeconds(10)),
                    "Concurrent commits did not complete.");
                Assert.AreEqual(101, Config.Desktop.FirstItem);
                Assert.AreEqual(202, Config.Desktop.SecondItem);
            }
        }

        [Test]
        public void Partial_WindowAlpha_Commit_Preserves_Concurrent_Full_Mutation() {
            var writer = new FirstWriteBarrierWriter();
            using(ConfigTestScope.WithWriter(writer)) {
                Task apply = Task.Run(() => ConfigManager.MutateAndCommit(config =>
                    config.desktop.FirstItem = 303, ConfigCommitScope.All, false));

                Assert.IsTrue(writer.FirstWriteEntered.Wait(TimeSpan.FromSeconds(5)));
                Task local = Task.Run(() => ConfigManager.PersistWindowAlpha(0x33));
                writer.SecondMutationEntered.Set();

                Assert.IsTrue(Task.WaitAll(new[] { apply, local }, TimeSpan.FromSeconds(10)));
                Assert.AreEqual(303, Config.Desktop.FirstItem);
                Assert.AreEqual((byte)0x33, Config.Window.WindowAlpha);
            }
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
