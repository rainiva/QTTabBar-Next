using System;

namespace QTTabBarLib {
    internal enum IpcCommand : byte {
        SelectTab = 1,
        OpenOptions = 2,
        ReloadConfig = 3,
        ReloadGroups = 4,
        ReloadApps = 5,
        RefreshButtonBars = 6,
    }

    /// <summary>
    /// Typed IPC messages prefixed with QTIP so they never fall through to BinaryFormatter.
    /// Layout: 'Q''T''I''P' | version (1) | command (1) | payload...
    /// </summary>
    internal static class IpcCommandMessage {
        internal const byte ProtocolVersion = 1;
        private const int HeaderLength = 6;

        internal static bool TryParse(byte[] buffer, out IpcCommand command, out byte[] payload) {
            command = default(IpcCommand);
            payload = Array.Empty<byte>();
            if(buffer == null || buffer.Length < HeaderLength) {
                return false;
            }
            if(buffer[0] != (byte)'Q' || buffer[1] != (byte)'T' ||
               buffer[2] != (byte)'I' || buffer[3] != (byte)'P') {
                return false;
            }
            if(buffer[4] != ProtocolVersion) {
                return false;
            }
            command = (IpcCommand)buffer[5];
            int payloadLength = buffer.Length - HeaderLength;
            if(payloadLength > 0) {
                payload = new byte[payloadLength];
                Buffer.BlockCopy(buffer, HeaderLength, payload, 0, payloadLength);
            }
            return true;
        }

        internal static byte[] Encode(IpcCommand command, byte[] payload = null) {
            int payloadLength = payload == null ? 0 : payload.Length;
            byte[] buffer = new byte[HeaderLength + payloadLength];
            buffer[0] = (byte)'Q';
            buffer[1] = (byte)'T';
            buffer[2] = (byte)'I';
            buffer[3] = (byte)'P';
            buffer[4] = ProtocolVersion;
            buffer[5] = (byte)command;
            if(payloadLength > 0) {
                Buffer.BlockCopy(payload, 0, buffer, HeaderLength, payloadLength);
            }
            return buffer;
        }

        internal static byte[] EncodeSelectTab(IntPtr tabBarHandle, int index) {
            byte[] payload = new byte[12];
            Buffer.BlockCopy(BitConverter.GetBytes(tabBarHandle.ToInt64()), 0, payload, 0, 8);
            Buffer.BlockCopy(BitConverter.GetBytes(index), 0, payload, 8, 4);
            return Encode(IpcCommand.SelectTab, payload);
        }

        internal static byte[] EncodeOpenOptions() {
            return Encode(IpcCommand.OpenOptions);
        }

        internal static byte[] EncodeRefreshButtonBars() {
            return Encode(IpcCommand.RefreshButtonBars);
        }

        // Encodes a ReloadConfig command carrying an 8-byte configuration version
        // in the payload. Kept separate from the default Encode path so callers
        // that do not care about versioning still emit the 6-byte header, keeping
        // the wire format backward compatible (old receivers ignore the payload).
        internal static byte[] EncodeReloadConfig(long version) {
            return Encode(IpcCommand.ReloadConfig, BitConverter.GetBytes(version));
        }

        // Reads the configuration version from a ReloadConfig payload. Tolerates a
        // null / too-short payload (legacy sender, no version) by returning 0,
        // which downstream treats as "unspecified".
        internal static long DecodeConfigVersion(byte[] payload) {
            if(payload == null || payload.Length < 8) {
                return 0L;
            }
            return BitConverter.ToInt64(payload, 0);
        }

        internal static bool TryDecodeSelectTab(byte[] payload, out IntPtr tabBarHandle, out int index) {
            tabBarHandle = IntPtr.Zero;
            index = 0;
            if(payload == null || payload.Length < 12) {
                return false;
            }
            tabBarHandle = new IntPtr(BitConverter.ToInt64(payload, 0));
            index = BitConverter.ToInt32(payload, 8);
            return true;
        }
    }
}
