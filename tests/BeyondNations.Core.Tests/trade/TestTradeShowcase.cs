using System;
using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestTradeShowcase {

        private static Simulation staged(out TradeShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = TradeShowcase.stage(simulation);
            return simulation;
        }

        private static float groundDistance(Vector3 a, Vector3 b) {
            float dx = a.X - b.X;
            float dz = a.Z - b.Z;
            return MathF.Sqrt(dx * dx + dz * dz);
        }

        [Fact]
        public void testStage_PlayerLeadsANationHomedInAVillageWithAStorehouse() {
            TradeShowcase.Staged result;
            Simulation simulation = staged(out result);
            Player player = simulation.getPlayer();

            Assert.Equal(player.getId(), result.playerNation.getLeaderId());
            Assert.Equal(result.playerNation.getId(), player.getNationId());
            Assert.Equal(result.playerSettlement.getId(), player.getHomeSettlementId());
            Assert.Equal(result.playerNation.getId(), result.playerSettlement.getNationId());
            Assert.Equal(3, result.playerSettlement.getNumBuildings());
            Assert.Equal(1, result.playerSettlement.getNumBuildings(BuildingType.STOREHOUSE));
            // the player and their pawns all call it home
            Assert.Equal(TradeShowcase.PlayerPawns + 1, result.playerNation.getMemberIds().Count);
            Assert.Equal(TradeShowcase.PlayerPawns + 1, result.playerSettlement.getResidentCount());
            // the player has a nation, a Storehouse, and no caravans out, so may send one
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, result.playerSettlement);
            Assert.Null(simulation.getTradeCaravans().getDispatchRefusal());
        }

        [Fact]
        public void testStage_TheTradersArePawnLedWithAMerchantAndWoodToSpare() {
            TradeShowcase.Staged result;
            Simulation simulation = staged(out result);
            EntityId leaderId = result.traders.getLeaderId();
            Pawn leader = simulation.getEntityRepository().getEntity(leaderId) as Pawn;

            Assert.NotNull(leader);
            Assert.Equal(result.traders.getId(), leader.getNationId());
            Assert.Equal(NationRole.LEADER, result.traders.getRole(leaderId));
            Assert.Equal(TradeShowcase.TraderMembers, result.traders.getMemberIds().Count);
            Assert.Equal(TradeShowcase.TraderMembers, result.traderSettlement.getResidentCount());
            Assert.Equal(result.traders.getId(), result.traderSettlement.getNationId());
            Assert.Equal(1, result.traderSettlement.getNumBuildings(BuildingType.STOREHOUSE));

            Stall merchant = null;
            foreach (Stall stall in result.traderSettlement.getMarket().getStalls()) {
                if (leaderId.Equals(stall.getOwnerId())) {
                    merchant = stall;
                }
            }
            Assert.NotNull(merchant);
            Assert.Equal(400, merchant.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(30, merchant.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testStage_TheVillagesFlankTheRoadAheadOnFriendlyTerms() {
            TradeShowcase.Staged result;
            Simulation simulation = staged(out result);
            Vector3 start = simulation.getPlayer().getPosition();
            float flank = MathF.Sqrt(TradeShowcase.HalfGap * TradeShowcase.HalfGap + TradeShowcase.AheadDistance * TradeShowcase.AheadDistance);

            Assert.Equal(2f * TradeShowcase.HalfGap, groundDistance(result.playerSettlement.getPosition(), result.traderSettlement.getPosition()), 2);
            Assert.Equal(flank, groundDistance(start, result.playerSettlement.getPosition()), 2);
            Assert.Equal(flank, groundDistance(start, result.traderSettlement.getPosition()), 2);
            Assert.Equal(25, simulation.getNationRepository().getRelations().getScore(result.playerNation, result.traders));
        }

        [Fact]
        public void testStage_ATraderCaravanIsOutboundToThePlayerSevenPacesAhead() {
            TradeShowcase.Staged result;
            Simulation simulation = staged(out result);
            Caravan caravan = result.caravan;

            Assert.Equal(result.traders.getId(), caravan.getNationId());
            Assert.Equal(result.traderSettlement.getId(), caravan.getOriginId());
            Assert.Equal(result.playerSettlement.getId(), caravan.getDestinationId());
            Assert.Equal(CaravanState.OUTBOUND, caravan.getState());
            Assert.False(caravan.isPlayerOwned());
            Assert.Equal(7f, groundDistance(simulation.getPlayer().getPosition(), caravan.getPosition()), 2);
            Assert.InRange(caravan.getCargoCount(), 1, TradeCaravans.MAX_CARGO);
            Assert.Equal(TradeCaravans.PURSE_COINS, caravan.getPurse());
            Assert.Single(simulation.getTradeCaravans().getCaravans());
            Assert.Contains(result.traders.getName(), simulation.getPlayer().getStatus().getLatest());
        }

        [Fact]
        public void testStage_GivesThePlayerGoodsAndCoinsToTradeWith() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Inventory inventory = simulation.getPlayer().getInventory();
            int wood = inventory.getNumItems(ItemType.WOOD);
            int stone = inventory.getNumItems(ItemType.STONE);
            int apples = inventory.getNumItems(ItemType.APPLE);
            int coins = inventory.getNumItems(ItemType.COIN);

            TradeShowcase.stage(simulation);

            Assert.Equal(wood + 20, inventory.getNumItems(ItemType.WOOD));
            Assert.Equal(stone + 10, inventory.getNumItems(ItemType.STONE));
            Assert.Equal(apples + 6, inventory.getNumItems(ItemType.APPLE));
            Assert.Equal(coins + 200, inventory.getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testStage_IsDeterministicForAWorldSeed() {
            TradeShowcase.Staged first;
            TradeShowcase.Staged second;
            staged(out first);
            staged(out second);

            Assert.Equal(first.playerNation.getName(), second.playerNation.getName());
            Assert.Equal(first.traders.getName(), second.traders.getName());
            Assert.Equal(first.caravan.describeCargo(), second.caravan.describeCargo());
            Assert.Equal(first.caravan.getPosition(), second.caravan.getPosition());
        }
    }
}
