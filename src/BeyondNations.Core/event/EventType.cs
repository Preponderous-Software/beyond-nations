namespace beyondnations {

    public enum EventType {
        None,
        ChunkGenerate,
        PlayerFallingIntoVoid,
        NationCreation,
        NationDisband,
        NationJoin,
        NationLeave,
        PawnSpawn,
        PawnDeath,
        PlayerDeath,
        PawnRelationshipIncrease,
        NationRelationChange,
        NationRaid,
        SettlementCapture,
        SettlementFounded,
        StallBuilt,
        LeadershipChange,
        // a ruler's acts and the fate of their line: laws, war and peace, succession (realm/)
        Statecraft,
        WorldEvent,
        Caravan
    }
}