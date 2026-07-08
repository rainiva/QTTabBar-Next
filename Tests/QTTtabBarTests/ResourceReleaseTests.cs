using System;
using System.Drawing;
using NUnit.Framework;
using QTTabBarLib;
using QTTabBarLib.Interop;

namespace QTTtabBarTests {
    /// <summary>
    /// 第二轮审查 HIGH 级资源释放泄漏的针对性单元测试。
    ///
    /// 说明（TDD 分层豁免理由）：这三处泄漏都发生在底层非托管/COM 资源管理路径上
    /// （IShellView COM 引用、PIDL 非托管内存、GDI HBITMAP 句柄）。要在真实 shell 环境
    /// 之外做确定性断言，按任务测试策略把“释放/所有权判定”重构成可注入依赖的小方法，
    /// 用假的 releaser/deleteObject 委托断言“释放是否发生、发生几次、针对哪个对象”，
    /// 从而得到脱离真实 shell 的确定性 RED→GREEN，且不使用 Assert.Pass 占位。
    /// </summary>
    [TestFixture]
    public class ResourceReleaseTests {

        // ---------------------------------------------------------------------
        // Point 1: ShellBrowserEx.OnNavigateComplete —— QueryActiveShellView 得到的
        // ppshv(IShellView) 在赋值 folderView 后必须释放不再需要的 IShellView 引用。
        // ---------------------------------------------------------------------

        [Test]
        public void AcquireFolderViewFromShellView_KeepsReference_WhenCastSucceeds() {
            var spy = new ReleaseSpy();
            var fake = new FakeFolderView();

            IFolderView result = ShellBrowserEx.AcquireFolderViewFromShellView(fake, spy.Release);

            Assert.AreEqual(0, spy.Count, "Successful cast must not release the shared RCW");
            Assert.AreSame(fake, result, "应返回强转得到的 folderView");
        }

        [Test]
        public void AcquireFolderViewFromShellView_NullInput_DoesNotRelease() {
            var spy = new ReleaseSpy();

            IFolderView result = ShellBrowserEx.AcquireFolderViewFromShellView(null, spy.Release);

            Assert.AreEqual(0, spy.Count, "输入为 null 时不应调用释放");
            Assert.IsNull(result);
        }

        [Test]
        public void AcquireFolderViewFromShellView_NonFolderView_StillReleasesShellView() {
            var spy = new ReleaseSpy();
            var plain = new object(); // 不实现 IFolderView

            IFolderView result = ShellBrowserEx.AcquireFolderViewFromShellView(plain, spy.Release);

            // 即使强转失败（不是 IFolderView），QueryActiveShellView 已 AddRef 的 ppshv
            // 仍必须释放，避免 COM 引用泄漏。
            Assert.AreEqual(1, spy.Count, "强转失败也必须释放 IShellView 引用");
            Assert.AreSame(plain, spy.LastObject);
            Assert.IsNull(result);
        }

        // ---------------------------------------------------------------------
        // Point 2: ShellBrowserEx.GetItem —— 明确原始 ppidl 的所有权，保证所有路径都
        // 释放且不二次释放。ShouldFreeRawPidl 表达 finally 是否应释放原始 ppidl。
        // ---------------------------------------------------------------------

        [Test]
        public void ShouldFreeRawPidl_NoAppend_TransfersOwnershipToWrapper_ReturnsFalse() {
            // noAppend 分支返回 new IDLWrapper(ppidl)，所有权已交给 wrapper，
            // finally 不能再释放，否则二次释放。
            Assert.IsFalse(ShellBrowserEx.ShouldFreeRawPidl(false, true));
        }

        [Test]
        public void ShouldFreeRawPidl_AppendPath_MustFreeOriginal_ReturnsTrue() {
            // 非 noAppend 路径把 ppidl 与父路径 ILCombine 成一个“新”的 PIDL（由 wrapper
            // 持有），原始 ppidl 必须由 finally 释放。
            Assert.IsTrue(ShellBrowserEx.ShouldFreeRawPidl(false, false));
        }

        [Test]
        public void ShouldFreeRawPidl_ZeroPidl_NothingToFree_ReturnsFalse() {
            Assert.IsFalse(ShellBrowserEx.ShouldFreeRawPidl(true, false));
            Assert.IsFalse(ShellBrowserEx.ShouldFreeRawPidl(true, true));
        }

