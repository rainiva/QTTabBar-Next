using System.Media;

namespace QTTabBarLib {
    internal static class SoundFeedbackService {
        internal static void AsteriskPlay() {
            if(Config.Misc.SoundBox) {
                SystemSounds.Asterisk.Play();
            }
        }

        internal static void SoundPlay() {
            if(Config.Misc.SoundBox) {
                SystemSounds.Hand.Play();
            }
        }
    }
}
