using System;
using System.Reflection;
using QTTabBarLib;

namespace QTTtabBarTests {
    internal class RecordingConfigWriter : IConfigWriter {
        public int WriteCount { get; private set; }
        public bool DesktopOnly { get; private set; }
        public Config LastConfig { get; private set; }

        public void Write(Config config, bool desktopOnly) {
            WriteCount++;
            DesktopOnly = desktopOnly;
            LastConfig = SerializationHelper.DeepClone(config);
        }
    }

    internal class ConfigTestScope : IDisposable {
        private static readonly FieldInfo WriterField = typeof(ConfigManager).GetField(
            "_writer", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly IConfigWriter _previousWriter;

        private ConfigTestScope(IConfigWriter writer) {
            Writer = writer;
            _previousWriter = (IConfigWriter)WriterField.GetValue(null);
            WriterField.SetValue(null, writer);
        }

        public IConfigWriter Writer { get; }

        public static ConfigTestScope WithWriter(IConfigWriter writer) {
            return new ConfigTestScope(writer);
        }

        public static ConfigTestScope WithRecordingWriter() {
            return WithWriter(new RecordingConfigWriter());
        }

        public void Dispose() {
            WriterField.SetValue(null, _previousWriter);
        }
    }
}
