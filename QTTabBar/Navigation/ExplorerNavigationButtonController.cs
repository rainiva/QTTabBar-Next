using System;
using System.Drawing;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal sealed class ExplorerNavigationButtonController {
        private readonly IExplorerNavigationButtonHost _host;
        private readonly EventHandler _navigationClick;

        internal ExplorerNavigationButtonController(IExplorerNavigationButtonHost host, EventHandler navigationClick) {
            _host = host;
            _navigationClick = navigationClick;
        }

        internal void Initialize(bool sync) {
            ToolStripClasses toolbar = new ToolStripClasses();
            ToolStripButton back = new ToolStripButton();
            ToolStripButton forward = new ToolStripButton();
            toolbar.SuspendLayout();
            if(!IconManager.ImageGlobalContainsKey("navBack")) IconManager.AddImageToGlobal("navBack", Resources_Image.imgNavBack);
            if(!IconManager.ImageGlobalContainsKey("navFrwd")) IconManager.AddImageToGlobal("navFrwd", Resources_Image.imgNavFwd);
            toolbar.Dock = Config.Tabs.NavButtonsOnRight ? DockStyle.Right : DockStyle.Left;
            toolbar.AutoSize = false;
            toolbar.CanOverflow = false;
            toolbar.LayoutStyle = ToolStripLayoutStyle.HorizontalStackWithOverflow;
            toolbar.GripStyle = ToolStripGripStyle.Hidden;
            toolbar.Items.AddRange(new ToolStripItem[] { back, forward, _host.HistoryButton });
            toolbar.Renderer = new ToolbarRenderer();
            toolbar.Width = 0x3f;
            toolbar.TabStop = false;
            toolbar.BackColor = ThemeRefreshService.IsDark ? Color.Black : Color.WhiteSmoke;
            ConfigureButton(back, sync && ((_host.NavigationFlags & 1) != 0), IconManager.GetImageFromGlobal("navBack"));
            ConfigureButton(forward, sync && ((_host.NavigationFlags & 2) != 0), IconManager.GetImageFromGlobal("navFrwd"));
            _host.InstallNavigationControls(toolbar, back, forward);
        }

        private void ConfigureButton(ToolStripButton button, bool enabled, Image image) {
            button.AutoSize = false;
            button.DisplayStyle = ToolStripItemDisplayStyle.Image;
            button.Enabled = enabled;
            button.Image = image;
            button.Size = new Size(0x15, 0x15);
            button.Click += _navigationClick;
        }
    }
}
