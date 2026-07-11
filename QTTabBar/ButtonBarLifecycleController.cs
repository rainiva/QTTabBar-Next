using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ButtonBarLifecycleController {
        private readonly IButtonBarLifecycleHost _host;

        public ButtonBarLifecycleController(IButtonBarLifecycleHost host) {
            _host = host;
        }

        public void GetBandInfo(ref DESKBANDINFO dbi) {
            if((dbi.dwMask & DBIM.ACTUAL) != 0) {
                dbi.ptActual.X = _host.GetBandSize().Width;
                dbi.ptActual.Y = _host.GetBandHeight();
            }
            if((dbi.dwMask & DBIM.INTEGRAL) != 0) {
                dbi.ptIntegral.X = -1;
                dbi.ptIntegral.Y = 10;
            }
            if((dbi.dwMask & DBIM.MAXSIZE) != 0) {
                dbi.ptMaxSize.X = -1;
                dbi.ptMaxSize.Y = _host.GetBandHeight();
            }
            if((dbi.dwMask & DBIM.MINSIZE) != 0) {
                dbi.ptMinSize.X = _host.GetMinimumBandSize().Width;
                dbi.ptMinSize.Y = _host.GetBandHeight();
            }
            if((dbi.dwMask & DBIM.MODEFLAGS) != 0) {
                dbi.dwModeFlags = DBIMF.NORMAL;
            }
            if((dbi.dwMask & DBIM.BKCOLOR) != 0) {
                dbi.dwMask &= ~DBIM.BKCOLOR;
            }
            if((dbi.dwMask & DBIM.TITLE) != 0) {
                dbi.wszTitle = null;
            }
        }

        public void InitializeComponents() {
            _host.InitializeBandComponents();
        }

        public void OnExplorerAttached() {
            try {
                _host.AttachToExplorerAndInitializeItems();
            }
            catch(Exception ex) {
                QTLogger.MakeErrorLog(ex, "QTButtonBar OnExplorerAttached");
            }
        }

        public bool TryPaintBackground(PaintEventArgs e) {
            if(System.Windows.Forms.VisualStyles.VisualStyleRenderer.IsSupported) {
                _host.DrawVisualStyleBackground(e);
                return true;
            }
            return _host.TryDrawRebarBackground(e);
        }

        public bool ShouldHaveBreak() {
            return _host.ReadBreakPreference();
        }

        public void OnShowChanged(bool fShow) {
            if(!fShow) {
                _host.PersistBreakPreference();
            }
        }
    }

    internal static class ButtonBarPluginEventController {
        public static void HandleButtonClick(object sender, IntPtr explorerHandle) {
            ToolStripItem item = (ToolStripItem)sender;
            Plugin plugin;
            if(!TryGetPlugin(item, out plugin)) return;

            try {
                ((IBarButton)plugin.Instance).OnButtonClick();
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, explorerHandle, plugin.PluginInformation.Name, "On button clicked.");
            }
        }

        public static void HandleDropDownOpening(ToolStripClasses toolStrip, object sender, IntPtr explorerHandle) {
            toolStrip.HideToolTip();
            ToolStripDropDownItem item = (ToolStripDropDownItem)sender;
            item.DropDown.SuspendLayout();
            Plugin plugin;
            if(TryGetPlugin(item, out plugin)) {
                try {
                    ((IBarDropButton)plugin.Instance).OnDropDownOpening((ToolStripDropDownMenu)item.DropDown);
                }
                catch(Exception exception) {
                    PluginManager.HandlePluginException(exception, explorerHandle, plugin.PluginInformation.Name, "On dropdwon menu is showing.");
                }
            }
            item.DropDown.ResumeLayout();
        }

        public static void HandleDropDownItemClick(object sender, ToolStripItem clickedItem, MouseButtons button, IntPtr explorerHandle) {
            ToolStripDropDownItem ownerItem = (ToolStripDropDownItem)((DropDownMenuReorderable)sender).OwnerItem;
            Plugin plugin;
            if(!TryGetPlugin(ownerItem, out plugin)) return;

            try {
                ((IBarDropButton)plugin.Instance).OnDropDownItemClick(clickedItem, button);
            }
            catch(Exception exception) {
                PluginManager.HandlePluginException(exception, explorerHandle, plugin.PluginInformation.Name,
                    button == MouseButtons.Right ? "On dropdown menu is right clicked." : "On dropdown menu is clicked.");
            }
        }

        private static bool TryGetPlugin(ToolStripItem item, out Plugin plugin) {
            plugin = null;
            string pluginID = Config.BBar.ActivePluginIDs[((int)item.Tag).HiWord() - 1];
            QTTabBarClass tabbar = TabInstanceRegistry.GetThreadTabBar();
            return tabbar != null && tabbar.pluginServer.TryGetPlugin(pluginID, out plugin);
        }
    }

    internal sealed class ButtonBarSearchController {
        internal static int GetSearchBoxWidth() {
            using(RegistryKey key = Registry.CurrentUser.OpenSubKey(RegConst.Root)) {
                return key == null ? 100 : Math.Max(Math.Min((int)key.GetValue("SearchBoxWidth", 100), 1024), 32);
            }
        }

        internal static void SetSearchBoxWidth(int width) {
            using(RegistryKey key = RegistryAccess.OpenRootCreate()) key.SetValue("SearchBoxWidth", width);
        }

        internal static void RearrangeFolderView(ShellBrowserEx shellBrowser, Action<bool> setRearranging) {
            IShellView shellView = null;
            try {
                if(shellBrowser.GetIShellBrowser().QueryActiveShellView(out shellView) != 0) return;
                IShellFolderView folderView = shellView as IShellFolderView;
                if(folderView == null) return;
                IntPtr arrange;
                if(folderView.GetArrangeParam(out arrange) != 0 || ((int)arrange & 0xffff) == 0) return;
                setRearranging(true); folderView.Rearrange(arrange); folderView.Rearrange(arrange); setRearranging(false);
            }
            catch(Exception exception) { QTLogger.MakeErrorLog(exception, "RearrangeFolderView"); }
            finally { setRearranging(false); if(shellView != null) Marshal.ReleaseComObject(shellView); }
        }

        internal static bool IncrementalSearch(ShellBrowserEx browser, List<IntPtr> hiddenItems, string input,
                Regex asterisk, Regex question, Func<IShellFolder, IntPtr, Regex, bool> displayNameMatches, Action<int> setResultCount) {
            QTTabBarClass tabBar = TabInstanceRegistry.GetThreadTabBar();
            if(tabBar == null) return false;
            AbstractListView listView = tabBar.GetListView();
            listView.HideSubDirTip(9); listView.HideThumbnailTooltip(9);
            IShellView shellView = null; IShellFolder folder = null; IntPtr rootPidl = IntPtr.Zero; bool added = false;
            try {
                if(browser.GetIShellBrowser().QueryActiveShellView(out shellView) != 0) return false;
                IFolderView folderView = shellView as IFolderView; IShellFolderView shellFolderView = shellView as IShellFolderView;
                if(folderView == null || shellFolderView == null) return false;
                IPersistFolder2 persistFolder = null;
                try { Guid iid = ExplorerGUIDs.IID_IPersistFolder2; if(folderView.GetFolder(ref iid, out persistFolder) == 0) persistFolder.GetCurFolder(out rootPidl); }
                finally { if(persistFolder != null) Marshal.ReleaseComObject(persistFolder); }
                if(rootPidl == IntPtr.Zero || !ShellMethods.GetShellFolder(rootPidl, out folder)) return false;
                int itemCount; folderView.ItemCount(SVGIO.ALLVIEW, out itemCount);
                Regex regex;
                if(input.StartsWith("/") && input.EndsWith("/")) {
                    try { regex = new Regex(input.Substring(1, input.Length - 2), RegexOptions.IgnoreCase); }
                    catch(Exception exception) { QTLogger.MakeErrorLog(exception, "ShellViewIncrementalSearch new Regex"); SoundFeedbackService.AsteriskPlay(); return false; }
                }
                else if(tabBar.pluginServer.FilterPlugin == null || !tabBar.pluginServer.FilterPlugin.QueryRegex(input, out regex) || regex == null) {
                    regex = new Regex(question.Replace(asterisk.Replace(Regex.Escape(input), ".*"), "."), RegexOptions.IgnoreCase);
                }
                bool useCore = tabBar.pluginServer.FilterCorePlugin != null;
                IFilterCore core = useCore ? tabBar.pluginServer.FilterCorePlugin : null;
                QTPlugin.Interop.IShellFolder coreFolder = useCore ? (QTPlugin.Interop.IShellFolder)folder : null;
                listView.SetRedraw(false);
                try {
                    if(!useCore && (regex.ToString().Length == 0 || regex.ToString() == ".*")) {
                        added = hiddenItems.Count > 0; foreach(IntPtr child in hiddenItems) { int ignored; shellFolderView.AddObject(child, out ignored); PInvoke.CoTaskMemFree(child); } hiddenItems.Clear();
                    }
                    else {
                        List<IntPtr> removed = new List<IntPtr>();
                        for(int i = 0; i < itemCount; i++) { IntPtr child; if(folderView.Item(i, out child) != 0) continue; bool matches = useCore ? core.IsMatch(coreFolder, child, regex) : displayNameMatches(folder, child, regex); if(matches) PInvoke.CoTaskMemFree(child); else { int ignored; removed.Add(child); if(shellFolderView.RemoveObject(child, out ignored) == 0) { itemCount--; i--; } } }
                        for(int i = hiddenItems.Count - 1; i >= 0; i--) { IntPtr child = hiddenItems[i]; bool matches = useCore ? core.IsMatch(coreFolder, child, regex) : displayNameMatches(folder, child, regex); if(!matches) continue; int ignored; hiddenItems.RemoveAt(i); shellFolderView.AddObject(child, out ignored); PInvoke.CoTaskMemFree(child); added = true; }
                        hiddenItems.AddRange(removed);
                    }
                    int count; folderView.ItemCount(SVGIO.ALLVIEW, out count); setResultCount(count); browser.SetStatusText(string.Concat(count, " / ", count + hiddenItems.Count, ResourceCache.TextResourcesDic["ButtonBar_Misc"][5]));
                }
                finally { listView.SetRedraw(true); }
            }
            catch(Exception exception) { QTLogger.MakeErrorLog(exception); added = false; }
            finally { if(shellView != null) Marshal.ReleaseComObject(shellView); if(folder != null) Marshal.ReleaseComObject(folder); if(rootPidl != IntPtr.Zero) PInvoke.CoTaskMemFree(rootPidl); }
            return added;
        }
    }
}
