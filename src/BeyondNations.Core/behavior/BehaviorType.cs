namespace beyondnations {

    public enum BehaviorType {
        NONE,

        // environment modification
        GATHER_RESOURCES,
        PLANT_SAPLING,

        // movement
        WANDER,
        GO_TO_HOME_SETTLEMENT,
        EXIT_SETTLEMENT,

        // trading
        SELL_RESOURCES,
        PURCHASE_FOOD,
        PURCHASE_STALL,
        TRANSFER_ITEMS_TO_STALL,
        COLLECT_PROFIT_FROM_STALL,
        COLLECT_FOOD_FROM_STALL,

        // construction
        CONSTRUCT_SETTLEMENT,        
        CONSTRUCT_STALL,
        CONSTRUCT_BUILDING,

        // nation
        CREATE_NATION,
        JOIN_NATION,
        JOIN_RANDOM_SETTLEMENT,
        WITHDRAW_SETTLEMENT_FUNDS,

        // added later; appended so earlier values keep their numbers
        CONTRIBUTE_TO_STOCKPILE,
        SEEK_SETTLEMENT_SITE
    }
}