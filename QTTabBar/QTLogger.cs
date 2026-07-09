using System;
using System.Collections.Generic;

namespace QTTabBarLib {
    /// <summary>
    /// Public logging surface extracted from QTUtility2; delegates to internal Logger.
    /// </summary>
    internal static class QTLogger {
        public static void flog(string optional) {
            Logger.flog(optional);
        }

        public static void log(string optional) {
            Logger.log(optional);
        }

        public static void log(string level, string optional, Dictionary<string, string> dic = null) {
            Logger.log(level, optional, dic);
        }

        public static void MakeErrorLog(Exception ex, string optional = null) {
            Logger.MakeErrorLog(ex, optional);
        }

        public static void MakeErrorLog(string optional = null) {
            Logger.MakeErrorLog(optional);
        }
    }
}
