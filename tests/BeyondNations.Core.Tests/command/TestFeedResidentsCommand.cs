using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Warning of and feeding a nation's hungry pawns (#353): in the 0.5.0
    * playtest fifty pawns starved through one winter with no warning and no
    * way for the player to feed them.
    */
    public class TestFeedResidentsCommand {

        private static Simulation inOwnSettlement(out Nation nation, out Settlement settlement) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            nation = simulation.getNationRepository().getNation(player.getNationId());
            settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            player.getInventory().clear();
            return simulation;
        }

        private static Pawn addMember(Simulation simulation, Nation nation, Settlement settlement, float energy, bool present) {
            Pawn pawn = new Pawn(settlement.getPosition(), "Member", simulation.getRandom());
            simulation.getEntityRepository().addEntity(pawn);
            pawn.setNationId(nation.getId());
            nation.addMember(pawn.getId());
            pawn.getInventory().clear();
            pawn.setEnergy(energy);
            if (present) {
                pawn.setCurrentSettlementId(settlement.getId());
            }
            return pawn;
        }

        [Fact]
        public void testWarning_NamesTheHungryAndStarving_OnlyStarvingOutsideWinter() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            EntityRepository entities = simulation.getEntityRepository();
            Assert.Null(ResidentHunger.describeWarning(entities, nation, true));

            addMember(simulation, nation, settlement, 70f, true);
            Assert.Null(ResidentHunger.describeWarning(entities, nation, false));
            Assert.Equal(nation.getName() + ": 1 member hungry: in the " + settlement.getDisplayName() + ". Bring food home and Feed Residents.",
                ResidentHunger.describeWarning(entities, nation, true));

            addMember(simulation, nation, settlement, 20f, false);
            Assert.Equal(nation.getName() + ": 2 members hungry, 1 starving: 1 in the " + settlement.getDisplayName() + ", 1 outside the walls. Bring food home and Feed Residents.",
                ResidentHunger.describeWarning(entities, nation, false));
        }

        /**
        * Playtest 0.6.2 s02 (finding 8): the banner said 6 hungry, the button
        * 5, because one counted every member and the other only those
        * present, and nothing said so. Standing in the settlement, the
        * banner's "here" is the button's count.
        */
        [Fact]
        public void testWarning_InTheSettlement_SplitsThoseHereFromThoseAway_AndHereIsTheButtonsCount() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            EntityRepository entities = simulation.getEntityRepository();
            Player player = simulation.getPlayer();
            addMember(simulation, nation, settlement, 70f, true);
            addMember(simulation, nation, settlement, 20f, true);
            addMember(simulation, nation, settlement, 70f, false);
            FeedResidentsCommand command = new FeedResidentsCommand(entities);

            Assert.Equal(2, command.countHungryHere(player));
            Assert.Equal(nation.getName() + ": 3 members hungry, 1 starving: 2 here, 1 outside the walls. Feed Residents feeds the 2 here.",
                ResidentHunger.describeWarning(entities, nation, true, command.getOwnSettlement(player)));
        }

        /** s02: "Feed Residents (1 hungry)" with no banner, on the first night of spring. */
        [Fact]
        public void testWarning_ShowsWheneverTheButtonHasSomeoneToFeed_EvenOutsideWinter() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            EntityRepository entities = simulation.getEntityRepository();
            Player player = simulation.getPlayer();
            addMember(simulation, nation, settlement, 70f, true);
            FeedResidentsCommand command = new FeedResidentsCommand(entities);

            Assert.Equal(1, command.countHungryHere(player));
            Assert.Equal(nation.getName() + ": 1 member hungry: here. Feed Residents feeds the 1 here.",
                ResidentHunger.describeWarning(entities, nation, false, command.getOwnSettlement(player)));
            // away from home, a merely hungry member outside winter is still no alarm
            Assert.Null(ResidentHunger.describeWarning(entities, nation, false));
        }

        /** s02: after feeding everyone present the banner said "1 member hungry" with no hint where. */
        [Fact]
        public void testWarning_AfterFeedingEveryoneHere_SaysWhereTheRestAre() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            EntityRepository entities = simulation.getEntityRepository();
            Player player = simulation.getPlayer();
            addMember(simulation, nation, settlement, 70f, true);
            addMember(simulation, nation, settlement, 70f, false);
            player.getInventory().addItem(ItemType.APPLE, 5);
            FeedResidentsCommand command = new FeedResidentsCommand(entities);
            Assert.Equal(1, command.execute(player));

            Assert.Equal(0, command.countHungryHere(player));
            Assert.Equal(nation.getName() + ": 1 member hungry: outside the walls. Only those here can be fed: they must come home.",
                ResidentHunger.describeWarning(entities, nation, true, command.getOwnSettlement(player)));
        }

        /** s02: the button said 5 and the click "fed 8". With food enough, the click feeds exactly the button's count. */
        [Fact]
        public void testFeed_FeedsExactlyTheButtonsCount() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            for (int i = 0; i < 5; i++) {
                addMember(simulation, nation, settlement, i % 2 == 0 ? 70f : 20f, true);
            }
            addMember(simulation, nation, settlement, 20f, false);
            addMember(simulation, nation, settlement, 95f, true);
            player.getInventory().addItem(ItemType.APPLE, 20);
            FeedResidentsCommand command = new FeedResidentsCommand(simulation.getEntityRepository());

            int onTheButton = command.countHungryHere(player);
            Assert.Equal(5, onTheButton);
            Assert.Equal(onTheButton, command.execute(player));
            Assert.StartsWith("You fed 5 residents", player.getStatus().getStatus());
        }

        [Fact]
        public void testFeed_GivesFoodToTheHungryPresent_StarvingFirst_UntilItRunsOut() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            Pawn hungry = addMember(simulation, nation, settlement, 70f, true);
            Pawn starving = addMember(simulation, nation, settlement, 20f, true);
            Pawn away = addMember(simulation, nation, settlement, 20f, false);
            Pawn fed = addMember(simulation, nation, settlement, 95f, true);
            FeedResidentsCommand command = new FeedResidentsCommand(simulation.getEntityRepository());

            Assert.Equal(2, command.countHungryHere(player));
            Assert.Equal(0, command.execute(player));
            Assert.Equal("2 residents here are hungry, and you have no food to share.", player.getStatus().getStatus());

            player.getInventory().addItem(ItemType.APPLE, 1);
            Assert.Equal(1, command.execute(player));
            Assert.Equal(1, starving.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, hungry.getInventory().getNumItems(ItemType.APPLE));
            Assert.EndsWith("1 still hungry: bring more food.", player.getStatus().getStatus());

            player.getInventory().addItem(ItemType.APPLE, 5);
            Assert.Equal(1, command.execute(player));
            Assert.Equal(1, hungry.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, away.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, fed.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(4, player.getInventory().getNumItems(ItemType.APPLE));
            Assert.Equal(0, command.countHungryHere(player));
        }

        [Fact]
        public void testFeed_OnlyInsideTheNationsOwnSettlement() {
            Simulation simulation = inOwnSettlement(out Nation nation, out Settlement settlement);
            Player player = simulation.getPlayer();
            addMember(simulation, nation, settlement, 20f, true);
            player.getInventory().addItem(ItemType.APPLE, 3);
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
            FeedResidentsCommand command = new FeedResidentsCommand(simulation.getEntityRepository());

            Assert.Equal(0, command.countHungryHere(player));
            Assert.Equal(0, command.execute(player));
            Assert.Equal(3, player.getInventory().getNumItems(ItemType.APPLE));
        }
    }
}
