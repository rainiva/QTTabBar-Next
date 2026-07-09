//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Runtime.InteropServices;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public partial class QTTabBarClass {
        internal partial class ExplorerControllerModule {
            #region Travel log

            public void ClearTravelLogs() {
                IEnumTravelLogEntry ppenum = null;
                try {
                    if((_owner.TravelLog.EnumEntries(0x30, out ppenum) != 0) || (ppenum == null)) {
                        return;
                    }
                    int num = 0;
                Label_0018:
                    ITravelLogEntry entry2 = null;
                    try {
                        if(ppenum.Next(1, out entry2, 0) == 0) {
                            IntPtr ptr;
                            if((num++ != 0) && (entry2.GetURL(out ptr) == 0)) {
                                string path = Marshal.PtrToStringUni(ptr);
                                PInvoke.CoTaskMemFree(ptr);
                                if(!_owner.IsSpecialFolderNeedsToTravel(path)) {
                                    _owner.TravelLog.RemoveEntry(entry2);
                                }
                            }
                            goto Label_0018;
                        }
                    }
                    finally {
                        if(entry2 != null) {
                            QTLogger.log("ReleaseComObject entry2");
                            Marshal.ReleaseComObject(entry2);
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                }
            }

            public void NavigateBackToTheFuture() {
                IEnumTravelLogEntry ppenum = null;
                ITravelLogEntry rgElt = null;
                try {
                    int num;
                    if(((_owner.TravelLog.EnumEntries(0x20, out ppenum) == 0) && (_owner.TravelLog.GetCount(0x20, out num) == 0)) && (num > 0)) {
                        while(ppenum.Next(1, out rgElt, 0) == 0) {
                            if(--num == 0) {
                                break;
                            }
                            if(rgElt != null) {
                                QTLogger.log("ReleaseComObject rgElt");
                                Marshal.ReleaseComObject(rgElt);
                                rgElt = null;
                            }
                        }
                        if(rgElt != null) {
                            _owner.TravelLog.TravelTo(rgElt);
                        }
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                    if(rgElt != null) {
                        QTLogger.log("ReleaseComObject rgElt");
                        Marshal.ReleaseComObject(rgElt);
                    }
                }
            }

            private ITravelLogEntry GetCurrentLogEntry() {
                IEnumTravelLogEntry ppenum = null;
                ITravelLogEntry rgElt = null;
                ITravelLogEntry entry3;
                try {
                    if(_owner.TravelLog.EnumEntries(1, out ppenum) == 0) {
                        ppenum.Next(1, out rgElt, 0);
                    }
                    entry3 = rgElt;
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                    entry3 = null;
                }
                finally {
                    if(ppenum != null) {
                        QTLogger.log("ReleaseComObject ppenum");
                        Marshal.ReleaseComObject(ppenum);
                    }
                }
                return entry3;
            }

            #endregion
        }
    }
}
