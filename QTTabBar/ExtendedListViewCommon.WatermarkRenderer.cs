//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
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
using QTPlugin;
using QTTabBarLib.Common;
using QTTabBarLib.ExplorerBrowser;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal abstract partial class ExtendedListViewCommon {

        // Batch6 GC6a: watermark / background rendering extracted out of the main
        // ExtendedListViewCommon body into this controller. Behavior is preserved
        // 1:1; the public RefreshViewWatermark override stays in the main file as a
        // facade that forwards to this renderer. The renderer reaches back to the
        // owning list view via _owner and reuses the owner's LVBKIF constants /
        // BG_IMG (both remain declared on the outer class, still in scope here).
        private sealed class WatermarkRenderer {
            private readonly ExtendedListViewCommon _owner;

            internal WatermarkRenderer(ExtendedListViewCommon owner) {
                _owner = owner;
            }

            internal void RefreshViewWatermark(bool fClear) {
                if(!_owner.VistaLayout) {
                    return;
                }
                if(Config.Tweaks.ViewWatermarking) {
                    Bitmap bmp = null;
                    switch(_owner.ViewPerceivedType) {
                        case PerceivedType.Unknown:
                            bmp = ExplorerManager.GetWatermarkImage(BmpCacheKey.Watermark_General);
                            break;
                        case PerceivedType.Image:
                            bmp = ExplorerManager.GetWatermarkImage(BmpCacheKey.Watermark_Picture);
                            break;
                        case PerceivedType.Audio:
                            bmp = ExplorerManager.GetWatermarkImage(BmpCacheKey.Watermark_Music);
                            break;
                        case PerceivedType.Video:
                            bmp = ExplorerManager.GetWatermarkImage(BmpCacheKey.Watermark_Movie);
                            break;
                        case PerceivedType.Document:
                            bmp = ExplorerManager.GetWatermarkImage(BmpCacheKey.Watermark_Document);
                            break;
                    }
                    if(bmp != null) {
                        using(Bitmap clone = (Bitmap)bmp.Clone()) {
                            SetWaterMarkImage(clone);
                        }
                    }
                }
                else {
                    if(!fClear) {
                        return;
                    }
                    SetWaterMarkImage((Bitmap)null);
                }
            }

            private unsafe void SetWaterMarkImage(Bitmap bmp)
            {
                if (bmp != null)
                {
                    LVBKIMAGE* lParam = stackalloc LVBKIMAGE[1];
                    lParam->ulFlags = 805306368;
                    lParam->hBmp = bmp.GetHbitmap(Color.Black);
                    if (!(IntPtr.Zero == PInvoke.SendMessage(_owner.Handle, 4234, (void*)null, (void*)lParam)) || !(lParam->hBmp != IntPtr.Zero))
                        return;
                    PInvoke.DeleteObject(lParam->hBmp);
                }
                else
                {
                    LVBKIMAGE* lParam = stackalloc LVBKIMAGE[1];
                    lParam->ulFlags = 268435456;
                    PInvoke.SendMessage(_owner.Handle, 4234, (void*)null, (void*)lParam);
                }
            }

            internal bool SetBackgroundImage2(bool isWatermark, bool isTiled, int xOffset, int yOffset)
            {
                LVBKIMAGE lvbkimage = new LVBKIMAGE();
                // IntPtr handle = ShellViewController.Handle;
                IntPtr handle = _owner.ListViewController.Handle;
                /*var findWindowEx = PInvoke.FindWindowEx(ListViewController.Handle, IntPtr.Zero, "DirectUIHWND", null);
                if (handle != findWindowEx)
                {
                    handle = findWindowEx;
                }*/
                // We have to clear any pre-existing background image, otherwise the attempt to set the image will fail.
                // We don't know which type may already have been set, so we just clear both the watermark and the image.
                lvbkimage.ulFlags = LVBKIF_TYPE_WATERMARK;
                IntPtr result = PInvoke.SendMessageLVBKIMAGE(handle, LVM_SETBKIMAGE, 0, ref lvbkimage);
                lvbkimage.ulFlags = LVBKIF_SOURCE_HBITMAP;
                result = PInvoke.SendMessageLVBKIMAGE(handle, LVM_SETBKIMAGE, 0, ref lvbkimage);

                if(File.Exists(BG_IMG)) {
                    using(FreeBitmap freeBitmap = new FreeBitmap(BG_IMG))
                    using(Bitmap bm = freeBitmap.Clone()) {
                        lvbkimage.hBmp = bm.GetHbitmap();
                        lvbkimage.ulFlags = isWatermark ? LVBKIF_TYPE_WATERMARK : (isTiled ? LVBKIF_SOURCE_HBITMAP | LVBKIF_STYLE_TILE : LVBKIF_SOURCE_HBITMAP);
                        lvbkimage.xOffset = xOffset;
                        lvbkimage.yOffset = yOffset;
                        IntPtr setResult = PInvoke.SendMessage(handle, 4234, IntPtr.Zero, ref lvbkimage);
                        if(setResult == IntPtr.Zero && lvbkimage.hBmp != IntPtr.Zero) {
                            PInvoke.DeleteObject(lvbkimage.hBmp);
                        }
                        return setResult != IntPtr.Zero;
                    }
                }
                return (result != IntPtr.Zero);
            }

            public  bool SetBackgroundImage(bool isWatermark, bool isTiled, int xOffset, int yOffset)
            {
                LVBKIMAGE lvbkimage = new LVBKIMAGE();
                // IntPtr handle = ShellViewController.Handle;
                IntPtr handle = _owner.ListViewController.Handle; // DirectUIHWND  SHELLDLL_DefView
                // find parent ShellTabWindowClass  DUIViewWndClassName DirectUIHWND

                // [log] PID:15516 TID:1 2022/9/22 9:17:17  parent name SHELLDLL_DefView
                //     [log] PID:15516 TID:1 2022/9/22 9:17:17  parent name ShellTabWindowClass
                //     [log] PID:15516 TID:1 2022/9/22 9:17:17  parent name CabinetWClass
                var name = PInvoke.GetClassName(handle);
                QTLogger.log("name " + name);
                var parent = PInvoke.GetParent(handle);
                name = PInvoke.GetClassName(parent);
                QTLogger.log(" parent name " + name);
                parent = PInvoke.GetParent(parent);
                name = PInvoke.GetClassName(parent);
                QTLogger.log(" parent name " + name);

                // var findWindowEx = PInvoke.FindWindowEx(parent, IntPtr.Zero, "DUIViewWndClassName", null);
                IntPtr findWindowEx = WindowUtils.FindChildWindow(parent, hwnd => PInvoke.GetClassName(hwnd) == "DirectUIHWND");
                if (IntPtr.Zero != findWindowEx)
                {
                    QTLogger.log(" found DirectUIHWND ");
                    handle = findWindowEx;
                }
                // parent = PInvoke.GetParent(parent);
                // name = PInvoke.GetClassName(parent);
                // QTLogger.log(" parent name " + name);
                /*handle = findParent("ShellTabWindowClass");
                if (handle == IntPtr.Zero)
                {   
                    QTLogger.log("SetBackgroundImage not found class" );
                    return false;
                }*/
                // We have to clear any pre-existing background image, otherwise the attempt to set the image will fail.
                // We don't know which type may already have been set, so we just clear both the watermark and the image.
                lvbkimage.ulFlags = LVBKIF_TYPE_WATERMARK;
                IntPtr result = PInvoke.SendMessageLVBKIMAGE(handle, LVM_SETBKIMAGE, 0, ref lvbkimage);
                lvbkimage.ulFlags = LVBKIF_SOURCE_HBITMAP;
                result = PInvoke.SendMessageLVBKIMAGE(handle, LVM_SETBKIMAGE, 0, ref lvbkimage);


                if (File.Exists(BG_IMG))
                {
                    using (FreeBitmap freeBitmap = new FreeBitmap(BG_IMG))
                    using (Bitmap bm = freeBitmap.Clone())
                    {
                        lvbkimage.hBmp = bm.GetHbitmap(Color.Black);
                    }
                }
                else
                {
                    lvbkimage.hBmp = IntPtr.Zero;
                }
                lvbkimage.ulFlags = isWatermark ? LVBKIF_TYPE_WATERMARK : (isTiled ? LVBKIF_SOURCE_HBITMAP | LVBKIF_STYLE_TILE : LVBKIF_SOURCE_HBITMAP);
                lvbkimage.xOffset = xOffset;
                lvbkimage.yOffset = yOffset;
                IntPtr setResult = PInvoke.SendMessageLVBKIMAGE(handle, LVM_SETBKIMAGE, 0, ref lvbkimage);
                if(setResult == IntPtr.Zero && lvbkimage.hBmp != IntPtr.Zero) {
                    PInvoke.DeleteObject(lvbkimage.hBmp);
                }
                QTLogger.log("SetWaterMarkImage " + BG_IMG);
                return (setResult != IntPtr.Zero);
            }

            private IntPtr findParent(string className)
            {
                int count = 0;
                do
                {
                    var intPtr = PInvoke.GetParent(_owner.Handle);
                    var name = PInvoke.GetClassName(intPtr);
                    if (name.Equals(className))
                    {
                        return intPtr;
                    }
                    count++;
                } while ( count <= 10);
                return IntPtr.Zero;
            }
        }
    }
}
