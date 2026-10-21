using System;

namespace beyondnations {
    
    public abstract class Event {
        private EventType type;
        private string description;
        private DateTime date;

        public Event(EventType type, string description) {
            this.type = type;
            this.description = description;
            this.date = DateTime.Now;
        }

        /** An event as a save recorded it, with the date it first happened (docs/SAVES.md). */
        protected Event(EventType type, string description, DateTime date) {
            this.type = type;
            this.description = description;
            this.date = date;
        }

        public EventType getType() {
            return type;
        }

        public string getDescription() {
            return description;
        }

        public DateTime getDate() {
            return date;
        }
    }
}