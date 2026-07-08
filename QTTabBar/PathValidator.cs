//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2024  Quizo, Paul Accisano
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

using System.IO;
using System.Text.RegularExpressions;

namespace QTTabBarLib {
    // Task 3.3 extraction: path / string predicate helpers moved out of QTUtility.
    // Behavior is identical to the original QTUtility implementations; QTUtility now
    // forwards to these via one-line facades so cross-file callers are unchanged.
    internal static class PathValidator {

        internal static bool IsNetworkRootFolder(string path) {
            string str = path.Substring(2);
            int index = str.IndexOf(Path.DirectorySeparatorChar);
            if(index != -1) {
                string str2 = str.Substring(index + 1);
                if(str2.Length > 0) {
                    return (str2.IndexOf(Path.DirectorySeparatorChar) == -1);
                }
            }
            return false;
        }

        public static bool IsEmptyStr(string strs) {
            return strs == null || strs.Trim().Length == 0;
        }

        public static bool IsNetPath(string path) {
            return !IsEmptyStr(path) && path.StartsWith(@"\\");
        }

        public static bool IsNoCapturePaths(string path) {
            // Control Panel root and the Printers folder are never captured as tabs.
            string controlPanel = "::{26EE0668-A00A-44D7-9371-BEB064C98683}";
            string print = @"::{21EC2020-3AEA-1069-A2DD-08002B30309D}\::{2227A280-3AEA-1069-A2DE-08002B30309D}";
            return !IsEmptyStr(path) && (
                path.StartsWith(controlPanel) ||
                path.StartsWith(print)
                );
        }

        public static bool IsSimpleDateStr(string input) {
            if (IsEmptyStr(input))
            {
                return false;
            }
            string pattern = @"\d{4}/\d{2}/\d{2} \d{2}:\d{2}:\d{2}";
            return Regex.IsMatch(input, pattern);
        }

        public static bool IsShortDateStr(string input) {
            if (IsEmptyStr(input))
            {
                return false;
            }
            string pattern = @"\d{1,2}/\d{1,2}/\d{1,2}\s��[һ|��|��|��|��|��|��]\s\d{1,2}:\d{1,2}:\d{1,2}";
            return Regex.IsMatch(input, pattern);
        }
    }
}
