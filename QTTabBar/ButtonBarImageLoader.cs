using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Microsoft.Win32;

namespace QTTabBarLib {
    internal static class ButtonBarImageLoader {
        internal static void Manage(ImageStrip large, ImageStrip small) {
            if(Config.BBar.ImageStripPath == null) {
                LoadDefaultImages(large, small, false);
            }
            else if(Config.BBar.ImageStripPath.Length == 0 || !LoadExternalImage(Config.BBar.ImageStripPath, large, small)) {
                LoadDefaultImages(large, small, true);
            }
        }

        internal static bool LoadExternalImage(string path, ImageStrip large, ImageStrip small) {
            Bitmap bitmap;
            Bitmap bitmap2;
            if(LoadExternalImage(path, out bitmap, out bitmap2)) {
                large.AddStrip(bitmap);
                small.AddStrip(bitmap2);
                bitmap.Dispose();
                bitmap2.Dispose();
                if(Path.GetExtension(path).PathEquals(".bmp")) {
                    large.TransparentColor = small.TransparentColor = Color.Magenta;
                }
                else {
                    large.TransparentColor = small.TransparentColor = Color.Empty;
                }
                return true;
            }
            return false;
        }

        internal static bool LoadExternalImage(string path, out Bitmap bmpLarge, out Bitmap bmpSmall) {
            bmpLarge = bmpSmall = null;
            if(File.Exists(path)) {
                try {
                    using(Bitmap bitmap = new Bitmap(path)) {
                        if(bitmap.Width >= 504 && bitmap.Height >= 24) {
                            bmpLarge = bitmap.Clone(new Rectangle(0, 0, 504, 24), PixelFormat.Format32bppArgb);
                            bmpSmall = (Bitmap)Resize(bmpLarge, 336, 16);
                            return true;
                        }
                    }
                }
                catch(Exception ex) {
                    QTLogger.MakeErrorLog(ex);
                }
            }
            return false;
        }

        internal static Image Resize(Bitmap original, int desiredWidth, int desiredHeight) {
            if(desiredWidth < 4 || desiredHeight < 4) {
                throw new InvalidOperationException("Bounding Box of Resize Photo must be larger than 4X4 pixels.");
            }
            decimal oW = original.Width, oH = original.Height, dW = desiredWidth, dH = desiredHeight;
            if(oW < dW && oH < dH) return original;
            if(oW == oH && dW == dH) return new Bitmap(original, (int)dW, (int)dH);
            if(oW == oH) {
                int smallSide = (int)Math.Min(dW, dH);
                return new Bitmap(original, smallSide, smallSide);
            }
            decimal ratio;
            if(oW > dW && oH > dH) ratio = Math.Min(dW, dH) / Math.Min(oW, oH);
            else ratio = oW > dW ? dW / oW : dH / oH;
            return new Bitmap(original, (int)(oW * ratio), (int)(oH * ratio));
        }

        private static void LoadDefaultImages(ImageStrip large, ImageStrip small, bool writeRegistry) {
            large.TransparentColor = small.TransparentColor = Color.Empty;
            Bitmap bmpLarge = ThemeRefreshService.IsDark ? Resources_Image.ButtonStripWhite24 : Resources_Image.ButtonStrip24;
            Bitmap bmpSmall = ThemeRefreshService.IsDark ? Resources_Image.ButtonStripWhite16 : Resources_Image.ButtonStrip16;
            large.AddStrip(bmpLarge);
            small.AddStrip(bmpSmall);
            bmpLarge.Dispose();
            bmpSmall.Dispose();
            if(writeRegistry) {
                using(RegistryKey key = RegistryAccess.OpenRootCreate()) {
                    key.SetValue("Buttons_ImagePath", string.Empty);
                }
            }
        }
    }
}
