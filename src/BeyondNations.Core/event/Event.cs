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