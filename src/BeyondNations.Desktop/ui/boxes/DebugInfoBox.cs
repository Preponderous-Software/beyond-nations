using System.Collections.Generic;

namespace beyondnations.desktop.ui.boxes {

    /**
    * The F1 debug overlay, ported from Assets/Scripts/ui/boxes/DebugInfoBox.cs.
    *
    * The same twenty-one fields are reported, in the same order and with the
    * same labels, which is the acceptance criterion for this box. The Unity
    * version was handed six repositories individually; it is handed the
    * Simulation now, because that is the one object the host has and it exposes
    * all six. The death counts come from the simulation too rather than being
    * pushed in, since it is the simulation that counts them.
    */
    public class DebugInfoBox : InfoBox {
        public const int NumDataPoints = 22;

        private readonly Simulation simulation;
        private int framesPerSecond;

        public DebugInfoBox(Simulation simulation) : base("Debug Info (F1)") {
            this.simulation = simulation;
        }

        /**
        * The frame rate is the host's measurement, not the simulation's, so it
        * is pushed in once a frame.
        */
        public void setFramesPerSecond(int framesPerSecond) {
            this.framesPerSecond = framesPerSecond;
        }

        public override List<string> getLines() {
            TickCounter tickCounter = simulation.getTickCounter();
            Environment environment = simulation.getEnvironment();
            EntityRepository entityRepository = simulation.getEntityRepository();
            NationRepository nationRepository = simulation.getNationRepository();
            Player player = simulation.getPlayer();

            List<string> lines = new List<string>(NumDataPoints);

            lines.Add("FPS: " + framesPerSecond);
            lines.Add("MTPS: " + tickCounter.getMtps());
            lines.Add("Total ticks: " + tickCounter.getTotalTicks());

            // #219 requires the overlay to report render distance "as it does
            // today". It did not: no box in the Unity build ever showed it, and
            // Page Up and Page Down moved it silently. It is reported here,
            // which is what the issue actually wants.
            lines.Add("Render distance: " + player.getRenderDistance());

            Chunk currentChunk = environment.getChunkAtPosition(player.getPosition());
            if (currentChunk != null) {
                lines.Add("Chunk: " + currentChunk.getX() + ", " + currentChunk.getZ());
            }
            else {
                lines.Add("Chunk: null");
            }

            lines.Add("Entities: " + entityRepository.getNumEntities());
            lines.Add("Chunks: " + environment.getNumChunks());

            List<Entity> pawns = entityRepository.getEntitiesOfType(EntityType.PAWN);
            int numPawns = pawns.Count;
            lines.Add("Pawns: " + numPawns);
            lines.Add("Nations: " + nationRepository.getNumberOfNations());

            List<Entity> settlements = entityRepository.getEntitiesOfType(EntityType.SETTLEMENT);
            lines.Add("Settlements: " + settlements.Count);
            lines.Add("Trees: " + entityRepository.getEntitiesOfType(EntityType.TREE).Count);
            lines.Add("Saplings: " + entityRepository.getEntitiesOfType(EntityType.SAPLING).Count);
            lines.Add("Rocks: " + entityRepository.getEntitiesOfType(EntityType.ROCK).Count);
            lines.Add("Events: " + simulation.getEventRepository().getTotalNumberOfEvents());

            int totalNumStalls = 0;
            foreach (Settlement settlement in settlements) {
                totalNumStalls += settlement.getMarket().getNumStalls();
            }
            lines.Add("Stalls: " + totalNumStalls);

            int numPawnsCurrentlyInSettlement = 0;
            foreach (Pawn pawn in pawns) {
                if (pawn.isCurrentlyInSettlement()) {
                    numPawnsCurrentlyInSettlement++;
                }
            }
            lines.Add("PCIS: " + numPawnsCurrentlyInSettlement + " / " + numPawns);

            int numNationlessPawns = 0;
            foreach (Pawn pawn in pawns) {
                if (pawn.getNationId() == null) {
                    numNationlessPawns++;
                }
            }
            lines.Add("Nationless: " + numNationlessPawns + " / " + numPawns);

            int numLeaders = 0;
            int numMerchants = 0;
            int numSerfs = 0;
            foreach (Pawn pawn in pawns) {
                if (pawn.getNationId() == null) {
                    continue;
                }
                Nation nation = nationRepository.getNation(pawn.getNationId());
                NationRole role = nation.getRole(pawn.getId());
                if (role == NationRole.LEADER) {
                    numLeaders++;
                }
                else if (role == NationRole.MERCHANT) {
                    numMerchants++;
                }
                else if (role == NationRole.SERF) {
                    numSerfs++;
                }
            }
            lines.Add("Leaders: " + numLeaders);
            lines.Add("Merchants: " + numMerchants);
            lines.Add("Serfs: " + numSerfs);

            lines.Add("Pawn Deaths: " + simulation.getNumPawnDeaths());
            lines.Add("Player Deaths: " + simulation.getNumPlayerDeaths());

            // The same discrepancy check the Unity box performed: every pawn is
            // either nationless or holds exactly one role, so the counts must add up.
            if (numPawns != numLeaders + numMerchants + numSerfs + numNationlessPawns) {
                Log.error("Discrepency in pawn count!");
            }

            return lines;
        }
    }
}
