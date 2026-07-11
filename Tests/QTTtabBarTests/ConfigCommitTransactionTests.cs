using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigCommitTransactionTests {
        [SetUp]
        public void SetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
        }

        [Test]
        public void CommitSnapshot_Clones_Candidate_Before_Publishing() {
            using(ConfigTestScope.WithRecordingWriter()) {
                var candidate = new Config();
                candidate.tabs.ActivateNewTab = false;
                ConfigManager.CommitSnapshot(candidate, ConfigCommitScope.All, false);
                candidate.tabs.ActivateNewTab = true;
                Assert.IsFalse(Config.Tabs.ActivateNewTab);
            }
        }

        [Test]
        public void CommitSnapshot_Writes_Exactly_Once() {
            var writer = new RecordingConfigWriter();
            using(ConfigTestScope.WithWriter(writer)) {
                ConfigManager.CommitSnapshot(new Config(), ConfigCommitScope.All, false);
                Assert.AreEqual(1, writer.WriteCount);
                Assert.IsFalse(writer.DesktopOnly);
            }
        }
    }
}
