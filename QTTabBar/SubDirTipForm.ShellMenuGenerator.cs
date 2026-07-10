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
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class SubDirTipForm {

        // Batch7 GC7a: shell context-menu building (directory / IDL / parent menus
        // and their lazy QueryVirtualMenu expansion) delegated to this controller.
        private readonly ShellMenuGenerator _menuGenerator;

        private sealed class ShellMenuGenerator {
            private readonly SubDirTipForm _owner;

            internal ShellMenuGenerator(SubDirTipForm owner) {
                _owner = owner;
            }

            internal QMenuItem CreateDirectoryItem(DirectoryInfo diSub, string title, bool fIcon, bool fLink) {
                bool flag;
                FileSystemInfo targetIfFolderLink = ShellMethods.GetTargetIfFolderLink(diSub, out flag);
                if(!flag) {
                    return null;
                }
                QMenuItem item = new QMenuItem(title, MenuTarget.Folder, MenuGenre.SubDirTip);
                item.Exists = true;
                item.HasIcon = fIcon;
                string fullName = diSub.FullName;
                if(fIcon) {
                    item.SetImageReservationKey(fullName, null);
                }
                else if(!fLink) {
                    item.ImageKey = "folder";
                }
                item.Path = fullName;
                item.TargetPath = targetIfFolderLink.FullName;
                item.QueryVirtualMenu += directoryItem_QueryVirtualMenu;
                return item;
            }

            internal List<QMenuItem> CreateMenu(DirectoryInfo di, string pathChild) {
                List<QMenuItem> list = new List<QMenuItem>();
                List<QMenuItem> collection = new List<QMenuItem>();
                bool flag = true;
                try {
                    flag = new DriveInfo(di.FullName).DriveFormat == "NTFS";
                }
                catch {
                }
                try {
                    bool flag2, flag3;
                    ShellFolderSettingsReader.GetHiddenFileSettings(out flag3, out flag2);
                    const FileAttributes attributes = FileAttributes.ReparsePoint | FileAttributes.System | FileAttributes.Hidden;
                    int num = 0;
                    foreach(DirectoryInfo info in di.GetDirectories()) {
                        try {
                            string fullName = info.FullName;
                            string name = info.Name;
                            if((((fullName.Length != 0x1c) || !name.PathEquals("System Volume Information")) && ((fullName.Length != 15) || !name.PathEquals("$RECYCLE.BIN"))) && ((fullName.Length != 11) || !name.PathEquals("RECYCLER"))) {
                                FileAttributes attributes2 = info.Attributes;
                                if(OSDetector.IsXP || ((attributes2 & attributes) != attributes)) {
                                    bool flag5 = (attributes2 & FileAttributes.System) != 0;
                                    bool flag6 = (attributes2 & FileAttributes.ReadOnly) != 0;
                                    bool flag7 = (attributes2 & FileAttributes.Hidden) != 0;
                                    if((!flag5 || flag2) && (!flag7 || flag3)) {
                                        bool fTruncated;
                                        string title = QTUtility2.MakeNameEllipsis(name, out fTruncated);
                                        QMenuItem item = CreateDirectoryItem(info, title, flag5 || flag6, false);
                                        if(item != null) {
                                            if(fTruncated) {
                                                item.OriginalTitle = name;
                                            }
                                            if((pathChild != null) && (item.Path == pathChild)) {
                                                item.BackColor = QTUtility2.MakeModColor(SystemColors.Highlight);
                                                pathChild = null;
                                            }
                                            list.Add(item);
                                        }
                                        else {
                                            string path = fullName;
                                            ToolStripMenuItemEx ex = new ToolStripMenuItemEx(title);
                                            ex.Exists = true;
                                            ex.SetImageReservationKey(path, null);
                                            ex.ThumbnailIndex = 0xffff + num++;
                                            ex.ThumbnailPath = path;
                                            ex.Path = path;
                                            ex.Name = name;
                                            ex.Extension = Path.GetExtension(path).ToLower();
                                            if(fTruncated) {
                                                ex.OriginalTitle = name;
                                            }
                                            ex.MouseMove += _owner.tsmi_Files_MouseMove;
                                            ex.MouseDown += _owner.tsmi_MouseDown;
                                            ex.MouseUp += _owner.tsmi_MouseUp;
                                            collection.Add(ex);
                                        }
                                    }
                                }
                            }
                        }
                        catch(Exception exception) {
                            QTLogger.MakeErrorLog(exception, "creating subdir menu");
                        }
                    }
                    if(!flag) {
                        if(_owner.tsmiComparer == null) {
                            _owner.tsmiComparer = new ToolStripMeuItemComparer();
                        }
                        list.Sort(_owner.tsmiComparer);
                    }
                    if(!Config.Tips.SubDirTipsFiles) {
                        return list;
                    }
                    int num2 = 0;
                    string str5 = ".lnk";
                    string str6 = ".url";
                    foreach(FileInfo info2 in di.GetFiles()) {
                        try {
                            FileAttributes attributes3 = info2.Attributes;
                            bool flag8 = (attributes3 & FileAttributes.System) != 0;
                            bool flag9 = (attributes3 & FileAttributes.Hidden) != 0;
                            if((!flag8 || flag2) && (!flag9 || flag3)) {
                                string fileNameWithoutExtension;
                                bool fTruncated;
                                string lnkPath = info2.FullName;
                                string str8 = lnkPath;
                                string ext = info2.Extension.ToLower();
                                if(ext == str5) {
                                    string linkTargetPath = ShellMethods.GetLinkTargetPath(lnkPath);
                                    if(!string.IsNullOrEmpty(linkTargetPath)) {
                                        DirectoryInfo diSub = new DirectoryInfo(linkTargetPath);
                                        if(diSub.Exists) {
                                            string str12 = QTUtility2.MakeNameEllipsis(Path.GetFileNameWithoutExtension(info2.Name), out fTruncated);
                                            QMenuItem item2 = CreateDirectoryItem(diSub, str12, false, true);
                                            if(item2 != null) {
                                                item2.Path = lnkPath;
                                                item2.TargetPath = linkTargetPath;
                                                item2.Name = info2.Name;
                                                item2.Extension = ext;
                                                if(fTruncated) {
                                                    item2.OriginalTitle = info2.Name;
                                                }
                                                item2.HasIcon = true;
                                                item2.SetImageReservationKey(lnkPath, ext);
                                                collection.Add(item2);
                                            }
                                            continue;
                                        }
                                        str8 = linkTargetPath;
                                    }
                                }
                                if((ext == str5) || (ext == str6)) {
                                    fileNameWithoutExtension = Path.GetFileNameWithoutExtension(info2.Name);
                                }
                                else {
                                    fileNameWithoutExtension = info2.Name;
                                }
                                ToolStripMenuItemEx ex2 = new ToolStripMenuItemEx(QTUtility2.MakeNameEllipsis(fileNameWithoutExtension, out fTruncated));
                                ex2.ThumbnailIndex = num2++;
                                ex2.ThumbnailPath = str8;
                                ex2.Path = lnkPath;
                                ex2.Name = info2.Name;
                                ex2.Extension = ext;
                                if(fTruncated) {
                                    ex2.OriginalTitle = fileNameWithoutExtension;
                                }
                                ex2.Exists = true;
                                ex2.SetImageReservationKey(lnkPath, ext);
                                ex2.MouseMove += _owner.tsmi_Files_MouseMove;
                                ex2.MouseDown += _owner.tsmi_MouseDown;
                                ex2.MouseUp += _owner.tsmi_MouseUp;
                                collection.Add(ex2);
                            }
                        }
                        catch(Exception exception2) {
                            QTLogger.MakeErrorLog(exception2, "creating subfile menu");
                        }
                    }
                    collection.Sort(_owner.extComparer);
                    list.AddRange(collection);
                }
                catch {
                }
                return list;
            }

            // TODO: clean
            internal List<QMenuItem> CreateMenuFromIDL(IDLWrapper idlw, byte[] idlChild) {
                List<QMenuItem> list = new List<QMenuItem>();
                List<QMenuItem> collection = new List<QMenuItem>();
                if(idlw.Available) {
                    IShellFolder shellFolder = null;
                    IEnumIDList ppenumIDList = null;
                    IntPtr zero = IntPtr.Zero;
                    IntPtr ptr2 = IntPtr.Zero;
                    if(idlChild != null) {
                        zero = ShellMethods.CreateIDL(idlChild);
                        ptr2 = PInvoke.ILFindLastID(zero);
                    }
                    bool dummy;
                    bool flag;
                    ShellFolderSettingsReader.GetHiddenFileSettings(out flag, out dummy);
                    int grfFlags = 0x60;
                    if(flag) {
                        grfFlags |= 0x80;
                    }
                    try {
                        IntPtr ptr3;
                        if(!ShellMethods.GetShellFolder(idlw.PIDL, out shellFolder) || (shellFolder.EnumObjects(IntPtr.Zero, grfFlags, out ppenumIDList) != 0)) {
                            return list;
                        }
                        int num2 = 0;
                        while(ppenumIDList.Next(1, out ptr3, null) == 0) {
                            IntPtr pIDL = PInvoke.ILCombine(idlw.PIDL, ptr3);
                            string str = ShellMethods.GetDisplayName(shellFolder, ptr3, false);
                            if(!string.IsNullOrEmpty(str)) {
                                uint rgfInOut = 0x60000000;
                                IntPtr[] apidl = new IntPtr[] { ptr3 };
                                if(shellFolder.GetAttributesOf(1, apidl, ref rgfInOut) == 0) {
                                    bool fTruncated;
                                    bool flag2 = (rgfInOut & 0x20000000) == 0x20000000;
                                    bool flag3 = (rgfInOut & 0x40000000) == 0x40000000;
                                    string name = ShellMethods.GetDisplayName(shellFolder, ptr3, true);
                                    string title = QTUtility2.MakeNameEllipsis(name, out fTruncated);
                                    if(flag3 && !flag2) {
                                        ToolStripMenuItemEx ex = new ToolStripMenuItemEx(title);
                                        ex.ThumbnailIndex = num2++;
                                        ex.ThumbnailPath = str;
                                        ex.Path = str;
                                        ex.Name = name;
                                        ex.Extension = Path.GetExtension(str).ToLower();
                                        if(fTruncated) {
                                            ex.OriginalTitle = name;
                                        }
                                        ex.Exists = true;
                                        ex.SetImageReservationKey(str, Path.GetExtension(str).ToLower());
                                        ex.MouseMove += _owner.tsmi_Files_MouseMove;
                                        ex.MouseDown += _owner.tsmi_MouseDown;
                                        ex.MouseUp += _owner.tsmi_MouseUp;
                                        collection.Add(ex);
                                    }
                                    else {
                                        QMenuItem item = new QMenuItem(title, flag2 ? MenuTarget.Folder : MenuTarget.File, MenuGenre.SubDirTip);
                                        if(str.Length == 3) {
                                            if(!IconManager.ImageGlobalContainsKey(str)) {
                                                IconManager.AddImageToGlobal(str, IconManager.GetIcon(pIDL));
                                            }
                                            item.ImageKey = str;
                                        }
                                        else {
                                            item.SetImageReservationKey(str, flag2 ? null : Path.GetExtension(str).ToLower());
                                        }
                                        item.Exists = flag3;
                                        item.Path = str;
                                        item.TargetPath = str;
                                        item.ForceToolTip = true;
                                        if((idlChild != null) && (shellFolder.CompareIDs((IntPtr)0x10000000, ptr3, ptr2) == 0)) {
                                            item.BackColor = QTUtility2.MakeModColor(SystemColors.Highlight);
                                        }
                                        item.IDLData = ShellMethods.GetIDLData(pIDL);
                                        item.QueryVirtualMenu += directory_FromIDL_QueryVirtualMenu;
                                        list.Add(item);
                                    }
                                }
                                if(ptr3 != IntPtr.Zero) {
                                    PInvoke.CoTaskMemFree(ptr3);
                                }
                                if(pIDL != IntPtr.Zero) {
                                    PInvoke.CoTaskMemFree(pIDL);
                                }
                            }
                        }
                        collection.Sort(_owner.extComparer);
                        list.AddRange(collection);
                    }
                    catch {
                    }
                    finally {
                        if(shellFolder != null) {
                            QTLogger.log("ReleaseComObject shellFolder");
                            Marshal.ReleaseComObject(shellFolder);
                        }
                        if(ppenumIDList != null) {
                            QTLogger.log("ReleaseComObject ppenumIDList");
                            Marshal.ReleaseComObject(ppenumIDList);
                        }
                        if(zero != IntPtr.Zero) {
                            PInvoke.CoTaskMemFree(zero);
                        }
                    }
                }
                return list;
            }

            internal List<QMenuItem> CreateParentMenu(IDLWrapper idlw, List<QMenuItem> lst) {
                if(lst == null) {
                    lst = new List<QMenuItem>();
                }
                using(IDLWrapper wrapper = idlw.GetParent()) {
                    if(!wrapper.Available || !wrapper.HasPath) {
                        return lst;
                    }
                    bool isDesktop = PInvoke.ILGetSize(wrapper.PIDL) == 2;
                    QMenuItem item = new QMenuItem(ShellMethods.GetDisplayName(wrapper.PIDL, true), MenuTarget.Folder, MenuGenre.SubDirTip);
                    if(!IconManager.ImageGlobalContainsKey(wrapper.Path)) {
                        IconManager.AddImageToGlobal(wrapper.Path, IconManager.GetIcon(wrapper.PIDL));
                    }
                    item.ImageKey = item.Path = item.TargetPath = wrapper.Path;
                    item.IDLDataChild = idlw.IDL;
                    item.PathChild = idlw.Path;
                    item.MouseMove += _owner.tsmi_Folder_MouseMove;
                    DropDownMenuDropTarget target = new DropDownMenuDropTarget(null, true, !_owner.fDesktop, false, _owner.hwndDialogParent);
                    target.SuspendLayout();
                    target.CheckOnEdgeClick = true;
                    target.MessageParent = _owner.hwndMessageReflect;
                    target.Items.Add(new ToolStripMenuItem("dummy"));
                    target.ImageList = ResourceCache.ImageListGlobal;
                    target.SpaceKeyExecute = true;
                    target.MouseLeave += _owner.ddmr_MouseLeave;
                    target.ItemRightClicked += _owner.ddmr_ItemRightClicked;
                    target.Opened += _owner.ddmr_Opened;
                    target.MenuDragEnter += _owner.ddmr_MenuDragEnter;
                    item.DropDown = target;
                    item.DropDownOpening += _owner.tsmi_DropDownOpening;
                    item.DropDownItemClicked += _owner.ddmr_ItemClicked;
                    if(wrapper.IsFileSystem) {
                        item.MouseDown += _owner.tsmi_MouseDown;
                        item.MouseUp += _owner.tsmi_MouseUp;
                        target.MouseDragMove += _owner.ddmr_MouseDragMove;
                        target.MouseUpBeforeDrop += _owner.ddmr_MouseUpBeforeDrop;
                        target.KeyUp += _owner.ddmr_KeyUp;
                        target.PreviewKeyDown += _owner.ddmr_PreviewKeyDown;
                        target.MouseScroll += _owner.ddmr_MouseScroll;
                        target.Path = wrapper.Path;
                    }
                    target.ResumeLayout();
                    lst.Add(item);
                    if(!isDesktop) {
                        CreateParentMenu(wrapper, lst);
                    }
                }
                return lst;
            }

            private void directory_FromIDL_QueryVirtualMenu(object sender, EventArgs e) {
                QMenuItem item = (QMenuItem)sender;
                string path = item.Path;
                item.MouseMove += _owner.tsmi_Folder_MouseMove;
                bool flag = (path.ToLower() == @"a:\") || (path.ToLower() == @"b:\");
                bool flag2 = (path.Length == 3) ? (!flag && new DriveInfo(path).IsReady) : true;
                if((item.Target == MenuTarget.Folder) && flag2) {
                    DropDownMenuDropTarget target = new DropDownMenuDropTarget(null, true, !_owner.fDesktop, false, _owner.hwndDialogParent);
                    target.SuspendLayout();
                    target.CheckOnEdgeClick = true;
                    target.MessageParent = _owner.hwndMessageReflect;
                    target.Items.Add(new ToolStripMenuItem("dummy"));
                    target.ImageList = ResourceCache.ImageListGlobal;
                    target.SpaceKeyExecute = true;
                    target.Path = path;
                    target.MouseLeave += _owner.ddmr_MouseLeave;
                    target.ItemRightClicked += _owner.ddmr_ItemRightClicked;
                    target.Opened += _owner.ddmr_Opened;
                    target.MouseScroll += _owner.ddmr_MouseScroll;
                    if(item.Exists) {
                        target.MouseDragMove += _owner.ddmr_MouseDragMove;
                        target.MouseUpBeforeDrop += _owner.ddmr_MouseUpBeforeDrop;
                        target.KeyUp += _owner.ddmr_KeyUp;
                        target.PreviewKeyDown += _owner.ddmr_PreviewKeyDown;
                        target.MenuDragEnter += _owner.ddmr_MenuDragEnter;
                    }
                    item.DropDown = target;
                    item.DropDownOpening += _owner.tsmi_DropDownOpening;
                    item.DropDownItemClicked += _owner.ddmr_ItemClicked;
                    target.ResumeLayout();
                }
            }

            private void directoryItem_QueryVirtualMenu(object sender, EventArgs e) {
                QMenuItem item = (QMenuItem)sender;
                item.MouseMove += _owner.tsmi_Folder_MouseMove;
                item.MouseDown += _owner.tsmi_MouseDown;
                item.MouseUp += _owner.tsmi_MouseUp;
                bool fSearchHidden;
                bool fSearchSystem;
                ShellFolderSettingsReader.GetHiddenFileSettings(out fSearchHidden, out fSearchSystem);
                bool flag3 = Config.Tips.SubDirTipsFiles;
                bool flag4;
                using(FindFile file = new FindFile(item.TargetPath, fSearchHidden, fSearchSystem)) {
                    flag4 = file.SubDirectoryExists() || (flag3 && file.SubFileExists());
                }
                if(flag4) {
                    DropDownMenuDropTarget target = new DropDownMenuDropTarget(null, true, !_owner.fDesktop, false, _owner.hwndDialogParent);
                    target.SuspendLayout();
                    target.CheckOnEdgeClick = true;
                    target.MessageParent = _owner.hwndMessageReflect;
                    target.Items.Add(new ToolStripMenuItem("dummy"));
                    target.ImageList = ResourceCache.ImageListGlobal;
                    target.SpaceKeyExecute = true;
                    target.Path = item.TargetPath;
                    target.MouseLeave += _owner.ddmr_MouseLeave;
                    target.MouseDragMove += _owner.ddmr_MouseDragMove;
                    target.MouseUpBeforeDrop += _owner.ddmr_MouseUpBeforeDrop;
                    target.ItemRightClicked += _owner.ddmr_ItemRightClicked;
                    target.Opened += _owner.ddmr_Opened;
                    target.KeyUp += _owner.ddmr_KeyUp;
                    target.PreviewKeyDown += _owner.ddmr_PreviewKeyDown;
                    target.MenuDragEnter += _owner.ddmr_MenuDragEnter;
                    target.MouseScroll += _owner.ddmr_MouseScroll;
                    item.DropDown = target;
                    item.DropDownOpening += _owner.tsmi_DropDownOpening;
                    item.DropDownItemClicked += _owner.ddmr_ItemClicked;
                    target.ResumeLayout();
                }
            }
        }
    }
}
