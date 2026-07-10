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
    public sealed partial class QTDesktopTool {
        #region ---------- Settings ----------

        private void ReadSetting() {
            lstItemOrder.Clear();
            lstItemOrder.Add(ValidationHelper.ValidateMinMax(Config.Desktop.FirstItem, 0, ITEMTYPE_COUNT));
            lstItemOrder.Add(ValidationHelper.ValidateMinMax(Config.Desktop.SecondItem, 0, ITEMTYPE_COUNT));
            lstItemOrder.Add(ValidationHelper.ValidateMinMax(Config.Desktop.ThirdItem, 0, ITEMTYPE_COUNT));
            lstItemOrder.Add(ValidationHelper.ValidateMinMax(Config.Desktop.FourthItem, 0, ITEMTYPE_COUNT));
            for(int i = 0; i < ITEMTYPE_COUNT; i++) {
                if(!lstItemOrder.Contains(i)) {
                    lstItemOrder.Add(i);
                }
            }
            ExpandState[0] = Config.Desktop.GroupExpanded;
            ExpandState[1] = Config.Desktop.RecentTabExpanded;
            ExpandState[2] = Config.Desktop.ApplicationExpanded;
            ExpandState[3] = Config.Desktop.RecentFileExpanded;
        }

        private void SaveSetting() {
            ConfigManager.MutateAndCommit(config => {
                config.desktop.FirstItem = lstItemOrder[0];
                config.desktop.SecondItem = lstItemOrder[1];
                config.desktop.ThirdItem = lstItemOrder[2];
                config.desktop.FourthItem = lstItemOrder[3];
                config.desktop.GroupExpanded = ExpandState[0];
                config.desktop.RecentTabExpanded = ExpandState[1];
                config.desktop.ApplicationExpanded = ExpandState[2];
                config.desktop.RecentFileExpanded = ExpandState[3];
                config.desktop.TaskBarDblClickEnabled = tsmiTaskBar.Checked;
                config.desktop.DesktopDblClickEnabled = tsmiDesktop.Checked;
                config.desktop.LockMenu = tsmiLockItems.Checked;
                config.desktop.TitleBackground = tsmiVSTitle.Checked;
                config.desktop.IncludeGroup = tsmiOnGroup.Checked;
                config.desktop.IncludeRecentTab = tsmiOnHistory.Checked;
                config.desktop.IncludeApplication = tsmiOnUserApps.Checked;
                config.desktop.IncludeRecentFile = tsmiOnRecentFile.Checked;
                config.desktop.OneClickMenu = tsmiOneClick.Checked;
                config.desktop.EnableAppShortcuts = tsmiAppKeys.Checked;
                config.desktop.Width = Width;
            }, ConfigCommitScope.DesktopOnly);
        }

        private void contextMenuForSetting_ItemClicked(object sender, ToolStripItemClickedEventArgs e) {
            if(e.ClickedItem is ToolStripSeparator)
                return;

            if(e.ClickedItem == tsmiTaskBar) {
                tsmiTaskBar.Checked = !tsmiTaskBar.Checked;
            }
            else if(e.ClickedItem == tsmiDesktop) {
                tsmiDesktop.Checked = !tsmiDesktop.Checked;
            }
            else if(e.ClickedItem == tsmiLockItems) {
                tsmiLockItems.Checked = !tsmiLockItems.Checked;

                contextMenu.ReorderEnabled =
                        ddmrGroups.ReorderEnabled = !tsmiLockItems.Checked;
                // todo
                //ddmrUserapps.SetChildrenReorderEnabled(!tsmiLockItems.Checked);
            }

            else if(e.ClickedItem == tsmiOnGroup) {
                tsmiOnGroup.Checked = !tsmiOnGroup.Checked;

                lstRefreshRequired[ITEMINDEX_GROUP] = true;
            }
            else if(e.ClickedItem == tsmiOnHistory) {
                tsmiOnHistory.Checked = !tsmiOnHistory.Checked;

                lstRefreshRequired[ITEMINDEX_RECENTTAB] = true;
            }
            else if(e.ClickedItem == tsmiOnUserApps) {
                tsmiOnUserApps.Checked = !tsmiOnUserApps.Checked;

                lstRefreshRequired[ITEMINDEX_APPLAUNCHER] = true;
            }
            else if(e.ClickedItem == tsmiOnRecentFile) {
                tsmiOnRecentFile.Checked = !tsmiOnRecentFile.Checked;

                lstRefreshRequired[ITEMINDEX_RECENTFILE] = true;
            }

            else if(e.ClickedItem == tsmiVSTitle) {
                tsmiVSTitle.Checked = !tsmiVSTitle.Checked;

                TitleMenuItem.DrawBackground = tsmiVSTitle.Checked;
            }
            else if(e.ClickedItem == tsmiOneClick) {
                tsmiOneClick.Checked = !tsmiOneClick.Checked;
            }
            else if(e.ClickedItem == tsmiAppKeys) {
                tsmiAppKeys.Checked = !tsmiAppKeys.Checked;
            }

            SaveSetting();
        }

        private void RefreshStringResources() {
            string[] ResTaskbar = ResourceCache.TextResourcesDic["TaskBar_Menu"];

            tsmiTaskBar.Text = ResTaskbar[0];
            tsmiDesktop.Text = ResTaskbar[1];
            tsmiLockItems.Text = ResTaskbar[2];
            tsmiVSTitle.Text = ResTaskbar[3];
            tsmiOneClick.Text = ResTaskbar[4];
            tsmiAppKeys.Text = ResTaskbar[5];

            string[] titles = ResourceCache.TextResourcesDic["TaskBar_Titles"];

            tsmiOnGroup.Text =
                    tmiLabel_Group.Text =
                            tmiGroup.Text = titles[0];

            tsmiOnHistory.Text =
                    tmiHistory.Text =
                            tmiLabel_History.Text = titles[1];

            tsmiOnUserApps.Text =
                    tmiUserApp.Text =
                            tmiLabel_UserApp.Text = titles[2];

            tsmiOnRecentFile.Text =
                    tmiRecentFile.Text =
                            tmiLabel_RecentFile.Text = titles[3];
        }

        #endregion
    }
}
