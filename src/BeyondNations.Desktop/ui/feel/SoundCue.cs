namespace beyondnations.desktop.ui.feel {

    /**
    * The sounds the game asks for. A sound service turns each into whatever
    * it can play: the browser synthesises them with WebAudio
    * (wwwroot/audio.js); the desktop has no audio backend yet and plays
    * nothing (NullSoundService).
    *
    * Sounds are presentation only. Nothing in the simulation hears them, and
    * nothing here reads the simulation's random source, so a world plays out
    * the same with the sound on, off or missing.
    */
    public enum SoundCue {
        Chop,
        Mine,
        Pickup,
        Craft,
        BuildComplete,
        Coin,
        HungerWarning,
        CombatHit,
        UiClick,
        ObjectiveComplete
    }

    /** The background loop: birdsong and wind by day, crickets by night, or nothing (menus, muted). */
    public enum AmbientKind {
        None,
        Day,
        Night
    }

    /**
    * Plays cues and one ambient loop. Volumes are final gains from 0 to 1;
    * the caller has already applied the master volume and mute.
    */
    public interface ISoundService {
        /** A short name for the log: "webaudio", "none". */
        string getName();

        void play(SoundCue cue, float volume);

        /** Switches the loop, or changes its volume; the same kind twice is cheap. */
        void setAmbient(AmbientKind kind, float volume);

        /** A one-word state for the page's status: "none", "locked" (waiting for a tap or key), "running". */
        string getState();
    }

    /**
    * No audio. The desktop build uses it: Strata has no audio module and the
    * project takes on no audio dependency (an OpenAL backend would need a
    * native library on every player's machine), so the desktop is silent and
    * the sound settings only matter in the browser.
    */
    public class NullSoundService : ISoundService {
        public string getName() { return "none"; }
        public void play(SoundCue cue, float volume) { }
        public void setAmbient(AmbientKind kind, float volume) { }
        public string getState() { return "none"; }
    }
}
