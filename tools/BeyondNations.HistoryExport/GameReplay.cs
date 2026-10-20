namespace beyondnations.historyexport {

    /**
    * The replay the game's own Share > Export replay writes (RunRecorder and
    * ReplayUsdaWriter in Core), for a seeded world run headless. CI exports
    * one with `--game-replay` and runs usd-recorder's `replay validate` on
    * it, so the in-game export is held to the same usd-replay/1 contract as
    * tools/history-to-replay.py's (docs/CI-CD.md, docs/SHARING.md).
    */
    public static class GameReplay {

        /** Runs seed for ticks and returns the .usda Share > Export replay would write at the end. */
        public static string export(int seed, int ticks, int ticksPerSecond, int interval, string simVersion) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(seed);
            config.setTicksPerSecond(ticksPerSecond);
            Simulation simulation = new Simulation(config);
            RunRecorder recorder = interval > 0 ? new RunRecorder(interval, RunRecorder.DefaultMaxSamples) : new RunRecorder();
            recorder.begin(simulation);
            float dt = 1f / ticksPerSecond;
            for (int i = 0; i < ticks; i++) {
                simulation.fixedUpdate(dt);
                recorder.observe(simulation);
            }
            return ReplayUsdaWriter.write(recorder, simulation, simVersion);
        }
    }
}
