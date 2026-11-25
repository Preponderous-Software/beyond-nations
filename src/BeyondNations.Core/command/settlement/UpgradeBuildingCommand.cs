namespace beyondnations {

    /**
    * The player upgrades a building of one type in the settlement they are
    * standing in, paying coins from their purse (BuildingUpgrades). The
    * upgrade goes to the building of that type with the lowest level.
    *
    * The same people may upgrade as may build (BuildBuildingCommand): any
    * member of the settlement's nation, standing inside it. Level 1 needs
    * the Town era and level 2 the Kingdom era.
    */
    public class UpgradeBuildingCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public UpgradeBuildingCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, BuildingType type) {
            Settlement settlement = BuildBuildingCommand.findOwnSettlement(player, entityRepository);
            if (settlement == null) {
                return false;
            }
            string name = BuildingCatalog.getDisplayName(type);
            string whyNot = BuildingUpgrades.describeWhyNot(settlement, player.getInventory(), type);
            if (whyNot != null) {
                player.getStatus().update("Upgrade " + name + ": " + char.ToLowerInvariant(whyNot[0]) + whyNot.Substring(1));
                return false;
            }
            int price = BuildingUpgrades.getNextPrice(settlement, type);
            Building upgraded = BuildingUpgrades.upgrade(settlement, player.getInventory(), type);
            player.getStatus().update(name + " upgraded to level " + upgraded.getLevel() + " for " + BuildingPurchase.formatCoins(price)
                + " coins: " + BuildingUpgrades.describeEffect(type, upgraded.getLevel()) + ".");
            return true;
        }
    }
}
