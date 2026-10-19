using System;

namespace beyondnations {

    /**
    * An event read back from a save (docs/SAVES.md). A save keeps what an
    * event says and when it happened -- its type, description and date --
    * but not the live objects a fresh event points at (the pawn that died,
    * the nation that formed), which may long since be gone.
    */
    public class RestoredEvent : Event {
        public RestoredEvent(EventType type, string description, DateTime date) : base(type, description, date) {
        }

        public override string ToString() {
            return "RestoredEvent{" + getType() + ": " + getDescription() + "}";
        }
    }
}
