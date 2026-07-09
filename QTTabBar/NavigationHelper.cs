using System;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    /// <summary>
    /// Provides navigation-related helper methods extracted from QTTabBarClass.
    /// Contains pure logic that doesn't depend on form state.
    /// </summary>
    internal static class NavigationHelper {

        /// <summary>
        /// Determines whether the given path is a Windows search results folder.
        /// On XP, uses ResMisc[2]; on later OS, uses PATH_SEARCHFOLDER.
        /// </summary>
        public static bool IsSearchResultFolder(string path) {
            return path.PathStartsWith(OSDetector.IsXP ? QTUtility.ResMisc[2] : OSDetector.PATH_SEARCHFOLDER);
        }

        /// <summary>
        /// Gets the search folder path for the current OS version.
        /// </summary>
        public static string SearchFolderPath {
            get {
                return OSDetector.IsXP ? QTUtility.ResMisc[2] : OSDetector.PATH_SEARCHFOLDER;
            }
        }
    }
}
