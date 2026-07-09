//    View mode controller extracted from QTTabBarClass (arch-batch3c6t).

using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal class ViewModeController {
            private readonly QTTabBarClass _owner;

            public ViewModeController(QTTabBarClass owner) {
                _owner = owner;
            }

            public void ChangeViewMode(bool fUp) {
                FVM orig = _owner.ShellBrowser.ViewMode;
                FVM mode = orig;
                switch(mode) {
                    case FVM.ICON:
                        mode = fUp ? FVM.TILE : FVM.LIST;
                        break;

                    case FVM.LIST:
                        mode = fUp ? FVM.ICON : FVM.DETAILS;
                        break;

                    case FVM.DETAILS:
                        if(fUp) {
                            mode = FVM.LIST;
                        }
                        break;

                    case FVM.THUMBNAIL:
                        mode = fUp ? FVM.THUMBSTRIP : FVM.TILE;
                        break;

                    case FVM.TILE:
                        mode = fUp ? FVM.THUMBNAIL : FVM.ICON;
                        break;

                    case FVM.THUMBSTRIP:
                        if(!fUp) {
                            mode = FVM.THUMBNAIL;
                        }
                        break;
                }
                if(mode != orig) {
                    _owner.ShellBrowser.ViewMode = mode;
                }
            }
        }
    }
}
