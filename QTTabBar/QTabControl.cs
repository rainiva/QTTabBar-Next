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
        private Bitmap bmpCloseBtn_Cold;
        private Bitmap bmpCloseBtn_ColdAlt;
        private Bitmap bmpCloseBtn_Hot;
        private Bitmap bmpCloseBtn_Pressed;
        private Bitmap bmpFolIconBG;
        private Bitmap bmpLocked;
        private SolidBrush brshActive;
        private SolidBrush brshInactv;
        private Color[] colorSet;
        private IContainer components;
        private QTabItem draggingTab;
        private bool fActiveTxtBold;
        private bool fAutoSubText;
        private bool fCloseBtnOnHover;
        private bool fDrawCloseButton;
        private bool fDrawFolderImg;
        private bool fDrawShadow;
        private bool fForceClassic;
        private bool fLimitSize;
        private bool fNeedToDrawUpDown;
        // �Ƿ�����������ť
        private bool fNeedPlusButton;
        private bool fNowMouseIsOnCloseBtn;
        private bool fNowMouseIsOnIcon;
        private bool fNowShowCloseBtnAlt;
        private bool fNowTabContextMenuStripShowing;
        private Font fnt_Underline;
        private Font fntBold;
        private Font fntBold_Underline;
        private Font fntDriveLetter;
        private Font fntSubText;
        private bool fOncePainted;
        internal const float FONTSIZE_DIFF = 0.75f;
        private bool fRedrawSuspended;
        private bool fShowSubDirTip;
        private bool fSubDirShown;
        private bool fSuppressDoubleClick;
        private bool fSuppressMouseUp;
        private QTabItem hotTab;
        private int iCurrentRow;
        private int iFocusedTabIndex = -1;
        private int iMultipleType;
        private int iPointedChanged_LastRaisedIndex = -2;
        private int iPseudoHotIndex = -1;
        private int iScrollClickedCount;
        private int iScrollWidth;
        private int iSelectedIndex;
        private int iTabIndexOfSubDirShown = -1;
        private int iTabMouseOnButtonsIndex = -1;
        private Size itemSize = new Size(100, 0x18);
        private int iToolTipIndex = -1;
        private int maxAllowedTabWidth = 10;
        private int minAllowedTabWidth = 10;
        private QTabItem selectedTabPage;
        private StringFormat sfTypoGraphic;
        private TabSizeMode sizeMode;
        private Padding sizingMargin;
        private Bitmap[] tabImages;
        private QTabCollection tabPages;
        private StringAlignment tabTextAlignment;
        private Timer timerSuppressDoubleClick;
        private ToolTip toolTip;
        private UpDown upDown;
        private const int UPDOWN_WIDTH = 0x24;

        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_LPressed;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_MPressed;
        private static VisualStyleRenderer vsr_RHot;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_RNormal;
        [ThreadStatic()]
        private static VisualStyleRenderer vsr_RPressed;

        public event QTabCancelEventHandler CloseButtonClicked; // �ر��¼�
        public event QTabCancelEventHandler Deselecting; 
        public event ItemDragEventHandler ItemDrag;
        public event QTabCancelEventHandler PointedTabChanged;
        public event QEventHandler RowCountChanged;
        public event EventHandler SelectedIndexChanged;
        public event QTabCancelEventHandler Selecting;
        public event QTabCancelEventHandler TabCountChanged;
        public event QTabCancelEventHandler TabIconMouseDown;
        // ��ɫ��ť�¼�
        public event QTabCancelEventHandler PlusButtonClicked;

        public QTabControl() {
            fNeedPlusButton = Config.Tabs.NeedPlusButton;
            /*SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint | 
                     ControlStyles.SupportsTransparentBackColor | 
                     ControlStyles.ResizeRedraw | 
                     ControlStyles.UserPaint, true);*/
            
            // ControlStyles.UserPaint//ʹ���Զ���Ļ��Ʒ�ʽ
            // |ControlStyles.ResizeRedraw//���ؼ���С�����仯ʱ�����»���
            // |ControlStyles.SupportsTransparentBackColor//��ؼ����� alpha �����С�� 255 ���� BackColor ��ģ��͸����
            // | ControlStyles.AllPaintingInWmPaint//��ؼ����Դ�����Ϣ WM_ERASEBKGND �Լ�����˸
            // | ControlStyles.OptimizedDoubleBuffer//��ؼ������Ȼ��Ƶ�������������ֱ�ӻ��Ƶ���Ļ������Լ�����˸
       
            // ��ʼ��֮ǰ���л�ȡһ�ΰ���ģʽ
            ThemeRefreshService.RefreshCacheOnly();

            SetStyle(ControlStyles.UserPaint
                     | ControlStyles.OptimizedDoubleBuffer 
                     | ControlStyles.ResizeRedraw//���ؼ���С�����仯ʱ�����»���
                     | ControlStyles.AllPaintingInWmPaint //��ؼ����Դ�����Ϣ WM_ERASEBKGND �Լ�����˸
                     | ControlStyles.SupportsTransparentBackColor//��ؼ����� alpha �����С�� 255 ���� BackColor ��ģ��͸����
                     | ControlStyles.OptimizedDoubleBuffer //��ؼ������Ȼ��Ƶ�������������ֱ�ӻ��Ƶ���Ļ������Լ�����˸
            , value : true);

            /*this.SetStyle(ControlStyles.UserPaint |
                          ControlStyles.SupportsTransparentBackColor |
                          ControlStyles.AllPaintingInWmPaint |
                          ControlStyles.OptimizedDoubleBuffer, true);*/
            
            components = new Container();
            tabPages = new QTabCollection(this);
            
            sfTypoGraphic = StringFormat.GenericTypographic;
            // MeasureTrailingSpaces ����ÿһ�н�β����β��ո� ��Ĭ������£�MeasureString �������صı߽���ζ����ų�ÿһ�н�β���Ŀո� ���ô˱���Ա��ڲⶨʱ���ո������ȥ��
            // NoWrap �ھ��������ø�ʽʱ�������Զ����й��ܡ� �����ݵ��ǵ�����Ǿ���ʱ������ָ�����ε��г���Ϊ��ʱ���������˱�ǡ�
            sfTypoGraphic.FormatFlags |= StringFormatFlags.MeasureTrailingSpaces | StringFormatFlags.NoWrap;
            // Figure-2 style: title vertically centered with the icon (not bottom-aligned).
            sfTypoGraphic.LineAlignment = StringAlignment.Center;
            sfTypoGraphic.Trimming = StringTrimming.EllipsisCharacter;
            if (OSDetector.IsRTL)
            {
                this.sfTypoGraphic.FormatFlags |= StringFormatFlags.DirectionRightToLeft;
            }

            /*if (ThemeRefreshService.IsDark)
            {
                this.colorSet = new Color[]
                {
                    ShellColors.NightModeTextColor,
                    ShellColors.NightModeDisabledColor,
                    Config.Skin.TabTextHotColor,
                    ShellColors.NightModeTextShadow,
                     Config.Skin.TabShadInactiveColor,
                    ShellColors.NightModeColor
                };
            }
            else {
                colorSet = new Color[] 
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabTextInactiveColor,
                    Config.Skin.TabTextHotColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor,
                    Config.Skin.TabShadHotColor
                };
            }*/
            // brshActive = new SolidBrush(colorSet[0]);
            // brshInactv = new SolidBrush(colorSet[1]);
            // ���䰵�� by indiff dark mode
            /*brshActive = new SolidBrush(Config.Skin.TabTextActiveColor);  // ��ǩ���ˢ
            brshInactv = new SolidBrush(Config.Skin.TabTextInactiveColor); // ��ǩ�Ǽ��ˢ
            if (ThemeRefreshService.IsDark)
            {
                BackColor = Config.Skin.TabShadActiveColor;
            }
            else
            {
                BackColor = Color.Transparent;
            }*/

            InitializeColors();
            this.BackColor = Color.Transparent;
            /*
            if (ThemeRefreshService.IsDark)
            {
                // this.BackColor = SystemColors.ControlDarkDark;;
                this.BackColor = Color.Black;
            }
            else
            {
                this.BackColor = SystemColors.Window;
            }*/
            // ��ʱ����֧��˫���¼�
            timerSuppressDoubleClick = new Timer(components);
            timerSuppressDoubleClick.Interval = SystemInformation.DoubleClickTime + 100;
            timerSuppressDoubleClick.Tick += timerSuppressDoubleClick_Tick;
            if(VisualStyleRenderer.IsSupported) {
                InitializeRenderer();
            }
        }

        public  void InitializeColors()
        {
            if (ThemeRefreshService.IsDark)
                this.colorSet = new Color[5]
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabShadInactiveColor,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    ShellColors.TextShadow,
                    ShellColors.Default,
                };
            else
                this.colorSet = new Color[5]
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabShadInactiveColor,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor
                };
            if (brshActive == null)
            {
                brshActive = new SolidBrush(this.colorSet[0]);
                brshInactv = new SolidBrush(this.colorSet[1]);
            }
            else
            {
                brshActive.Color = this.colorSet[0];
                brshInactv.Color = this.colorSet[1];
            }
        }

        public static Color selectedColor(bool fSelected)
        {
            Color[] colorSet = new Color[5];
            if (ThemeRefreshService.IsDark)
                colorSet = new Color[5]
                {
                    ShellColors.Text,
                    ShellColors.Disabled,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    ShellColors.TextShadow,
                    ShellColors.Default
                };
            else
                colorSet = new Color[5]
                {
                    Config.Skin.TabTextActiveColor,
                    Config.Skin.TabTextInactiveColor,
                    Config.Skin.TabTextActiveColor, // Config.TabHiliteColor,
                    Config.Skin.TabShadActiveColor,
                    Config.Skin.TabShadInactiveColor
                };
            if (fSelected)
            {
                return colorSet[0];
            }
            else
            {
                return colorSet[1];
            }
        }
        private bool CalculateItemRectangle() {
            int x = 0;
            int count = tabPages.Count;
            if(sizeMode == TabSizeMode.Fixed) {
                for(int i = 0; i < count; i++) {
                    tabPages[i].TabBounds = new Rectangle(x, 0, itemSize.Width, itemSize.Height);
                    tabPages[i].Edge = 0;
                    x += itemSize.Width;
                }
            }
            else {
                int width;
                if(fLimitSize) {
                    for(int j = 0; j < count; j++) {
                        width = tabPages[j].TabBounds.Width;
                        if(width > maxAllowedTabWidth) {
                            width = maxAllowedTabWidth;
                        }
                        if(width < minAllowedTabWidth) {
                            width = minAllowedTabWidth;
                        }
                        tabPages[j].TabBounds = new Rectangle(x, 0, width, itemSize.Height);
                        tabPages[j].Edge = 0;
                        x += width;
                    }
                }
                else {
                    for(int k = 0; k < count; k++) {
                        width = tabPages[k].TabBounds.Width;
                        tabPages[k].TabBounds = new Rectangle(x, 0, width, itemSize.Height);
                        tabPages[k].Edge = 0;
                        x += width;
                    }
                }
            }
            if(tabPages.Count > 1) {
                tabPages[0].Edge = Edges.Left;
                tabPages[tabPages.Count - 1].Edge = Edges.Right;
            }
            return (x > (Width - 0x24));
        }

        private void CalculateItemRectangle_MultiRows() {
            int x = 0;
            int count = tabPages.Count;
            int width = Width;
            int num4 = itemSize.Width;
            int height = itemSize.Height;
            int num6 = height - 3;
            int num7 = 0;
            int num8 = 0;
            if(sizeMode == TabSizeMode.Fixed) {  // �̶�����
                for(int i = 0; i < count; i++) {
                    if((x + num4) > width) {
                        num7++;
                        x = 0;
                    }
                    tabPages[i].TabBounds = new Rectangle(x, num6 * num7, num4, height);
                    tabPages[i].Row = num7;
                    if(x == 0) {
                        tabPages[i].Edge = Edges.Left;
                    }
                    else if((i == (count - 1)) || (((x + num4) + num4) > width)) {
                        tabPages[i].Edge = Edges.Right;
                    }
                    else {
                        tabPages[i].Edge = 0;
                    }
                    x += num4;
                    if(i == iSelectedIndex) {
                        num8 = num7;
                    }
                }
            }
            else {
                int maxTabWidth;
                if(fLimitSize) {
                    for(int j = 0; j < count; j++) {
                        maxTabWidth = tabPages[j].TabBounds.Width;
                        if(maxTabWidth > maxAllowedTabWidth) {
                            maxTabWidth = maxAllowedTabWidth;
                        }
                        if(maxTabWidth < minAllowedTabWidth) {
                            maxTabWidth = minAllowedTabWidth;
                        }
                        if((x + maxTabWidth) > width) {
                            num7++;
                            x = 0;
                        }
                        tabPages[j].TabBounds = new Rectangle(x, num6 * num7, maxTabWidth, height);
                        tabPages[j].Row = num7;
                        if(x == 0) {
                            tabPages[j].Edge = Edges.Left;
                        }
                        else if(j == (count - 1)) {
                            tabPages[j].Edge = Edges.Right;
                        }
                        else {
                            int minTabWidth = tabPages[j + 1].TabBounds.Width;
                            if(minTabWidth > maxAllowedTabWidth) {
                                minTabWidth = maxAllowedTabWidth;
                            }
                            if(minTabWidth < minAllowedTabWidth) {
                                minTabWidth = minAllowedTabWidth;
                            }
                            if(((x + maxTabWidth) + minTabWidth) > width) {
                                tabPages[j].Edge = Edges.Right;
                            }
                            else {
                                tabPages[j].Edge = 0;
                            }
                        }
                        x += maxTabWidth;
                        if(j == iSelectedIndex) {
                            num8 = num7;
                        }
                    }
                }
                else {
                    for(int k = 0; k < count; k++) {
                        maxTabWidth = tabPages[k].TabBounds.Width;
                        if((x + maxTabWidth) > width) {
                            num7++;
                            x = 0;
                        }
                        tabPages[k].TabBounds = new Rectangle(x, num6 * num7, maxTabWidth, height);
                        tabPages[k].Row = num7;
                        if(x == 0) {
                            tabPages[k].Edge = Edges.Left;
                        }
                        else if(k == (count - 1)) {
                            tabPages[k].Edge = Edges.Right;
                        }
                        else {
                            int num14 = tabPages[k + 1].TabBounds.Width;
                            if(((x + maxTabWidth) + num14) > width) {
                                tabPages[k].Edge = Edges.Right;
                            }
                            else {
                                tabPages[k].Edge = 0;
                            }
                        }
                        x += maxTabWidth;
                        if(k == iSelectedIndex) {
                            num8 = num7;
                        }
                    }
                }
            }
            if((num7 != 0) && (iMultipleType == 1)) {
                int num15 = num7 - num8;
                if(num15 > 0) {
                    for(int m = 0; m < count; m++) {
                        QTabItem base2 = tabPages[m];
                        Rectangle tabBounds = base2.TabBounds;
                        if(base2.Row > num8) {
                            base2.Row -= num8 + 1;
                            tabBounds.Y = base2.Row * num6;
                            base2.TabBounds = tabBounds;
                        }
                        else {
                            tabBounds.Y += num15 * num6;
                            base2.TabBounds = tabBounds;
                            base2.Row += num15;
                        }
                    }
                }
            }
            if(num7 != iCurrentRow) {
                iCurrentRow = num7;
                if(RowCountChanged != null) {
                    RowCountChanged(this, new QEventArgs(iCurrentRow + 1));
                }
            }
        }

        /**
         * ��ǩ�л�
         */
        private bool ChangeSelection(QTabItem tabToSelect, int index) {
            if(((Deselecting != null) && (this.iSelectedIndex > -1)) && (this.iSelectedIndex < tabPages.Count)) {
                QTabCancelEventArgs e = new QTabCancelEventArgs(tabPages[this.iSelectedIndex], this.iSelectedIndex, false, TabControlAction.Deselecting);
                Deselecting(this, e);
            }
            int curSelectedIndex = this.iSelectedIndex;
            QTabItem curSelectedTabPage = this.selectedTabPage;
            this.iSelectedIndex = index;
            this.selectedTabPage = tabToSelect;
            if(Selecting != null) {
                QTabCancelEventArgs args2 = new QTabCancelEventArgs(tabToSelect, index, false, TabControlAction.Selecting);
                Selecting(this, args2);
                if(args2.Cancel) {
                    this.iSelectedIndex = curSelectedIndex;
                    this.selectedTabPage = curSelectedTabPage;
                    return false;
                }
            }
            if(fNeedToDrawUpDown) {
                if((tabToSelect.TabBounds.X + iScrollWidth) < 0) {
                    iScrollWidth = -tabToSelect.TabBounds.X;
                    iScrollClickedCount = index;
                }
                else if((tabToSelect.TabBounds.X + iScrollWidth) > (Width - 0x24)) {
                    while((tabToSelect.TabBounds.Right + iScrollWidth) > Width) {
                        OnUpDownClicked(true, true);
                    }
                }
            }
            Refresh();
            if(SelectedIndexChanged != null) { // ѡ��ı�ǩ���������仯�� ����ö�Ӧ���¼�
                SelectedIndexChanged(this, new EventArgs());
            }
            iFocusedTabIndex = -1;
            return true;
        }

    }
}