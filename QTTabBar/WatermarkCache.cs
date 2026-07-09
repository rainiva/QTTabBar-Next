using System;

using System.Collections.Concurrent;

using System.Drawing;

using System.IO;

using QTTabBarLib.Common;

using QTTabBarLib.Interop;



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



    internal static class ViewPerceivedTypeResolver {

        private static readonly (Guid folderId, PerceivedType type)[] FolderTypeMap = {
            (FolderIdentifiers.Pictures, PerceivedType.Image),
            (FolderIdentifiers.PicturesLibrary, PerceivedType.Image),
            (FolderIdentifiers.PhotoAlbums, PerceivedType.Image),
            (FolderIdentifiers.OriginalImages, PerceivedType.Image),
            (FolderIdentifiers.PublicPictures, PerceivedType.Image),
            (FolderIdentifiers.SamplePictures, PerceivedType.Image),
            (FolderIdentifiers.Music, PerceivedType.Audio),
            (FolderIdentifiers.MusicLibrary, PerceivedType.Audio),
            (FolderIdentifiers.Playlists, PerceivedType.Audio),
            (FolderIdentifiers.Ringtones, PerceivedType.Audio),
            (FolderIdentifiers.PublicMusic, PerceivedType.Audio),
            (FolderIdentifiers.SampleMusic, PerceivedType.Audio),
            (FolderIdentifiers.Videos, PerceivedType.Video),
            (FolderIdentifiers.VideosLibrary, PerceivedType.Video),
            (FolderIdentifiers.RecordedTVLibrary, PerceivedType.Video),
            (FolderIdentifiers.PublicVideos, PerceivedType.Video),
            (FolderIdentifiers.SampleVideos, PerceivedType.Video),
            (FolderIdentifiers.Documents, PerceivedType.Document),
            (FolderIdentifiers.DocumentsLibrary, PerceivedType.Document),
            (FolderIdentifiers.PublicDocuments, PerceivedType.Document),
        };



        internal static PerceivedType Resolve(ShellBrowserEx shellBrowser) {

            if(shellBrowser == null) {

                return PerceivedType.Unknown;

            }

            using(IDLWrapper location = shellBrowser.GetShellPath()) {

                if(!location.Available) {

                    return PerceivedType.Unknown;

                }

                PerceivedType byFolder = ResolveByKnownFolder(location);

                if(byFolder != PerceivedType.Unknown) {

                    return byFolder;

                }

                return ResolveBySpecialFolderPath(location.Path);

            }

        }



        internal static PerceivedType FromWindowsPerceivedType(int? value) {

            if(!value.HasValue) {

                return PerceivedType.Unknown;

            }

            switch(value.Value) {

                case 2:

                    return PerceivedType.Image;

                case 3:

                    return PerceivedType.Audio;

                case 4:

                    return PerceivedType.Video;

                case 6:

                    return PerceivedType.Document;

                default:

                    return PerceivedType.Unknown;

            }

        }



        internal static bool IsSameOrUnderPath(string currentPath, string ancestorPath) {

            if(string.IsNullOrEmpty(currentPath) || string.IsNullOrEmpty(ancestorPath)) {

                return false;

            }

            if(currentPath.StartsWith("::", StringComparison.Ordinal) ||

               ancestorPath.StartsWith("::", StringComparison.Ordinal)) {

                return currentPath.Equals(ancestorPath, StringComparison.OrdinalIgnoreCase);

            }

            string current = NormalizePath(currentPath);

            string ancestor = NormalizePath(ancestorPath);

            if(current.Equals(ancestor, StringComparison.OrdinalIgnoreCase)) {

                return true;

            }

            char separator = current.IndexOf('/') >= 0 ? '/' : Path.DirectorySeparatorChar;

            return current.StartsWith(ancestor + separator, StringComparison.OrdinalIgnoreCase);

        }



        private static PerceivedType ResolveByKnownFolder(IDLWrapper location) {

            foreach((Guid folderId, PerceivedType type) in FolderTypeMap) {

                if(IsUnderKnownFolder(location, folderId)) {

                    return type;

                }

            }

            return PerceivedType.Unknown;

        }



        private static PerceivedType ResolveBySpecialFolderPath(string path) {

            if(string.IsNullOrEmpty(path) || path.StartsWith("::", StringComparison.Ordinal)) {

                return PerceivedType.Unknown;

            }

            if(IsSameOrUnderPath(path, Environment.GetFolderPath(Environment.SpecialFolder.MyPictures))) {

                return PerceivedType.Image;

            }

            if(IsSameOrUnderPath(path, Environment.GetFolderPath(Environment.SpecialFolder.MyMusic))) {

                return PerceivedType.Audio;

            }

            if(IsSameOrUnderPath(path, Environment.GetFolderPath(Environment.SpecialFolder.MyVideos))) {

                return PerceivedType.Video;

            }

            if(IsSameOrUnderPath(path, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))) {

                return PerceivedType.Document;

            }

            return PerceivedType.Unknown;

        }



        private static bool IsUnderKnownFolder(IDLWrapper location, Guid folderId) {

            IntPtr knownPidl = IntPtr.Zero;

            try {

                if(PInvoke.SHGetKnownFolderIDList(ref folderId, 0, IntPtr.Zero, out knownPidl) != 0 ||

                   knownPidl == IntPtr.Zero) {

                    return false;

                }

                using(IDLWrapper known = new IDLWrapper(knownPidl)) {

                    if(location == known) {

                        return true;

                    }

                    if(!string.IsNullOrEmpty(location.Path) && !location.Path.StartsWith("::", StringComparison.Ordinal) &&

                       !string.IsNullOrEmpty(known.Path) && !known.Path.StartsWith("::", StringComparison.Ordinal)) {

                        return IsSameOrUnderPath(location.Path, known.Path);

                    }

                    return false;

                }

            }

            catch(Exception ex) {

                QTLogger.MakeErrorLog(ex, "ViewPerceivedTypeResolver.IsUnderKnownFolder");

                return false;

            }

        }



        private static string NormalizePath(string path) {

            try {

                return Path.GetFullPath(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));

            }

            catch {

                return path.TrimEnd('\\', '/');

            }

        }

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

                byte[] data = File.ReadAllBytes(path);

                using(var ms = new MemoryStream(data)) {

                    using(Image image = Image.FromStream(ms)) {

                        return new Bitmap(image);

                    }

                }

            }

            catch(Exception ex) {

                QTLogger.MakeErrorLog(ex, "KeyResourceConverters.ToBitmap: " + path);

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

                if(value == null) {

                    return null;

                }

                TValue existing = cache.GetOrAdd(key, value);

                if(!ReferenceEquals(existing, value)) {

                    (value as IDisposable)?.Dispose();

                }

                return existing;

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


