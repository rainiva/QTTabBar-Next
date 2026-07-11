using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigCommitConcurrencyTests {
        private sealed class FirstWriteBarrierWriter : IConfigWriter {
            private int _writeCount;

            public ManualResetEventSlim FirstWriteEntered { get; } = new ManualResetEventSlim(false);
            public ManualResetEventSlim SecondMutationEntered { get; } = new ManualResetEventSlim(false);

            public void Write(Config config, bool desktopOnly) {
                if(Interlocked.Increment(ref _writeCount) == 1) {
                    FirstWriteEntered.Set();
                    SecondMutationEntered.Wait(TimeSpan.FromSeconds(2));
                }
            }
        }

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
