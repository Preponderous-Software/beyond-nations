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
                                && Stall.canAffordToBuild(player.getInventory())) {
                            commands.Add(new WorldCommand("Build Stall", () =>
                                new BuildStallCommand(nationRepository, entityRepository).execute(player)));
                        }

                        // one button per building the player can afford and the settlement has room for
                        foreach (BuildingType buildingType in BuildingPlanner.getPriorityOrder(settlement)) {
                            if (settlement.canAddBuilding(buildingType)
                                    && BuildingCatalog.canAfford(player.getInventory(), settlement.getInventory(), buildingType)) {
                                BuildingType typeToBuild = buildingType;
                                commands.Add(new WorldCommand("Build " + BuildingCatalog.getDisplayName(buildingType), () =>
                                    new BuildBuildingCommand(nationRepository, entityRepository).execute(player, typeToBuild)));
                            }
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

                            int stockpile = CollectSettlementStockpileCommand.getStockpileSize(settlement);
                            if (stockpile > 0) {
                                commands.Add(new WorldCommand("Collect Stockpile (" + stockpile + ")", () =>
                                    new CollectSettlementStockpileCommand(nationRepository, entityRepository).execute(player)));
                            }
                        }
                    }
                }
            }

            addPawnCommands(simulation, commands);
            addDiplomacyCommands(simulation, commands);

            if (player.getInventory().getNumItems(ItemType.SAPLING) > 0 && !player.isCurrentlyInSettlement()) {
                commands.Add(new WorldCommand("Plant Sapling", () =>
                    new PlantSaplingCommand(entityRepository, simulation.getRandom(), simulation.getTickCounter()).execute(player)));
            }

            addCraftingCommands(simulation, commands);

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

                if (player.getInventory().getNumItems(ItemType.WOOD) > 0 || player.getInventory().getNumItems(ItemType.STONE) > 0
                        || player.getInventory().getNumItems(ItemType.PLANKS) > 0 || player.getInventory().getNumItems(ItemType.BRICKS) > 0) {
                    commands.Add(new WorldCommand("Sell Resources", () =>
                        new SellResourcesAtMarketCommand(nationRepository, entityRepository).execute(player)));
                }
            }

            return commands;
        }

        /**
        * Standing in another nation's land, a leader can deal with it on the
        * spot: a gift, an alliance, a denunciation. The same three are on the
        * nations screen (O) for every nation.
        */
        private static void addDiplomacyCommands(Simulation simulation, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            NationRepository nationRepository = simulation.getNationRepository();
            if (!NationDiplomacy.playerLeads(player, nationRepository)) {
                return;
            }
            Settlement claim = Territory.findClaimAt(simulation.getEntityRepository(), player.getPosition());
            if (claim == null || claim.getNationId() == player.getNationId()) {
                return;
            }
            Nation own = nationRepository.getNation(player.getNationId());
            Nation owner = nationRepository.getNation(claim.getNationId());
            if (owner == null) {
                return;
            }
            Diplomacy diplomacy = simulation.getDiplomacy();
            if (player.getInventory().getNumItems(ItemType.COIN) >= Diplomacy.PLAYER_GIFT_COINS) {
                commands.Add(new WorldCommand("Gift " + owner.getName() + " " + Diplomacy.PLAYER_GIFT_COINS + " Coins", () =>
                    new GiftNationCommand(nationRepository, diplomacy).execute(player, owner)));
            }
            if (!nationRepository.getRelations().isAllied(own, owner)) {
                commands.Add(new WorldCommand("Propose Alliance to " + owner.getName(), () =>
                    new ProposeAllianceCommand(nationRepository).execute(player, owner)));
            }
            commands.Add(new WorldCommand("Denounce " + owner.getName(), () =>
                new DenounceNationCommand(nationRepository, diplomacy).execute(player, owner)));
        }

        /**
        * Shortcuts into crafting (#47): a player without an axe or pickaxe who
        * can make one is offered it outright -- the first craft is the one
        * that teaches the screen exists -- and inside a settlement, a tool a
        * merchant has stocked can be bought when the player can pay for it.
        * Everything else is on the crafting screen (C).
        */
        private static void addCraftingCommands(Simulation simulation, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            EntityRepository entityRepository = simulation.getEntityRepository();
            Inventory inventory = player.getInventory();
            Settlement settlement = CraftItemCommand.getCurrentSettlement(player, entityRepository);

            foreach (Recipe recipe in new[] { RecipeBook.Axe, RecipeBook.Pickaxe }) {
                ItemType tool = recipe.getOutput();
                if (inventory.getNumItems(tool) > 0) {
                    continue;
                }
                if (Crafting.canCraft(recipe, inventory, settlement)) {
                    Recipe toCraft = recipe;
                    commands.Add(new WorldCommand("Craft " + recipe.getName(), () =>
                        new CraftItemCommand(entityRepository).execute(player, toCraft)));
                    continue;
                }
                if (settlement != null) {
                    int price = settlement.getMarket().getPriceToBuy(player, tool);
                    if (price >= 0 && inventory.getNumItems(ItemType.COIN) >= price) {
                        ItemType toBuy = tool;
                        commands.Add(new WorldCommand("Buy " + recipe.getName() + " (" + price + ")", () =>
                            new BuyItemFromMarketCommand(entityRepository).execute(player, toBuy)));
                    }
                }
            }
        }

        /**
        * Talk, gift and invite for the pawn within reach, plus the leader's
        * levers when the player leads that pawn's nation. Each label names the
        * pawn and how it feels about the player, so the bar doubles as the
        * relationship readout for whoever is standing in front of the player.
        */
        private static void addPawnCommands(Simulation simulation, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            Pawn pawn = PawnRelations.findPawnInRange(player, simulation.getEnvironment());
            if (pawn == null) {
                return;
            }
            NationRepository nationRepository = simulation.getNationRepository();
            EntityRepository entityRepository = simulation.getEntityRepository();
            EventProducer eventProducer = simulation.getEventProducer();

            commands.Add(new WorldCommand("Talk to " + PawnRelations.describeWithTraits(pawn, player), () =>
                new TalkToPawnCommand(nationRepository, eventProducer, simulation.getTickCounter()).execute(player, pawn)));

            if (GiftPawnCommand.canGift(player)) {
                commands.Add(new WorldCommand("Gift " + pawn.getName(), () =>
                    new GiftPawnCommand(eventProducer).execute(player, pawn)));
            }

            if (InvitePawnToNationCommand.isApplicable(player, pawn)) {
                commands.Add(new WorldCommand("Invite " + pawn.getName(), () =>
                    new InvitePawnToNationCommand(nationRepository, eventProducer, entityRepository).execute(player, pawn)));
            }

            if (LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                if (pawn.getOrder() == null) {
                    commands.Add(new WorldCommand("Order " + pawn.getName() + ": Gather Wood", () =>
                        new OrderPawnToGatherCommand(nationRepository).execute(player, pawn)));
                }
                if (LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository)) {
                    commands.Add(new WorldCommand("Appoint " + pawn.getName() + " Merchant", () =>
                        new AppointMerchantCommand(nationRepository, entityRepository, eventProducer).execute(player, pawn)));
                }
            }
        }
    }
}
