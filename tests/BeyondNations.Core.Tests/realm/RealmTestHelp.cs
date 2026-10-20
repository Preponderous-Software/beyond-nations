using System.Collections.Generic;

using beyondnations;

namespace beyondnationstests {

    public static class RealmTestHelp {
        /** How many events of a type there have been; the repository throws for a type it has never seen. */
        public static int count(EventRepository repository, EventType type) {
            try {
                return repository.getNumberOfEvents(type);
            } catch (KeyNotFoundException) {
                return 0;
            }
        }
    }
}
