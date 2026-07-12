using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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

    internal sealed class RecordingWindowWriter : IConfigWindowWriter {
        public List<ConfigWindowField> FieldsWritten { get; } = new List<ConfigWindowField>();
        public Config._Window LastWindow { get; private set; }

        public void Write(Config._Window window, ConfigWindowField fields) {
            FieldsWritten.Add(fields);
            LastWindow = SerializationHelper.DeepClone(window);
        }
    }

    internal sealed class FirstWriteBarrierWriter : IConfigWriter {
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

    internal sealed class FirstWriteBarrierWindowWriter : IConfigWindowWriter {
        private int _writeCount;

        public ManualResetEventSlim FirstWriteEntered { get; } = new ManualResetEventSlim(false);
        public ManualResetEventSlim SecondMutationEntered { get; } = new ManualResetEventSlim(false);

        public void Write(Config._Window window, ConfigWindowField fields) {
            if(Interlocked.Increment(ref _writeCount) == 1) {
                FirstWriteEntered.Set();
                SecondMutationEntered.Wait(TimeSpan.FromSeconds(2));
            }
        }
    }

    internal class ConfigTestScope : IDisposable {
        private static readonly FieldInfo WriterField = typeof(ConfigManager).GetField(
            "_writer", BindingFlags.NonPublic | BindingFlags.Static);
        private static readonly FieldInfo WindowWriterField = typeof(ConfigManager).GetField(
            "_windowWriter", BindingFlags.NonPublic | BindingFlags.Static);

        private readonly IConfigWriter _previousWriter;
        private readonly IConfigWindowWriter _previousWindowWriter;

        private ConfigTestScope(IConfigWriter writer, IConfigWindowWriter windowWriter) {
            Writer = writer;
            WindowWriter = windowWriter;
            _previousWriter = (IConfigWriter)WriterField.GetValue(null);
            _previousWindowWriter = (IConfigWindowWriter)WindowWriterField.GetValue(null);
            WriterField.SetValue(null, writer);
            if(windowWriter != null) {
                WindowWriterField.SetValue(null, windowWriter);
            }
        }

        public IConfigWriter Writer { get; }
        public IConfigWindowWriter WindowWriter { get; }

        public static ConfigTestScope WithWriter(IConfigWriter writer) {
            return new ConfigTestScope(writer, null);
        }

        public static ConfigTestScope WithWindowWriter(IConfigWindowWriter windowWriter) {
            return new ConfigTestScope(new RecordingConfigWriter(), windowWriter);
        }

        public static ConfigTestScope WithWriters(IConfigWriter writer, IConfigWindowWriter windowWriter) {
            return new ConfigTestScope(writer, windowWriter);
        }

        public static ConfigTestScope WithRecordingWriter() {
            return WithWriter(new RecordingConfigWriter());
        }

        public void Dispose() {
            WriterField.SetValue(null, _previousWriter);
            WindowWriterField.SetValue(null, _previousWindowWriter);
        }
    }
}
