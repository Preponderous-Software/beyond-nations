using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Pawn traits: rolled from the seed, inherited, shown, and -- each one --
    * changing what a pawn does.
    */
    public class TestPawnTraits {
        private readonly RandomSource random = new RandomSource(20261018);

        private readonly EntityRepository entityRepository;
        private readonly Environment environment;
        private readonly NationRepository nationRepository;
        private readonly EventProducer eventProducer;
        private readonly Player player;

        public TestPawnTraits() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            nationRepository = new NationRepository(random);
            eventProducer = new EventProducer(new EventRepository());
            player = new Player(5, 10, new TickCounter(), 500, 200, random);
            player.setPosition(Vector3.Zero);
            entityRepository.addEntity(player);
        }

        private Pawn makePawn(params PawnTrait[] traits) {
            Pawn pawn = new Pawn(Vector3.Zero, "Ada Smith", random);
            pawn.setTraits(traits);
            entityRepository.addEntity(pawn);
            return pawn;
        }

        // --- rolling and inheriting ------------------------------------------

        [Fact]
        public void testRoll_OneOrTwoDistinctTraits_AndEveryTraitTurnsUp() {
            RandomSource traitRandom = new RandomSource(42);
            HashSet<PawnTrait> seen = new HashSet<PawnTrait>();
            int withTwo = 0;
            for (int i = 0; i < 1000; i++) {
                List<PawnTrait> traits = PawnTraits.roll(traitRandom);
                Assert.InRange(traits.Count, 1, 2);
                Assert.Equal(traits.Count, traits.Distinct().Count());
                seen.UnionWith(traits);
                if (traits.Count == 2) {
                    withTwo++;
                }
            }
            Assert.Equal(PawnTraits.All.Length, seen.Count);
            Assert.InRange(withTwo, 300, 500);
        }

        [Fact]
        public void testRoll_SameSeedGivesTheSameCharacters() {
            RandomSource first = new RandomSource(7);
            RandomSource second = new RandomSource(7);
            for (int i = 0; i < 50; i++) {
                Assert.Equal(PawnTraits.roll(first), PawnTraits.roll(second));
            }
        }

        [Fact]
        public void testWorldGeneratedPawns_HaveTraits_FromTheSeed() {
            string describeWorld(int seed) {
                GameConfig config = new GameConfig();
                config.setWorldSeed(seed);
                Simulation simulation = new Simulation(config);
                simulation.fixedUpdate(1f / 50f);
                List<string> characters = new List<string>();
                foreach (Entity entity in simulation.getEntityRepository().getEntitiesOfType(EntityType.PAWN)) {
                    Pawn pawn = (Pawn) entity;
                    Assert.NotEmpty(pawn.getTraits());
                    characters.Add(pawn.getName() + ": " + PawnTraits.describe(pawn));
                }
                return string.Join("\n", characters);
            }

            string world = describeWorld(42);
            Assert.NotEmpty(world);
            Assert.Equal(world, describeWorld(42));
        }

        [Fact]
        public void testInherit_TakesAParentsTraitMostOfTheTime() {
            Pawn mother = makePawn(PawnTrait.INDUSTRIOUS, PawnTrait.GREEDY);
            Pawn father = makePawn(PawnTrait.LOYAL);
            RandomSource inheritRandom = new RandomSource(3);
            int industrious = 0;
            int loyal = 0;
            for (int i = 0; i < 500; i++) {
                List<PawnTrait> traits = PawnTraits.inherit(mother, father, inheritRandom);
                Assert.InRange(traits.Count, 1, 2);
                if (traits.Contains(PawnTrait.INDUSTRIOUS)) industrious++;
                if (traits.Contains(PawnTrait.LOYAL)) loyal++;
            }
            Assert.InRange(industrious, 370, 480);
            Assert.InRange(loyal, 370, 480);
        }

        // --- effects ----------------------------------------------------------

        private int gatherWoodFromTree(Pawn pawn) {
            PawnBehaviorExecutor executor = new PawnBehaviorExecutor(environment, nationRepository, eventProducer, entityRepository, random, new NationNameGenerator(random), new TickCounter());
            AppleTree tree = new AppleTree(new Vector3(1, 0, 0), 5, random);
            tree.getInventory().setNumItems(ItemType.WOOD, 6);
            entityRepository.addEntity(tree);
            pawn.setTargetEntity(tree);
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);
            entityRepository.removeEntity(tree);
            return pawn.getInventory().getNumItems(ItemType.WOOD);
        }

        [Fact]
        public void testIndustrious_TakesHalfAgainFromATree() {
            Assert.Equal(6, gatherWoodFromTree(makePawn()));
            Assert.Equal(9, gatherWoodFromTree(makePawn(PawnTrait.INDUSTRIOUS)));
        }

        [Fact]
        public void testIndustrious_StacksWithAnAxe() {
            Pawn pawn = makePawn(PawnTrait.INDUSTRIOUS);
            pawn.getInventory().addItem(ItemType.AXE, 1);
            Assert.Equal(6 + Tools.AXE_BONUS_WOOD + 3, gatherWoodFromTree(pawn));
        }

        [Fact]
        public void testIndustrious_TakesHalfAgainFromARock() {
            Pawn pawn = makePawn(PawnTrait.INDUSTRIOUS);
            Rock rock = new Rock(new Vector3(1, 0, 0));
            int stone = rock.getInventory().getNumItems(ItemType.STONE);
            Assert.True(stone > 0);
            entityRepository.addEntity(rock);
            pawn.setTargetEntity(rock);
            new PawnBehaviorExecutor(environment, nationRepository, eventProducer, entityRepository, random, new NationNameGenerator(random), new TickCounter())
                .executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);
            Assert.Equal(stone + PawnTraits.gatherBonus(pawn, stone), pawn.getInventory().getNumItems(ItemType.STONE));
            Assert.True(pawn.getInventory().getNumItems(ItemType.STONE) > stone);
        }

        private int coinsFromSellingWood(Pawn seller) {
            Market market = new Market(4, random);
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(new EntityId());
            stall.getInventory().addItem(ItemType.COIN, 1000);
            seller.getInventory().setNumItems(ItemType.COIN, 0);
            seller.getInventory().addItem(ItemType.WOOD, 1);
            Assert.NotNull(market.sellResources(seller, PawnTraits.sellPriceBonusPercent(seller)));
            return seller.getInventory().getNumItems(ItemType.COIN);
        }

        [Fact]
        public void testGreedy_SellsDearer() {
            int plain = coinsFromSellingWood(makePawn());
            int greedy = coinsFromSellingWood(makePawn(PawnTrait.GREEDY));
            Assert.Equal(Market.askingPrice(plain, PawnTraits.GREEDY_PRICE_BONUS_PERCENT), greedy);
            Assert.True(greedy > plain);
        }

        [Fact]
        public void testGreedy_WillNotSellToAStallThatCannotPayItsPrice() {
            Market market = new Market(4, random);
            market.createStall();
            Stall stall = market.getStallForSale();
            stall.setOwnerId(new EntityId());
            Pawn greedy = makePawn(PawnTrait.GREEDY);
            greedy.getInventory().addItem(ItemType.WOOD, 1);
            int price = ItemCostCalculator.calculateCostBasedOnSupply(ItemType.WOOD, market);
            stall.getInventory().addItem(ItemType.COIN, price);

            Assert.Null(market.sellResources(greedy, PawnTraits.sellPriceBonusPercent(greedy)));
            Assert.NotNull(market.sellResources(greedy));
        }

        [Fact]
        public void testAskingPrice_RaisesByAtLeastOneCoin() {
            Assert.Equal(10, Market.askingPrice(10, 0));
            Assert.Equal(12, Market.askingPrice(10, 25));
            Assert.Equal(2, Market.askingPrice(1, 25));
        }

        [Fact]
        public void testGregarious_WarmsFasterToThePlayer() {
            Pawn plain = makePawn();
            Pawn gregarious = makePawn(PawnTrait.GREGARIOUS);
            PawnRelations.raise(plain, player, PawnRelations.TALK_RELATIONSHIP_GAIN, eventProducer);
            PawnRelations.raise(gregarious, player, PawnRelations.TALK_RELATIONSHIP_GAIN, eventProducer);

            Assert.Equal(2, PawnRelations.getRelationship(plain, player));
            Assert.Equal(3, PawnRelations.getRelationship(gregarious, player));
            // the player's side is kept in step
            Assert.Equal(3, player.getRelationships()[gregarious.getId()]);
        }

        [Fact]
        public void testGregarious_ReachesAcquaintanceInFewerConversations() {
            int conversationsTo(Pawn pawn) {
                int count = 0;
                while (PawnRelations.getTier(pawn, player) < RelationshipTier.ACQUAINTANCE) {
                    PawnRelations.raise(pawn, player, PawnRelations.TALK_RELATIONSHIP_GAIN, eventProducer);
                    count++;
                }
                return count;
            }
            Assert.Equal(3, conversationsTo(makePawn()));
            Assert.Equal(2, conversationsTo(makePawn(PawnTrait.GREGARIOUS)));
        }

        [Fact]
        public void testGlutton_BurnsEnergyHalfAgainAsFast() {
            Pawn pawn = new Pawn(Vector3.Zero, "test", new RandomSource(5));
            Pawn glutton = new Pawn(Vector3.Zero, "test", new RandomSource(5));
            glutton.setTraits(new[] { PawnTrait.GLUTTON });
            Assert.Equal(pawn.getMetabolism() * PawnTraits.GLUTTON_METABOLISM_MULTIPLIER, glutton.getMetabolism(), 6);
        }

        private BehaviorType nationlessDecisionNearASettlement(Pawn pawn, float distance) {
            Pawn leader = makePawn();
            Nation nation = new Nation("Rivalia", leader.getId(), random);
            nationRepository.addNation(nation);
            leader.setNationId(nation.getId());
            Settlement settlement = new Settlement(new Vector3(distance, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            pawn.setPosition(Vector3.Zero);
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);
            return calculator.computeBehaviorType(pawn);
        }

        [Fact]
        public void testAmbitious_DoesNotJoinANationItIsMerelyNear() {
            Assert.Equal(BehaviorType.JOIN_NATION, nationlessDecisionNearASettlement(makePawn(), 80));
        }

        [Fact]
        public void testAmbitious_GathersToFoundItsOwnInstead() {
            Assert.Equal(BehaviorType.GATHER_RESOURCES, nationlessDecisionNearASettlement(makePawn(PawnTrait.AMBITIOUS), 80));
        }

        [Fact]
        public void testAmbitious_StillJoinsWhenPracticallyInside() {
            Assert.Equal(BehaviorType.JOIN_NATION, nationlessDecisionNearASettlement(makePawn(PawnTrait.AMBITIOUS), PawnTraits.AMBITIOUS_JOIN_RANGE - 10));
        }

        private Pawn rivalMember(params PawnTrait[] traits) {
            Pawn leader = makePawn();
            Nation rival = new Nation("Rivalia", leader.getId(), random);
            nationRepository.addNation(rival);
            leader.setNationId(rival.getId());
            Pawn pawn = makePawn(traits);
            rival.addMember(pawn.getId());
            pawn.setNationId(rival.getId());
            Nation home = new Nation("Homeland", player.getId(), random);
            nationRepository.addNation(home);
            player.setNationId(home.getId());
            return pawn;
        }

        [Fact]
        public void testLoyal_NeedsFarMoreThanLoyalStandingToDefect() {
            Pawn plain = rivalMember();
            Pawn loyal = rivalMember(PawnTrait.LOYAL);
            plain.increaseRelationship(player, RelationshipTiers.LOYAL_THRESHOLD);
            loyal.increaseRelationship(player, RelationshipTiers.LOYAL_THRESHOLD);
            InvitePawnToNationCommand invite = new InvitePawnToNationCommand(nationRepository, eventProducer, entityRepository);

            invite.execute(player, plain);
            Assert.Equal(player.getNationId(), plain.getNationId());

            invite.execute(player, loyal);
            Assert.NotEqual(player.getNationId(), loyal.getNationId());
            Assert.Contains("Loyal by nature", player.getStatus().getStatus());

            loyal.increaseRelationship(player, PawnTraits.LOYAL_EXTRA_RELATIONSHIP_TO_DEFECT);
            invite.execute(player, loyal);
            Assert.Equal(player.getNationId(), loyal.getNationId());
        }

        // --- shown to the player -------------------------------------------

        [Fact]
        public void testDescribe_NamesTheTraits() {
            Assert.Equal("", PawnTraits.describe(makePawn()));
            Assert.Equal("Industrious, Greedy", PawnTraits.describe(makePawn(PawnTrait.INDUSTRIOUS, PawnTrait.GREEDY)));
            Pawn pawn = makePawn(PawnTrait.GLUTTON);
            Assert.Equal("Ada Smith (Stranger) - Glutton", PawnRelations.describeWithTraits(pawn, player));
            Assert.Equal("Ada Smith (Stranger)", PawnRelations.describeWithTraits(makePawn(), player));
        }

        [Fact]
        public void testDialogue_EndsWithAWordAboutItself() {
            Pawn pawn = makePawn(PawnTrait.GLUTTON);
            Assert.Equal("Who are you? I don't belong to any nation. Is it supper time yet?", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testDialogue_AmbitiousMeansToFoundItsOwn() {
            Pawn pawn = makePawn(PawnTrait.AMBITIOUS);
            Assert.Contains("I mean to found my own.", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testDialogue_LoyalWillNotBeTempted() {
            Pawn plain = rivalMember();
            Pawn loyal = rivalMember(PawnTrait.LOYAL);
            plain.increaseRelationship(player, RelationshipTiers.LOYAL_THRESHOLD);
            loyal.increaseRelationship(player, RelationshipTiers.LOYAL_THRESHOLD);
            Assert.Contains("I'd leave Rivalia for Homeland if you asked.", PawnDialogue.compose(plain, player, nationRepository));
            Assert.Contains("I won't leave Rivalia.", PawnDialogue.compose(loyal, player, nationRepository));
        }

        [Fact]
        public void testDialogue_StarvingPawnSkipsTheAside() {
            Pawn pawn = makePawn(PawnTrait.GLUTTON);
            pawn.setEnergy(10);
            Assert.Equal("Who are you? I'm so hungry I can hardly think.", PawnDialogue.compose(pawn, player, nationRepository));
        }

        [Fact]
        public void testNametag_ShowsTraits() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Pawn pawn = new Pawn(new Vector3(0, 1.5f, 0), "Ada Smith", simulation.getRandom());
            pawn.setTraits(new[] { PawnTrait.LOYAL, PawnTrait.GREEDY });
            simulation.getEntityRepository().addEntity(pawn);
            simulation.fixedUpdate(1f / 50f);
            Assert.StartsWith("Ada Smith\nLoyal, Greedy\n", pawn.getNameTag());
        }
    }
}
