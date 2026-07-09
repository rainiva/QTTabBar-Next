//    Plus-button handlers shared by QTTabBarClass and QTSecondViewBar (arch-batch3w3a).

using System;
using System.IO;
using QTPlugin;

namespace QTTabBarLib {
    public abstract partial class TabBarBase {
        internal void tabControl1_PlusButtonClicked(object sender, QTabCancelEventArgs e) {
            string clipPath = QTUtility2.GetStringClipboard();
            if(string.IsNullOrEmpty(clipPath)) {
                openDefault();
                return;
            }
            clipPath = clipPath.Trim().Trim(new char[] { ' ', '"' });
            bool blockSelecting = false, fForceNew = true;
            if(File.Exists(clipPath)) {
                try {
                    QTUtility2.log("tabControl1_PlusButtonClicked file exist " + clipPath);
                    string pathRoot = Path.GetPathRoot(clipPath);
                    DirectoryInfo di = new DirectoryInfo(clipPath);
                    if(Directory.Exists(di.Parent.FullName)) {
                        OpenNewTab(di.Parent.FullName, blockSelecting, fForceNew);
                    }
                    else {
                        OpenNewTab(pathRoot, blockSelecting, fForceNew);
                    }

                    string selectMe = Path.GetFileName(clipPath);
                    ShellBrowser.TrySetSelection(new Address[] { new Address(selectMe) }, null, true);
                }
                catch(Exception e1) {
                    QTUtility2.MakeErrorLog(e1, "tabControl1_PlusButtonClicked for file:" + clipPath);
                    openDefault();
                }
            }
            else if(Directory.Exists(clipPath)) {
                try {
                    QTUtility2.log("tabControl1_PlusButtonClicked Directory exist " + clipPath);
                    OpenNewTab(clipPath, blockSelecting, fForceNew);
                }
                catch(Exception e1) {
                    QTUtility2.MakeErrorLog(e1, "tabControl1_PlusButtonClicked for director :" + clipPath);
                    openDefault();
                }
            }
            else {
                openDefault();
            }
        }

        internal void openDefault() {
            bool isOpend = false;
            using(IDLWrapper wrapper = new IDLWrapper(Config.Window.DefaultLocation)) {
                QTUtility2.log("tabControl1_PlusButtonClicked others default " + Config.Window.DefaultLocation);
                OpenNewTab(wrapper, false, true);
                isOpend = true;
            }

            if(!isOpend) {
                string idl = "::{20D04FE0-3AEA-1069-A2D8-08002B30309D}";
                using(IDLWrapper w = new IDLWrapper(idl)) {
                    QTUtility2.log("tabControl1_PlusButtonClicked MyComputer ");
                    OpenNewTab(w, false, true);
                }
            }
        }
    }
}
