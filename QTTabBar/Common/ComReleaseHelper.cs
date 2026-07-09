//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano, indiff
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

namespace QTTabBarLib {
    /// <summary>
    /// Centralizes safe release of COM objects. Releasing a null, already-released,
    /// or otherwise invalid COM object must never throw and must never interrupt the
    /// caller's flow. All failures are logged with contextual information instead.
    /// </summary>
    public static class ComReleaseHelper {

        /// <summary>
        /// Safely releases a COM object via <see cref="Marshal.ReleaseComObject(object)"/>.
        /// </summary>
        /// <param name="obj">The COM object to release. A null value is a no-op.</param>
        /// <param name="context">Optional caller context for diagnostics/logging.</param>
        /// <returns>
        /// The remaining reference count on success, 0 when <paramref name="obj"/> is null,
        /// or -1 when the release failed (the failure is logged).
        /// </returns>
        public static int SafeReleaseComObject(object obj, string context = "") {
            if(obj == null) {
                return 0;
            }
            try {
                return Marshal.ReleaseComObject(obj);
            }
            catch(COMException ex) {
                QTLogger.MakeErrorLog(ex, "SafeReleaseComObject COMException. context=" + context);
            }
            catch(Exception ex) {
                // Covers InvalidComObjectException (already released) and
                // ArgumentException (not a COM object), among others.
                QTLogger.MakeErrorLog(ex, "SafeReleaseComObject failed. context=" + context);
            }
            return -1;
        }
    }
}
