using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using QTPlugin;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class FileToolsController {
        private readonly IFileToolsHost _host;
        private static FileHashComputerForm md5Form;

        public FileToolsController(IFileToolsHost host) {
            _host = host;
        }

        internal static void DisposeMd5Form() {
            if((md5Form != null) && !md5Form.InvokeRequired) {
                md5Form.SaveMD5FormStat();
                md5Form.Dispose();
                md5Form = null;
            }
        }

        public bool DoFileTools(int index) {
            try {
                Address[] addressArray;
                List<string> list;
                int num;
                string displayName = string.Empty;
                switch(index) {
                    case 0:
                    case 1:
                    case 4:
                        string str2;
                        if(!_host.ShellBrowser.TryGetSelection(out addressArray, out str2, index == 1)) {
                            goto Label_019C;
                        }
                        list = new List<string>();
                        num = 0;
                        goto Label_00A1;
                    case 2:
                    case 3:
                        using(IDLWrapper wrapper = _host.ShellBrowser.GetShellPath()) {
                            if(wrapper.Available) {
                                displayName = ShellMethods.GetDisplayName(wrapper.PIDL, index == 3);
                            }
                            goto Label_019C;
                        }
                    case 5:
                        foreach(QTabItem item in _host.TabControl.TabPages) {
                            string currentPath = item.CurrentPath;
                            int length = currentPath.IndexOf("???");
                            if(length != -1) {
                                currentPath = currentPath.Substring(0, length);
                            }
                            int num3 = currentPath.IndexOf("*?*?*");
                            if(num3 != -1) {
                                currentPath = currentPath.Substring(0, num3);
                            }
                            displayName = displayName + ((displayName.Length == 0) ? string.Empty : "\r\n") + currentPath;
                        }
                        goto Label_019C;
                    default:
                        goto Label_019C;
                }
            Label_004B:
                if(addressArray[num].Path != null) {
                    if(index != 4) {
                        displayName = displayName + ((displayName.Length == 0) ? string.Empty : "\r\n") + addressArray[num].Path;
                    }
                    else {
                        list.Add(addressArray[num].Path);
                    }
                }
                num++;
            Label_00A1:
                if(num < addressArray.Length) {
                    goto Label_004B;
                }
                if(index == 4) {
                    ShowMD5(list.ToArray());
                    return true;
                }
            Label_019C:
                if(displayName.Length > 0) {
                    QTUtility2.SetStringClipboard(displayName);
                    return true;
                }
            }
            catch(Exception e) {
                QTLogger.MakeErrorLog(e, "DoFileTools");
            }
            if(index == 4) {
                ShowMD5(null);
                return true;
            }
            return false;
        }

        internal static void ShowMD5(string[] paths) {
            if(md5Form == null) {
                md5Form = new FileHashComputerForm();
            }
            List<string> list = new List<string>();
            if(paths != null) {
                list.AddRange(paths.Where(File.Exists));
            }
            string[] strArray = null;
            if(list.Count > 0) {
                strArray = list.ToArray();
            }
            if(md5Form.InvokeRequired) {
                md5Form.Invoke(new FormMethodInvoker(ShowMD5FormCore), new object[] { strArray });
            }
            else {
                ShowMD5FormCore(strArray);
            }
        }

        private static void ShowMD5FormCore(object paths) {
            md5Form.ShowFileHashForm((string[])paths);
        }
    }
}
