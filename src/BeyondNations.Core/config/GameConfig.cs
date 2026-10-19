using System.Text.Json;
namespace beyondnations {

    public class GameConfig {
        
        // TODO: create "ConfigOption" class & make this more flexible

        // modifiable
        private int chunkSize;
        private int locationScale;
        private bool respawnPawns;
        private bool keepInventoryOnDeath;
        private bool lagPreventionEnabled;
        private int worldSeed;
        private int ticksPerSecond;

        // non-modifiable
        private int statusExpirationTicks;
        private int playerWalkSpeed;
        private int playerRunSpeed;
        private int ticksBetweenBehaviorCalculations;
        private int ticksBetweenBehaviorExecutions;
        private int minDistanceBetweenSettlements;
        private int settlementJoinRange;
        private float settlementMetabolismMultiplier;
        private float playerMetabolism;
        private int ticksBetweenSettlementIncome;
        private int renderDistance;
        private int maxNumEntities;
        private int maxNumChunks;
        private int maxNumPawns;
        private string beyondNationsDirectoryPath;

        public GameConfig() {
            // modifiable
            chunkSize = 7;
            locationScale = 15;
            respawnPawns = false;
            keepInventoryOnDeath = true;
            lagPreventionEnabled = true;
            // 0 means pick a fresh seed each run; any other value reproduces that world.
            worldSeed = 0;
            // Simulated ticks per simulated second. Sapling growth and the
            // pawn behaviour cadence are measured against it, never against
            // the wall clock (#272). The host runs its fixed step at this rate.
            ticksPerSecond = TickCounter.DefaultTicksPerSecond;

            // non-modifiable
            statusExpirationTicks = 500;
            playerWalkSpeed = 25;
            playerRunSpeed = 50;
            // Both cadences are measured against the total tick count. Pawns
            // rethink once per simulated second (50 ticks) -- the cadence the
            // old "1000" actually produced, since it was compared against the
            // tick within the second -- and act every fifth tick.
            ticksBetweenBehaviorCalculations = 50;
            ticksBetweenBehaviorExecutions = 5;
            minDistanceBetweenSettlements = 250;
            // A nationless pawn this close to a settlement joins its nation;
            // farther out it founds its own. It was 500, twice the distance
            // between settlements, so every pawn in the opening chunks joined
            // the first nation and the world never had a second.
            settlementJoinRange = 100;
            settlementMetabolismMultiplier = 0.8f;
            // Energy the player burns per tick outside a settlement. A full
            // bar of 100 lasts one in-game day (TimeOfDay.DefaultTicksPerDay,
            // ten minutes at 50 ticks per second) without eating. It used to
            // be a random 0.001-0.010, so an unlucky player starved in three
            // minutes.
            playerMetabolism = 100f / TimeOfDay.DefaultTicksPerDay;
            // Each settlement mints one coin per stall this often (10 s). Until
            // #285 the windowed game advanced two ticks per step, so the old
            // "every 1000 ticks" ran every 10 s of play; this keeps that rate.
            ticksBetweenSettlementIncome = 500;
            renderDistance = 200;
            maxNumEntities = 100000;
            maxNumChunks = 10000;
            // With lag prevention on, neither world generation nor growth
            // (PopulationGrowth) takes the world past this many pawns.
            maxNumPawns = 100;
            beyondNationsDirectoryPath = AppDataPaths.getBaseDirectory();
        }

        // modifiable
        public int getWorldSeed() {
            return worldSeed;
        }

        public int getTicksPerSecond() {
            return ticksPerSecond;
        }

        public void setTicksPerSecond(int ticksPerSecond) {
            if (ticksPerSecond <= 0) {
                throw new System.ArgumentOutOfRangeException(nameof(ticksPerSecond), "must be positive");
            }
            this.ticksPerSecond = ticksPerSecond;
        }

        public void setWorldSeed(int worldSeed) {
            this.worldSeed = worldSeed;
        }

        public int getChunkSize() {
            return chunkSize;
        }

        public void setChunkSize(int chunkSize) {
            this.chunkSize = chunkSize;
        }

        public int getLocationScale() {
            return locationScale;
        }

        public void setLocationScale(int locationScale) {
            this.locationScale = locationScale;
        }

        public bool getRespawnPawns() {
            return respawnPawns;
        }

        public void setRespawnPawns(bool respawnPawns) {
            this.respawnPawns = respawnPawns;
        }

        public bool getKeepInventoryOnDeath() {
            return keepInventoryOnDeath;
        }

        public void setKeepInventoryOnDeath(bool keepInventoryOnDeath) {
            this.keepInventoryOnDeath = keepInventoryOnDeath;
        }

        public bool getLagPreventionEnabled() {
            return lagPreventionEnabled;
        }

        public void setLagPreventionEnabled(bool lagPreventionEnabled) {
            this.lagPreventionEnabled = lagPreventionEnabled;
        }

        // non-modifiable
        public int getStatusExpirationTicks() {
            return statusExpirationTicks;
        }

        public int getPlayerWalkSpeed() {
            return playerWalkSpeed;
        }

        public int getPlayerRunSpeed() {
            return playerRunSpeed;
        }

        public int getTicksBetweenBehaviorCalculations() {
            return ticksBetweenBehaviorCalculations;
        }

        public int getTicksBetweenBehaviorExecutions() {
            return ticksBetweenBehaviorExecutions;
        }

        public int getMinDistanceBetweenSettlements() {
            return minDistanceBetweenSettlements;
        }

