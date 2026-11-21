namespace beyondnations {

    /** A loaded world, and whether it came from the backup because the save itself was damaged. */
    public class SaveLoadResult {
        public Simulation Simulation { get; internal set; }
        public SaveHeader Header { get; internal set; }
        public bool FromBackup { get; internal set; }
        /** Why the save itself could not be read, when the backup was used. */
        public string Problem { get; internal set; }
    }
}
