namespace beyondnations {

    /**
    * The player buys a building in the settlement they are standing in,
    * paying coins for the wood and stone they are short of (BuildingPurchase).
    * Whatever wood and stone they carry, and the settlement's stockpile, go
    * in first, so coins only make up the difference.
    *
    * The same people may buy as may build (BuildBuildingCommand): any member
    * of the settlement's nation, in a settlement with room for the building
    * and an era that has unlocked it. When the materials already cover the
    * building there is nothing to buy, and the player is told to build it.
    */
    public class BuyBuildingCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public BuyBuildingCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, BuildingType type) {
            Settlement settlement = BuildBuildingCommand.findOwnSettlement(player, entityRepository);
            if (settlement == null) {
                return false;
            }
            string name = BuildingCatalog.getDisplayName(type);
            if (!settlement.isBuildingUnlocked(type)) {
                player.getStatus().update(EraRules.describeLocked(type));
                return false;
            }
            if (!settlement.canAddBuilding(type)) {
                player.getStatus().update("No room for another " + name + " here.");
                return false;
            }
            Inventory carried = player.getInventory();
            string whyNot = BuildingPurchase.describeWhyNot(settlement, carried, type);
            if (whyNot != null) {
                player.getStatus().update("A " + name + ": " + char.ToLowerInvariant(whyNot[0]) + whyNot.Substring(1));
                return false;
            }

            string paidFor = BuildingPurchase.describeShortfall(settlement, carried, type);
            SettlementTier before = settlement.getTier();
            BuildingPurchase.pay(settlement, carried, type);
            settlement.addBuilding(type);
            player.getStatus().update(name + " bought: you paid for " + paidFor + "."
                + BuildBuildingCommand.describeTierChange(before, settlement));
            return true;
        }
    }
}
