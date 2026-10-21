using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations {

    /**
    * What a world event can see and touch while it is being prepared or
    * resolved: the world, the event stream's random numbers, and a few
    * helpers for the things nearly every event does (paying, finding
    * residents, taking food), so each event reads as its story rather than
    * as bookkeeping. Made fresh for each call and never kept.
    */
    public class WorldEventContext {
        private readonly Simulation simulation;
        private readonly WorldEvents worldEvents;

        public WorldEventContext(Simulation simulation, WorldEvents worldEvents) {
            this.simulation = simulation;
            this.worldEvents = worldEvents;
        }

        public Simulation getSimulation() { return simulation; }
        public WorldEvents getWorldEvents() { return worldEvents; }
        /** The world events' own stream: drawing from it never shifts the rest of a seeded world. */
        public RandomSource getRandom() { return worldEvents.getRandom(); }
        public Player getPlayer() { return simulation.getPlayer(); }
        public int getTick() { return simulation.getTickCounter().getTotalTicks(); }

        public int ticks(int seconds) {
            return seconds * simulation.getTickCounter().getTicksPerSecond();
        }

        // --- seasons --------------------------------------------------------

        /** The season it is now (SeasonCalendar): no drought or bountiful harvest in winter. */
        public Season getSeason() {
            return simulation.getSeasonCalendar().getSeason();
        }

        public bool isWinter() {
            return simulation.getSeasonCalendar().isWinter();
        }

        /** The kind of land at a point (RegionMap). */
        public Region regionAt(Vector3 position) {
            return simulation.getRegionMap().regionAt(position);
        }

        // --- nations and settlements ----------------------------------------

        public Nation getNation(NationId id) {
            return id == null ? null : simulation.getNationRepository().getNation(id);
        }

        public Settlement getSettlement(EntityId id) {
            return id == null ? null : simulation.getEntityRepository().getEntity(id) as Settlement;
        }

        public Entity getEntity(EntityId id) {
            return id == null ? null : simulation.getEntityRepository().getEntity(id);
        }

        public bool isLedByPlayer(Nation nation) {
            return nation != null && nation.getLeaderId() != null && nation.getLeaderId().Equals(getPlayer().getId());
        }

        public bool isPlayersNation(Nation nation) {
            return nation != null && getPlayer().getNationId() != null && getPlayer().getNationId().Equals(nation.getId());
        }

        /** The nation's settlements that still stand, in the nation's order. */
        public List<Settlement> settlementsOf(Nation nation) {
            List<Settlement> settlements = new List<Settlement>();
            if (nation == null) {
                return settlements;
            }
            foreach (EntityId id in nation.getSettlements()) {
                Settlement settlement = getSettlement(id);
                if (settlement != null && !settlement.isMarkedForDeletion()) {
                    settlements.Add(settlement);
                }
            }
            return settlements;
        }

        /** The nation's member pawns, in the nation's order. */
        public List<Pawn> membersOf(Nation nation) {
            List<Pawn> members = new List<Pawn>();
            if (nation == null) {
                return members;
            }
            foreach (EntityId id in nation.getMemberIds()) {
                if (getEntity(id) is Pawn pawn && !pawn.isMarkedForDeletion()) {
                    members.Add(pawn);
                }
            }
            return members;
        }

        /** The pawns that call the settlement home, in repository order. */
        public List<Pawn> residentsOf(Settlement settlement) {
            List<Pawn> residents = new List<Pawn>();
            if (settlement == null) {
                return residents;
            }
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN)) {
                Pawn pawn = (Pawn) entity;
                if (!pawn.isMarkedForDeletion() && pawn.getHomeSettlementId() != null && pawn.getHomeSettlementId().Equals(settlement.getId())) {
                    residents.Add(pawn);
                }
            }
            return residents;
        }

        public int countFarms(Nation nation) {
            int farms = 0;
            foreach (Settlement settlement in settlementsOf(nation)) {
                farms += settlement.getNumBuildings(BuildingType.FARM);
            }
            return farms;
        }

        // --- paying ---------------------------------------------------------

        /**
        * The coins the chooser can spend on an event: the player's purse and,
        * when the event is about a nation, its treasuries (a leader pays from
        * either). A pawn-led nation pays from its treasuries.
        */
        public int availableCoins(WorldEventInstance instance) {
            int coins = instance.isForPlayer() ? getPlayer().getInventory().getNumItems(ItemType.COIN) : 0;
            foreach (Settlement settlement in settlementsOf(getNation(instance.getNationId()))) {
                coins += settlement.getFunds();
            }
            return coins;
        }

        /** Takes coins as availableCoins counts them, the player's purse first. Returns false, taking nothing, if short. */
        public bool payCoins(WorldEventInstance instance, int amount) {
            return pay(instance, ItemType.COIN, amount);
        }

        /** Wood, stone or food the chooser can spend: the player's pack and the nation's stockpiles. */
        public int available(WorldEventInstance instance, ItemType itemType) {
            int count = instance.isForPlayer() ? getPlayer().getInventory().getNumItems(itemType) : 0;
            foreach (Settlement settlement in settlementsOf(getNation(instance.getNationId()))) {
                count += settlement.getInventory().getNumItems(itemType);
            }
            return count;
        }

        public bool pay(WorldEventInstance instance, ItemType itemType, int amount) {
            if (amount <= 0) {
                return true;
            }
            if (itemType == ItemType.COIN ? availableCoins(instance) < amount : available(instance, itemType) < amount) {
                return false;
            }
            if (instance.isForPlayer()) {
                amount -= takeUpTo(getPlayer().getInventory(), itemType, amount);
            }
            foreach (Settlement settlement in settlementsOf(getNation(instance.getNationId()))) {
                if (amount == 0) {
                    break;
                }
                amount -= takeUpTo(settlement.getInventory(), itemType, amount);
            }
            return true;
        }

        public static int takeUpTo(Inventory inventory, ItemType itemType, int amount) {
            int taken = Math.Min(amount, inventory.getNumItems(itemType));
            if (taken > 0) {
                inventory.removeItem(itemType, taken);
            }
            return taken;
        }

        /** Food of every kind the chooser holds, counting the settlements' owned stalls too. */
        public int availableFood(WorldEventInstance instance) {
            int food = 0;
            foreach (ItemType type in FoodItems.getFoodInDescendingEnergyOrder()) {
                food += available(instance, type);
                foreach (Settlement settlement in settlementsOf(getNation(instance.getNationId()))) {
                    foreach (Stall stall in settlement.getMarket().getStalls()) {
                        if (stall.getOwnerId() != null) {
                            food += stall.getInventory().getNumItems(type);
                        }
                    }
                }
            }
            return food;
        }

        /** Takes food, the least nourishing first so a meal is not wasted on a stranger; false, taking nothing, if short. */
        public bool payFood(WorldEventInstance instance, int amount) {
            if (availableFood(instance) < amount) {
                return false;
            }
            List<ItemType> order = new List<ItemType>(FoodItems.getFoodInDescendingEnergyOrder());
            order.Reverse();
            foreach (ItemType type in order) {
                if (amount == 0) {
                    return true;
                }
                if (instance.isForPlayer()) {
                    amount -= takeUpTo(getPlayer().getInventory(), type, amount);
                }
                foreach (Settlement settlement in settlementsOf(getNation(instance.getNationId()))) {
                    amount -= takeUpTo(settlement.getInventory(), type, amount);
                    foreach (Stall stall in settlement.getMarket().getStalls()) {
                        if (stall.getOwnerId() != null && amount > 0) {
                            amount -= takeUpTo(stall.getInventory(), type, amount);
                        }
                    }
                }
            }
            return true;
        }

        // --- people ---------------------------------------------------------

        /**
        * A newcomer standing near a point, made the way world generation
        * makes pawns (the world's stream for the pawn, the trait stream for
        * its traits). Null when lag prevention says the world is full.
        */
        public Pawn spawnPawnNear(Vector3 centre, float distance) {
            GameConfig config = simulation.getGameConfig();
            EntityRepository entities = simulation.getEntityRepository();
            if (config.getLagPreventionEnabled() && entities.getNumEntitiesOfType(EntityType.PAWN) >= config.getMaxNumPawns()) {
                return null;
            }
            float angle = getRandom().range(0f, 2f * MathF.PI);
            Vector3 position = new Vector3(centre.X + MathF.Cos(angle) * distance, 1.5f, centre.Z + MathF.Sin(angle) * distance);
            Pawn pawn = new Pawn(position, simulation.getPawnNameGenerator().generate(), simulation.getRandom());
            pawn.setTraits(PawnTraits.roll(simulation.getTraitRandom()));
            entities.addEntity(pawn);
            simulation.getEventProducer().producePawnSpawnEvent(position, pawn);
            return pawn;
        }

        /** Makes a nationless pawn a member, the ordinary way. */
        public void join(Pawn pawn, Nation nation) {
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.setColor(nation.getColor());
            simulation.getEventProducer().produceNationJoinEvent(nation, pawn.getId());
        }

        /** Raises how a pawn feels about the player (a gregarious pawn warms faster, as everywhere). */
        public void warmToPlayer(Pawn pawn, int amount) {
            PawnRelations.raise(pawn, getPlayer(), amount, simulation.getEventProducer());
        }

        /** Says something on the player's status line. */
        public void tellPlayer(string text) {
            getPlayer().getStatus().update(text);
        }

        /** "you" for the player, otherwise the pawn's name. */
        public string nameOf(EntityId id) {
            Entity entity = getEntity(id);
            if (entity == null) {
                return "a stranger";
            }
            return entity.getType() == EntityType.PLAYER ? "you" : entity.getName();
        }

        /** Who chose, for a line of history: "You" or the nation's leader. */
        public string chooser(WorldEventInstance instance) {
            if (instance.isForPlayer()) {
                return "you";
            }
            Nation nation = getNation(instance.getNationId());
            return nation == null ? "its leader" : nameOf(nation.getLeaderId());
        }
    }
}
