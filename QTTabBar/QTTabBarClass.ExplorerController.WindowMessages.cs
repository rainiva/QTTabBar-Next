using System.Windows.Forms;

namespace QTTabBarLib {
    internal partial class ExplorerController {
        public bool explorerController_MessageCaptured(ref Message message) {
            return WindowMessageController.Process(ref message);
        }
    }
}
