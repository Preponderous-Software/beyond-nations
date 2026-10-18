using System.Collections.Generic;
using System.Numerics;

namespace beyondnations.desktop.ui {

    /**
    * Which command buttons the world HUD offers right now.
    *
    * This is WorldScreen.drawCommandButtons() from
    * Assets/Scripts/screens/WorldScreen.cs with the drawing taken out. The
    * conditions are unchanged -- whether the player is in a nation, leads it,
    * is standing in one of its settlements, owns a stall, has the wood or the
    * coin -- and so is the order the buttons appear in.
    *
    * Deciding the list is separate from drawing it because the deciding is the
    * part worth testing, and it needs no window: a test can stand a Simulation
    * up, put the player in a state and assert on the labels.
    */
    public static class WorldCommandBar {

        /**
        * How close a settlement has to be before it can be entered. Unity kept
        * this as a local named distanceThreshold.
        */
        public const int EnterSettlementDistance = 50;

        public static List<WorldCommand> getCommands(Simulation simulation) {
            List<WorldCommand> commands = new List<WorldCommand>();
            Player player = simulation.getPlayer();
            EntityRepository entityRepository = simulation.getEntityRepository();
            NationRepository nationRepository = simulation.getNationRepository();

            if (player.getNationId() == null) {
                commands.Add(new WorldCommand("Create Nation", () =>
                    new NationCreateCommand(nationRepository, simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                        .execute(player)));
                commands.Add(new WorldCommand("Join Nation", () =>
                    new NationJoinCommand(nationRepository, simulation.getEventProducer(), simulation.getRandom())
                        .execute(player)));
            }
            else {
                Nation nation = nationRepository.getNation(player.getNationId());

                commands.Add(new WorldCommand("Leave Nation", () =>
                    new NationLeaveCommand(nationRepository, simulation.getEventProducer(), entityRepository)
                        .execute(player)));

                if (player.getId() == nation.getLeaderId()
                        && nation.getNumberOfSettlements() == 0
                        && player.getInventory().getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD) {
                    commands.Add(new WorldCommand("Found Settlement", () =>
                        new FoundSettlementCommand(nationRepository, simulation.getEventProducer(), entityRepository, simulation.getGameConfig(), simulation.getRandom())
                            .execute(player)));
                }

                if (!player.isCurrentlyInSettlement() && player.getHomeSettlementId() != null) {
                    commands.Add(new WorldCommand("Teleport Home", () =>
                        new TeleportHomeCommand(entityRepository, simulation.getRandom())
                            .execute(player)));
                }

                if (player.isCurrentlyInSettlement()) {
                    Settlement settlement = (Settlement) entityRepository.getEntity(player.getCurrentSettlementId());
                    if (settlement.getNationId() == player.getNationId()) {
                        Market market = settlement.getMarket();

                        if (player.getId() == nation.getLeaderId()
                                && market.getNumStalls() < market.getMaxNumStalls()
                                && player.getInventory().getNumItems(ItemType.WOOD) >= Stall.WOOD_COST_TO_BUILD) {
                            commands.Add(new WorldCommand("Build Stall", () =>
                                new BuildStallCommand(nationRepository, entityRepository).execute(player)));
                        }

                        if (nation.getRole(player.getId()) == NationRole.SERF
                                && player.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE
                                && market.getNumStallsForSale() > 0) {
                            commands.Add(new WorldCommand("Purchase Stall", () =>
                                new PurchaseStallCommand(nationRepository, entityRepository).execute(player)));
                        }

                        Stall stall = market.getStall(player.getId());
                        if (stall != null) {
                            Inventory stallInventory = stall.getInventory();

                            commands.Add(new WorldCommand("Transfer Items", () =>
                                new TransferItemsToStallCommand(nationRepository, entityRepository).execute(player)));

                            int numCoins = stallInventory.getNumItems(ItemType.COIN);
                            if (numCoins > 0) {
                                commands.Add(new WorldCommand("Collect Coins (" + numCoins + ")", () =>
                                    new CollectProfitFromStallCommand(nationRepository, entityRepository).execute(player)));
                            }

                            int numApples = stallInventory.getNumItems(ItemType.APPLE);
                            if (numApples > 0) {
                                commands.Add(new WorldCommand("Collect Food (" + numApples + ")", () =>
                                    new CollectFoodFromStallCommand(nationRepository, entityRepository).execute(player)));
                            }
                        }

                        if (player.getId() == nation.getLeaderId()) {
                            commands.Add(new WorldCommand("Withdraw Funds", () =>
                                new WithdrawSettlementFundsCommand(nationRepository, entityRepository).execute(player)));
                        }
                    }
                }
            }

            if (player.getInventory().getNumItems(ItemType.SAPLING) > 0 && !player.isCurrentlyInSettlement()) {
                commands.Add(new WorldCommand("Plant Sapling", () =>
                    new PlantSaplingCommand(entityRepository, simulation.getRandom()).execute(player)));
            }

            if (!player.isCurrentlyInSettlement()) {
                Settlement nearestSettlement = (Settlement) simulation.getEnvironment().getNearestEntityOfType(player.getPosition(), EntityType.SETTLEMENT);
                if (nearestSettlement != null) {
                    int distance = (int) Vector3.Distance(player.getPosition(), nearestSettlement.getPosition());
                    if (distance < EnterSettlementDistance) {
                        commands.Add(new WorldCommand("Enter Settlement", () =>
                            new EnterSettlementCommand(entityRepository).execute(player, nearestSettlement)));
                    }
                }
            }
            else {
                commands.Add(new WorldCommand("Exit Settlement", () =>
                    new ExitSettlementCommand(entityRepository).execute(player)));

                if (player.getInventory().getNumItems(ItemType.COIN) > 0) {
                    commands.Add(new WorldCommand("Purchase Food", () =>
                        new PurchaseFoodFromMarketCommand(nationRepository, entityRepository).execute(player)));
                }

                if (player.getInventory().getNumItems(ItemType.WOOD) > 0 || player.getInventory().getNumItems(ItemType.STONE) > 0) {
                    commands.Add(new WorldCommand("Sell Resources", () =>
                        new SellResourcesAtMarketCommand(nationRepository, entityRepository).execute(player)));
                }
            }

            return commands;
        }
    }
}
