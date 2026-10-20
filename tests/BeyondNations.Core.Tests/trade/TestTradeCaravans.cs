using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Trade caravans (TradeCaravans): who sends them and where, what they do
    * to prices when they arrive, and what the player can do to them -- with
    * every coin in the world counted before and after, so a caravan can move
    * coins but never make them.
    */
    public class TestTradeCaravans {
        private const float Step = 1f / 50f;

        private readonly DiplomacyFixture world = new DiplomacyFixture();
        private readonly TradeCaravans trade;
        private readonly Nation traders;
        private readonly Nation buyers;
        private readonly Settlement origin;
        private readonly Settlement market;
        private readonly Stall merchantStall;
        private int tick = 1;

        public TestTradeCaravans() {
            trade = world.simulation.getTradeCaravans();
            traders = world.pawnNation("Traderland", 3);
            buyers = world.pawnNation("Buyerland", 3);
            origin = world.settlement(traders, 0, 0);
            market = world.settlement(buyers, 400, 0);
            origin.getInventory().addItem(ItemType.WOOD, 100);
            origin.addFunds(50);
            market.setResidentCount(8);
            market.getMarket().createStall();
            merchantStall = market.getMarket().getStallForSale();
            merchantStall.setOwnerId(buyers.getLeaderId());
            merchantStall.getInventory().addItem(ItemType.COIN, 1000);
            // the player starts far from the road unless a test walks them over
            world.player.setPosition(new Vector3(0, 2, 3000));
        }

        private int coinsInWorld() {
            int coins = 0;
            foreach (Entity entity in world.entities.getEntities()) {
                coins += entity.getInventory().getNumItems(ItemType.COIN);
                if (entity is ItemDrop drop && drop.getItemStack().getItemType() == ItemType.COIN) {
                    coins += drop.getItemStack().getQuantity();
                }
                if (entity is Settlement settlement) {
                    foreach (Stall stall in settlement.getMarket().getStalls()) {
                        coins += stall.getInventory().getNumItems(ItemType.COIN);
                    }
                }
            }
            return coins;
        }

        private Caravan dispatch() {
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS);
            List<Caravan> caravans = trade.getCaravans();
            Assert.Single(caravans);
            return caravans[0];
        }

        /** Ticks the trade system (not the whole world, so nothing else moves) until done or out of time. */
        private void roll(int ticks, System.Func<bool> done = null, System.Action eachTick = null) {
            for (int i = 0; i < ticks; i++) {
                tick++;
                if (tick % TradeCaravans.DISPATCH_TICKS == 0) {
                    tick++; // no fresh dispatches mid-test
                }
                trade.update(tick, Step);
                eachTick?.Invoke();
                if (done != null && done()) {
                    return;
                }
            }
        }

        private static float distance(Vector3 a, Vector3 b) {
            return Vector3.Distance(new Vector3(a.X, 0, a.Z), new Vector3(b.X, 0, b.Z));
        }

        // --- dispatch ---

        [Fact]
        public void testANationWithSurplus_SendsItToADearerMarket() {
            int coins = coinsInWorld();
            Caravan caravan = dispatch();

            Assert.Equal(traders.getId(), caravan.getNationId());
            Assert.Equal(origin.getId(), caravan.getOriginId());
            Assert.Equal(market.getId(), caravan.getDestinationId());
            Assert.Equal(TradeCaravans.MAX_CARGO, caravan.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(100 - TradeCaravans.MAX_CARGO, origin.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(TradeCaravans.PURSE_COINS, caravan.getPurse());
            Assert.Equal(coins, coinsInWorld());
            Assert.Contains("Traderland caravan", caravan.getAppearance().getLabel());
            Assert.True(caravan.getAppearance().getParts().Count >= 6, "a cart, a pack and a drover");
        }

        [Fact]
        public void testNoCaravan_GoesToANationAtWar() {
            world.relations.adjust(traders, buyers, -60);
            Assert.Equal(NationRelationState.HOSTILE, world.relations.getState(traders, buyers));
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS);
            Assert.Empty(trade.getCaravans());
            Assert.Equal(100, origin.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testNoCaravan_GoesToANationItHasDeclaredWarOn() {
            world.simulation.getRealm().declareWar(traders, buyers);
            Assert.True(trade.atWar(traders.getId(), buyers.getId()));
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS);
            Assert.Empty(trade.getCaravans());
        }

        [Fact]
        public void testATruce_IsPeace_AndTheTrucesCaravansCannotBeRobbed() {
            Nation own = world.playerNation(2);
            world.simulation.getRealm().grantTruce(own, traders);
            Caravan caravan = dispatch();
            Assert.False(trade.atWar(own.getId(), traders.getId()));
            roll(30);
            walkPlayerTo(caravan);
            Assert.True(trade.isUnderTruceWithPlayer(caravan));
            Assert.False(trade.canRaid(caravan));
            Assert.False(trade.raid(caravan));
            Assert.False(caravan.isMarkedForDeletion());
        }

        [Fact]
        public void testNoCaravan_WithoutASurplus() {
            origin.getInventory().setNumItems(ItemType.WOOD, TradeNeeds.getReserve(trade.getNeeds(), origin, ItemType.WOOD) + TradeCaravans.MIN_CARGO - 1);
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS);
            Assert.Empty(trade.getCaravans());
        }

        [Fact]
        public void testOneCaravanANation_AndACooldownBetween() {
            dispatch();
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS * 2);
            Assert.Single(trade.getCaravans());
        }

        [Fact]
        public void testThePlayersNation_NeverSendsCaravansOnItsOwn() {
            Nation own = world.playerNation(2);
            Settlement home = world.settlement(own, -400, 0);
            home.getInventory().addItem(ItemType.WOOD, 200);
            origin.getInventory().setNumItems(ItemType.WOOD, 0);
            trade.dispatchNationCaravans(TradeCaravans.DISPATCH_TICKS);
            Assert.Empty(trade.getCaravans());
        }

        // --- delivery ---

        [Fact]
        public void testACaravan_DeliversItsGoods_AndTheMarketsPriceFalls() {
            int coins = coinsInWorld();
            int priceBefore = market.getMarket().getSellPrice(ItemType.WOOD);
            Caravan caravan = dispatch();

            roll(20000, () => caravan.getState() == CaravanState.RETURNING);
            Assert.Equal(CaravanState.RETURNING, caravan.getState());
            Assert.True(distance(caravan.getPosition(), market.getPosition()) <= Caravan.ARRIVAL_DISTANCE + 1f);
            Assert.Equal(TradeCaravans.MAX_CARGO, merchantStall.getInventory().getNumItems(ItemType.WOOD));
            Assert.True(caravan.getEarnings() > 0);
            Assert.True(market.getMarket().getSellPrice(ItemType.WOOD) < priceBefore, "a delivery gluts the market");
            Assert.Equal(1000 - caravan.getEarnings(), merchantStall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(coins, coinsInWorld());
            Assert.True(world.relations.getScore(traders, buyers) > 0, "trade warms relations");

            int earnings = caravan.getEarnings();
            int fundsBefore = origin.getFunds();
            roll(20000, () => caravan.isMarkedForDeletion());
            Assert.True(caravan.isMarkedForDeletion(), "home again, the caravan is unloaded and gone");
            Assert.Equal(fundsBefore + earnings + TradeCaravans.PURSE_COINS, origin.getFunds());
            Assert.Equal(coins, coinsInWorld());
            Assert.Equal(1, trade.getNumDelivered());
        }

        [Fact]
        public void testWhenNoStallCanPay_TheTreasuryBuysForTheStockpile_AndTheRestGoesHome() {
            merchantStall.getInventory().setNumItems(ItemType.COIN, 0);
            market.addFunds(200);
            int coins = coinsInWorld();
            Caravan caravan = dispatch();
            roll(20000, () => caravan.getState() == CaravanState.RETURNING);

            Assert.True(market.getInventory().getNumItems(ItemType.WOOD) > 0);
            Assert.True(market.getFunds() < 200);
            Assert.Equal(coins, coinsInWorld());
            int unsold = caravan.getInventory().getNumItems(ItemType.WOOD);
            int woodAtHome = origin.getInventory().getNumItems(ItemType.WOOD);
            roll(20000, () => caravan.isMarkedForDeletion());
            Assert.Equal(woodAtHome + unsold, origin.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(coins, coinsInWorld());
        }

        [Fact]
        public void testWarBreakingOut_TurnsACaravanForHome() {
            Caravan caravan = dispatch();
            roll(100);
            world.relations.adjust(traders, buyers, -80);
            roll(1);
            Assert.Equal(CaravanState.RETURNING, caravan.getState());
            Assert.Equal(0, merchantStall.getInventory().getNumItems(ItemType.WOOD));
        }

        [Fact]
        public void testAnAbandonedCaravan_LeavesItsGoodsOnTheGround() {
            int coins = coinsInWorld();
            Caravan caravan = dispatch();
            roll(50);
            world.nations.removeNation(traders);
            roll(1);
            Assert.True(caravan.isMarkedForDeletion());
            Assert.NotEmpty(world.entities.getEntitiesOfType(EntityType.ITEM_DROP));
            Assert.Equal(coins, coinsInWorld());
        }

        // --- the player ---

        private void walkPlayerTo(Caravan caravan) {
            Vector3 at = caravan.getPosition();
            world.player.setPosition(new Vector3(at.X, 2, at.Z + 3));
        }

        [Fact]
        public void testRaiding_TakesTheLoot_AndCanStartAWar() {
            Nation own = world.playerNation(2);
            world.relations.adjust(own, traders, -25); // a rival already
            Caravan caravan = dispatch();
            roll(30);
            walkPlayerTo(caravan);
            Assert.Same(caravan, trade.findCaravanInReach());
            Assert.True(trade.canRaid(caravan));
            int coins = coinsInWorld();
            int relationChanges = world.simulation.getEventRepository().getNumberOfEvents(EventType.NationRelationChange);

            Assert.True(trade.raid(caravan));
            Assert.True(caravan.isMarkedForDeletion());
            Assert.Equal(TradeCaravans.MAX_CARGO, world.player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(-25 - TradeCaravans.RAID_RELATION_PENALTY, world.relations.getScore(own, traders));
            Assert.Equal(NationRelationState.HOSTILE, world.relations.getState(own, traders));
            Assert.Contains("at war", world.player.getStatus().getStatus());
            Assert.True(world.simulation.getEventRepository().getNumberOfEvents(EventType.NationRelationChange) > relationChanges);
            Assert.Equal(coins, coinsInWorld());
            Assert.Equal(1, trade.getNumRaided());

            ChronicleEntry last = world.simulation.getChronicle().getEntries()[world.simulation.getChronicle().getCount() - 1];
            bool told = false;
            foreach (ChronicleEntry entry in world.simulation.getChronicle().getEntries()) {
                told |= entry.getText().Contains("raided a Traderland caravan");
            }
            Assert.True(told, "the raid is in the chronicle; last entry: " + last.getText());
        }

        [Fact]
        public void testRaiding_WithoutANation_CostsStandingWithItsPeople() {
            Caravan caravan = dispatch();
            roll(30);
            walkPlayerTo(caravan);
            Assert.True(trade.raid(caravan));
            foreach (EntityId memberId in traders.getMemberIds()) {
                Pawn member = (Pawn) world.entities.getEntity(memberId);
                Assert.Equal(-TradeCaravans.RAID_PAWN_RELATION_PENALTY, member.getRelationships()[world.player.getId()]);
            }
        }

        [Fact]
        public void testOwnNationsCaravans_CannotBeRaided() {
            Caravan caravan = dispatch();
            traders.addMember(world.player.getId());
            world.player.setNationId(traders.getId());
            Assert.False(trade.canRaid(caravan));
            Assert.False(trade.raid(caravan));
            Assert.False(caravan.isMarkedForDeletion());
        }

        [Fact]
        public void testEscorting_PaysAShare_AndWinsGoodwill() {
            Nation own = world.playerNation(2);
            Caravan caravan = dispatch();
            roll(5);
            walkPlayerTo(caravan);
            Assert.True(trade.escort(caravan));
            Assert.False(trade.canRaid(caravan), "a guard does not rob its own charge");
            int coins = coinsInWorld();
            int playerCoins = world.player.getInventory().getNumItems(ItemType.COIN);

            roll(20000, () => caravan.getState() == CaravanState.RETURNING, () => walkPlayerTo(caravan));
            int reward = world.player.getInventory().getNumItems(ItemType.COIN) - playerCoins;
            Assert.Equal(System.Math.Max(TradeCaravans.ESCORT_MIN_REWARD, caravan.getEarnings() * TradeCaravans.ESCORT_SHARE_PERCENT / 100), reward);
            Assert.Equal(TradeCaravans.ESCORT_RELATION, world.relations.getScore(own, traders));
            Assert.Equal(coins, coinsInWorld());
            Assert.Equal(1, trade.getNumEscorted());
            Assert.Null(caravan.getEscortId());
        }

        [Fact]
        public void testAnEscortWhoWandersOff_IsNotPaid() {
            Caravan caravan = dispatch();
            roll(5);
            walkPlayerTo(caravan);
            Assert.True(trade.escort(caravan));
            int playerCoins = world.player.getInventory().getNumItems(ItemType.COIN);
            world.player.setPosition(new Vector3(0, 2, -2000));
            roll(1);
            Assert.Null(caravan.getEscortId());
            roll(20000, () => caravan.getState() == CaravanState.RETURNING);
            Assert.Equal(playerCoins, world.player.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testBuyingFromACaravan_AndSellingBack_NeverMakesMoney() {
            Caravan caravan = dispatch();
            roll(5);
            walkPlayerTo(caravan);
            world.player.getInventory().setNumItems(ItemType.COIN, 1000);
            int coins = coinsInWorld();
            for (int i = 0; i < 10; i++) {
                Assert.True(trade.buyFromCaravan(caravan, ItemType.WOOD));
                Assert.True(trade.sellToCaravan(caravan) > 0);
            }
            Assert.True(world.player.getInventory().getNumItems(ItemType.COIN) < 1000);
            Assert.Equal(coins, coinsInWorld());
        }

        [Fact]
        public void testTheCaravansAskingPrice_IsTheMarketsPrice() {
            Caravan caravan = dispatch();
            Assert.Equal(market.getMarket().getBuyPrice(ItemType.WOOD), trade.getPriceToBuyFrom(caravan, ItemType.WOOD));
            Assert.True(trade.getPriceToSellTo(caravan, ItemType.WOOD) < market.getMarket().getSellPrice(ItemType.WOOD));
        }

        // --- the player's own caravans ---

        [Fact]
        public void testThePlayer_NeedsAStorehouseToSendCaravans() {
            Nation own = world.playerNation(2);
            Settlement home = world.settlement(own, -300, 0);
            world.player.setCurrentSettlementId(home.getId());
            Assert.Contains("Storehouse", trade.getDispatchRefusal());
            home.addBuilding(BuildingType.STOREHOUSE);
            Assert.Null(trade.getDispatchRefusal());
        }

        [Fact]
        public void testThePlayersCaravan_SellsAndBringsTheTakingsHome() {
            Nation own = world.playerNation(2);
            Settlement home = world.settlement(own, -300, 0);
            home.addBuilding(BuildingType.STOREHOUSE);
            world.player.setCurrentSettlementId(home.getId());
            world.player.getInventory().addItem(ItemType.STONE, 15);
            int coins = coinsInWorld();

            Assert.Contains(market, trade.getDestinations(home, own.getId()));
            Caravan caravan = trade.dispatchPlayerCaravan(market, new Dictionary<ItemType, int> { [ItemType.STONE] = 15 }, tick);
            Assert.NotNull(caravan);
            Assert.True(caravan.isPlayerOwned());
            Assert.Equal(0, world.player.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(15, caravan.getInventory().getNumItems(ItemType.STONE));

            roll(40000, () => caravan.isMarkedForDeletion());
            Assert.True(caravan.isMarkedForDeletion());
            Assert.Equal(15, merchantStall.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(caravan.getEarnings(), home.getFunds());
            Assert.True(home.getFunds() > 0);
            Assert.Equal(coins, coinsInWorld());
            Assert.Contains("came home", world.player.getStatus().getStatus());
        }

        [Fact]
        public void testThePlayer_CannotLoadWhatTheyDoNotHave() {
            Nation own = world.playerNation(2);
            Settlement home = world.settlement(own, -300, 0);
            home.addBuilding(BuildingType.STOREHOUSE);
            world.player.setCurrentSettlementId(home.getId());
            world.player.getInventory().addItem(ItemType.STONE, 3);
            Assert.Null(trade.dispatchPlayerCaravan(market, new Dictionary<ItemType, int> { [ItemType.STONE] = 4 }, tick));
            Assert.Null(trade.dispatchPlayerCaravan(market, new Dictionary<ItemType, int> { [ItemType.COIN] = 4 }, tick));
            Assert.Null(trade.dispatchPlayerCaravan(market, new Dictionary<ItemType, int>(), tick));
            Assert.Empty(trade.getCaravans());
            Assert.Equal(3, world.player.getInventory().getNumItems(ItemType.STONE));
        }

        // --- saves ---

        /**
        * A caravan saved half way down the road, with an escort riding along
        * and prices that have drifted, loads exactly as it was and arrives on
        * the same tick with the same takings.
        */
        [Fact]
        public void testACaravanMidRoute_SavesAndLoads_AndRunsOnIdentically() {
            world.playerNation(2);
            for (int i = 0; i < 6; i++) {
                MarketPricing.runRound(world.entities);
            }
            Caravan caravan = dispatch();
            roll(400);
            walkPlayerTo(caravan);
            Assert.True(trade.escort(caravan));

            byte[] file = SaveGame.encode(world.simulation, "test");
            Simulation copy = SaveGame.decode(file);
            Assert.Null(WorldComparer.difference(world.simulation, copy));
            Caravan loaded = copy.getTradeCaravans().getCaravans()[0];
            Assert.Equal(caravan.getPosition(), loaded.getPosition());
            Assert.Equal(caravan.describeCargo(), loaded.describeCargo());
            Assert.Equal(copy.getPlayer().getId(), loaded.getEscortId());
            Assert.Equal(market.getMarket().getPrices().getPressure(ItemType.WOOD),
                ((Settlement) copy.getEntityRepository().getEntity(market.getId())).getMarket().getPrices().getPressure(ItemType.WOOD));

            for (int i = 0; i < 3000; i++) {
                world.simulation.fixedUpdate(Step);
                copy.fixedUpdate(Step);
            }
            Assert.Equal(WorldComparer.fingerprint(world.simulation), WorldComparer.fingerprint(copy));
            Assert.Equal(trade.getNumDelivered(), copy.getTradeCaravans().getNumDelivered());
        }

        [Fact]
        public void testAVersion1Save_LoadsWithNoCaravans_AndPlainPrices() {
            Simulation loaded = SaveGame.decode(System.IO.File.ReadAllBytes(SaveFixtures.pathFor(1)));
            Assert.Empty(loaded.getTradeCaravans().getCaravans());
            Assert.Equal(0, loaded.getTradeCaravans().getNumDispatched());
            foreach (Entity entity in loaded.getEntityRepository().getEntitiesOfType(EntityType.SETTLEMENT)) {
                Market loadedMarket = ((Settlement) entity).getMarket();
                foreach (ItemType itemType in MarketPricing.PricedItems) {
                    Assert.Equal(MarketPrices.DEFAULT_PRESSURE, loadedMarket.getPrices().getPressure(itemType));
                    Assert.Equal(0, MarketPricing.getTrend(loadedMarket, itemType));
                }
            }
        }

        // --- the chronicle ---

        [Fact]
        public void testOtherNationsCaravans_AreToldOnlyForABigHaul() {
            int before = world.simulation.getChronicle().getTotalRecorded();
            Caravan caravan = dispatch();
            Assert.Equal(before, world.simulation.getChronicle().getTotalRecorded()); // setting out is not news
            Assert.Equal(1, world.simulation.getEventRepository().getNumberOfEvents(EventType.Caravan));

            roll(20000, () => caravan.getState() == CaravanState.RETURNING);
            Assert.True(caravan.getEarnings() >= TradeCaravans.NOTABLE_EARNINGS);
            ChronicleEntry told = world.simulation.getChronicle().getEntries()[world.simulation.getChronicle().getCount() - 1];
            Assert.Equal(EventType.Caravan, told.getEventType());
            Assert.Contains("A caravan from Traderland sold 20 wood", told.getText());
        }
    }
}
