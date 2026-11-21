namespace beyondnations {

    /** One entry in the Load list: a slot and what its newest readable file says. */
    public class SaveSlotInfo {
        public string Slot { get; internal set; }
        /** Null when neither the save nor its backup can be read. */
        public SaveHeader Header { get; internal set; }
        /** Why the save cannot be read, or null when it can. */
        public string Problem { get; internal set; }
        /** True when the save itself is unreadable and the header shown is its backup's. */
        public bool FromBackup { get; internal set; }

        public bool isLoadable() {
            return Header != null;
        }
    }
}
