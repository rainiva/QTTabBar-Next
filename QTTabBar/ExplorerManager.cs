using System.Drawing;

namespace QTTabBarLib {
    /// <summary>
    /// Watermark image cache for Explorer list views. The full ExplorerManager
    /// implementation lives in docs/archive/ExplorerManager.full.cs.
    /// </summary>
    internal static class ExplorerManager {
        private static readonly ResourceCache<BmpCacheKey, Bitmap> watermarkImageCache =
            new ResourceCache<BmpCacheKey, Bitmap>(KeyResourceConverters.ToBitmap);

        public static Bitmap GetWatermarkImage(BmpCacheKey key) {
            return watermarkImageCache[key];
        }

        internal static void ClearWatermarkCache() {
            watermarkImageCache.Clear();
        }
    }
}
