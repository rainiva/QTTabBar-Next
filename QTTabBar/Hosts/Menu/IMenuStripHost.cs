using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace QTTabBarLib {
    internal interface IMenuStripHost {
        IContainer components { get; }
        ContextMenuStripEx contextMenuSys { get; }
        ContextMenuStripEx contextMenuTab { get; }
        IntPtr Handle { get; }

        ToolStripMenuItem tsmiClose { get; set; }
        ToolStripMenuItem tsmiCloseRight { get; set; }
        ToolStripMenuItem tsmiCloseLeft { get; set; }
        ToolStripMenuItem tsmiCloseAllButThis { get; set; }
        ToolStripMenuItem tsmiAddToGroup { get; set; }
        ToolStripMenuItem tsmiCreateGroup { get; set; }
        ToolStripMenuItem tsmiLockThis { get; set; }
        ToolStripMenuItem tsmiCloneThis { get; set; }
        ToolStripMenuItem tsmiCreateWindow { get; set; }
        ToolStripMenuItem tsmiCopy { get; set; }
        ToolStripMenuItem tsmiProp { get; set; }
        ToolStripMenuItem tsmiHistory { get; set; }
        ToolStripMenuItem tsmiTabOrder { get; set; }
        ToolStripMenuItem tsmiOpenCmd { get; set; }
        ToolStripMenuItem enableApiHook { get; set; }
        ToolStripTextBox menuTextBoxTabAlias { get; set; }
        ToolStripSeparator tssep_Tab1 { get; set; }
        ToolStripSeparator tssep_Tab2 { get; set; }
        ToolStripSeparator tssep_Tab3 { get; set; }

        ToolStripMenuItem tsmiGroups { get; set; }
        ToolStripMenuItem tsmiUndoClose { get; set; }
        ToolStripMenuItem tsmiLastActiv { get; set; }
        ToolStripMenuItem tsmiExecuted { get; set; }
        ToolStripMenuItem tsmiBrowseFolder { get; set; }
        ToolStripMenuItem tsmiCloseAllButCurrent { get; set; }
        ToolStripMenuItem tsmiCloseWindow { get; set; }
        ToolStripMenuItem tsmiOption { get; set; }
        ToolStripMenuItem tsmiLockToolbar { get; set; }
        ToolStripMenuItem tsmiMergeWindows { get; set; }
        ToolStripSeparator tssep_Sys1 { get; set; }
        ToolStripSeparator tssep_Sys2 { get; set; }

        void menuTextBoxTabAlias_GotFocus(object sender, EventArgs e);
        void menuTextBoxTabAlias_LostFocus(object sender, EventArgs e);
        void menuTextBoxTabAlias_KeyPress(object sender, KeyPressEventArgs e);
        void menuitemTabOrder_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void menuitemUndoClose_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
        void tsmiBranchRoot_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e);
    }
}
