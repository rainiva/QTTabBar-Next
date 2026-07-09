//    This file is part of QTTabBar, a shell extension for Microsoft
//    Windows Explorer.
//    Copyright (C) 2007-2022  Quizo, Paul Accisano, indiff
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
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using QTTabBarLib.Interop;

namespace QTTabBarLib {
    public sealed partial class QTabControl : Control
    {
        protected override void Dispose(bool disposing) {
            if(disposing && (components != null)) {
                components.Dispose();
            }
            if(brshActive != null) {
                brshActive.Dispose();
                brshActive = null;
            }
            if(brshInactv != null) {
                brshInactv.Dispose();
                brshInactv = null;
            }
            if(sfTypoGraphic != null) {
                sfTypoGraphic.Dispose();
                sfTypoGraphic = null;
            }
            if(bmpLocked != null) {
                bmpLocked.Dispose();
                bmpLocked = null;
            }
            if(bmpCloseBtn_Cold != null) {
                bmpCloseBtn_Cold.Dispose();
                bmpCloseBtn_Cold = null;
            }
            if(bmpCloseBtn_Hot != null) {
                bmpCloseBtn_Hot.Dispose();
                bmpCloseBtn_Hot = null;
            }
            if(bmpCloseBtn_Pressed != null) {
                bmpCloseBtn_Pressed.Dispose();
                bmpCloseBtn_Pressed = null;
            }
            if(bmpCloseBtn_ColdAlt != null) {
                bmpCloseBtn_ColdAlt.Dispose();
            }
            if(bmpFolIconBG != null) {
                bmpFolIconBG.Dispose();
                bmpFolIconBG = null;
            }
            if(fnt_Underline != null) {
                fnt_Underline.Dispose();
                fnt_Underline = null;
            }
            if(fntBold != null) {
                fntBold.Dispose();
                fntBold = null;
            }
            if(fntBold_Underline != null) {
                fntBold_Underline.Dispose();
                fntBold_Underline = null;
            }
            if(fntSubText != null) {
                fntSubText.Dispose();
                fntSubText = null;
            }
            if(fntDriveLetter != null) {
                fntDriveLetter.Dispose();
                fntDriveLetter = null;
            }
            foreach(QTabItem base2 in tabPages) {
                if(base2 != null) {
                    base2.OnClose();
                }
            }
            base.Dispose(disposing);
        }

