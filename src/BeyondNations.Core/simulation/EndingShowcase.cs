namespace beyondnations {

    /**
    * Opens a world at the end of a run, so the end-of-run screen can be
    * seen (and screenshotted) without hours of play: the lived-in town of
    * SettlementShowcase, a minute of play so the chronicle has something to
    * tell, and then the run is put at the Kingdom era with a Prosperity
    * victory, or at the Town era with a collapse, the way EraTracker records
    * them in play.
    *
    * Used by the host's --stage-victory and --stage-defeat flags and by
    * tests. Deterministic for a given world seed.
    */
    public static class EndingShowcase {
        public const int TicksPlayed = 3000;

        public static void stage(Simulation simulation, bool victory) {
            SettlementShowcase.stage(simulation);
            for (int i = 0; i < TicksPlayed; i++) {
                simulation.fixedUpdate(1f / simulation.getTickCounter().getTicksPerSecond());
            }
            simulation.getEraTracker().stageEnding(simulation,
                victory ? Era.KINGDOM : Era.TOWN,
                victory ? RunOutcome.PROSPERITY : RunOutcome.DEFEAT);
        }
    }
}
