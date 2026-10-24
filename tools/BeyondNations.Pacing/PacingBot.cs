using System;
using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.pacing {

    /**
    * A scripted "reasonable player" for the pacing harness (docs/PACING.md).
    *
    * It plays the way a person who reads the objective hints would: it
    * works on whatever the current objective is, walks (sprinting) to things
    * rather than teleporting, presses one key at a time, and only uses the
    * Core commands the keys and HUD buttons run. It never uses a debug
    * command, so the times it records are times a player could reach.
    *
    * It reads the current objective generically. Ids it knows get a
    * specific plan; any other objective is planned from its progress
    * counter's label ("Wood", "Stone", "Members", "Settlements", "Funds",
    * "Day"), and anything else falls back to general play (gather, sell,
    * befriend, expand). That way a rework of the objective list still gets
    * measured instead of stalling the harness.
    *
    * Everything the bot does is a function of the simulation state and the
    * tick count, so a seed gives the same run every time.
    */
    public sealed class PacingBot {

        /** Ticks between decisions: five a second at the default rate, a quick player's key rate. */
        public const int DecisionTicks = 10;

        /** How close the bot walks to something before it presses a key at it. */
        private const float ArriveDistance = 2.5f;

        /** Interact, gifts and invitations all reach this far. */
        private const float ReachDistance = 4.5f;

        /** Resources carried before the bot goes to market to sell. */
        private const int SellBatch = 10;

        /** Coins the bot keeps back rather than spend on gifts. */
        private const int CoinReserve = 20;

        private readonly Simulation simulation;
        private readonly Player player;

        private Vector3? walkTarget = null;
        private string activity = "idle";
        private bool foundedOnce = false;
        private int nextSellTick = 0;
        private int nextSocialTick = 0;
        private int nextDiplomacyTick = 0;
        private EntityId recruitId = null;
        private int courtingSinceTick = 0;

        /** How long the bot courts one settler before trying another: two minutes at the default rate. */
        private const int CourtingTicks = 120 * TickCounter.DefaultTicksPerSecond;

        public PacingBot(Simulation simulation) {
            this.simulation = simulation ?? throw new ArgumentNullException(nameof(simulation));
            this.player = simulation.getPlayer();
        }

        /** What the bot was last doing, for the report. */
        public string getActivity() { return activity; }

        /**
        * Called once per tick, before Simulation.fixedUpdate, the same place
        * the host applies a frame's input.
        */
        public void beforeTick() {
            int tick = simulation.getTickCounter().getTotalTicks();
            if (tick % DecisionTicks == 0) {
                decide();
            }
            steer();
        }

        // --- steering ---------------------------------------------------------

        private void walkTo(Vector3 target) {
            walkTarget = target;
        }

        private void stop() {
            walkTarget = null;
            player.setMovementInput(0, 0);
            player.setSprinting(false);
        }

        private void steer() {
            if (walkTarget == null || player.isCurrentlyInSettlement()) {
                player.setMovementInput(0, 0);
                return;
            }
            Vector3 target = walkTarget.Value;
            float dx = target.X - player.getPosition().X;
            float dz = target.Z - player.getPosition().Z;
            float distance = MathF.Sqrt(dx * dx + dz * dz);
            if (distance < ArriveDistance) {
                stop();
                return;
            }
            // face the target exactly (a right-drag turn) and run at it
            float desiredYaw = MathF.Atan2(dx, dz);
            player.turn(desiredYaw - player.getYaw());
            player.setSprinting(true);
            player.setMovementInput(0, 1);
        }

        private float groundDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        /** Walks to the position, or returns true once within reach of it. */
        private bool approach(Vector3 position) {
            if (groundDistance(player.getPosition(), position) <= ReachDistance) {
                stop();
                return true;
            }
            walkTo(position);
            return false;
        }

        // --- deciding ---------------------------------------------------------

        private void decide() {
            if (recover()) {
                return;
            }
            Objective current = simulation.getObjectiveTracker().getCurrent();
            if (current == null) {
                generalPlay();
                return;
            }
            switch (current.getId()) {
                case "gather-wood": gather(EntityType.TREE); return;
                // Food comes with the wood; a new world starts the player hungry enough to eat it at once.
                case "eat": gather(EntityType.TREE); return;
                case "join-nation": joinNation(); return;
                case "found-settlement": foundSettlement(); return;
                case "enter-settlement": enterOwnSettlement(); return;
                case "build-stall": buildStall(); return;
                case "sell-resources": sellResources(); return;
                case "second-settlement": foundSettlement(); return;
                // "Reach the Village era" (the arc's seventh step), like every era ambition
                case "grow-nation": advanceEra(); return;
            }
            ObjectiveProgress progress = simulation.getObjectiveTracker().getCurrentProgress();
            planFromLabel(progress.hasCounter() ? progress.getLabel() : null);
        }

        private void planFromLabel(string label) {
            switch (label) {
                case "Wood": gather(EntityType.TREE); return;
                case "Stone": gather(EntityType.ROCK); return;
                case "Members": growNation(); return;
                case "Settlements": foundSettlement(); return;
                case "Funds": earnFunds(); return;
                case "Day": generalPlay(); return;
                case "Buildings": buildBuilding(); return;
                case "Requirements": advanceEra(); return;
                default: generalPlay(); return;
            }
        }

        /**
        * A player who has lost everything to a siege starts again: a nation
        * of their own, then a settlement. Only once the arc has had them
        * found one, so the arc's own steps are measured as they come.
        */
        private bool recover() {
            if (defend()) {
                return true;
            }
            if (!foundedOnce) {
                foundedOnce = WorldView.settlements(simulation) > 0;
                return false;
            }
            if (player.getNationId() == null) {
                joinNation();
                return true;
            }
            if (WorldView.isLeader(simulation) && WorldView.settlements(simulation) == 0) {
                foundSettlement();
                return true;
            }
            return false;
        }

        /**
        * The siege warning says "Gift, ally or grow it to hold it", so a
        * player who can afford it sends the besieger a gift (Diplomacy panel),
        * one a second at most.
        */
        private bool defend() {
            Nation own = WorldView.playerNation(simulation);
            if (own == null || !WorldView.isLeader(simulation) || WorldView.coins(simulation) < Diplomacy.PLAYER_GIFT_COINS) {
                return false;
            }
            int tick = simulation.getTickCounter().getTotalTicks();
            if (tick < nextDiplomacyTick) {
                return false;
            }
            foreach (EntityId settlementId in own.getSettlements()) {
                if (!(simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement)) {
                    continue;
                }
                Nation besieger = simulation.getDiplomacy().getBesieger(settlement);
                if (besieger == null || simulation.getNationRepository().getNation(besieger.getId()) == null) {
                    continue;
                }
                activity = "buying off a siege";
                new GiftNationCommand(simulation.getNationRepository(), simulation.getDiplomacy()).execute(player, besieger);
                nextDiplomacyTick = tick + simulation.getTickCounter().getTicksPerSecond();
                return true;
            }
            return false;
        }

        /** What the bot does with no specific goal: earn, then expand, then recruit. */
        private void generalPlay() {
            if (player.getNationId() == null) {
                joinNation();
                return;
            }
            if (WorldView.isLeader(simulation) && WorldView.nationStalls(simulation) == 0) {
                buildStall();
                return;
            }
            earnFunds();
        }

        // --- plans ------------------------------------------------------------

        private void leaveSettlementIfInside() {
            if (player.isCurrentlyInSettlement()) {
                interact(); // Interact inside a settlement walks out of it
            }
        }

        private void gather(EntityType source) {
            activity = source == EntityType.TREE ? "gathering wood" : "gathering stone";
            if (player.isCurrentlyInSettlement()) {
                leaveSettlementIfInside();
                return;
            }
            Entity resource = nearestClearResource(source);
            if (resource == null) {
                // nothing in sight: walk on and let the world generate more
                walkTo(player.getPosition() + new Vector3(60, 0, 0));
                return;
            }
            if (approach(resource.getPosition())) {
                interact();
            }
        }

        private void joinNation() {
            activity = "creating a nation";
            if (player.getNationId() != null) {
                return; // pressed already; the objective is checked once a second
            }
            leaveSettlementIfInside();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getRandom(), simulation.getNationNameGenerator()).execute(player);
        }

        private void foundSettlement() {
            if (player.getNationId() == null) {
                joinNation();
                return;
            }
            if (!WorldView.isLeader(simulation)) {
                // the objective hint: leave, then lead a nation of your own
                activity = "leaving to lead";
                leaveSettlementIfInside();
                new NationLeaveCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                    simulation.getEntityRepository()).execute(player);
                return;
            }
            if (WorldView.wood(simulation) < Settlement.WOOD_COST_TO_BUILD) {
                gather(EntityType.TREE);
                return;
            }
            activity = "founding a settlement";
            if (player.isCurrentlyInSettlement()) {
                leaveSettlementIfInside();
                return;
            }
            Vector3? site = nearestFoundingSite();
            if (site == null) {
                walkTo(player.getPosition() + new Vector3(100, 0, 0));
                return;
            }
            if (groundDistance(player.getPosition(), site.Value) <= ArriveDistance) {
                stop();
                new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                    simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom()).execute(player);
            }
            else {
                walkTo(site.Value);
            }
        }

        private void enterOwnSettlement() {
            activity = "entering the settlement";
            if (WorldView.isInOwnSettlement(simulation)) {
                return;
            }
            leaveSettlementIfInside();
            Settlement home = homeSettlement();
            if (home == null) {
                foundSettlement();
                return;
            }
            if (approach(home.getPosition())) {
                interact();
            }
        }

        private void buildStall() {
            if (!WorldView.isLeader(simulation)) {
                foundSettlement(); // becomes a leader first
                return;
            }
            Settlement home = homeSettlement();
            if (home == null) {
                foundSettlement();
                return;
            }
            if (WorldView.wood(simulation) < Stall.WOOD_COST_TO_BUILD) {
                gather(EntityType.TREE);
                return;
            }
            if (WorldView.stone(simulation) < Stall.STONE_COST_TO_BUILD) {
                gather(EntityType.ROCK);
                return;
            }
            activity = "building a stall";
            if (WorldView.isInOwnSettlement(simulation)) {
                new BuildStallCommand(simulation.getNationRepository(), simulation.getEntityRepository(),
                    simulation.getEventProducer()).execute(player);
                return;
            }
            enterOwnSettlement();
        }

        /**
        * Works on the next era (EraRules) as the era box lists it: the first
        * requirement still short, by the plan its label names.
        */
        private void advanceEra() {
            EraRequirement? unmet = EraRules.firstUnmet(simulation.getEraTracker().getRequirements(simulation));
            if (unmet == null) {
                generalPlay();
                return;
            }
            switch (unmet.Value.label) {
                case "Village":
                case "Town":
                case "Towns": buildBuilding(); return;
                case "Stalls": buildStall(); return;
                case "Members": growNation(); return;
                case "Settlements": foundSettlement(); return;
                case "Funds": earnFunds(); return;
                // waiting on the calendar: get ahead on the next era's members
                case "Day": growNation(); return;
                default: generalPlay(); return;
            }
        }

        /**
        * Puts up whatever building the settlement needs most (K), in the
        * nation's settlement nearest to becoming a town, gathering the wood
        * and stone it costs first.
        */
        private void buildBuilding() {
            if (!WorldView.isLeader(simulation)) {
                foundSettlement();
                return;
            }
            Settlement target = settlementToBuildIn();
            if (target == null) {
                foundSettlement();
                return;
            }
            BuildingType? wanted = BuildingPlanner.suggestNext(target);
            if (wanted == null) {
                generalPlay();
                return;
            }
            Inventory stockpile = target.getInventory();
            if (WorldView.wood(simulation) + stockpile.getNumItems(ItemType.WOOD) < BuildingCatalog.getWoodCost(wanted.Value)) {
                gather(EntityType.TREE);
                return;
            }
            if (WorldView.stone(simulation) + stockpile.getNumItems(ItemType.STONE) < BuildingCatalog.getStoneCost(wanted.Value)) {
                gather(EntityType.ROCK);
                return;
            }
            activity = "building a " + BuildingCatalog.getDisplayName(wanted.Value).ToLowerInvariant();
            if (player.getCurrentSettlementId() == target.getId()) {
                new BuildBuildingCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player, null);
                return;
            }
            leaveSettlementIfInside();
            if (approach(target.getPosition())) {
                interact();
            }
        }

        /** The nation's settlement with the most buildings that is not yet a town, or its first. */
        private Settlement settlementToBuildIn() {
            Nation nation = WorldView.playerNation(simulation);
            if (nation == null) {
                return null;
            }
            Settlement best = null;
            foreach (EntityId settlementId in nation.getSettlements()) {
                if (!(simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement)
                        || BuildingPlanner.suggestNext(settlement) == null) {
                    continue;
                }
                bool town = settlement.getTier() == SettlementTier.TOWN;
                bool bestTown = best != null && best.getTier() == SettlementTier.TOWN;
                if (best == null || (bestTown && !town) || (town == bestTown && settlement.getNumBuildings() > best.getNumBuildings())) {
                    best = settlement;
                }
            }
            return best;
        }

        private void sellResources() {
            int carried = WorldView.wood(simulation) + WorldView.stone(simulation);
            if (carried < SellBatch && !player.isCurrentlyInSettlement()) {
                gather(WorldView.wood(simulation) <= WorldView.stone(simulation) ? EntityType.TREE : EntityType.ROCK);
                return;
            }
            if (carried == 0) {
                leaveSettlementIfInside();
                return;
            }
            activity = "selling at market";
            Settlement market = nearestMarket();
            if (market == null) {
                if (WorldView.nationStalls(simulation) == 0) {
                    buildStall(); // no market anywhere: build one at home
                    return;
                }
                // wait at home for a settler to buy the stall and earn coins
                activity = "waiting for a buyer";
                if (!WorldView.isInOwnSettlement(simulation)) {
                    enterOwnSettlement();
                    activity = "waiting for a buyer";
                }
                return;
            }
            if (player.isCurrentlyInSettlement()) {
                if (player.getCurrentSettlementId() != market.getId()) {
                    leaveSettlementIfInside();
                    return;
                }
                int tick = simulation.getTickCounter().getTotalTicks();
                if (tick < nextSellTick) {
                    return;
                }
                int salesBefore = player.getMarketSales();
                new SellResourcesAtMarketCommand(simulation.getNationRepository(), simulation.getEntityRepository()).execute(player);
                // one sale a second at most; a refusal is retried after a few seconds
                nextSellTick = tick + simulation.getTickCounter().getTicksPerSecond() * (player.getMarketSales() > salesBefore ? 1 : 5);
                return;
            }
            if (approach(market.getPosition())) {
                interact();
            }
        }

        private void earnFunds() {
            Settlement home = homeSettlement();
            if (WorldView.isLeader(simulation) && home != null
                    && home.getMarket().getNumStalls() < 3 && WorldView.wood(simulation) >= Stall.WOOD_COST_TO_BUILD
                    && WorldView.stone(simulation) >= Stall.STONE_COST_TO_BUILD) {
                // stalls earn the treasury a coin each per income tick
                buildStall();
                return;
            }
            sellResources();
        }

        private void growNation() {
            if (player.getNationId() == null) {
                joinNation();
                return;
            }
            activity = "recruiting settlers";
            if (player.isCurrentlyInSettlement()) {
                leaveSettlementIfInside();
                return;
            }
            Pawn recruit = currentRecruit();
            if (recruit == null) {
                // nobody to win over: earn, and let settlers find the nation's settlements
                earnFunds();
                return;
            }
            if (!approach(recruit.getPosition())) {
                return;
            }
            // the keys act on the nearest pawn in reach, which may be another one
            Pawn inReach = PawnRelations.findPawnInRange(player, simulation.getEnvironment());
            if (inReach == null) {
                walkTo(recruit.getPosition());
                return;
            }
            if (!isRecruitable(inReach)) {
                walkTo(recruit.getPosition());
                return;
            }
            RelationshipTier needed = inReach.getNationId() == null
                ? InvitePawnToNationCommand.NATIONLESS_TIER_REQUIRED
                : InvitePawnToNationCommand.RIVAL_TIER_REQUIRED;
            if (PawnRelations.getTier(inReach, player) >= needed) {
                new InvitePawnToNationCommand(simulation.getNationRepository(), simulation.getEventProducer(),
                    simulation.getEntityRepository()).execute(player, inReach);
                recruitId = null;
                return;
            }
            int tick = simulation.getTickCounter().getTotalTicks();
            if (tick < nextSocialTick) {
                return;
            }
            // a conversation or a gift a second at most, as fast as a player reads the reply
            nextSocialTick = tick + simulation.getTickCounter().getTicksPerSecond();
            if (!inReach.isConversationWarm(player.getId(), tick, PawnRelations.TALK_COOLDOWN_TICKS)) {
                interact();
            }
            else if (GiftPawnCommand.canGift(player) && (FoodItems.hasFood(player.getInventory())
                    || WorldView.coins(simulation) >= CoinReserve + GiftPawnCommand.COIN_GIFT_AMOUNT)) {
                new GiftPawnCommand(simulation.getEventProducer()).execute(player, inReach);
            }
            else {
                recruitId = null; // out of gifts: try again later
                earnFunds();
            }
        }

        /**
        * Anyone the invitation could persuade: a nationless settler (it joins
        * at Acquaintance), or another nation's member who does not lead it and
        * is not Loyal by nature (it needs Loyal standing, and poaching sours
        * relations). currentRecruit prefers nationless settlers.
        */
        private bool isRecruitable(Pawn pawn) {
            if (pawn.isMarkedForDeletion() || pawn.isCurrentlyInSettlement() || !InvitePawnToNationCommand.isApplicable(player, pawn)) {
                return false;
            }
            if (pawn.getNationId() == null) {
                return true;
            }
            Nation nation = simulation.getNationRepository().getNation(pawn.getNationId());
            return nation != null && nation.getLeaderId() != pawn.getId()
                && PawnTraits.relationshipToDefect(pawn) <= RelationshipTiers.LOYAL_THRESHOLD;
        }

        /** The settler being courted; the bot sticks with one until it joins or wanders off. */
        private Pawn currentRecruit() {
            int tick = simulation.getTickCounter().getTotalTicks();
            // stay with a half-won settler while it is out and about, for up to two minutes
            if (recruitId != null && simulation.getEntityRepository().getEntity(recruitId) is Pawn kept
                    && tick - courtingSinceTick < CourtingTicks * simulation.getTickCounter().getTicksPerSecond() / TickCounter.DefaultTicksPerSecond
                    && isRecruitable(kept)) {
                return kept;
            }
            recruitId = null;
            Pawn best = null;
            // the nearest one anywhere: a player with no one in sight goes looking (the minimap shows pawns)
            float bestDistance = float.MaxValue;
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN)) {
                Pawn pawn = (Pawn) entity;
                if (!isRecruitable(pawn)) {
                    continue;
                }
                float distance = groundDistance(player.getPosition(), pawn.getPosition());
                if (pawn.getNationId() != null) {
                    distance += 100000f; // only once no nationless settler is left anywhere
                }
                if (distance < bestDistance) {
                    bestDistance = distance;
                    best = pawn;
                }
            }
            if (best != null) {
                recruitId = best.getId();
                courtingSinceTick = tick;
            }
            return best;
        }

        // --- what the bot can see ---------------------------------------------

        private void interact() {
            new InteractCommand(simulation.getEnvironment(), simulation.getNationRepository(), simulation.getEventProducer(),
                simulation.getEntityRepository(), simulation.getRandom(), simulation.getTickCounter()).execute(player);
        }

        private Settlement homeSettlement() {
            EntityId id = player.getHomeSettlementId();
            if (id != null && simulation.getEntityRepository().getEntity(id) is Settlement home
                    && home.getNationId() == player.getNationId()) {
                return home;
            }
            Nation nation = WorldView.playerNation(simulation);
            if (nation == null) {
                return null;
            }
            foreach (EntityId settlementId in nation.getSettlements()) {
                if (simulation.getEntityRepository().getEntity(settlementId) is Settlement settlement) {
                    return settlement;
                }
            }
            return null;
        }

        /**
        * The nearest settlement with a stall that has an owner who is not the
        * player and coins to pay with, preferring the player's own.
        */
        private Settlement nearestMarket() {
            Settlement best = null;
            float bestScore = float.MaxValue;
            foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                Settlement settlement = (Settlement) entity;
                if (settlement.isMarkedForDeletion() || !hasBuyer(settlement)) {
                    continue;
                }
                float score = groundDistance(player.getPosition(), settlement.getPosition());
                if (settlement.getNationId() != null && settlement.getNationId() == player.getNationId()) {
                    score *= 0.5f;
                }
                if (score < bestScore) {
                    bestScore = score;
                    best = settlement;
                }
            }
            return best;
        }

        /** Whether a stall here would pay for something the player carries (Market.sellResources' own test). */
        private bool hasBuyer(Settlement settlement) {
            Market market = settlement.getMarket();
            foreach (Stall stall in market.getStalls()) {
                if (stall.getOwnerId() == null || stall.getOwnerId() == player.getId()) {
                    continue;
                }
                foreach (ItemType itemType in Enum.GetValues(typeof(ItemType))) {
                    if (!Market.isSoldInBulk(itemType) || !player.getInventory().hasItem(itemType)) {
                        continue;
                    }
                    int cost = Market.askingPrice(ItemCostCalculator.calculateCostBasedOnSupply(itemType, market), 0);
                    if (stall.getInventory().getNumItems(ItemType.COIN) >= cost) {
                        return true;
                    }
                }
            }
            return false;
        }

        /**
        * The nearest tree or rock with no settlement close enough for Interact
        * to enter it instead: the well's reach plus the bot's, so nowhere the
        * bot stops to gather is in reach of a well. At 6 units, seed 3 with
        * minute-long sieges stopped by a tree 2.6 units off and within reach
        * of a well, entered, left, and entered again for 2m 50s.
        */
        private Entity nearestClearResource(EntityType source) {
            List<Entity> blockers = simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT);
            Entity best = null;
            float bestDistance = float.MaxValue;
            foreach (Entity candidate in simulation.getEntityRepository().getEntitiesOfType(source)) {
                if (candidate.isMarkedForDeletion()) {
                    continue;
                }
                float distance = groundDistance(player.getPosition(), candidate.getPosition());
                if (distance >= bestDistance || nearAny(candidate.getPosition(), blockers, InteractCommand.Reach + ReachDistance + 0.5f)) {
                    continue;
                }
                bestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private bool nearAny(Vector3 position, List<Entity> entities, float range) {
            foreach (Entity entity in entities) {
                if (groundDistance(position, entity.getPosition()) < range) {
                    return true;
                }
            }
            return false;
        }

        /**
        * The nearest point on rings around the player that FoundSettlementCommand
        * would accept: far enough from every settlement and outside every
        * other nation's land.
        */
        private Vector3? nearestFoundingSite() {
            List<Entity> settlements = simulation.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT);
            int minDistance = simulation.getGameConfig().getMinDistanceBetweenSettlements();
            Vector3 origin = player.getPosition();
            for (int ring = 0; ring <= 40; ring++) {
                float radius = ring * 25f;
                int points = ring == 0 ? 1 : 8 + ring * 4;
                for (int i = 0; i < points; i++) {
                    float angle = 2f * MathF.PI * i / points;
                    Vector3 site = new Vector3(origin.X + radius * MathF.Sin(angle), 1f, origin.Z + radius * MathF.Cos(angle));
                    if (nearAny(site, settlements, minDistance + 1)) {
                        continue;
                    }
                    if (Territory.getFoundingObjection(simulation.getEntityRepository(), simulation.getNationRepository(),
                            site, player.getNationId()) != null) {
                        continue;
                    }
                    return site;
                }
            }
            return null;
        }
    }
}
