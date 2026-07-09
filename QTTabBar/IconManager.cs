//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano
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
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    // Task 3.3 extraction: icon extraction + the global ImageList cache moved out
    // of QTUtility. Behavior is identical; QTUtility forwards to these via one-line
    // facades so cross-file callers are unchanged.
    //
    // Lock ordering (unchanged, P0-4): read cached data under SessionState.SyncRoot,
    // release it, THEN take ResourceCache.ImageListLock to write. The two locks are
    // never nested and imageListLock only ever guards the ImageList collection ops.
    internal static class IconManager {

        private readonly static string[] strIconExt = new string[] { ".exe", ".lnk", ".ico", ".url", ".sln" };

        public static bool ExtHasIcon(string ext) {
            return strIconExt.Contains(ext);
        }

        public static Icon GetIcon(IntPtr pIDL) {
            SHFILEINFO psfi = new SHFILEINFO();
            if((IntPtr.Zero != PInvoke.SHGetFileInfo(pIDL, 0, ref psfi, Marshal.SizeOf(psfi), 0x109)) && (psfi.hIcon != IntPtr.Zero)) {
                Icon icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                PInvoke.DestroyIcon(psfi.hIcon);
                return icon;
            }
            return Resources_Image.icoEmpty;
        }

        public static Icon GetIcon(string path, bool fExtension) {
            Icon icon;
            SHFILEINFO psfi = new SHFILEINFO();
            if(fExtension) {
                if(path.Length == 0) {
                    path = ".*";
                }
                if((IntPtr.Zero != PInvoke.SHGetFileInfo("*" + path, 0x80, ref psfi, Marshal.SizeOf(psfi), 0x111)) && (psfi.hIcon != IntPtr.Zero)) {
                    icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                    PInvoke.DestroyIcon(psfi.hIcon);
                    return icon;
                }
                return Resources_Image.icoEmpty;
            }
            if(path.Length == 0) {
                if((IntPtr.Zero != PInvoke.SHGetFileInfo("dummy", 0x10, ref psfi, Marshal.SizeOf(psfi), 0x111)) && (psfi.hIcon != IntPtr.Zero)) {
                    icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                    PInvoke.DestroyIcon(psfi.hIcon);
                    return icon;
                }
                return Resources_Image.icoEmpty;
            }
            if(!OSDetector.IsXP && path.StartsWith("::")) {
                IntPtr pszPath = PInvoke.ILCreateFromPath(path);
                if(pszPath != IntPtr.Zero) {
                    if((IntPtr.Zero != PInvoke.SHGetFileInfo(pszPath, 0, ref psfi, Marshal.SizeOf(psfi), 0x109)) && (psfi.hIcon != IntPtr.Zero)) {
                        icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                        PInvoke.DestroyIcon(psfi.hIcon);
                        PInvoke.CoTaskMemFree(pszPath);
                        return icon;
                    }
                    PInvoke.CoTaskMemFree(pszPath);
                }
            }
            else if((IntPtr.Zero != PInvoke.SHGetFileInfo(path, 0, ref psfi, Marshal.SizeOf(psfi), 0x101)) && (psfi.hIcon != IntPtr.Zero)) {
                icon = new Icon(Icon.FromHandle(psfi.hIcon), 0x10, 0x10);
                PInvoke.DestroyIcon(psfi.hIcon);
                return icon;
            }
            return Resources_Image.icoEmpty;
        }

        private static readonly string[] CompressedExtensions = { ".zip", ".lzh", ".cab" };

        public static bool ExtIsCompressed(string ext) {
            return CompressedExtensions.Contains(ext);
        }

        public static string GetImageKey(string path, string ext) {
            if(!string.IsNullOrEmpty(path)) {
                if(QTUtility2.IsNetworkPath(path)) {
                    if(ext != null) {
                        ext = ext.ToLower();
                        if(ext.Length == 0) {
                            SetImageKey("noext", path);
                            return "noext";
                        }
                        if(!ImageGlobalContainsKey(ext)) {
                            AddImageToGlobal(ext, GetIcon(ext, true));
                        }
                        return ext;
                    }
                    if(PathValidator.IsNetworkRootFolder(path)) {
                        SetImageKey(path, path);
                        return path;
                    }
                    SetImageKey("mynetwork", OSDetector.PATH_MYNETWORK);
                    return "mynetwork";
                }
                if(path.StartsWith("::")) {
                    SetImageKey(path, path);
                    return path;
                }
                if(ext != null) {
                    ext = ext.ToLower();
                    if(ext.Length == 0) {
                        SetImageKey("noext", path);
                        return "noext";
                    }
                    if(ExtHasIcon(ext)) {
                        SetImageKey(path, path);
                        return path;
                    }
                    SetImageKey(ext, path);
                    return ext;
                }
                if(path.Contains("*?*?*")) {
                    byte[] buffer;
                    if(ImageGlobalContainsKey(path)) {
                        return path;
                    }
                    // Read the cached PIDL under syncRoot and release it, THEN take
                    // imageListLock inside AddImageToGlobal; never nest the two locks.
                    bool found;
                    lock(SessionState.SyncRoot) {
                        found = SessionState.ITEMIDLIST_Dic_Session.TryGetValue(path, out buffer);
                    }
                    if(found) {
                        using(IDLWrapper w = new IDLWrapper(buffer)) {
                            if(w.Available) {
                                AddImageToGlobal(path, GetIcon(w.PIDL));
                                return path;
                            }
                        }
                    }
                    return "noimage";
                }
                if(QTUtility2.IsShellPathButNotFileSystem(path)) {
                    IDLWrapper wrapper;
                    if(ImageGlobalContainsKey(path)) {
                        return path;
                    }
                    if(IDLWrapper.TryGetCache(path, out wrapper)) {
                        using(wrapper) {
                            if(wrapper.Available) {
                                AddImageToGlobal(path, GetIcon(wrapper.PIDL));
                                return path;
                            }
                        }
                    }
                    return "noimage";
                }
                if(path.StartsWith("ftp://") || path.StartsWith("http://")) {
                    return "folder";
                }
                try {
                    DirectoryInfo info = new DirectoryInfo(path);
                    if(info.Exists) {
                        FileAttributes attributes = info.Attributes;
                        if(((attributes & FileAttributes.System) != 0) || ((attributes & FileAttributes.ReadOnly) != 0)) {
                            SetImageKey(path, path);
                            return path;
                        }
                        return "folder";
                    }
                    if(File.Exists(path)) {
                        ext = Path.GetExtension(path).ToLower();
                        if(ext.Length == 0) {
                            SetImageKey("noext", path);
                            return "noext";
                        }
                        if(ExtHasIcon(ext)) {
                            SetImageKey(path, path);
                            return path;
                        }
                        SetImageKey(ext, path);
                        return ext;
                    }
                    if(path.ToLower().Contains(@".zip\")) {
                        return "folder";
                    }
                }
                catch {
                }
            }
            return "noimage";
        }

        public static void LoadReservedImage(ImageReservationKey irk) {
            if(ImageGlobalContainsKey(irk.ImageKey)) {
                return;
            }
            switch(irk.ImageType) {
                case 0:
                    if(irk.ImageKey != "noimage") {
                        if(irk.ImageKey == "noext") {
                            AddImageToGlobal("noext", GetIcon(string.Empty, true));
                            return;
                        }
                        return;
                    }
                    return;

                case 1:
                    AddImageToGlobal(irk.ImageKey, GetIcon(irk.ImageKey, true));
                    return;

                case 2:
                case 4:
                    AddImageToGlobal(irk.ImageKey, GetIcon(irk.ImageKey, false));
                    return;

                case 3:
                    return;

                case 5:
                    // Read the cache under syncRoot and release it before taking
                    // imageListLock, so the two locks are never nested.
                    byte[] buffer;
                    bool found5;
                    lock(SessionState.SyncRoot) {
                        found5 = SessionState.ITEMIDLIST_Dic_Session.TryGetValue(irk.ImageKey, out buffer);
                    }
                    if(found5) {
                        using(IDLWrapper w = new IDLWrapper(buffer)) {
                            if(w.Available) {
                                AddImageToGlobal(irk.ImageKey, GetIcon(w.PIDL));
                            }
                        }
                    }
                    return;

                case 6:
                    IDLWrapper wrapper;
                    if(IDLWrapper.TryGetCache(irk.ImageKey, out wrapper)) {
                        using(wrapper) {
                            if(wrapper.Available) {
                                AddImageToGlobal(irk.ImageKey, GetIcon(wrapper.PIDL));
                            }
                        }
                    }
                    return;
            }
        }

        // Fast path: already present -> no icon extraction needed (read under lock
        // to avoid a race with concurrent writes). Icon extraction (possibly slow)
        // happens outside the lock and is written through AddImageToGlobal.
        internal static void SetImageKey(string key, string itemPath) {
            if(ImageGlobalContainsKey(key)) {
                return;
            }
            AddImageToGlobal(key, GetIcon(itemPath, false));
        }

        // Unified entry points for the ImageListGlobal cache (P0-4 thread safety).
        // Every Add / ContainsKey / indexer access to ImageListGlobal.Images must go
        // through these, guarded by ResourceCache.ImageListLock. The lock scope is kept
        // short and only ever covers the collection operation itself.
        internal static void AddImageToGlobal(string key, Image image) {
            lock(ResourceCache.ImageListLock) {
                if(!ResourceCache.ImageListGlobal.Images.ContainsKey(key)) {
                    ResourceCache.ImageListGlobal.Images.Add(key, image);
                }
            }
        }

        internal static void AddImageToGlobal(string key, Icon icon) {
            lock(ResourceCache.ImageListLock) {
                if(!ResourceCache.ImageListGlobal.Images.ContainsKey(key)) {
                    ResourceCache.ImageListGlobal.Images.Add(key, icon);
                }
            }
        }

        internal static bool ImageGlobalContainsKey(string key) {
            lock(ResourceCache.ImageListLock) {
                return ResourceCache.ImageListGlobal != null
                    && ResourceCache.ImageListGlobal.Images != null
                    && ResourceCache.ImageListGlobal.Images.ContainsKey(key);
            }
        }

        internal static Image GetImageFromGlobal(string key) {
            lock(ResourceCache.ImageListLock) {
                return ResourceCache.ImageListGlobal.Images[key];
            }
        }

        public static ImageReservationKey ReserveImageKey(QMenuItem qmi, string path, string ext) {
            ImageReservationKey key = null;
            if(string.IsNullOrEmpty(path)) {
                return new ImageReservationKey("noimage", 0);
            }
            if(!string.IsNullOrEmpty(ext)) {
                ext = ext.ToLower();
                if(ExtHasIcon(ext) && !QTUtility2.IsNetworkPath(path)) {
                    return new ImageReservationKey(path, 2);
                }
                return new ImageReservationKey(ext, 1);
            }
            if(QTUtility2.IsNetworkPath(path)) {
                if(PathValidator.IsNetworkRootFolder(path)) {
                    return new ImageReservationKey(path, 4);
                }
                return new ImageReservationKey("folder", 3);
            }
            if(path.StartsWith("::")) {
                return new ImageReservationKey(path, 4);
            }
            if(path.Contains("*?*?*")) {
                return new ImageReservationKey(path, 5);
            }
            if(QTUtility2.IsShellPathButNotFileSystem(path)) {
                return new ImageReservationKey(path, 6);
            }
            if(path.StartsWith("ftp://") || path.StartsWith("http://")) {
                return new ImageReservationKey("folder", 3);
            }
            try {
                if(qmi.Exists) {
                    if(qmi.Target == MenuTarget.Folder) {
                        if(qmi.HasIcon) {
                            return new ImageReservationKey(path, 4);
                        }
                        return new ImageReservationKey("folder", 3);
                    }
                    if(qmi.Target == MenuTarget.File) {
                        ext = Path.GetExtension(path).ToLower();
                        if(ext.Length == 0) {
                            return new ImageReservationKey("noext", 0);
                        }
                        if(ExtHasIcon(ext)) {
                            return new ImageReservationKey(path, 2);
                        }
                        return new ImageReservationKey(ext, 1);
                    }
                }
                DirectoryInfo info = new DirectoryInfo(path);
                if(info.Exists) {
                    FileAttributes attributes = info.Attributes;
                    if(((attributes & FileAttributes.System) != 0) || ((attributes & FileAttributes.ReadOnly) != 0)) {
                        return new ImageReservationKey(path, 4);
                    }
                    return new ImageReservationKey("folder", 3);
                }
                if(!File.Exists(path)) {
                    return new ImageReservationKey("noimage", 0);
                }
                ext = Path.GetExtension(path).ToLower();
                if(ext.Length == 0) {
                    return new ImageReservationKey("noext", 0);
                }
                if(ExtHasIcon(ext)) {
                    return new ImageReservationKey(path, 2);
                }
                key = new ImageReservationKey(ext, 1);
            }
            catch {
            }
            return key;
        }
    }
}
