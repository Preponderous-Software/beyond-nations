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
    * coin. The order is not: the bar now groups its buttons and keeps each
    * in a fixed place (CommandGroup, CommandBarLayout), so that one coming
    * into range never moves another under the cursor.
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

        /** The Build buttons' order, which is the same every time (not the planner's, which shifts). */
        public static readonly BuildingType[] BuildOrder = {
            BuildingType.HOUSE, BuildingType.FARM, BuildingType.STOREHOUSE, BuildingType.WORKSHOP
        };

        /**
        * The commands in the order the bar shows them (CommandBarLayout.order):
        * grouped, and within a group in a fixed order. A button that is only
        * unavailable for now -- Purchase Food with no coin, a building the
        * player cannot yet pay for -- is offered disabled rather than left
        * out, so the buttons after it do not move.
        */
        public static List<WorldCommand> getCommands(Simulation simulation) {
            List<WorldCommand> commands = new List<WorldCommand>();
            Player player = simulation.getPlayer();
            EntityRepository entityRepository = simulation.getEntityRepository();
            NationRepository nationRepository = simulation.getNationRepository();
            Inventory inventory = player.getInventory();

            if (player.getNationId() == null) {
                commands.Add(new WorldCommand("Create Nation", CommandGroup.Nation, () =>
                    new NationCreateCommand(nationRepository, simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                        .execute(player)));
                commands.Add(new WorldCommand("Join Nation", CommandGroup.Nation, () =>
                    new NationJoinCommand(nationRepository, simulation.getEventProducer(), simulation.getRandom())
                        .execute(player)));
            }
            else {
                Nation nation = nationRepository.getNation(player.getNationId());

                commands.Add(new WorldCommand("Leave Nation", CommandGroup.Drastic, () =>
                    new NationLeaveCommand(nationRepository, simulation.getEventProducer(), entityRepository)
                        .execute(player)));

                // offered to a leader with no settlement yet, and enabled once
                // they carry the wood
                if (player.getId() == nation.getLeaderId() && nation.getNumberOfSettlements() == 0) {
                    commands.Add(new WorldCommand("Found Settlement", CommandGroup.Nation, () =>
                        new FoundSettlementCommand(nationRepository, simulation.getEventProducer(), entityRepository, simulation.getGameConfig(), simulation.getRandom())
                            .execute(player))
                        .setEnabled(inventory.getNumItems(ItemType.WOOD) >= Settlement.WOOD_COST_TO_BUILD));
                }

                if (!player.isCurrentlyInSettlement() && player.getHomeSettlementId() != null) {
                    commands.Add(new WorldCommand("Teleport Home", CommandGroup.Place, () =>
                        new TeleportHomeCommand(entityRepository, simulation.getRandom())
                            .execute(player)));
                }

                if (player.isCurrentlyInSettlement()) {
                    Settlement settlement = (Settlement) entityRepository.getEntity(player.getCurrentSettlementId());
                    if (settlement.getNationId() == player.getNationId()) {
                        addOwnSettlementCommands(simulation, nation, settlement, commands);
                    }
                }
            }

            addBanditCommands(simulation, commands);
            addPawnCommands(simulation, commands);
            addDiplomacyCommands(simulation, commands);
            CaravanCommands.addCommands(simulation, commands);
            addCraftingCommands(simulation, commands);

            if (!player.isCurrentlyInSettlement()) {
                // Enter Settlement is always the first place button out in the
                // world, disabled until a settlement is within reach, so it
                // cannot appear in the slot another button was in
                Settlement nearestSettlement = (Settlement) simulation.getEnvironment().getNearestEntityOfType(player.getPosition(), EntityType.SETTLEMENT);
                bool inReach = nearestSettlement != null
                    && (int) Vector3.Distance(player.getPosition(), nearestSettlement.getPosition()) < EnterSettlementDistance;
                commands.Insert(0, new WorldCommand("Enter Settlement", CommandGroup.Place, () =>
                    new EnterSettlementCommand(entityRepository).execute(player, nearestSettlement))
                    .setEnabled(inReach));

                commands.Add(new WorldCommand("Plant Sapling", CommandGroup.Place, () =>
                    new PlantSaplingCommand(entityRepository, simulation.getRandom(), simulation.getTickCounter()).execute(player))
                    .setEnabled(inventory.getNumItems(ItemType.SAPLING) > 0));
            }
            else {
                commands.Insert(0, new WorldCommand("Exit Settlement", CommandGroup.Place, () =>
                    new ExitSettlementCommand(entityRepository).execute(player)));

                commands.Add(new WorldCommand("Purchase Food", CommandGroup.Place, () =>
                    new PurchaseFoodFromMarketCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(inventory.getNumItems(ItemType.COIN) > 0));

                bool hasResources = inventory.getNumItems(ItemType.WOOD) > 0 || inventory.getNumItems(ItemType.STONE) > 0
                    || inventory.getNumItems(ItemType.PLANKS) > 0 || inventory.getNumItems(ItemType.BRICKS) > 0;
                commands.Add(new WorldCommand("Sell Resources", CommandGroup.Place, () =>
                    new SellResourcesAtMarketCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(hasResources));
            }

            return CommandBarLayout.order(commands);
        }

        /** The buttons for a settlement of the player's own nation that they are standing in. */
        private static void addOwnSettlementCommands(Simulation simulation, Nation nation, Settlement settlement, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            EntityRepository entityRepository = simulation.getEntityRepository();
            NationRepository nationRepository = simulation.getNationRepository();
            Market market = settlement.getMarket();
            bool leads = player.getId() == nation.getLeaderId();

            // with the pawn buttons, last on the bar, so its coming and going moves nothing (#353)
            FeedResidentsCommand feed = new FeedResidentsCommand(entityRepository);
            int hungryHere = feed.countHungryHere(player);
            if (hungryHere > 0) {
                commands.Add(new WorldCommand("Feed Residents (" + hungryHere + " hungry)", CommandGroup.People, () => feed.execute(player))
                    .setEnabled(FoodItems.hasFood(player.getInventory())));
            }

            if (leads && market.getNumStalls() < market.getMaxNumStalls()) {
                commands.Add(new WorldCommand("Build Stall", CommandGroup.Build, () =>
                    new BuildStallCommand(nationRepository, entityRepository, simulation.getEventProducer()).execute(player))
                    .setEnabled(Stall.canAffordToBuild(player.getInventory()))
                    .requireConfirm());
            }

            // one button per building the settlement has room for, always in
            // the same order, enabled when the player can pay for it
            foreach (BuildingType buildingType in BuildOrder) {
                if (settlement.canAddBuilding(buildingType)) {
                    BuildingType typeToBuild = buildingType;
                    commands.Add(new WorldCommand(buildLabel(buildingType), CommandGroup.Build, () =>
                        new BuildBuildingCommand(nationRepository, entityRepository).execute(player, typeToBuild))
                        .setEnabled(BuildingCatalog.canAfford(player.getInventory(), settlement.getInventory(), buildingType))
                        .requireConfirm());
                }
            }

            if (nation.getRole(player.getId()) == NationRole.SERF
                    && player.getInventory().getNumItems(ItemType.COIN) >= Stall.COIN_COST_TO_PURCHASE
                    && market.getNumStallsForSale() > 0) {
                commands.Add(new WorldCommand("Purchase Stall", CommandGroup.Stall, () =>
                    new PurchaseStallCommand(nationRepository, entityRepository).execute(player)));
            }

            Stall stall = market.getStall(player.getId());
            if (stall != null) {
                Inventory stallInventory = stall.getInventory();

                commands.Add(new WorldCommand("Transfer Items", CommandGroup.Stall, () =>
                    new TransferItemsToStallCommand(nationRepository, entityRepository).execute(player)));

                int numCoins = stallInventory.getNumItems(ItemType.COIN);
                commands.Add(new WorldCommand("Collect Coins (" + numCoins + ")", CommandGroup.Stall, () =>
                    new CollectProfitFromStallCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(numCoins > 0));

                int numApples = stallInventory.getNumItems(ItemType.APPLE);
                commands.Add(new WorldCommand("Collect Food (" + numApples + ")", CommandGroup.Stall, () =>
                    new CollectFoodFromStallCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(numApples > 0));
            }

            if (leads) {
                commands.Add(new WorldCommand("Withdraw Funds", CommandGroup.Nation, () =>
                    new WithdrawSettlementFundsCommand(nationRepository, entityRepository).execute(player)));

                int stockpile = CollectSettlementStockpileCommand.getStockpileSize(settlement);
                commands.Add(new WorldCommand("Collect Stockpile (" + stockpile + ")", CommandGroup.Nation, () =>
                    new CollectSettlementStockpileCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(stockpile > 0));
            }
        }

        /** "Build Farm" */
        public static string buildLabel(BuildingType type) {
            return "Build " + BuildingCatalog.getDisplayName(type);
        }

        /**
        * A bandit camp the player barred the gates against can still be
        * dealt with while it stands: one button per camp outside the
        * settlements of the nation the player leads, opening the world event
        * sheet again on that camp (drive them off, pay them, or keep waiting).
        */
        private static void addBanditCommands(Simulation simulation, List<WorldCommand> commands) {
            WorldEvents events = simulation.getWorldEvents();
            if (events == null || events.isAwaitingPlayer()) {
                return;
            }
            foreach (Settlement settlement in events.getStandingBanditCamps(simulation)) {
                EntityId settlementId = settlement.getId();
                commands.Add(new WorldCommand(dealWithBanditsLabel(settlement), CommandGroup.Trade, () => events.offerStandingBanditCamp(simulation, settlementId)));
            }
        }

        /** "Deal with Bandits (Oakford)" */
        public static string dealWithBanditsLabel(Settlement settlement) {
            return "Deal with Bandits (" + settlement.getDisplayName() + ")";
        }

        /**
        * Standing in another nation's land, a leader can deal with it on the
        * spot: a gift, an alliance, peace, war, a denunciation -- the same
        * actions, by the same rules (NationDiplomacy), as the nations screen
        * (O). An alliance is not offered while the two are at war or at odds,
        * since it cannot be accepted there; Offer Peace is.
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
                commands.Add(new WorldCommand("Gift " + owner.getName() + " " + Diplomacy.PLAYER_GIFT_COINS + " Coins", CommandGroup.Trade, () =>
                    new GiftNationCommand(nationRepository, diplomacy, simulation.getRealm()).execute(player, owner)));
            }
            Realm realm = simulation.getRealm();
            if (NationDiplomacy.canProposeAlliance(nationRepository, realm, own, owner)) {
                commands.Add(new WorldCommand("Propose Alliance to " + owner.getName(), CommandGroup.Trade, () =>
                    new ProposeAllianceCommand(nationRepository, realm).execute(player, owner)));
            }
            if (OfferPeaceCommand.isApplicable(nationRepository, realm, own, owner)) {
                commands.Add(new WorldCommand("Offer Peace to " + owner.getName(), CommandGroup.Trade, () =>
                    new OfferPeaceCommand(nationRepository, realm).execute(player, owner)));
            }
            commands.Add(new WorldCommand("Denounce " + owner.getName(), CommandGroup.Drastic, () =>
                new DenounceNationCommand(nationRepository, diplomacy).execute(player, owner)));
            // after Denounce, which is always there, so war coming or going
            // (declared, or peace made) moves nothing on the red row
            if (NationDiplomacy.canDeclareWar(realm, own, owner)) {
                commands.Add(new WorldCommand("Declare War on " + owner.getName(), CommandGroup.Drastic, () =>
                    new DeclareWarCommand(nationRepository, realm).execute(player, owner)));
            }
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
                    commands.Add(new WorldCommand("Craft " + recipe.getName(), CommandGroup.Craft, () =>
                        new CraftItemCommand(entityRepository).execute(player, toCraft)));
                    continue;
                }
                if (settlement != null) {
                    int price = settlement.getMarket().getPriceToBuy(player, tool);
                    if (price >= 0 && inventory.getNumItems(ItemType.COIN) >= price) {
                        ItemType toBuy = tool;
                        commands.Add(new WorldCommand("Buy " + recipe.getName() + " (" + price + ")", CommandGroup.Craft, () =>
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

            commands.Add(new WorldCommand("Talk to " + PawnRelations.describeWithTraits(pawn, player), CommandGroup.People, () =>
                new TalkToPawnCommand(nationRepository, eventProducer, simulation.getTickCounter()).execute(player, pawn)));

            if (GiftPawnCommand.canGift(player)) {
                commands.Add(new WorldCommand("Gift " + pawn.getName(), CommandGroup.People, () =>
                    new GiftPawnCommand(eventProducer).execute(player, pawn)));
            }

            if (InvitePawnToNationCommand.isApplicable(player, pawn)) {
                commands.Add(new WorldCommand("Invite " + pawn.getName(), CommandGroup.People, () =>
                    new InvitePawnToNationCommand(nationRepository, eventProducer, entityRepository).execute(player, pawn)));
            }

            if (LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                if (pawn.getOrder() == null) {
                    commands.Add(new WorldCommand("Order " + pawn.getName() + ": Gather Wood", CommandGroup.People, () =>
                        new OrderPawnToGatherCommand(nationRepository).execute(player, pawn)));
                }
                if (LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository)) {
                    commands.Add(new WorldCommand("Appoint " + pawn.getName() + " Merchant", CommandGroup.People, () =>
                        new AppointMerchantCommand(nationRepository, entityRepository, eventProducer).execute(player, pawn)));
                }
            }
        }
    }
}
