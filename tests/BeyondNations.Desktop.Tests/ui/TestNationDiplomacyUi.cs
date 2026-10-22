using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.boxes;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The nations screen, the diplomacy buttons on the command bar and the
    * land line in the player box, read off the --stage-diplomacy world.
    */
    // reads text that names keys or buttons by TouchLayout, which another test may switch on
    [Collection("TouchLayout")]
    public class TestNationDiplomacyUi {

        private static Simulation staged(out DiplomacyShowcase.Staged result) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            result = DiplomacyShowcase.stage(simulation);
            return simulation;
        }

        private static List<string> labels(Simulation simulation) {
            List<string> result = new List<string>();
            foreach (WorldCommand command in WorldCommandBar.getCommands(simulation)) {
                result.Add(command.getLabel());
            }
            return result;
        }

        [Fact]
        public void testNationScreen_ListsEveryOtherNationWithSizeAndRelation() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            List<NationScreen.Row> rows = NationScreen.getRows(simulation);

            Assert.Equal(2, rows.Count);
            Assert.Same(result.hostile, rows[0].nation);
            // the staged hostile nation also takes in the wild pawns around the stage
            Assert.StartsWith(result.hostile.getName() + ": " + result.hostile.getNumberOfMembers() + " members, 1 village. Hostile (", rows[0].text);
            Assert.True(result.hostile.getNumberOfMembers() >= DiplomacyShowcase.HostileMembers);
            Assert.EndsWith(", neighbour", rows[0].text);
            Assert.StartsWith(result.friendly.getName() + ": 3 members, 1 camp. Friendly (", rows[1].text);
            Assert.True(rows[0].canAct);
        }

        [Fact]
        public void testNationScreen_HeaderNamesTheSiege() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            List<string> header = NationScreen.getHeaderLines(simulation);

            Assert.Equal("Your nation: " + result.playerNation.getName() + ", 3 members, 1 village. You lead it.", header[0]);
            Assert.Contains("Your village: Besieged by " + result.hostile.getName() + " (2/6)!", header);
        }

        [Fact]
        public void testNationScreen_WithoutANation_SaysHowToGetOne() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);

            Assert.Equal(new List<string> { "You belong to no nation. Press N to found one, or J to join one." }, NationScreen.getHeaderLines(simulation));
            Assert.Empty(NationScreen.getRows(simulation));
        }

        [Fact]
        public void testCommandBar_InForeignLand_OffersDiplomacyWithItsOwner() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            Player player = simulation.getPlayer();
            player.setPosition(result.hostileSettlement.getPosition() + new Vector3(30, 0, 0));
            player.getInventory().addItem(ItemType.COIN, 50);

            List<string> offered = labels(simulation);

            string name = result.hostile.getName();
            Assert.Contains("Gift " + name + " 25 Coins", offered);
            Assert.Contains("Denounce " + name, offered);
            // Hostile: an alliance cannot be accepted, so it is not offered; peace and war are (playtest 0.4.1 s04)
            Assert.DoesNotContain("Propose Alliance to " + name, offered);
            Assert.Contains("Offer Peace to " + name, offered);
            Assert.Contains("Declare War on " + name, offered);
        }

        [Fact]
        public void testCommandBar_InFriendlyLand_OffersAllianceAndWarButNoPeace() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            simulation.getPlayer().setPosition(result.friendlySettlement.getPosition() + new Vector3(20, 0, 0));

            List<string> offered = labels(simulation);

            string name = result.friendly.getName();
            Assert.Contains("Propose Alliance to " + name, offered);
            Assert.Contains("Declare War on " + name, offered);
            Assert.DoesNotContain("Offer Peace to " + name, offered);
        }

        [Fact]
        public void testCommandBar_AtWar_OffersPeaceNotWarNorAlliance() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            simulation.getRealm().declareWar(result.playerNation, result.friendly);
            simulation.getPlayer().setPosition(result.friendlySettlement.getPosition() + new Vector3(20, 0, 0));

            List<string> offered = labels(simulation);

            string name = result.friendly.getName();
            Assert.Contains("Offer Peace to " + name, offered);
            Assert.DoesNotContain("Declare War on " + name, offered);
            Assert.DoesNotContain("Propose Alliance to " + name, offered);
        }

        [Fact]
        public void testCommandBarAndNationScreen_OfferTheSameActions() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            simulation.getPlayer().setPosition(result.hostileSettlement.getPosition() + new Vector3(30, 0, 0));

            NationScreen.Row row = NationScreen.getRows(simulation).Find(r => r.nation == result.hostile);
            List<string> offered = labels(simulation);

            string name = result.hostile.getName();
            Assert.Equal(row.canProposeAlliance, offered.Contains("Propose Alliance to " + name));
            Assert.Equal(row.canOfferPeace, offered.Contains("Offer Peace to " + name));
            Assert.Equal(row.canDeclareWar, offered.Contains("Declare War on " + name));
        }

        [Fact]
        public void testProposeAlliance_ToAHostileNation_SaysPeaceComesFirst() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);

            new ProposeAllianceCommand(simulation.getNationRepository(), simulation.getRealm()).execute(simulation.getPlayer(), result.hostile);

            Assert.StartsWith(result.hostile.getName() + " will not talk of an alliance while relations are Hostile", simulation.getPlayer().getStatus().getStatus());
            Assert.EndsWith("Offer peace or send tribute first.", simulation.getPlayer().getStatus().getStatus());
        }

        [Fact]
        public void testNationScreen_NeighboursAndEnemiesFirst_ThenByDistance() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            NationRepository nations = simulation.getNationRepository();
            // a dozen far-off nations founded before the real neighbours used to push them off the screen
            List<Nation> faraway = new List<Nation>();
            for (int i = 0; i < 12; i++) {
                Nation nation = new Nation("Faraway" + i, new EntityId(), simulation.getRandom());
                nations.addNation(nation);
                Settlement settlement = new Settlement(new Vector3(3000 + i * 400, 0, 3000), nation.getId(), nation.getColor(), nation.getName(), simulation.getRandom());
                simulation.getEntityRepository().addEntity(settlement);
                nation.addSettlement(settlement.getId());
                faraway.Add(nation);
            }

            List<NationScreen.Row> rows = NationScreen.getRows(simulation);

            Assert.Equal(14, rows.Count);
            Assert.Same(result.hostile, rows[0].nation);
            Assert.Same(result.friendly, rows[1].nation);
            // the rest nearest first
            for (int i = 0; i < 12; i++) {
                Assert.Same(faraway[i], rows[2 + i].nation);
            }
        }

        [Fact]
        public void testNationScreen_CountLine_SaysWhatIsShownAndHowToSeeMore() {
            Assert.Equal("Nations 1-7 of 20, neighbours and enemies first. Scroll for more.", NationScreen.describeShown(0, 7, 20, false));
            Assert.Equal("Nations 8-14 of 20, neighbours and enemies first. Drag or tap Up/Down for more.", NationScreen.describeShown(7, 7, 20, true));
            Assert.Equal("2 other nations, neighbours and enemies first.", NationScreen.describeShown(0, 2, 2, false));
            Assert.Equal(0, NationScreen.clampFirstRow(-3, 20, 7));
            Assert.Equal(13, NationScreen.clampFirstRow(40, 20, 7));
            Assert.Equal(0, NationScreen.clampFirstRow(5, 3, 7));
        }

        [Fact]
        public void testCommandBar_OnOwnLand_OffersNoDiplomacy() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            simulation.getPlayer().setPosition(result.playerSettlement.getPosition() + new Vector3(30, 0, 0));

            foreach (string label in labels(simulation)) {
                Assert.DoesNotContain("Denounce", label);
            }
        }

        [Fact]
        public void testPlayerBox_SaysWhoseLandThePlayerIsIn() {
            DiplomacyShowcase.Staged result;
            Simulation simulation = staged(out result);
            Player player = simulation.getPlayer();
            player.setPosition(result.hostileSettlement.getPosition() + new Vector3(30, 0, 0));

            List<string> lines = new PlayerInfoBox(player, simulation.getNationRepository(), simulation.getEntityRepository()).getLines();

            Assert.Equal("Land: " + result.hostile.getName() + " (Hostile)", lines[lines.Count - 1]);
        }
    }
}
