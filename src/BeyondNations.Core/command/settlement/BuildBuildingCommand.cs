namespace beyondnations {

    /**
    * The player puts up a building in the settlement they are standing in.
    *
    * Any member of the settlement's nation may build, not only its leader:
    * growing a town is something a player who joined a pawn's nation can help
    * with. A null type builds whatever the settlement needs most that the
    * player can pay for (BuildingPlanner), which is what the build key does;
    * the command bar names a type.
    *
    * The cost is paid from what the player carries first and then from the
    * settlement's stockpile, so wood and stone that ordered pawns delivered
    * (NationOrderDelivery) go straight into growing the town.
    */
    public class BuildBuildingCommand {
        private NationRepository nationRepository;
        private EntityRepository entityRepository;

        public BuildBuildingCommand(NationRepository nationRepository, EntityRepository entityRepository) {
            this.nationRepository = nationRepository;
            this.entityRepository = entityRepository;
        }

        public bool execute(Player player, BuildingType? requestedType) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return false;
            }
            if (player.getCurrentSettlementId() == null) {
                player.getStatus().update("You are not inside a settlement.");
                return false;
            }
            Settlement settlement = entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
            if (settlement == null || !settlement.getNationId().Equals(player.getNationId())) {
                player.getStatus().update("You are not inside a settlement owned by your nation.");
                return false;
            }

            BuildingType type;
            if (requestedType != null) {
                type = requestedType.Value;
            }
            else {
                BuildingType? affordable = BuildingPlanner.chooseNext(settlement, player.getInventory());
                if (affordable != null) {
                    type = affordable.Value;
                }
                else {
                    BuildingType? wanted = BuildingPlanner.suggestNext(settlement);
                    if (wanted == null) {
                        player.getStatus().update("This settlement has no room for another building.");
                        return false;
                    }
                    type = wanted.Value;
                }
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
            Inventory stockpile = settlement.getInventory();
            if (!BuildingCatalog.canAfford(player.getInventory(), stockpile, type)) {
                int wood = player.getInventory().getNumItems(ItemType.WOOD) + stockpile.getNumItems(ItemType.WOOD);
                int stone = player.getInventory().getNumItems(ItemType.STONE) + stockpile.getNumItems(ItemType.STONE);
                int woodShort = System.Math.Max(0, BuildingCatalog.getWoodCost(type) - wood);
                int stoneShort = System.Math.Max(0, BuildingCatalog.getStoneCost(type) - stone);
                player.getStatus().update("A " + name + " needs " + BuildingCatalog.describeCost(type) + ". Need " + woodShort + " more wood, " + stoneShort + " more stone.");
                return false;
            }

            SettlementTier before = settlement.getTier();
            BuildingCatalog.deductCost(player.getInventory(), stockpile, type);
            settlement.addBuilding(type);
            string message = name + " built.";
            if (settlement.getTier() != before) {
                message += " The settlement is now a " + SettlementTiers.getDisplayName(settlement.getTier()) + "!";
            }
            player.getStatus().update(message);
            return true;
        }
    }
}
