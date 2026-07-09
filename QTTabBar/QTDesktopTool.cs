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
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTTabBarLib.Interop;
using IShellBrowser = QTTabBarLib.Interop.IShellBrowser;
using MSG = BandObjectLib.MSG;
using Timer = System.Windows.Forms.Timer;


namespace QTTabBarLib {
    [ComVisible(true)]
    [Guid("D2BF470E-ED1C-487F-A555-2BD8835EB6CE")]
    public sealed partial class QTDesktopTool : BandObject, IDeskBand2 {
        // FUNCTIONS  									  |	THREAD		
        // -----------------------------------------------+-----------
        // taskbar toolbar								  | taskbar
        // Menu											  | taskbar (even if shown by double-click on destop)
        // *subDirTip_TB	(real directory menu in menu) |	taskbar
        //
        // Sub Directory Menu							  |	desktop
        // Thumbnail for desktop item					  |	desktop


        #region ---------- Fields ----------

        // PLATFORM INVOKE & INTEROPS
        private IntPtr hHook_MsgDesktop;
        private IntPtr hHook_MsgShell_TrayWnd;
        private IntPtr hHook_KeyDesktop;
        private HookProc hookProc_Msg_Desktop;
        private HookProc hookProc_Msg_ShellTrayWnd;
        private HookProc hookProc_Keys_Desktop;

        private IntPtr hwndShellTray, hwndThis;
        private IntPtr hwndListViewDesktop;
        private IntPtr hwndShellViewDesktop;
        private ExtendedSysListView32 slvDesktop;
        private DesktopTooltipController _tooltipController;

        private ShellContextMenu iContextMenu2 = new ShellContextMenu(); // for taskbar thread, events handled at WndProc
        private ShellBrowserEx ShellBrowser;

        private NativeWindowController shellViewListener;


        // CONTROLS
        private System.ComponentModel.IContainer components;
        private DropDownMenuReorderable contextMenu, ddmrGroups, ddmrHistory, ddmrUserapps, ddmrRecentFile;
        private TitleMenuItem tmiLabel_Group, tmiLabel_History, tmiLabel_UserApp, tmiLabel_RecentFile;
        private TitleMenuItem tmiGroup, tmiHistory, tmiUserApp, tmiRecentFile;

        private Timer timerHooks;
        private int iHookTimeout;

        private List<ToolStripItem> lstGroupItems = new List<ToolStripItem>();
        private List<ToolStripItem> lstUndoClosedItems = new List<ToolStripItem>();
        private List<ToolStripItem> lstUserAppItems = new List<ToolStripItem>();
        private List<ToolStripItem> lstRecentFileItems = new List<ToolStripItem>();

        private ContextMenuStripEx contextMenuForSetting;

        private ToolStripMenuItem tsmiTaskBar,
                                  tsmiDesktop,
                                  tsmiLockItems,
                                  tsmiVSTitle,
                                  tsmiOnGroup,
                                  tsmiOnHistory,
                                  tsmiOnUserApps,
                                  tsmiOnRecentFile,
                                  tsmiOneClick,
                                  tsmiAppKeys;

        private ToolStripMenuItem tsmiExperimental;

        private ThumbnailTooltipForm thumbnailTooltip;
       // private Timer timer_Thumbnail, timer_ThumbnailMouseHover, timer_Thumbnail_NoTooltip;

        private SubDirTipForm subDirTip; // subDirTip_TB;
        private ShellContextMenu iContextMenu2_Desktop = new ShellContextMenu(); // for desktop thread, handled at shellViewListener_MessageCaptured
      //  private int thumbnailIndex = -1;
        private int itemIndexDROPHILITED = -1;
       // private int thumbnailIndex_Inactive = -1;
       // private Timer timer_HoverSubDirTipMenu;

        private VisualStyleRenderer bgRenderer;
        private StringFormat stringFormat;


