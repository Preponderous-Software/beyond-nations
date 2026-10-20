using System;

namespace beyondnations.desktop.ui {

    /**
    * Sharing a world (docs/SHARING.md), for both hosts: the seed the New
    * World screen is about to build, the recording of the world in play, and
    * the Share screen's three actions -- hand over the link, export the run
    * as a usd-replay/1 replay, export the chronicle as a history -- with the
    * line that says how each went.
    *
    * The host calls beginWorld when a world is created or loaded and
    * onFixedStep after every fixed step, as it does for SaveSession. The
    * platform (clipboard, files, downloads) is behind ISharePlatform, so this
    * class, like the screens, runs in both builds and in tests.
    */
    public class ShareSession {
        private readonly ISharePlatform platform;
        private readonly string simVersion;
        private readonly Random seedRolls;
        private RunRecorder recorder = new RunRecorder();
        private string seedText = "";
        private string feedback;
        private bool feedbackError;

        public ShareSession(ISharePlatform platform, string simVersion, string initialSeedText)
            : this(platform, simVersion, initialSeedText, new Random()) {
        }

        /** seedRolls is where the Random button's seeds come from; a test passes a seeded one. */
        public ShareSession(ISharePlatform platform, string simVersion, string initialSeedText, Random seedRolls) {
            this.platform = platform;
            this.simVersion = simVersion;
            this.seedRolls = seedRolls;
            this.seedText = initialSeedText ?? "";
        }

        public ISharePlatform getPlatform() { return platform; }
        public RunRecorder getRecorder() { return recorder; }

        /** Replaces the recorder, for tests that want a short interval. */
        public void setRecorder(RunRecorder recorder) { this.recorder = recorder; }

        // --- the New World screen -------------------------------------------

        public string getSeedText() { return seedText; }

        public void setSeedText(string text) {
            seedText = text ?? "";
        }

        /** The seed the New World screen would build: WorldSeed.fromText of the field, 0 for a fresh one. */
        public int getChosenSeed() {
            return WorldSeed.fromText(seedText);
        }

        /** The Random button: a fresh seed, written into the field so it can be read and shared before Begin. */
        public void rollSeed() {
            seedText = WorldSeed.format(WorldSeed.roll(seedRolls));
        }

        /** Asks the platform for the seed text (phones). Cancelling keeps what was there. */
        public void promptForSeed() {
            string typed = platform.promptText("World seed: a number or any word. Leave it empty for a fresh world.", seedText);
            if (typed != null) {
                seedText = typed.Trim();
            }
        }

        /**
        * What the New World screen says under the field: which world Begin
        * will build.
        */
        public string describeChosenSeed() {
            string trimmed = seedText.Trim();
            if (trimmed.Length == 0 || getChosenSeed() == WorldSeed.Random) {
                return "No seed: a fresh world, with a seed of its own.";
            }
            if (WorldSeed.isNumber(trimmed)) {
                return "Seed " + WorldSeed.format(getChosenSeed()) + ".";
            }
            return "\"" + trimmed + "\" is seed " + WorldSeed.format(getChosenSeed()) + ".";
        }

        // --- the world in play ----------------------------------------------

        public void beginWorld(Simulation simulation) {
            feedback = null;
            feedbackError = false;
            recorder.begin(simulation);
        }

        public void onFixedStep(Simulation simulation) {
            recorder.observe(simulation);
        }

        public string getFeedback() { return feedback; }
        public bool isFeedbackError() { return feedbackError; }

        public void clearFeedback() {
            feedback = null;
            feedbackError = false;
        }

        // --- the Share screen -----------------------------------------------

        public static int seedOf(Simulation simulation) {
            return simulation.getRandom().getSeed();
        }

        public string linkFor(Simulation simulation) {
            return SeedLink.build(seedOf(simulation));
        }

        /**
        * Carries out one of the Share screen's actions; true when action was
        * one, so the host need not look at it again.
        */
        public bool handle(UiAction action, Simulation simulation) {
            switch (action) {
                case UiAction.ShareLink:
                    if (simulation != null) shareLink(simulation);
                    return true;
                case UiAction.ExportReplay:
                    if (simulation != null) exportReplay(simulation);
                    return true;
                case UiAction.ExportChronicle:
                    if (simulation != null) exportChronicle(simulation);
                    return true;
                case UiAction.RollSeed:
                    rollSeed();
                    return true;
                default:
                    return false;
            }
        }

        public void shareLink(Simulation simulation) {
            string url = linkFor(simulation);
            try {
                platform.shareLink(url, "Beyond Nations, seed " + WorldSeed.format(seedOf(simulation)), (message, error) => report(message, error));
            } catch (Exception exception) {
                report("Could not share the link: " + exception.Message, true);
            }
        }

        public void exportReplay(Simulation simulation) {
            try {
                string usda = ReplayUsdaWriter.write(recorder, simulation, simVersion);
                string name = ReplayUsdaWriter.fileName(seedOf(simulation), simulation.getTickCounter().getTotalTicks());
                report(platform.saveFile(name, "model/vnd.usda", usda), false);
            } catch (Exception exception) {
                Log.error("replay export failed: " + exception);
                report("Could not export the replay: " + exception.Message, true);
            }
        }

        public void exportChronicle(Simulation simulation) {
            try {
                string markdown = ChronicleExport.write(simulation, linkFor(simulation));
                report(platform.saveFile(ChronicleExport.fileName(seedOf(simulation)), "text/markdown", markdown), false);
            } catch (Exception exception) {
                Log.error("history export failed: " + exception);
                report("Could not export the history: " + exception.Message, true);
            }
        }

        private void report(string message, bool error) {
            feedback = message;
            feedbackError = error;
            Log.info("share: " + message);
        }
    }
}