        private void DrawBackground(Graphics g, bool bSelected, bool fHot, Rectangle rctItem, Edges edges, bool fVisualStyle, int index) {
            // add by indiff for dark mode
            Brush rectBrush = null;
            if (QTUtility.InNightMode)
            {
                // QTLogger.log("QTabControl DrawBackground InNightMode ");
                rectBrush = new SolidBrush(Config.Skin.TabShadActiveColor);
                // Color light = Color.FromArgb(242, 242, 242);
                Color light = Color.FromArgb(122, 122, 122);
                // Color defaultColor = ShellColors.Default;
                // Color defaultColor2 = Color.FromArgb(240, 240, 240);
                // defaultColor = Color.Black;
                /*Graphic.FillRectangleRTL(g, 
                    QTUtility.InNightMode ?
                        (bSelected ? ShellColors.Light : ShellColors.Default) : 
                        (QTUtility.LaterThan10Beta17666 ? 
                            (bSelected ? ShellColors.Light : ShellColors.Default) :
                            Color.Black), 
                    rctItem, 
                    OSDetector.IsRTL);*/
                Graphic.FillRectangleRTL(g,
                    (bSelected ? light : Color.Black),
                    rctItem,
                    true);
            }
            else
            {
                QTLogger.log("QTabControl DrawBackground NormanMode ");
                rectBrush = SystemBrushes.Control;
                g.FillRectangle(rectBrush, rctItem);
            }

            if(!fVisualStyle) {
               // g.FillRectangle(rectBrush, rctItem);
               /* 
                  g.FillRectangle(rectBrush, rctItem);
                  g.DrawRectangle(Pens.Black, new Rectangle(0, 0, rctItem.Width - 1, rctItem.Height - 1));
                  */
                int num = bSelected ? 0 : 1;
                if(tabImages == null) { // ���ͼƬΪ��
                    // g.FillRectangle(rectBrush, rctItem);
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X + 2, rctItem.Y), 
                        new Point(((rctItem.X + rctItem.Width) - 2) - num, rctItem.Y));
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X + 2, rctItem.Y), 
                        new Point(rctItem.X, rctItem.Y + 2));
                    g.DrawLine(SystemPens.ControlLightLight, 
                        new Point(rctItem.X, rctItem.Y + 2), 
                        new Point(rctItem.X, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDarkDark, 
                        new Point((rctItem.X + rctItem.Width) - num, rctItem.Y + 2), 
                        new Point((rctItem.X + rctItem.Width) - num, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDark, 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, rctItem.Y + 1), 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, (rctItem.Y + rctItem.Height) - 1));
                    g.DrawLine(SystemPens.ControlDarkDark, 
                        new Point(((rctItem.X + rctItem.Width) - num) - 1, rctItem.Y + 1), 
                        new Point((rctItem.X + rctItem.Width) - num, rctItem.Y + 2));
                    if(bSelected) {
                        // QTLogger.log("DrawBackground g.DrawLine bSelected");
                        Pen pen = new Pen(colorSet[2], 2f);
                        g.DrawLine(pen, 
                            new Point(rctItem.X, (rctItem.Y + rctItem.Height) - 1), 
                            new Point((rctItem.X + rctItem.Width) + 1,  (rctItem.Y + rctItem.Height) - 1));
                        pen.Dispose();
                    }
                }  else {  // ���ͼƬ��Ϊ��
                    Bitmap bitmap;
                    if(bSelected) {
                        // QTLogger.log("tabImages[0] ");
                        bitmap = tabImages[0];
                    }
                    else if(fHot || (iPseudoHotIndex == index)) {
                        // QTLogger.log("tabImages[2] ");
                        bitmap = tabImages[2];
                    }
                    else {
                        // QTLogger.log("tabImages[1] ");
                        bitmap = tabImages[1];
                    }
                    if(bitmap != null) { // ���ͼƬ��Ϊ��
                                int left = sizingMargin.Left;
                                int top = sizingMargin.Top;
                                int right = sizingMargin.Right;
                                int bottom = sizingMargin.Bottom;
                                int vertical = sizingMargin.Vertical;
                                int horizontal = sizingMargin.Horizontal;
                                Rectangle[] rectangleArray = new Rectangle[]
                                {
                                    new Rectangle(rctItem.X, rctItem.Y, left, top), 
                                    new Rectangle(rctItem.X + left, rctItem.Y, rctItem.Width - horizontal, top), 
                                    new Rectangle(rctItem.Right - right, rctItem.Y, right, top), 
                                    new Rectangle(rctItem.X, rctItem.Y + top, left, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.X + left, rctItem.Y + top, rctItem.Width - horizontal, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.Right - right, rctItem.Y + top, right, rctItem.Height - vertical), 
                                    new Rectangle(rctItem.X, rctItem.Bottom - bottom, left, bottom), 
                                    new Rectangle(rctItem.X + left, rctItem.Bottom - bottom, rctItem.Width - horizontal, bottom), 
                                    new Rectangle(rctItem.Right - right, rctItem.Bottom - bottom, right, bottom)
                                };
                                Rectangle[] rectangleArray2 = new Rectangle[9];
                                // QTLogger.log("ͼƬ�������� 9 ");
                                int width = bitmap.Width;
                                int height = bitmap.Height;

                                // QTLogger.log("ͼƬ����  " + width + " ͼƬ�߶�  " + height);
                                rectangleArray2[0] = new Rectangle(0, 0, left, top);
                                rectangleArray2[1] = new Rectangle(left, 0, width - horizontal, top);
                                rectangleArray2[2] = new Rectangle(width - right, 0, right, top);
                                rectangleArray2[3] = new Rectangle(0, top, left, height - vertical);
                                rectangleArray2[4] = new Rectangle(left, top, width - horizontal, height - vertical);
                                rectangleArray2[5] = new Rectangle(width - right, top, right, height - vertical);
                                rectangleArray2[6] = new Rectangle(0, height - bottom, left, bottom);
                                rectangleArray2[7] = new Rectangle(left, height - bottom, width - horizontal, bottom);
                                rectangleArray2[8] = new Rectangle(width - right, height - bottom, right, bottom);
                                for (int i = 0; i < 9; i++)
                                {
                                    g.DrawImage(bitmap, rectangleArray[i], rectangleArray2[i], GraphicsUnit.Pixel);
                                }
                                // QTLogger.log("drawbackground by image end");
                                // bitmap.Dispose(); // ���ﵼ��ͼƬ����
                    }
                }
            } // !fVisualStyle
            else {
                VisualStyleRenderer renderer;
                if(!bSelected) {
                    // ��ѡ������ renderer
                    if(!fHot && (iPseudoHotIndex != index)) {
                        Edges edges4 = edges;
                        if(edges4 == Edges.Left) {
                            renderer = vsr_LNormal;
                        }
                        else if(edges4 == Edges.Right) {
                            renderer = vsr_RNormal;
                        }
                        else {
                            renderer = vsr_MNormal;
                        }
                    }
                    else {
                        Edges edges3 = edges;
                        if(edges3 == Edges.Left) {
                            renderer = vsr_LHot;
                        }
                        else if(edges3 == Edges.Right) {
                            renderer = vsr_RHot;
                        }
                        else {
                            renderer = vsr_MHot;
                        }
                    }
                } //  !bSelected
                else {
                    Edges edges2 = edges;
                    if(edges2 == Edges.Left) {
                        renderer = vsr_LPressed;
                    }
                    else if(edges2 == Edges.Right) {
                        renderer = vsr_RPressed;
                    }
                    else {
                        renderer = vsr_MPressed;
                    }
                    // QTLogger.log("DrawBackground renderer.DrawBackground1");
                    if (!QTUtility.InNightMode)
                    {
                        renderer.DrawBackground(g, rctItem);
                    }
                    return;
                }
                // QTLogger.log("DrawBackground renderer.DrawBackground2");
                if (!QTUtility.InNightMode)
                {
                    renderer.DrawBackground(g, rctItem);
                }
            }
        }

        private static void DrawDriveLetter(Graphics g, string str, Font fnt, Rectangle rctFldImg, bool fSelected) {
            Rectangle layoutRectangle = new Rectangle(rctFldImg.X + 7, rctFldImg.Y + 6, 0x10, 0x10);
            using(SolidBrush brush = 
                        new SolidBrush( 
                                /*QTUtility2.MakeModColor(fSelected ? 
                                    Config.Skin.TabShadActiveColor : 
                                    Config.Skin.TabShadInactiveColor
                                    )*/
                                QTUtility2.MakeModColor(selectedColor(fSelected))
                        )
                   ) {
                Rectangle rectangle2 = layoutRectangle;
                rectangle2.Offset(1, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(-2, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(1, -1);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, 2);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(1, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, -2);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(-2, 0);
                g.DrawString(str, fnt, brush, rectangle2);
                rectangle2.Offset(0, 2);
                g.DrawString(str, fnt, brush, rectangle2);
                // dark mode brshActive.Color
                // brush.Color = fSelected ? Config.Skin.TabTextActiveColor : Config.Skin.TabTextInactiveColor;
                brush.Color = selectedColor(fSelected);
                g.DrawString(str, fnt, brush, layoutRectangle);
            }
        }

        // 43 ����bug
        /*
         * 
            Message ---
            δ�������������õ������ʵ����
            HelpLink ---

            Source ---
            QTTabBar

            StackTrace ---
               �� QTTabBarLib.QTabControl.DrawTab(Graphics g, Rectangle itemRct, Int32 index, QTabItem tabHot, Boolean fVisualStyle)
               �� QTTabBarLib.QTabControl.OnPaint_MultipleRow(PaintEventArgs e)
            TargetSite ---
            Void DrawTab(System.Drawing.Graphics, System.Drawing.Rectangle, Int32, QTTabBarLib.QTabItem, Boolean)
         
             Message ---
            ����������Χ������Ϊ�Ǹ�ֵ��С�ڼ��ϴ�С��
                       ������: index
            HelpLink ---

            Source ---
            mscorlib
            StackTrace ---
                       �� System.Collections.ArrayList.get_Item(Int32 index)
                       �� System.Windows.Forms.ImageList.ImageCollection.IndexOfKey(String key)
                       �� System.Windows.Forms.ImageList.ImageCollection.ContainsKey(String key)
                       �� QTTabBarLib.QTabControl.DrawTab(Graphics g, Rectangle itemRct, Int32 index, QTabItem tabHot, Boolean fVisualStyle)
*/
        // ��ָ���߿��ڻ��Ƶ�ǰ�Ӿ���ʽԪ�صı���ͼ��
        private void DrawTab(Graphics g, Rectangle itemRct, int index, QTabItem tabHot, bool fVisualStyle) {
            try
            {
                Rectangle textRect; // �����ı�����
                Rectangle rctItem = textRect = itemRct; // ��ǩ����
                // ����������Χ������Ϊ�Ǹ�ֵ��С�ڼ��ϴ�С��
                QTabItem baseTabItem = tabPages[index]; // ��ǰ�ı�ǩ��
                bool bSelected = iSelectedIndex == index; // �Ƿ�ѡ��
                bool fHot = baseTabItem == tabHot; // �Ƿ�δ�ȵ��ǩ
                textRect.X += 2; // x��ƫ�� 2 ����
                if(bSelected) {
                    rctItem.Width += 4; // ���ѡ������ȼӿ� 4 ����
                }
                else {
                    rctItem.X += 2;  // ��ѡ�� ��ǩ����x��ƫ�� 2 ����
                    rctItem.Y += 2;  // ��ѡ�� ��ǩ����y��ƫ�� 2 ����
                    rctItem.Height -= 2;  // ��ѡ�� ��ǩ����߶Ȼ��� 2 ����
                    // textRect.Y += 2; // ��ѡ�� �ı�����y��ƫ�� 2 ����
                }
                DrawBackground(g, bSelected, fHot, rctItem, baseTabItem.Edge, fVisualStyle, index);
                int tabPosYHalfTabHeight = (rctItem.Height - 0x10) / 2; // ��ǩY����� 10 ���ص�һ��
                // QTLogger.log("draw folder image " + fDrawFolderImg +  " baseTabItem.ImageKey " + baseTabItem.ImageKey );
                // �ж��Ƿ�ʹ��ͼƬ
                if(fDrawFolderImg && IconManager.ImageGlobalContainsKey(baseTabItem.ImageKey)) {
                    // ͼƬ���� 0x10 -> 16
                    Rectangle imgRect = new Rectangle(
                        rctItem.X + (bSelected ? 7 : 5), 
                        rctItem.Y + tabPosYHalfTabHeight, 
                        0x10, 
                        0x10); // 16 �߶�  * 16 ����
                    textRect.X += 0x18;
                    textRect.Width -= 0x18; // 24
                    if((fNowMouseIsOnIcon && (iTabMouseOnButtonsIndex == index)) || (iTabIndexOfSubDirShown == index)) {
                        if(fSubDirShown && (iTabIndexOfSubDirShown == index)) {
                            imgRect.X++;
                            imgRect.Y++;
                        }
                        if(bmpFolIconBG == null) {
                            bmpFolIconBG = Resources_Image.imgFolIconBG;
                        }
                        g.DrawImage(bmpFolIconBG, new Rectangle(imgRect.X - 2, imgRect.Y - 2, imgRect.Width + 4, imgRect.Height + 4));
                    }
					// ���Ʊ���ͼƬ
                    g.DrawImage(IconManager.GetImageFromGlobal(baseTabItem.ImageKey), imgRect);
					// �ж��Ƿ��������ͼ��
                    if(Config.Tabs.ShowDriveLetters) {
                        string pathInitial = baseTabItem.PathInitial;
                        if(pathInitial.Length > 0) {
                            DrawDriveLetter(g, pathInitial, fntDriveLetter, imgRect, bSelected);
                        }
                    }
                }
                else {
                    textRect.X += 4;
                    textRect.Width -= 4;
                }
                if(baseTabItem.TabLocked) { // ����������������ͼƬ
                    Rectangle lockRect = new Rectangle(
                        rctItem.X + (bSelected ? 6 : 4),  // ѡ��ƫ�� 6 ���ء���ѡ��ƫ�� 4 ����
                        rctItem.Y + tabPosYHalfTabHeight,  // Y��Ϊ��ǩһ��߶�
                        9, 
                        11); // 9 * 11
                    if(fDrawFolderImg) { // �����ļ���ͼƬ
                        lockRect.X += 9;   //  X ƫ�� 9 ����
                        lockRect.Y += 5;   //  Y ƫ�� 9 ����
                    }
                    else {
                        lockRect.Y += 2; //  X ƫ�� 2 ����
                        textRect.X += 10;//  Y ƫ�� 10 ����
                        textRect.Width -= 10;  // ���ȼ�10����
                    }
                    if(bmpLocked == null) {
                        bmpLocked = Resources_Image.imgLocked;
                    }
                    g.DrawImage(bmpLocked, lockRect);
                }
                bool isComment = baseTabItem.Comment.Length > 0;
                if((fDrawCloseButton && !fCloseBtnOnHover) && !fNowShowCloseBtnAlt) {
                    textRect.Width -= 15;
                }
                float textWidth = isComment ? 
                    ((baseTabItem.TitleTextSize.Width + baseTabItem.SubTitleTextSize.Width) + 4f) : 
                    (baseTabItem.TitleTextSize.Width + 2f);

                // ��ǩY��ƫ��Ϊ �ı�����߶�- �ı��߶�  һ��
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0.993���� 2022/10/1 16:57:52  Config.Skin.TabHeight 35
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0���� 2022/10/1 16:57:52  textRect.Height 35
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0���� 2022/10/1 16:57:52  baseTabItem.TitleTextSize.Height 20
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0���� 2022/10/1 16:57:52  textRect.X 26
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0���� 2022/10/1 16:57:52  textRect.Y 0
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0���� 2022/10/1 16:57:52  textPosX 53.5
                // [log] C:QTabControl M:DrawTab P:12464 T:1 cost:0.994���� 2022/10/1 16:57:52  textPosY 2.5
                // QTLogger.log(" Config.Skin.TabHeight " + Config.Skin.TabHeight);
                // QTLogger.log(" textRect.Height " + textRect.Height);
                // QTLogger.log(" baseTabItem.TitleTextSize.Height " + baseTabItem.TitleTextSize.Height);
                // QTLogger.log(" textRect.X " + textRect.X);
                // QTLogger.log(" textRect.Y " + textRect.Y);
                // QTLogger.log(" textPosX " + ((tabTextAlignment == StringAlignment.Center)
                //     ? Math.Max(((textRect.Width - textWidth) / 2f), 0f) :
                //     0f));
                // QTLogger.log(" textPosY " + Math.Max(((textRect.Height - baseTabItem.TitleTextSize.Height) / 2f) - 5, 0f));
                // float textPosY = Math.Max(((textRect.Height - baseTabItem.TitleTextSize.Height) / 2f) - 5 , 0f);
                // float textPosY = 0;
                // 垂直居中：Y 偏移到标题中线，绘制高度用标题高度（勿用整栏高度再 Center，会偏下）
                float textPosY = ComputeTabTitleTopOffset(textRect.Height, baseTabItem.TitleTextSize.Height);
                // 计算标签文本水平对齐的偏移值
                float textPosX = (tabTextAlignment == StringAlignment.Center)
                              ? Math.Max(((textRect.Width - textWidth) / 2f), 0f) :
                              0f; 
                RectangleF textRct = new RectangleF(
                                            textRect.X + textPosX, 
                                            textRect.Y + textPosY,
                                            Math.Min((baseTabItem.TitleTextSize.Width + 2f), (textRect.Width - textPosX)), 
                                            Math.Max(baseTabItem.TitleTextSize.Height, 1f));
                // ������Ӱ���� dark mode
                if(fDrawShadow)
                {
                    
                    // Color clrTxtColor = bSelected ? colorSet[0] : colorSet[1];
                    // Color clrShdwColor = bSelected ? colorSet[3] : colorSet[4];
                    // QTLogger.log("DrawTextWithShadow1 " + clrTxtColor + " " + clrShdwColor + " InNightMode " + QTUtility.InNightMode);
                    DrawTextWithShadow(g, 
                        baseTabItem.Text, 
                        bSelected ? colorSet[0] : colorSet[1], 
                        bSelected ? colorSet[3] : colorSet[4], 
                        (bSelected && fActiveTxtBold) ? 
                            (baseTabItem.Underline ? fntBold_Underline : fntBold) : 
                            (baseTabItem.Underline ? fnt_Underline : Font), 
                        textRct, 
                        sfTypoGraphic);
                }
                else {
                    // QTLogger.log("g.DrawString1 color " + brshInactv.Color + " InNightMode " + QTUtility.InNightMode);
                    if (QTUtility.InNightMode)
                    {
                        brshActive = new SolidBrush(Config.Skin.TabTextActiveColor);
                        brshInactv = new SolidBrush(Config.Skin.TabTextInactiveColor);
                    }
                    else
                    {
                        brshActive = new SolidBrush(Config.Skin.TabTextActiveColor);
                        brshInactv = new SolidBrush(Config.Skin.TabTextInactiveColor);
                    }
                    g.DrawString(baseTabItem.Text, 
                            (bSelected && fActiveTxtBold) ? 
                            (baseTabItem.Underline ? fntBold_Underline : fntBold) : 
                            (baseTabItem.Underline ? fnt_Underline : Font),
                            bSelected ? brshActive : brshInactv, textRct, sfTypoGraphic);
                }
                if(iFocusedTabIndex == index) {
                    Rectangle rectangle = rctItem;
                    rectangle.Inflate(-2, -1);
                    rectangle.Y++;
                    rectangle.Width--;
                    ControlPaint.DrawFocusRectangle(g, rectangle);
                }
				// �Ƿ����ñ�ע����
                if(isComment && (textRect.Width > baseTabItem.TitleTextSize.Width)) {
                    float posY = ComputeTabCommentTop(textRect.Y, textRect.Height, baseTabItem.SubTitleTextSize.Height);
					RectangleF drawStrRectF = new RectangleF(
                        textRct.Right, 
                        posY, 
                        Math.Min(
                            (baseTabItem.SubTitleTextSize.Width + 2f),
                            (textRect.Width - ((baseTabItem.TitleTextSize.Width + textPosX) + 4f))
                        ), 
                        Math.Max(baseTabItem.SubTitleTextSize.Height, 1f));
                    if(fDrawShadow) {
                        // QTLogger.log("DrawTextWithShadow2 " + clrTxtColor + " " + clrShdwColor + " InNightMode " + QTUtility.InNightMode);
                        DrawTextWithShadow(g, 
                            (fAutoSubText ? "@" : ":") + baseTabItem.Comment, 
                            bSelected ? colorSet[0] : colorSet[1], 
                            bSelected ? colorSet[3] : colorSet[4], 
                            fntSubText, 
                            drawStrRectF, 
                            sfTypoGraphic);
                    }
                    else {
                        // QTLogger.log("g.DrawString2 color " + brshInactv.Color + " InNightMode " + QTUtility.InNightMode);
                        g.DrawString((fAutoSubText ? "@" : ":") + baseTabItem.Comment, 
                            fntSubText, 
                            brshInactv, 
                            drawStrRectF, 
                            sfTypoGraphic);
                    }
                }
                if(fDrawCloseButton && (!fCloseBtnOnHover || fHot)) {
                    Rectangle closeButtonRectangle = GetCloseButtonRectangle(baseTabItem.TabBounds, bSelected);
                    if(fNowMouseIsOnCloseBtn && (iTabMouseOnButtonsIndex == index)) {
                        if(MouseButtons == MouseButtons.Left) {
                            if(bmpCloseBtn_Pressed == null) {
                                bmpCloseBtn_Pressed = Resources_Image.imgCloseButton_Press;
                            }
                            g.DrawImage(bmpCloseBtn_Pressed, closeButtonRectangle);
                        }
                        else {
                            if(bmpCloseBtn_Hot == null) {
                                bmpCloseBtn_Hot = Resources_Image.imgCloseButton_Hot;
                            }
                            g.DrawImage(bmpCloseBtn_Hot, closeButtonRectangle);
                        }
                    }
                    else if(fNowShowCloseBtnAlt || fCloseBtnOnHover) {
                        if(bmpCloseBtn_ColdAlt == null) {
                            bmpCloseBtn_ColdAlt = Resources_Image.imgCloseButton_ColdAlt;
                        }
                        g.DrawImage(bmpCloseBtn_ColdAlt, closeButtonRectangle);
                    }
                    else {
                        if(bmpCloseBtn_Cold == null) {
                            bmpCloseBtn_Cold = Resources_Image.imgCloseButton_Cold;
                        }
                        g.DrawImage(bmpCloseBtn_Cold, closeButtonRectangle);
                    }
                }
            }
            catch (Exception e)
            {
                QTLogger.MakeErrorLog(e, "DrawTab");
            }
        }

        private static void DrawTextWithShadow(Graphics g, string txt, Color clrTxt, Color clrShdw, Font fnt, RectangleF rct, StringFormat sf) {
            RectangleF layoutRectangle = rct;
            RectangleF ef2 = rct;
            RectangleF ef3 = rct;
            layoutRectangle.Offset(1f, 1f);
            ef2.Offset(2f, 0f);
            ef3.Offset(1f, 2f);
            Color color = Color.FromArgb(0xc0, clrShdw);
            Color color2 = Color.FromArgb(0x80, clrShdw);
            using(SolidBrush brush = new SolidBrush(Color.FromArgb(0x40, clrShdw))) {
                g.DrawString(txt, fnt, brush, ef3, sf);
                brush.Color = color2;
                g.DrawString(txt, fnt, brush, ef2, sf);
                brush.Color = color;
                g.DrawString(txt, fnt, brush, layoutRectangle, sf);
                brush.Color = clrTxt;
                g.DrawString(txt, fnt, brush, rct, sf);
            }
        }

        public bool FocusNextTab(bool fBack, bool fEntered, bool fEnd) {
            if(tabPages.Count <= 0) {
                return false;
            }
            if(fEntered) {
                iFocusedTabIndex = fBack ? (tabPages.Count - 1) : 0;
                SetPseudoHotIndex(iFocusedTabIndex);
                return true;
            }
            if((fBack && (iFocusedTabIndex == 0)) || (!fBack && (iFocusedTabIndex == (tabPages.Count - 1)))) {
                iFocusedTabIndex = -1;
                return false;
            }
            if(fEnd) {
                iFocusedTabIndex = fBack ? 0 : (tabPages.Count - 1);
            }
            else {
                iFocusedTabIndex += fBack ? -1 : 1;
                if(iFocusedTabIndex < 0) {
                    iFocusedTabIndex = tabPages.Count - 1;
                }
            }
            SetPseudoHotIndex(iFocusedTabIndex);
            return true;
        }

        private Rectangle GetCloseButtonRectangle(Rectangle rctTab, bool fSelected) {
            int num = ((itemSize.Height - 15) / 2) + 1;
            if(!fSelected) {
                num += 2;
            }
            if((iMultipleType == 0) && fNeedToDrawUpDown) {
                rctTab.X += iScrollWidth;
            }
            return new Rectangle(rctTab.Right - 0x11, rctTab.Top + num, 15, 15);
        }

        public int GetFocusedTabIndex() {
            return iFocusedTabIndex;
        }

        private Rectangle GetFolderIconRectangle(Rectangle rctTab, bool fSelected) {
            int num = (rctTab.Height - 0x10) / 2;
            if(!fSelected) {
                num += 2;
            }
            if((iMultipleType == 0) && fNeedToDrawUpDown) {
                rctTab.X += iScrollWidth;
            }
            return new Rectangle(rctTab.X + (fSelected ? 5 : 3), (rctTab.Y + num) - 2, 20, 20);
        }

        private Rectangle GetItemRectangle(int index) {
            Rectangle tabBounds = tabPages[index].TabBounds;
            if(fNeedToDrawUpDown) {
                tabBounds.X += iScrollWidth;
            }
            return tabBounds;
        }

        private Rectangle GetItemRectWithInflation(int index) {
            Rectangle tabBounds = tabPages[index].TabBounds;
            if(index == iSelectedIndex) {
                tabBounds.Inflate(4, 0);
            }
            if(fNeedToDrawUpDown) {
                tabBounds.X += iScrollWidth;
            }
            return tabBounds;
        }

        /**
         * ��ȡ�������ı�ǩ
         * bug ��ֻ��һ����ǩ��ʱ�򣬵����ǩ�հ״�ʶ��Ϊ��ǩ
         */
        public QTabItem GetTabMouseOn() {
            if (this == null || this.IsDisposed)
            {
                 if (tabPages.Count == 1)
                 {
                     Point pp = PointToClient(MousePosition);
                     if (((upDown != null) && upDown.Visible) && upDown.Bounds.Contains(pp))
                     {
                         return null;
                     }
                     QTLogger.log(" return tabPage[0] 1");
                     return tabPages[0];
                 }
                 return null;
            }
            Point pt = PointToClient(MousePosition);
            if (((upDown != null) && upDown.Visible) && upDown.Bounds.Contains(pt))
            {
                return null;
            }

            // �����ǩֻ��һ���Ļ�
            if (tabPages.Count == 1) {
                 if (tabPages[0].TabBounds.Contains(pt))
                 {
                     QTLogger.log("contains pt return tabPage[0] 2");
                     return tabPages[0];
                 }
                 return null;
            }

            QTabItem base2 = null;
            QTabItem base3 = null;
            for(int i = 0; i < tabPages.Count; i++) {
                if(GetItemRectWithInflation(i).Contains(pt)) {
                    if(base2 == null) {
                        base2 = tabPages[i];
                        if(iMultipleType == 0) {
                            return base2;
                        }
                    }
                    else {
                        base3 = tabPages[i];
                        break;
                    }
                }
            }
            if((base3 != null) && (base2.Row <= base3.Row)) {
                return base3;
            }
            return base2;
        }

        public QTabItem GetTabMouseOn(out int index) {
            Point pt = PointToClient(MousePosition);
            QTabItem base2 = null;
            QTabItem base3 = null;
            int num = -1;
            int num2 = -1;
            for(int i = 0; i < tabPages.Count; i++) {
                if(GetItemRectWithInflation(i).Contains(pt)) {
                    if(base2 == null) {
                        base2 = tabPages[i];
                        num = i;
                        if(iMultipleType == 0) {
                            index = i;
                            return base2;
                        }
                    }
                    else {
                        base3 = tabPages[i];
                        num2 = i;
                        break;
                    }
                }
            }
            if(base3 != null) {
                if(base2.Row > base3.Row) {
                    index = num;
                    return base2;
                }
                index = num2;
                return base3;
            }
            index = num;
            return base2;
        }

        public Rectangle GetTabRect(QTabItem tab) {
            Rectangle tabBounds = tab.TabBounds;
            if(fNeedToDrawUpDown) {
                tabBounds.X += iScrollWidth;
            }
            return tabBounds;
        }

        public Rectangle GetTabRect(int index, bool fInflation) {
            if((index <= -1) || (index >= tabPages.Count)) {
                throw new ArgumentOutOfRangeException("index," + index, "index is out of range.");
            }
            if(fInflation) {
                return GetItemRectWithInflation(index);
            }
            return GetItemRectangle(index);
        }

        private bool HitTestOnButtons(Rectangle rctTab, Point pntClient, bool fCloseButton, bool fSelected) {
            if(fCloseButton) {
                return GetCloseButtonRectangle(rctTab, fSelected).Contains(pntClient);
            }
            return GetFolderIconRectangle(rctTab, fSelected).Contains(pntClient);
        }

        private void InitializeRenderer() {
            vsr_LPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemLeftEdge.Pressed);
            vsr_RPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Pressed);
            vsr_MPressed = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Pressed);
            vsr_LNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemLeftEdge.Normal);
            vsr_RNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Normal);
            vsr_MNormal = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Normal);
            vsr_LHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Hot);
            vsr_RHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItemRightEdge.Hot);
            vsr_MHot = new VisualStyleRenderer(VisualStyleElement.Tab.TopTabItem.Hot);
        }

        private void InvalidateTabsOnMouseMove(QTabItem tabPage, int index, Point pnt) {
            iTabMouseOnButtonsIndex = index;
            if(tabPage != hotTab) {
                hotTab = tabPage;
                if((tabPage != null) && !tabPage.TabLocked) {
                    bool fSelected = index == iSelectedIndex;
                    if(fDrawCloseButton) {
                        fNowMouseIsOnCloseBtn = HitTestOnButtons(tabPage.TabBounds, pnt, true, fSelected);
                    }
                    if(fDrawFolderImg && fShowSubDirTip) {
                        fNowMouseIsOnIcon = HitTestOnButtons(tabPage.TabBounds, pnt, false, fSelected);
                    }
                }
                else {
                    fNowMouseIsOnCloseBtn = false;
                    fNowMouseIsOnIcon = false;
                }
                PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
            }
            else if(tabPage != null) {
                bool flag2 = index == iSelectedIndex;
                bool flag3 = false;
                if(fDrawCloseButton) {
                    bool flag4 = HitTestOnButtons(tabPage.TabBounds, pnt, true, flag2);
                    if(fNowMouseIsOnCloseBtn ^ flag4) {
                        fNowMouseIsOnCloseBtn = flag4 && !tabPage.TabLocked;
                        flag3 = true;
                    }
                }
                if(fDrawFolderImg && fShowSubDirTip) {
                    bool flag5 = HitTestOnButtons(tabPage.TabBounds, pnt, false, flag2);
                    if(fNowMouseIsOnIcon ^ flag5) {
                        fNowMouseIsOnIcon = flag5;
                        flag3 = true;
                    }
                }
                if(flag3) {
                    PInvoke.InvalidateRect(Handle, IntPtr.Zero, true);
                }
            }
        }

    }
}