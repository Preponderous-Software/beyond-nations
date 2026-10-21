using System.Collections.Generic;
using Xunit;

using beyondnations;
using beyondnations.desktop.ui;

namespace beyondnationstests.desktop.ui {

    /**
    * A bandit camp the player barred the gates against stays on the command
    * bar (a tap target, so it works with the touch controls too) until it
    * is dealt with, and the button opens the world event sheet on it.
    */
    public class TestBanditCommands {

        private static List<WorldCommand> commands(Simulation simulation, string label) {
            return WorldCommandBar.getCommands(simulation).FindAll(c => c.getLabel() == label);
        }

        [Fact]
        public void testAWaitedOutCamp_HasAButton_ThatOpensTheChoiceAgain() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            DiplomacyShowcase.Staged s = DiplomacyShowcase.stage(simulation);
            string label = WorldCommandBar.dealWithBanditsLabel(s.playerSettlement);
            Assert.Empty(commands(simulation, label));

            WorldEvents events = simulation.getWorldEvents();
            events.addEffect(new WorldEffect(WorldEffectKind.BANDITS, s.playerNation.getId(), s.playerSettlement.getId(), 0, 100000, 100000));
            Assert.Single(commands(simulation, label));
            // a pawn-led nation's camp is not the player's
            events.addEffect(new WorldEffect(WorldEffectKind.BANDITS, s.friendly.getId(), s.friendlySettlement.getId(), 0, 100000, 100000));
            Assert.Empty(commands(simulation, WorldCommandBar.dealWithBanditsLabel(s.friendlySettlement)));

            commands(simulation, label)[0].execute();
            Assert.True(events.isAwaitingPlayer());
            WorldEventSheet.View view = WorldEventSheet.getView(simulation);
            Assert.Equal("Bandit camp", view.Title);
            Assert.Equal("Keep the gates barred", view.Rows[2].Label);
            // the sheet is up: the bar does not offer it twice
            Assert.Empty(commands(simulation, label));

            events.choose(simulation, 0);
            Assert.False(events.hasBandits(s.playerSettlement.getId()));
            Assert.Empty(commands(simulation, label));
        }
    }
}