        // SETTINGS & FLAGS		
        private bool[] ExpandState = {false, false, false, false};
        private UniqueList<int> lstItemOrder = new UniqueList<int> {0, 1, 2, 3};
        private List<bool> lstRefreshRequired = new List<bool> { true, true, true, true };

        private bool fCancelClosing;
        private bool fNowMouseHovering;

        internal IntPtr HwndListView =>
                hwndListViewDesktop != IntPtr.Zero
                        ? hwndListViewDesktop
                        : slvDesktop != null ? slvDesktop.Handle : IntPtr.Zero;

        internal IntPtr HwndShellView => hwndShellViewDesktop;


        // CONSTANTS
        private const int ITEMTYPE_COUNT = 4;
        private const int ITEMINDEX_GROUP = 0;
        private const int ITEMINDEX_RECENTTAB = 1;
        private const int ITEMINDEX_APPLAUNCHER = 2;
        private const int ITEMINDEX_RECENTFILE = 3;


        private const string TEXT_TOOLBAR = "QTTab Desktop Tool";
        private const string MENUKEY_LABEL_GROUP = "labelG";
        private const string MENUKEY_LABEL_HISTORY = "labelH";
        private const string MENUKEY_LABEL_USERAPP = "labelU";
        private const string MENUKEY_LABEL_RECENT = "labelR";

        private const string MENUKEY_ITEM_GROUP = "groupItem";
        private const string MENUKEY_ITEM_HISTORY = "historyItem";
        private const string MENUKEY_ITEM_USERAPP = "userappItem";
        private const string MENUKEY_ITEM_RECENT = "recentItem";

        private const string MENUKEY_SUBMENUS = "submenu";
        private const string MENUKEY_LABELS = "label";

        private const string TSS_NAME_GRP = "groupSep";
        private const string TSS_NAME_APP = "appSep";


        private const string CLSIDSTR_TRASHBIN = "::{645FF040-5081-101B-9F08-00AA002F954E}";

        #endregion



        public QTDesktopTool() {
            // Methods are called in this order:
            // ctor -> SetSite -> InitializeComponent -> 
            // (touches Handle property, WM_CREATE) -> OnHandleCreated -> (OnVisibleChanged)
        }


