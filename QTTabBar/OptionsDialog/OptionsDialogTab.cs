using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Font = System.Drawing.Font;
using Keys = System.Windows.Forms.Keys;
using TreeView = System.Windows.Controls.TreeView;
using UserControl = System.Windows.Controls.UserControl;

namespace QTTabBarLib {
    sealed class OptionsNavItem {
        public OptionsNavItem(OptionsDialogTab page) {
            Page = page;
        }

        public OptionsDialogTab Page { get; }
        public int Index => Page.Index;
    }

    internal interface IHotkeyEntry {
        Keys ShortcutKey { get; set; }
        string KeyActionText { get; }
    }

    internal delegate bool NewHotkeyRequestedHandler(KeyEventArgs keyEvent, Keys currentKey, out Keys newKey);
    internal interface IHotkeyContainer {
        IEnumerable<IHotkeyEntry> GetHotkeyEntries();
        event NewHotkeyRequestedHandler NewHotkeyRequested;
    }

    /// <summary>
    /// The base class for the tab pages of the OptionsDialog.
    /// Contains a few things common to more than one page.
    /// </summary>
    internal abstract class OptionsDialogTab : UserControl {
        public static readonly DependencyProperty WorkingConfigProperty =
                DependencyProperty.Register("WorkingConfig", typeof(Config), typeof(OptionsDialogTab),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public Config WorkingConfig {
            get { return (Config)GetValue(WorkingConfigProperty); }
            set { SetValue(WorkingConfigProperty, value); }
        }

        // This is the index of the resource string that will be displayed in the category list.
        public int Index { get; set; }

        // Called when the options dialog is first shown, and when the user clicks Apply (after commit)
        public abstract void InitializeConfig();

        // Called when the user clicks the Reset buttons.
        public abstract void ResetConfig();

        // Called when the user clicks Apply or OK.
        public abstract void CommitConfig();


        #region ---------- Interfaces / Helper Classes ----------

        // Interface for Binding Classes that have some editable component
        protected interface IEditableEntry {
            bool IsEditing { get; set; }
        }

        // Interface for Binding Classes that belong to a ParentedCollection list.
        protected interface IChildItem {
            IList ParentList { get; set; }
            ITreeViewItem ParentItem { get; set; }
        }

        // Interface for TreeView items, to control selectedness and expandedness.
        protected interface ITreeViewItem : IChildItem {
            bool IsSelected { get; set; }
            bool IsExpanded { get; set; }
            IList ChildrenList { get; }
        }

        // A subclass of ObservableCollection that allows the parent list and item to be accessed from its children.
        protected sealed class ParentedCollection<TChild> : ObservableCollection<TChild>
            where TChild : class, IChildItem {
            private ITreeViewItem ParentItem;
            public ParentedCollection(ITreeViewItem parentItem, IEnumerable<TChild> collection = null) {
                ParentItem = parentItem;
                if(collection != null) {
                    foreach(TChild child in collection) {
                        child.ParentItem = ParentItem;
                        child.ParentList = this;
                        Add(child);
                    }
                }
                CollectionChanged += ParentedCollection_CollectionChanged;
            }

            private void ParentedCollection_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e) {
                if(e.NewItems != null) {
                    foreach(TChild newItem in e.NewItems) {
                        newItem.ParentItem = ParentItem;
                        newItem.ParentList = this;
                    }
                }
                if(e.OldItems != null) {
                    foreach(TChild oldItem in e.OldItems) {
                        oldItem.ParentItem = null;
                        oldItem.ParentList = null;
                    }
                }
            }
        }

        // Simple overloaded ColorDialog to start with the color picker active
        protected sealed class ColorDialogEx : System.Windows.Forms.ColorDialog {
            protected override int Options {
                get {
                    return (base.Options | 2);
                }
            }
        }

        #endregion

        // Common Font Chooser button click handler.
        protected void btnFontChoose_Click(object sender, System.Windows.RoutedEventArgs e) {
            var button = (System.Windows.Controls.Button)sender;
            try
            {
                using (var dialog = new System.Windows.Forms.FontDialog())
                {
                    dialog.Font = (Font)button.Tag;
                    dialog.ShowEffects = false;
                    dialog.AllowVerticalFonts = false;
                    if (System.Windows.Forms.DialogResult.OK == dialog.ShowDialog())
                    {
                        button.Tag = dialog.Font;
                    }
                }
            }
            catch (Exception ex)
            {
                QTLogger.MakeErrorLog(ex, "btnFontChoose_Click");

            }
        }

        // Utility method to move nodes up and down in a TreeView.
        protected static void UpDownOnTreeView(TreeView tvw, bool up, bool traverseFolders) {
            ITreeViewItem sel = tvw.SelectedItem as ITreeViewItem;
            if(sel == null) return;
            IList list = sel.ParentList;
            int index = list.IndexOf(sel);
            if(index == -1) return;
            bool expanded = sel.IsExpanded;
            if(up && index == 0) {
                if(!traverseFolders || sel.ParentItem == null) return;
                IList parentList = sel.ParentItem.ParentList;
                int parentIndex = parentList.IndexOf(sel.ParentItem);
                if(parentIndex == -1) return;
                list.RemoveAt(index);
                parentList.Insert(parentIndex, sel);
            }
            else if(!up && index == list.Count - 1) {
                if(!traverseFolders || sel.ParentItem == null) return;
                IList parentList = sel.ParentItem.ParentList;
                int parentIndex = parentList.IndexOf(sel.ParentItem);
                if(parentIndex == -1) return;
                list.RemoveAt(index);
                parentList.Insert(parentIndex + 1, sel);
            }
            else {
                ITreeViewItem next = (ITreeViewItem)list[index + (up ? -1 : 1)];
                if(traverseFolders && next.ChildrenList != null && (next.IsExpanded || next.ChildrenList.Count == 0)) {
                    list.RemoveAt(index);
                    list = next.ChildrenList;
                    list.Insert(up ? list.Count : 0, sel);
                    next.IsExpanded = true;
                }
                else {
                    list.RemoveAt(index);
                    list.Insert(index + (up ? -1 : 1), sel);                    
                }
            }
            sel.IsExpanded = expanded;
            sel.IsSelected = true;
        }
    }
}
