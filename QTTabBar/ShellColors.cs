using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace QTTabBarLib
{
    public static class ShellColors
    {
        public static Color LightModeColor = Color.White;

        public static Color ControlMainColor = Color.FromArgb(41, 128, 204);

        public static Color NightModeColor = Color.Black;

        public static Color NightModeTreeViewBackColor = Color.FromArgb(25, 25, 25);

        public static Color NightModeLightColor = Color.FromArgb(43, 43, 43);

        public static Color NightModeTextColor = Color.White;

        public static Color NightModeBorderColor = Color.FromArgb(83, 83, 83);

        public static Color NightModeDisabledColor = Color.FromArgb(140, 140, 140);

        public static Color NightModeTabColor = Color.FromArgb(217, 217, 217);

        public static Color NightModeTextShadow = Color.Gray;

        public static Color NightModeViewBackColor = Color.FromArgb(32, 32, 32);

        public static Color NightModeViewSelectionColor = Color.FromArgb(98, 98, 98);

        public static Color NightModeViewSelectionColorInactive = Color.FromArgb(51, 51, 51);

        public static Color NightModeViewSelectedAndFocusedColor = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewSelectedAndHiliteColor = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewSelectedAndHiliteColorInactive = Color.FromArgb(119, 119, 119);

        public static Color NightModeViewHiliteColor = Color.FromArgb(77, 77, 77);

        public static Color NightModeViewHeaderHiliteColor = Color.FromArgb(67, 67, 67);

        public static Color FaceColor17666 = !ThemeRefreshService.IsDark ? ShellColors.LightModeColor : ShellColors.NightModeColor;

        public static Color NightModeOptionColor = Color.FromArgb(44, 44, 44);

        private static ShellColors.ShellColorSet colorSet = ShellColors.Create();

        public static Color Light
        {
            get
            {
                return ShellColors.colorSet.Light;
            }
        }


        public static Color Default
        {
            get
            {
                return ShellColors.colorSet.Default;
            }
        }

        public static Color ExplorerBarHrztBGColor
        {
            get
            {
                return OSDetector.LaterThan7 ?
                    SystemColors.Window :
                    Color.FromArgb(241, 245, 251);
            }
        }

        public static Color ExplorerBarVertBGColor
        {
            get
            {
                return OSDetector.LaterThan7 ?
                    SystemColors.Window :
                    Color.FromArgb(241, 245, 251);
            }
        } 

        public static Color Text {
            get
            {
                return ShellColors.colorSet.Text;
            }
        } 

        public static Color Border {
            get
            {
                return ShellColors.colorSet.Border;
            }
        } 

        public static Color Separator {
            get
            {
                return ShellColors.colorSet.Separator;
            }
        } 

        public static Color Disabled {
            get
            {
                return ShellColors.colorSet.Disabled;
            }
        } 

        public static Color Tab {
            get
            {
                return ShellColors.colorSet.Tab;
            }
        } 

        public static Color TextShadow {
            get
            {
                return ShellColors.colorSet.TextShadow;

            }
        } 

        public static void Refresh()
        {
            ShellColors.colorSet = ShellColors.Create();
        } 

        private static ShellColors.ShellColorSet Create()
        {
            if (!ThemeRefreshService.IsDark)
                return new ShellColors.ShellColorSet();
            return OSDetector.IsWin11 ?
                new ShellColors.Windows10Dark() : 
                new ShellColors.Windows11Dark();
        }

        private class ShellColorSet
        {
          public  Color Default = Color.White;

          public  Color TreeViewBack = Color.White;

          public  Color Light = Color.FromArgb(242, 242, 242);

          public  Color Text = Color.Black;

          public  Color Border = Color.FromArgb(217, 217, 217);

          public  Color Separator = Color.FromKnownColor(KnownColor.GrayText);

          public  Color Disabled = Color.Gray;

          public  Color Tab = Color.Empty;

          public Color TextShadow = Color.Empty;

          public Color ViewBack = Color.Empty;

          public Color ViewSelection = Color.Empty;

          public  Color ViewSelectionInactive = Color.Empty;
          public Color ViewSelectionAndFocused = Color.Empty;

          public Color ViewSelectionAndHilite = Color.Empty;

          public Color ViewSelectionAndHiliteInactive = Color.Empty;

          public Color ViewHilite = Color.Empty;

          public Color ViewHeaderHilite = Color.Empty;

          public Color Option = Color.Empty;

          public  Color MenuSelection = Color.FromArgb(217, 217, 217);
        }

        private class Windows10Dark : ShellColors.ShellColorSet
        {
          public new Color Default = Color.Black;

          public new Color TreeViewBack = Color.FromArgb(25, 25, 25);

          public new Color Light = Color.FromArgb(43, 43, 43);

          public new Color Text = Color.White;

          public new Color Border = Color.FromArgb(83, 83, 83);

          public new Color Disabled = Color.FromArgb(140, 140, 140);

          public new Color Separator = Color.FromArgb(140, 140, 140);

          public new Color Tab = Color.FromArgb(217, 217, 217);

          public new Color TextShadow = Color.Gray;

          public new Color ViewBack = Color.FromArgb(32, 32, 32);

          public new Color ViewSelection = Color.FromArgb(98, 98, 98);

          public new Color ViewSelectionInactive = Color.FromArgb(51, 51, 51);

          public new Color ViewSelectionAndFocused = Color.FromArgb(119, 119, 119);

          public new Color ViewSelectionAndHilite = Color.FromArgb(119, 119, 119);

          public new Color ViewSelectionAndHiliteInactive = Color.FromArgb(119, 119, 119);

          public new Color ViewHilite = Color.FromArgb(77, 77, 77);

          public new Color ViewHeaderHilite = Color.FromArgb(67, 67, 67);

          public new Color Option = Color.FromArgb(44, 44, 44);

          public new Color MenuSelection = Color.FromArgb(65, 65, 65);
        }

        private class Windows11Dark : ShellColors.Windows10Dark
        {
          public new Color Default  = Color.FromArgb(30, 32, 35);

          public new Color TreeViewBack = Color.FromArgb(25, 25, 25);

          public new Color Light = Color.FromArgb(44, 44, 44);

          public new Color Border = Color.FromArgb(62, 62, 62);

          public new Color Separator = Color.FromArgb(62, 62, 62);

          public new Color Tab = Color.FromArgb(169, 169, 169);

          public new Color MenuSelection = Color.FromArgb(51, 51, 51);
        }
    }



}
