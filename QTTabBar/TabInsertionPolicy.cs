namespace QTTabBarLib {
    internal static class TabInsertionPolicy {
        internal static int Resolve(TabPos position, int tabCount, int selectedIndex) {
            switch(position) {
                case TabPos.LastActive:
                case TabPos.Leftmost:
                    return 0;
                case TabPos.Left:
                    return selectedIndex - 1;
                case TabPos.Right:
                    return selectedIndex + 1;
                case TabPos.Rightmost:
                default:
                    return tabCount;
            }
        }
    }
}
