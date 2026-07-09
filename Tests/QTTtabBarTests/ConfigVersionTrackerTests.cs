using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
    ///  - DecodeConfigVersion tolerates null/short payload as version 0;
    ///  - ShouldApply rejects version 0 (legacy unspecified payloads are not applied).
    ///
    /// Task 2 复审整改补充：ShouldApply 去重契约与 IpcCommandDispatcher 端的
    /// ReloadConfig 去重门控护栏测试（针对已存在的生产逻辑加固，不改行为语义）。
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

        // --- 去重契约测试用的反射辅助 ---
        // ConfigVersionTracker 为 internal static，lastAppliedVersion 为私有静态字段；
        // 每个测试通过 SetLastApplied 显式重置到已知基线，保证相互独立、可重复。

        private static FieldInfo LastAppliedField() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "ConfigVersionTracker 应存在");
            FieldInfo f = t.GetField("lastAppliedVersion", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "ConfigVersionTracker 应有 lastAppliedVersion 私有静态字段");
            return f;
        }

        private static long GetLastApplied() {
            return Convert.ToInt64(LastAppliedField().GetValue(null));
        }

        private static void SetLastApplied(long value) {
            LastAppliedField().SetValue(null, value);
        }

        // currentVersion 为私有静态字段；测试通过 SetCurrentVersion 显式重置以
        // 模拟「新进程/重启后进程内计数器从 0 开始」的发送方，保证可重复。
        private static FieldInfo CurrentVersionField() {
            Type t = TrackerType;
            Assert.IsNotNull(t, "ConfigVersionTracker 应存在");
            FieldInfo f = t.GetField("currentVersion", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "ConfigVersionTracker 应有 currentVersion 私有静态字段");
            return f;
        }

        private static void SetCurrentVersion(long value) {
            CurrentVersionField().SetValue(null, value);
        }

        private static bool InvokeShouldApply(long version) {
            MethodInfo m = TrackerType.GetMethod(
                "ShouldApply", AnyStatic, null, new[] { typeof(long) }, null);
            Assert.IsNotNull(m, "ConfigVersionTracker 应有 ShouldApply(long)");
            return (bool)m.Invoke(null, new object[] { version });
        }

        private static bool InvokeTryCreateClientAction(byte[] buffer, out Action work) {
            Type dispatcher = typeof(IpcCommandMessage).Assembly.GetType("QTTabBarLib.IpcCommandDispatcher");
            Assert.IsNotNull(dispatcher, "QTTabBarLib.IpcCommandDispatcher 类型应存在");
            MethodInfo m = dispatcher.GetMethod(
                "TryCreateClientAction", AnyStatic, null,
                new[] { typeof(byte[]), typeof(Action).MakeByRefType() }, null);
            Assert.IsNotNull(m, "IpcCommandDispatcher 应有 TryCreateClientAction(byte[], out Action)");
            object[] args = new object[] { buffer, null };
            bool result = (bool)m.Invoke(null, args);
            work = (Action)args[1];
            return result;
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
            Assert.Greater(r1, before, "Increment 应返回严格大于此前 Current 的版本号");
            long r2 = InvokeIncrement();
            Assert.Greater(r2, r1, "连续 Increment 应严格单调递增");
            Assert.GreaterOrEqual(GetCurrent(), r2, "Current 应 >= 最近一次 Increment 结果");
        }

        [Test]
        public void Increment_IsThreadSafe() {
            long before = GetCurrent();
            const int threads = 32;
            const int perThread = 500;
            ConcurrentBag<long> produced = new ConcurrentBag<long>();
            Task[] tasks = new Task[threads];
            for(int i = 0; i < threads; i++) {
                tasks[i] = Task.Factory.StartNew(() => {
                    for(int j = 0; j < perThread; j++) {
                        produced.Add(InvokeIncrement());
                    }
                });
            }
            Task.WaitAll(tasks);

            int total = threads * perThread;
            Assert.AreEqual(total, produced.Count, "每次 Increment 都应产出一个返回值");
            // CAS 不丢更新的强断言：全部返回值互不相同（版本生成方式无关）。
            HashSet<long> distinct = new HashSet<long>(produced);
            Assert.AreEqual(total, distinct.Count,
                "并发 Increment 每次都应返回唯一的新版本（CAS 不丢更新）");
            long max = long.MinValue;
            foreach(long v in produced) {
                if(v > max) {
                    max = v;
                }
                Assert.Greater(v, before, "并发产出的每个版本都应严格大于起始 Current");
            }
            Assert.GreaterOrEqual(GetCurrent(), max, "Current 应 >= 并发产出的最大版本");
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

        #region ShouldApply 去重契约（版本 0 拒绝 / 更高应用 / 重复+陈旧忽略 / 并发）

        [Test]
        public void ShouldApply_VersionZero_IsRejected() {
            // version 0 = decode 容错值（旧发送方/无 payload），ShouldApply 拒绝且不推进基线。
            SetLastApplied(500L);
            Assert.IsFalse(InvokeShouldApply(0L), "version 0 应被拒绝（不再应用 legacy 无版本广播）");
            Assert.AreEqual(500L, GetLastApplied(), "version 0 不应改变 lastAppliedVersion");
            Assert.IsFalse(InvokeShouldApply(0L), "连续 version 0 仍应拒绝");
            Assert.AreEqual(500L, GetLastApplied(), "连续 version 0 依旧不改变 lastAppliedVersion");
        }

        [Test]
        public void ShouldApply_StrictlyHigherVersion_Applies_AndAdvancesBaseline() {
            long baseline = 1000L;
            SetLastApplied(baseline);
            Assert.IsTrue(InvokeShouldApply(baseline + 1), "严格更高的版本应被应用");
            Assert.AreEqual(baseline + 1, GetLastApplied(), "应用后 lastAppliedVersion 应推进到新版本");
        }

        [Test]
        public void ShouldApply_DuplicateVersion_IsIgnored() {
            long v = 2000L;
            SetLastApplied(0L);
            Assert.IsTrue(InvokeShouldApply(v), "首次应用该非零版本应 true");
            Assert.IsFalse(InvokeShouldApply(v), "重复同一非零版本第二次应 false");
            Assert.AreEqual(v, GetLastApplied(), "重复版本不应改变 lastAppliedVersion");
        }

        [Test]
        public void ShouldApply_StaleVersion_IsIgnored() {
            SetLastApplied(0L);
            Assert.IsTrue(InvokeShouldApply(3000L), "较高版本应先被应用");
            Assert.IsFalse(InvokeShouldApply(2999L), "随后陈旧的较低非零版本应被忽略");
            Assert.AreEqual(3000L, GetLastApplied(), "陈旧版本不应回退 lastAppliedVersion");
        }

        [Test]
        public void ShouldApply_Concurrent_EachVersionAppliedAtMostOnce_AndBaselineEndsAtMax() {
            // 多线程对同一版本区间 1..maxVersion 竞争应用：由于版本严格单调 + CAS，
            // 每个版本至多被应用一次；结束时 lastAppliedVersion 恰为成功应用的最大版本。
            SetLastApplied(0L);
            const int maxVersion = 1000;
            const int threads = 8;
            int[] appliedCount = new int[maxVersion + 1];
            Task[] tasks = new Task[threads];
            for(int i = 0; i < threads; i++) {
                tasks[i] = Task.Factory.StartNew(() => {
                    for(int v = 1; v <= maxVersion; v++) {
                        if(InvokeShouldApply(v)) {
                            Interlocked.Increment(ref appliedCount[v]);
                        }
                    }
                });
            }
            Task.WaitAll(tasks);

            for(int v = 1; v <= maxVersion; v++) {
                Assert.LessOrEqual(appliedCount[v], 1, "版本 " + v + " 不应被重复应用");
            }
            Assert.AreEqual((long)maxVersion, GetLastApplied(),
                "并发结束后 lastAppliedVersion 应等于成功应用的最大版本");
            Assert.AreEqual(1, appliedCount[maxVersion], "最大版本应恰好被应用一次");
        }

        #endregion

        #region IpcCommandDispatcher 端 ReloadConfig 去重门控集成

        [Test]
        public void Dispatcher_ReloadConfig_DuplicateVersion_DoesNotTriggerReload() {
            long applied = 4000L;
            SetLastApplied(applied);

            byte[] buffer = InvokeEncodeReloadConfig(applied); // 重复版本（== 已应用）
            Action work;
            Assert.IsTrue(InvokeTryCreateClientAction(buffer, out work),
                "带版本的 ReloadConfig 报文应可创建 client action");
            Assert.IsNotNull(work, "应返回非空 work 委托");

            // 重复版本：ShouldApply 返回 false，ReloadConfigOnClient 在触发
            // ConfigManager.ReadConfig()/UpdateConfig(false) 之前提前 return，
            // 因此 invoke work 无副作用、不抛异常、也不改变 lastAppliedVersion。
            Assert.DoesNotThrow(() => work(), "重复版本的 work 应提前 return，不触发副作用/异常");
            Assert.AreEqual(applied, GetLastApplied(), "重复版本不应改变 lastAppliedVersion（未触发 reload）");
        }

        [Test]
        public void Dispatcher_ReloadConfig_LegacySixBytePayload_DoesNotReload() {
            SetLastApplied(7000L);

            byte[] buffer = IpcCommandMessage.Encode(IpcCommand.ReloadConfig);
            Assert.AreEqual(6, buffer.Length, "legacy ReloadConfig 应为 6 字节头（无 payload）");

            Action work;
            Assert.IsTrue(InvokeTryCreateClientAction(buffer, out work),
                "legacy ReloadConfig 报文应可创建 client action");
            Assert.IsNotNull(work, "应返回非空 work 委托");

            Assert.DoesNotThrow(() => work(), "legacy 无版本 payload 的 work 应提前 return");
            Assert.AreEqual(7000L, GetLastApplied(), "legacy ReloadConfig 不应改变 lastAppliedVersion");
        }

        [Test]
        public void Dispatcher_ReloadConfig_StaleVersion_DoesNotTriggerReload() {
            long applied = 5000L;
            SetLastApplied(applied);

            byte[] buffer = InvokeEncodeReloadConfig(applied - 1); // 陈旧的非零版本
            Action work;
            Assert.IsTrue(InvokeTryCreateClientAction(buffer, out work),
                "带版本的 ReloadConfig 报文应可创建 client action");
            Assert.IsNotNull(work, "应返回非空 work 委托");

            Assert.DoesNotThrow(() => work(), "陈旧版本的 work 应提前 return，不触发副作用/异常");
            Assert.AreEqual(applied, GetLastApplied(), "陈旧版本不应改变 lastAppliedVersion（未触发 reload）");
        }

        [Test]
        public void Dispatcher_ReloadConfig_NewVersion_IsRoutedThroughDedupGate() {
            long baseline = 6000L;
            SetLastApplied(baseline);
            long newVersion = baseline + 1;

            byte[] buffer = InvokeEncodeReloadConfig(newVersion);

            // (a) 报文能被 dispatcher 识别并生成 work（路由存在）。
            Action work;
            Assert.IsTrue(InvokeTryCreateClientAction(buffer, out work),
                "新版本 ReloadConfig 报文应可创建 client action");
            Assert.IsNotNull(work, "新版本应返回非空 work 委托");

            // (b) 报文载荷解析出的版本号即 dispatcher 送入去重门控的值（路由值正确）。
            IpcCommand cmd;
            byte[] payload;
            Assert.IsTrue(IpcCommandMessage.TryParse(buffer, out cmd, out payload), "新版本报文应可解析");
            Assert.AreEqual(IpcCommand.ReloadConfig, cmd, "命令应为 ReloadConfig");
            Assert.AreEqual(newVersion, InvokeDecodeConfigVersion(payload),
                "dispatcher 应把报文中的新版本号解析出来送入去重门控");

            // (c) 注意：不直接 invoke 新版本 work —— 它会经 ReloadConfigOnClient 触发
            //     ConfigManager.ReadConfig()/UpdateConfig(false)（注册表读写、菜单渲染器初始化、
            //     PluginManager.RefreshPlugins、InstanceManager 广播等重副作用），无法在测试
            //     环境安全隔离。按任务授权退到「去重门控」层：验证门控对该新版本放行
            //     （ShouldApply(newVersion) 为 true 并推进基线），即新版本会触发 reload。
            Assert.IsTrue(InvokeShouldApply(newVersion), "新版本应通过去重门控（意味着会触发 reload）");
            Assert.AreEqual(newVersion, GetLastApplied(), "通过门控后基线应推进到新版本");
        }

        #endregion

        #region 跨进程/跨重启多发送方版本去重误判（RED→GREEN）

        [Test]
        public void MultipleSenders_RestartedSenderVersion_NotMisjudgedAsDuplicate() {
            // 复现缺陷：接收端已应用发送方 A 的版本并推进 lastAppliedVersion；
            // 随后另一个「从 0 重新计数」的发送方 B（新进程/重启）调用 Increment，
            // 其新版本必须严格大于已应用版本并被 ShouldApply 放行，否则 B 的
            // 合法配置变更会被误判为重复/陈旧而跳过。
            SetCurrentVersion(0L);
            SetLastApplied(0L);

            // 发送方 A 生成版本并被接收端应用（推进基线）。
            long versionA = InvokeIncrement();
            Assert.IsTrue(InvokeShouldApply(versionA), "发送方 A 的版本应被接收端应用");
            Assert.AreEqual(versionA, GetLastApplied(), "应用 A 后基线应推进到 versionA");

            // 让系统时钟前进，保证基于时钟的全局单调版本严格更新（消除时钟分辨率抖动）。
            Thread.Sleep(50);

            // 发送方 B：新进程/重启 → 进程内计数器从 0 开始。
            SetCurrentVersion(0L);
            long versionB = InvokeIncrement();

            // 旧实现（进程内从 0 自增）：versionA==1、versionB==1 → versionB 不大于
            // versionA，ShouldApply(versionB) 返回 false（RED）。
            // UTC ticks 全局单调修复后：versionB > versionA 且被放行（GREEN）。
            Assert.Greater(versionB, versionA,
                "跨进程/重启后从 0 重新计数的发送方，其新版本仍应严格大于已应用版本");
            Assert.IsTrue(InvokeShouldApply(versionB),
                "新发送方的合法配置变更不应被误判为重复/陈旧而跳过");
            Assert.AreEqual(versionB, GetLastApplied(), "放行 B 后基线应推进到 versionB");
        }

        #endregion
    }
}
