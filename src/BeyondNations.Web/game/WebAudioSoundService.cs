using System.Runtime.InteropServices.JavaScript;
using beyondnations.desktop.ui.feel;
using beyondnations.web.host;

namespace beyondnations.web {

    /**
    * The page's sound (wwwroot/audio.js, the "bn-audio" module): every cue is
    * synthesised with WebAudio when it is played, so the game ships no sound
    * files. Browsers only let a page make sound after the player has touched
    * it, so the audio context is created on the first key, click or tap and
    * anything asked for before that is dropped (the ambient loop starts as
    * soon as it can).
    */
    public static partial class WebAudio {
        public const string Module = "bn-audio";

        [JSImport("play", Module)]
        public static partial void play(string cue, double volume);

        [JSImport("setAmbient", Module)]
        public static partial void setAmbient(string kind, double volume);

        /** "locked" until the first user gesture, then "running" (or "unavailable"). */
        [JSImport("state", Module)]
        public static partial string state();

        /** A phone or tablet with no mouse: the tips talk about the on-screen controls. */
        [JSImport("isTouchOnly", Module)]
        public static partial bool isTouchOnly();
    }

    public class WebAudioSoundService : ISoundService {
        public string getName() { return "webaudio"; }

        public void play(SoundCue cue, float volume) {
            WebAudio.play(cueName(cue), volume);
        }

        public void setAmbient(AmbientKind kind, float volume) {
            WebAudio.setAmbient(kind == AmbientKind.Day ? "day" : kind == AmbientKind.Night ? "night" : "none", volume);
        }

        public string getState() {
            return WebAudio.state();
        }

        /** The cue's name in audio.js. */
        public static string cueName(SoundCue cue) {
            switch (cue) {
                case SoundCue.Chop: return "chop";
                case SoundCue.Mine: return "mine";
                case SoundCue.Pickup: return "pickup";
                case SoundCue.Craft: return "craft";
                case SoundCue.BuildComplete: return "build";
                case SoundCue.Coin: return "coin";
                case SoundCue.HungerWarning: return "hunger";
                case SoundCue.CombatHit: return "hit";
                case SoundCue.UiClick: return "click";
                default: return "objective";
            }
        }
    }

    /** FeelSettings in localStorage, beside the saves. */
    public class LocalStorageFeelSettingsStore : IFeelSettingsStore {
        public const string Key = "beyond-nations/feel-settings";

        public string read() {
            return WebPage.storageGet(Key);
        }

        public void write(string text) {
            string problem = WebPage.storageSet(Key, text);
            if (!string.IsNullOrEmpty(problem)) {
                Log.warning("sound and tips settings were not stored: " + problem);
            }
        }
    }
}
