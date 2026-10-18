
using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    public class TestPawnBehaviorCalculator{
        private readonly RandomSource random = new RandomSource(20260816);


        /**
            * Input: pawn is nationless, no settlements nearby and does not have enough wood to create a settlement
            * Expected output: GATHER_RESOURCES
        */
        [Fact]
        public void testComputeBehaviorType_Nationless_ShouldGatherResources() {
            // prepare
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(pawn);
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();

            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // run
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // check
            Assert.Equal(BehaviorType.GATHER_RESOURCES, behaviorType);

            // cleanup
        }

        /**
            * Input: pawn is nationless, settlement within range
            * Expected output: JOIN_NATION
        */
        [Fact]
        public void testComputeBehaviorType_Nationless_ShouldJoinNation() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(pawn);

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.JOIN_NATION, behaviorType);

            // cleanup
        }

        /**
            * Input: pawn is nationless, settlement not within range, enough wood to create settlement
            * Expected output: CREATE_NATION
        */
        [Fact]
        public void testComputeBehaviorType_Nationless_ShouldCreateSettlement() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            pawn.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(pawn);

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.CREATE_NATION, behaviorType);

            // cleanup
        }

        /**
            * Input: pawn is in nation but not in settlement
            * Expected output: JOIN_RANDOM_SETTLEMENT
        */
        [Fact]
        public void testComputeBehaviorType_InNation_ShouldJoinRandomSettlement() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.JOIN_RANDOM_SETTLEMENT, behaviorType);

            // cleanup
        }

        /**
            * Input: pawn is leader of nation and has enough wood to create a settlement
            * Expected output: CONSTRUCT_SETTLEMENT
        */
        [Fact]
        public void testComputeBehaviorType_IsLeaderAndHasEnoughWood_ShouldConstructSettlement() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare pawn & nation
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            nationLeader.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationLeader.setNationId(nation.getId());
            nationRepository.addNation(nation);

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(nationLeader);

            // verify
            Assert.Equal(BehaviorType.CONSTRUCT_SETTLEMENT, behaviorType);

            // cleanup
        }

        /**
            * Input: pawn is leader of nation, has enough wood to build a stall & market is not full
            * Expected output: CONSTRUCT_STALL
        */
        [Fact]
        public void testComputeBehaviorType_IsLeaderAndHasEnoughWood_ShouldConstructStall() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare pawn & nation
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            nationLeader.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationLeader.setNationId(nation.getId());
            nationRepository.addNation(nation);

            // prepare settlement
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            nationLeader.setHomeSettlementId(settlement.getId());
            nationLeader.setCurrentSettlementId(settlement.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(nationLeader);

            // verify
            Assert.Equal(BehaviorType.CONSTRUCT_STALL, behaviorType);

            // cleanup
        }
        
        /**
            * Input: pawn is leader of nation, has enough wood to build a stall & market is full
            * Expected output: EXIT_SETTLEMENT or NONE
        */
        [Fact]
        public void testComputeBehaviorType_IsLeaderAndHasEnoughWoodButMarketFull_ShouldExitOrDoNothing() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare pawn & nation
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            nationLeader.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationLeader.setNationId(nation.getId());
            nationRepository.addNation(nation);

            // prepare settlement
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            nationLeader.setHomeSettlementId(settlement.getId());
            nationLeader.setCurrentSettlementId(settlement.getId());

            // prepare market
            for (int i = 0; i < settlement.getMarket().getMaxNumStalls(); i++) {
                settlement.getMarket().createStall();
            }

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(nationLeader);

            // verify
            Assert.True(behaviorType == BehaviorType.EXIT_SETTLEMENT || behaviorType == BehaviorType.NONE);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_IsLeaderAndLowOnFunds_ShouldWithdrawFunds() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare pawn & nation
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            nationLeader.setEnergy(100);
            nationLeader.getInventory().addItem(ItemType.APPLE, 100);
            // A pawn is given between 50 and 199 starting coins, and the
            // behaviour under test only triggers below 100, so the precondition
            // this test is named for has to be pinned rather than left to chance.
            nationLeader.getInventory().setNumItems(ItemType.COIN, 50);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationLeader.setNationId(nation.getId());
            nationRepository.addNation(nation);
            nation.setRole(nationLeader.getId(), NationRole.LEADER);

            // prepare settlement
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            settlement.addFunds(200);
            entityRepository.addEntity(settlement);
            nation.addSettlement(settlement.getId());
            nationLeader.setHomeSettlementId(settlement.getId());
            nationLeader.setCurrentSettlementId(settlement.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(nationLeader);

            // verify
            Assert.Equal(BehaviorType.WITHDRAW_SETTLEMENT_FUNDS, behaviorType);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_InSettlement_ShouldPurchaseFood() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare existing stall & merchant
            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(ItemType.APPLE, 10);

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            pawn.setEnergy(10);
            pawn.getInventory().addItem(ItemType.COIN, 10);
            entityRepository.addEntity(pawn);
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setCurrentSettlementId(settlement.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.PURCHASE_FOOD, behaviorType);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_InSettlement_ShouldSellResources() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare existing stall & merchant
            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(ItemType.COIN, 100);

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            Inventory inventory = pawn.getInventory();
            inventory.addItem(ItemType.WOOD, 100);
            inventory.addItem(ItemType.STONE, 100);
            inventory.addItem(ItemType.COIN, 100);
            inventory.addItem(ItemType.APPLE, 100);
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setCurrentSettlementId(settlement.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.SELL_RESOURCES, behaviorType);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_InSettlement_ShouldCollectProfitFromStall() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare existing stall & merchant
            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            nation.addMember(merchant.getId());
            nation.setRole(merchant.getId(), NationRole.MERCHANT);
            merchant.setNationId(nation.getId());
            merchant.setHomeSettlementId(settlement.getId());
            merchant.setCurrentSettlementId(settlement.getId());
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(ItemType.COIN, 200);

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(merchant);

            // verify
            Assert.Equal(BehaviorType.COLLECT_PROFIT_FROM_STALL, behaviorType);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_InSettlementNoFoodAvailable_ShouldNotPurchaseFood() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare existing stall & merchant
            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            nation.addMember(merchant.getId());
            nation.setRole(merchant.getId(), NationRole.MERCHANT);
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());

            // prepare pawn
            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            pawn.setEnergy(10);
            pawn.getInventory().addItem(ItemType.COIN, 10);
            entityRepository.addEntity(pawn);
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setCurrentSettlementId(settlement.getId());

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.NotEqual(BehaviorType.PURCHASE_FOOD, behaviorType);

            // cleanup
        }

        [Fact]
        public void testComputeBehaviorType_InSettlementIsMerchantStallHasFood_ShouldCollectFoodFromStall() {
            // prepare world
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            // prepare existing nation & settlement
            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            // prepare existing stall & merchant
            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            nation.addMember(merchant.getId());
            nation.setRole(merchant.getId(), NationRole.MERCHANT);
            merchant.setNationId(nation.getId());
            merchant.setEnergy(10);
            merchant.setHomeSettlementId(settlement.getId());
            merchant.setCurrentSettlementId(settlement.getId());
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(ItemType.APPLE, 10);

            // prepare calculator
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();
            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(merchant);

            // verify
            Assert.Equal(BehaviorType.COLLECT_FOOD_FROM_STALL, behaviorType);

            // cleanup
        }

        /**
            * Computes the behavior of a hungry pawn standing in a settlement whose
            * only stall is owned by a merchant and stocked as given. When
            * pawnOwnsStall is set the hungry pawn is that merchant instead.
        */
        private BehaviorType computeHungryPawnInSettlementWithStall(ItemType stockType, int stockQuantity, int pawnCoins, bool pawnOwnsStall) {
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);

            Pawn nationLeader = new Pawn(new Vector3(0, 0, 0), "test", random);
            entityRepository.addEntity(nationLeader);
            Nation nation = new Nation("test", nationLeader.getId(), random);
            nationRepository.addNation(nation);
            Settlement settlement = new Settlement(new Vector3(0, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(settlement);

            Pawn merchant = new Pawn(new Vector3(0, 0, 0), "test", random);
            nation.addMember(merchant.getId());
            nation.setRole(merchant.getId(), NationRole.MERCHANT);
            merchant.setNationId(nation.getId());
            entityRepository.addEntity(merchant);
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(merchant.getId());
            stall.getInventory().addItem(stockType, stockQuantity);

            Pawn pawn = merchant;
            if (!pawnOwnsStall) {
                pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
                entityRepository.addEntity(pawn);
            }
            pawn.setEnergy(10);
            pawn.getInventory().setNumItems(ItemType.COIN, pawnCoins);
            pawn.setHomeSettlementId(settlement.getId());
            pawn.setCurrentSettlementId(settlement.getId());

            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, new GameConfig(), new TickCounter(), random);
            return calculator.computeBehaviorType(pawn);
        }

        /**
            * Input: hungry pawn in a settlement whose only food for sale is chicken meat
            * Expected output: PURCHASE_FOOD (#255)
        */
        [Fact]
        public void testComputeBehaviorType_InSettlementOnlyMeatForSale_ShouldPurchaseFood() {
            // run
            BehaviorType behaviorType = computeHungryPawnInSettlementWithStall(ItemType.CHICKEN_MEAT, 1, 100, false);

            // verify
            Assert.Equal(BehaviorType.PURCHASE_FOOD, behaviorType);
        }

        /**
            * Input: hungry pawn with fewer coins than the only food on sale costs
            * Expected output: not PURCHASE_FOOD, since the purchase would fail
        */
        [Fact]
        public void testComputeBehaviorType_InSettlementCannotAffordFood_ShouldNotPurchaseFood() {
            // run: one apple is priced at its base cost of 40
            BehaviorType behaviorType = computeHungryPawnInSettlementWithStall(ItemType.APPLE, 1, 39, false);

            // verify
            Assert.NotEqual(BehaviorType.PURCHASE_FOOD, behaviorType);
        }

        /**
            * Input: hungry merchant whose own stall holds chicken meat and no apples
            * Expected output: COLLECT_FOOD_FROM_STALL (#255)
        */
        [Fact]
        public void testComputeBehaviorType_InSettlementIsMerchantStallHasMeat_ShouldCollectFoodFromStall() {
            // run
            BehaviorType behaviorType = computeHungryPawnInSettlementWithStall(ItemType.CHICKEN_MEAT, 4, 0, true);

            // verify
            Assert.Equal(BehaviorType.COLLECT_FOOD_FROM_STALL, behaviorType);
        }

        /**
            * Input: pawn is outside a settlement, low on energy and carrying nothing edible
            * Expected output: GO_TO_HOME_SETTLEMENT
        */
        [Fact]
        public void testComputeBehaviorType_OutsideSettlementAndHungryWithNoFood_ShouldGoHome() {
            // prepare
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();

            Pawn pawn = makeHungryPawnWithADistantHome(entityRepository, nationRepository);

            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify
            Assert.Equal(BehaviorType.GO_TO_HOME_SETTLEMENT, behaviorType);

            // cleanup
        }

        /**
            * Input: the same pawn, carrying chicken meat instead of nothing
            * Expected output: not GO_TO_HOME_SETTLEMENT -- the meat is food, so the
            * pawn eats on the next energy step rather than walking home for an apple (#83)
        */
        [Fact]
        public void testComputeBehaviorType_OutsideSettlementAndHungryWithChickenMeat_ShouldNotGoHomeForFood() {
            // prepare: identical to the test above but for the meat
            EntityRepository entityRepository = new EntityRepository(random);
            Environment environment = new Environment(5, 5, entityRepository, random);
            NationRepository nationRepository = new NationRepository(random);
            GameConfig gameConfig = new GameConfig();
            TickCounter tickCounter = new TickCounter();

            Pawn pawn = makeHungryPawnWithADistantHome(entityRepository, nationRepository);
            pawn.getInventory().addItem(ItemType.CHICKEN_MEAT, 1);

            PawnBehaviorCalculator calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            // execute
            BehaviorType behaviorType = calculator.computeBehaviorType(pawn);

            // verify: nationless, no settlement within join range and not enough
            // wood to found one, so the pawn goes back to gathering
            Assert.Equal(BehaviorType.GATHER_RESOURCES, behaviorType);

            // cleanup
        }

        /**
        * A nationless pawn at the origin, below the energy threshold, holding coins
        * and a home settlement far enough away that it is out of join range. The
        * only thing separating the two food tests above is what it is carrying.
        */
        private Pawn makeHungryPawnWithADistantHome(EntityRepository entityRepository, NationRepository nationRepository) {
            Pawn homeLeader = new Pawn(new Vector3(1000, 0, 1000), "test", random);
            entityRepository.addEntity(homeLeader);
            Nation homeNation = new Nation("test", homeLeader.getId(), random);
            nationRepository.addNation(homeNation);
            Settlement home = new Settlement(new Vector3(1000, 0, 1000), homeNation.getId(), homeNation.getColor(), homeNation.getName(), random);
            entityRepository.addEntity(home);

            Pawn pawn = new Pawn(new Vector3(0, 0, 0), "test", random);
            pawn.setEnergy(10);
            // A pawn starts with between 50 and 199 coins; the branch under test
            // needs at least one, so the precondition is pinned rather than rolled.
            pawn.getInventory().setNumItems(ItemType.COIN, 10);
            pawn.setHomeSettlementId(home.getId());
            entityRepository.addEntity(pawn);
            return pawn;
        }
    }
}