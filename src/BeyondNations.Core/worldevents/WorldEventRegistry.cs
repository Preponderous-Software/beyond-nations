using System;
using System.Collections.Generic;

namespace beyondnations {

    /**
    * Every kind of world event the world can throw up, by id. The default
    * registry holds the events in worldevents/events; a new one is a class
    * implementing IWorldEvent and one line in createDefault (or a call to
    * register, for a test or a mod).
    *
    * The order is part of the world: the events are weighed in registration
    * order, so adding one in the middle changes which event a seed rolls.
    * Add new ones at the end.
    */
    public class WorldEventRegistry {
        private readonly List<IWorldEvent> events = new List<IWorldEvent>();

        public static WorldEventRegistry createDefault() {
            WorldEventRegistry registry = new WorldEventRegistry();
            registry.register(new DroughtEvent());
            registry.register(new SicknessEvent());
            registry.register(new WanderersEvent());
            registry.register(new BanditCampEvent());
            registry.register(new ClaimantEvent());
            registry.register(new BountifulHarvestEvent());
            registry.register(new LostTravellerEvent());
            registry.register(new StorehouseFireEvent());
            registry.register(new RareMerchantEvent());
            return registry;
        }

        public void register(IWorldEvent worldEvent) {
            if (worldEvent == null) {
                throw new ArgumentNullException(nameof(worldEvent));
            }
            if (get(worldEvent.getId()) != null) {
                throw new ArgumentException("a world event with id '" + worldEvent.getId() + "' is already registered");
            }
            events.Add(worldEvent);
        }

        /** The event with this id, or null. */
        public IWorldEvent get(string id) {
            foreach (IWorldEvent worldEvent in events) {
                if (worldEvent.getId() == id) {
                    return worldEvent;
                }
            }
            return null;
        }

        public IReadOnlyList<IWorldEvent> getAll() {
            return events;
        }
    }
}
