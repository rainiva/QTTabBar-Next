using System.Windows.Forms;

namespace QTTabBarLib {
    internal partial class ExplorerController {
        private bool RouteExplorerWindowMessage(ref Message message) {
            return MessageRoutingController.Route(ref message);
        }
    }
}
