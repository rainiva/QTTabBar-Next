using System;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    [TestFixture]
    public class IpcCommandMessageTests {
        [Test]
        public void EncodeSelectTab_RoundtripsHandleAndIndex() {
            IntPtr handle = new IntPtr(0x12345678);
            byte[] encoded = IpcCommandMessage.EncodeSelectTab(handle, 3);

            IpcCommand command;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(encoded, out command, out payload));
            Assert.AreEqual(IpcCommand.SelectTab, command);

            IntPtr decodedHandle;
            int decodedIndex;
            Assert.IsTrue(IpcCommandMessage.TryDecodeSelectTab(payload, out decodedHandle, out decodedIndex));
            Assert.AreEqual(handle, decodedHandle);
            Assert.AreEqual(3, decodedIndex);
        }

        [Test]
        public void EncodeOpenOptions_UsesTypedHeader() {
            byte[] encoded = IpcCommandMessage.EncodeOpenOptions();

            IpcCommand command;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(encoded, out command, out payload));
            Assert.AreEqual(IpcCommand.OpenOptions, command);
            Assert.AreEqual(0, payload.Length);
        }

        [Test]
        public void TryParse_RejectsLegacyBinaryFormatterPayload() {
            IpcCommand command;
            byte[] payload;
            Assert.IsFalse(IpcCommandMessage.TryParse(new byte[] { 0x00, 0x01, 0x00, 0x00, 0x00, 0xFF }, out command, out payload));
        }
    }
}
