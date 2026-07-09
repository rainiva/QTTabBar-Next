using System;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// 契约冻结护栏测试：锁定 IpcCommandMessage 的线格式（wire format）稳定性。
    /// 头部布局：'Q''T''I''P' | version(1) | command(1) | payload...
    /// 这些断言补充（而非重复）现有 IpcCommandMessageTests，聚焦字节格式稳定性、
    /// 各命令枚举值、往返与边界条件。应对当前代码全部通过（GREEN）。
    /// </summary>
    [TestFixture]
    public class IpcContractRoundTripTests {

        private const int HeaderLength = 6;

        private static void AssertHeader(byte[] buffer, IpcCommand expected) {
            Assert.GreaterOrEqual(buffer.Length, HeaderLength, "buffer must contain full header");
            Assert.AreEqual((byte)'Q', buffer[0], "magic[0] must be 'Q'");
            Assert.AreEqual((byte)'T', buffer[1], "magic[1] must be 'T'");
            Assert.AreEqual((byte)'I', buffer[2], "magic[2] must be 'I'");
            Assert.AreEqual((byte)'P', buffer[3], "magic[3] must be 'P'");
            Assert.AreEqual((byte)1, buffer[4], "protocol version must be 1");
            Assert.AreEqual((byte)expected, buffer[5], "command byte must match");
        }

        #region Protocol constants / enum values are frozen

        [Test]
        public void ProtocolVersion_Is_One() {
            Assert.AreEqual((byte)1, IpcCommandMessage.ProtocolVersion, "ProtocolVersion must stay 1");
        }

        [Test]
        public void IpcCommand_EnumValues_Are_Frozen() {
            Assert.AreEqual(1, (byte)IpcCommand.SelectTab);
            Assert.AreEqual(2, (byte)IpcCommand.OpenOptions);
            Assert.AreEqual(3, (byte)IpcCommand.ReloadConfig);
            Assert.AreEqual(4, (byte)IpcCommand.ReloadGroups);
            Assert.AreEqual(5, (byte)IpcCommand.ReloadApps);
            Assert.AreEqual(6, (byte)IpcCommand.RefreshButtonBars);
        }

        #endregion

        #region Header format stability per command

        [Test]
        public void EncodeSelectTab_Produces_Stable_Header() {
            byte[] buffer = IpcCommandMessage.EncodeSelectTab(new IntPtr(0x12345678), 3);
            AssertHeader(buffer, IpcCommand.SelectTab);
            Assert.AreEqual(HeaderLength + 12, buffer.Length, "SelectTab payload is 12 bytes");
        }

        [Test]
        public void EncodeOpenOptions_Produces_Stable_Header_EmptyPayload() {
            byte[] buffer = IpcCommandMessage.EncodeOpenOptions();
            AssertHeader(buffer, IpcCommand.OpenOptions);
            Assert.AreEqual(HeaderLength, buffer.Length, "OpenOptions has no payload");
        }

        [Test]
        public void Encode_ReloadCommands_Have_EmptyPayload() {
            foreach(IpcCommand cmd in new[] { IpcCommand.ReloadConfig, IpcCommand.ReloadGroups, IpcCommand.ReloadApps }) {
                byte[] buffer = IpcCommandMessage.Encode(cmd);
                AssertHeader(buffer, cmd);
                Assert.AreEqual(HeaderLength, buffer.Length, cmd + " must have empty payload");

                IpcCommand parsed;
                byte[] payload;
                Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out parsed, out payload), cmd + " must parse");
                Assert.AreEqual(cmd, parsed);
                Assert.AreEqual(0, payload.Length, cmd + " payload must be empty");
            }
        }

        #endregion

        #region SelectTab payload byte layout is frozen

        [Test]
        public void SelectTab_Payload_ByteLayout_Is_Frozen() {
            // 布局：handle 作为 Int64 存于 payload[0..8]，index 作为 Int32 存于 payload[8..12]。
            // 注意：测试项目为 x86，IntPtr 为 32 位，因此 handle 取值需落在 Int32 范围内。
            IntPtr handle = new IntPtr(0x12345678);
            int index = 0x0A0B0C0D;
            byte[] buffer = IpcCommandMessage.EncodeSelectTab(handle, index);

            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.SelectTab, cmd);
            Assert.AreEqual(12, payload.Length, "SelectTab payload length must be 12");

            byte[] expectedHandle = BitConverter.GetBytes(handle.ToInt64());
            byte[] expectedIndex = BitConverter.GetBytes(index);
            for(int i = 0; i < 8; i++) {
                Assert.AreEqual(expectedHandle[i], payload[i], "handle byte " + i + " mismatch");
            }
            for(int i = 0; i < 4; i++) {
                Assert.AreEqual(expectedIndex[i], payload[8 + i], "index byte " + i + " mismatch");
            }
        }

        [Test]
        public void SelectTab_Roundtrip_Preserves_Negative_Index() {
            IntPtr handle = new IntPtr(-1);
            int index = -12345;
            byte[] buffer = IpcCommandMessage.EncodeSelectTab(handle, index);

            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));

            IntPtr decodedHandle;
            int decodedIndex;
            Assert.IsTrue(IpcCommandMessage.TryDecodeSelectTab(payload, out decodedHandle, out decodedIndex));
            Assert.AreEqual(handle, decodedHandle);
            Assert.AreEqual(index, decodedIndex);
        }

        #endregion

        #region Generic Encode/TryParse round-trip with arbitrary payload

        [Test]
        public void Encode_TryParse_Roundtrips_Arbitrary_Payload() {
            byte[] originalPayload = { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x7F };
            byte[] buffer = IpcCommandMessage.Encode(IpcCommand.SelectTab, originalPayload);
            AssertHeader(buffer, IpcCommand.SelectTab);

            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload));
            Assert.AreEqual(IpcCommand.SelectTab, cmd);
            CollectionAssert.AreEqual(originalPayload, payload, "payload must round-trip byte-for-byte");
        }

        [Test]
        public void Encode_Null_Payload_Yields_HeaderOnly() {
            byte[] buffer = IpcCommandMessage.Encode(IpcCommand.ReloadConfig, null);
            Assert.AreEqual(HeaderLength, buffer.Length);
        }

        #endregion

        #region Boundary / rejection cases

        [Test]
        public void TryParse_Rejects_Null_Buffer() {
            IpcCommand cmd;
            byte[] payload;
            Assert.IsFalse(IpcCommandMessage.TryParse(null, out cmd, out payload));
        }

        [Test]
        public void TryParse_Rejects_TooShort_Buffer() {
            IpcCommand cmd;
            byte[] payload;
            Assert.IsFalse(IpcCommandMessage.TryParse(new byte[] { (byte)'Q', (byte)'T', (byte)'I', (byte)'P', 1 }, out cmd, out payload),
                "5-byte buffer is shorter than the 6-byte header");
        }

        [Test]
        public void TryParse_Rejects_Wrong_Magic() {
            IpcCommand cmd;
            byte[] payload;
            byte[] bad = { (byte)'X', (byte)'T', (byte)'I', (byte)'P', 1, (byte)IpcCommand.OpenOptions };
            Assert.IsFalse(IpcCommandMessage.TryParse(bad, out cmd, out payload));
        }

        [Test]
        public void TryParse_Rejects_Wrong_Version() {
            IpcCommand cmd;
            byte[] payload;
            byte[] bad = { (byte)'Q', (byte)'T', (byte)'I', (byte)'P', 2, (byte)IpcCommand.OpenOptions };
            Assert.IsFalse(IpcCommandMessage.TryParse(bad, out cmd, out payload));
        }

        [Test]
        public void TryDecodeSelectTab_Rejects_Short_Payload() {
            IntPtr handle;
            int index;
            Assert.IsFalse(IpcCommandMessage.TryDecodeSelectTab(new byte[11], out handle, out index),
                "payload shorter than 12 bytes must be rejected");
            Assert.IsFalse(IpcCommandMessage.TryDecodeSelectTab(null, out handle, out index));
        }

        #endregion
    }
}