        private void InitializeComponent() {
            // handle creation
            // todo: doesn't look necessary...
            hwndThis = Handle;

            bool reorderEnabled = !Config.Desktop.LockMenu;

            components = new System.ComponentModel.Container();
            contextMenu = new DropDownMenuReorderable(components, true, false);
            contextMenuForSetting = new ContextMenuStripEx(components, true); // todo: , false);
            tmiLabel_Group = new TitleMenuItem(MenuGenre.Group, true);
            tmiLabel_History = new TitleMenuItem(MenuGenre.History, true);
            tmiLabel_UserApp = new TitleMenuItem(MenuGenre.Application, true);
            tmiLabel_RecentFile = new TitleMenuItem(MenuGenre.RecentFile, true);

            contextMenu.SuspendLayout();
            contextMenuForSetting.SuspendLayout();
            SuspendLayout();
            //
            // contextMenu
            //
            contextMenu.ProhibitedKey.Add(MENUKEY_ITEM_HISTORY);
            contextMenu.ProhibitedKey.Add(MENUKEY_ITEM_RECENT);
            contextMenu.ReorderEnabled = reorderEnabled;
            contextMenu.MessageParent = Handle;
            contextMenu.ImageList = QTUtility.ImageListGlobal;
            contextMenu.ItemClicked += dropDowns_ItemClicked;
            contextMenu.Closing += contextMenu_Closing;
            contextMenu.ReorderFinished += contextMenu_ReorderFinished;
            contextMenu.ItemRightClicked += dropDowns_ItemRightClicked;
            if(!OSDetector.IsXP) {
                contextMenu.CreateControl();
            }
            //
            // ddmrGroups 
            //
            ddmrGroups = new DropDownMenuReorderable(components, true, false);
            ddmrGroups.ReorderEnabled = reorderEnabled;
            ddmrGroups.ImageList = QTUtility.ImageListGlobal;
            ddmrGroups.ReorderFinished += dropDowns_ReorderFinished;
            ddmrGroups.ItemClicked += dropDowns_ItemClicked;
            ddmrGroups.ItemRightClicked += dropDowns_ItemRightClicked;
            //
            // tmiGroup 
            //
            tmiGroup = new TitleMenuItem(MenuGenre.Group, false) {DropDown = ddmrGroups};
            //
            // ddmrHistory
            //
            ddmrHistory = new DropDownMenuReorderable(components, true, false);
            ddmrHistory.ReorderEnabled = false;
            ddmrHistory.ImageList = QTUtility.ImageListGlobal;
            ddmrHistory.MessageParent = Handle;
            ddmrHistory.ItemClicked += dropDowns_ItemClicked;
            ddmrHistory.ItemRightClicked += dropDowns_ItemRightClicked;
            //
            // tmiHistory 
            //
            tmiHistory = new TitleMenuItem(MenuGenre.History, false);
            tmiHistory.DropDown = ddmrHistory;
            //
            // ddmrUserapps 
            //
            ddmrUserapps = new DropDownMenuReorderable(components);
            ddmrUserapps.ReorderEnabled = reorderEnabled;
            ddmrUserapps.ImageList = QTUtility.ImageListGlobal;
            ddmrUserapps.MessageParent = Handle;
            ddmrUserapps.ReorderFinished += dropDowns_ReorderFinished;
            ddmrUserapps.ItemClicked += dropDowns_ItemClicked;
            ddmrUserapps.ItemRightClicked += dropDowns_ItemRightClicked;
            //
            // tmiUserApp
            //
            tmiUserApp = new TitleMenuItem(MenuGenre.Application, false);
            tmiUserApp.DropDown = ddmrUserapps;
            //
            // ddmrRecentFile
            //
            ddmrRecentFile = new DropDownMenuReorderable(components, false, false, false);
            ddmrRecentFile.ImageList = QTUtility.ImageListGlobal;
            ddmrRecentFile.MessageParent = Handle;
            ddmrRecentFile.ItemClicked += dropDowns_ItemClicked;
            ddmrRecentFile.ItemRightClicked += dropDowns_ItemRightClicked;
            //
            // tmiRecentFile
            //
            tmiRecentFile = new TitleMenuItem(MenuGenre.RecentFile, false);
            tmiRecentFile.DropDown = ddmrRecentFile;
            //
            // contextMenuForSetting
            //
            tsmiTaskBar = new ToolStripMenuItem();
            tsmiDesktop = new ToolStripMenuItem();
            tsmiLockItems = new ToolStripMenuItem();
            tsmiVSTitle = new ToolStripMenuItem();
            tsmiTaskBar.Checked = Config.Desktop.TaskBarDblClickEnabled;
            tsmiDesktop.Checked = Config.Desktop.DesktopDblClickEnabled;
            tsmiLockItems.Checked = Config.Desktop.LockMenu;
            tsmiVSTitle.Checked = Config.Desktop.TitleBackground;

            tsmiOnGroup = new ToolStripMenuItem();
            tsmiOnHistory = new ToolStripMenuItem();
            tsmiOnUserApps = new ToolStripMenuItem();
            tsmiOnRecentFile = new ToolStripMenuItem();
            tsmiOneClick = new ToolStripMenuItem();
            tsmiAppKeys = new ToolStripMenuItem();
            tsmiOnGroup.Checked = Config.Desktop.IncludeGroup;
            tsmiOnHistory.Checked = Config.Desktop.IncludeRecentTab;
            tsmiOnUserApps.Checked = Config.Desktop.IncludeApplication;
            tsmiOnRecentFile.Checked = Config.Desktop.IncludeRecentFile;
            tsmiOneClick.Checked = Config.Desktop.OneClickMenu;
            tsmiAppKeys.Checked = Config.Desktop.EnableAppShortcuts;
            
            tsmiExperimental = new ToolStripMenuItem(QTUtility.TextResourcesDic["Misc_Strings"][6]);
            tsmiExperimental.DropDown.Items.Add(new ToolStripMenuItem(QTUtility.TextResourcesDic["Misc_Strings"][7]));
            //TODO does this respect RTL settings?
            tsmiExperimental.DropDownDirection = ToolStripDropDownDirection.Left;
            tsmiExperimental.DropDownItemClicked += tsmiExperimental_DropDownItemClicked;
            tsmiExperimental.DropDownOpening += tsmiExperimental_DropDownOpening;

            contextMenuForSetting.Items.AddRange(new ToolStripItem[] {
                    tsmiTaskBar, tsmiDesktop, new ToolStripSeparator(),
                    tsmiOnGroup, tsmiOnHistory, tsmiOnUserApps, tsmiOnRecentFile,
                    new ToolStripSeparator(),
                    tsmiLockItems, tsmiVSTitle, tsmiOneClick, tsmiAppKeys, tsmiExperimental
            });
            contextMenuForSetting.ItemClicked += contextMenuForSetting_ItemClicked;
            RefreshStringResources();

            //
            // QTCoTaskBar
            //
            ContextMenuStrip = contextMenuForSetting;
            Width = Config.Desktop.Width;
            MinSize = new Size(8, 22);
            Dock = DockStyle.Fill;
            MouseClick += desktopTool_MouseClick;
            MouseDoubleClick += desktopTool_MouseDoubleClick;

            contextMenu.ResumeLayout(false);
            contextMenuForSetting.ResumeLayout(false);
            ResumeLayout(false);
        }

