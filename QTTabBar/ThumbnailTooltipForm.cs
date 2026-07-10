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
    /**
     * Ԥ������
     */
    internal sealed partial class ThumbnailTooltipForm : Form {
        private const string EMPTYFILE = "  *empty file";
        private bool fFontAsigned;
        private bool fIsShownByKey;
        private ImageCacheStore imageCacheStore;
        private const string IOERROR_CANNOTACCESS = "  *Access Error!";
        private static IOException ioException;
        private bool isShowing;
        private Label lblInfo;
        private Label lblText;
        private List<string> lstPathFailedThumbnail;
        private const int MAX_CACHE_LENGTH = 0x80;
        private const int MAX_TEXT_LENGTH = 0x400;
        public const int MAX_THUMBNAIL_HEIGHT = 0x4b0;
        public const int MAX_THUMBNAIL_WIDTH = 0x780;
        private int maxHeight = Config.Tips.PreviewMaxHeight;
        private int maxWidth = Config.Tips.PreviewMaxWidth;
        private PictureBox pictureBox1;
        public event QEventHandler ThumbnailVisibleChanged;
       //  static readonly string BomMarkUtf8String = Encoding.UTF8.GetString(BomMarkUtf8);



        /// <summary>
        /// static fields.
        /// </summary>
        private static string supportedImages;
        // ֧�ֵ���Ƶ��ʽ
        private static string supportedMovies = ".asx;.dvr-ms;.mp2;.flv;..mkv;.ts;.3g2;.3gp;.3gp2;.3gpp;.amr;.amv;.asf;.avi;.bdmv;.bik;.d2v;.divx;.drc;.dsa;.dsm;.dss;.dsv;.evo;.f4v;.flc;.fli;.flic;.flv;.hdmov;.ifo;.ivf;.m1v;.m2p;.m2t;.m2ts;.m2v;.m4b;.m4p;.m4v;.mkv;.mp2v;.mp4;.mp4v;.mpe;.mpeg;.mpg;.mpls;.mpv2;.mpv4;.mov;.mts;.ogm;.ogv;.pss;.pva;.qt;.ram;.ratdvd;.rm;.rmm;.rmvb;.roq;.rpm;.smil;.smk;.swf;.tp;.tpr;.ts;.vob;.vp6;.webm;.wm;.wmp;.wmv";





        public ThumbnailTooltipForm() {
            InitializeComponent();
            lstPathFailedThumbnail = new List<string>();
            imageCacheStore = new ImageCacheStore(0x80);
        }

        public void ClearCache() {
            imageCacheStore.Clear();
        }

        private bool CreateThumbnail(string path, ref Size formSize) {
            string ext = Path.GetExtension(path).ToLower();
            if(ExtIsImage(ext)) {
                FileInfo info = new FileInfo(path);
                if(!info.Exists || (info.Length <= 0L)) {
                    return false;
                }
                bool flag = false;
                bool thumbnail = false;
                bool fCached = false;
                Bitmap bitmap = null;
                ImageData item = null;
                Size empty = Size.Empty;
                Size sizeActual = Size.Empty;
                lblInfo.Text = string.Empty;
                string toolTipText = null;
                if((maxWidth != Config.Tips.PreviewMaxWidth) || (maxHeight != Config.Tips.PreviewMaxHeight)) {
                    maxWidth = Config.Tips.PreviewMaxWidth;
                    maxHeight = Config.Tips.PreviewMaxHeight;
                    pictureBox1.Image = null;
                    imageCacheStore.Clear();
                    lstPathFailedThumbnail.Clear();
                }
                foreach(ImageData data2 in imageCacheStore) {
                    if(data2.Path.PathEquals(path)) {
                        if(data2.ModifiedDate == info.LastWriteTime) {
                            bitmap = data2.Bitmap;
                            thumbnail = data2.Thumbnail;
                            empty = data2.RawSize;
                            sizeActual = data2.ZoomedSize;
                            toolTipText = data2.TooltipText;
                            flag = true;
                        }
                        else {
                            item = data2;
                        }
                        break;
                    }
                }
                if(item != null) {
                    imageCacheStore.Remove(item);
                }
                if(!flag) {
                    try {
                        ImageData data3;
                        if(!ExtIsDefaultImage(ext)) {
                            if(lstPathFailedThumbnail.Contains(path)) {
                                return false;
                            }
                            thumbnail = true;
                            if(!OSDetector.IsXP) {
                                data3 = ThumbnailImageLoader.LoadThumbnail(path, info.LastWriteTime, out empty, out sizeActual, out toolTipText, out fCached);
                            }
                            else {
                                data3 = ThumbnailImageLoader.LoadThumbnail2(path, info.LastWriteTime, out empty, out sizeActual, out toolTipText, out fCached);
                            }
                        }
                        else {
                            data3 = ThumbnailImageLoader.LoadImageFile(path, info.LastWriteTime, out empty, out sizeActual);
                        }
                        if(data3 == null) {
                            lstPathFailedThumbnail.Add(path);
                            return false;
                        }
                        bitmap = data3.Bitmap;
                        imageCacheStore.Add(data3);
                    }
                    catch (Exception e)
                    {
                        QTLogger.MakeErrorLog(e, "CreateThumbnail");
                        return false;
                    }
                }
                int width = 0x9e;
                if(width < sizeActual.Width) {
                    width = sizeActual.Width;
                }
                bool flag4 = false;
                if(Config.Tips.ShowPreviewInfo) {
                    SizeF ef;
                    string text = Path.GetFileName(path) + "\r\n";
                    if(thumbnail && (toolTipText != null)) {
                        text = text + toolTipText;
                    }
                    else {
                        bool flag5 = sizeActual == empty;
                        text = text + FormatSize(info.Length);
                        if(!thumbnail) {
                            object obj2 = text;
                            text = string.Concat(new object[] { obj2, "    ( ", empty.Width, " x ", empty.Height, " )", flag5 ? string.Empty : "*" });
                        }
                        text = text + "\r\n" + info.LastWriteTime;
                    }
                    using(Graphics graphics = lblInfo.CreateGraphics()) {
                        ef = graphics.MeasureString(text, lblInfo.Font, (width - 8));
                    }
                    lblInfo.SuspendLayout();
                    lblInfo.Text = text;
                    lblInfo.Width = width;
                    lblInfo.Height = (int)(ef.Height + 8f);
                    lblInfo.ResumeLayout();
                    formSize = new Size(width + 8, (sizeActual.Height + lblInfo.Height) + 8);
                }
                else {
                    flag4 = true;
                    formSize = new Size(width + 8, sizeActual.Height + 8);
                }
                try {
                    SuspendLayout();
                    if(flag4) {
                        lblInfo.Dock = DockStyle.None;
                    }
                    else {
                        lblInfo.Dock = DockStyle.Bottom;
                        lblInfo.BringToFront();
                    }
                    pictureBox1.SuspendLayout();
                    pictureBox1.SizeMode = (sizeActual != bitmap.Size) ? PictureBoxSizeMode.Zoom : PictureBoxSizeMode.CenterImage;
                    pictureBox1.Image = bitmap;
                    pictureBox1.ResumeLayout();
                    pictureBox1.BringToFront();
                    ResumeLayout();
                    return true;
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                    return false;
                }
            }
            if(ExtIsText(ext)) { // ���Ԥ�������ı��ļ�
                FileInfo textFileInfo = new FileInfo(path);
                if(textFileInfo.Exists) {
                    try {
                        SizeF sizeF;
                        bool fLoadedAll = false;
                        bool isEmptyText = false;
                        string content;
                        ioException = null;
                        // ����Ԥ�����߼�

                        /*if (textFileInfo.Length > 0L && textFileInfo.Length <= MAX_TEXT_LENGTH)
                        {
                            content = LoadTextFile(path, out fLoadedAll);
                        }
                        else if (textFileInfo.Length > 0L && textFileInfo.Length > MAX_TEXT_LENGTH)
                        {
                            content = LoadTextFile(path, MAX_TEXT_LENGTH, out fLoadedAll);
                        }*/

                        if (textFileInfo.Length > 0L)
                        {
                            // content = LoadTextFile2(path, out fLoadedAll);
                            content = LoadTextFile3(path, out fLoadedAll);
                        }
                        else {
                            isEmptyText = true;
                            // str4 = "  *empty file";
                            content = EMPTYFILE;
                        }
                        lblText.ForeColor = (ioException != null) ? Color.Red : (isEmptyText ? SystemColors.GrayText : SystemColors.InfoText);
                        try {
                            lblText.Font = Config.Tips.PreviewFont;
                            fFontAsigned = true;
                        }
                        catch (Exception e)
                        {
                            QTLogger.MakeErrorLog(e, "ExtIsText");
                            fFontAsigned = false;
                        }
                        int num2 = 0x100;
                        if(fFontAsigned) {
                            num2 = Math.Max((int)(num2 * (Config.Tips.PreviewFont.SizeInPoints / DefaultFont.Size)), 0x80);
                            formSize.Width = num2;
                        }
                        using(Graphics graphics2 = lblText.CreateGraphics()) {
                            sizeF = graphics2.MeasureString(content, lblText.Font, num2);
                        }
                        if((sizeF.Height < 512f) || fLoadedAll) {
                            formSize.Height = (int)(sizeF.Height + 8f);
                        }
                        else {
                            formSize.Height = 0x200;
                        }
                        SuspendLayout();
                        lblInfo.Dock = DockStyle.None;
                        lblText.Text = content;
                        lblText.BringToFront();
                        ResumeLayout();
                        return true;
                    }
                    catch(Exception exception2) {
                        QTLogger.MakeErrorLog(exception2, null);
                        return false;
                    }
                }
            }
            return false;
        }

        protected override void Dispose(bool disposing) {
            imageCacheStore.Clear();
            base.Dispose(disposing);
        }

        private static bool ExtIsDefaultImage(string ext) {
            return GetGDIPSupportedImages().Contains(ext.ToLower());
        }

        private static bool ExtIsImage(string ext) {
            return (ext.Length != 0 && Config.Tips.ImageExt.Contains(ext.ToLower()) && ext != ".ico");
        }

        public static bool ExtIsSupported(string ext) {
            if(!ExtIsImage(ext)) {
                return ExtIsText(ext);
            }
            return true;
        }

        private static bool ExtIsText(string ext) {
            return (ext.Length != 0 && Config.Tips.TextExt.Contains(ext.ToLower()));
        }

        private static string FormatSize(long size) {
            string str = size + " bytes";
            if(size >= 0x400L) {
                str = Math.Round(((size) / 1024.0), 1) + " KB";
            }
            if(size >= 0x100000L) {
                str = Math.Round(((size) / 1048576.0), 1) + " MB";
            }
            return str;
        }

        private static string GetGDIPSupportedImages() {
            if(supportedImages == null) {
                ImageCodecInfo[] imageDecoders = ImageCodecInfo.GetImageDecoders();
                StringBuilder builder = new StringBuilder();
                foreach(ImageCodecInfo info in imageDecoders) {
                    builder.Append(info.FilenameExtension + ";");
                }
                supportedImages = builder.ToString()
                    .ToLower()
                    .Replace("*", string.Empty)
                    .Replace(".ico;", string.Empty);
            }
            return supportedImages;
        }

        public bool HideToolTip() {
            if(fIsShownByKey) {
                fIsShownByKey = false;
                return false;
            }
            isShowing = false;
            PInvoke.ShowWindow(Handle, 0);
            pictureBox1.Image = null;
            if(ThumbnailVisibleChanged != null) {
                ThumbnailVisibleChanged(this, new QEventArgs(ArrowDirection.Down));
            }
            return true;
        }

        private void InitializeComponent() {
            pictureBox1 = new PictureBox();
            lblText = new Label();
            lblInfo = new Label();
            ((ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            lblInfo.ForeColor = SystemColors.InfoText;
            lblInfo.BackColor = Color.Transparent;
            lblInfo.Dock = DockStyle.Bottom;
            lblInfo.Padding = new Padding(4);
            lblInfo.Size = new Size(0x10, 50);
            lblInfo.UseMnemonic = false;
            pictureBox1.BackColor = Color.Transparent;
            pictureBox1.Dock = DockStyle.Fill;
            pictureBox1.Location = new Point(0, 0);
            pictureBox1.Padding = new Padding(4);
            pictureBox1.Size = new Size(0x100, 0x80);
            pictureBox1.SizeMode = PictureBoxSizeMode.CenterImage;
            pictureBox1.TabStop = false;
            lblText.AutoEllipsis = true;
            lblText.ForeColor = SystemColors.InfoText;
            lblText.BackColor = Color.Transparent;
            lblText.Dock = DockStyle.Fill;
            lblText.Location = new Point(0, 0);
            lblText.Padding = new Padding(4);
            lblText.Size = new Size(0x100, 0x80);
            lblText.UseMnemonic = false;
            AutoScaleDimensions = new SizeF(6f, 13f);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Info;
            ClientSize = new Size(0x100, 0x80);
            Controls.Add(lblText);
            Controls.Add(pictureBox1);
            Controls.Add(lblInfo);
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            ((ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
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

        private static string LoadTextFile3(string path, out bool fLoadedAll)
        {
            byte[] buffer = null;
            int count = MAX_TEXT_LENGTH;
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
                ioException = exception;
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
            int count = MAX_TEXT_LENGTH;
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
                ioException = exception;
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

        // Batch10 GC10a: implementation moved into the nested ThumbnailImageLoader.
        // A thin facade is kept so existing callers/tests (ResourceReleaseTests)
        // can still reach it via ThumbnailTooltipForm.CreateManagedBitmapAndReleaseHandle.
        internal static Bitmap CreateManagedBitmapAndReleaseHandle(
                IntPtr hBitmap, Func<IntPtr, Bitmap> fromHbitmap, Func<IntPtr, bool> deleteObject) {
            return ThumbnailImageLoader.CreateManagedBitmapAndReleaseHandle(hBitmap, fromHbitmap, deleteObject);
        }

        internal static List<string> MakeDefaultImgExts() {
            StringBuilder builder = new StringBuilder();
            builder.Append(GetGDIPSupportedImages());
            builder.Append(supportedMovies);
            var strs = builder.ToString();
            if (PathValidator.IsEmptyStr(strs))
            {
                return new List<string>();
            }
            return new List<string>(strs.Split(QTUtility.SEPARATOR_CHAR));
        }

        protected override void OnPaintBackground(PaintEventArgs e) {
            if(!OSDetector.IsXP && VisualStyleRenderer.IsSupported) {
                new VisualStyleRenderer(VisualStyleElement.ToolTip.Standard.Normal).DrawBackground(e.Graphics, new Rectangle(0, 0, Width, Height));
            }
            else {
                base.OnPaintBackground(e);
                e.Graphics.DrawRectangle(SystemPens.InfoText, new Rectangle(0, 0, Width - 1, Height - 1));
            }
        }

        public bool ShowToolTip(string path, Point pnt) {
            Size formSize = new Size(0x100, 0x80);
            if(!CreateThumbnail(path, ref formSize)) {
                return false;
            }
            Rectangle workingArea = Screen.FromPoint(pnt).WorkingArea;
            int num = ((formSize.Width + pnt.X) - workingArea.Right) + 8;
            int num2 = ((formSize.Height + pnt.Y) - workingArea.Bottom) + 0x10;
            bool flag = (workingArea.Right - pnt.X) < (pnt.X - workingArea.Left);
            bool flag2 = (workingArea.Bottom - pnt.Y) < (pnt.Y - workingArea.Top);
            bool flag3 = false;
            if((num > 0) && (num2 > 0)) {
                if(flag) {
                    pnt.X -= ((formSize.Width + 0x18) + 0x20) + 0x10;
                }
                if(flag2) {
                    pnt.Y -= formSize.Height + 0x20;
                    flag3 = true;
                }
            }
            else if(num > 0) {
                if(flag) {
                    pnt.X -= num + 0x20;
                }
            }
            else if(num2 > 0) {
                pnt.Y -= num2 + 0x10;
                flag3 = true;
            }
            if(pnt.X < workingArea.X) {
                pnt.X = workingArea.X + 8;
                if(!flag3) {
                    pnt.Y += 8;
                }
            }
            isShowing = true;
            PInvoke.SetWindowPos(Handle, (IntPtr)(-1), pnt.X + 0x18, pnt.Y + 0x10, formSize.Width, formSize.Height, 0x10);
            PInvoke.ShowWindow(Handle, 4);
            if(ThumbnailVisibleChanged != null) {
                ThumbnailVisibleChanged(this, new QEventArgs(ArrowDirection.Up));
            }
            return true;
        }

        public bool ShowToolTip(string path, Rectangle rctMenuItem) {
            Size formSize = new Size(0x100, 0x80);
            if(!CreateThumbnail(path, ref formSize)) {
                return false;
            }
            Point point = new Point(rctMenuItem.Right + 8, rctMenuItem.Bottom);
            Rectangle workingArea = Screen.FromPoint(rctMenuItem.Location).WorkingArea;
            bool flag = (workingArea.Right - point.X) < (point.X - workingArea.Left);
            bool flag2 = false;
            if(((((formSize.Width + point.X) - workingArea.Right) + 8) > 0) && flag) {
                point.X = (rctMenuItem.X - formSize.Width) - 8;
            }
            if((((formSize.Height + point.Y) - workingArea.Bottom) + 8) > 0) {
                point.Y = (workingArea.Bottom - formSize.Height) - 0x10;
                flag2 = true;
            }
            if(point.X < workingArea.X) {
                point.X = workingArea.X + 8;
                if(!flag2) {
                    point.Y += 8;
                }
            }
            isShowing = true;
            PInvoke.SetWindowPos(Handle, (IntPtr)(-1), point.X, point.Y, formSize.Width, formSize.Height, 0x10);
            PInvoke.ShowWindow(Handle, 4);
            return true;
        }

        protected override CreateParams CreateParams {
            get {
                CreateParams createParams = base.CreateParams;
                createParams.ClassStyle |= 0x20000;
                return createParams;
            }
        }

        public bool IsShowing {
            get {
                return isShowing;
            }
        }

        public bool IsShownByKey {
            get {
                return fIsShownByKey;
            }
            set {
                fIsShownByKey = value;
            }
        }

        private sealed class ImageCacheStore : Collection<ImageData> {
            private int max_cache_length;
            private object syncObject = new object();

            public ImageCacheStore(int max_cache_length) {
                this.max_cache_length = max_cache_length;
            }

            protected override void ClearItems() {
                lock(syncObject) {
                    foreach(ImageData data in this) {
                        data.Dispose();
                    }
                    base.ClearItems();
                }
            }

            protected override void InsertItem(int index, ImageData item) {
                lock(syncObject) {
                    base.InsertItem(index, item);
                    if(Count > max_cache_length) {
                        base[0].Dispose();
                        base.RemoveItem(0);
                    }
                }
            }

            protected override void RemoveItem(int index) {
                lock(syncObject) {
                    base[index].Dispose();
                    base.RemoveItem(index);
                }
            }

            protected override void SetItem(int index, ImageData item) {
                lock(syncObject) {
                    base.SetItem(index, item);
                }
            }
        }

        private sealed class ImageData : IDisposable {
            public Bitmap Bitmap;
            public DateTime ModifiedDate;
            public MemoryStream ms;
            public string Path;
            public Size RawSize;
            public bool Thumbnail;
            public string TooltipText;
            public Size ZoomedSize;

            public ImageData(Bitmap bmp, MemoryStream memoryStream, string path, DateTime dtModified, Size sizeRaw, Size sizeZoomed) {
                Bitmap = bmp;
                ms = memoryStream;
                Path = path;
                ModifiedDate = dtModified;
                RawSize = sizeRaw;
                ZoomedSize = sizeZoomed;
            }

            public void Dispose() {
                try {
                    if(Bitmap != null) {
                        Bitmap.Dispose();
                        Bitmap = null;
                    }
                    if(ms != null) {
                        ms.Dispose();
                        ms = null;
                    }
                }
                catch(Exception exception) {
                    QTLogger.MakeErrorLog(exception);
                }
            }
        }
    }
}
