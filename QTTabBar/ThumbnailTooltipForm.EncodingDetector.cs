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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using BandObjectLib;
using MultiLanguage;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    internal sealed partial class ThumbnailTooltipForm {
        // Batch10 GC10c: charset-detection cluster extracted from the
        // ThumbnailTooltipForm god class into this nested static helper. Pure
        // static utility; no dependency on form instance state.
        private static class EncodingDetector {
        private  static byte[] _utf16BeBom =
        {
            0xFE,
            0xFF
        };

        private  static byte[] _utf16LeBom =
        {
            0xFF,
            0xFE
        };

        private  static byte[] _utf8Bom =
        {
            0xEF,
            0xBB,
            0xBF
        };

        private static Encoding Encoding950 = Encoding.GetEncoding(950);
        private static Encoding Encoding936 = Encoding.GetEncoding(936);
        private static Encoding EncodingUTF16LeBom = (Encoding)new UnicodeEncoding(false, true);
        private static Encoding EncodingUTF16BeBom = (Encoding)new UnicodeEncoding(true, true);
        private static Encoding EncodingUTF16LeNoBom = (Encoding)new UnicodeEncoding(false, false);
        private static Encoding EncodingUTF16BeNoBom = (Encoding)new UnicodeEncoding(true, false);
        private static Encoding EncodingUTF8Bom = (Encoding)new UTF8Encoding(true);
        private static Encoding EncodingUTF8NoBom = (Encoding)new UTF8Encoding(false);
        static readonly byte[] BomMarkUtf8 = Encoding.UTF8.GetPreamble();
        private static bool _nullSuggestsBinary = true;
        private static double _utf16ExpectedNullPercent = 70;
        private static double _utf16UnexpectedNullPercent = 10;

        /// <summary> 
        /// �����ļ���·������ȡ�ļ��Ķ��������ݣ��ж��ļ��ı������� 
        /// </summary> 
        /// <param name=��FILE_NAME��>�ļ�·��</param> 
        /// <returns>�ļ��ı�������</returns> 
        public static System.Text.Encoding GetType(string FILE_NAME)
        {
            using(FileStream fs = new FileStream(FILE_NAME, FileMode.Open, FileAccess.Read)) {
                return GetType(fs);
            }
        }

        /// <summary> 
        /// ͨ���������ļ������ж��ļ��ı������� 
        /// </summary> 
        /// <param name=��fs��>�ļ���</param> 
        /// <returns>�ļ��ı�������</returns> 
        public static System.Text.Encoding GetType(FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //��BOM 
            Encoding reVal = Encoding.Default;

            BinaryReader r = new BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = Encoding.Unicode;
            }
            if (r != null) {
                r.Close();
               // r.Dispose();
            }
            
           // QTUtility2.Close(streamReader);
            return reVal;

        } 

        /// <summary> 
        /// �ж��Ƿ��BOM��UTF8��ʽ�����㷽����
        /// BOM��Byte Order Mark�������ֽ�˳��
        /// UTF-8����ҪBOM�����ֽ�˳�򣬵���BOM����ʾ���뷽ʽ��
        /// Windows���ǲ���BOM������ı��ļ��ı��뷽ʽ�ģ�
        /// ���԰�UTF-8��ASCII�ȱ������ֿ�����
        /// ����Windows֮�⣨�磬Linux ������������⡣
        /// </summary> 
        /// <param name="data"></param> 
        /// <returns></returns> 
        private static bool IsUTF8Bytes(byte[] data)
        {
            // �ֽ���
            int charByteCounter = 1;
            // ��ǰ�ֽ�
            byte curByte;
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        // �жϵ�ǰ 
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        // ���λ��λ��Ϊ��0 ��������2��1��ʼ
                        // ��:110XXXXX...........1111110X 
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    // ����UTF-8 ��ʱ��һλ����Ϊ1 
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("FileLoader.IsUTF8Bytes ERROR: UNEXPECTED byte FORMAT!");
            }
            return true;
        }

        public static Encoding DetectEncoding(byte[] bytes)
        {
            if ((bytes == null) || (bytes.Length == 0))
            {
                return Encoding.Default;
            }
            MultiLanguage.IMultiLanguage2 lang = new MultiLanguage.CMultiLanguageClass();
            // IMultiLanguage2 lang = (IMultiLanguage2)new MultiLanguage();
            int len = bytes.Length;
            DetectEncodingInfo info = new DetectEncodingInfo();
            int scores = 1;

            // bytes to IntPtr
            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            IntPtr pbytes = Marshal.UnsafeAddrOfPinnedArrayElement(bytes, 0);

            try
            {
                // setup options (none)   
                MultiLanguage.MLDETECTCP options = MultiLanguage.MLDETECTCP.MLDETECTCP_NONE;
                lang.DetectInputCodepage(0, 0,  pbytes, ref len, out info, ref scores);
            }
            catch
            {
                info.nCodePage = (uint)Encoding.Default.CodePage;
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(lang);
            }
            if (info.nCodePage == Encoding.ASCII.CodePage)
            {
                //ASCII�ΤȤ���UTF-8�ˤ���
                return Encoding.UTF8;
            }
            return Encoding.GetEncoding((int)info.nCodePage);
        }


        /// <summary>
        /// Detect the most probable codepage from an byte array
        /// </summary>
        /// <param name="input">array containing the raw data</param>
        /// <returns>the detected encoding or the default encoding if the detection failed</returns>
        public static Encoding DetectInputCodepage(byte[] input)
        {
            try
            {
                Encoding[] detected = DetectInputCodepages(input, 1);
                if (detected.Length > 0)
                    return detected[0];
                return Encoding.Default;
            }
            catch (COMException)
            {
                // return default codepage on error
                return Encoding.Default;
            }
        }

        /// <summary>
        /// Rerurns up to maxEncodings codpages that are assumed to be apropriate
        /// </summary>
        /// <param name="input">array containing the raw data</param>
        /// <param name="maxEncodings">maxiumum number of encodings to detect</param>
        /// <returns>an array of Encoding with assumed encodings</returns>
        public static Encoding[] DetectInputCodepages(byte[] input, int maxEncodings)
        {
            // StopWatch.Start("DetectInputCodepages_" + Thread.CurrentThread.ManagedThreadId);

            if (maxEncodings < 1)
                throw new ArgumentOutOfRangeException("at least one encoding must be returend", "maxEncodings");

            if (input == null)
                throw new ArgumentNullException("input");

            // empty strings can always be encoded as Default ASCII
            if (input.Length == 0)
                return new Encoding[] { Encoding.ASCII };

            // expand the string to be at least 256 bytes
            if (input.Length < 256)
            {
                byte[] newInput = new byte[256];
                int steps = 256 / input.Length;
                for (int i = 0; i < steps; i++)
                    Array.Copy(input, 0, newInput, input.Length * i, input.Length);

                int rest = 256 % input.Length;
                if (rest > 0)
                    Array.Copy(input, 0, newInput, steps * input.Length, rest);
                input = newInput;
            }

            List<Encoding> result = new List<Encoding>();

            // get the IMultiLanguage" interface
            MultiLanguage.IMultiLanguage2 multilang2 = new MultiLanguage.CMultiLanguageClass();
            if (multilang2 == null)
                throw new System.Runtime.InteropServices.COMException("Failed to get IMultilang2");
            try
            {
                MultiLanguage.DetectEncodingInfo[] detectedEncdings = new MultiLanguage.DetectEncodingInfo[maxEncodings];

                int scores = detectedEncdings.Length;
                int srcLen = input.Length;

                // setup options (none)   
                MultiLanguage.MLDETECTCP options = MultiLanguage.MLDETECTCP.MLDETECTCP_NONE;

                // finally... call to DetectInputCodepage
                multilang2.DetectInputCodepage(options, 0,
                    ref input[0], ref srcLen, ref detectedEncdings[0], ref scores);

                // get result
                if (scores > 0)
                {
                    for (int i = 0; i < scores; i++)
                    {
                        // add the result
                        result.Add(Encoding.GetEncoding((int)detectedEncdings[i].nCodePage));
                    }
                }
            }
            finally
            {
                Marshal.FinalReleaseComObject(multilang2);
            }
            // nothing found
            return result.ToArray();
        }

        /**
         * codepage=936 ��������GBK
            codepage=950 ��������BIG5
            codepage=437 ����/���ô�Ӣ��
            codepage=932 ����
            codepage=949 ����
            codepage=866 ����
         */
        public static Encoding TryGetEncoding(byte[] bytes)
        {
            Encoding encoding = CheckBom(bytes);
            if (encoding != null)
            {
                return encoding;
            }

            // Now check for valid UTF8
            encoding = CheckUtf8(bytes);
            if (encoding != null)
            {
                return encoding;
            }

            // Now try UTF16 
            encoding = CheckUtf16NewlineChars(bytes);
            if (encoding != null)
            {
                return encoding;
            }

            encoding = CheckUtf16Ascii(bytes);
            if (encoding != null)
            {
                return encoding;
            }

            // ANSI or None (binary) then
            if (!DoesContainNulls(bytes))
            {
                return Encoding.Default;
            }
           
            if (bytes.Length > 4)
            {
                if (bytes[0] == (byte)0 && bytes[1] == (byte)0 && bytes[2] == (byte)254 && bytes[3] == byte.MaxValue)
                {
                    return (Encoding)new UTF32Encoding(true, true);
                }
                if (bytes[0] == byte.MaxValue && bytes[1] == (byte)254 && bytes[2] == (byte)0 && bytes[3] == (byte)0)
                {
                    return (Encoding)new UTF32Encoding(false, true);
                }
            }
            return null;
        }


        /// <summary>
        /// Checks if a buffer contains any nulls. Used to check for binary vs text data.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="size">The size of the byte buffer.</param>
        private static bool DoesContainNulls(byte[] buffer)
        {
            int size = buffer.Length;
            uint pos = 0;
            while (pos < size)
            {
                if (buffer[pos++] == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Checks if a buffer contains text that looks like utf16. This is done based
        ///     on the use of nulls which in ASCII/script like text can be useful to identify.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>Encoding.none, Encoding.Utf16LeNoBom or Encoding.Utf16BeNoBom.</returns>
        private static Encoding CheckUtf16Ascii(byte[] buffer)
        {
            int size = buffer.Length;
            var numOddNulls = 0;
            var numEvenNulls = 0;

            // Get even nulls
            uint pos = 0;
            while (pos < size)
            {
                if (buffer[pos] == 0)
                {
                    numEvenNulls++;
                }

                pos += 2;
            }

            // Get odd nulls
            pos = 1;
            while (pos < size)
            {
                if (buffer[pos] == 0)
                {
                    numOddNulls++;
                }

                pos += 2;
            }

            double evenNullThreshold = numEvenNulls * 2.0 / size;
            double oddNullThreshold = numOddNulls * 2.0 / size;
            double expectedNullThreshold = _utf16ExpectedNullPercent / 100.0;
            double unexpectedNullThreshold = _utf16UnexpectedNullPercent / 100.0;

            // Lots of odd nulls, low number of even nulls
            if (evenNullThreshold < unexpectedNullThreshold && oddNullThreshold > expectedNullThreshold)
            {
                return EncodingUTF16LeNoBom;
            }

            // Lots of even nulls, low number of odd nulls
            if (oddNullThreshold < unexpectedNullThreshold && evenNullThreshold > expectedNullThreshold)
            {
                return EncodingUTF16BeNoBom;
            }

            // Don't know
            return null;
        }


        /// <summary>
        ///     Checks if a buffer contains text that looks like utf16 by scanning for
        ///     newline chars that would be present even in non-english text.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <returns>Encoding.none, Encoding.Utf16LeNoBom or Encoding.Utf16BeNoBom.</returns>
        private static Encoding CheckUtf16NewlineChars(byte[] buffer)
        {
            int size = buffer.Length;
            if (size < 2)
            {
                return null;
            }

            // Reduce size by 1 so we don't need to worry about bounds checking for pairs of bytes
            size--;

            var leControlChars = 0;
            var beControlChars = 0;

            uint pos = 0;
            while (pos < size)
            {
                byte ch1 = buffer[pos++];
                byte ch2 = buffer[pos++];

                if (ch1 == 0)
                {
                    if (ch2 == 0x0a || ch2 == 0x0d)
                    {
                        ++beControlChars;
                    }
                }
                else if (ch2 == 0)
                {
                    if (ch1 == 0x0a || ch1 == 0x0d)
                    {
                        ++leControlChars;
                    }
                }

                // If we are getting both LE and BE control chars then this file is not utf16
                if (leControlChars > 0 && beControlChars > 0)
                {
                    return null;
                }
            }

            if (leControlChars > 0)
            {
                return EncodingUTF16LeNoBom;
            }

            return beControlChars > 0 ? EncodingUTF16BeNoBom : null;
        }

        /// <summary>
        ///     Checks if a buffer contains valid utf8.
        /// </summary>
        /// <param name="buffer">The byte buffer.</param>
        /// <param name="size">The size of the byte buffer.</param>
        /// <returns>
        ///     Encoding type of Encoding.None (invalid UTF8), Encoding.Utf8NoBom (valid utf8 multibyte strings) or
        ///     Encoding.ASCII (data in 0.127 range).
        /// </returns>
        /// <returns>2</returns>
        private static  Encoding CheckUtf8(byte[] buffer)
        {
            // UTF8 Valid sequences
            // 0xxxxxxx  ASCII
            // 110xxxxx 10xxxxxx  2-byte
            // 1110xxxx 10xxxxxx 10xxxxxx  3-byte
            // 11110xxx 10xxxxxx 10xxxxxx 10xxxxxx  4-byte
            //
            // Width in UTF8
            // Decimal      Width
            // 0-127        1 byte
            // 194-223      2 bytes
            // 224-239      3 bytes
            // 240-244      4 bytes
            //
            // Subsequent chars are in the range 128-191
            var onlySawAsciiRange = true;
            uint pos = 0;
            int size = buffer.Length;
            while (pos < size)
            {
                byte ch = buffer[pos++];

                if (ch == 0 && _nullSuggestsBinary)
                {
                    return null;
                }

                int moreChars;
                if (ch <= 127)
                {
                    // 1 byte
                    moreChars = 0;
                }
                else if (ch >= 194 && ch <= 223)
                {
                    // 2 Byte
                    moreChars = 1;
                }
                else if (ch >= 224 && ch <= 239)
                {
                    // 3 Byte
                    moreChars = 2;
                }
                else if (ch >= 240 && ch <= 244)
                {
                    // 4 Byte
                    moreChars = 3;
                }
                else
                {
                    return null; // Not utf8
                }

                // Check secondary chars are in range if we are expecting any
                while (moreChars > 0 && pos < size)
                {
                    onlySawAsciiRange = false; // Seen non-ascii chars now

                    ch = buffer[pos++];
                    if (ch < 128 || ch > 191)
                    {
                        return null; // Not utf8
                    }

                    --moreChars;
                }
            }

            // If we get to here then only valid UTF-8 sequences have been processed

            // If we only saw chars in the range 0-127 then we can't assume UTF8 (the caller will need to decide)
            return onlySawAsciiRange ? null : EncodingUTF8NoBom;
        }


        public static Encoding CheckBom(byte[] buffer)
        {
            int size = buffer.Length;
            // Check for BOM
            if (size >= 2 && buffer[0] == _utf16LeBom[0] && buffer[1] == _utf16LeBom[1])
            {
                return EncodingUTF16LeBom; ;
            }

            if (size >= 2 && buffer[0] == _utf16BeBom[0] && buffer[1] == _utf16BeBom[1])
            {
                return EncodingUTF16BeBom;
            }

            if (size >= 3 && buffer[0] == _utf8Bom[0] && buffer[1] == _utf8Bom[1] && buffer[2] == _utf8Bom[2])
            {
                return EncodingUTF8Bom;
            }

            return null;
        }


        public static Encoding GetEncoding2(byte[] bytes)
        {
            if ((bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF))
            {
                return Encoding.UTF8;
            }
            else if (bytes[0] == 0xFE && bytes[1] == 0xFF && bytes[2] == 0x00)
            {
                return Encoding.BigEndianUnicode;
            }
            else if (bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x41)
            {
                return Encoding.Unicode;
            }
            else if (IsTragetEncoding(bytes, Encoding.UTF8))
            {
                return Encoding.UTF8;
            }
            else if (IsTragetEncoding(bytes, Encoding.Unicode))
            {
                return Encoding.Unicode;
            }
            else if (IsTragetEncoding(bytes, Encoding936))
            {
                return Encoding936;
            }
            else if (IsTragetEncoding(bytes, Encoding950))
            {
                return Encoding950;
            }
            else
            {
                return Encoding.Default;
            }
        }

        private static byte[] UTF8Preamble = Encoding.UTF8.GetPreamble();

        private static bool IsUtf8Bom(byte[] buffer)
        {
            var isUtf8Bom = false;
            if (buffer != null && buffer.Length > 2)
            {
                if (buffer[0] == UTF8Preamble[0]
                    && buffer[1] == UTF8Preamble[1]
                    && buffer[2] == UTF8Preamble[2])
                {
                    isUtf8Bom = true;
                }
            }
            return isUtf8Bom;
        }

        private static byte[] RemoveBom(byte[] buffer)
        {
            if (buffer != null && buffer.Length > 2)
            {
                byte[] bomBuffer = Encoding.UTF8.GetPreamble();
                while (IsUtf8Bom(buffer))
                {
                    buffer = buffer.Skip(3).ToArray();
                }
            }
            return buffer;
        }

        public static bool IsTragetEncoding(byte[] bytes, Encoding targetEncoding)
        {
            //��byte[]�D��string���D��byte[]��λԪ���Ƿ���׃
            var stringWithTragetEncoding = targetEncoding.GetString(bytes);
            var bytesWithTragetEncodingCount = targetEncoding.GetByteCount(stringWithTragetEncoding);
            return bytes.Length == bytesWithTragetEncodingCount;
        }

        /// <summary> 
        /// �ж��ļ����ı������� 
        /// </summary> 
        /// <param name="filestream">�ļ���</param> 
        /// <returns>���ı�������</returns> 
        private static Encoding GetStreamEncoding(byte[] ss)
        {
            try
            {
                byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
                byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
                //��BOM 
                byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF };
                Encoding reVal = Encoding.Default;
                if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
                {
                    reVal = Encoding.UTF8;
                }
                else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
                {
                    reVal = Encoding.BigEndianUnicode;
                }
                else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
                {
                    reVal = Encoding.Unicode;
                }
                return reVal;
            }
            catch (Exception ex)
            {
                throw new Exception("FileLoader.GetStreamEncoding ERROR:" + ex.Message);
            }
        }

        public static System.Text.Encoding detechBytes2(byte[] bytes, bool thorough)
        {
            if (bytes.Length >= 4 && bytes[0] == 0x00 && bytes[1] == 0x00 && bytes[2] == 0xFE && bytes[3] == 0xFF) // UTF32-BE 
                return System.Text.Encoding.GetEncoding("utf-32BE"); // UTF-32, big-endian 
            else if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0x00 && bytes[3] == 0x00) // UTF32-LE
                return System.Text.Encoding.UTF32; // UTF-32, little-endian
            // https://en.wikipedia.org/wiki/Byte_order_mark#cite_note-14    
            else if (bytes.Length >= 4 && bytes[0] == 0x2b && bytes[1] == 0x2f && bytes[2] == 0x76 && (bytes[3] == 0x38 || bytes[3] == 0x39 || bytes[3] == 0x2B || bytes[3] == 0x2F)) // UTF7
                return System.Text.Encoding.UTF7;  // UTF-7
            else if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) // UTF-8
                return System.Text.Encoding.UTF8;  // UTF-8
            else if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) // UTF16-BE
                return System.Text.Encoding.BigEndianUnicode; // UTF-16, big-endian
            else if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) // UTF16-LE
                return System.Text.Encoding.Unicode; // UTF-16, little-endian

            Encoding encoding = null;
            String text = null;
            // Test UTF8 with BOM. This check can easily be copied and adapted
            // to detect many other encodings that use BOMs.
            UTF8Encoding encUtf8Bom = new UTF8Encoding(true, true);
            Boolean couldBeUtf8 = true;
            Byte[] preamble = encUtf8Bom.GetPreamble();
            Int32 prLen = preamble.Length;
            if (bytes.Length >= prLen && preamble.SequenceEqual(bytes.Take(prLen)))
            {
                // UTF8 BOM found; use encUtf8Bom to decode.
                try
                {
                    // Seems that despite being an encoding with preamble,
                    // it doesn't actually skip said preamble when decoding...
                    return encUtf8Bom;
                }
                catch (ArgumentException)
                {
                    // Confirmed as not UTF-8!
                    couldBeUtf8 = false;
                }
            }
            // use boolean to skip this if it's already confirmed as incorrect UTF-8 decoding.
            if (couldBeUtf8 && encoding == null)
            {
                // test UTF-8 on strict encoding rules. Note that on pure ASCII this will
                // succeed as well, since valid ASCII is automatically valid UTF-8.
                UTF8Encoding encUtf8NoBom = new UTF8Encoding(false, true);
                try
                {
                    return encUtf8NoBom;
                }
                catch (ArgumentException)
                {
                    // Confirmed as not UTF-8!
                }
            }
            // fall back to default ANSI encoding.
            return Encoding.Default;
        }

        public static System.Text.Encoding detechBytes(byte[] b, bool thorough)
        {
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) // UTF32-BE 
                return System.Text.Encoding.GetEncoding("utf-32BE"); // UTF-32, big-endian 
            else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) // UTF32-LE
                return System.Text.Encoding.UTF32; // UTF-32, little-endian
            // https://en.wikipedia.org/wiki/Byte_order_mark#cite_note-14    
            else if (b.Length >= 4 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76 && (b[3] == 0x38 || b[3] == 0x39 || b[3] == 0x2B || b[3] == 0x2F)) // UTF7
                return System.Text.Encoding.UTF7;  // UTF-7
            else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) // UTF-8
                return System.Text.Encoding.UTF8;  // UTF-8
            else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) // UTF16-BE
                return System.Text.Encoding.BigEndianUnicode; // UTF-16, big-endian
            else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) // UTF16-LE
                return System.Text.Encoding.Unicode; // UTF-16, little-endian

            // Maybe there is a future encoding ...
            // PS: The above yields more than this - this doesn't find UTF7 ...
            if (thorough)
            {
                System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]>> lsPreambles =
                    new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]>>();

                foreach (System.Text.EncodingInfo ei in System.Text.Encoding.GetEncodings())
                {
                    System.Text.Encoding enc = ei.GetEncoding();

                    byte[] preamble = enc.GetPreamble();

                    if (preamble == null)
                        continue;

                    if (preamble.Length == 0)
                        continue;

                    if (preamble.Length > b.Length)
                        continue;

                    System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]> kvp =
                        new System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]>(enc, preamble);

                    lsPreambles.Add(kvp);
                } // Next ei

                // li.Sort((a, b) => a.CompareTo(b)); // ascending sort
                // li.Sort((a, b) => b.CompareTo(a)); // descending sort
                lsPreambles.Sort(
                    delegate(
                        System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]> kvp1,
                        System.Collections.Generic.KeyValuePair<System.Text.Encoding, byte[]> kvp2)
                    {
                        return kvp2.Value.Length.CompareTo(kvp1.Value.Length);
                    }
                );


                for (int j = 0; j < lsPreambles.Count; ++j)
                {
                    for (int i = 0; i < lsPreambles[j].Value.Length; ++i)
                    {
                        if (b[i] != lsPreambles[j].Value[i])
                        {
                            goto NEXT_J_AND_NOT_NEXT_I;
                        }
                    } // Next i 

                    return lsPreambles[j].Key;
                NEXT_J_AND_NOT_NEXT_I: continue;
                } // Next j 

            } // End if (thorough)

            return Encoding.Default;
        } // End Function BomInfo 

        }
    }
}
