using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QTTabBarLib.FileRename;
using QTTabBarLib.Interop;

namespace QTTabBarLib
{
    internal sealed class ExplorerManager : NativeWindow, IDisposable, ICommandInvokerWindow
    {
        [ThreadStatic]
        public static ExplorerManager ThreadInstance = null;
        [ThreadStatic]
        public static IntPtr ThreadExplorerHandle = IntPtr.Zero;
        [ThreadStatic]
        private static int windowDpi = 0;
        [ThreadStatic]
        public static bool StartUpProcessComplete = false;
        // private IBandSite bandSite;
        // private ShellFolderView shellFolderView;
        // private NavigationPane navPane;
        // private NavigationPane navigationPaneMouseEvent;
        // private ToolbarImageHelper toolbarImageHelper;
        // private PluginManager pluginManager;
        private byte[] currentIDL = new byte[2];
        // private SyncControl syncControl;
        public static bool fNowActivateNofocusExpected = false;
        private bool fTreeViewColorPending = true;
        [ThreadStatic]
        public static bool CurrentExplorerIsRooted = false;
        // private static ResourceCache<BmpCacheKey, Bitmap> watermarkImageCache = new ResourceCache<BmpCacheKey, Bitmap>(new Func<BmpCacheKey, Bitmap>(KeyResourceConverters.ToBitmap));
        private const int POLLINGINTERVAL_SHRINK = 333;
        private static int FOLDERBAND_HEIGHT = 34;
        // private static ConcurrentDictionary<ExplorerManager, SHOWWINDOW> dicNotifyIcon;
        public static bool fMergingAllWindow = false;
        private ExplorerManager.ToolbarManager toolbarManager = null;
        // private ExtraViewResizer extraViewResizer;
        // private Rebar rebar;
        // private static OptionDialog optionsDialog;
        // private static RefreshProcessInfo refreshProcessInfo;
        // private Dictionary<int, EventData> dicUserEventData;
        internal QTabItem tabDragSourceInTheWindow = null;
        // public IList<ItemIDList> PendingFoldersDefault;
        public bool PendingFoldersDefaultProcessed = false;
        public IList<string> PendingGroupsDefault = null;
        public IList<string> PendingGroupsExtraView = null;
        // public IList<ItemIDList> PendingFoldersExtraView;
        // public CommandInfo PendingCommandExtraView;
        public QTabItem PendingTabExtraView = null;
        public QTabItem PendingModifyTabExtraView2nd = null;
        public QTabItem PendingModifyTabExtraView3rd = null;
        public QTabItem PendingTabDefaultView = null;
        public bool StartUpSelectionPendingExtraView = false;

        public ExplorerManager.ToolbarManager Toolbars
        {
            get
            {
                return this.toolbarManager;
            }
        }

        public static int WindowDpi
        {
            get
            {
                return ExplorerManager.windowDpi != 0 ? ExplorerManager.windowDpi : 96;
            }
        }

        public IntPtr CommandWindowHandle { get; private set; }

        public static float WindowScaling
        {
            get
            {
                return ExplorerManager.windowDpi != 0 ? (float)ExplorerManager.windowDpi / 96f : 1f;
            }
        }


        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public sealed class ToolbarManager
        {
            private ExplorerManager explorerManager;
            // private QCommandBar commandBar1st;
            // private QCommandBar2nd commandBar2nd;
            // private QCommandBarVertical commandBarVrt;
            // private QManagementBar managementBar;
            // private QThirdViewBar thirdViewBar;
            private bool fNowHiding3rdViewBar;

            public ToolbarManager(ExplorerManager explorerManager)
            {
                this.explorerManager = explorerManager;
            }

             public void Show(Toolbar toolbar, bool fShow, bool fSaveBandLayout = false)
             {
        try
        {
          string str = (string) null;
          switch (toolbar)
          {
            case Toolbar.TabBar:
              str = typeof (QTTabBarClass).GUID.ToString("B");
              break;
            case Toolbar.CommandBar1:
              // str = typeof (QCommandBar).GUID.ToString("B");
              break;
            case Toolbar.CommandBar2:
              // str = typeof (QCommandBar2nd).GUID.ToString("B");
              break;
            case Toolbar.CommandBarVertical:
              // if (this.IsThirdViewBarVisible && !this.fNowHiding3rdViewBar)
              // {
              //   if (fShow)
              //   {
              //     this.ThirdViewBar.ShowVerticalCommandBar();
              //     return;
              //   }
              //   this.ThirdViewBar.HideVerticalCommandBar();
              //   return;
              // }
              // this.save3rdViewBarWidth();
              // fSaveBandLayout = false;
              // str = typeof (QCommandBarVertical).GUID.ToString("B");
              break;
            case Toolbar.BottomTabBar:
              fSaveBandLayout = false;
              // str = typeof (QTHorizontalExplorerBar).GUID.ToString("B");
              break;
            case Toolbar.ManagementBar:
              fSaveBandLayout = false;
              // if (!fShow)
              //   this.NowHidingManagementBarByCode = true;
              // str = typeof (QManagementBar).GUID.ToString("B");
              break;
            case Toolbar.SecondViewBar:
              fSaveBandLayout = false;
              // str = typeof (QSecondViewBar).GUID.ToString("B");
              break;
            case Toolbar.ThirdViewBar:
              fSaveBandLayout = false;
              // str = typeof (QThirdViewBar).GUID.ToString("B");
              break;
          }
          if (str != null)
          {
            object obj1 = (object) str;
            object obj2 = (object) fShow;
            /*try
            {
              // ISSUE: variable of a compiler-generated type
              SHDocVw.WebBrowser explorer = this.explorerManager.explorer;
              ref object local1 = ref obj1;
              ref object local2 = ref obj2;
              object missing = Type.Missing;
              ref object local3 = ref missing;
              // ISSUE: reference to a compiler-generated method
              explorer.ShowBrowserBar(ref local1, ref local2, ref local3);
            }
            catch (Exception ex)
            {
            }*/
          }
          if (!fSaveBandLayout)
            return;
          // this.explorerManager.SaveCurrentBandLayout();
        }
        finally
        {
          this.fNowHiding3rdViewBar = false;
        }
      }

        }
        
    }

}
