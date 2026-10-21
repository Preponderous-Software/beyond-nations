using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

using beyondnations;
using beyondnations.desktop;
using beyondnations.desktop.ui;

namespace beyondnationstests {

    /**
    * ShareSession (docs/SHARING.md): the New World screen's seed, and the
    * Share screen's link and exports, against a stand-in platform.
    */
    public class TestShareSession {

        private sealed class FakePlatform : ISharePlatform {
            public readonly List<string> links = new List<string>();
            public readonly Dictionary<string, string> files = new Dictionary<string, string>();
            public readonly Dictionary<string, string> types = new Dictionary<string, string>();
            public string promptAnswer;
            public bool prompts;
            public string getShareLinkLabel() { return "Copy link"; }
            public void shareLink(string url, string title, Action<string, bool> done) { links.Add(url); done("Link copied: " + url, false); }
            public string saveFile(string fileName, string mimeType, string content) {
                files[fileName] = content;
                types[fileName] = mimeType;
                return "Saved to /exports/" + fileName;
            }
            public bool promptsForText() { return prompts; }
            public string promptText(string message, string current) { return promptAnswer; }
        }

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
        public void testTheNewWorldSeedComesFromTheField() {
            ShareSession share = new ShareSession(new FakePlatform(), "test", "Ironvale");
            Assert.Equal(1450162717, share.getChosenSeed());
            Assert.Equal("\"Ironvale\" is seed 1450162717.", share.describeChosenSeed());

            share.setSeedText("42");
            Assert.Equal(42, share.getChosenSeed());
            Assert.Equal("Seed 42.", share.describeChosenSeed());

            share.setSeedText("  ");
            Assert.Equal(0, share.getChosenSeed());
            Assert.StartsWith("No seed", share.describeChosenSeed());

            Assert.Equal(0, new ShareSession(new FakePlatform(), "test", null).getChosenSeed());
        }

        [Fact]
        public void testRandomFillsTheFieldWithAFreshSeed() {
            ShareSession share = new ShareSession(new FakePlatform(), "test", "", new Random(3));
            Assert.True(share.handle(UiAction.RollSeed, null));
            Assert.True(WorldSeed.isNumber(share.getSeedText()));
            Assert.True(share.getChosenSeed() > 0);
            ShareSession again = new ShareSession(new FakePlatform(), "test", "", new Random(3));
            again.rollSeed();
            Assert.Equal(share.getSeedText(), again.getSeedText());
        }

        [Fact]
        public void testOnAPhoneTheSeedIsAskedForAndCancellingKeepsIt() {
            FakePlatform platform = new FakePlatform { prompts = true, promptAnswer = "  river  " };
            ShareSession share = new ShareSession(platform, "test", "old");
            share.promptForSeed();
            Assert.Equal("river", share.getSeedText());
            platform.promptAnswer = null;
            share.promptForSeed();
            Assert.Equal("river", share.getSeedText());
        }

        [Fact]
        public void testShareLinkHandsOverTheSeedLink() {
            FakePlatform platform = new FakePlatform();
            ShareSession share = new ShareSession(platform, "test", null);
            Simulation simulation = world(42, 1);
            share.beginWorld(simulation);

            Assert.True(share.handle(UiAction.ShareLink, simulation));
            Assert.Equal(new[] { "https://beyond-nations.play.danielstephenson.dev/?seed=42" }, platform.links);
            Assert.Equal("Link copied: https://beyond-nations.play.danielstephenson.dev/?seed=42", share.getFeedback());
            Assert.False(share.isFeedbackError());
        }

        [Fact]
        public void testExportsWriteTheReplayAndTheHistory() {
            FakePlatform platform = new FakePlatform();
            ShareSession share = new ShareSession(platform, "test", null);
            share.setRecorder(new RunRecorder(100, 1000));
            Simulation simulation = world(42, 0);
            share.beginWorld(simulation);
            for (int i = 0; i < 600; i++) {
                simulation.fixedUpdate(1f / 50f);
                share.onFixedStep(simulation);
            }

            Assert.True(share.handle(UiAction.ExportReplay, simulation));
            Assert.Equal("Saved to /exports/beyond-nations-seed-42-tick-600.usda", share.getFeedback());
            string usda = platform.files["beyond-nations-seed-42-tick-600.usda"];
            Assert.Contains("string schema = \"usd-replay/1\"", usda);
            Assert.Contains("string simVersion = \"test\"", usda);
            Assert.Contains("startTimeCode = 0\n", usda);
            Assert.Contains("endTimeCode = 600\n", usda);

            Assert.True(share.handle(UiAction.ExportChronicle, simulation));
            Assert.Equal("Saved to /exports/beyond-nations-seed-42-history.md", share.getFeedback());
            Assert.Equal("text/markdown", platform.types["beyond-nations-seed-42-history.md"]);
            Assert.Contains("?seed=42", platform.files["beyond-nations-seed-42-history.md"]);
        }

        [Fact]
        public void testOtherActionsAreLeftToTheHost() {
            ShareSession share = new ShareSession(new FakePlatform(), "test", null);
            Assert.False(share.handle(UiAction.StartGame, null));
            Assert.False(share.handle(UiAction.BeginWorld, null));
            Assert.False(share.handle(UiAction.None, null));
        }

        [Fact]
        public void testTheDesktopWritesExportsWhereItSays() {
            string directory = Path.Combine(Path.GetTempPath(), "bn-exports-" + Guid.NewGuid().ToString("N"));
            try {
                DesktopSharePlatform platform = new DesktopSharePlatform(null, directory);
                string said = platform.saveFile("a.md", "text/markdown", "# hi\n");
                Assert.Equal("Saved to " + Path.Combine(directory, "a.md"), said);
                Assert.Equal("# hi\n", File.ReadAllText(Path.Combine(directory, "a.md")));
                Assert.EndsWith(Path.Combine("BeyondNations", "Exports"), AppDataPaths.getExportsDirectory());
            } finally {
                if (Directory.Exists(directory)) {
                    Directory.Delete(directory, true);
                }
            }
        }
    }
}
