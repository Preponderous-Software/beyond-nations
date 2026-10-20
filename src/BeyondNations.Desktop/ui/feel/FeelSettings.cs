using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace beyondnations.desktop.ui.feel {

    /**
    * Where FeelSettings live between runs: a file beside the saves on the
    * desktop, a localStorage key in the browser. Both may fail (a read-only
    * directory, a private window), so read returns null and write swallows
    * the failure: losing a volume setting is not worth an error.
    */
    public interface IFeelSettingsStore {
        string read();
        void write(string text);
    }

    /** Keeps nothing; for tests and runs with saving turned off. */
    public class MemoryFeelSettingsStore : IFeelSettingsStore {
        private string text;
        public MemoryFeelSettingsStore(string initial = null) { text = initial; }
        public string read() { return text; }
        public void write(string value) { text = value; }
    }

    /**
    * The player's own preferences for sound and for the first-five-minutes
    * tips. They belong to the player, not to a world, so they are kept apart
    * from saves: a tip finished in one world is never shown again in the
    * next, and muting stays muted.
    *
    * Stored as plain "key=value" lines, read leniently: an unknown key is
    * ignored and a bad value keeps its default, so an old or hand-edited
    * file never stops the game.
    */
    public class FeelSettings {
        public const float DefaultVolume = 0.7f;

        private float volume = DefaultVolume;
        private bool muted;
        private bool hintsEnabled = true;
        private readonly HashSet<string> hintsDone = new HashSet<string>();
        private bool changed;
        // not stored: whether this build can play sound at all (the desktop cannot yet)
        private bool soundAvailable = true;

        public bool isSoundAvailable() { return soundAvailable; }
        public void setSoundAvailable(bool value) { soundAvailable = value; }

        public float getVolume() { return volume; }
        public bool isMuted() { return muted; }
        public bool areHintsEnabled() { return hintsEnabled; }

        /** The master gain sounds are played at: the volume, or 0 when muted. */
        public float getEffectiveVolume() {
            return muted ? 0f : volume;
        }

        public void setVolume(float value) {
            float clamped = float.IsNaN(value) ? DefaultVolume : Math.Clamp(value, 0f, 1f);
            if (clamped != volume) {
                volume = clamped;
                changed = true;
            }
        }

        public void setMuted(bool value) {
            if (value != muted) {
                muted = value;
                changed = true;
            }
        }

        public void setHintsEnabled(bool value) {
            if (value != hintsEnabled) {
                hintsEnabled = value;
                changed = true;
            }
        }

        public bool isHintDone(string id) {
            return hintsDone.Contains(id);
        }

        public void markHintDone(string id) {
            if (hintsDone.Add(id)) {
                changed = true;
            }
        }

        /** Forgets every finished tip and turns tips back on (Config: "Show tips again"). */
        public void resetHints() {
            if (hintsDone.Count > 0 || !hintsEnabled) {
                hintsDone.Clear();
                hintsEnabled = true;
                changed = true;
            }
        }

        /** True once after any change, so the owner knows to write the settings out. */
        public bool consumeChanged() {
            bool was = changed;
            changed = false;
            return was;
        }

        public string serialize() {
            StringBuilder text = new StringBuilder();
            text.Append("volume=").Append(volume.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            text.Append("muted=").Append(muted ? "1" : "0").Append('\n');
            text.Append("hints=").Append(hintsEnabled ? "1" : "0").Append('\n');
            List<string> done = new List<string>(hintsDone);
            done.Sort(StringComparer.Ordinal);
            text.Append("hintsDone=").Append(string.Join(",", done)).Append('\n');
            return text.ToString();
        }

        public static FeelSettings parse(string text) {
            FeelSettings settings = new FeelSettings();
            if (string.IsNullOrEmpty(text)) {
                return settings;
            }
            foreach (string rawLine in text.Split('\n')) {
                string line = rawLine.Trim();
                int equals = line.IndexOf('=');
                if (equals <= 0) {
                    continue;
                }
                string key = line.Substring(0, equals).Trim();
                string value = line.Substring(equals + 1).Trim();
                switch (key) {
                    case "volume":
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)) {
                            settings.volume = float.IsNaN(parsed) ? DefaultVolume : Math.Clamp(parsed, 0f, 1f);
                        }
                        break;
                    case "muted":
                        settings.muted = value == "1" || value == "true";
                        break;
                    case "hints":
                        settings.hintsEnabled = !(value == "0" || value == "false");
                        break;
                    case "hintsDone":
                        foreach (string id in value.Split(',')) {
                            if (id.Trim().Length > 0) {
                                settings.hintsDone.Add(id.Trim());
                            }
                        }
                        break;
                }
            }
            return settings;
        }

        /** Reads from a store, falling back to the defaults when it holds nothing or fails. */
        public static FeelSettings load(IFeelSettingsStore store) {
            try {
                return parse(store?.read());
            } catch (Exception exception) {
                Log.warning("sound and tips settings could not be read: " + exception.Message);
                return new FeelSettings();
            }
        }

        public void save(IFeelSettingsStore store) {
            if (store == null) {
                return;
            }
            try {
                store.write(serialize());
            } catch (Exception exception) {
                Log.warning("sound and tips settings could not be written: " + exception.Message);
            }
        }
    }
}
