using Xunit;

using beyondnations;

namespace beyondnationstests {

    /**
    * Share > Export history: the chronicle as "The History of <nation>" in
    * Markdown (docs/SHARING.md).
    */
    public class TestChronicleExport {

        private static Simulation world(int seed, int ticks) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            Simulation simulation = new Simulation(config);
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(1f / 50f);
            }
            return simulation;
        }

        [Fact]
        public void testAWanderersHistoryTellsTheChronicleUnderDayHeadings() {
            Simulation simulation = world(42, 2400);
            Assert.True(simulation.getChronicle().getCount() > 0, "seed 42 should have founded a nation by tick 2400");

            string md = ChronicleExport.write(simulation, SeedLink.build(42));

            Assert.StartsWith("# The History of a Wanderer's World\n", md);
            Assert.Contains("- World seed: `42`", md);
            Assert.Contains("- Play this world: https://beyond-nations.play.danielstephenson.dev/?seed=42", md);
            Assert.Contains("## The nations today", md);
            Assert.Contains("### Day 1", md);
            foreach (ChronicleEntry entry in simulation.getChronicle().getEntries()) {
                Assert.Contains(entry.getText(), md);
            }
        }

        [Fact]
        public void testThePlayersNationNamesTheHistory() {
            Simulation simulation = world(42, 10);
            Player player = simulation.getPlayer();
            Nation nation = new Nation("Ironvale", player.getId(), simulation.getRandom());
            simulation.getNationRepository().addNation(nation);
            nation.addMember(player.getId());
            player.setNationId(nation.getId());

            string md = ChronicleExport.write(simulation, null);

            Assert.StartsWith("# The History of Ironvale\n", md);
            Assert.Contains("- You: of Ironvale", md);
            Assert.Contains("**Ironvale**", md);
            Assert.Contains("(your nation)", md);
            Assert.DoesNotContain("Play this world", md);
        }

        [Fact]
        public void testAnEmptyChronicleSaysSo() {
            Simulation simulation = world(78, 1);
            if (simulation.getChronicle().getCount() == 0) {
                Assert.Contains("Nothing worth telling has happened yet.", ChronicleExport.write(simulation, null));
            }
            Assert.Equal("beyond-nations-seed-78-history.md", ChronicleExport.fileName(78));
        }
    }
}
