using System.Drawing;
using System.Windows.Forms;
using BandObjectLib;
using QTPlugin;

namespace QTTabBarLib {
    internal interface IButtonBarHost {
        System.IntPtr Handle { get; }
        System.IntPtr ExplorerHandle { get; }
        ToolStrip ToolStrip { get; }
        void OpenPath(string path, bool newWindow);
        void ExecuteBindAction(BindAction action);
        void RefreshItems();
        void ShowOptions();
    }

    internal interface IButtonBarLifecycleHost {
        Size GetBandSize();
        int GetBandHeight();
        Size GetMinimumBandSize();
        void InitializeBandComponents();
        void AttachToExplorerAndInitializeItems();
        void DrawVisualStyleBackground(PaintEventArgs e);
        bool TryDrawRebarBackground(PaintEventArgs e);
        bool ReadBreakPreference();
        void PersistBreakPreference();
    }
}
