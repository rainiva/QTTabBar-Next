//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2021  Quizo, Paul Accisano
//
//    QTTabBar is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    QTTabBar is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with QTTabBar.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;
using System.Text;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class ThumbnailTooltipForm {
        // Batch10 GC10b: text-file reading cluster extracted from the
        // ThumbnailTooltipForm god class into this nested static helper.
        private static class TextFileLoader {
        internal static string FormatSize(long size) {
            string str = size + " bytes";
            if(size >= 0x400L) {
                str = Math.Round(((size) / 1024.0), 1) + " KB";
            }
            if(size >= 0x100000L) {
                str = Math.Round(((size) / 1048576.0), 1) + " MB";
            }
            return str;
        }

        private static string LoadTextFile(string path, int count, out bool fLoadedAll)
        {
            using (StreamReader sr = new StreamReader(path, EncodingDetector.GetType(path)))
            {
                char[] chars = new char[count];
                int readCnt = sr.Read(chars, 0, count);
                string text = new string(chars, 0 , readCnt );
                fLoadedAll = false;
                // QTUtility2.Close(sr);
                return text;
            }
        }

        private static string LoadTextFile(string path, out bool fLoadedAll)
        {
            using (StreamReader sr = new StreamReader(path, EncodingDetector.GetType(path)))
            {
                string textall = sr.ReadToEnd();
                fLoadedAll = true;
                // QTUtility2.Close(sr);
                return textall;
            }
        }

        internal static string LoadTextFile3(string path, out bool fLoadedAll)
        {
            byte[] buffer = null;
            int count = ThumbnailTooltipForm.MAX_TEXT_LENGTH;
            string str = string.Empty;
            fLoadedAll = false;
            Encoding detechted = null;
            try
            {
                // using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                /*using (var reader = new StreamReader(path, Encoding.Default, true))
                {
                    if (reader.Peek() >= 0) // you need this!
                        reader.Read();

                    detechted = reader.CurrentEncoding;
                }*/
                using (FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read , count, FileOptions.Asynchronous))
                {
                    if (stream.Length < count)
                    {
                        fLoadedAll = true;
                        count = (int)stream.Length;
                    }
                    buffer = new byte[count];
                    stream.Read(buffer, 0, count);
                    // detechted = detechBytes(buffer, true);
                    // detechted = detechBytes2(buffer, true);
                }
            }
            catch (IOException exception)
            {
                ThumbnailTooltipForm.ioException = exception;
                return "  *Access Error!";
            }
            if (buffer.Length <= 0)
            {
                return str;
            }

            detechted = EncodingDetector.TryGetEncoding(buffer);
            if (detechted != null)
            {
                QTLogger.log(" try get encoding " + detechted.EncodingName + " " + detechted.CodePage);
                return detechted.GetString(buffer);
            }

            // detechted = DetectInputCodepage(buffer);
            detechted = EncodingDetector.DetectEncoding(buffer);
            if (detechted != null)
            {
                // QTLogger.log(" try get DetectInputCodepage " + detechted.EncodingName + " " + detechted.CodePage);
                QTLogger.log(" try get DetectEncoding " + detechted.EncodingName + " " + detechted.CodePage);
                return detechted.GetString(buffer);
            }
            return Encoding.Default.GetString(buffer);
        }

        private static string LoadTextFile2(string path, out bool fLoadedAll) {
            byte[] buffer;
            int count = ThumbnailTooltipForm.MAX_TEXT_LENGTH;
            string str = string.Empty;
            fLoadedAll = false;
            try {
                using(FileStream stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    if(stream.Length < count) {
                        fLoadedAll = true;
                        count = (int)stream.Length;
                    }
                    buffer = new byte[count];
                    stream.Read(buffer, 0, count);
                    // QTUtility2.Close(stream);
                }
            }
            catch(IOException exception) {
                ThumbnailTooltipForm.ioException = exception;
                return "  *Access Error!";
            }
            if(buffer.Length <= 0) {
                return str;
            }
            Encoding encoding = null;
            if(PluginManager.IEncodingDetector != null) {
                try {
                    encoding = PluginManager.IEncodingDetector.GetEncoding(ref buffer);
                }
                catch(Exception exception2) {
                    PluginManager.HandlePluginException(exception2, IntPtr.Zero, "Unknown IEncodingDetector", "Getting Encoding object.");
                    QTLogger.MakeErrorLog(exception2);
                }
            }
            if(encoding == null) {
                encoding = TxtEnc.GetEncoding(ref buffer);

                QTLogger.log("TxtEnc :" + encoding.EncodingName + " " + encoding.CodePage);

                if((encoding == null) ||
                   (((
                         (Encoding.Default.CodePage != 0x3a4) &&
                         (encoding.CodePage != 0xfde8)) && 
                     ((encoding.CodePage != 0xfde9) && 
                      (encoding.CodePage != 0x4b0))) 
                    && (encoding.CodePage != 0x2ee0))) {
                    encoding = Encoding.Default;
                }
            }
            QTLogger.log("Final :" + encoding.EncodingName + " " + encoding.CodePage);
            return encoding.GetString(buffer);
        }

        }
    }
}
