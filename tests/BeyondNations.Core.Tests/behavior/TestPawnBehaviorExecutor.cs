
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPawnBehaviorExecutor {
        private readonly RandomSource random = new RandomSource(20260816);

        private EntityRepository entityRepository;
        private Environment environment;
        private PawnBehaviorExecutor executor;
        private NationRepository nationRepository;

        public TestPawnBehaviorExecutor() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            nationRepository = new NationRepository(random);
            EventProducer eventProducer = new EventProducer(new EventRepository());
            executor = new PawnBehaviorExecutor(environment, nationRepository, eventProducer, entityRepository, random, new NationNameGenerator(random), new TickCounter());
        }

        /**
            * A pawn starts with coins only, so it needs food as soon as its energy
            * drops under the threshold.
        */
        private Pawn createPawn(float energy) {
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            pawn.setEnergy(energy);
            pawn.setCurrentBehaviorType(BehaviorType.GATHER_RESOURCES);
            entityRepository.addEntity(pawn);
            return pawn;
        }

        /**
            * Input: hungry pawn, a tree close by and a chicken further away
            * Expected output: the pawn targets the chicken (#253)
        */
        [Fact]
        public void testGatherResources_HungryPawn_TargetsChickenOverCloserTree() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            AppleTree tree = new AppleTree(new Vector3(20, 0, 0), 3, random);
            entityRepository.addEntity(tree);
            Chicken chicken = new Chicken(new Vector3(60, 0, 0), random);
            entityRepository.addEntity(chicken);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(chicken, pawn.getTargetEntity());
            Assert.False(chicken.isMarkedForDeletion());
        }

        /**
            * Input: pawn with full energy, a chicken close by and a tree further away
            * Expected output: the pawn leaves the chicken alone and targets the tree
        */
        [Fact]
        public void testGatherResources_SatedPawn_LeavesChickenAlone() {
            // prepare
            Pawn pawn = createPawn(100);
            Chicken chicken = new Chicken(new Vector3(20, 0, 0), random);
            entityRepository.addEntity(chicken);
            AppleTree tree = new AppleTree(new Vector3(60, 0, 0), 3, random);
            entityRepository.addEntity(tree);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(tree, pawn.getTargetEntity());
        }

        /**
            * Input: pawn under the energy threshold but already carrying food, chicken close by
            * Expected output: the pawn does not hunt, since it has something to eat
        */
        [Fact]
        public void testGatherResources_HungryPawnCarryingFood_DoesNotHunt() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            pawn.getInventory().addItem(ItemType.APPLE, 1);
            Chicken chicken = new Chicken(new Vector3(20, 0, 0), random);
            entityRepository.addEntity(chicken);
            Rock rock = new Rock(new Vector3(60, 0, 0));
            entityRepository.addEntity(rock);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(rock, pawn.getTargetEntity());
        }

        /**
            * Input: hungry pawn, no chicken anywhere, a rock nearer than a tree
            * Expected output: the pawn goes for the tree, which carries apples,
            * not the rock: a starving pawn wants food, not stone
        */
        [Fact]
        public void testGatherResources_HungryPawnWithoutChicken_TargetsAFruitTreeOverACloserRock() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            AppleTree tree = new AppleTree(new Vector3(60, 0, 0), 3, random);
            entityRepository.addEntity(tree);
            Rock rock = new Rock(new Vector3(20, 0, 0));
            entityRepository.addEntity(rock);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(tree, pawn.getTargetEntity());
        }

        /**
            * Input: hungry pawn in spring, a fruit tree close by and the only
            * chicken far beyond the hunting range
            * Expected output: the pawn eats from the tree rather than trekking
            * across the world after the chicken (the early-spring die-off)
        */
        [Fact]
        public void testGatherResources_HungryPawn_TakesANearTreeOverAFarChicken() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            AppleTree tree = new AppleTree(new Vector3(20, 0, 0), 3, random);
            entityRepository.addEntity(tree);
            Chicken chicken = new Chicken(new Vector3(SeasonalRules.STORES_HUNT_RANGE * 3, 0, 0), random);
            entityRepository.addEntity(chicken);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(tree, pawn.getTargetEntity());
        }

        /**
            * Input: the same, in winter, when the trees are bare for pawns
            * Expected output: the pawn still walks for the chicken -- winter stays lean
        */
        [Fact]
        public void testGatherResources_HungryPawnInWinter_StillWalksForAFarChicken() {
            // prepare
            TickCounter ticks = new TickCounter();
            SeasonCalendar calendar = new SeasonCalendar(new TimeOfDay(ticks));
            while (!calendar.isWinter()) {
                ticks.increment();
            }
            executor.setSeasonCalendar(calendar);
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            AppleTree tree = new AppleTree(new Vector3(20, 0, 0), 3, random);
            entityRepository.addEntity(tree);
            Chicken chicken = new Chicken(new Vector3(SeasonalRules.STORES_HUNT_RANGE * 3, 0, 0), random);
            entityRepository.addEntity(chicken);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(chicken, pawn.getTargetEntity());
        }

        /**
            * Input: hungry pawn standing next to a chicken
            * Expected output: the chicken is harvested -- marked for deletion, its
            * meat moved to the pawn, the target cleared and the behavior finished
        */
        [Fact]
        public void testGatherResources_AtChicken_HarvestsMeat() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            Chicken chicken = new Chicken(new Vector3(2, 0, 0), random);
            entityRepository.addEntity(chicken);
            int meatOnChicken = chicken.getInventory().getNumItems(ItemType.CHICKEN_MEAT);
            Assert.True(meatOnChicken > 0);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.True(chicken.isMarkedForDeletion());
            Assert.Equal(meatOnChicken, pawn.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(0, chicken.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Null(pawn.getTargetEntity());
            Assert.Equal(BehaviorType.NONE, pawn.getCurrentBehaviorType());
        }

        /**
            * Input: pawn already walking to a chicken
            * Expected output: the target is kept rather than re-selected
        */
        [Fact]
        public void testGatherResources_ExistingChickenTarget_IsKept() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            Chicken chicken = new Chicken(new Vector3(60, 0, 0), random);
            entityRepository.addEntity(chicken);
            pawn.setTargetEntity(chicken);
            // a tree appearing closer must not distract the pawn mid-hunt
            AppleTree tree = new AppleTree(new Vector3(20, 0, 0), 3, random);
            entityRepository.addEntity(tree);

            // run
            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            // check
            Assert.Same(chicken, pawn.getTargetEntity());
            Assert.True(pawn.getVelocity().X > 0);
        }

        /**
            * Input: hungry pawn in a settlement, owning a stall that holds meat and apples
            * Expected output: half the meat moves to the pawn and the apples stay (#255)
        */
        [Fact]
        public void testCollectFoodFromStall_TakesHalfTheMostNourishingFood() {
            // prepare
            Pawn pawn = createPawn(Pawn.HUNGRY_ENERGY_THRESHOLD - 1);
            Nation nation = new Nation("test", pawn.getId(), random);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            pawn.setCurrentSettlementId(settlement.getId());
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(pawn.getId());
            stall.getInventory().addItem(ItemType.CHICKEN_MEAT, 4);
            stall.getInventory().addItem(ItemType.APPLE, 6);

            // run
            executor.executeBehavior(pawn, BehaviorType.COLLECT_FOOD_FROM_STALL);

            // check
            Assert.Equal(2, pawn.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(2, stall.getInventory().getNumItems(ItemType.CHICKEN_MEAT));
            Assert.Equal(6, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, pawn.getInventory().getNumItems(ItemType.APPLE));
        }

        /**
            * Input: a merchant inside its settlement with wood and stone, stall empty
            * Expected output: it makes an axe and a pickaxe and stocks them with the rest (#47)
        */
        [Fact]
        public void testTransferItemsToStall_MerchantCraftsToolsForItsStall() {
            Pawn pawn = createPawn(100);
            pawn.getInventory().clear();
            Settlement settlement = new Settlement(new Vector3(0, 1, 0), new NationId(), Rgba.Red, "Testland", random);
            entityRepository.addEntity(settlement);
            settlement.getMarket().createStall();
            settlement.getMarket().getStallForSale().setOwnerId(pawn.getId());
            pawn.setCurrentSettlementId(settlement.getId());
            pawn.getInventory().addItem(ItemType.WOOD, 10);
            pawn.getInventory().addItem(ItemType.STONE, 6);

            executor.executeBehavior(pawn, BehaviorType.TRANSFER_ITEMS_TO_STALL);

            Inventory stall = settlement.getMarket().getStall(pawn.getId()).getInventory();
            Assert.Equal(1, stall.getNumItems(ItemType.AXE));
            Assert.Equal(1, stall.getNumItems(ItemType.PICKAXE));
            Assert.Equal(3, stall.getNumItems(ItemType.WOOD));
            Assert.Equal(1, stall.getNumItems(ItemType.STONE));
            Assert.Equal(0, pawn.getInventory().getNumItems(ItemType.WOOD));
        }

        /**
        * A merchant of its own nation standing in a settlement where it keeps
        * no stall (its stall, full of coin and food, is in another). The
        * 0.6.0 release-candidate playtest (session s02, seed 102) crashed on
        * such a merchant.
        */
        private Pawn merchantAwayFromItsStall(out Settlement elsewhere, out Stall stall) {
            Pawn pawn = createPawn(100);
            pawn.getInventory().clear();
            Nation nation = new Nation("test", new EntityId(), random);
            nationRepository.addNation(nation);
            nation.addMember(pawn.getId());
            nation.setRole(pawn.getId(), NationRole.MERCHANT);
            pawn.setNationId(nation.getId());

            Settlement home = new Settlement(new Vector3(0, 1, 0), nation.getId(), nation.getColor(), "Home", random);
            entityRepository.addEntity(home);
            home.getMarket().createStall();
            stall = home.getMarket().getStallForSale();
            stall.setOwnerId(pawn.getId());
            stall.getInventory().addItem(ItemType.COIN, 100);
            stall.getInventory().addItem(ItemType.APPLE, 6);

            elsewhere = new Settlement(new Vector3(200, 1, 0), nation.getId(), nation.getColor(), "Elsewhere", random);
            entityRepository.addEntity(elsewhere);
            elsewhere.getMarket().createStall();
            pawn.setCurrentSettlementId(elsewhere.getId());
            return pawn;
        }

        /**
            * Input: TRANSFER_ITEMS_TO_STALL for a merchant with wood and stone, in a settlement without its stall
            * Expected output: no crash, nothing moves anywhere, and the pawn drops the behaviour
        */
        [Fact]
        public void testTransferItemsToStall_NoStallInThisMarket_DoesNothing() {
            Pawn pawn = merchantAwayFromItsStall(out Settlement elsewhere, out Stall stall);
            pawn.getInventory().addItem(ItemType.WOOD, 10);
            pawn.getInventory().addItem(ItemType.STONE, 6);
            pawn.setCurrentBehaviorType(BehaviorType.TRANSFER_ITEMS_TO_STALL);

            executor.executeBehavior(pawn, BehaviorType.TRANSFER_ITEMS_TO_STALL);

            Assert.Equal(10, pawn.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(6, pawn.getInventory().getNumItems(ItemType.STONE));
            Assert.Equal(100, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(0, elsewhere.getMarket().getStalls()[0].getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(BehaviorType.NONE, pawn.getCurrentBehaviorType());
        }

        /**
            * Input: COLLECT_PROFIT_FROM_STALL for a merchant in a settlement without its stall
            * Expected output: no crash, no coin taken from anyone's stall, behaviour dropped
        */
        [Fact]
        public void testCollectProfitFromStall_NoStallInThisMarket_DoesNothing() {
            Pawn pawn = merchantAwayFromItsStall(out Settlement elsewhere, out Stall stall);
            pawn.setCurrentBehaviorType(BehaviorType.COLLECT_PROFIT_FROM_STALL);

            executor.executeBehavior(pawn, BehaviorType.COLLECT_PROFIT_FROM_STALL);

            Assert.Equal(0, pawn.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(100, stall.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(BehaviorType.NONE, pawn.getCurrentBehaviorType());
        }

        /**
            * Input: COLLECT_FOOD_FROM_STALL for a merchant in a settlement without its stall
            * Expected output: no crash, no food taken, behaviour dropped
        */
        [Fact]
        public void testCollectFoodFromStall_NoStallInThisMarket_DoesNothing() {
            Pawn pawn = merchantAwayFromItsStall(out Settlement elsewhere, out Stall stall);
            pawn.setCurrentBehaviorType(BehaviorType.COLLECT_FOOD_FROM_STALL);

            executor.executeBehavior(pawn, BehaviorType.COLLECT_FOOD_FROM_STALL);

            Assert.Equal(0, pawn.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(6, stall.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(BehaviorType.NONE, pawn.getCurrentBehaviorType());
        }

        /**
            * Input: PURCHASE_STALL for a serf with coin, inside a foreign settlement with a stall for sale
            * Expected output: no sale -- the stall stays for sale, the coin stays with the pawn and it stays a serf
        */
        [Fact]
        public void testPurchaseStall_InAForeignSettlement_IsRefused() {
            Pawn pawn = createPawn(100);
            pawn.getInventory().setNumItems(ItemType.COIN, Stall.COIN_COST_TO_PURCHASE * 2);
            Nation own = new Nation("own", new EntityId(), random);
            nationRepository.addNation(own);
            own.addMember(pawn.getId());
            pawn.setNationId(own.getId());
            Nation foreign = new Nation("foreign", new EntityId(), random);
            nationRepository.addNation(foreign);
            Settlement foreignTown = new Settlement(new Vector3(0, 1, 0), foreign.getId(), foreign.getColor(), "Abroad", random);
            entityRepository.addEntity(foreignTown);
            foreignTown.getMarket().createStall();
            pawn.setCurrentSettlementId(foreignTown.getId());

            executor.executeBehavior(pawn, BehaviorType.PURCHASE_STALL);

            Assert.Equal(1, foreignTown.getMarket().getNumStallsForSale());
            Assert.Null(foreignTown.getMarket().getStall(pawn.getId()));
            Assert.Equal(Stall.COIN_COST_TO_PURCHASE * 2, pawn.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(NationRole.SERF, own.getRole(pawn.getId()));
        }

        /**
            * Input: PURCHASE_STALL for a serf with coin, inside its own nation's settlement with a stall for sale
            * Expected output: it buys the stall and becomes a merchant
        */
        [Fact]
        public void testPurchaseStall_InItsOwnNationsSettlement_Succeeds() {
            Pawn pawn = createPawn(100);
            pawn.getInventory().setNumItems(ItemType.COIN, Stall.COIN_COST_TO_PURCHASE * 2);
            Nation own = new Nation("own", new EntityId(), random);
            nationRepository.addNation(own);
            own.addMember(pawn.getId());
            pawn.setNationId(own.getId());
            Settlement home = new Settlement(new Vector3(0, 1, 0), own.getId(), own.getColor(), "Home", random);
            entityRepository.addEntity(home);
            home.getMarket().createStall();
            pawn.setCurrentSettlementId(home.getId());

            executor.executeBehavior(pawn, BehaviorType.PURCHASE_STALL);

            Assert.NotNull(home.getMarket().getStall(pawn.getId()));
            Assert.Equal(Stall.COIN_COST_TO_PURCHASE, pawn.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal(NationRole.MERCHANT, own.getRole(pawn.getId()));
        }

        /**
            * Input: a pawn carrying an axe gathers a tree
            * Expected output: the axe's extra wood, the same as the player gets
        */
        [Fact]
        public void testGatherResources_PawnWithAxe_GetsExtraWood() {
            Pawn pawn = createPawn(100);
            pawn.getInventory().clear();
            pawn.getInventory().addItem(ItemType.AXE, 1);
            AppleTree tree = new AppleTree(new Vector3(0, 0, 0), 3, random);
            int wood = tree.getInventory().getNumItems(ItemType.WOOD);
            entityRepository.addEntity(tree);
            pawn.setTargetEntity(tree);

            executor.executeBehavior(pawn, BehaviorType.GATHER_RESOURCES);

            Assert.True(tree.isMarkedForDeletion());
            Assert.Equal(wood + Tools.AXE_BONUS_WOOD, pawn.getInventory().getNumItems(ItemType.WOOD));
        }
    }
}
