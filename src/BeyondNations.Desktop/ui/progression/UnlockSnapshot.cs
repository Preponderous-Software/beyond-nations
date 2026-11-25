using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui.progression {

    /**
    * What HudUnlocks' rules read about the world: a plain copy of the few
    * facts they need, taken with read-only calls (of), so the rules are pure
    * functions that tests can feed by hand and reading them can never step
    * or change the simulation.
    *
    * Built from Core alone, so the pacing harness compiles this file too
    * (tools/BeyondNations.Pacing) and measures the unlocks under its bot.
    */
    public sealed class UnlockSnapshot {
        /** How close a settlement has to be to enter it (WorldCommandBar.EnterSettlementDistance). */
        public const float EnterSettlementDistance = 50f;

        public int wood;
        public int stone;
        public int coins;
        /** Coins plus the treasuries of the nation's settlements. */
        public int funds;
        public int mealsEaten;
        public int marketSales;
        public bool hasSapling;
        public bool hasTool;
        public bool canCraftTool;

        public bool inNation;
        public bool leads;
        public int members;
        public int settlements;
        public bool hasHome;
        public bool inSettlement;
        public bool inOwnSettlement;
        /** In one of the nation's settlements that the player may build in (SettlementOwnership.mayManage). */
        public bool managesHere;
        public bool nearSettlement;
        /** Standing in a settlement whose market has a stall. */
        public bool marketHere;
        public int nationStalls;
        public int nationBuildings;
        /** Some settlement of the nation holds coins or a stockpile to collect. */
        public bool treasuryToCollect;
        public int hungryHere;

        public Era era = Era.CAMP;
        public Season season = Season.SPRING;

        public bool caravanInReach;
        public int playerCaravans;
        /** A leader standing on another nation's land. */
        public bool onForeignClaim;
        /** The player's nation is at war with, or a rival of, some other nation. */
        public bool hostileNeighbour;

        /** The ids of the objectives the player has been pointed to: every one completed, and the current one. */
        public HashSet<string> objectivesReached = new HashSet<string>();

        public static UnlockSnapshot of(Simulation simulation) {
            UnlockSnapshot s = new UnlockSnapshot();
            Player player = simulation.getPlayer();
            Inventory inventory = player.getInventory();
            EntityRepository entities = simulation.getEntityRepository();
            NationRepository nations = simulation.getNationRepository();

            s.wood = inventory.getNumItems(ItemType.WOOD);
            s.stone = inventory.getNumItems(ItemType.STONE);
            s.coins = inventory.getNumItems(ItemType.COIN);
            s.funds = s.coins;
            s.mealsEaten = player.getMealsEaten();
            s.marketSales = player.getMarketSales();
            s.hasSapling = inventory.getNumItems(ItemType.SAPLING) > 0;
            s.hasTool = inventory.getNumItems(ItemType.AXE) > 0 || inventory.getNumItems(ItemType.PICKAXE) > 0;

            s.inSettlement = player.isCurrentlyInSettlement();
            Settlement here = s.inSettlement ? entities.getEntity(player.getCurrentSettlementId()) as Settlement : null;
            s.canCraftTool = Crafting.canCraft(RecipeBook.Axe, inventory, here) || Crafting.canCraft(RecipeBook.Pickaxe, inventory, here);
            s.marketHere = here != null && here.getMarket() != null && here.getMarket().getNumStalls() > 0;
            s.hasHome = player.getHomeSettlementId() != null;

            Nation nation = player.getNationId() == null ? null : nations.getNation(player.getNationId());
            if (nation != null) {
                s.inNation = true;
                s.leads = nation.getLeaderId() == player.getId();
                s.members = nation.getNumberOfMembers();
                s.settlements = nation.getNumberOfSettlements();
                foreach (EntityId settlementId in nation.getSettlements()) {
                    if (entities.getEntity(settlementId) is Settlement settlement) {
                        s.funds += settlement.getFunds();
                        s.nationBuildings += settlement.getNumBuildings();
                        if (settlement.getMarket() != null) {
                            s.nationStalls += settlement.getMarket().getNumStalls();
                        }
                        if (settlement.getFunds() > 0 || CollectSettlementStockpileCommand.getStockpileSize(settlement) > 0) {
                            s.treasuryToCollect = true;
                        }
                    }
                }
                if (here != null && nation.getId().Equals(here.getNationId())) {
                    s.inOwnSettlement = true;
                    s.managesHere = SettlementOwnership.mayManage(player.getId(), nation, here);
                    s.hungryHere = new FeedResidentsCommand(entities).countHungryHere(player);
                }
                foreach (Nation other in nations.getNations()) {
                    if (other == nation) {
                        continue;
                    }
                    NationRelationState state = nations.getRelations().getState(nation, other);
                    if (state == NationRelationState.HOSTILE || state == NationRelationState.RIVAL
                            || (simulation.getRealm() != null && simulation.getRealm().isAtWar(nation, other))) {
                        s.hostileNeighbour = true;
                        break;
                    }
                }
                if (s.leads && !s.inSettlement) {
                    Settlement claim = Territory.findClaimAt(entities, player.getPosition());
                    s.onForeignClaim = claim != null && claim.getNationId() != null && !claim.getNationId().Equals(nation.getId());
                }
            }

            if (!s.inSettlement) {
                Vector3 position = player.getPosition();
                foreach (Entity entity in entities.getEntitiesOfType(EntityType.SETTLEMENT)) {
                    if (entity is Settlement settlement && !settlement.isMarkedForDeletion()
                            && Vector3.Distance(position, settlement.getPosition()) < EnterSettlementDistance) {
                        s.nearSettlement = true;
                        break;
                    }
                }
            }

            s.era = simulation.getEraTracker().getEra();
            if (simulation.getSeasonCalendar() != null) {
                s.season = simulation.getSeasonCalendar().getSeason();
            }
            TradeCaravans trade = simulation.getTradeCaravans();
            if (trade != null) {
                s.playerCaravans = trade.getNumPlayerCaravans();
                s.caravanInReach = trade.findCaravanInReach() != null;
            }

            ObjectiveTracker tracker = simulation.getObjectiveTracker();
            IReadOnlyList<Objective> objectives = tracker.getObjectives();
            int reached = System.Math.Min(tracker.getCompletedCount(), objectives.Count - 1);
            for (int i = 0; i <= reached; i++) {
                s.objectivesReached.Add(objectives[i].getId());
            }
            return s;
        }
    }
}
