using System;

using System.Collections.Generic;

using System.Collections.ObjectModel;

using System.Linq;



namespace QTTabBarLib {

    /// <summary>

    /// Process-local ephemeral state shared across Explorer instances in this process.

    /// Complements Registry/LoadedConfig: values here are not persisted as full config snapshots

    /// and may be cleared on restart. Cross-process consistency for CreateWindow* lists relies on

    /// registry polling via RegBackedList.Update(), not ReloadConfig IPC.

    /// </summary>

    internal static class StaticReg {

        internal static string CreateWindowGroup {

            get { return (string)StaticRegAccess.ReadProp("CreateWindowGroup") ?? ""; }

            set { StaticRegAccess.WriteProp("CreateWindowGroup", value); }

        }



        internal static bool SkipNextCapture {

            get { return (int)(StaticRegAccess.ReadProp("SkipNextCapture") ?? 0) != 0; }

            set { StaticRegAccess.WriteProp("SkipNextCapture", value ? 1 : 0); }

        }



        internal static UniqueList<string> ExecutedPathsList = new UniqueList<string>(16); // todo

        internal static UniqueList<string> ClosedTabHistoryList = new UniqueList<string>(16); // todo



        internal static UniqueList<string> LockedTabsToRestoreList =

            new UniqueList<string>(16, StringEqualityComparer.CaseInsensitiveComparer);



        private static RegBackedList<byte[]> _CreateWindowIDLs = new RegBackedList<byte[]>("CreateWindowIDLs");

        internal static RegBackedList<byte[]> CreateWindowIDLs {

            get {

                _CreateWindowIDLs.Update();

                return _CreateWindowIDLs;

            }

        }



        private static RegBackedList<string> _CreateWindowPaths = new RegBackedList<string>("CreateWindowPaths");

        internal static RegBackedList<string> CreateWindowPaths {

            get {

                _CreateWindowPaths.Update();

                return _CreateWindowPaths;

            }

        }

    }



    internal sealed class RegBackedList<T> : Collection<T> {

        private readonly object _assignLock = new object();

        private string key;

        private int lastUpdate = 0;

        private bool updating = false;

        public RegBackedList(IList<T> list, string key) : base(list) {

            this.key = key;

        }



        public RegBackedList(string key) {

            this.key = key;

        }



        protected override void SetItem(int index, T item) {

            base.SetItem(index, item);

            if(updating) return;

            Update();

            using(var reg = StaticRegAccess.OpenListKey(key, true)) {

                if(reg == null) return;

                reg.SetValue("" + index, item);

                reg.SetValue("", ++lastUpdate);

            }

        }



        protected override void RemoveItem(int index) {

            base.RemoveItem(index);

            if(updating) return;

            Update();

            using(var reg = StaticRegAccess.OpenListKey(key, true)) {

                if(reg == null) return;

                for(int i = index; i < Count; i++) {

                    reg.SetValue("" + i, this[i]);

                }

                reg.DeleteValue("" + Count);

                reg.SetValue("", ++lastUpdate);

            }

        }



        protected override void InsertItem(int index, T item) {

            base.InsertItem(index, item);

            if(updating) return;

            Update();

            using(var reg = StaticRegAccess.OpenListKey(key, true)) {

                if(reg == null) return;

                for(int i = index; i < Count; i++) {

                    reg.SetValue("" + i, this[i]);

                }

                reg.SetValue("", ++lastUpdate);

            }

        }



        protected override void ClearItems() {

            base.ClearItems();

            if(updating) return;

            Update();

            using(var reg = StaticRegAccess.OpenListKey(key, true)) {

                if(reg == null) return;

                foreach(string name in reg.GetValueNames()) {

                    reg.DeleteValue(name);

                }

                reg.SetValue("", ++lastUpdate);

            }

        }



        public void Update() {

            using(var reg = StaticRegAccess.OpenListKey(key, false)) {

                if(reg == null) return;

                int update = (int)reg.GetValue("", 0);

                if(update == lastUpdate) return;

                lastUpdate = update;

                updating = true;

                Clear();

                for(int i = 0;; i++) {

                    object obj = reg.GetValue("" + i);

                    if(obj == null) break;

                    Add((T)obj);

                }

                updating = false;

            }

        }



        public void AddRange(IEnumerable<T> range) {

            foreach(T t in range) {

                Add(t); // todo: make more efficient

            }

        }



        public void Assign(IEnumerable<T> collection) {

            lock(_assignLock) {

                updating = true;

                try {

                    base.ClearItems();

                    if(collection != null) {

                        foreach(T item in collection) {

                            InsertItem(Count, item);

                        }

                    }

                    using(var reg = StaticRegAccess.OpenListKey(key, true)) {

                        if(reg == null) return;

                        foreach(string name in reg.GetValueNames()) {

                            reg.DeleteValue(name);

                        }

                        for(int i = 0; i < Count; i++) {

                            reg.SetValue("" + i, this[i]);

                        }

                        reg.SetValue("", ++lastUpdate);

                    }

                }

                finally {

                    updating = false;

                }

            }

        }

    }

}


