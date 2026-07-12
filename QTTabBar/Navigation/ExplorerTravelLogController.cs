using System;
using System.Runtime.InteropServices;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed class ExplorerTravelLogController {
        private readonly IExplorerTravelHost _host;

        internal ExplorerTravelLogController(IExplorerTravelHost host) {
            _host = host;
        }

        internal void ClearTravelLogs() {
            IEnumTravelLogEntry entries = null;
            try {
                if(_host.TravelLog.EnumEntries(0x30, out entries) != 0 || entries == null) return;
                int index = 0;
                while(true) {
                    ITravelLogEntry entry = null;
                    try {
                        if(entries.Next(1, out entry, 0) != 0) return;
                        IntPtr url;
                        if(index++ != 0 && entry.GetURL(out url) == 0) {
                            string path = Marshal.PtrToStringUni(url);
                            PInvoke.CoTaskMemFree(url);
                            if(!_host.IsSpecialFolderNeedsToTravel(path)) _host.TravelLog.RemoveEntry(entry);
                        }
                    }
                    finally {
                        if(entry != null) {
                            QTLogger.log("ReleaseComObject entry2");
                            Marshal.ReleaseComObject(entry);
                        }
                    }
                }
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
            }
            finally {
                if(entries != null) {
                    QTLogger.log("ReleaseComObject ppenum");
                    Marshal.ReleaseComObject(entries);
                }
            }
        }

        internal void NavigateBackToTheFuture() {
            IEnumTravelLogEntry entries = null;
            ITravelLogEntry entry = null;
            try {
                int count;
                if(_host.TravelLog.EnumEntries(0x20, out entries) == 0 && _host.TravelLog.GetCount(0x20, out count) == 0 && count > 0) {
                    while(entries.Next(1, out entry, 0) == 0) {
                        if(--count == 0) break;
                        if(entry != null) {
                            QTLogger.log("ReleaseComObject rgElt");
                            Marshal.ReleaseComObject(entry);
                            entry = null;
                        }
                    }
                    if(entry != null) _host.TravelLog.TravelTo(entry);
                }
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
            }
            finally {
                if(entries != null) {
                    QTLogger.log("ReleaseComObject ppenum");
                    Marshal.ReleaseComObject(entries);
                }
                if(entry != null) {
                    QTLogger.log("ReleaseComObject rgElt");
                    Marshal.ReleaseComObject(entry);
                }
            }
        }

        internal ITravelLogEntry GetCurrentLogEntry() {
            IEnumTravelLogEntry entries = null;
            ITravelLogEntry entry = null;
            try {
                if(_host.TravelLog.EnumEntries(1, out entries) == 0) entries.Next(1, out entry, 0);
                return entry;
            }
            catch(Exception exception) {
                QTLogger.MakeErrorLog(exception);
                return null;
            }
            finally {
                if(entries != null) {
                    QTLogger.log("ReleaseComObject ppenum");
                    Marshal.ReleaseComObject(entries);
                }
            }
        }
    }
}
