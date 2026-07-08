using System;
using System.Reflection;
using System.Security.Principal;
using System.ServiceModel;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// P0-2: IPC 命名管道认证测试。
    /// 验证:
    /// 1) 服务端/客户端使用的 NetNamedPipeBinding 启用了传输层安全(SecurityMode.Transport),
    ///    而不是 None——即任意本机进程都可连接的裸管道。
    /// 2) 调用方会话/SID 校验:仅与服务端进程为同一 Windows 用户(相同 SID)的调用方被放行,
    ///    不同用户/未授权(或无法确定身份)的调用方被拒绝。
    ///
    /// 说明:核心断言通过被测的静态工厂/校验方法(CreatePipeBinding / IsAuthorizedCaller)
    /// 完成,确定性且可自动运行。需要真实 WCF 端到端的用例标记为 [Explicit]。
    /// 采用反射访问,使测试在 RED 阶段(方法尚不存在)仍能编译,并在运行时产生真实断言失败。
    /// </summary>
    [TestFixture]
    public class IPCSecurityTests {

        private static MethodInfo GetStaticMethod(string name, params Type[] argTypes) {
            return typeof(InstanceManager).GetMethod(
                name,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static,
                null, argTypes, null);
        }

        // ---------------- 传输层安全(Transport) ----------------

        [Test]
        public void CreatePipeBinding_EnablesTransportSecurity() {
            MethodInfo m = GetStaticMethod("CreatePipeBinding", Type.EmptyTypes);
            Assert.IsNotNull(m, "InstanceManager 应提供可测的静态工厂方法 CreatePipeBinding()");

            object binding = m.Invoke(null, null);
            Assert.IsInstanceOf<NetNamedPipeBinding>(binding,
                "CreatePipeBinding() 应返回 NetNamedPipeBinding");

            NetNamedPipeBinding b = (NetNamedPipeBinding)binding;
            Assert.AreEqual(NetNamedPipeSecurityMode.Transport, b.Security.Mode,
                "管道绑定必须启用传输层安全(Transport),不能是 None");
        }

        [Test]
        public void CreatePipeBinding_PreservesLargeMessageQuotas() {
            // 回归保护:启用安全后,原有的大消息配额应保持在合理上限(4MB),
            // 而不是无限制的 int.MaxValue。
            MethodInfo m = GetStaticMethod("CreatePipeBinding", Type.EmptyTypes);
            Assert.IsNotNull(m, "InstanceManager 应提供可测的静态工厂方法 CreatePipeBinding()");

            NetNamedPipeBinding b = (NetNamedPipeBinding)m.Invoke(null, null);
            const int expected = 4 * 1024 * 1024;
            Assert.AreEqual(expected, b.MaxReceivedMessageSize, "MaxReceivedMessageSize 应限制为 4MB");
            Assert.AreEqual(expected, b.MaxBufferSize, "MaxBufferSize 应限制为 4MB");
            Assert.AreEqual(expected, b.ReaderQuotas.MaxArrayLength, "ReaderQuotas.MaxArrayLength 应限制为 4MB");
            Assert.AreEqual(TimeSpan.MaxValue, b.ReceiveTimeout, "ReceiveTimeout 应保持 TimeSpan.MaxValue");
        }

        // ---------------- 调用方 SID / 会话校验 ----------------

        [Test]
        public void IsAuthorizedCaller_AcceptsSameUserSid() {
            MethodInfo m = GetStaticMethod("IsAuthorizedCaller", typeof(SecurityIdentifier));
            Assert.IsNotNull(m, "InstanceManager 应提供可测的静态校验方法 IsAuthorizedCaller(SecurityIdentifier)");

            SecurityIdentifier selfSid;
            using (WindowsIdentity self = WindowsIdentity.GetCurrent()) {
                selfSid = self.User;
            }

            bool result = (bool)m.Invoke(null, new object[] { selfSid });
            Assert.IsTrue(result, "同一用户(当前进程用户 SID)必须被授权放行");
        }

        [Test]
        public void IsAuthorizedCaller_RejectsDifferentUserSid() {
            MethodInfo m = GetStaticMethod("IsAuthorizedCaller", typeof(SecurityIdentifier));
            Assert.IsNotNull(m, "InstanceManager 应提供可测的静态校验方法 IsAuthorizedCaller(SecurityIdentifier)");

            // NullSid (S-1-0-0) 不可能等于任何真实用户 SID,代表不同用户/未授权调用方。
            SecurityIdentifier otherSid = new SecurityIdentifier(WellKnownSidType.NullSid, null);

            bool result = (bool)m.Invoke(null, new object[] { otherSid });
            Assert.IsFalse(result, "不同用户/未授权 SID 必须被拒绝");
        }

        [Test]
        public void IsAuthorizedCaller_RejectsNullSid() {
            MethodInfo m = GetStaticMethod("IsAuthorizedCaller", typeof(SecurityIdentifier));
            Assert.IsNotNull(m, "InstanceManager 应提供可测的静态校验方法 IsAuthorizedCaller(SecurityIdentifier)");

            bool result = (bool)m.Invoke(null, new object[] { null });
            Assert.IsFalse(result, "无法确定调用方身份(null SID)时必须拒绝");
        }

        // ---------------- 真实 WCF 端到端(需显式运行) ----------------

        [ServiceContract]
        private interface ISecProbe {
            [OperationContract]
            string WhoAmI();
        }

        [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
        private class SecProbe : ISecProbe {
            public string WhoAmI() {
                ServiceSecurityContext ctx = ServiceSecurityContext.Current;
                WindowsIdentity id = ctx != null ? ctx.WindowsIdentity : null;
                return id != null && id.User != null ? id.User.Value : null;
            }
        }

        [Test]
        [Explicit("真实 WCF 端到端:验证 Transport 安全下同一用户可连通,且服务端能取得调用方 Windows 身份并通过授权校验")]
        public void EndToEnd_TransportBinding_SameUser_CarriesIdentityAndPasses() {
            MethodInfo factory = GetStaticMethod("CreatePipeBinding", Type.EmptyTypes);
            Assert.IsNotNull(factory, "需要 CreatePipeBinding() 工厂方法");
            MethodInfo auth = GetStaticMethod("IsAuthorizedCaller", typeof(SecurityIdentifier));
            Assert.IsNotNull(auth, "需要 IsAuthorizedCaller() 校验方法");

            string address = "net.pipe://localhost/QTTabBarPipeSecProbe" + Guid.NewGuid().ToString("N");

            ServiceHost host = new ServiceHost(new SecProbe(), new Uri(address));
            host.AddServiceEndpoint(typeof(ISecProbe),
                (System.ServiceModel.Channels.Binding)factory.Invoke(null, null),
                new Uri(address));
            host.Open();

            ChannelFactory<ISecProbe> cf = null;
            try {
                cf = new ChannelFactory<ISecProbe>(
                    (System.ServiceModel.Channels.Binding)factory.Invoke(null, null),
                    new EndpointAddress(address));
                ISecProbe proxy = cf.CreateChannel();

                string observedSid = proxy.WhoAmI();

                string expectedSid;
                using (WindowsIdentity self = WindowsIdentity.GetCurrent()) {
                    expectedSid = self.User.Value;
                }

                Assert.AreEqual(expectedSid, observedSid,
                    "Transport 安全下服务端应能取得同一用户的调用方 SID");

                bool authorized = (bool)auth.Invoke(null,
                    new object[] { new SecurityIdentifier(observedSid) });
                Assert.IsTrue(authorized, "端到端取得的调用方 SID 应通过授权校验");

                ((IClientChannel)proxy).Close();
            }
            finally {
                if (cf != null) {
                    try { cf.Close(); } catch { }
                }
                try { host.Close(); } catch { }
            }
        }
    }
}
