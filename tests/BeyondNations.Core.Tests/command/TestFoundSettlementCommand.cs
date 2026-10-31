using System.Numerics;

using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestFoundSettlementCommand {
        private readonly RandomSource random = new RandomSource(20260816);

        private NationRepository nationRepository;
        private EntityRepository entityRepository;
        private EventRepository eventRepository;
        private GameConfig gameConfig;
        private FoundSettlementCommand command;
        private Player player;

        public TestFoundSettlementCommand() {
            nationRepository = new NationRepository(random);
            entityRepository = new EntityRepository(random);
            eventRepository = new EventRepository();
            EventProducer eventProducer = new EventProducer(eventRepository);
            gameConfig = new GameConfig();
            command = new FoundSettlementCommand(nationRepository, eventProducer, entityRepository, gameConfig, random);

            player = new Player(5, 10, new TickCounter(), 500, 200, random);
        }

        private Nation makePlayerLeader() {
            Nation nation = new Nation("Testland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            return nation;
        }

        private Settlement addSettlementAt(Vector3 position) {
            Nation otherNation = new Nation("Otherland", new EntityId(), random);
            nationRepository.addNation(otherNation);
            Settlement settlement = new Settlement(position, otherNation.getId(), otherNation.getColor(), otherNation.getName(), random);
            entityRepository.addEntity(settlement);
            return settlement;
        }

        [Fact]
        public void testExecute_NotInNation_NoSettlement() {
            // prepare
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not in a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_MemberButNotLeader_NoSettlement() {
            // prepare
            Nation nation = new Nation("Testland", new EntityId(), random);
            nation.addMember(player.getId());
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("You are not the leader of a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderShortOfWood_ReportsWoodStillNeeded() {
            // prepare
            Nation nation = makePlayerLeader();
            player.getInventory().addItem(ItemType.WOOD, 40);

            // run
            command.execute(player);

            // check
            Assert.Equal(0, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(40, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal("Not enough wood. Need " + (Settlement.WOOD_COST_TO_BUILD - 40) + " more.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_TooCloseToAnotherSettlement_KeepsWood() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(Vector3.Zero);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            addSettlementAt(new Vector3(gameConfig.getMinDistanceBetweenSettlements() - 1, 0, 0));

            // run
            command.execute(player);

            // check
            Assert.Equal(1, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(0, nation.getNumberOfSettlements());
            Assert.Equal(Settlement.WOOD_COST_TO_BUILD, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Null(player.getHomeSettlementId());
            // which settlement, how far, the minimum and the way out (playtests 0.4.1: seven sessions guessed)
            Assert.Equal("Too close to the Camp of Otherland: " + (gameConfig.getMinDistanceBetweenSettlements() - 1) + " paces away, and settlements must be "
                + gameConfig.getMinDistanceBetweenSettlements() + " paces apart. Open ground lies about 10 paces to the west.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_TooClose_NamesTheNearestOfSeveral() {
            makePlayerLeader();
            player.setPosition(Vector3.Zero);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            addSettlementAt(new Vector3(0, 0, 200));
            Settlement nearest = addSettlementAt(new Vector3(0, 0, -120));

            command.execute(player);

            Assert.StartsWith("Too close to the " + nearest.getDisplayName() + ": 120 paces away", player.getStatus().getStatus());
            // open ground lies east or west, clear of both
            string status = player.getStatus().getStatus();
            Assert.True(status.EndsWith(" to the east.") || status.EndsWith(" to the west."), status);
        }

        [Fact]
        public void testNearestOpenSite_IsClearOfEverySettlement() {
            makePlayerLeader();
            addSettlementAt(new Vector3(100, 0, 0));
            addSettlementAt(new Vector3(-100, 0, 0));
            int min = gameConfig.getMinDistanceBetweenSettlements();

            Vector3? site = SettlementSites.findNearestOpenSite(entityRepository, Vector3.Zero, min, player.getNationId());

            Assert.NotNull(site);
            Assert.True(SettlementSites.isOpen(entityRepository, site.Value, min, player.getNationId()));
            Assert.Null(SettlementSites.findTooClose(entityRepository, site.Value, min));
        }

        [Fact]
        public void testCompass_NorthIsPlusZ() {
            Assert.Equal("north", Compass.describe(Vector3.Zero, new Vector3(0, 0, 10)));
            Assert.Equal("east", Compass.describe(Vector3.Zero, new Vector3(10, 5, 0)));
            Assert.Equal("south-west", Compass.describe(Vector3.Zero, new Vector3(-10, 0, -10)));
            Assert.Equal("here", Compass.describe(Vector3.Zero, Vector3.Zero));
        }

        [Fact]
        public void testCompass_Facing_NamesTheDirectionAlongTheGround() {
            // a fresh player faces +Z, north (#374)
            Assert.Equal("north", Compass.facing(player.getForward()));
            Assert.Equal("north-west", Compass.facing(new Vector3(-1, 3, 1)));
            Assert.Equal("here", Compass.facing(Vector3.UnitY));
        }

        [Fact]
        public void testNearestOpenSite_NoneWithinTheSearch_SaysSoInPaces() {
            // a settlement here with a spacing wider than the search leaves nowhere open
            addSettlementAt(Vector3.Zero);

            Assert.Equal("No open ground lies within " + (int) SettlementSites.SEARCH_RADIUS + " paces.",
                SettlementSites.describeNearestOpenSite(entityRepository, Vector3.Zero, 5000, player.getNationId()));
        }

        [Fact]
        public void testExecute_ExactlyMinDistanceAway_FoundsSettlement() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(Vector3.Zero);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            addSettlementAt(new Vector3(gameConfig.getMinDistanceBetweenSettlements(), 0, 0));

            // run
            command.execute(player);

            // check
            Assert.Equal(2, entityRepository.getNumEntitiesOfType(EntityType.SETTLEMENT));
            Assert.Equal(1, nation.getNumberOfSettlements());
            // founding this near another nation's settlement is resented
            Assert.StartsWith("Settlement founded. ", player.getStatus().getStatus());
            Assert.EndsWith(" resent a new settlement so near their land.", player.getStatus().getStatus());
        }

        [Fact]
        public void testExecute_LeaderWithEnoughWood_FoundsSettlementAndMakesItHome() {
            // prepare
            Nation nation = makePlayerLeader();
            player.setPosition(new Vector3(30, 0, -20));
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD + 15);

            // run
            command.execute(player);

            // check
            Assert.Equal(1, nation.getNumberOfSettlements());
            EntityId settlementId = nation.getSettlement(0);
            Settlement settlement = (Settlement) entityRepository.getEntity(settlementId);
            Assert.NotNull(settlement);
            Assert.Equal(nation.getId(), settlement.getNationId());
            Assert.Equal(nation.getColor(), settlement.getColor());
            Assert.Equal(new Vector3(30, 0, -20), settlement.getPosition());
            Assert.Equal(settlementId, player.getHomeSettlementId());
            Assert.Equal(15, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(1, eventRepository.getTotalNumberOfEvents());
            SettlementFoundedEvent founded = (SettlementFoundedEvent) eventRepository.getEvents(EventType.SettlementFounded)[0];
            Assert.Same(settlement, founded.getSettlement());
            Assert.Equal(player.getId(), founded.getFounderId());
            Assert.Equal("Settlement founded.", player.getStatus().getStatus());
        }
    }
}
