namespace beyondnations {

    /**
    * The part of the seasons that acts on its own each tick, called once a
    * tick from Simulation.fixedUpdate: chickens hatching in spring and
    * summer, apples spoiling (SeasonalRules.applesSpoiling), and telling
    * the player when a season turns and when winter is a day away. The rest
    * of the seasons are rules other systems ask about (SeasonalRules).
    *
    * It holds no state of its own -- every decision is read off the tick
    * count -- so there is nothing to save.
    */
    public class SeasonalWorld {
        private readonly SeasonCalendar calendar;
        private readonly RegionMap regionMap;
        private readonly Environment environment;
        private readonly EntityRepository entityRepository;
        private readonly GameConfig gameConfig;
        private readonly Player player;
        private readonly RandomSource random;

        /** With lag prevention on, hatching stops at this many chickens, the same cap world generation uses. */
        public const int MAX_CHICKENS = 50;

        public SeasonalWorld(SeasonCalendar calendar, RegionMap regionMap, Environment environment, EntityRepository entityRepository,
                GameConfig gameConfig, Player player, RandomSource random) {
            this.calendar = calendar;
            this.regionMap = regionMap;
            this.environment = environment;
            this.entityRepository = entityRepository;
            this.gameConfig = gameConfig;
            this.player = player;
            this.random = random;
        }

        private NationRepository nationRepository;
        private EventProducer eventProducer;

        /**
        * Lets the winter warning name the player's nation and write its line
        * to the chronicle (WinterStores, #353). Without it the status line
        * still says winter is coming.
        */
        public void setNations(NationRepository nationRepository, EventProducer eventProducer) {
            this.nationRepository = nationRepository;
            this.eventProducer = eventProducer;
        }

        /** The world event id the winter warning's chronicle line goes out under. */
        public const string WinterComingEventId = "winter-coming";

        public void update(int totalTicks) {
            announce(totalTicks);
            if (totalTicks % SeasonalRules.CHICKEN_HATCH_INTERVAL_TICKS == 0 && SeasonalRules.chickensHatch(calendar.getSeason())) {
                hatchChicken();
            }
            if (totalTicks % SeasonalRules.APPLE_SPOIL_INTERVAL_TICKS == 0) {
                spoilApples();
            }
        }

        /**
        * One round of spoilage: every pawn and every market stall loses a share
        * of the apples it holds beyond its kept store. The player's pack and
        * the player's own stalls are left alone. Returns how many apples rotted.
        */
        public int spoilApples() {
            int spoiled = 0;
            foreach (Entity entity in entityRepository.getEntities()) {
                if (entity.getType() == EntityType.PAWN) {
                    spoiled += spoil(entity.getInventory(), SeasonalRules.PAWN_APPLES_KEPT);
                }
                else if (entity.getType() == EntityType.SETTLEMENT) {
                    foreach (Stall stall in ((Settlement) entity).getMarket().getStalls()) {
                        if (player.getId().Equals(stall.getOwnerId())) {
                            continue;
                        }
                        spoiled += spoil(stall.getInventory(), SeasonalRules.STALL_APPLES_KEPT);
                    }
                }
            }
            return spoiled;
        }

        private static int spoil(Inventory inventory, int kept) {
            int rotting = SeasonalRules.applesSpoiling(inventory.getNumItems(ItemType.APPLE), kept);
            if (rotting > 0) {
                inventory.removeItem(ItemType.APPLE, rotting);
            }
            return rotting;
        }

        /**
        * A chicken hatches in a random generated chunk, more often on the
        * plains than in the hills (RegionCatalog.chickenFactor). Returns
        * whether one did.
        */
        public bool hatchChicken() {
            int cap = gameConfig.getLagPreventionEnabled() ? MAX_CHICKENS : environment.getNumChunks();
            if (entityRepository.getNumEntitiesOfType(EntityType.CHICKEN) >= cap) {
                return false;
            }
            Chunk chunk = environment.getRandomChunk();
            if (chunk == null) {
                return false;
            }
            float chance = 50f * RegionCatalog.chickenFactor(regionMap.regionOf(chunk));
            if (random.range(0, 100) >= chance) {
                return false;
            }
            spawnChickenIn(chunk);
            return true;
        }

        private void spawnChickenIn(Chunk chunk) {
            System.Numerics.Vector3 at = chunk.getRandomLocation().getPosition();
            entityRepository.addEntity(new Chicken(new System.Numerics.Vector3(at.X, at.Y + 0.5f, at.Z), random));
        }

        /**
        * On the first tick of a new day, says so on the status line if a
        * season has begun, or if winter is a day away.
        */
        private void announce(int totalTicks) {
            if (totalTicks <= 0) {
                return;
            }
            TimeOfDay timeOfDay = calendar.getTimeOfDay();
            int today = TimeOfDay.dayAt(totalTicks, timeOfDay.getTicksPerDay(), timeOfDay.getStartFraction());
            int yesterday = TimeOfDay.dayAt(totalTicks - 1, timeOfDay.getTicksPerDay(), timeOfDay.getStartFraction());
            if (today == yesterday) {
                return;
            }
            string message = announcementFor(today);
            if (message == null) {
                return;
            }
            Nation nation = player.getNationId() == null || nationRepository == null ? null : nationRepository.getNation(player.getNationId());
            if (nation != null && SeasonCalendar.daysUntil(today, Season.WINTER) == 1) {
                // a day ahead, the nation's stores: on the status line, and kept in the chronicle
                string line = WinterStores.describeForChronicle(entityRepository, nation);
                if (line != null) {
                    message += " " + WinterStores.describe(nation, WinterStores.count(entityRepository, nation.getId()));
                    if (eventProducer != null) {
                        eventProducer.produceWorldEventEvent(WinterComingEventId, line,
                            new System.Collections.Generic.List<NationId> { nation.getId() }, null);
                    }
                }
            }
            player.getStatus().update(message);
        }

        /** What the status line says at the start of a day, or null for nothing. */
        public static string announcementFor(int day) {
            Season season = SeasonCalendar.seasonOfDay(day);
            if (SeasonCalendar.seasonOfDay(day - 1) != season) {
                return SeasonCalendar.name(season) + " has come. " + SeasonalRules.describe(season);
            }
            if (SeasonCalendar.daysUntil(day, Season.WINTER) == 1) {
                return "Winter comes tomorrow. Lay in food while the farms still yield.";
            }
            return null;
        }
    }
}