        private bool ShowSubDirTip(IntPtr pIDL, int iItem, bool fSkipFocusCheck) {
            return _tooltipController.ShowSubDirTip(pIDL, iItem, fSkipFocusCheck);
        }

        private void HideSubDirTip() {
            _tooltipController.HideSubDirTip();
        }


        #region ---------- Inner Classes ----------

        private sealed class TitleMenuItem : ToolStripMenuItem {
            private static StringFormat sf;
            private static Bitmap bmpTitle;
            public static bool DrawBackground { private get; set; }

            private Bitmap bmpArrow_Cls, bmpArrow_Opn;
            public MenuGenre Genre { get; private set; }
            public bool IsOpened { get; private set; }

            public TitleMenuItem(MenuGenre genre, bool fOpened) {
                Genre = genre;
                IsOpened = fOpened;

                bmpArrow_Opn = Resources_Image.menuOpen;
                bmpArrow_Cls = Resources_Image.menuClose;

                if(sf == null)
                    Init();
            }

            private static void Init() {
                sf = new StringFormat();
                sf.Alignment = StringAlignment.Near;
                sf.LineAlignment = StringAlignment.Center;

                bmpTitle = Resources_Image.TitleBar;
            }

            protected override void Dispose(bool disposing) {
                if(bmpArrow_Opn != null) {
                    bmpArrow_Opn.Dispose();
                    bmpArrow_Opn = null;
                }
                if(bmpArrow_Cls != null) {
                    bmpArrow_Cls.Dispose();
                    bmpArrow_Cls = null;
                }

                base.Dispose(disposing);
            }

