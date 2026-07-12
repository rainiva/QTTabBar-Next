using System;
using System.Threading.Tasks;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class ConfigPartialCommitTests {
        [SetUp]
        public void SetUp() {
            ConfigManager.ReplaceLoadedConfigForTests(new Config());
        }

        [Test]
        public void Partial_And_Full_Commit_Preserve_Both_Changes() {
            var writer = new FirstWriteBarrierWriter();
            var windowWriter = new FirstWriteBarrierWindowWriter();
            using(ConfigTestScope.WithWriters(writer, windowWriter)) {
                Task apply = Task.Run(() => ConfigManager.MutateAndCommit(config =>
                    config.desktop.FirstItem = 101, ConfigCommitScope.All, false));

                Assert.IsTrue(writer.FirstWriteEntered.Wait(TimeSpan.FromSeconds(5)));
                Task local = Task.Run(() => ConfigManager.PersistBreakTabBar(true));
                writer.SecondMutationEntered.Set();

                Assert.IsTrue(Task.WaitAll(new[] { apply, local }, TimeSpan.FromSeconds(10)));
                Assert.AreEqual(101, Config.Desktop.FirstItem);
                Assert.IsTrue(Config.Window.BreakTabBar);
            }
        }

        [Test]
        public void Partial_WindowAlpha_And_Full_Commit_Preserve_Both_Changes() {
            var writer = new FirstWriteBarrierWriter();
            var windowWriter = new RecordingWindowWriter();
            using(ConfigTestScope.WithWriters(writer, windowWriter)) {
                ConfigManager.MutateAndCommit(config => config.desktop.FirstItem = 55, ConfigCommitScope.All, false);
                ConfigManager.PersistWindowAlpha(0x55);
                Assert.AreEqual(55, Config.Desktop.FirstItem);
                Assert.AreEqual((byte)0x55, Config.Window.WindowAlpha);
            }
        }

        [Test]
        public void Partial_NoCaptureAt_And_Full_Commit_Preserve_Both_Changes() {
            var writer = new FirstWriteBarrierWriter();
            var windowWriter = new RecordingWindowWriter();
            using(ConfigTestScope.WithWriters(writer, windowWriter)) {
                ConfigManager.MutateAndCommit(config => config.desktop.SecondItem = 88, ConfigCommitScope.All, false);
                ConfigManager.SetNoCapturePathsAndBroadcast(new[] { @"C:\NoCaptureA" });
                Assert.AreEqual(88, Config.Desktop.SecondItem);
                Assert.That(Config.Window.NoCaptureAt, Does.Contain(@"C:\NoCaptureA"));
                Assert.That(SessionState.NoCapturePathsList, Does.Contain(@"C:\NoCaptureA"));
            }
        }

        [Test]
        public void WindowWriter_Only_Writes_Requested_Field() {
            var windowWriter = new RecordingWindowWriter();
            using(ConfigTestScope.WithWindowWriter(windowWriter)) {
                ConfigManager.PersistBreakTabBar(true);
                Assert.AreEqual(1, windowWriter.FieldsWritten.Count);
                Assert.AreEqual(ConfigWindowField.BreakTabBar, windowWriter.FieldsWritten[0]);
                Assert.IsTrue(windowWriter.LastWindow.BreakTabBar);
            }
        }
    }
}
