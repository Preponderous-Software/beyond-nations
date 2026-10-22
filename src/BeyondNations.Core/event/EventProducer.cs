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
            Log.debug("Produced event: " + chunkGenerateEvent);
        }

        public void producePlayerFallingIntoVoidEvent(Vector3 position) {
            PlayerFallingIntoVoidEvent playerFallingIntoVoidEvent = new PlayerFallingIntoVoidEvent(position);
            eventRepository.addEvent(playerFallingIntoVoidEvent);
            Log.debug("Produced event: " + playerFallingIntoVoidEvent);
        }

        public void produceNationCreationEvent(Nation nation) {
            NationCreationEvent nationCreationEvent = new NationCreationEvent(nation);
            eventRepository.addEvent(nationCreationEvent);
            Log.debug("Produced event: " + nationCreationEvent);
        }

        public void produceNationJoinEvent(Nation nation, EntityId entityId) {
            NationJoinEvent nationJoinEvent = new NationJoinEvent(nation, entityId);
            eventRepository.addEvent(nationJoinEvent);
            Log.debug("Produced event: " + nationJoinEvent);
        }

        public void produceNationLeaveEvent(Nation nation, EntityId entityId) {
            NationLeaveEvent nationLeaveEvent = new NationLeaveEvent(nation, entityId);
            eventRepository.addEvent(nationLeaveEvent);
            Log.debug("Produced event: " + nationLeaveEvent);
        }

        public void producePawnSpawnEvent(Vector3 position, Pawn pawn) {
            PawnSpawnEvent pawnSpawnEvent = new PawnSpawnEvent(position, pawn);
            eventRepository.addEvent(pawnSpawnEvent);
            Log.debug("Produced event: " + pawnSpawnEvent);
        }

        public void produceNationDisbandEvent(Nation nation) {
            NationDisbandEvent nationDisbandEvent = new NationDisbandEvent(nation);
            eventRepository.addEvent(nationDisbandEvent);
            Log.debug("Produced event: " + nationDisbandEvent);
        }

        public void producePlayerDeathEvent(Player player) {
            PlayerDeathEvent playerDeathEvent = new PlayerDeathEvent(player);
            eventRepository.addEvent(playerDeathEvent);
            Log.debug("Produced event: " + playerDeathEvent);
        }

        public void producePawnDeathEvent(Pawn pawn) {
            PawnDeathEvent pawnDeathEvent = new PawnDeathEvent(pawn);
            eventRepository.addEvent(pawnDeathEvent);
            Log.debug("Produced event: " + pawnDeathEvent);
        }

        public void producePawnRelationshipIncreaseEvent(Pawn pawn1, Entity entity2, int increase) {
            PawnRelationshipIncreaseEvent pawnRelationshipIncreaseEvent = new PawnRelationshipIncreaseEvent(pawn1, entity2, increase);
            eventRepository.addEvent(pawnRelationshipIncreaseEvent);
            Log.debug("Produced event: " + pawnRelationshipIncreaseEvent);
        }
    
        public void produceNationRelationChangeEvent(Nation nation1, Nation nation2, NationRelationState before, NationRelationState after, string why = null) {
            NationRelationChangeEvent e = new NationRelationChangeEvent(nation1, nation2, before, after, why);
            eventRepository.addEvent(e);
            Log.debug("Produced event: " + e);
        }

        public void produceNationRaidEvent(Nation raider, Nation victim, Settlement settlement, string loot) {
            NationRaidEvent e = new NationRaidEvent(raider, victim, settlement, loot);
            eventRepository.addEvent(e);
            Log.debug("Produced event: " + e);
        }

        public void produceSettlementCaptureEvent(Settlement settlement, Nation from, Nation to) {
            SettlementCaptureEvent e = new SettlementCaptureEvent(settlement, from, to);
            eventRepository.addEvent(e);
            Log.debug("Produced event: " + e);
        }

        public void produceSettlementFoundedEvent(Settlement settlement, Nation nation, EntityId founderId) {
            SettlementFoundedEvent settlementFoundedEvent = new SettlementFoundedEvent(settlement, nation, founderId);
            eventRepository.addEvent(settlementFoundedEvent);
            Log.debug("Produced event: " + settlementFoundedEvent);
        }

        public void produceStallBuiltEvent(Settlement settlement, Nation nation, EntityId builderId) {
            StallBuiltEvent stallBuiltEvent = new StallBuiltEvent(settlement, nation, builderId);
            eventRepository.addEvent(stallBuiltEvent);
            Log.debug("Produced event: " + stallBuiltEvent);
        }

        public void produceLeadershipChangeEvent(Nation nation, EntityId previousLeaderId, EntityId newLeaderId) {
            LeadershipChangeEvent leadershipChangeEvent = new LeadershipChangeEvent(nation, previousLeaderId, newLeaderId);
            eventRepository.addEvent(leadershipChangeEvent);
            Log.debug("Produced event: " + leadershipChangeEvent);
        }

        /** A ruler's act or the fate of their line, already told as a line of history (realm/). */
        public void produceStatecraftEvent(string text, Nation nation, Nation otherNation, EntityId entityId) {
            StatecraftEvent statecraftEvent = new StatecraftEvent(text, nation, otherNation, entityId);
            eventRepository.addEvent(statecraftEvent);
            Log.debug("Produced event: " + statecraftEvent);
        }

        public void produceWorldEventEvent(string eventId, string text, System.Collections.Generic.IReadOnlyList<NationId> nations,
                System.Collections.Generic.IReadOnlyList<EntityId> entities) {
            WorldEventEvent worldEventEvent = new WorldEventEvent(eventId, text, nations, entities);
            eventRepository.addEvent(worldEventEvent);
            Log.debug("Produced event: " + worldEventEvent);
        }

        public void produceCaravanEvent(CaravanEvent caravanEvent) {
            eventRepository.addEvent(caravanEvent);
            Log.debug("Produced event: " + caravanEvent);
        }
    }
}
