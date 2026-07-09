using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using QTPlugin.Interop;
using QTTabBarLib.Interop;
using PInvoke = QTTabBarLib.Interop.PInvoke;
using RECT = QTTabBarLib.Interop.RECT;

namespace QTTabBarLib {
    public sealed partial class QTDesktopTool {
        internal sealed class DesktopTooltipController {
            private readonly QTDesktopTool _owner;

            public DesktopTooltipController(QTDesktopTool owner) {
                _owner = owner;
            }

            public bool ShowSubDirTip(IntPtr pIDL, int iItem, bool fSkipFocusCheck) {
                if(fSkipFocusCheck || Config.Tips.SubDirTipForInactiveWindow ||
                        _owner.HwndListView == PInvoke.GetFocus()) {
                    try {
                        string path = ShellMethods.GetDisplayName(pIDL, false);
                        if(!ShellMethods.TryMakeSubDirTipPath(ref path)) {
                            return false;
                        }

                        byte[] idl = ShellMethods.GetIDLData(pIDL);
                        FOLDERVIEWMODE folderViewMode = FOLDERVIEWMODE.FVM_ICON;
                        RECT rct = GetLVITEMRECT(_owner.HwndListView, iItem, true, folderViewMode);
                        Point pnt = new Point(rct.right - 16, rct.bottom - 16);

                        if(_owner.subDirTip == null) {
                            _owner.subDirTip = new SubDirTipForm(_owner.hwndThis, false, _owner.slvDesktop);
                            _owner.subDirTip.MenuItemClicked += _owner.subDirTip_MenuItemClicked;
                            _owner.subDirTip.MultipleMenuItemsClicked += _owner.subDirTip_MultipleMenuItemsClicked;
                            _owner.subDirTip.MenuItemRightClicked += _owner.subDirTip_MenuItemRightClicked;
                            _owner.subDirTip.MultipleMenuItemsRightClicked += _owner.subDirTip_MultipleMenuItemsRightClicked;
                        }

                        _owner.subDirTip.ShowSubDirTip(path, idl, pnt);
                        return true;
                    }
                    catch(Exception ex) {
                        QTLogger.MakeErrorLog(ex, "QTDesktopTool DesktopTooltipController ShowSubDirTip");
                    }
                }
                return false;
            }

            public void HideSubDirTip() {
                if(_owner.subDirTip != null && _owner.subDirTip.Visible) {
                    _owner.subDirTip.HideSubDirTip(false);
                }

                _owner.itemIndexDROPHILITED = -1;
            }

            internal static RECT GetLVITEMRECT(IntPtr hwndListView, int iItem, bool fSubDirTip, FOLDERVIEWMODE fvm) {
                const uint LVM_FIRST = 0x1000;
                const uint LVM_GETVIEW = (LVM_FIRST + 143);
                const uint LVM_GETITEMW = (LVM_FIRST + 75);
                const uint LVM_GETSTRINGWIDTHW = (LVM_FIRST + 87);
                const uint LVM_GETITEMSPACING = (LVM_FIRST + 51);
                const int LVIR_BOUNDS = 0;
                const int LVIR_ICON = 1;
                const int LVIR_LABEL = 2;
                const int LV_VIEW_ICON = 0x0000;
                const int LV_VIEW_DETAILS = 0x0001;
                const int LV_VIEW_LIST = 0x0003;
                const int LV_VIEW_TILE = 0x0004;
                const int LVIF_TEXT = 0x00000001;

                int view = (int)PInvoke.SendMessage(hwndListView, (int)LVM_GETVIEW, IntPtr.Zero, IntPtr.Zero);
                int code = view == LV_VIEW_DETAILS ? LVIR_LABEL : LVIR_BOUNDS;

                bool fIcon = false;
                bool fList = false;
                bool isVistaOnly = Environment.OSVersion.Version.Major == 6 &&
                        Environment.OSVersion.Version.Minor == 0;

                if(fSubDirTip) {
                    switch(view) {
                        case LV_VIEW_ICON:
                            fIcon = !isVistaOnly;
                            code = LVIR_ICON;
                            break;

                        case LV_VIEW_DETAILS:
                            code = LVIR_LABEL;
                            break;

                        case LV_VIEW_LIST:
                            if(isVistaOnly) {
                                code = LVIR_LABEL;
                            }
                            else {
                                fList = true;
                                code = LVIR_ICON;
                            }
                            break;

                        case LV_VIEW_TILE:
                            code = LVIR_ICON;
                            break;

                        default:
                            code = LVIR_BOUNDS;
                            break;
                    }
                }

                RECT rct = PInvoke.ListView_GetItemRect(hwndListView, iItem, 0, code);
                PInvoke.MapWindowPoints(hwndListView, IntPtr.Zero, ref rct, 2);

                if(fIcon) {
                    if(fvm == FOLDERVIEWMODE.FVM_THUMBNAIL || fvm == FOLDERVIEWMODE.FVM_THUMBSTRIP) {
                        rct.right -= 13;
                    }
                    else {
                        int currentIconSpacing =
                                (int)(long)PInvoke.SendMessage(hwndListView, (int)LVM_GETITEMSPACING, IntPtr.Zero, IntPtr.Zero);
                        Size sz = SystemInformation.IconSize;
                        rct.right = rct.left + (((currentIconSpacing & 0xFFFF) - sz.Width)/2) + sz.Width + 8;
                        rct.bottom = rct.top + sz.Height + 6;
                    }
                }
                else if(fList) {
                    LVITEM lvitem = new LVITEM();
                    lvitem.pszText = Marshal.AllocCoTaskMem(520);
                    lvitem.cchTextMax = 260;
                    lvitem.iItem = iItem;
                    lvitem.mask = LVIF_TEXT;
                    IntPtr pLI = Marshal.AllocCoTaskMem(Marshal.SizeOf(lvitem));
                    Marshal.StructureToPtr(lvitem, pLI, false);

                    PInvoke.SendMessage(hwndListView, (int)LVM_GETITEMW, IntPtr.Zero, pLI);

                    int w = (int)PInvoke.SendMessage(hwndListView, (int)LVM_GETSTRINGWIDTHW, IntPtr.Zero, lvitem.pszText);
                    w += 20;

                    Marshal.FreeCoTaskMem(lvitem.pszText);
                    Marshal.FreeCoTaskMem(pLI);

                    rct.right += w;
                    rct.top += 2;
                    rct.bottom += 2;
                }

                return rct;
            }
        }
    }
}
