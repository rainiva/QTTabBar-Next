using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.IO;

namespace QTTabBarLib {
    internal enum BmpCacheKey {
        Watermark_General,
        Watermark_Picture,
        Watermark_Music,
        Watermark_Movie,
        Watermark_Document,
    }

    internal enum PerceivedType {
        Unknown,
        Image,
        Audio,
        Video,
        Document,
    }

    internal static class KeyResourceConverters {
        private static readonly string ImageDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "QTTabBar", "Image");

        internal static Bitmap ToBitmap(BmpCacheKey key) {
            string fileName = key.ToString().Replace("Watermark_", string.Empty) + ".png";
            string path = Path.Combine(ImageDirectory, fileName);
            if(!File.Exists(path)) {
                return null;
            }
            try {
                using(var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    return new Bitmap(stream);
                }
            }
            catch(Exception ex) {
                QTUtility2.MakeErrorLog(ex, "KeyResourceConverters.ToBitmap: " + path);
                return null;
            }
        }
    }

    internal sealed class ResourceCache<TKey, TValue> where TValue : class {
        private readonly ConcurrentDictionary<TKey, TValue> cache = new ConcurrentDictionary<TKey, TValue>();
        private readonly Func<TKey, TValue> factory;

        internal ResourceCache(Func<TKey, TValue> factory) {
            this.factory = factory ?? throw new ArgumentNullException(nameof(factory));
        }

        internal TValue this[TKey key] {
            get {
                TValue value;
                if(cache.TryGetValue(key, out value)) {
                    return value;
                }
                value = factory(key);
                if(value != null) {
                    cache[key] = value;
                }
                return value;
            }
        }

        internal void Clear() {
            foreach(TValue value in cache.Values) {
                (value as IDisposable)?.Dispose();
            }
            cache.Clear();
        }
    }
}
