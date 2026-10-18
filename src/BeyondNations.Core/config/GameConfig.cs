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

        // non-modifiable
        private int statusExpirationTicks;
        private int playerWalkSpeed;
        private int playerRunSpeed;
        private int ticksBetweenBehaviorCalculations;
        private int ticksBetweenBehaviorExecutions;
        private int minDistanceBetweenSettlements;
        private int settlementJoinRange;
        private float settlementMetabolismMultiplier;
        private int renderDistance;
        private int maxNumEntities;
        private int maxNumChunks;
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

            // non-modifiable
            statusExpirationTicks = 500;
            playerWalkSpeed = 25;
            playerRunSpeed = 50;
            ticksBetweenBehaviorCalculations = 1000;
            ticksBetweenBehaviorExecutions = 5;
            minDistanceBetweenSettlements = 250;
            settlementJoinRange = 500;
            settlementMetabolismMultiplier = 0.8f;
            renderDistance = 200;
            maxNumEntities = 100000;
            maxNumChunks = 10000;
            beyondNationsDirectoryPath = AppDataPaths.getBaseDirectory();
        }

        // modifiable
        public int getWorldSeed() {
            return worldSeed;
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

        public int getRenderDistance() {
            return renderDistance;
        }

        public int getMaxNumEntities() {
            return maxNumEntities;
        }

        public int getMaxNumChunks() {
            return maxNumChunks;
        }

        public string getBeyondNationsDirectoryPath() {
            return beyondNationsDirectoryPath;
        }
    }
}