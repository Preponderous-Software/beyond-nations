using System;

namespace beyondnations {

    /**
    * A save that cannot be loaded: corrupt, from a newer version, or not a
    * save at all. The message is meant for the player.
    */
    public class SaveFormatException : Exception {
        public SaveFormatException(string message) : base(message) {
        }

        public SaveFormatException(string message, Exception inner) : base(message, inner) {
        }
    }
}
