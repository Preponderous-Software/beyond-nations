namespace beyondnations.desktop.ui {

    /**
    * The settings the config screen can change, read out of a GameConfig and
    * written back to it.
    *
    * The Unity screen mutated GameConfig in place from inside OnGUI, one
    * setter call per button press, and enforced its ranges by wrapping round to
    * the minimum when a value went past the maximum -- the only affordance a
    * button that cycles a number can offer. ImGui has sliders, so the values are
    * clamped into the same range instead of wrapping out of it.
    *
    * The screen still edits the very same GameConfig instance the simulation
    * holds, so a change is live: respawn, keep-inventory and lag prevention are
    * read every tick, and chunk size and location scale are read when a world is
    * built, which is why the world is not built until Start is pressed.
    *
    * Nothing here draws, so the round trip can be tested without a window.
    */
    public class ConfigValues {
        public const int MinChunkSize = 7;
        public const int MaxChunkSize = 17;
        public const int MinLocationScale = 7;
        public const int MaxLocationScale = 17;

        public int ChunkSize;
        public int LocationScale;
        public bool RespawnPawns;
        public bool KeepInventoryOnDeath;
        public bool LagPreventionEnabled;

        public static ConfigValues readFrom(GameConfig gameConfig) {
            ConfigValues values = new ConfigValues();
            values.ChunkSize = gameConfig.getChunkSize();
            values.LocationScale = gameConfig.getLocationScale();
            values.RespawnPawns = gameConfig.getRespawnPawns();
            values.KeepInventoryOnDeath = gameConfig.getKeepInventoryOnDeath();
            values.LagPreventionEnabled = gameConfig.getLagPreventionEnabled();
            return values;
        }

        public void applyTo(GameConfig gameConfig) {
            gameConfig.setChunkSize(clamp(ChunkSize, MinChunkSize, MaxChunkSize));
            gameConfig.setLocationScale(clamp(LocationScale, MinLocationScale, MaxLocationScale));
            gameConfig.setRespawnPawns(RespawnPawns);
            gameConfig.setKeepInventoryOnDeath(KeepInventoryOnDeath);
            gameConfig.setLagPreventionEnabled(LagPreventionEnabled);
        }

        public bool differsFrom(ConfigValues other) {
            return ChunkSize != other.ChunkSize
                || LocationScale != other.LocationScale
                || RespawnPawns != other.RespawnPawns
                || KeepInventoryOnDeath != other.KeepInventoryOnDeath
                || LagPreventionEnabled != other.LagPreventionEnabled;
        }

        private static int clamp(int value, int minimum, int maximum) {
            if (value < minimum) {
                return minimum;
            }
            if (value > maximum) {
                return maximum;
            }
            return value;
        }
    }
}
