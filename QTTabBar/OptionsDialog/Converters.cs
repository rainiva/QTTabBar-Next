using System;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using QTTabBarLib.Interop;
using Bitmap = System.Drawing.Bitmap;
using Font = System.Drawing.Font;

namespace QTTabBarLib {
    #region ---------- Converters ----------

    // Inverts the value of a boolean
    internal class BoolInverterConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool ? !(bool)value : value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return value is bool ? !(bool)value : value;
        }
    }

    // Converts between booleans and one using logical and.
    internal class LogicalAndMultiConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            return values.All(b => b is bool && (bool)b);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return new object[] { value };
        }
    }

    // Converts between many booleans and a string by StringJoining them.
    internal class BoolJoinMultiConverter : IMultiValueConverter {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
            return values.StringJoin(",");
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
            return ((string)value).Split(',').Select(s => (object)bool.Parse(s)).ToArray();
        }
    }

    // Converts between a boolean and a string by comparing the string to the 
    // passed parameter.
    [ValueConversion(typeof(string), typeof(bool))]
    internal class StringEqualityConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return (string)parameter == (string)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            return (bool)value ? parameter : Binding.DoNothing;
        }
    }

    // Converts Bitmaps to ImageSources.
    [ValueConversion(typeof(Bitmap), typeof(ImageSource))]
    internal class BitmapToImageSourceConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value == null || !(value is Bitmap)) return null;
            IntPtr hBitmap = ((Bitmap)value).GetHbitmap();
            try {
                return Imaging.CreateBitmapSourceFromHBitmap(hBitmap, IntPtr.Zero,
                        Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally {
                PInvoke.DeleteObject(hBitmap);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    // Converts between Colors and Brushes
    [ValueConversion(typeof(System.Drawing.Color), typeof(Brush))]
    internal class ColorToBrushConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            var c = (System.Drawing.Color)(value ?? System.Drawing.Color.Red);
            return new SolidColorBrush(Color.FromArgb(c.A, c.R, c.G, c.B));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    // Converts between Fonts and strings.
    [ValueConversion(typeof(Font), typeof(string))]
    internal class FontStringConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            if(value == null) return "";
            Font font = (Font)value;
            return string.Format("{0}, {1} pt", font.Name, Math.Round(font.SizeInPoints));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    // Converts an object to its class name.
    [ValueConversion(typeof(object), typeof(string))]
    internal class ObjectToClassNameConverter : IValueConverter {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
            return value == null ? null : value.GetType().Name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) {
            throw new NotSupportedException();
        }
    }

    #endregion
}
