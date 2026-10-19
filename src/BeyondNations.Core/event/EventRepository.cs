using System;
using System.Collections.Generic;
using System.Text.Json;

namespace beyondnations {

    /**
    * Every event the world has produced, grouped by type, and the publisher
    * other systems subscribe to.
    *
    * addEvent stores the event and then hands it, synchronously and in
    * subscription order, to each listener for its type and then to each
    * listener for all events. Listeners therefore run inside the tick that
    * produced the event and see the world exactly as it was at that moment
    * (a disbanding nation still has its settlements, a dying pawn still has
    * its nation). A listener that throws is logged and skipped, so a broken
    * subscriber can never stop the simulation or starve the listeners after
    * it.
    */
    public class EventRepository {
        private Dictionary<EventType, List<Event>> events;
        // events a loaded save no longer holds, counted so the totals stay whole
        private Dictionary<EventType, int> droppedCounts = new Dictionary<EventType, int>();
        private List<Action<Event>> listeners = new List<Action<Event>>();
        private Dictionary<EventType, List<Action<Event>>> typedListeners = new Dictionary<EventType, List<Action<Event>>>();

        public EventRepository() {
            events = new Dictionary<EventType, List<Event>>();
        }

        public void addEvent(Event e) {
            if (!events.TryGetValue(e.getType(), out List<Event> list)) {
                list = new List<Event>();
                events.Add(e.getType(), list);
            }
            list.Add(e);
            publish(e);
        }

        /** Calls listener with every event added from now on. */
        public void subscribe(Action<Event> listener) {
            if (listener == null) {
                throw new ArgumentNullException(nameof(listener));
            }
            listeners.Add(listener);
        }

        /** Calls listener with every event of the given type added from now on. */
        public void subscribe(EventType eventType, Action<Event> listener) {
            if (listener == null) {
                throw new ArgumentNullException(nameof(listener));
            }
            if (!typedListeners.TryGetValue(eventType, out List<Action<Event>> list)) {
                list = new List<Action<Event>>();
                typedListeners.Add(eventType, list);
            }
            list.Add(listener);
        }

        /** Removes an all-events listener. Returns false if it was not subscribed. */
        public bool unsubscribe(Action<Event> listener) {
            return listeners.Remove(listener);
        }

        /** Removes a typed listener. Returns false if it was not subscribed to that type. */
        public bool unsubscribe(EventType eventType, Action<Event> listener) {
            return typedListeners.TryGetValue(eventType, out List<Action<Event>> list) && list.Remove(listener);
        }

        public int getNumberOfListeners() {
            int count = listeners.Count;
            foreach (List<Action<Event>> list in typedListeners.Values) {
                count += list.Count;
            }
            return count;
        }

        private void publish(Event e) {
            // Copies, so a listener may subscribe or unsubscribe while being called.
            if (typedListeners.TryGetValue(e.getType(), out List<Action<Event>> typed) && typed.Count > 0) {
                foreach (Action<Event> listener in typed.ToArray()) {
                    notify(listener, e);
                }
            }
            if (listeners.Count > 0) {
                foreach (Action<Event> listener in listeners.ToArray()) {
                    notify(listener, e);
                }
            }
        }

        private static void notify(Action<Event> listener, Event e) {
            try {
                listener(e);
            } catch (Exception exception) {
                Log.error("Event listener threw on " + e.getType() + ": " + exception);
            }
        }

        public List<Event> getEvents(EventType eventType) {
            return events[eventType];
        }

        public int getTotalNumberOfEvents() {
            int totalNumberOfEvents = 0;
            foreach (List<Event> list in events.Values) {
                totalNumberOfEvents += list.Count;
            }
            foreach (int dropped in droppedCounts.Values) {
                totalNumberOfEvents += dropped;
            }
            return totalNumberOfEvents;
        }

        public int getNumberOfEvents(EventType eventType) {
            int dropped;
            droppedCounts.TryGetValue(eventType, out dropped);
            return events[eventType].Count + dropped;
        }

        // --- saving (docs/SAVES.md) ---

        /**
        * How many events of each type a save left behind. A save keeps only
        * the most recent SavedEventsPerType of each, so the record does not
        * grow without bound; the counts stay whole.
        */
        public const int SavedEventsPerType = 100;

        internal void save(SaveWriter writer) {
            writer.beginArray("types");
            foreach (KeyValuePair<EventType, List<Event>> entry in events) {
                List<Event> list = entry.Value;
                int first = Math.Max(0, list.Count - SavedEventsPerType);
                writer.beginObject();
                writer.writeEnum("type", entry.Key);
                writer.write("count", getNumberOfEvents(entry.Key));
                writer.beginArray("recent");
                for (int i = first; i < list.Count; i++) {
                    writer.beginObject();
                    writer.write("description", list[i].getDescription());
                    writer.write("date", list[i].getDate().ToUniversalTime().Ticks);
                    writer.endObject();
                }
                writer.endArray();
                writer.endObject();
            }
            writer.endArray();
        }

        internal void load(JsonElement element) {
            events.Clear();
            droppedCounts.Clear();
            foreach (JsonElement saved in SaveReader.property(element, "types").EnumerateArray()) {
                EventType type = SaveReader.getEnum<EventType>(saved, "type");
                List<Event> list = new List<Event>();
                foreach (JsonElement e in SaveReader.property(saved, "recent").EnumerateArray()) {
                    DateTime date = new DateTime(SaveReader.getLong(e, "date"), DateTimeKind.Utc).ToLocalTime();
                    list.Add(new RestoredEvent(type, SaveReader.getString(e, "description"), date));
                }
                events[type] = list;
                int dropped = SaveReader.getInt(saved, "count") - list.Count;
                if (dropped < 0) {
                    throw new SaveFormatException("an event count smaller than the events kept");
                }
                if (dropped > 0) {
                    droppedCounts[type] = dropped;
                }
            }
        }
    }
}
