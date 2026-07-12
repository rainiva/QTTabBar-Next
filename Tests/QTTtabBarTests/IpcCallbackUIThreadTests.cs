using System;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;
using QTTabBarLib.Ipc;

namespace QTTtabBarTests {
    /// <summary>
    /// P0-5: IPC 回调 UI 线程封送测试。
    /// 验证 InstanceManager.CommClient.Execute 反序列化得到的委托,
    /// 在后台(IPC 回调)线程被调用时,能够被封送到已注册的主 UI 控件线程执行,
    /// 而不是直接在后台线程执行(后者在真实场景会引发跨线程 InvalidOperationException)。
    /// </summary>
    [TestFixture]
    public class IpcCallbackUIThreadTests {

        // 被序列化委托(指向静态方法,可干净地二进制序列化)引用的共享状态。
        public static Control SharedControl;
        public static int ExecutedThreadId;
        public static bool ExecutedInvokeRequired;
        public static ManualResetEvent ExecutedSignal;

        // 委托实际执行体:记录执行所在线程,以及此时相对主控件是否 InvokeRequired。
        public static void RecordExecution() {
            ExecutedThreadId = Thread.CurrentThread.ManagedThreadId;
            Control c = SharedControl;
            ExecutedInvokeRequired = c != null && c.InvokeRequired;
            ExecutedSignal.Set();
        }

        [Test]
        public void Execute_MarshalsCallbackToUIThread_WhenCalledFromBackgroundThread() {
            ExecutedThreadId = 0;
            ExecutedInvokeRequired = false;
            ExecutedSignal = new ManualResetEvent(false);

            Control uiControl = null;
            int uiThreadId = 0;
            ManualResetEvent uiReady = new ManualResetEvent(false);

            // 启动一个带消息循环的专用 UI 线程,并在其上创建控件句柄。
            Thread uiThread = new Thread(() => {
                uiControl = new Control();
                IntPtr forceHandle = uiControl.Handle; // 强制创建句柄
                GC.KeepAlive(forceHandle);
                uiThreadId = Thread.CurrentThread.ManagedThreadId;
                uiReady.Set();
                Application.Run(); // 泵送消息,使 BeginInvoke 能够被派发
            });
            uiThread.IsBackground = true;
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Assert.IsTrue(uiReady.WaitOne(5000), "UI 线程未能初始化");

            try {
                SharedControl = uiControl;

                // 注册主 UI 控件(GREEN 功能)。加保护判断,使 RED 阶段
                // (SetMainUIControl 尚不存在时)仍能真实走 Execute 的现有直接执行路径。
                MethodInfo setMain = typeof(InstanceManager).GetMethod(
                    "SetMainUIControl", BindingFlags.Public | BindingFlags.Static);
                if(setMain != null) {
                    setMain.Invoke(null, new object[] { uiControl });
                }

                // 与生产代码一致地构造编码后的委托字节(DelToByte)。
                MethodInfo delToByte = typeof(IpcCommandGateway).GetMethod(
                    "DelToByte", BindingFlags.NonPublic | BindingFlags.Static);
                Assert.IsNotNull(delToByte, "DelToByte 私有方法应当存在");
                Action callback = RecordExecution;
                byte[] encoded = (byte[])delToByte.Invoke(null, new object[] { callback });
                Assert.IsNotNull(encoded, "编码后的委托字节不应为空");

                Type commClientType = typeof(IpcCommandGateway).Assembly.GetType(
                    "QTTabBarLib.Ipc.CommClient");
                Assert.IsNotNull(commClientType, "CommClient 类型应当存在");
                object commClient = Activator.CreateInstance(commClientType, true);
                MethodInfo execute = commClientType.GetMethod("Execute");
                Assert.IsNotNull(execute, "Execute 方法应当存在");

                // 在后台线程(模拟 IPC 回调线程)上调用 Execute。
                Exception bgException = null;
                Thread bgThread = new Thread(() => {
                    try {
                        execute.Invoke(commClient, new object[] { encoded });
                    }
                    catch(Exception ex) {
                        bgException = ex;
                    }
                });
                bgThread.IsBackground = true;
                bgThread.Start();
                bgThread.Join(5000);

                Assert.IsNull(bgException, "Execute 抛出异常: " + bgException);
                Assert.IsTrue(ExecutedSignal.WaitOne(5000), "回调委托未被执行");

                // 核心断言:回调必须被封送到 UI 线程执行,
                // 即 InvokeRequired == false 且执行线程 id == UI 线程 id。
                Assert.IsFalse(ExecutedInvokeRequired,
                    "回调在非 UI 线程执行(InvokeRequired 为 true)——未做封送");
                Assert.AreEqual(uiThreadId, ExecutedThreadId,
                    "回调未在已注册的 UI 线程上执行");
            }
            finally {
                if(uiControl != null && uiControl.IsHandleCreated) {
                    try {
                        uiControl.BeginInvoke(new Action(() => Application.ExitThread()));
                    }
                    catch { }
                }
                uiThread.Join(3000);
                MethodInfo setMainReset = typeof(InstanceManager).GetMethod(
                    "SetMainUIControl", BindingFlags.Public | BindingFlags.Static);
                if(setMainReset != null) {
                    try { setMainReset.Invoke(null, new object[] { null }); } catch { }
                }
                SharedControl = null;
            }
        }
    }
}
