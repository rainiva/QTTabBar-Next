using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QTTabBarLib {
    /// <summary>
    /// R-10 root-cure feature probe: new menu affordance via Context only (no Host partial growth).
    /// </summary>
    internal sealed class RootCureFeatureProbeController {
        internal const string ProbeItemName = "RootCureProbe_CurrentPath";

        private readonly IMenuContext _menuContext;
        private readonly ITabContext _tabContext;
        private ToolStripMenuItem _probeItem;

        public RootCureFeatureProbeController(IMenuContext menuContext, ITabContext tabContext) {
            _menuContext = menuContext ?? throw new ArgumentNullException(nameof(menuContext));
            _tabContext = tabContext ?? throw new ArgumentNullException(nameof(tabContext));
        }

        public void AttachTo(ContextMenuStripEx contextMenuTab) {
            if(contextMenuTab == null) {
                throw new ArgumentNullException(nameof(contextMenuTab));
            }
            contextMenuTab.Opening += OnContextMenuOpening;
        }

        private void OnContextMenuOpening(object sender, CancelEventArgs e) {
            var menu = (ContextMenuStrip)sender;
            QTabItem tab = _menuContext.ContextMenuedTab ?? _tabContext.CurrentTab;
            if(tab == null) {
                return;
            }

            string label = FormatProbeLabel(tab.CurrentPath);
            if(_probeItem == null || _probeItem.Owner != menu) {
                _probeItem = new ToolStripMenuItem(label) {
                    Name = ProbeItemName,
                    Enabled = false,
                };
                int insertIndex = menu.Items.Count > 0 ? 1 : 0;
                menu.Items.Insert(insertIndex, _probeItem);
            }
            else {
                _probeItem.Text = label;
            }
        }

        public static string FormatProbeLabel(string currentPath) {
            if(string.IsNullOrEmpty(currentPath)) {
                return "Path: (empty)";
            }
            return "Path: " + currentPath;
        }
    }
}
