using System;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;

namespace QTTabBarLib {
    internal enum IpcCommand : byte {
        SelectTab = 1,
        OpenOptions = 2,
        ReloadConfig = 3,
        ReloadGroups = 4,
        ReloadApps = 5,
        RefreshButtonBars = 6,
        SyncSearchBoxWidth = 7,
        RestoreMainWindow = 8,
        OpenGroup = 9,
        OpenNewTabFromIdl = 10,
        CaptureNewWindow = 11,
        MergeTabs = 12,
        OpenNewTabOrWindowFromPath = 13,
        OpenPluginOptions = 14,
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

        internal static byte[] EncodeSyncSearchBoxWidth(int width) {
            return Encode(IpcCommand.SyncSearchBoxWidth, BitConverter.GetBytes(width));
        }

        internal static byte[] EncodeRestoreMainWindow() {
            return Encode(IpcCommand.RestoreMainWindow);
        }

        internal static byte[] EncodeOpenGroup(string groupName) {
            byte[] utf8 = Encoding.UTF8.GetBytes(groupName ?? string.Empty);
            byte[] payload = new byte[4 + utf8.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, payload, 0, 4);
            if(utf8.Length > 0) {
                Buffer.BlockCopy(utf8, 0, payload, 4, utf8.Length);
            }
            return Encode(IpcCommand.OpenGroup, payload);
        }

        internal static bool TryDecodeSyncSearchBoxWidth(byte[] payload, out int width) {
            width = 0;
            if(payload == null || payload.Length < 4) {
                return false;
            }
            width = BitConverter.ToInt32(payload, 0);
            return true;
        }

        internal static bool TryDecodeOpenGroup(byte[] payload, out string groupName) {
            groupName = string.Empty;
            if(payload == null || payload.Length < 4) {
                return false;
            }
            int length = BitConverter.ToInt32(payload, 0);
            if(length < 0 || payload.Length < 4 + length) {
                return false;
            }
            groupName = length == 0 ? string.Empty : Encoding.UTF8.GetString(payload, 4, length);
            return true;
        }

        private const byte OpenNewTabOrWindowKind = 0;
        private const byte OpenNewTabSequenceKind = 1;

        internal static byte[] EncodeOpenNewTabOrWindowFromIdl(byte[] idl) {
            byte[] idlBytes = idl ?? Array.Empty<byte>();
            byte[] payload = new byte[1 + 4 + idlBytes.Length];
            payload[0] = OpenNewTabOrWindowKind;
            Buffer.BlockCopy(BitConverter.GetBytes(idlBytes.Length), 0, payload, 1, 4);
            if(idlBytes.Length > 0) {
                Buffer.BlockCopy(idlBytes, 0, payload, 5, idlBytes.Length);
            }
            return Encode(IpcCommand.OpenNewTabFromIdl, payload);
        }

        internal static byte[] EncodeOpenNewTabSequence(byte[][] idls) {
            int count = idls == null ? 0 : idls.Length;
            int payloadLength = 1 + 4;
            int[] lengths = new int[count];
            for(int i = 0; i < count; i++) {
                lengths[i] = idls[i] == null ? 0 : idls[i].Length;
                payloadLength += 4 + lengths[i];
            }
            byte[] payload = new byte[payloadLength];
            int offset = 0;
            payload[offset++] = OpenNewTabSequenceKind;
            Buffer.BlockCopy(BitConverter.GetBytes(count), 0, payload, offset, 4);
            offset += 4;
            for(int i = 0; i < count; i++) {
                Buffer.BlockCopy(BitConverter.GetBytes(lengths[i]), 0, payload, offset, 4);
                offset += 4;
                if(lengths[i] > 0) {
                    Buffer.BlockCopy(idls[i], 0, payload, offset, lengths[i]);
                    offset += lengths[i];
                }
            }
            return Encode(IpcCommand.OpenNewTabFromIdl, payload);
        }

        internal static bool TryDecodeOpenNewTabOrWindowFromIdl(byte[] payload, out byte[] idl) {
            idl = Array.Empty<byte>();
            if(payload == null || payload.Length < 5 || payload[0] != OpenNewTabOrWindowKind) {
                return false;
            }
            int length = BitConverter.ToInt32(payload, 1);
            if(length < 0 || payload.Length < 5 + length) {
                return false;
            }
            if(length == 0) {
                return true;
            }
            idl = new byte[length];
            Buffer.BlockCopy(payload, 5, idl, 0, length);
            return true;
        }

        internal static bool TryDecodeOpenNewTabSequence(byte[] payload, out byte[][] idls) {
            idls = Array.Empty<byte[]>();
            if(payload == null || payload.Length < 5 || payload[0] != OpenNewTabSequenceKind) {
                return false;
            }
            int count = BitConverter.ToInt32(payload, 1);
            if(count < 0) {
                return false;
            }
            int offset = 5;
            idls = new byte[count][];
            for(int i = 0; i < count; i++) {
                if(payload.Length < offset + 4) {
                    return false;
                }
                int length = BitConverter.ToInt32(payload, offset);
                offset += 4;
                if(length < 0 || payload.Length < offset + length) {
                    return false;
                }
                if(length == 0) {
                    idls[i] = Array.Empty<byte>();
                    continue;
                }
                idls[i] = new byte[length];
                Buffer.BlockCopy(payload, offset, idls[i], 0, length);
                offset += length;
            }
            return true;
        }

