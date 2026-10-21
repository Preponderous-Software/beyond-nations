using System.Collections.Generic;

namespace beyondnations {

    public enum CaravanEventKind {
        /** Set out from home with goods. */
        Dispatched,
        /** Sold its goods at a market. */
        Delivered,
        /** Came home with the takings. */
        Returned,
        /** Was robbed on the road. */
        Raided,
        /** Arrived with the player riding guard. */
        Escorted,
    }

    /**
    * Something happened to a trade caravan (TradeCaravans). The story -- the
    * chronicle's line for it -- is written by whoever produced the event,
    * which knows who was involved; notable says whether it is worth a line
    * at all, since a busy world sends caravans all day.
    */
    public class CaravanEvent : Event {
        private readonly CaravanEventKind kind;
        private readonly Caravan caravan;
        private readonly string story;
        private readonly bool notable;
        private readonly List<NationId> nationIds;
        private readonly List<EntityId> entityIds;

        public CaravanEvent(CaravanEventKind kind, Caravan caravan, string story, bool notable, List<NationId> nationIds, List<EntityId> entityIds)
                : base(EventType.Caravan, story) {
            this.kind = kind;
            this.caravan = caravan;
            this.story = story;
            this.notable = notable;
            this.nationIds = nationIds ?? new List<NationId>();
            this.entityIds = entityIds ?? new List<EntityId>();
        }

        public CaravanEventKind getKind() { return kind; }
        public Caravan getCaravan() { return caravan; }
        public string getStory() { return story; }
        public bool isNotable() { return notable; }
        public List<NationId> getNationIds() { return nationIds; }
        public List<EntityId> getEntityIds() { return entityIds; }

        public override string ToString() {
            return "CaravanEvent [kind=" + kind + ", notable=" + notable + ", type=" + getType() + ", description=" + getDescription() + ", date=" + getDate() + "]";
        }
    }
}
