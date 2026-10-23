using System.Numerics;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * A merchant keeps its stall in one of its own nation's settlements. The
    * 0.6.0 release-candidate playtest (session s02, seed 102) saved a world
    * holding a merchant whose stall was in a foreign market -- it had bought
    * the stall it had just given up, while still standing in the nation the
    * player invited it away from -- and the game crashed on it at home, and
    * again on every load of the autosave.
    */
    public class TestStallOwnership {
        private readonly RandomSource random = new RandomSource(20261022);

        private class World {
            public EntityRepository entities;
            public NationRepository nations;
            public Nation own;
            public Nation foreign;
            public Settlement home;
            public Settlement foreignTown;
            public Pawn merchant;
        }

        private World makeWorld(EntityRepository entities, NationRepository nations, Vector3 at) {
            World w = new World();
            w.entities = entities;
            w.nations = nations;

            Pawn ownLeader = new Pawn(at, "own leader", random);
            entities.addEntity(ownLeader);
            w.own = new Nation("Ownland", ownLeader.getId(), random);
            nations.addNation(w.own);
            w.home = new Settlement(at, w.own.getId(), w.own.getColor(), w.own.getName(), random);
            entities.addEntity(w.home);
            w.own.addSettlement(w.home.getId());
            ownLeader.setNationId(w.own.getId());
            ownLeader.setHomeSettlementId(w.home.getId());

            Vector3 abroad = at + new Vector3(300, 0, 0);
            Pawn foreignLeader = new Pawn(abroad, "foreign leader", random);
            entities.addEntity(foreignLeader);
            w.foreign = new Nation("Abroadland", foreignLeader.getId(), random);
            nations.addNation(w.foreign);
            w.foreignTown = new Settlement(abroad, w.foreign.getId(), w.foreign.getColor(), w.foreign.getName(), random);
            entities.addEntity(w.foreignTown);
            w.foreign.addSettlement(w.foreignTown.getId());
            foreignLeader.setNationId(w.foreign.getId());
            foreignLeader.setHomeSettlementId(w.foreignTown.getId());

            w.merchant = new Pawn(at, "merchant", random);
            entities.addEntity(w.merchant);
            w.own.addMember(w.merchant.getId());
            w.merchant.setNationId(w.own.getId());
            w.merchant.setHomeSettlementId(w.home.getId());
            w.own.setRole(w.merchant.getId(), NationRole.MERCHANT);
            return w;
        }

        private static Stall stallIn(Settlement settlement, EntityId owner) {
            settlement.getMarket().createStall();
            Stall stall = settlement.getMarket().getStallForSale();
            stall.setOwnerId(owner);
            return stall;
        }

        [Fact]
        public void testReconcile_AMerchantWithItsStallAbroad_GivesItUpAndIsASerfAgain() {
            World w = makeWorld(new EntityRepository(random), new NationRepository(random), Vector3.Zero);
            Stall abroad = stallIn(w.foreignTown, w.merchant.getId());

            int repairs = StallOwnership.reconcile(w.entities, w.nations);

            Assert.Equal(2, repairs);
            Assert.Null(abroad.getOwnerId());
            Assert.Equal(NationRole.SERF, w.own.getRole(w.merchant.getId()));
        }

        [Fact]
        public void testReconcile_AMerchantWithItsStallAtHome_IsLeftAlone() {
            World w = makeWorld(new EntityRepository(random), new NationRepository(random), Vector3.Zero);
            Stall atHome = stallIn(w.home, w.merchant.getId());

            int repairs = StallOwnership.reconcile(w.entities, w.nations);

            Assert.Equal(0, repairs);
            Assert.Equal(w.merchant.getId(), atHome.getOwnerId());
            Assert.Equal(NationRole.MERCHANT, w.own.getRole(w.merchant.getId()));
        }

        /**
        * The playtest's save: such a merchant, standing in its home settlement.
        * It loads with the merchant a serf and the foreign stall for sale, and
        * the world plays on.
        */
        [Fact]
        public void testASaveHoldingAMerchantWithItsStallAbroad_LoadsRepairedAndPlaysOn() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(11);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < 100; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            World w = makeWorld(simulation.getEntityRepository(), simulation.getNationRepository(), new Vector3(40, 0, 40));
            Stall abroad = stallIn(w.foreignTown, w.merchant.getId());
            SettlementLife.admit(w.merchant, w.home);

            Simulation loaded = SaveGame.decode(SaveGame.encode(simulation, "test"));

            Nation own = loaded.getNationRepository().getNation(w.own.getId());
            Settlement foreignTown = (Settlement) loaded.getEntityRepository().getEntity(w.foreignTown.getId());
            Assert.Equal(NationRole.SERF, own.getRole(w.merchant.getId()));
            Assert.Null(foreignTown.getMarket().getStall(w.merchant.getId()));
            Assert.Equal(1, foreignTown.getMarket().getNumStallsForSale());
            for (int i = 0; i < 2000; i++) {
                loaded.fixedUpdate(1f / 50f);
            }
        }

        /**
        * The same merchant, never saved: the simulation itself must not crash
        * on it (PawnBehaviorCalculator), whether or not a load has repaired it.
        */
        [Fact]
        public void testAMerchantAtHomeWithItsStallAbroad_DoesNotCrashTheSimulation() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(11);
            Simulation simulation = new Simulation(config);
            World w = makeWorld(simulation.getEntityRepository(), simulation.getNationRepository(), new Vector3(40, 0, 40));
            stallIn(w.foreignTown, w.merchant.getId());
            // nothing to stock, as in the playtest: it went straight to its (missing) stall's coin
            w.merchant.getInventory().removeItem(ItemType.WOOD, w.merchant.getInventory().getNumItems(ItemType.WOOD));
            w.merchant.getInventory().removeItem(ItemType.STONE, w.merchant.getInventory().getNumItems(ItemType.STONE));
            SettlementLife.admit(w.merchant, w.home);

            for (int i = 0; i < 2000; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
        }
    }
}
