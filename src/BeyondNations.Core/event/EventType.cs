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
        Caravan,
        // a trader of a nation took up trade, bought a load or sold it (#487, trade/Traders)
        Trader
    }
}