        // ---------------------------------------------------------------------
        // Point 3: ThumbnailTooltipForm.LoadThumbnail —— ppvThumb.Detach 得到的
        // HBITMAP 经 Image.FromHbitmap 拷贝像素后，原始 HBITMAP 必须 DeleteObject。
        // ---------------------------------------------------------------------

        [Test]
        public void CreateManagedBitmapAndReleaseHandle_DeletesSourceHandle_ExactlyOnce() {
            IntPtr fakeHandle = new IntPtr(0x1234);
            IntPtr deleted = IntPtr.Zero;
            int deleteCount = 0;

            using(var stub = new Bitmap(3, 3)) {
                Func<IntPtr, Bitmap> fromHbitmap = h => stub;
                Func<IntPtr, bool> deleteObject = h => { deleted = h; deleteCount++; return true; };

                Bitmap result = ThumbnailTooltipForm.CreateManagedBitmapAndReleaseHandle(
                        fakeHandle, fromHbitmap, deleteObject);

                Assert.AreSame(stub, result, "应返回 Image.FromHbitmap 生成的位图");
                Assert.AreEqual(1, deleteCount, "应恰好删除一次源 HBITMAP");
                Assert.AreEqual(fakeHandle, deleted, "删除的必须是 Detach 出来的原始句柄");
            }
        }

        [Test]
        public void CreateManagedBitmapAndReleaseHandle_RealHandle_ProducesUsableBitmap() {
            // 用真实 GDI 句柄验证 Image.FromHbitmap 会拷贝像素、删除原句柄安全。
            using(var src = new Bitmap(5, 7)) {
                IntPtr h = src.GetHbitmap();
                bool deleted = false;
                Func<IntPtr, bool> deleteObject = ptr => { deleted = true; return PInvoke.DeleteObject(ptr); };

                Bitmap result = ThumbnailTooltipForm.CreateManagedBitmapAndReleaseHandle(
                        h, Image.FromHbitmap, deleteObject);

                Assert.IsNotNull(result);
                Assert.AreEqual(new Size(5, 7), result.Size, "拷贝后的位图尺寸应与源一致");
                Assert.IsTrue(deleted, "原始 HBITMAP 应被删除");
                result.Dispose();
            }
        }

        [Test]
        public void CreateManagedBitmapAndReleaseHandle_DeletesHandle_WhenFromHbitmapThrows() {
            IntPtr fakeHandle = new IntPtr(0x5678);
            bool deleted = false;

            Assert.Throws<InvalidOperationException>(() =>
                ThumbnailTooltipForm.CreateManagedBitmapAndReleaseHandle(
                    fakeHandle,
                    h => { throw new InvalidOperationException("fromHbitmap failed"); },
                    h => { deleted = true; return true; }));

            Assert.IsTrue(deleted, "fromHbitmap 抛异常时仍应释放原始 HBITMAP");
        }

        // ---- 测试替身 -------------------------------------------------------

        private sealed class ReleaseSpy {
            public int Count;
            public object LastObject;

            public int Release(object obj, string context) {
                Count++;
                LastObject = obj;
                return 0;
            }
        }

        /// <summary>
        /// 最小 IFolderView 假实现：所有方法返回 0，仅用于验证强转与释放语义。
        /// </summary>
        private sealed class FakeFolderView : IFolderView {
            public int GetCurrentViewMode(ref FVM pViewMode) { return 0; }
            public int SetCurrentViewMode(FVM ViewMode) { return 0; }
            public int GetFolder(ref Guid riid, out IPersistFolder2 ppv) { ppv = null; return 0; }
            public int Item(int iItemIndex, out IntPtr ppidl) { ppidl = IntPtr.Zero; return 0; }
            public int ItemCount(SVGIO uFlags, out int pcItems) { pcItems = 0; return 0; }
            public int Items(SVGIO uFlags, ref Guid riid, out IEnumIDList ppv) { ppv = null; return 0; }
            public int GetSelectionMarkedItem(out int piItem) { piItem = 0; return 0; }
            public int GetFocusedItem(out int piItem) { piItem = 0; return 0; }
            public int GetItemPosition(IntPtr pidl, out Point ppt) { ppt = Point.Empty; return 0; }
            public int GetSpacing(ref Point ppt) { return 0; }
            public int GetDefaultSpacing(ref Point ppt) { return 0; }
            public int GetAutoArrange() { return 0; }
            public int SelectItem(int iItem, SVSIF dwFlags) { return 0; }
            public int SelectAndPositionItems(uint cidl, IntPtr apidl, IntPtr apt, SVSIF dwFlags) { return 0; }
        }
    }
}
