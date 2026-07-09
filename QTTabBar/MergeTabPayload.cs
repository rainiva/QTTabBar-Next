using System.Runtime.Serialization;

namespace QTTabBarLib {
    [DataContract]
    internal sealed class MergeTabPayload {
        [DataMember(Name = "path")]
        internal string Path { get; set; }

        [DataMember(Name = "text")]
        internal string Text { get; set; }

        [DataMember(Name = "locked")]
        internal bool Locked { get; set; }

        [DataMember(Name = "imageKey")]
        internal string ImageKey { get; set; }

        internal static MergeTabPayload FromTab(QTabItem tab) {
            if(tab == null) {
                return null;
            }
            return new MergeTabPayload {
                Path = tab.CurrentPath,
                Text = tab.Text,
                Locked = tab.TabLocked,
                ImageKey = tab.ImageKey,
            };
        }
    }
}
