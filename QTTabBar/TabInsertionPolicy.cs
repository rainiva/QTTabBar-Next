namespace QTTabBarLib {
    internal static class TabInsertionPolicy {
        internal static int Resolve(TabPos position, int tabCount, int selectedIndex) {
            tabCount = System.Math.Max(0, tabCount);
            selectedIndex = System.Math.Min(tabCount, System.Math.Max(0, selectedIndex));

            int index;
            switch(position) {
                case TabPos.LastActive:
                case TabPos.Leftmost:
                    index = 0;
                    break;
                case TabPos.Left:
                    index = selectedIndex - 1;
                    break;
                case TabPos.Right:
                    index = selectedIndex + 1;
                    break;
                case TabPos.Rightmost:
                default:
                    index = tabCount;
                    break;
            }

            return System.Math.Min(tabCount, System.Math.Max(0, index));
        }
    }
}
