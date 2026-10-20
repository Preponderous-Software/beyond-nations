using System.Collections.Generic;

namespace beyondnations {

    /**
    * Something happened to a nation or to the player from outside the
    * ordinary run of the world -- a drought, bandits, a stranger with a map
    * -- and how it turned out (worldevents/WorldEvents). The line is written
    * by the world event itself, since only it knows what was chosen; the
    * chronicle tells it as it is.
    */
    public class WorldEventEvent : Event {
        private string eventId;
        private List<NationId> nations;
        private List<EntityId> entities;

        public WorldEventEvent(string eventId, string text, IReadOnlyList<NationId> nations, IReadOnlyList<EntityId> entities)
                : base(EventType.WorldEvent, text) {
            this.eventId = eventId;
            this.nations = nations == null ? new List<NationId>() : new List<NationId>(nations);
            this.entities = entities == null ? new List<EntityId>() : new List<EntityId>(entities);
        }

        /** The id of the world event (IWorldEvent.getId) this line is about. */
        public string getEventId() { return eventId; }
        public List<NationId> getNations() { return nations; }
        public List<EntityId> getEntities() { return entities; }

        public override string ToString() {
            return "WorldEventEvent [" + "eventId=" + eventId + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
