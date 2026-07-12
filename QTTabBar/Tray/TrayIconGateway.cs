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

namespace QTTabBarLib.Tray {
    internal static class TrayIconGateway {
        private static TrayIcon trayIcon;

        internal static void AddToTrayIcon(IntPtr tabBarHandle, IntPtr explorerHandle, string currentPath, string[] tabNames, string[] tabPaths) {
            if (trayIcon == null) trayIcon = new TrayIcon();
            trayIcon.AddToTrayIcon(tabBarHandle, explorerHandle, currentPath, tabNames, tabPaths);
        }

        internal static void RemoveFromTrayIcon(IntPtr tabBarHandle) {
            if (trayIcon == null) trayIcon = new TrayIcon();
            trayIcon.RestoreWindow(tabBarHandle);
        }
    }
}
