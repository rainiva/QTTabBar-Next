using System;
using System.Reflection;
using System.Threading.Tasks;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// Task 2.4 RED guard tests for ConfigVersionTracker + the version-aware
    /// ReloadConfig IPC encoding. Members that do not exist yet are probed via
    /// reflection so the test project still compiles and the RED phase shows up
    /// as runtime test failures (not compile errors).
    ///
    /// Backward-compatibility invariants asserted here:
    ///  - default Encode(ReloadConfig) stays a 6-byte header (no payload);
    ///  - EncodeReloadConfig(version) round-trips the version through TryParse;
    ///  - DecodeConfigVersion tolerates null/short payload as version 0.
    /// </summary>
    [TestFixture]
    public class ConfigVersionTrackerTests {
        private const BindingFlags AnyStatic =
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

        private static Type TrackerType {
            get { return typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.ConfigVersionTracker"); }
        }

        private static long GetCurrent() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "QTTabBarLib.ConfigVersionTracker 类型应存在");
            PropertyInfo p = t.GetProperty("Current", AnyStatic);
            if(p != null) {
                return Convert.ToInt64(p.GetValue(null, null));
            }
            MethodInfo m = t.GetMethod("Current", AnyStatic, null, Type.EmptyTypes, null);
            Assert.IsNotNull(m, "ConfigVersionTracker 应暴露 Current");
            return Convert.ToInt64(m.Invoke(null, null));
        }

        private static long InvokeIncrement() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "QTTabBarLib.ConfigVersionTracker 类型应存在");
            MethodInfo m = t.GetMethod("Increment", AnyStatic, null, Type.EmptyTypes, null);
            Assert.IsNotNull(m, "ConfigVersionTracker 应有 Increment 方法");
            return Convert.ToInt64(m.Invoke(null, null));
        }

        private static byte[] InvokeEncodeReloadConfig(long version) {
            MethodInfo m = typeof(IpcCommandMessage).GetMethod(
                "EncodeReloadConfig", AnyStatic, null, new[] { typeof(long) }, null);
            Assert.IsNotNull(m, "IpcCommandMessage 应有 EncodeReloadConfig(long)");
            return (byte[])m.Invoke(null, new object[] { version });
        }

        private static long InvokeDecodeConfigVersion(byte[] payload) {
            MethodInfo m = typeof(IpcCommandMessage).GetMethod(
                "DecodeConfigVersion", AnyStatic, null, new[] { typeof(byte[]) }, null);
            Assert.IsNotNull(m, "IpcCommandMessage 应有 DecodeConfigVersion(byte[])");
            return Convert.ToInt64(m.Invoke(null, new object[] { payload }));
        }

        #region ConfigVersionTracker shape + monotonic / thread-safe semantics

        [Test]
        public void ConfigVersionTracker_Type_Exists_Internal_Static() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "QTTabBarLib.ConfigVersionTracker 类型应存在");
            Assert.IsTrue(t.IsAbstract && t.IsSealed, "ConfigVersionTracker 应为 static 类（abstract+sealed）");
            Assert.IsFalse(t.IsPublic, "ConfigVersionTracker 应为 internal（非 public）");
        }

        [Test]
        public void ConfigVersionTracker_Has_Long_VersionField() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "ConfigVersionTracker 应存在");
            bool hasLong = false;
            foreach(FieldInfo f in t.GetFields(AnyStatic)) {
                if(f.FieldType == typeof(long)) {
                    hasLong = true;
                    break;
                }
            }
            Assert.IsTrue(hasLong, "ConfigVersionTracker 应含 long 版本号字段（线程安全递增的后备存储）");
        }

        [Test]
        public void Increment_ReturnsMonotonicallyIncreasingValue() {
            long before = GetCurrent();
            long r1 = InvokeIncrement();
            Assert.AreEqual(before + 1, r1, "Increment 应返回自增后的版本号");
            long r2 = InvokeIncrement();
            Assert.AreEqual(r1 + 1, r2, "连续 Increment 应单调 +1");
            Assert.GreaterOrEqual(GetCurrent(), r2, "Current 应 >= 最近一次 Increment 结果");
        }

        [Test]
        public void Increment_IsThreadSafe() {
            long before = GetCurrent();
            const int threads = 32;
            const int perThread = 500;
            Task[] tasks = new Task[threads];
            for(int i = 0; i < threads; i++) {
                tasks[i] = Task.Factory.StartNew(() => {
                    for(int j = 0; j < perThread; j++) {
                        InvokeIncrement();
                    }
                });
            }
            Task.WaitAll(tasks);
            Assert.AreEqual(before + (long)threads * perThread, GetCurrent(),
                "并发 Increment 不得丢失更新（线程安全）");
        }

        #endregion

        #region IPC version encode/decode round-trip + backward compatibility

        [Test]
        public void EncodeReloadConfig_Roundtrips_Version() {
            long version = 0x1122334455667788L;
            byte[] buffer = InvokeEncodeReloadConfig(version);
            Assert.AreEqual(6 + 8, buffer.Length, "EncodeReloadConfig 应为 6 字节头 + 8 字节版本 payload");

            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload), "带版本的 ReloadConfig 应可解析");
            Assert.AreEqual(IpcCommand.ReloadConfig, cmd, "命令应为 ReloadConfig");
            Assert.AreEqual(8, payload.Length, "payload 应为 8 字节");

            long decoded = InvokeDecodeConfigVersion(payload);
            Assert.AreEqual(version, decoded, "版本号应往返一致");
        }

        [Test]
        public void DecodeConfigVersion_Tolerates_Null_And_Short_Payload_As_Zero() {
            Assert.AreEqual(0L, InvokeDecodeConfigVersion(null), "null payload 容错为版本 0");
            Assert.AreEqual(0L, InvokeDecodeConfigVersion(new byte[0]), "空 payload 容错为版本 0");
            Assert.AreEqual(0L, InvokeDecodeConfigVersion(new byte[7]), "不足 8 字节容错为版本 0");
        }

        [Test]
        public void Encode_ReloadConfig_DefaultPath_Stays_SixBytes() {
            // 旧路径（无 payload）必须保持 6 字节头，保证旧进程/旧发送方兼容。
            byte[] buffer = IpcCommandMessage.Encode(IpcCommand.ReloadConfig);
            Assert.AreEqual(6, buffer.Length, "默认 Encode(ReloadConfig) 仍应为 6 字节头（向后兼容）");
        }

        #endregion
    }
}