        internal static byte[] EncodeCaptureNewWindow(string path, int cmdType, string selectName) {
            byte[] pathBytes = Encoding.UTF8.GetBytes(path ?? string.Empty);
            byte[] selectBytes = Encoding.UTF8.GetBytes(selectName ?? string.Empty);
            byte[] payload = new byte[4 + 4 + pathBytes.Length + 4 + selectBytes.Length];
            int offset = 0;
            Buffer.BlockCopy(BitConverter.GetBytes(cmdType), 0, payload, offset, 4);
            offset += 4;
            Buffer.BlockCopy(BitConverter.GetBytes(pathBytes.Length), 0, payload, offset, 4);
            offset += 4;
            if(pathBytes.Length > 0) {
                Buffer.BlockCopy(pathBytes, 0, payload, offset, pathBytes.Length);
                offset += pathBytes.Length;
            }
            Buffer.BlockCopy(BitConverter.GetBytes(selectBytes.Length), 0, payload, offset, 4);
            offset += 4;
            if(selectBytes.Length > 0) {
                Buffer.BlockCopy(selectBytes, 0, payload, offset, selectBytes.Length);
            }
            return Encode(IpcCommand.CaptureNewWindow, payload);
        }

        internal static bool TryDecodeCaptureNewWindow(
            byte[] payload,
            out string path,
            out int cmdType,
            out string selectName) {
            path = string.Empty;
            selectName = string.Empty;
            cmdType = 0;
            if(payload == null || payload.Length < 12) {
                return false;
            }
            int offset = 0;
            cmdType = BitConverter.ToInt32(payload, offset);
            offset += 4;
            int pathLength = BitConverter.ToInt32(payload, offset);
            offset += 4;
            if(pathLength < 0 || payload.Length < offset + pathLength + 4) {
                return false;
            }
            path = pathLength == 0 ? string.Empty : Encoding.UTF8.GetString(payload, offset, pathLength);
            offset += pathLength;
            int selectLength = BitConverter.ToInt32(payload, offset);
            offset += 4;
            if(selectLength < 0 || payload.Length < offset + selectLength) {
                return false;
            }
            selectName = selectLength == 0 ? string.Empty : Encoding.UTF8.GetString(payload, offset, selectLength);
            return true;
        }

        internal static byte[] EncodeMergeTabs(MergeTabPayload[] tabs) {
            using(var stream = new MemoryStream()) {
                var serializer = new DataContractJsonSerializer(typeof(MergeTabPayload[]));
                serializer.WriteObject(stream, tabs ?? Array.Empty<MergeTabPayload>());
                return Encode(IpcCommand.MergeTabs, stream.ToArray());
            }
        }

        internal static bool TryDecodeMergeTabs(byte[] payload, out MergeTabPayload[] tabs) {
            tabs = Array.Empty<MergeTabPayload>();
            if(payload == null || payload.Length == 0) {
                return true;
            }
            try {
                using(var stream = new MemoryStream(payload)) {
                    var serializer = new DataContractJsonSerializer(typeof(MergeTabPayload[]));
                    tabs = serializer.ReadObject(stream) as MergeTabPayload[] ?? Array.Empty<MergeTabPayload>();
                    return true;
                }
            }
            catch {
                return false;
            }
        }

        internal static byte[] EncodeOpenNewTabOrWindowFromPath(string path) {
            byte[] utf8 = Encoding.UTF8.GetBytes(path ?? string.Empty);
            byte[] payload = new byte[4 + utf8.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, payload, 0, 4);
            if(utf8.Length > 0) {
                Buffer.BlockCopy(utf8, 0, payload, 4, utf8.Length);
            }
            return Encode(IpcCommand.OpenNewTabOrWindowFromPath, payload);
        }

        internal static bool TryDecodeOpenNewTabOrWindowFromPath(byte[] payload, out string path) {
            path = string.Empty;
            if(payload == null || payload.Length < 4) {
                return false;
            }
            int length = BitConverter.ToInt32(payload, 0);
            if(length < 0 || payload.Length < 4 + length) {
                return false;
            }
            path = length == 0 ? string.Empty : Encoding.UTF8.GetString(payload, 4, length);
            return true;
        }

        internal static byte[] EncodeOpenPluginOptions(string pluginId) {
            byte[] utf8 = Encoding.UTF8.GetBytes(pluginId ?? string.Empty);
            byte[] payload = new byte[4 + utf8.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(utf8.Length), 0, payload, 0, 4);
            if(utf8.Length > 0) {
                Buffer.BlockCopy(utf8, 0, payload, 4, utf8.Length);
            }
            return Encode(IpcCommand.OpenPluginOptions, payload);
        }

        internal static bool TryDecodeOpenPluginOptions(byte[] payload, out string pluginId) {
            pluginId = string.Empty;
            if(payload == null || payload.Length < 4) {
                return false;
            }
            int length = BitConverter.ToInt32(payload, 0);
            if(length < 0 || payload.Length < 4 + length) {
                return false;
            }
            pluginId = length == 0 ? string.Empty : Encoding.UTF8.GetString(payload, 4, length);
            return true;
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
