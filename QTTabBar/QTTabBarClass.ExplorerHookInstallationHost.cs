using System;
using BandObjectLib;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass : IExplorerHookInstallationHost {
        void IExplorerHookInstallationHost.InstallInputHook(int threadId) {
            _hookInputController.Install(threadId);
        }

        void IExplorerHookInstallationHost.InstallExplorerWindowHook(NativeWindowController.MessageEventHandler handler) {
            explorerController = new NativeWindowController(ExplorerHandle);
            explorerController.MessageCaptured += handler;
        }

        bool IExplorerHookInstallationHost.HasRebar => ReBarHandle != IntPtr.Zero;

        void IExplorerHookInstallationHost.InstallRebar() {
            rebarController = new RebarController(this, ReBarHandle, BandObjectSite as IOleCommandTarget);
        }

        bool IExplorerHookInstallationHost.IsLegacyWindowsXp => OSDetector.IsXP;

        void IExplorerHookInstallationHost.InstallTravelToolbarHook(NativeWindowController.MessageEventHandler handler) {
            TravelToolBarHandle = GetTravelToolBarWindow32();
            if(TravelToolBarHandle == IntPtr.Zero) return;
            travelBtnController = new NativeWindowController(TravelToolBarHandle);
            travelBtnController.MessageCaptured += handler;
        }

        void IExplorerHookInstallationHost.InstallDropTarget() {
            dropTargetWrapper = new DropTargetWrapper(this);
            dropTargetWrapper.DragFileEnter += dropTargetWrapper_DragFileEnter;
            dropTargetWrapper.DragFileOver += dropTargetWrapper_DragFileOver;
            dropTargetWrapper.DragFileLeave += dropTargetWrapper_DragFileLeave;
            dropTargetWrapper.DragFileDrop += dropTargetWrapper_DragFileDrop;
        }
    }
}
