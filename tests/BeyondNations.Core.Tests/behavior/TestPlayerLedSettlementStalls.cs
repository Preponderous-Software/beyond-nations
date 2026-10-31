using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * The player's own settlement is the player's to build up (#423): its
    * residents leave stall building to the player, as they leave it to a pawn
    * leader who lives with them. The 0.6.4 playtest saw residents treat the
    * player's town as a colony (the leader is not a pawn, so "the leader does
    * not live here") and fill all four stalls before the player could build one.
    */
    public class TestPlayerLedSettlementStalls {
        private readonly RandomSource random = new RandomSource(20261029);

        private readonly GameConfig gameConfig = new GameConfig();
        private readonly EntityRepository entityRepository;
        private readonly Environment environment;
        private readonly NationRepository nationRepository;
        private readonly TickCounter tickCounter = new TickCounter();
        private readonly PawnBehaviorCalculator calculator;

        private readonly Player player;
        private readonly Nation nation;
        private readonly Settlement home;

        public TestPlayerLedSettlementStalls() {
            entityRepository = new EntityRepository(random);
            environment = new Environment(5, 5, entityRepository, random);
            nationRepository = new NationRepository(random);
            calculator = new PawnBehaviorCalculator(environment, entityRepository, nationRepository, gameConfig, tickCounter, random);

            player = new Player(1, 2, tickCounter, 100, 100, random);
            entityRepository.addEntity(player);
            nation = new Nation("Homeland", player.getId(), random);
            nationRepository.addNation(nation);
            player.setNationId(nation.getId());

            home = new Settlement(Vector3.Zero, nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(home);
            nation.addSettlement(home.getId());
            player.setHomeSettlementId(home.getId());
        }

        private Pawn addResident() {
            Pawn pawn = new Pawn(Vector3.Zero, "Resident", random);
            entityRepository.addEntity(pawn);
            nation.addMember(pawn.getId());
            pawn.setNationId(nation.getId());
            pawn.getInventory().setNumItems(ItemType.COIN, 0);
            pawn.setHomeSettlementId(home.getId());
            SettlementLife.admit(pawn, home);
            return pawn;
        }

        [Fact]
        public void testResidentOfThePlayersTown_LeavesStallBuildingToThePlayer() {
            Pawn resident = addResident();
            home.getInventory().addItem(ItemType.WOOD, 100);
            home.getInventory().addItem(ItemType.STONE, 100);

            for (int i = 0; i < 100; i++) {
                Assert.NotEqual(BehaviorType.CONSTRUCT_STALL, calculator.computeBehaviorType(resident));
            }
        }

        [Fact]
        public void testResidentOfAColonyThePlayerDoesNotLiveIn_StillBuildsAStall() {
            // the colony rule stands: a settlement its leader does not live in is built up by its residents
            Settlement colony = new Settlement(new Vector3(1000, 0, 0), nation.getId(), nation.getColor(), nation.getName(), random);
            entityRepository.addEntity(colony);
            nation.addSettlement(colony.getId());
            Pawn resident = new Pawn(colony.getPosition(), "Colonist", random);
            entityRepository.addEntity(resident);
            nation.addMember(resident.getId());
            resident.setNationId(nation.getId());
            resident.getInventory().setNumItems(ItemType.COIN, 0);
            resident.setHomeSettlementId(colony.getId());
            SettlementLife.admit(resident, colony);
            colony.getInventory().addItem(ItemType.WOOD, Stall.WOOD_COST_TO_BUILD);
            colony.getInventory().addItem(ItemType.STONE, Stall.STONE_COST_TO_BUILD);

            Assert.Equal(BehaviorType.CONSTRUCT_STALL, calculator.computeBehaviorType(resident));
        }
    }
}
