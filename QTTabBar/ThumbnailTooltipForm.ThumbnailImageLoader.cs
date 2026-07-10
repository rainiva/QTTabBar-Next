//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class ThumbnailTooltipForm {
        // Batch10 GC10a: image / thumbnail loading cluster extracted from the
        // ThumbnailTooltipForm god class into this nested static helper. These are
        // pure static loaders with no dependency on form instance state.
        private static class ThumbnailImageLoader {
            internal static ImageData LoadImageFile(string path, DateTime dtLastWriteTime, out Size sizeRaw, out Size sizeActual) {
                sizeRaw = sizeActual = Size.Empty;
                using(Bitmap bitmap = new Bitmap(path)) {
                    if(bitmap != null) {
                        sizeRaw = bitmap.Size;
                        int width = sizeRaw.Width;
                        int height = sizeRaw.Height;
                        int maxWidth = Config.Tips.PreviewMaxWidth;
                        int maxHeight = Config.Tips.PreviewMaxHeight;
                        if((height > maxHeight) || (width > maxWidth)) {
                            if(height > maxHeight) {
                                width = (int)((maxHeight / ((double)height)) * width);
                                height = maxHeight;
                                if(width > maxWidth) {
                                    height = (int)((maxWidth / ((double)width)) * height);
                                    width = maxWidth;
                                }
                            }
                            else {
                                height = (int)((maxWidth / ((double)width)) * height);
                                width = maxWidth;
                            }
                            sizeActual = new Size(width, height);
                            if(ImageAnimator.CanAnimate(bitmap)) {
                                using(MemoryStream stream = new MemoryStream()) {
                                    bitmap.Save(stream, bitmap.RawFormat);
                                    var imgObj = new ImageData(new Bitmap(stream), stream, path, dtLastWriteTime, sizeRaw, sizeActual);
                                    return imgObj;
                                }
                            }
                            return new ImageData(new Bitmap(bitmap, width, height), null, path, dtLastWriteTime, sizeRaw, sizeActual);
                        }
                        sizeActual = sizeRaw;
                        using(MemoryStream stream2 = new MemoryStream()) {
                            bitmap.Save(stream2, bitmap.RawFormat);
                            return new ImageData(new Bitmap(stream2), stream2, path, dtLastWriteTime, sizeRaw, sizeRaw);
                        }
                    }
                }
                return null;
            }

            internal static ImageData LoadThumbnail(string path, DateTime dtLastWriteTime, out Size sizeRaw, out Size sizeActual, out string toolTipText, out bool fCached) {
                sizeRaw = sizeActual = Size.Empty;
                toolTipText = null;
                fCached = false;
                IntPtr zero = IntPtr.Zero;
                IShellItem ppsi = null;
                ISharedBitmap ppvThumb = null;
                LocalThumbnailCache o = null;
                try {
                    zero = PInvoke.ILCreateFromPath(path);
                    if((zero != IntPtr.Zero) && (PInvoke.SHCreateShellItem(IntPtr.Zero, null, zero, out ppsi) == 0)) {
                        o = new LocalThumbnailCache();
                        IThumbnailCache cache2 = (IThumbnailCache)o;
                        uint flags = 0;
                        uint pOutFlags = 0;
                        WTS_THUMBNAILID pThumbnailID = new WTS_THUMBNAILID();
                        uint cxyRequestedThumbSize = (uint)Math.Min(0x400, Math.Min(Config.Tips.PreviewMaxWidth, Config.Tips.PreviewMaxHeight));
                        if(cache2.GetThumbnail(ppsi, cxyRequestedThumbSize, flags, out ppvThumb, ref pOutFlags, ref pThumbnailID) == 0) {
                            IntPtr ptr2;
                            if((pOutFlags & 2) == 2) {
                                fCached = true;
                            }
                            if(ppvThumb.Detach(out ptr2) == 0) {
                                Bitmap bmp = CreateManagedBitmapAndReleaseHandle(ptr2, Image.FromHbitmap, PInvoke.DeleteObject);
                                Size size = bmp.Size;
                                sizeRaw = sizeActual = size;
                                ImageData data = new ImageData(bmp, null, path, dtLastWriteTime, size, size);
                                data.Thumbnail = true;
                                try {
                                    toolTipText = data.TooltipText = ShellMethods.GetShellInfoTipText(zero, false);
                                }
                                catch (Exception e)
                                {
                                    QTLogger.MakeErrorLog(e, "LoadThumbnail GetShellInfoTipText");
                                }
                                return data;
                            }
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(zero != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(zero);
                    }
                    if(ppsi != null) {
                        QTLogger.log("ReleaseComObject ppsi");
                        Marshal.ReleaseComObject(ppsi);
                    }
                    if(ppvThumb != null) {
                        QTLogger.log("ReleaseComObject ppvThumb");
                        Marshal.ReleaseComObject(ppvThumb);
                    }
                    if(o != null) {
                        QTLogger.log("ReleaseComObject o");
                        Marshal.ReleaseComObject(o);
                    }
                }
                return null;
            }

            internal static ImageData LoadThumbnail2(string path, DateTime dtLastWriteTime, out Size sizeRaw, out Size sizeActual, out string toolTipText, out bool fCached) {
                sizeRaw = sizeActual = Size.Empty;
                toolTipText = null;
                fCached = false;
                IntPtr zero = IntPtr.Zero;
                IShellFolder ppv = null;
                object obj2 = null;
                try {
                    IntPtr ptr3;
                    zero = PInvoke.ILCreateFromPath(path);
                    if((zero != IntPtr.Zero) && (PInvoke.SHBindToParent(zero, ExplorerGUIDs.IID_IShellFolder, out ppv, out ptr3) == 0)) {
                        uint rgfReserved = 0;
                        Guid riid = ExplorerGUIDs.IID_IExtractImage;
                        IntPtr[] apidl = new IntPtr[] { ptr3 };
                        if(ppv.GetUIObjectOf(IntPtr.Zero, 1, apidl, ref riid, ref rgfReserved, out obj2) == 0) {
                            IntPtr ptr2;
                            IExtractImage image = (IExtractImage)obj2;
                            StringBuilder pszPathBuffer = new StringBuilder(260);
                            int pdwPriority = 0;
                            Size prgSize = new Size(Config.Tips.PreviewMaxWidth, Config.Tips.PreviewMaxHeight);
                            int pdwFlags = 0x60;
                            if(((image.GetLocation(pszPathBuffer, pszPathBuffer.Capacity, ref pdwPriority, ref prgSize, 0x18, ref pdwFlags) == 0) && (image.Extract(out ptr2) == 0)) && (ptr2 != IntPtr.Zero)) {
                                Bitmap bmp = CreateManagedBitmapAndReleaseHandle(ptr2, Image.FromHbitmap, PInvoke.DeleteObject);
                                Size size = bmp.Size;
                                sizeRaw = sizeActual = size;
                                ImageData data = new ImageData(bmp, null, path, dtLastWriteTime, size, size);
                                data.Thumbnail = true;
                                try {
                                    toolTipText = data.TooltipText = ShellMethods.GetShellInfoTipText(zero, false);
                                }
                                catch (Exception e)
                                {
                                    QTLogger.MakeErrorLog(e, "GetShellInfoTipText");
                                }
                                return data;
                            }
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(zero != IntPtr.Zero) {
                        PInvoke.CoTaskMemFree(zero);
                    }
                    if(ppv != null) {
                        QTLogger.log("ReleaseComObject ppv");
                        Marshal.ReleaseComObject(ppv);
                    }
                    if(obj2 != null) {
                        QTLogger.log("ReleaseComObject obj2");
                        Marshal.ReleaseComObject(obj2);
                    }
                }
                return null;
            }

            // 由非托管 HBITMAP 生成托管 Bitmap，并删除原始 HBITMAP 句柄，避免 GDI 句柄泄漏。
            //
            // 所有权：IShellItemImageFactory/ISharedBitmap.Detach 或 IExtractImage.Extract 返回的
            // HBITMAP 归调用方所有。Image.FromHbitmap 会把像素数据拷贝进一个新的 GDI+ Bitmap，
            // 与原始 HBITMAP 不再共享像素，因此拷贝完成后立即 DeleteObject 原始句柄是安全且必需的。
            // 委托参数用于测试注入（可断言删除是否发生、发生几次、针对哪个句柄）。
            internal static Bitmap CreateManagedBitmapAndReleaseHandle(
                    IntPtr hBitmap, Func<IntPtr, Bitmap> fromHbitmap, Func<IntPtr, bool> deleteObject) {
                try {
                    Bitmap bmp = fromHbitmap(hBitmap);
                    deleteObject(hBitmap);
                    return bmp;
                }
                catch {
                    deleteObject(hBitmap);
                    throw;
                }
            }
        }
    }
}
