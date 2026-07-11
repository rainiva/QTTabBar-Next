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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using Microsoft.Win32;
using QTPlugin;
using QTTabBarLib.Interop;
using SHDocVw;
using Timer = System.Windows.Forms.Timer;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using QTTabBarLib.Common;
using Control = System.Windows.Forms.Control;
using IShellFolder = QTTabBarLib.Interop.IShellFolder;
using IShellView = QTTabBarLib.Interop.IShellView;
using ToolTip = System.Windows.Forms.ToolTip;
using System.Management;
using IDataObject = System.Runtime.InteropServices.ComTypes.IDataObject;
namespace QTTabBarLib {
        /// <summary>
        /// Explorer interaction & navigation controller (Task 28 / Batch 13 extracted from QTTabBarClass).
        /// Uses role-specific host contracts for Explorer interaction and navigation.
        /// </summary>
        internal partial class ExplorerController {
            private readonly IExplorerSessionRestoreHost _sessionRestoreHost;
            private readonly IExplorerWindowCaptureHost _windowCaptureHost;
            private readonly IExplorerTravelLogHost _travelLogHost;
            private readonly IExplorerWindowMessageHost _windowMessageHost;
            private readonly IExplorerMessageRoutingHost _messageRoutingHost;
            private readonly IExplorerNavigationHost _navigationHost;
            private readonly IExplorerAttachmentHost _attachmentHost;
            private readonly IExplorerNavigationButtonHost _navigationButtonHost;
            private readonly IExplorerHookInstallationHost _hookInstallationHost;
            private readonly IExplorerTravelToolbarHost _travelToolbarHost;
            private readonly IExplorerNavigationLifecycleHost _navigationLifecycleHost;
            private readonly IExplorerComEventHost _comEventHost;
            private readonly IExplorerLockedTabNavigationHost _lockedTabNavigationHost;
            private readonly IExplorerSpecialTravelLogHost _specialTravelLogHost;
            private readonly IExplorerNavigationCleanupHost _navigationCleanupHost;
            private readonly IExplorerPostNavigationHost _postNavigationHost;
            private readonly IExplorerShutdownNavigationHost _shutdownNavigationHost;
            private readonly IExplorerLegacyNavigationHost _legacyNavigationHost;
            private readonly IExplorerTooltipHost _tooltipHost;
            private readonly IExplorerSelectionRestoreHost _selectionRestoreHost;
            private readonly IExplorerNavigationStateHost _navigationStateHost;
            private readonly IExplorerNavigationCompleteHost _navigationCompleteHost;

            internal ExplorerController(IExplorerIntegrationHost integrationHost) : this((QTTabBarClass)integrationHost) { }

            internal ExplorerController(QTTabBarClass owner) {
                _sessionRestoreHost = owner;
                _windowCaptureHost = owner;
                _travelLogHost = owner;
                _windowMessageHost = owner;
                _messageRoutingHost = owner;
                _navigationHost = owner;
                _attachmentHost = owner;
                _navigationButtonHost = owner;
                _hookInstallationHost = owner;
                _travelToolbarHost = owner;
                _navigationLifecycleHost = owner;
                _comEventHost = owner;
                _lockedTabNavigationHost = owner;
                _specialTravelLogHost = owner;
                _navigationCleanupHost = owner;
                _postNavigationHost = owner;
                _shutdownNavigationHost = owner;
                _legacyNavigationHost = owner;
                _tooltipHost = owner;
                _selectionRestoreHost = owner;
                _navigationStateHost = owner;
                _navigationCompleteHost = owner;
            }

