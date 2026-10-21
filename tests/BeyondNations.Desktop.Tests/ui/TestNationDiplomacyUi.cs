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
            Assert.Contains("Propose Alliance to " + name, offered);
            Assert.Contains("Denounce " + name, offered);
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
