using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using QTPlugin;

namespace QTTabBarLib {
    /// <summary>
    /// Host interface for the composition-root controller (ComponentBuildController).
    /// Exposes setters for controller fields, UI control fields, form properties,
    /// and a single WireControlEvents() hook so the controller can wire events
    /// without _owner back-references.
    /// </summary>
    internal interface IComponentBuildHost {
        // --- Controller field setters ---
        ExplorerController ExplorerControllerModule { get; set; }
        TabManager TabManager { get; set; }
        MenuController MenuController { get; set; }
        DragDropController DragDropController { get; set; }
        HookInputController HookInputController { get; set; }
        FileToolsController FileToolsController { get; set; }
        BindActionController BindActionController { get; set; }
        ShellCommandController ShellCommandController { get; set; }
        ListViewInputController ListViewInputController { get; set; }
        KeyboardAcceleratorController KeyboardAcceleratorController { get; set; }
        ShellUiController ShellUiController { get; set; }
        ButtonBarClickController ButtonBarClickController { get; set; }
        BandInfoController BandInfoController { get; set; }
        BandLifecycleController BandLifecycleController { get; set; }
        ShellNavigationController ShellNavigationController { get; set; }
        TabTooltipController TabTooltipController { get; set; }
        WindowManagementController WindowManagementController { get; set; }
        BandWindowController BandWindowController { get; set; }
        DroppedFilesController DroppedFilesController { get; set; }
        FolderTreeController FolderTreeController { get; set; }
        ViewModeController ViewModeController { get; set; }
        PluginMenuController PluginMenuController { get; set; }
        ShutdownController ShutdownController { get; set; }

        // --- UI control field getters/setters ---
        ToolStripDropDownButton ButtonNavHistoryMenu { get; set; }
        QTabControl TabControl1 { get; set; }
        QTabItem CurrentTab { get; set; }
        ContextMenuStripEx ContextMenuTab { get; set; }
        ContextMenuStripEx ContextMenuSys { get; set; }

        // --- Form properties ---
        IContainer Components { get; }
        ToolStrip ToolStrip { get; }
        Control.ControlCollection Controls { get; }
        Size MinSize { get; set; }
        int Height { get; set; }
        int BandHeight { get; set; }
        ContextMenuStrip ContextMenuStrip { get; set; }
        float GetBandDpiScale();
        void SuspendLayout();
        void ResumeLayout(bool performLayout);

        // --- Event wiring hook ---
        void WireControlEvents();
    }
}