            private ExplorerSessionRestoreController _sessionRestore;
            private ExplorerCommandDispatcher _commandDispatch;
            private ExplorerTravelLogController _travelLogController;
            private ExplorerWindowMessageController _windowMessageController;
            private ExplorerMessageRoutingController _messageRoutingController;
            private ExplorerNavigationController _navigationController;
            private ExplorerAttachmentController _attachmentController;
            private ExplorerNavigationButtonController _navigationButtonController;
            private ExplorerHookInstallationController _hookInstallationController;
            private ExplorerTravelToolbarController _travelToolbarController;
            private ExplorerNavigationLifecycleController _navigationLifecycleController;
            private ExplorerComEventController _comEventController;
            private ExplorerLockedTabNavigationController _lockedTabNavigationController;
            private ExplorerSpecialTravelLogController _specialTravelLogController;
            private ExplorerNavigationCleanupController _navigationCleanupController;
            private ExplorerPostNavigationController _postNavigationController;
            private ExplorerShutdownNavigationController _shutdownNavigationController;
            private ExplorerLegacyNavigationController _legacyNavigationController;
            private ExplorerTooltipController _tooltipController;
            private ExplorerSelectionRestoreController _selectionRestoreController;
            private ExplorerNavigationStateController _navigationStateController;

            internal ExplorerSessionRestoreController SessionRestore =>
                _sessionRestore ?? (_sessionRestore = new ExplorerSessionRestoreController(
                    _sessionRestoreHost,
                    InstallHooks,
                    NavigateAfterInstallation));

            internal ExplorerCommandDispatcher CommandDispatch =>
                _commandDispatch ?? (_commandDispatch = new ExplorerCommandDispatcher(_windowCaptureHost));

            private ExplorerTravelLogController TravelLogController =>
                _travelLogController ?? (_travelLogController = new ExplorerTravelLogController(_travelLogHost));

            private ExplorerWindowMessageController WindowMessageController =>
                _windowMessageController ?? (_windowMessageController = new ExplorerWindowMessageController(
                    _windowMessageHost,
                    NavigateCurrentTab,
                    BeforeNavigate,
                    RouteExplorerWindowMessage));

            private ExplorerMessageRoutingController MessageRoutingController =>
                _messageRoutingController ?? (_messageRoutingController = new ExplorerMessageRoutingController(_messageRoutingHost));

            private ExplorerNavigationController NavigationController =>
                _navigationController ?? (_navigationController = new ExplorerNavigationController(
                    _navigationHost,
                    CancelFailedNavigation));

            private ExplorerAttachmentController AttachmentController =>
                _attachmentController ?? (_attachmentController = new ExplorerAttachmentController(
                    _attachmentHost,
                    Explorer_BeforeNavigate2,
                    Explorer_NavigateComplete2));

            private ExplorerNavigationButtonController NavigationButtonController =>
                _navigationButtonController ?? (_navigationButtonController = new ExplorerNavigationButtonController(
                    _navigationButtonHost,
                    NavigationButtons_Click));

            private ExplorerHookInstallationController HookInstallationController =>
                _hookInstallationController ?? (_hookInstallationController = new ExplorerHookInstallationController(
                    _hookInstallationHost,
                    explorerController_MessageCaptured,
                    TravelToolbarMessageCaptured));

            private ExplorerTravelToolbarController TravelToolbarController =>
                _travelToolbarController ?? (_travelToolbarController = new ExplorerTravelToolbarController(
                    _travelToolbarHost));

            private ExplorerNavigationLifecycleController NavigationLifecycleController =>
                _navigationLifecycleController ?? (_navigationLifecycleController = new ExplorerNavigationLifecycleController(
                    _navigationLifecycleHost));

            private ExplorerComEventController ComEventController =>
                _comEventController ?? (_comEventController = new ExplorerComEventController(
                    _comEventHost,
                    DoFirstNavigation));

            private ExplorerLockedTabNavigationController LockedTabNavigationController =>
                _lockedTabNavigationController ?? (_lockedTabNavigationController = new ExplorerLockedTabNavigationController(
                    _lockedTabNavigationHost));

            private ExplorerSpecialTravelLogController SpecialTravelLogController =>
                _specialTravelLogController ?? (_specialTravelLogController = new ExplorerSpecialTravelLogController(
                    _specialTravelLogHost));

            private ExplorerNavigationCleanupController NavigationCleanupController =>
                _navigationCleanupController ?? (_navigationCleanupController = new ExplorerNavigationCleanupController(_navigationCleanupHost));

            private ExplorerPostNavigationController PostNavigationController =>
                _postNavigationController ?? (_postNavigationController = new ExplorerPostNavigationController(_postNavigationHost));

