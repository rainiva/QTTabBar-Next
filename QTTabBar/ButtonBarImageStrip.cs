using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

namespace QTTabBarLib {
    internal sealed class ImageStrip : IDisposable {
        private readonly List<Bitmap> _images = new List<Bitmap>();
        private readonly Size _size;
        private Color _transparentColor;
        private static readonly object ImageLock = new object();

        internal ImageStrip(Size size) { _size = size; }
        internal int LstImagesLength() { return _images.Count; }
        internal void AddStrip(Bitmap source) {
            int width = source.Width, index = 0;
            if(width % _size.Width != 0 || source.Height != _size.Height) throw new ArgumentException("size invalid.");
            Rectangle rectangle = new Rectangle(Point.Empty, _size);
            while(width >= _size.Width) {
                Bitmap image = source.Clone(rectangle, PixelFormat.Format32bppArgb);
                if(_transparentColor != Color.Empty) image.MakeTransparent(_transparentColor);
                lock(ImageLock) {
                    if(_images.Count > index && _images[index] != null) {
                        using(Graphics graphics = Graphics.FromImage(_images[index])) { graphics.Clear(Color.Transparent); graphics.DrawImage(image, 0, 0); }
                        image.Dispose();
                    }
                    else _images.Add(image);
                }
                index++; width -= _size.Width; rectangle.X += _size.Width;
            }
        }
        public void Dispose() { foreach(Bitmap bitmap in _images) if(bitmap != null) bitmap.Dispose(); _images.Clear(); }
        internal Bitmap this[int index] { get { return _images[index]; } }
        internal Color TransparentColor { set { _transparentColor = value; } }
    }
}