        public int getSettlementJoinRange() {
            return settlementJoinRange;
        }

        public float getSettlementMetabolismMultiplier() {
            return settlementMetabolismMultiplier;
        }

        public float getPlayerMetabolism() {
            return playerMetabolism;
        }

        public void setPlayerMetabolism(float playerMetabolism) {
            if (playerMetabolism < 0f || float.IsNaN(playerMetabolism)) {
                throw new System.ArgumentOutOfRangeException(nameof(playerMetabolism), "must be zero or positive");
            }
            this.playerMetabolism = playerMetabolism;
        }

        public int getTicksBetweenSettlementIncome() {
            return ticksBetweenSettlementIncome;
        }

        public void setTicksBetweenBehaviorCalculations(int ticks) {
            if (ticks <= 0) {
                throw new System.ArgumentOutOfRangeException(nameof(ticks), "must be positive");
            }
            this.ticksBetweenBehaviorCalculations = ticks;
        }

        public int getRenderDistance() {
            return renderDistance;
        }

        public int getMaxNumEntities() {
            return maxNumEntities;
        }

        public int getMaxNumPawns() {
            return maxNumPawns;
        }

        public void setMaxNumPawns(int maxNumPawns) {
            if (maxNumPawns < 0) {
                throw new System.ArgumentOutOfRangeException(nameof(maxNumPawns), "must be zero or positive");
            }
            this.maxNumPawns = maxNumPawns;
        }

        public int getMaxNumChunks() {
            return maxNumChunks;
        }

        public string getBeyondNationsDirectoryPath() {
            return beyondNationsDirectoryPath;
        }

        // --- saving (docs/SAVES.md) ---

        /**
        * Every setting the simulation reads, so a loaded world runs under the
        * rules it was started with. The data directory is the machine's, not
        * the world's, and is left as this host has it.
        */
        internal void save(SaveWriter writer) {
            writer.write("chunkSize", chunkSize);
            writer.write("locationScale", locationScale);
            writer.write("respawnPawns", respawnPawns);
            writer.write("keepInventoryOnDeath", keepInventoryOnDeath);
            writer.write("lagPreventionEnabled", lagPreventionEnabled);
            writer.write("worldSeed", worldSeed);
            writer.write("ticksPerSecond", ticksPerSecond);
            writer.write("statusExpirationTicks", statusExpirationTicks);
            writer.write("playerWalkSpeed", playerWalkSpeed);
            writer.write("playerRunSpeed", playerRunSpeed);
            writer.write("ticksBetweenBehaviorCalculations", ticksBetweenBehaviorCalculations);
            writer.write("ticksBetweenBehaviorExecutions", ticksBetweenBehaviorExecutions);
            writer.write("minDistanceBetweenSettlements", minDistanceBetweenSettlements);
            writer.write("settlementJoinRange", settlementJoinRange);
            writer.write("settlementMetabolismMultiplier", settlementMetabolismMultiplier);
            writer.write("playerMetabolism", playerMetabolism);
            writer.write("ticksBetweenSettlementIncome", ticksBetweenSettlementIncome);
            writer.write("renderDistance", renderDistance);
            writer.write("maxNumEntities", maxNumEntities);
            writer.write("maxNumChunks", maxNumChunks);
            writer.write("maxNumPawns", maxNumPawns);
        }

        internal void load(JsonElement element) {
            chunkSize = SaveReader.getInt(element, "chunkSize");
            locationScale = SaveReader.getInt(element, "locationScale");
            respawnPawns = SaveReader.getBool(element, "respawnPawns");
            keepInventoryOnDeath = SaveReader.getBool(element, "keepInventoryOnDeath");
            lagPreventionEnabled = SaveReader.getBool(element, "lagPreventionEnabled");
            worldSeed = SaveReader.getInt(element, "worldSeed");
            ticksPerSecond = SaveReader.getInt(element, "ticksPerSecond");
            statusExpirationTicks = SaveReader.getInt(element, "statusExpirationTicks");
            playerWalkSpeed = SaveReader.getInt(element, "playerWalkSpeed");
            playerRunSpeed = SaveReader.getInt(element, "playerRunSpeed");
            ticksBetweenBehaviorCalculations = SaveReader.getInt(element, "ticksBetweenBehaviorCalculations");
            ticksBetweenBehaviorExecutions = SaveReader.getInt(element, "ticksBetweenBehaviorExecutions");
            minDistanceBetweenSettlements = SaveReader.getInt(element, "minDistanceBetweenSettlements");
            settlementJoinRange = SaveReader.getInt(element, "settlementJoinRange");
            settlementMetabolismMultiplier = SaveReader.getFloat(element, "settlementMetabolismMultiplier");
            playerMetabolism = SaveReader.getFloat(element, "playerMetabolism");
            ticksBetweenSettlementIncome = SaveReader.getInt(element, "ticksBetweenSettlementIncome");
            renderDistance = SaveReader.getInt(element, "renderDistance");
            maxNumEntities = SaveReader.getInt(element, "maxNumEntities");
            maxNumChunks = SaveReader.getInt(element, "maxNumChunks");
            maxNumPawns = SaveReader.getInt(element, "maxNumPawns");
            if (chunkSize <= 0 || locationScale <= 0 || ticksPerSecond <= 0
                    || ticksBetweenBehaviorCalculations <= 0 || ticksBetweenBehaviorExecutions <= 0
                    || ticksBetweenSettlementIncome <= 0) {
                throw new SaveFormatException("the save's settings are out of range");
            }
        }
    }
}