            private ExplorerShutdownNavigationController ShutdownNavigationController =>
                _shutdownNavigationController ?? (_shutdownNavigationController = new ExplorerShutdownNavigationController(_shutdownNavigationHost));

            private ExplorerLegacyNavigationController LegacyNavigationController =>
                _legacyNavigationController ?? (_legacyNavigationController = new ExplorerLegacyNavigationController(_legacyNavigationHost));

            private ExplorerTooltipController TooltipController =>
                _tooltipController ?? (_tooltipController = new ExplorerTooltipController(_tooltipHost));

            private ExplorerSelectionRestoreController SelectionRestoreController =>
                _selectionRestoreController ?? (_selectionRestoreController = new ExplorerSelectionRestoreController(_selectionRestoreHost));

            private ExplorerNavigationStateController NavigationStateController =>
                _navigationStateController ?? (_navigationStateController = new ExplorerNavigationStateController(_navigationStateHost));

            private void NavigateAfterInstallation(object locationUrl) {
                Explorer_NavigateComplete2(null, ref locationUrl);
            }

            #region BeforeNavigate / navigation core

            // This function is used as a more available version of BeforeNavigate2.
            // Return true to suppress the navigation.  Target IDL should not be relied
            // upon; it's not guaranteed to be accurate.
            public bool BeforeNavigate(IDLWrapper target, bool autonav) {
                return NavigationLifecycleController.BeforeNavigate(target, autonav);
            }

            public void CancelFailedNavigation(string failedPath, bool fRollBackForward, int countRollback) {
                NavigationLifecycleController.CancelFailedNavigation(failedPath, fRollBackForward, countRollback);
            }

            #endregion

            #region Explorer COM event handlers

            public void Explorer_BeforeNavigate2(object pDisp,
                                                    ref object URL,
                                                    ref object Flags,
                                                    ref object TargetFrameName,
                                                    ref object PostData,
                                                    ref object Headers,
                                                    ref bool Cancel) {
                QTLogger.log("QTTabBarClass Explorer_BeforeNavigate2  pDisp :" + pDisp
                        + " URL :" + (string)URL
                        + " Flags :" + Flags
                        + " TargetFrameName :" + TargetFrameName
                        + " PostData :" + PostData
                        + " Headers :" + Headers
                        + " Cancel :" + Cancel
                    );
                ComEventController.BeforeNavigate((string)URL);
            }

            public void Explorer_NavigateComplete2(object pDisp, ref object URL) {
                string path = (string)URL;
                _navigationCompleteHost.CompleteBrowseObjectNavigation();
                QTLogger.log("QTTabBarClass ShellBrowser.OnNavigateComplete reset field FolderView");

                if(!_navigationCompleteHost.IsShown()) {
                    QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !IsShown");
                    DoFirstNavigation(false, path);
                }

                if(ShutdownNavigationController.HandleNavigationComplete()) {
                    QTLogger.log("fNowQuitting Close Explorer Explorer.Quit2");
                }
                else {
                    int hash = -1;
                    bool flag = _navigationCompleteHost.IsSpecialTravelPath(path);
                    bool flag2 = QTUtility2.IsShellPathButNotFileSystem(path);

                    LockedTabNavigationController.CloneForExternalNavigation(path);
                    hash = SpecialTravelLogController.RecordWhenNeeded(flag);
                    if(hash != -1) {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2  !NavigatedByCode && flag");
                    }
                    ClearTravelLogs();
                    try {
                        _navigationCompleteHost.DisableTabRedraw();
                        ExplorerNavigationState state = NavigationStateController.Synchronize(path, flag, flag2, hash);
                        path = state.Path;
                        byte[] idl = state.Idl;
                        LegacyNavigationController.CompleteNavigation();
                        TooltipController.Refresh((string)URL, flag2);
                        SelectionRestoreController.RestoreAfterNavigation();
                        PostNavigationController.Complete(path, idl, (string)URL);
                    }
                    catch(Exception exception) {
                        QTLogger.MakeErrorLog(exception);
                    }
                    finally {
                        QTLogger.log("QTTabBarClass Explorer_NavigateComplete2 tabControl1.SetRedraw(true)");
                        NavigationCleanupController.Complete();
                    }
                }
            }

            #endregion
        }
}
