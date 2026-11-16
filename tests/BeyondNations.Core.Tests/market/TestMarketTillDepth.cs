using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Markets can buy a load, and a press deals what it said (0.8.0 playtest
    * s05). At the Village of Westernville wood was priced 66/63 against a
    * 100-coin till: one sale emptied it ("No till here can pay for one"), and
    * a whole visit netted +8. "Sell 6 for 178" then sold 5 for 122, because
    * the tills paid out between the label and the press.
    *
    * The fixes: a stall bids no more than a share of its till for one unit
    * (Market.getBid), a new merchant's savings go into its till with the
    * stall's price (Stall.depositSavings), merchants leave a larger float
    * (Stall.TILL_RESERVE), and the panel's presses deal exactly what they
    * were shown or nothing (TradeAtMarketCommand.sellAsQuoted). None of it
    * makes coins: they only move between purses and tills.
    */
    public class TestMarketTillDepth {
        private readonly RandomSource random = new RandomSource(462);
        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly PawnBehaviorExecutor executor;
        private readonly Nation nation;
        private readonly Settlement village;
        private readonly Market market;
        private readonly Player player;

        public TestMarketTillDepth() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            executor = new PawnBehaviorExecutor(environment, nationRepository, new EventProducer(new EventRepository()),
                entityRepository, random, new NationNameGenerator(random), new TickCounter());
            nation = new Nation("Westernville", new EntityId(), random);
            nationRepository.addNation(nation);
            village = new Settlement(new Vector3(0, 1, 0), nation.getId(), nation.getColor(), "Westernville", random);
            entityRepository.addEntity(village);
            nation.addSettlement(village.getId());
            market = village.getMarket();

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            entityRepository.addEntity(player);
            player.getInventory().clear();
            player.setCurrentSettlementId(village.getId());
        }

        private Pawn makeMember(NationRole role, int coins) {
            Pawn pawn = new Pawn(Vector3.Zero, "Mike Thompson", random);
            pawn.setEnergy(100);
            pawn.getInventory().clear();
            if (coins > 0) {
                pawn.getInventory().addItem(ItemType.COIN, coins);
            }
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            nation.setRole(pawn.getId(), role);
            pawn.setNationId(nation.getId());
            pawn.setCurrentSettlementId(village.getId());
            return pawn;
        }

        private Stall giveStall(Entity owner, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            if (coins > 0) {
                stall.getInventory().addItem(ItemType.COIN, coins);
            }
            return stall;
        }

        private int woodFloor() {
            return ItemCostCalculator.floorOf(ItemCostCalculator.getBaseCost(ItemType.WOOD));
        }

        private int coinsInPlay() {
            return player.getInventory().getNumItems(ItemType.COIN) + market.getTotalCoins();
        }

        // --- the bid follows the till ---

        [Fact]
        public void testAThinTill_BidsAShareOfItsCoins_NotTheFullCurvePrice() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 100);
            int curve = ItemCostCalculator.calculateCostAtSupply(ItemType.WOOD, market, 1);

            int bid = market.getSellPrice(ItemType.WOOD);

            Assert.True(curve > 50, "an empty market prices wood near its base: " + curve);
            Assert.Equal(System.Math.Max(woodFloor(), 100 / Market.BID_TILL_DIVISOR), bid);
            Assert.True(bid < curve);
        }

        [Fact]
        public void testADeepTill_BidsTheCurvePrice() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 5000);

            Assert.Equal(ItemCostCalculator.calculateCostAtSupply(ItemType.WOOD, market, 1), market.getSellPrice(ItemType.WOOD));
        }

        [Fact]
        public void testTheBid_NeverFallsBelowTheFloor() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 10);

            Assert.Equal(woodFloor(), market.getSellPrice(ItemType.WOOD));
            Assert.Equal(woodFloor(), market.getBid(ItemType.WOOD, 1, 0));
        }

        [Fact]
        public void testThePlaytestsMarket_NowBuysSeveralWood_WhereItBoughtOne() {
            // Westernville: one stall, a 100-coin till, wood at its base price
            giveStall(makeMember(NationRole.MERCHANT, 0), 100);
            player.getInventory().addItem(ItemType.WOOD, 56);
            int before = coinsInPlay();

            TradeReceipt receipt = new TradeAtMarketCommand(nationRepository, entityRepository).sell(player, ItemType.WOOD, 56);

            // it bought one for 63 before; now it buys at the floor until dry
            Assert.True(receipt.getQuantity() >= 4, "sold " + receipt.getQuantity());
            Assert.Equal(before, coinsInPlay());
        }

        [Fact]
        public void testANewMerchantsMarket_BuysAFullLoadOfAStaple() {
            // a settler with a typical purse buys the stall: its price and its
            // savings open the till, and the player sells into it
            market.createStall();
            Pawn settler = makeMember(NationRole.SERF, 180);
            executor.executeBehavior(settler, BehaviorType.PURCHASE_STALL);
            player.getInventory().addItem(ItemType.WOOD, 56);
            player.getInventory().addItem(ItemType.APPLE, 40);

            TradeReceipt wood = market.quoteSale(player, ItemType.WOOD, 56, 0);
            TradeReceipt apples = market.quoteSale(player, ItemType.APPLE, 40, 0);

            Assert.True(wood.getQuantity() >= 5, "wood " + wood.getQuantity());
            Assert.True(apples.getQuantity() >= 8, "apples " + apples.getQuantity());
        }

        [Fact]
        public void testBuyingAndSellingBack_NeverComesOutAhead_AtAThinTill() {
            Pawn merchant = makeMember(NationRole.MERCHANT, 0);
            Stall stall = giveStall(merchant, 150);
            stall.getInventory().addItem(ItemType.WOOD, 3);
            player.getInventory().addItem(ItemType.COIN, 500);
            int before = coinsInPlay();

            TradeReceipt bought = market.buyUnits(player, ItemType.WOOD, 1);
            TradeReceipt sold = market.sellUnits(player, ItemType.WOOD, 1, 0);

            Assert.Equal(1, bought.getQuantity());
            Assert.Equal(1, sold.getQuantity());
            Assert.True(sold.getCoins() <= bought.getCoins());
            Assert.Equal(before, coinsInPlay());
        }

        [Fact]
        public void testATradersEstimate_UsesTheSameBid() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 120);
            player.getInventory().addItem(ItemType.WOOD, 3);

            TradeReceipt quote = market.quoteSale(player, ItemType.WOOD, 3, 0);

            Assert.Equal(quote.getCoins(), TradeCaravans.estimateSale(market, ItemType.WOOD, 3));
        }

        // --- the merchant's capital ---

        [Fact]
        public void testANewMerchant_PutsItsSavingsInTheTill_KeepingAPurse() {
            market.createStall();
            Pawn settler = makeMember(NationRole.SERF, 180);

            executor.executeBehavior(settler, BehaviorType.PURCHASE_STALL);

            Stall stall = market.getStall(settler.getId());
            Assert.NotNull(stall);
            Assert.Equal(180 - Stall.MERCHANT_PURSE, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(Stall.MERCHANT_PURSE, settler.getInventory().getNumItems(ItemType.COIN));
        }

        [Fact]
        public void testAMerchant_CollectsOnlyOnceTheTillPassesItsFloat() {
            Pawn merchant = makeMember(NationRole.MERCHANT, 0);
            Stall stall = giveStall(merchant, Stall.TILL_RESERVE + 50);

            executor.executeBehavior(merchant, BehaviorType.COLLECT_PROFIT_FROM_STALL);

            Assert.Equal(Stall.TILL_RESERVE, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(50, merchant.getInventory().getNumItems(ItemType.COIN));
        }

        // --- a press deals what it showed, or nothing ---

        [Fact]
        public void testAPress_SellsExactlyWhatItsQuoteSaid() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 1000);
            player.getInventory().addItem(ItemType.APPLE, 22);
            TradeReceipt shown = market.quoteSale(player, ItemType.APPLE, 6, 0);

            TradeReceipt receipt = new TradeAtMarketCommand(nationRepository, entityRepository).sellAsQuoted(player, shown);

            Assert.Equal(6, receipt.getQuantity());
            Assert.Equal(shown.getCoins(), receipt.getCoins());
            Assert.Equal(16, player.getInventory().getNumItems(ItemType.APPLE));
        }

        [Fact]
        public void testAPress_WhoseQuoteNoLongerHolds_SellsNothing_AndSaysWhatItWouldBeNow() {
            // the playtest's "Sell 6 for 178" that sold 5 for 122: the till
            // paid someone else out between the label and the press
            Pawn merchant = makeMember(NationRole.MERCHANT, 0);
            Stall stall = giveStall(merchant, 300);
            player.getInventory().addItem(ItemType.APPLE, 22);
            TradeReceipt shown = market.quoteSale(player, ItemType.APPLE, 6, 0);
            Assert.Equal(6, shown.getQuantity());
            stall.getInventory().removeItem(ItemType.COIN, 220);
            int coins = player.getInventory().getNumItems(ItemType.COIN);

            TradeReceipt receipt = new TradeAtMarketCommand(nationRepository, entityRepository).sellAsQuoted(player, shown);

            Assert.True(receipt.isEmpty());
            Assert.Equal(22, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(coins, player.getInventory().getNumItems(ItemType.COIN));
            string status = player.getStatus().getLatest();
            Assert.StartsWith("The market moved before your press: selling would now be ", status);
            Assert.Contains("not 6 for " + shown.getCoins(), status);
            Assert.Contains("Nothing was sold; press Sell again", status);

            // the second press, of the new quote, takes it
            TradeReceipt now = market.quoteSale(player, ItemType.APPLE, 6, 0);
            TradeReceipt second = new TradeAtMarketCommand(nationRepository, entityRepository).sellAsQuoted(player, now);
            Assert.Equal(now.getQuantity(), second.getQuantity());
            Assert.Equal(now.getCoins(), second.getCoins());
        }

        [Fact]
        public void testABuyPress_WhosePriceMoved_BuysNothing() {
            Pawn merchant = makeMember(NationRole.MERCHANT, 0);
            Stall stall = giveStall(merchant, 0);
            stall.getInventory().addItem(ItemType.APPLE, 10);
            player.getInventory().addItem(ItemType.COIN, 1000);
            TradeReceipt shown = market.quotePurchase(player, ItemType.APPLE, 3);
            // someone else buys first, so the price climbs
            stall.getInventory().removeItem(ItemType.APPLE, 4);

            TradeReceipt receipt = new TradeAtMarketCommand(nationRepository, entityRepository).buyAsQuoted(player, shown);

            Assert.True(receipt.isEmpty());
            Assert.Equal(1000, player.getInventory().getNumItems(ItemType.COIN));
            Assert.Contains("Nothing was bought; press Buy again", player.getStatus().getLatest());
        }

        // --- the list keeps its order ---

        [Fact]
        public void testKeepOrder_AddsNewcomersAtTheEnd_AndTakesNothingAway() {
            List<ItemType> shown = new List<ItemType> { ItemType.APPLE, ItemType.WOOD, ItemType.STONE, ItemType.SAPLING };
            // bricks, axes and pickaxes come up, and the stone is sold off
            List<ItemType> current = new List<ItemType> {
                ItemType.APPLE, ItemType.WOOD, ItemType.BRICKS, ItemType.AXE, ItemType.PICKAXE, ItemType.SAPLING,
            };

            List<ItemType> kept = MarketTrade.keepOrder(shown, current);

            Assert.Equal(new List<ItemType> {
                ItemType.APPLE, ItemType.WOOD, ItemType.STONE, ItemType.SAPLING, ItemType.BRICKS, ItemType.AXE, ItemType.PICKAXE,
            }, kept);
        }

        [Fact]
        public void testMatchesQuote_NeedsTheSameNumberAndCoins() {
            giveStall(makeMember(NationRole.MERCHANT, 0), 1000);
            player.getInventory().addItem(ItemType.APPLE, 10);
            TradeReceipt a = market.quoteSale(player, ItemType.APPLE, 3, 0);
            TradeReceipt b = market.quoteSale(player, ItemType.APPLE, 3, 0);
            TradeReceipt c = market.quoteSale(player, ItemType.APPLE, 2, 0);

            Assert.True(MarketTrade.matchesQuote(a, b));
            Assert.False(MarketTrade.matchesQuote(a, c));
            Assert.False(MarketTrade.matchesQuote(a, market.quotePurchase(player, ItemType.APPLE, 3)));
            Assert.False(MarketTrade.matchesQuote(null, b));
        }
    }
}
