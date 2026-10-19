using System;
using System.Collections.Generic;

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
            return totalNumberOfEvents;
        }

        public int getNumberOfEvents(EventType eventType) {
            return events[eventType].Count;
        }
    }
}
