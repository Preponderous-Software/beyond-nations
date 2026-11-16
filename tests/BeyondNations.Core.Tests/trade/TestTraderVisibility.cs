using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * #487: the trader role was invisible. In the 0.8.0 re-run playtest (s05)
    * a member became Trader with no word on screen, the Appoint Trader
    * button vanished once the one slot was taken without saying why, and no
    * line ever told of a trader buying or selling.
    *
    * The player leads Playerland, which holds Lowford (stalls full of cheap
    * wood). Highford, 400 away, belongs to Highland and pays well for wood.
    */
    public class TestTraderVisibility {
        private readonly RandomSource random = new RandomSource(487);

        private readonly EntityRepository entityRepository;
        private readonly NationRepository nationRepository;
        private readonly Environment environment;
        private readonly EventRepository eventRepository;
        private readonly EventProducer eventProducer;
        private readonly PawnBehaviorExecutor executor;
        private readonly Chronicle chronicle;
        private readonly Player player;
        private readonly Nation playerNation;
        private readonly Nation highland;
        private readonly Settlement lowford;
        private readonly Settlement highford;

        public TestTraderVisibility() {
            entityRepository = new EntityRepository(random);
            nationRepository = new NationRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            eventRepository = new EventRepository();
            eventProducer = new EventProducer(eventRepository);
            executor = new PawnBehaviorExecutor(environment, nationRepository, eventProducer,
                entityRepository, random, new NationNameGenerator(random), new TickCounter());
            chronicle = new Chronicle(eventRepository, entityRepository, nationRepository, null, new TickCounter(), 487);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.getInventory().clear();
            entityRepository.addEntity(player);
            playerNation = new Nation("Playerland", player.getId(), random);
            nationRepository.addNation(playerNation);
            player.setNationId(playerNation.getId());

            highland = new Nation("Highland", new EntityId(), random);
            nationRepository.addNation(highland);

            lowford = makeSettlement(playerNation, new Vector3(0, 1, 0), "Lowford");
            highford = makeSettlement(highland, new Vector3(400, 1, 0), "Highford");

            Pawn lowMerchant = makeMember(playerNation, NationRole.MERCHANT, lowford, 0, "Lowe");
            giveStall(lowford.getMarket(), lowMerchant, Stall.TILL_RESERVE).getInventory().addItem(ItemType.WOOD, 60);
            Pawn highMerchant = makeMember(highland, NationRole.MERCHANT, highford, 0, "Hale");
            giveStall(highford.getMarket(), highMerchant, Stall.TILL_RESERVE + 400);
        }

        private Settlement makeSettlement(Nation nation, Vector3 at, string name) {
            Settlement settlement = new Settlement(at, nation.getId(), nation.getColor(), name, random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            return settlement;
        }

        private Pawn makeMember(Nation nation, NationRole role, Settlement home, int coins, string name) {
            Pawn pawn = new Pawn(home.getPosition(), name, random);
            pawn.setEnergy(100);
            pawn.getInventory().clear();
            if (coins > 0) {
                pawn.getInventory().addItem(ItemType.COIN, coins);
            }
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            nation.setRole(pawn.getId(), role);
            pawn.setNationId(nation.getId());
            pawn.setHomeSettlementId(home.getId());
            pawn.setCurrentSettlementId(home.getId());
            return pawn;
        }

        private static Stall giveStall(Market market, Entity owner, int coins) {
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(owner.getId());
            stall.getInventory().addItem(ItemType.COIN, coins);
            return stall;
        }

        private List<string> chronicleLines() {
            return chronicle.getEntries().Select(e => e.getText()).ToList();
        }

        // --- the Appoint Trader button and its reason ---

        [Fact]
        public void testASerfCanBeAppointed_WhenASlotIsFreeAndTheStakeIsInHand() {
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);
            Assert.Null(LeaderPawnCommands.whyNotAppointTrader(player, serf, nationRepository, entityRepository));
        }

        [Fact]
        public void testWithoutTheStake_TheReasonSaysWhatItCosts() {
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            Assert.Equal("A trader needs a stake of " + Traders.TRADER_STAKE + " coins from you.",
                LeaderPawnCommands.whyNotAppointTrader(player, serf, nationRepository, entityRepository));
        }

        [Fact]
        public void testWithTheOneSlotTaken_TheReasonNamesWhoHoldsIt() {
            // the s05 case: Charlotte Evans took the only slot on her own
            makeMember(playerNation, NationRole.TRADER, lowford, 40, "Charlotte Evans");
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);

            string reason = LeaderPawnCommands.whyNotAppointTrader(player, serf, nationRepository, entityRepository);

            Assert.Equal("One trader per settlement: Charlotte Evans (" + lowford.getDisplayName() + ") already trades for Playerland."
                + " Found another settlement to take on another trader.", reason);
            Assert.False(LeaderPawnCommands.canAppointTrader(player, serf, nationRepository));
        }

        [Fact]
        public void testTheCommand_RefusesAFullSlotWithTheSameReason() {
            makeMember(playerNation, NationRole.TRADER, lowford, 40, "Charlotte Evans");
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);

            Assert.False(new AppointTraderCommand(nationRepository, eventProducer, entityRepository).execute(player, serf));
            Assert.Equal(LeaderPawnCommands.whyNotAppointTrader(player, serf, nationRepository, entityRepository),
                player.getStatus().getLatest());
        }

        [Fact]
        public void testTheTraderAndAMerchant_AreToldWhyNot() {
            Pawn trader = makeMember(playerNation, NationRole.TRADER, lowford, 40, "Charlotte Evans");
            Pawn merchant = makeMember(playerNation, NationRole.MERCHANT, lowford, 0, "Mae");
            Assert.Equal("Charlotte Evans is already your trader.",
                LeaderPawnCommands.whyNotAppointTrader(player, trader, nationRepository, entityRepository));
            Assert.Equal("Mae is already a merchant.",
                LeaderPawnCommands.whyNotAppointTrader(player, merchant, nationRepository, entityRepository));
        }

        [Fact]
        public void testAHomelessSerf_IsToldWhyNot() {
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            serf.setHomeSettlementId(null);
            Assert.Equal("Ada has no home settlement to trade from.",
                LeaderPawnCommands.whyNotAppointTrader(player, serf, nationRepository, entityRepository));
        }

        // --- the chronicle tells what the player's traders do ---

        [Fact]
        public void testAppointing_IsChronicled() {
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 0, "Ada");
            player.getInventory().addItem(ItemType.COIN, Traders.TRADER_STAKE);

            Assert.True(new AppointTraderCommand(nationRepository, eventProducer, entityRepository).execute(player, serf));

            Assert.Contains("You made Ada a trader for Playerland, with a stake of " + Traders.TRADER_STAKE + " coins.", chronicleLines());
        }

        [Fact]
        public void testASerfTakingUpTrade_ThenBuyingAndSelling_IsChronicled() {
            Pawn serf = makeMember(playerNation, NationRole.SERF, lowford, 200, "Charlotte Evans");

            executor.executeBehavior(serf, BehaviorType.BUY_TRADE_GOODS);

            Assert.Equal(NationRole.TRADER, playerNation.getRole(serf.getId()));
            List<string> lines = chronicleLines();
            Assert.Contains("Charlotte Evans took up trade for Playerland at the " + lowford.getDisplayName() + " (one trader per settlement).", lines);
            int wood = serf.getInventory().getNumItems(ItemType.WOOD);
            Assert.True(wood > 0);
            Assert.Contains(lines, line => line.StartsWith("Playerland's trader Charlotte Evans bought " + wood + " wood at the "
                + lowford.getDisplayName() + " for ") && line.EndsWith(", to sell at the " + highford.getDisplayName() + "."));

            // the Members tab says where the load is going
            serf.setCurrentSettlementId(null);
            Assert.Equal("Trading: carrying " + wood + " wood to the " + highford.getDisplayName() + ".",
                Traders.describeActivity(entityRepository, nationRepository, serf));

            serf.setCurrentSettlementId(highford.getId());
            int coinsBefore = serf.getInventory().getNumItems(ItemType.COIN);
            executor.executeBehavior(serf, BehaviorType.SELL_RESOURCES);
            int takings = serf.getInventory().getNumItems(ItemType.COIN) - coinsBefore;

            Assert.True(takings > 0);
            Assert.Contains("Playerland's trader Charlotte Evans sold " + wood + " wood at the " + highford.getDisplayName()
                + " for " + takings + " coins.", chronicleLines());
        }

        [Fact]
        public void testAnotherNationsTrader_IsNotChronicled() {
            Settlement highlandOther = makeSettlement(highland, new Vector3(0, 1, 300), "Hillcrest");
            Pawn lowMerchant = makeMember(highland, NationRole.MERCHANT, highlandOther, 0, "Hob");
            giveStall(highlandOther.getMarket(), lowMerchant, Stall.TILL_RESERVE).getInventory().addItem(ItemType.WOOD, 60);
            Pawn serf = makeMember(highland, NationRole.SERF, highlandOther, 200, "Ivo");

            executor.executeBehavior(serf, BehaviorType.BUY_TRADE_GOODS);

            Assert.Equal(NationRole.TRADER, highland.getRole(serf.getId()));
            Assert.Empty(chronicleLines());
            Assert.True(eventRepository.getNumberOfEvents(EventType.Trader) >= 2);
        }

        [Fact]
        public void testAnIdleTrader_SaysWhyItWaits() {
            Pawn trader = makeMember(playerNation, NationRole.TRADER, highford, 3, "Charlotte Evans");
            // Highford's stalls hold no goods to carry anywhere
            Assert.Equal("Trading: 3 coins in hand, waiting for a run worth the walk (at least " + Traders.MIN_PROFIT
                + " coins of profit within reach).", Traders.describeActivity(entityRepository, nationRepository, trader));
        }
    }
}