            protected override void OnPaint(PaintEventArgs e) {
                if(DrawBackground) {
                    Rectangle rct = new Rectangle(1, 0, Bounds.Width, Bounds.Height);

                    // draw background	100x24
                    e.Graphics.DrawImage(bmpTitle,
                            new Rectangle(new Point(1, 0), new Size(1, Bounds.Height)),
                            new Rectangle(Point.Empty, new Size(1, 24)), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(bmpTitle,
                            new Rectangle(new Point(2, 0), new Size(Bounds.Width - 3, 1)),
                            new Rectangle(new Point(1, 0), new Size(98, 1)), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(bmpTitle,
                            new Rectangle(new Point(Bounds.Width - 1, 0), new Size(1, Bounds.Height)),
                            new Rectangle(new Point(99, 0), new Size(1, 24)), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(bmpTitle,
                            new Rectangle(new Point(2, Bounds.Height - 1), new Size(Bounds.Width - 3, 1)),
                            new Rectangle(new Point(1, 23), new Size(98, 1)), GraphicsUnit.Pixel);
                    e.Graphics.DrawImage(bmpTitle,
                            new Rectangle(new Point(2, 1), new Size(Bounds.Width - 3, Bounds.Height - 2)),
                            new Rectangle(new Point(1, 1), new Size(98, 22)), GraphicsUnit.Pixel);

                    // draw overwrite highlight
                    if(Selected) {
                        SolidBrush sb = new SolidBrush(Color.FromArgb(96, SystemColors.Highlight));
                        e.Graphics.FillRectangle(sb, rct);
                        sb.Dispose();
                    }

                    // draw arrow
                    if(HasDropDownItems) {
                        int y = (rct.Height - 16)/2;
                        if(y < 0)
                            y = 5;
                        else
                            y += 5;

                        using(SolidBrush sb = new SolidBrush(Color.FromArgb(Selected ? 255 : 128, Color.White))) {
                            Point p = new Point(rct.Width - 15, y);
                            Point[] ps = {p, new Point(p.X, p.Y + 8), new Point(p.X + 4, p.Y + 4)};
                            e.Graphics.FillPolygon(sb, ps);
                        }
                    }

                    // draw string
                    e.Graphics.DrawString(Text, Font, Brushes.White,
                            new RectangleF(34, 2, rct.Width - 34, rct.Height - 2), sf);
                }
                else
                    base.OnPaint(e);

                // draw image		//Resources_Image.menuOpen : Resources_Image.menuClose,
                e.Graphics.DrawImage(IsOpened ? bmpArrow_Cls : bmpArrow_Opn, new Rectangle(5, 4, 16, 16));
            }

            protected override Point DropDownLocation {
                get {
                    // show dropdown in the screen where parent dropdown is contained.
                    // multi monitor support...

                    Point pnt = base.DropDownLocation;

                    if(pnt != Point.Empty && HasDropDownItems) {
                        ToolStrip tsOwner = Owner;
                                // this ToolStrip must be ToolStripDropDown. not MenuStrip or something.
                        if(tsOwner != null && !Screen.FromPoint(tsOwner.Bounds.Location).Bounds.Contains(pnt)) {
                            pnt.X = tsOwner.Bounds.X - DropDown.Width;
                        }
                    }

                    return pnt;
                }
            }
        }

        #endregion


        #region ---------- IDeskBand2 members ----------

        public void CanRenderComposited(out bool pfCanRenderComposited) {
            pfCanRenderComposited = true;
        }

        public void SetCompositionState(bool fCompositionEnabled) {
        }

        public void GetCompositionState(out bool pfCompositionEnabled) {
            pfCompositionEnabled = true;
        }

        #endregion


        #region ---------- Register / Unregister ----------


        [ComRegisterFunction]
        private static void Register(Type t) {
            const string strDesktopTool = "QTTab Desktop Tool";
            string guid = t.GUID.ToString("B");
            ComRegistrationManager.RegisterBand(guid, strDesktopTool, strDesktopTool, strDesktopTool);
            // DeskBand category
            ComRegistrationManager.RegisterImplementedCategory(guid, "{00021492-0000-0000-C000-000000000046}");
        }

        [ComUnregisterFunction]
        private static void Unregister(Type t) {
            ComRegistrationManager.UnregisterClsid(t.GUID.ToString("B"));
        }

        #endregion

    }
}
