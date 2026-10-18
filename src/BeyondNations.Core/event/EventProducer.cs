using System;
using System.Numerics;

namespace beyondnations {

    public class EventProducer {
        private EventRepository eventRepository;

        public EventProducer(EventRepository eventRepository) {
            this.eventRepository = eventRepository;
        }

        public void produceChunkGenerateEvent(int chunkX, int chunkZ) {
            ChunkGenerateEvent chunkGenerateEvent = new ChunkGenerateEvent(chunkX, chunkZ);
            eventRepository.addEvent(chunkGenerateEvent);
            Log.info("Produced event: " + chunkGenerateEvent);
        }

        public void producePlayerFallingIntoVoidEvent(Vector3 position) {
            PlayerFallingIntoVoidEvent playerFallingIntoVoidEvent = new PlayerFallingIntoVoidEvent(position);
            eventRepository.addEvent(playerFallingIntoVoidEvent);
            Log.info("Produced event: " + playerFallingIntoVoidEvent);
        }

        public void produceNationCreationEvent(Nation nation) {
            NationCreationEvent nationCreationEvent = new NationCreationEvent(nation);
            eventRepository.addEvent(nationCreationEvent);
            Log.info("Produced event: " + nationCreationEvent);
        }

        public void produceNationJoinEvent(Nation nation, EntityId entityId) {
            NationJoinEvent nationJoinEvent = new NationJoinEvent(nation, entityId);
            eventRepository.addEvent(nationJoinEvent);
            Log.info("Produced event: " + nationJoinEvent);
        }

        public void produceNationLeaveEvent(Nation nation, EntityId entityId) {
            NationLeaveEvent nationLeaveEvent = new NationLeaveEvent(nation, entityId);
            eventRepository.addEvent(nationLeaveEvent);
            Log.info("Produced event: " + nationLeaveEvent);
        }

        public void producePawnSpawnEvent(Vector3 position, Pawn pawn) {
            PawnSpawnEvent pawnSpawnEvent = new PawnSpawnEvent(position, pawn);
            eventRepository.addEvent(pawnSpawnEvent);
            Log.info("Produced event: " + pawnSpawnEvent);
        }

        public void produceNationDisbandEvent(Nation nation) {
            NationDisbandEvent nationDisbandEvent = new NationDisbandEvent(nation);
            eventRepository.addEvent(nationDisbandEvent);
            Log.info("Produced event: " + nationDisbandEvent);
        }

        public void producePlayerDeathEvent(Player player) {
            PlayerDeathEvent playerDeathEvent = new PlayerDeathEvent(player);
            eventRepository.addEvent(playerDeathEvent);
            Log.info("Produced event: " + playerDeathEvent);
        }

        public void producePawnDeathEvent(Pawn pawn) {
            PawnDeathEvent pawnDeathEvent = new PawnDeathEvent(pawn);
            eventRepository.addEvent(pawnDeathEvent);
            Log.info("Produced event: " + pawnDeathEvent);
        }

        public void producePawnRelationshipIncreaseEvent(Pawn pawn1, Entity entity2, int increase) {
            PawnRelationshipIncreaseEvent pawnRelationshipIncreaseEvent = new PawnRelationshipIncreaseEvent(pawn1, entity2, increase);
            eventRepository.addEvent(pawnRelationshipIncreaseEvent);
            Log.info("Produced event: " + pawnRelationshipIncreaseEvent);
        }
    }
}