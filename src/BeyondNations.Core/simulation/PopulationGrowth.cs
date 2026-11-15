using System.Text.Json;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * How the world gains pawns once it has been generated (#133).
    *
    * Pawns used to come only from world generation, and with "Respawn Pawns"
    * off every death removed one for good, so a world could only shrink.
    * Every TICKS_BETWEEN_CHECKS this:
    *
    *   - warms neighbours: pawns inside the same settlement at the same time
    *     grow fonder of each other (a gregarious pawn twice as fast);
    *   - brings offspring: two residents of a settlement who are fond enough
    *     of each other have a child there, if the settlement has room for
    *     another resident and food to spare -- the food is taken from its
    *     stockpile and stalls. The child takes a parent's surname and one
    *     trait from each parent, and is born into their nation and home;
    *   - brings immigrants: a thriving settlement (two stalls, or a village)
    *     with room now and then draws a nationless newcomer to its edge, who
    *     joins its nation the ordinary way;
    *   - sends out settlers: a full settlement with the wood for another in
    *     its stockpile sends a serf off with it to found a new settlement for
    *     the nation, which frees a place at home (and, since a settlement
    *     needs no stone, keeps a nation growing when its stone has run out).
    *     An ambitious serf is sent first, and it leaves the nation to found
    *     a rival one of its own;
    *   - brings wanderers: a world that has fallen below WANDERER_FLOOR pawns
    *     has a nationless wanderer turn up somewhere already generated.
    *
    * Nothing here takes the world past GameConfig.getMaxNumPawns() while lag
    * prevention is on, except births and newcomers in a settlement of the
    * player's nation, which may take it PLAYER_NATION_HEADROOM past it. Draws from its own RandomSource, so growth never
    * shifts the rest of a seeded world's stream.
    */
    public class PopulationGrowth {
        /** Thirty seconds at the default tick rate. */
        public const int TICKS_BETWEEN_CHECKS = 1500;
        public const int OFFSPRING_RELATIONSHIP = 10;
        public const int OFFSPRING_FOOD_COST = 3;
        public const int BIRTH_COOLDOWN_TICKS = 6000;
        public const int FAMILY_RELATIONSHIP = 10;
        public const int IMMIGRATION_COOLDOWN_TICKS = 9000;
        public const int IMMIGRATION_MIN_OWNED_STALLS = 2;
        public const float IMMIGRANT_ARRIVAL_DISTANCE = 40f;
        public const int WANDERER_FLOOR = 12;
        public const int WANDERER_COOLDOWN_TICKS = 3000;
        public const int SETTLER_COOLDOWN_TICKS = 9000;
        /**
        * How far past GameConfig.getMaxNumPawns() births and newcomers in a
        * settlement of the player's nation may take the world (#371). The
        * world fills to its limit in its first few minutes, and before this a
        * full world drew no newcomer and bore no child anywhere, so a player
        * who had not courted settlers in those minutes was held at a handful
        * of members for good.
        */
        public const int PLAYER_NATION_HEADROOM = 20;

        private readonly RandomSource random;

        internal RandomSource getRandom() {
            return random;
        }
        private readonly PawnNameGenerator nameGenerator;
        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly Environment environment;
        private readonly EventProducer eventProducer;
        private readonly GameConfig gameConfig;
        private readonly Player player;
        // the player's laws (realm/) can put immigrants off or draw them in
        private Realm realm;

        private readonly Dictionary<EntityId, int> lastBirthTicks = new Dictionary<EntityId, int>();
        private readonly Dictionary<EntityId, int> lastImmigrationTicks = new Dictionary<EntityId, int>();
        private readonly Dictionary<EntityId, int> lastSettlerTicks = new Dictionary<EntityId, int>();
        private int numSettlers = 0;
        private int numSecessions = 0;
        private int lastWandererTick = int.MinValue / 2;

        private int numBirths = 0;
        private int numImmigrants = 0;
        private int numWanderers = 0;

        public PopulationGrowth(RandomSource random, EntityRepository entityRepository, NationRepository nationRepository, Environment environment, EventProducer eventProducer, GameConfig gameConfig, Player player) {
            this.random = random;
            this.nameGenerator = new PawnNameGenerator(random);
            this.entityRepository = entityRepository;
            this.nationRepository = nationRepository;
            this.environment = environment;
            this.eventProducer = eventProducer;
            this.gameConfig = gameConfig;
            this.player = player;
        }

        public void setRealm(Realm realm) {
            this.realm = realm;
        }

        public int getNumBirths() { return numBirths; }
        public int getNumImmigrants() { return numImmigrants; }
        public int getNumWanderers() { return numWanderers; }
        public int getNumSettlers() { return numSettlers; }
        /** Settlers who were ambitious, and left their nation to found their own. */
        public int getNumSecessions() { return numSecessions; }

        public void update(int totalTicks) {
            if (totalTicks % TICKS_BETWEEN_CHECKS != 0) {
                return;
            }

            List<Settlement> settlements = new List<Settlement>();
            List<Pawn> pawns = new List<Pawn>();
            foreach (Entity entity in entityRepository.getEntities()) {
                if (entity.isMarkedForDeletion()) {
                    continue;
                }
                if (entity is Settlement) {
                    settlements.Add((Settlement) entity);
                }
                else if (entity is Pawn) {
                    pawns.Add((Pawn) entity);
                }
            }
            int numPawns = pawns.Count;

            foreach (Settlement settlement in settlements) {
                List<Pawn> present = new List<Pawn>();
                List<Pawn> residents = new List<Pawn>();
                foreach (Pawn pawn in pawns) {
                    if (settlement.getId().Equals(pawn.getCurrentSettlementId())) {
                        present.Add(pawn);
                    }
                    if (settlement.getId().Equals(pawn.getHomeSettlementId())) {
                        residents.Add(pawn);
                    }
                }
                warmNeighbours(present);

                if (!settlement.hasRoomForResident()) {
                    trySendSettler(settlement, residents, totalTicks);
                    continue;
                }
                if (!hasRoomForMorePawns(numPawns) && !(playerLeads(settlement) && numPawns < gameConfig.getMaxNumPawns() + PLAYER_NATION_HEADROOM)) {
                    continue;
                }
                if (tryBirth(settlement, residents, totalTicks)) {
                    numPawns++;
                    continue;
                }
                if (tryImmigration(settlement, totalTicks)) {
                    numPawns++;
                }
            }

            if (numPawns < WANDERER_FLOOR && hasRoomForMorePawns(numPawns) && totalTicks - lastWandererTick >= WANDERER_COOLDOWN_TICKS) {
                if (spawnWanderer()) {
                    lastWandererTick = totalTicks;
                }
            }
        }

        /** Whether the player leads the nation that holds this settlement. */
        private bool playerLeads(Settlement settlement) {
            if (player == null || settlement.getNationId() == null) {
                return false;
            }
            Nation nation = nationRepository.getNation(settlement.getNationId());
            return nation != null && player.getId().Equals(nation.getLeaderId());
        }

        private bool hasRoomForMorePawns(int numPawns) {
            return !gameConfig.getLagPreventionEnabled() || numPawns < gameConfig.getMaxNumPawns();
        }

        private static void warmNeighbours(List<Pawn> present) {
            for (int i = 0; i < present.Count; i++) {
                for (int j = i + 1; j < present.Count; j++) {
                    present[i].increaseRelationship(present[j], PawnTraits.neighbourGain(present[i]));
                    present[j].increaseRelationship(present[i], PawnTraits.neighbourGain(present[j]));
                }
            }
        }

        /** Whether these two are fond enough of each other to raise a child. */
        public static bool areCouple(Pawn a, Pawn b) {
            return relationship(a, b) >= OFFSPRING_RELATIONSHIP && relationship(b, a) >= OFFSPRING_RELATIONSHIP;
        }

        private static int relationship(Pawn from, Pawn to) {
            int value;
            return from.getRelationships().TryGetValue(to.getId(), out value) ? value : 0;
        }

        /** Food the settlement could spare for a newborn: its stockpile and its stalls. */
        public static int countFood(Settlement settlement) {
            int food = 0;
            foreach (ItemType type in FoodItems.getFoodInDescendingEnergyOrder()) {
                food += settlement.getInventory().getNumItems(type);
                food += settlement.getMarket().getQuantityAvailable(type);
            }
            return food;
        }

        private bool tryBirth(Settlement settlement, List<Pawn> residents, int totalTicks) {
            int lastBirth;
            if (lastBirthTicks.TryGetValue(settlement.getId(), out lastBirth) && totalTicks - lastBirth < BIRTH_COOLDOWN_TICKS) {
                return false;
            }
            if (countFood(settlement) < OFFSPRING_FOOD_COST) {
                return false;
            }
            Nation nation = nationRepository.getNation(settlement.getNationId());
            if (nation == null) {
                return false;
            }
            for (int i = 0; i < residents.Count; i++) {
                for (int j = i + 1; j < residents.Count; j++) {
                    if (areCouple(residents[i], residents[j])) {
                        bear(settlement, nation, residents[i], residents[j]);
                        lastBirthTicks[settlement.getId()] = totalTicks;
                        return true;
                    }
                }
            }
            return false;
        }

        private void bear(Settlement settlement, Nation nation, Pawn mother, Pawn father) {
            takeFood(settlement, OFFSPRING_FOOD_COST);

            string name = nameGenerator.generate();
            string surname = getSurname(mother.getName());
            if (surname != null) {
                name = getFirstName(name) + " " + surname;
            }
            Pawn child = new Pawn(settlement.getPosition(), name, random);
            child.setTraits(PawnTraits.inherit(mother, father, random));
            child.setEnergy(100);
            entityRepository.addEntity(child);
            nation.addMember(child.getId());
            child.setNationId(nation.getId());
            child.setColor(nation.getColor());
            child.setHomeSettlementId(settlement.getId());
            settlement.setResidentCount(settlement.getResidentCount() + 1);
            SettlementLife.admit(child, settlement);

            foreach (Pawn parent in new Pawn[] { mother, father }) {
                parent.increaseRelationship(child, FAMILY_RELATIONSHIP);
                child.increaseRelationship(parent, FAMILY_RELATIONSHIP);
            }
            eventProducer.producePawnSpawnEvent(settlement.getPosition(), child);
            numBirths++;
            Log.info(mother.getName() + " and " + father.getName() + " had a child in " + settlement.getName() + ": " + child.getName() + ".");
            if (player != null && player.getNationId() != null && player.getNationId() == nation.getId()) {
                player.getStatus().update(child.getName() + " was born to " + mother.getName() + " and " + father.getName() + ".");
            }
        }

        private static void takeFood(Settlement settlement, int amount) {
            foreach (ItemType type in FoodItems.getFoodInDescendingEnergyOrder()) {
                int fromStockpile = System.Math.Min(amount, settlement.getInventory().getNumItems(type));
                if (fromStockpile > 0) {
                    settlement.getInventory().removeItem(type, fromStockpile);
                    amount -= fromStockpile;
                }
                foreach (Stall stall in settlement.getMarket().getStalls()) {
                    if (amount == 0) {
                        return;
                    }
                    if (stall.getOwnerId() == null) {
                        continue;
                    }
                    int fromStall = System.Math.Min(amount, stall.getInventory().getNumItems(type));
                    if (fromStall > 0) {
                        stall.getInventory().removeItem(type, fromStall);
                        amount -= fromStall;
                    }
                }
                if (amount == 0) {
                    return;
                }
            }
        }

        private void trySendSettler(Settlement settlement, List<Pawn> residents, int totalTicks) {
            int last;
            if (lastSettlerTicks.TryGetValue(settlement.getId(), out last) && totalTicks - last < SETTLER_COOLDOWN_TICKS) {
                return;
            }
            if (settlement.getInventory().getNumItems(ItemType.WOOD) < Settlement.WOOD_COST_TO_BUILD) {
                return;
            }
            Nation nation = nationRepository.getNation(settlement.getNationId());
            if (nation == null) {
                return;
            }
            Pawn chosen = null;
            foreach (Pawn pawn in residents) {
                if (nation.getRole(pawn.getId()) != NationRole.SERF || pawn.getOrder() != null) {
                    continue;
                }
                // an ambitious serf is the first to go, and goes for itself
                if (chosen == null || (pawn.hasTrait(PawnTrait.AMBITIOUS) && !chosen.hasTrait(PawnTrait.AMBITIOUS))) {
                    chosen = pawn;
                }
            }
            // a nation the player leads sends no settler its era cannot house (EraRules);
            // an ambitious serf still leaves to found a nation of its own
            if (chosen != null && !chosen.hasTrait(PawnTrait.AMBITIOUS) && !nation.canFoundSettlement()) {
                chosen = null;
            }
            if (chosen != null) {
                Pawn pawn = chosen;
                settlement.getInventory().removeItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
                pawn.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
                pawn.setHomeSettlementId(null);
                pawn.setSettler(true);
                settlement.setResidentCount(settlement.getResidentCount() - 1);
                lastSettlerTicks[settlement.getId()] = totalTicks;
                numSettlers++;
                if (pawn.hasTrait(PawnTrait.AMBITIOUS)) {
                    // it leaves the nation to found one of its own
                    nation.removeMember(pawn.getId());
                    pawn.setNationId(null);
                    eventProducer.produceNationLeaveEvent(nation, pawn.getId());
                    numSecessions++;
                    Log.info(pawn.getName() + " left crowded " + settlement.getName() + " to found a nation of its own.");
                    return;
                }
                Log.info(pawn.getName() + " set out from crowded " + settlement.getName() + " to found a new settlement.");
            }
        }

        /** Whether a settlement is doing well enough to draw newcomers. */
        public static bool isThriving(Settlement settlement) {
            if (settlement.getTier() >= SettlementTier.VILLAGE) {
                return true;
            }
            int owned = 0;
            foreach (Stall stall in settlement.getMarket().getStalls()) {
                if (stall.getOwnerId() != null) {
                    owned++;
                }
            }
            return owned >= IMMIGRATION_MIN_OWNED_STALLS;
        }

        private bool tryImmigration(Settlement settlement, int totalTicks) {
            if (!isThriving(settlement)) {
                return false;
            }
            int cooldown = realm == null ? IMMIGRATION_COOLDOWN_TICKS : realm.immigrationCooldown(settlement, IMMIGRATION_COOLDOWN_TICKS);
            if (cooldown < 0) {
                return false;
            }
            int last;
            if (lastImmigrationTicks.TryGetValue(settlement.getId(), out last) && totalTicks - last < cooldown) {
                return false;
            }
            lastImmigrationTicks[settlement.getId()] = totalTicks;

            float angle = random.range(0f, 2f * System.MathF.PI);
            Vector3 centre = settlement.getPosition();
            Vector3 position = new Vector3(centre.X + System.MathF.Cos(angle) * IMMIGRANT_ARRIVAL_DISTANCE, 1.5f, centre.Z + System.MathF.Sin(angle) * IMMIGRANT_ARRIVAL_DISTANCE);
            spawnNewcomer(position);
            numImmigrants++;
            Log.info("A newcomer arrived at " + settlement.getName() + ".");
            return true;
        }

        private bool spawnWanderer() {
            List<Chunk> chunks = environment.getChunks();
            if (chunks.Count == 0) {
                return false;
            }
            Chunk chunk = chunks[random.range(0, chunks.Count)];
            // picked here rather than with chunk.getRandomLocation(), which draws from the world's stream
            Location location = chunk.getLocation(random.range(0, chunk.getSize()), random.range(0, chunk.getSize()));
            Vector3 at = location.getPosition();
            spawnNewcomer(new Vector3(at.X, at.Y + 1.5f, at.Z));
            numWanderers++;
            return true;
        }

        private Pawn spawnNewcomer(Vector3 position) {
            Pawn pawn = new Pawn(position, nameGenerator.generate(), random);
            pawn.setTraits(PawnTraits.roll(random));
            entityRepository.addEntity(pawn);
            eventProducer.producePawnSpawnEvent(position, pawn);
            return pawn;
        }

        private static string getFirstName(string name) {
            int space = name.IndexOf(' ');
            return space < 0 ? name : name.Substring(0, space);
        }

        private static string getSurname(string name) {
            int space = name.LastIndexOf(' ');
            return space < 0 ? null : name.Substring(space + 1);
        }

        // --- saving (docs/SAVES.md) ---

        internal void save(SaveWriter writer) {
            writer.writeIdIntMap("lastBirthTicks", lastBirthTicks);
            writer.writeIdIntMap("lastImmigrationTicks", lastImmigrationTicks);
            writer.writeIdIntMap("lastSettlerTicks", lastSettlerTicks);
            writer.write("numSettlers", numSettlers);
            writer.write("numSecessions", numSecessions);
            writer.write("lastWandererTick", lastWandererTick);
            writer.write("numBirths", numBirths);
            writer.write("numImmigrants", numImmigrants);
            writer.write("numWanderers", numWanderers);
            writer.writeStringList("namesGenerated", nameGenerator.getGenerated());
        }

        internal void load(JsonElement element, SaveReader reader) {
            reader.readIdIntMap(element, "lastBirthTicks", lastBirthTicks);
            reader.readIdIntMap(element, "lastImmigrationTicks", lastImmigrationTicks);
            reader.readIdIntMap(element, "lastSettlerTicks", lastSettlerTicks);
            numSettlers = SaveReader.getInt(element, "numSettlers");
            numSecessions = SaveReader.getInt(element, "numSecessions");
            lastWandererTick = SaveReader.getInt(element, "lastWandererTick");
            numBirths = SaveReader.getInt(element, "numBirths");
            numImmigrants = SaveReader.getInt(element, "numImmigrants");
            numWanderers = SaveReader.getInt(element, "numWanderers");
            nameGenerator.restoreGenerated(SaveReader.getStringList(element, "namesGenerated"));
        }
    }
}
