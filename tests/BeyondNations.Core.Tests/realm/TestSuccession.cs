using System.Collections.Generic;
using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * When the player dies in a nation, an heir takes their place and the
    * nation goes on; with none, the run ends (realm/Succession).
    */
    public class TestSuccession {
        private readonly DiplomacyFixture world = new DiplomacyFixture();
        private Realm realm { get { return world.simulation.getRealm(); } }

        private List<Pawn> pawnsOf(Nation nation) {
            List<Pawn> pawns = new List<Pawn>();
            foreach (EntityId id in nation.getMemberIds()) {
                Pawn pawn = world.entities.getEntity(id) as Pawn;
                if (pawn != null) {
                    pawns.Add(pawn);
                }
            }
            return pawns;
        }

        private static void befriend(Pawn pawn, Player player, int amount) {
            pawn.increaseRelationship(player, amount);
            player.increaseRelationship(pawn, amount);
        }

        [Fact]
        public void testTheMostLoyalMember_Succeeds_AndTheNationGoesOn() {
            Nation own = world.playerNation(4);
            Settlement home = world.settlement(own, 0, 0);
            List<Pawn> members = pawnsOf(own);
            befriend(members[0], world.player, 6);
            befriend(members[1], world.player, 20);
            Pawn heir = members[1];
            string heirName = heir.getName();
            heir.setHomeSettlementId(home.getId());
            heir.setEnergy(70f);
            heir.getInventory().addItem(ItemType.STONE, 9);

            Assert.True(realm.onPlayerDeath());

            Assert.Equal(heirName, world.player.getName());
            Assert.True(heir.isMarkedForDeletion());
            Assert.False(own.isMember(heir.getId()));
            Assert.Equal(world.player.getId(), own.getLeaderId());
            Assert.Equal(3, own.getNumberOfMembers());
            Assert.Equal(1, own.getNumberOfSettlements());
            Assert.Equal(own.getId(), world.player.getNationId());
            Assert.Equal(home.getId(), world.player.getHomeSettlementId());
            Assert.Equal(70f, world.player.getEnergy());
            Assert.Equal(9, world.player.getInventory().getNumItems(ItemType.STONE));
            // a new ruler starts with half the old one's standing
            Assert.Equal(3, PawnRelations.getRelationship(members[0], world.player));

            Assert.False(realm.isRunOver());
            Assert.Single(realm.getLineage());
            Assert.Equal("The founder", realm.getLineage()[0].getName());
            Assert.Equal(own.getName(), realm.getLineage()[0].getNationName());

            SuccessionNotice notice = realm.consumeNotice();
            Assert.NotNull(notice);
            Assert.False(notice.isLineEnded());
            Assert.Equal(heirName, notice.getHeirName());
            Assert.Equal("the most loyal of your people", notice.getReason());
            Assert.Null(realm.consumeNotice());

            string chronicled = world.simulation.getChronicle().getRecent(1)[0].getText();
            Assert.Equal("You died of hunger. " + heirName + ", the most loyal of your people, took up the rule of " + own.getName() + ".", chronicled);
        }

        /** Runs the clock on by ticks, with the realm's update as the simulation would call it. */
        private void pass(int ticks) {
            TickCounter counter = world.simulation.getTickCounter();
            for (int i = 0; i < ticks; i++) {
                counter.increment();
                realm.update(counter.getTotalTicks());
            }
        }

        [Fact]
        public void testOldAge_IsForetold_ThenHandsTheRuleAndBelongingsToTheHeir() {
            Nation own = world.playerNation(3);
            List<Pawn> members = pawnsOf(own);
            befriend(members[1], world.player, 20);
            string heirName = members[1].getName();
            int heirCoins = members[1].getInventory().getNumItems(ItemType.COIN);
            world.player.getInventory().addItem(ItemType.COIN, 40);
            int rulerCoins = world.player.getInventory().getNumItems(ItemType.COIN);

            pass(Realm.OLD_AGE_TICKS - Realm.OLD_AGE_WARNING_TICKS - Realm.LAW_ROUND_TICKS);
            Assert.Empty(realm.getLineage());
            pass(Realm.LAW_ROUND_TICKS);
            Assert.Equal("The founder is growing old. " + heirName + ", the most loyal of your people, is the heir.",
                world.player.getStatus().getStatus());
            Assert.Equal(Realm.OLD_AGE_WARNING_TICKS, realm.getTicksUntilOldAge());

            pass(Realm.OLD_AGE_WARNING_TICKS);
            Assert.Single(realm.getLineage());
            Assert.Equal(Realm.CAUSE_OLD_AGE, realm.getLineage()[0].getCause());
            Assert.Equal(Realm.OLD_AGE_TICKS, realm.getLineage()[0].getEndTick());
            Assert.Equal(heirName, world.player.getName());
            Assert.False(realm.isRunOver());
            // a peaceful end: the old ruler's purse goes with the crown
            Assert.Equal(rulerCoins + heirCoins, world.player.getInventory().getNumItems(ItemType.COIN));
            Assert.Equal("You died of old age. " + heirName + ", the most loyal of your people, took up the rule of " + own.getName() + ".",
                world.simulation.getChronicle().getRecent(1)[0].getText());
            // the new reign starts its own years
            Assert.Equal(Realm.OLD_AGE_TICKS, realm.getTicksUntilOldAge());
        }

        [Fact]
        public void testOldAge_WaitsForAnHeir_AndNeverEndsTheRun() {
            Nation own = world.playerNation(1);
            pass(Realm.OLD_AGE_TICKS + Realm.LAW_ROUND_TICKS);
            Assert.Empty(realm.getLineage());
            Assert.False(realm.isRunOver());

            world.addMember(own);
            pass(Realm.LAW_ROUND_TICKS);
            Assert.Single(realm.getLineage());
            Assert.Equal(Realm.CAUSE_OLD_AGE, realm.getLineage()[0].getCause());
        }

        [Fact]
        public void testANationlessWanderer_DoesNotAge() {
            pass(Realm.OLD_AGE_TICKS + Realm.LAW_ROUND_TICKS);
            Assert.Empty(realm.getLineage());
        }

        [Fact]
        public void testAbdication_HandsTheRuleToTheHeir() {
            Nation own = world.playerNation(2);
            string heirName = pawnsOf(own)[0].getName();
            int heirWood = pawnsOf(own)[0].getInventory().getNumItems(ItemType.WOOD);
            world.player.getInventory().addItem(ItemType.WOOD, 7);
            int rulerWood = world.player.getInventory().getNumItems(ItemType.WOOD);

            realm.abdicate();

            Assert.Equal(heirName, world.player.getName());
            Assert.Equal(world.player.getId(), own.getLeaderId());
            Assert.Equal(Realm.CAUSE_ABDICATION, realm.getLineage()[0].getCause());
            Assert.Equal(rulerWood + heirWood, world.player.getInventory().getNumItems(ItemType.WOOD));
            Assert.NotNull(realm.consumeNotice());
            Assert.StartsWith("You gave up the rule. " + heirName, world.simulation.getChronicle().getRecent(1)[0].getText());
        }

        [Fact]
        public void testAbdication_IsRefused_WithoutAnHeirOrANationToRule() {
            Assert.Equal("Only the ruler of a nation can abdicate.", realm.abdicate());
            Nation own = world.playerNation(1);
            Assert.Equal("No one in " + own.getName() + " was left to take your place.", realm.abdicate());
            Assert.Empty(realm.getLineage());
            Assert.Equal("Player", world.player.getName());
        }

        [Fact]
        public void testFamily_ComesBeforeLoyalty() {
            Nation own = world.playerNation(3);
            List<Pawn> members = pawnsOf(own);
            world.player.rename("Ada Smith");
            members[0].rename("Cy Jones");
            befriend(members[0], world.player, 40);
            members[1].rename("Bob Smith");

            realm.onPlayerDeath();

            Assert.Equal("Bob Smith", world.player.getName());
            Assert.Equal("of your family, the Smiths", realm.getNotice().getReason());
            Assert.Equal("Ada Smith", realm.getLineage()[0].getName());
        }

        [Fact]
        public void testTheHeirsFamily_WarmToTheNewRuler() {
            Nation own = world.playerNation(3);
            List<Pawn> members = pawnsOf(own);
            members[0].rename("Bob Smith");
            befriend(members[0], world.player, 10);
            members[1].rename("Dot Smith");

            realm.onPlayerDeath();

            Assert.Equal("Bob Smith", world.player.getName());
            Assert.Equal(Succession.FAMILY_LOYALTY, PawnRelations.getRelationship(members[1], world.player));
        }

        [Fact]
        public void testAStarvingMember_IsPassedOver() {
            Nation own = world.playerNation(3);
            List<Pawn> members = pawnsOf(own);
            befriend(members[0], world.player, 50);
            members[0].setEnergy(0f);

            realm.onPlayerDeath();

            Assert.Equal(members[1].getName(), world.player.getName());
        }

        [Fact]
        public void testWithNoHeir_TheRunEnds() {
            Nation own = world.playerNation(1);

            Assert.True(realm.onPlayerDeath());

            Assert.True(realm.isRunOver());
            Assert.Single(realm.getLineage());
            SuccessionNotice notice = realm.consumeNotice();
            Assert.True(notice.isLineEnded());
            Assert.Null(notice.getHeirName());
            Assert.Equal("No one in " + own.getName() + " was left to take your place.", notice.getReason());
            // the end of the line is handed over every time
            Assert.NotNull(realm.consumeNotice());
            Assert.Equal("You died of hunger, and with no heir in " + own.getName() + " your line ended.",
                world.simulation.getChronicle().getRecent(1)[0].getText());
        }

        [Fact]
        public void testANationlessWanderer_RisesAgainAsBefore() {
            Assert.False(realm.onPlayerDeath());

            world.player.setEnergy(0f);
            world.simulation.fixedUpdate(1f / 50f);

            Assert.Equal(1, world.simulation.getNumPlayerDeaths());
            Assert.Equal(100f, world.player.getEnergy(), 1);
            Assert.Empty(realm.getLineage());
            Assert.Null(realm.consumeNotice());
            Assert.Equal(1, RealmTestHelp.count(world.simulation.getEventRepository(), EventType.PlayerDeath));
        }

        [Fact]
        public void testDyingInTheWorld_HandsTheRuleOn_OnceATick() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            DiplomacyShowcase.Staged staged = DiplomacyShowcase.stage(simulation);
            Player player = simulation.getPlayer();
            int members = staged.playerNation.getNumberOfMembers();
            player.setEnergy(0.0001f);

            simulation.fixedUpdate(1f / 50f);

            Assert.Equal(1, simulation.getNumPlayerDeaths());
            Assert.NotEqual("Player", player.getName());
            Assert.Equal(members - 1, staged.playerNation.getNumberOfMembers());
            Assert.Equal(player.getId(), staged.playerNation.getLeaderId());
            Assert.Equal(0, RealmTestHelp.count(simulation.getEventRepository(), EventType.PlayerDeath));
            Assert.NotNull(simulation.getRealm().consumeNotice());
        }

        [Fact]
        public void testAfterTheLineEnds_DeathIsNotCountedAgain() {
            world.playerNation(1);
            world.player.setEnergy(0f);

            for (int i = 0; i < 5; i++) {
                world.simulation.fixedUpdate(1f / 50f);
            }

            Assert.True(realm.isRunOver());
            Assert.Equal(1, world.simulation.getNumPlayerDeaths());
        }

        [Fact]
        public void testAMerchantHeir_HandsTheirStall_ToAPlayerWhoDidNotLead() {
            Nation theirs = world.pawnNation("Aland", 2);
            Settlement town = world.settlement(theirs, 0, 0);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());
            Pawn merchant = pawnsOf(theirs)[1];
            befriend(merchant, world.player, 30);
            town.getMarket().createStall();
            List<Stall> stalls = town.getMarket().getStalls();
            Stall stall = stalls[stalls.Count - 1];
            stall.setOwnerId(merchant.getId());
            theirs.setRole(merchant.getId(), NationRole.MERCHANT);

            realm.onPlayerDeath();

            Assert.Equal(merchant.getName(), world.player.getName());
            Assert.Same(world.player.getId(), stall.getOwnerId());
            Assert.Equal(NationRole.MERCHANT, theirs.getRole(world.player.getId()));
            Assert.Equal("took your place in Aland.", world.simulation.getChronicle().getRecent(1)[0].getText().Substring(
                world.simulation.getChronicle().getRecent(1)[0].getText().Length - "took your place in Aland.".Length));
        }

        /**
        * A player who is only a member of a pawn-led nation is never
        * succeeded by its leader: the leader keeps their crown, so the nation
        * is never left pointing at a leader who has left the world.
        */
        [Fact]
        public void testTheLeaderOfTheNation_IsNeverAMembersHeir() {
            Nation theirs = world.pawnNation("Aland", 2);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());
            List<Pawn> members = pawnsOf(theirs);
            Pawn leader = members[0];
            Pawn other = members[1];
            Assert.Same(leader.getId(), theirs.getLeaderId());
            befriend(leader, world.player, 50);

            realm.onPlayerDeath();

            Assert.Equal(other.getName(), world.player.getName());
            Assert.False(leader.isMarkedForDeletion());
            Assert.Same(leader.getId(), theirs.getLeaderId());
            Assert.True(theirs.isMember(theirs.getLeaderId()));
            Assert.Equal(NationRole.LEADER, theirs.getRole(theirs.getLeaderId()));
        }

        [Fact]
        public void testWithOnlyTheLeaderLeft_AMembersLineEnds() {
            Nation theirs = world.pawnNation("Aland", 1);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());
            Pawn leader = pawnsOf(theirs)[0];

            Assert.True(realm.onPlayerDeath());

            Assert.True(realm.isRunOver());
            Assert.False(leader.isMarkedForDeletion());
            Assert.Same(leader.getId(), theirs.getLeaderId());
        }

        /** Even handed a nation's leader as heir, crowning keeps the nation led by someone still in the world. */
        [Fact]
        public void testCrowningTheLeader_HandsTheLeadershipToThePlayer() {
            Nation theirs = world.pawnNation("Aland", 2);
            theirs.addMember(world.player.getId());
            world.player.setNationId(theirs.getId());
            Pawn leader = pawnsOf(theirs)[0];

            Succession.crown(leader, theirs, world.player, world.entities, world.diplomacy, new GameConfig());

            Assert.True(leader.isMarkedForDeletion());
            Assert.Equal(world.player.getId(), theirs.getLeaderId());
            Assert.Equal(NationRole.LEADER, theirs.getRole(world.player.getId()));
        }
    }
}
