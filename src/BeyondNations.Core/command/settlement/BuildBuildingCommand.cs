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
            Settlement settlement = findOwnSettlement(player, entityRepository);
            if (settlement == null) {
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
            string message = name + " built." + describeTierChange(before, settlement);
            player.getStatus().update(message);
            return true;
        }

        /**
        * The settlement of the player's own nation they stand in, or null,
        * having told them why not. Build and Buy (BuyBuildingCommand) both
        * start here.
        */
        public static Settlement findOwnSettlement(Player player, EntityRepository entityRepository) {
            if (player.getNationId() == null) {
                player.getStatus().update("You are not in a nation.");
                return null;
            }
            if (player.getCurrentSettlementId() == null) {
                player.getStatus().update("You are not inside a settlement.");
                return null;
            }
            Settlement settlement = entityRepository.getEntity(player.getCurrentSettlementId()) as Settlement;
            if (settlement == null || !settlement.getNationId().Equals(player.getNationId())) {
                player.getStatus().update("You are not inside a settlement owned by your nation.");
                return null;
            }
            return settlement;
        }

        /** " The settlement is now a Town!" when a building lifted its tier, else empty. */
        public static string describeTierChange(SettlementTier before, Settlement settlement) {
            return settlement.getTier() != before
                ? " The settlement is now a " + SettlementTiers.getDisplayName(settlement.getTier()) + "!"
                : "";
        }
    }
}
