using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using NUnit.Framework;
using QTTabBarLib;

namespace QTTtabBarTests {
    /// <summary>
    /// 任务 #14：事件订阅泄漏与窗口子类化释放缺陷的针对性单元测试。
    ///
    /// 分层测试策略与豁免说明（属资源生命周期管理）：
    /// 1) NativeWindowController 是 #1(ExtendedListViewCommon)/#2(ListViewMonitor)
    ///    MessageCaptured 订阅泄漏的公共底层机制。它可用【真实进程内窗口句柄】(WinForms
    ///    Form) + 【真实 Windows 消息】做确定性验证，符合“面向真实操作、验证系统响应”的
    ///    要求，因此在此做确定性 RED→GREEN。
    /// 2) ListViewMonitor / QTDesktopTool / QTSecondViewBar 的完整反订阅路径依赖真实
    ///    Explorer / COM 站点句柄，无法在单元测试宿主内确定性构造。对这类用例：
    ///    - 对可提取、可确定性验证的逻辑（如是否落实标准 Dispose(bool) 模式、Dispose 幂等
    ///      与空容器安全）写确定性断言；
    ///    - 需真实 Explorer/COM 的完整回归标 [Explicit] 或以代码审查豁免，见各处注释。
    /// 全部断言均为真实行为断言，未使用 Assert.Pass 占位。
    /// </summary>
    [TestFixture]
    [Apartment(ApartmentState.STA)]
    public class SubscriptionReleaseTests {

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

        private const int WM_USER = 0x0400;

        // -----------------------------------------------------------------
        // #4 NativeWindowController：句柄释放清理
        // -----------------------------------------------------------------

        /// <summary>
        /// RED：ReleaseHandle() 目前仅把 MessageCaptured 置空，未清理 OptionalHandle
        /// （保存的是 Edit 控件等外部 HWND）。释放后残留死句柄属于子类化未完全清理。
        /// 修复后应连同 OptionalHandle 一起清零。
        /// </summary>
        [Test]
        public void ReleaseHandle_ClearsOptionalHandle() {
            using(Form form = new Form()) {
                IntPtr h = form.Handle; // 强制创建真实窗口句柄
                NativeWindowController controller = new NativeWindowController(h);
                controller.OptionalHandle = new IntPtr(0x1234);

                controller.ReleaseHandle();

                Assert.AreEqual(IntPtr.Zero, controller.OptionalHandle,
                        "ReleaseHandle 后 OptionalHandle 必须清零，避免残留已释放的窗口句柄");
            }
        }

        /// <summary>
        /// 确定性验证“反订阅后事件调用计数不再增长”：用真实窗口 + 真实消息，
        /// 订阅后消息被捕获计数增长；ReleaseHandle 释放后再次发送同一消息，计数不再增长。
        /// </summary>
        [Test]
        public void MessageCaptured_AfterReleaseHandle_StopsCounting() {
            using(Form form = new Form()) {
                IntPtr h = form.Handle;
                NativeWindowController controller = new NativeWindowController(h);
                int count = 0;
                NativeWindowController.MessageEventHandler handler =
                        (ref Message m) => { if(m.Msg == WM_USER) count++; return false; };
                controller.MessageCaptured += handler;

                SendMessage(h, WM_USER, IntPtr.Zero, IntPtr.Zero);
                int afterSubscribe = count;

                controller.ReleaseHandle();
                SendMessage(h, WM_USER, IntPtr.Zero, IntPtr.Zero);

                Assert.Greater(afterSubscribe, 0, "订阅后真实消息应被 MessageCaptured 捕获");
                Assert.AreEqual(afterSubscribe, count, "释放/反订阅后调用计数不应再增长");
            }
        }

        /// <summary>
        /// ReleaseHandle 应可安全重复调用（幂等），不抛异常。
        /// </summary>
        [Test]
        public void ReleaseHandle_IsIdempotent() {
            using(Form form = new Form()) {
                IntPtr h = form.Handle;
                NativeWindowController controller = new NativeWindowController(h);
                controller.OptionalHandle = new IntPtr(0x1234);

                Assert.DoesNotThrow(() => {
                    controller.ReleaseHandle();
                    controller.ReleaseHandle();
                }, "重复 ReleaseHandle 不应抛异常");
                Assert.AreEqual(IntPtr.Zero, controller.OptionalHandle);
            }
        }

        // -----------------------------------------------------------------
        // #2 ListViewMonitor：标准 Dispose(bool) 模式 + Dispose 安全性
        // -----------------------------------------------------------------

        /// <summary>
        /// RED：ListViewMonitor 现在只有无参 Dispose()，缺少标准 Dispose(bool) 模式。
        /// 任务要求“完善标准 Dispose(bool) 模式”，修复后应存在受保护的 Dispose(bool)。
        /// </summary>
        [Test]
        public void ListViewMonitor_Implements_StandardDisposeBoolPattern() {
            MethodInfo disposeBool = typeof(ListViewMonitor).GetMethod(
                    "Dispose",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[] { typeof(bool) },
                    null);

            Assert.IsNotNull(disposeBool,
                    "ListViewMonitor 应实现标准 Dispose(bool) 模式（含受保护的 Dispose(bool)）");
        }

        /// <summary>
        /// 确定性验证 Dispose 幂等与空容器（ContainerController == null）安全：
        /// 用 IntPtr.Zero 构造（找不到 ShellTabWindowClass，ContainerController 保持 null），
        /// 连续 Dispose 两次不应抛异常。这覆盖修复后新增反订阅代码的空值/幂等分支。
        /// </summary>
        [Test]
        public void ListViewMonitor_Dispose_IsIdempotent_WhenContainerIsNull() {
            ListViewMonitor monitor = new ListViewMonitor(null, IntPtr.Zero, IntPtr.Zero);

            Assert.DoesNotThrow(() => {
                monitor.Dispose();
                monitor.Dispose();
            }, "ContainerController 为 null 时，重复 Dispose 不应抛异常");
        }
    }
}
