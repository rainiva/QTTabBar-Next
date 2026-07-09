using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib
{
    /**
     internal 只有在同一程序集的文件中，内部类型或成员才可访问
     */
    public abstract partial class TabBarBase : BandObject
    {
        // 添加到分组
        protected ToolStripMenuItem tsmiAddToGroup;
        protected ToolStripMenuItem tsmiBrowseFolder;
        protected ToolStripMenuItem tsmiCloneThis;
        protected ToolStripMenuItem tsmiClose;
        protected ToolStripMenuItem tsmiCloseAllButCurrent;
        protected ToolStripMenuItem tsmiCloseAllButThis;
        protected ToolStripMenuItem tsmiCloseLeft;
        protected ToolStripMenuItem tsmiCloseRight;
        protected ToolStripMenuItem tsmiCloseWindow;
        protected ToolStripMenuItem tsmiCopy;
        protected ToolStripMenuItem tsmiCreateGroup;
        protected ToolStripMenuItem tsmiCreateWindow;
        protected ToolStripMenuItem tsmiExecuted;
        protected ToolStripMenuItem tsmiGroups;
        protected ToolStripMenuItem tsmiHistory;
        protected ToolStripMenuItem tsmiLastActiv;
        protected ToolStripMenuItem tsmiLockThis;
        protected ToolStripMenuItem tsmiLockToolbar;
        protected ToolStripMenuItem tsmiMergeWindows;
        protected ToolStripMenuItem tsmiOption;
        protected ToolStripMenuItem tsmiProp;
        protected ToolStripMenuItem tsmiTabOrder;
        protected ToolStripMenuItem tsmiUndoClose;

        /*add by qwop 2012.07.13*/
        protected ToolStripMenuItem tsmiOpenCmd;
        protected ToolStripMenuItem enableApiHook;
        /*add by qwop 2012.07.13*/

        protected ToolStripSeparator tssep_Sys1;
        protected ToolStripSeparator tssep_Sys2;
        protected ToolStripSeparator tssep_Tab1;
        protected ToolStripSeparator tssep_Tab2;
        protected ToolStripSeparator tssep_Tab3;
        protected ContextMenuStripEx contextMenuSys;
        protected ContextMenuStripEx contextMenuTab;

        internal virtual bool IsBottomBar()
        {
            return false;
        }

        internal virtual bool IsVertical()
        {
            return false;
        }
        // internal abstract TargetView TargetView { get; }
        protected Color HorizontalExplorerBarBackgroundColor
        {
            get
            {
                if (Config.Skin.DrawHorizontalExplorerBarBgColor)
                    return ShellColors.ExplorerBarHrztBGColor;
                return !QTUtility.InNightMode ? ShellColors.ExplorerBarHrztBGColor : ShellColors.Default;
            }
        }

        protected Color VerticalExplorerBarBackgroundColor
        {
            get
            {
                if (Config.Skin.DrawVerticalExplorerBarBgColor)
                {
                    return ShellColors.ExplorerBarVertBGColor;
                }
                return !QTUtility.InNightMode ? ShellColors.ExplorerBarVertBGColor : ShellColors.Default;
            }
        }

        protected abstract bool IsTabSubFolderMenuVisible { get; }
        protected abstract int CalcBandHeight(int count);

        // Must be the single field used by SyncTravelState and derived bars.
        // Do not redeclare in QTTabBarClass — that shadows this and leaves it null.
        public QTabControl tabControl1;

        #region --- Shared Fields (moved from QTTabBarClass / QTSecondViewBar) ---

        public RebarController rebarController;
        protected string CurrentAddress;
        protected QTabItem CurrentTab;
        protected int BandHeight;
        public static int BandHeightSpace = 3;
        protected ShellBrowserEx ShellBrowser;

        protected List<QTabItem> lstActivatedTabs = new List<QTabItem>(0x10);
        protected IntPtr ExplorerHandle;
        protected Dictionary<int, ITravelLogEntry> LogEntryDic = new Dictionary<int, ITravelLogEntry>();
        protected AbstractListView listView = new AbstractListView();
        protected ListViewMonitor listViewManager;

        protected ITravelLogStg TravelLog;
        public QTTabBarClass.PluginServer pluginServer { get; set; }

        protected bool NavigatedByCode;

        protected bool NowTabsAddingRemoving;
        protected bool NowInTravelLog;
        protected bool NowModalDialogShown;
        protected bool NowTabCloned;
        protected bool NowTabCreated;
        protected bool fNavigatedByTabSelection;
        protected int CurrentTravelLogIndex;
        protected int navBtnsFlag;
        // TODO add fields
        protected ToolStripClasses toolStrip;
        protected ToolStripButton buttonBack;
        protected ToolStripButton buttonForward;
        protected ToolStripDropDownButton buttonNavHistoryMenu;
        protected IntPtr TravelToolBarHandle;

        #endregion

        #region --- Shared Methods (deduplicated from QTTabBarClass / QTSecondViewBar) ---

        protected static bool TryCallButtonBar(Action<QTButtonBar> action)
        {
            QTButtonBar bbar = ButtonBarRegistry.GetThreadButtonBar();
            if (bbar == null) return false;
            action(bbar);
            return true;
        }

        protected static bool IsSearchResultFolder(string path)
        {
            return NavigationHelper.IsSearchResultFolder(path);
        }

        protected void AddToHistory(QTabItem closingTab)
        {
            string currentPath = closingTab.CurrentPath;
            if ((Config.Misc.KeepHistory && !string.IsNullOrEmpty(currentPath)) && !IsSearchResultFolder(currentPath))
            {
                if (QTUtility2.IsShellPathButNotFileSystem(currentPath) && (currentPath.IndexOf("???") == -1))
                {
                    currentPath = currentPath + "???" + closingTab.GetLogHash(true, 0);
                }
                StaticReg.ClosedTabHistoryList.Add(currentPath);
                InstanceManager.ButtonBarBroadcast(bbar => bbar.RefreshButtons(), true);
            }
        }

        protected void ShowMessageNavCanceled(string failedPath, bool fModal)
        {
            QTUtility2.log("QTTabBarClass ShowMessageNavCanceled: " + failedPath);
            QTUtility2.MakeErrorLog(null, string.Format("Failed navigation: {0}", failedPath));
            if (Config.Window.ShowFailNavMsg)
            {
                MessageForm.Show(ExplorerHandle,
                    string.Format(QTUtility.TextResourcesDic["TabBar_Message"][0], failedPath),
                    string.Empty,
                    MessageBoxIcon.Asterisk,
                    0x2710,
                    fModal);
            }
        }

        protected bool IsSpecialFolderNeedsToTravel(string path)
        {
            int index = path.IndexOf("*?*?*");
            if (index != -1)
            {
                path = path.Substring(0, index);
            }
            if (!IsSearchResultFolder(path))
            {
                if (path.PathEquals("::{13E7F612-F261-4391-BEA2-39DF4F3FA311}"))
                {
                    return true;
                }
                if (!path.PathStartsWith(QTUtility.ResMisc[0]) && (!path.EndsWith(QTUtility.ResMisc[0], StringComparison.OrdinalIgnoreCase) || Path.IsPathRooted(path)))
                {
                    return false;
                }
            }
            return true;
        }

        protected bool NavigateToPastSpecialDir(int hash)
        {
            IEnumTravelLogEntry ppenum = null;
            try
            {
                ITravelLogEntry entry2;
                if (TravelLog.EnumEntries(0x31, out ppenum) != 0)
                {
                    goto Label_007C;
                }
            Label_0013:
                do
                {
                    if (ppenum.Next(1, out entry2, 0) != 0)
                    {
                        goto Label_007C;
                    }
                    if (entry2 != LogEntryDic[hash])
                    {
                        goto Label_0057;
                    }
                }
                while (TravelLog.TravelTo(entry2) != 0);
                NowInTravelLog = true;
                CurrentTravelLogIndex++;
                return true;
            Label_0057:
                if (entry2 != null)
                {
                    Marshal.ReleaseComObject(entry2);
                }
                goto Label_0013;
            }
            catch (Exception exception)
            {
                QTUtility2.MakeErrorLog(exception);
            }
            finally
            {
                if (ppenum != null)
                {
                    QTUtility2.log("ReleaseComObject ppenum");
                    Marshal.ReleaseComObject(ppenum);
                }
            }
        Label_007C:
            return false;
        }

        protected void SyncTravelState()
        {
            if (CurrentTab != null)
            {
                navBtnsFlag = ((CurrentTab.HistoryCount_Back > 1) ? 1 : 0) | ((CurrentTab.HistoryCount_Forward > 0) ? 2 : 0);
                if (Config.Tabs.ShowNavButtons && (toolStrip != null))
                {
                    buttonBack.Enabled = (navBtnsFlag & 1) != 0;
                    buttonForward.Enabled = (navBtnsFlag & 2) != 0;
                    buttonNavHistoryMenu.Enabled = navBtnsFlag != 0;
                }
                TryCallButtonBar(bbar => bbar.RefreshButtons());
                if(tabControl1 != null) {
                    QTabItem.CheckSubTexts(tabControl1);
                }
                SyncToolbarTravelButton();
            }
        }

        protected void SyncToolbarTravelButton()
        {
            if (!OSDetector.IsXP)
            {
                IntPtr ptr = (IntPtr)0x10001;
                IntPtr ptr2 = (IntPtr)0x10000;
                bool flag = (navBtnsFlag & 1) != 0;
                bool flag2 = (navBtnsFlag & 2) != 0;
                PInvoke.SendMessage(TravelToolBarHandle, 0x401, (IntPtr)0x100, flag ? ptr : ptr2);
                PInvoke.SendMessage(TravelToolBarHandle, 0x401, (IntPtr)0x101, flag2 ? ptr : ptr2);
                PInvoke.SendMessage(TravelToolBarHandle, 0x401, (IntPtr)0x102, (flag || flag2) ? ptr : ptr2);
            }
        }

        protected void tabControl1_RowCountChanged(object sender, QEventArgs e)
        {
            SetBarRows(e.RowCount);
        }

        /// <summary>
        /// Computes physical band height for Explorer rebar.
        /// At high DPI (e.g. 200%), logical TabHeight must be scaled or the bar
        /// collapses to ~half height and tab titles are clipped.
        /// </summary>
        public static int ComputeBandHeight(int rowCount, int tabHeight, float dpiScale)
        {
            int rows = rowCount < 1 ? 1 : rowCount;
            float scale = dpiScale <= 0f ? 1f : dpiScale;
            return Graphic.ScaleBy(scale, rows * tabHeight + BandHeightSpace);
        }

        /// <summary>
        /// Picks the best DPI scale for band/tab sizing.
        /// DPI-unaware Explorer often reports GetDeviceCaps/GetDpiForWindow=96
        /// while the user display is 150%/200% (AppliedDPI 144/192). Prefer a
        /// real per-monitor window/process DPI when it is above 96; otherwise
        /// take the max of device-caps and AppliedDPI so titles are not clipped.
        /// </summary>
        public static float ResolveDpiScale(int windowDpi, int processDpi, int deviceCapsDpi, int appliedDpi)
        {
            // Genuine per-monitor awareness reports values above 96.
            if(windowDpi > 96) {
                return windowDpi / 96f;
            }
            if(processDpi > 96) {
                return processDpi / 96f;
            }
            int dpi = 0;
            if(windowDpi > 0) {
                dpi = Math.Max(dpi, windowDpi);
            }
            if(processDpi > 0) {
                dpi = Math.Max(dpi, processDpi);
            }
            if(deviceCapsDpi > 0) {
                dpi = Math.Max(dpi, deviceCapsDpi);
            }
            if(appliedDpi > 0) {
                dpi = Math.Max(dpi, appliedDpi);
            }
            if(dpi > 0) {
                return dpi / 96f;
            }
            return 1f;
        }

        public static int TryReadAppliedDpi()
        {
            try {
                using(RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Control Panel\Desktop\WindowMetrics", false)) {
                    if(key != null) {
                        object value = key.GetValue("AppliedDPI");
                        if(value is int) {
                            return (int)value;
                        }
                        int parsed;
                        if(value != null && int.TryParse(value.ToString(), out parsed)) {
                            return parsed;
                        }
                    }
                }
            }
            catch {
                // Registry may be unavailable in some hosts.
            }
            return 0;
        }

        protected float GetBandDpiScale()
        {
            int windowDpi = 0;
            try {
                if(IsHandleCreated) {
                    windowDpi = PInvoke.GetDpiForWindow(Handle);
                }
            }
            catch {
                // Fall through.
            }
            int deviceCapsDpi = 0;
            try {
                IntPtr hdc = PInvoke.GetDC(IntPtr.Zero);
                if(hdc != IntPtr.Zero) {
                    deviceCapsDpi = PInvoke.GetDeviceCaps(hdc, 88); // LOGPIXELSX
                    PInvoke.ReleaseDC(IntPtr.Zero, hdc);
                }
            }
            catch {
                // ignore
            }
            return ResolveDpiScale(windowDpi, Dpi, deviceCapsDpi, TryReadAppliedDpi());
        }

        protected void SetBarRows(int count)
        {
            BandHeight = ComputeBandHeight(count, Config.Skin.TabHeight, GetBandDpiScale());
            try {
                Height = BandHeight;
                MinSize = new Size(MinSize.Width, BandHeight);
            }
            catch {
                // Handle may not be ready during early construction.
            }
            if (null != rebarController)
            {
                rebarController.RefreshHeight();
            }
        }

        #endregion

    }
}
