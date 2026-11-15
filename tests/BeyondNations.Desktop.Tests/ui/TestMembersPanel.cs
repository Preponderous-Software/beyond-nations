using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The Members tab of the nations screen (#358): who is in the player's
    * nation, their role, home, loyalty and traits, and which of them is the
    * heir. Read off the --stage-diplomacy world, with no window.
    */
    // reads text that names keys by TouchLayout, which another test may switch on
    [Collection("TouchLayout")]
    public class TestMembersPanel {

        private static Simulation staged(out DiplomacyShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = DiplomacyShowcase.stage(simulation);
            return simulation;
        }

        private static Pawn pawnOf(Simulation simulation, MembersPanel.Row row) {
            return (Pawn) simulation.getEntityRepository().getEntity(row.id);
        }

        [Fact]
        public void testRows_ThePlayerFirst_ThenEveryMemberOldestFirst_WithRoleHomeLoyaltyAndTraits() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            string home = result.playerSettlement.getDisplayName();

            List<MembersPanel.Row> rows = MembersPanel.getRows(simulation);

            Assert.Equal(1 + DiplomacyShowcase.PlayerPawns, rows.Count);
            Assert.True(rows[0].isPlayer);
            Assert.False(rows[0].isHeir);
            Assert.Equal("You (Player) - Leader, " + home, rows[0].title);
            List<EntityId> members = result.playerNation.getMemberIds();
            for (int i = 1; i < rows.Count; i++) {
                Pawn pawn = pawnOf(simulation, rows[i]);
                Assert.Equal(members[i], rows[i].id);
                Assert.Equal("Serf", rows[i].role);
                Assert.Equal(home, rows[i].settlement);
                Assert.Equal(0, rows[i].loyalty);
                Assert.Equal(PawnTraits.describe(pawn), rows[i].traits);
                Assert.Equal("Loyalty: Stranger (0). " + PawnTraits.describe(pawn) + ".", rows[i].detail);
            }
        }

        [Fact]
        public void testHeir_WithNoOneLoyal_IsTheLongestSworn() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            List<MembersPanel.Row> rows = MembersPanel.getRows(simulation);

            Pawn first = pawnOf(simulation, rows[1]);
            Assert.True(rows[1].isHeir);
            Assert.False(rows[2].isHeir);
            Assert.Equal(MembersPanel.HeirMark + ": " + first.getName() + " - Serf, " + result.playerSettlement.getDisplayName(), rows[1].title);
            Assert.Contains("Your heir: " + first.getName() + ", the longest sworn of your people.", MembersPanel.getLines(simulation));
        }

        [Fact]
        public void testHeir_MovesToTheMostLoyal_WithoutReorderingTheRows() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            List<MembersPanel.Row> before = MembersPanel.getRows(simulation);
            Pawn second = pawnOf(simulation, before[2]);
            second.increaseRelationship(simulation.getPlayer(), 20);

            List<MembersPanel.Row> rows = MembersPanel.getRows(simulation);

            Assert.Equal(before.ConvertAll(r => r.id), rows.ConvertAll(r => r.id));
            Assert.False(rows[1].isHeir);
            Assert.True(rows[2].isHeir);
            Assert.Equal(20, rows[2].loyalty);
            Assert.StartsWith("Loyalty: Friend (20).", rows[2].detail);
            Assert.Contains("Your heir: " + second.getName() + ", the most loyal of your people.", MembersPanel.getLines(simulation));
        }

        [Fact]
        public void testHeir_FamilyComesBeforeTheMostLoyal() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            List<MembersPanel.Row> rows = MembersPanel.getRows(simulation);
            Pawn first = pawnOf(simulation, rows[1]);
            Pawn second = pawnOf(simulation, rows[2]);
            second.increaseRelationship(simulation.getPlayer(), 40);
            first.rename("Ada Marrow");
            simulation.getPlayer().rename("Bran Marrow");

            rows = MembersPanel.getRows(simulation);

            Assert.True(rows[1].isHeir);
            Assert.False(rows[2].isHeir);
            Assert.Equal("You (Bran Marrow) - Leader, " + result.playerSettlement.getDisplayName(), rows[0].title);
            Assert.Contains("Your heir: Ada Marrow, of your family, the Marrows.", MembersPanel.getLines(simulation));
        }

        [Fact]
        public void testHeirMark_AgreesWithWhatSuccessionWouldChoose() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            pawnOf(simulation, MembersPanel.getRows(simulation)[2]).increaseRelationship(simulation.getPlayer(), 7);
            string reason;

            Pawn heir = Succession.chooseHeir(result.playerNation, simulation.getPlayer(), simulation.getEntityRepository(), out reason);

            List<MembersPanel.Row> heirs = MembersPanel.getRows(simulation).FindAll(r => r.isHeir);
            Assert.Single(heirs);
            Assert.Equal(heir.getId(), heirs[0].id);
        }

        [Fact]
        public void testANationOfOne_HasNoHeir_AndSaysTheLineWouldEnd() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(simulation.getPlayer());

            List<MembersPanel.Row> rows = MembersPanel.getRows(simulation);
            List<string> lines = MembersPanel.getLines(simulation);

            Assert.Single(rows);
            Assert.Equal("You (Player) - Leader, no home", rows[0].title);
            Assert.EndsWith(": 1 member. Loyalty is how each feels about you.", lines[0]);
            // and what to do about it (0.8.0 playtest s06)
            Assert.Equal("No heir: if you die now, your line ends. " + MembersPanel.HowToGainMembers, lines[1]);
            Assert.Contains("invite", lines[1]);
            Assert.Equal("When you die, your family comes first, then the most loyal, then the longest sworn.", lines[2]);
        }

        [Fact]
        public void testWithoutANation_TheTabSaysHowToGetOne() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);

            Assert.Empty(MembersPanel.getRows(simulation));
            Assert.Equal(new List<string> { "You belong to no nation, so no one is sworn to you. Found one (N) or join one (J)." },
                MembersPanel.getLines(simulation));
        }

        [Fact]
        public void testDescribeShown_NamesTheMembersOnScreen() {
            Assert.Equal("Members 1-5 of 9. Scroll for more.", MembersPanel.describeShown(0, 5, 9, false));
            Assert.Equal("Member 9 of 9. Drag or tap Up/Down for more.", MembersPanel.describeShown(8, 1, 9, true));
        }

        [Fact]
        public void testTabs_ShowOneAtATime() {
            NationScreen screen = new NationScreen();
            Assert.False(screen.isShowingMembers());
            screen.showMembers(true);
            Assert.True(screen.isShowingMembers());
            Assert.False(screen.isShowingLaws());
            screen.showLaws(true);
            Assert.True(screen.isShowingLaws());
            Assert.False(screen.isShowingMembers());
            Assert.Equal("> Members <", NationScreen.tabLabel(NationScreen.MembersTab, true));
            Assert.Equal("Members", NationScreen.tabLabel(NationScreen.MembersTab, false));
        }

        [Fact]
        public void testOptions_OpenOnTheMembersTab() {
            try {
                GameOptions options = GameOptions.parse(new string[] { "--start-screen", "members" });
                Assert.Equal(ScreenType.NATIONS, options.StartScreen);
                NationScreen screen = new NationScreen();
                Assert.True(screen.isShowingMembers());
                Assert.False(screen.isShowingLaws());
            } finally {
                NationScreen.startOnMembers = false;
            }
        }
    }
}
