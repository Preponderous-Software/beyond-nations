using System.Collections.Generic;
using System.Numerics;
using beyondnations.desktop.ui.screens;

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

        /** Opens the settlement screen from inside a settlement (#178). */
        public const string SettlementLabel = "Settlement";

        /** Why Purchase Food, Sell Resources and Trade at Market are greyed in a settlement with no market stalls. */
        public const string NoStallsReason = "This settlement has no stalls yet.";

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

                // offered to a leader with no settlement yet, and again (#145)
                // out in the wilds; enabled once the era lets the nation hold
                // another and they carry what it costs (FoundingCost). At the
                // era's cap it stays, greyed, saying which era lifts it
                // (0.8.0 playtest s07: it vanished, and only F told why)
                if (player.getId() == nation.getLeaderId()
                        && (nation.getNumberOfSettlements() == 0 || !player.isCurrentlyInSettlement())) {
                    int woodShort = FoundingCost.wood(nation) - inventory.getNumItems(ItemType.WOOD);
                    int stoneShort = FoundingCost.stone(nation) - inventory.getNumItems(ItemType.STONE);
                    bool capped = !nation.canFoundSettlement();
                    bool affords = woodShort <= 0 && stoneShort <= 0;
                    // --- #503 --- greyed, too, on another nation's land or too
                    // near a settlement, with the command's own reason in the
                    // order it checks: cap, cost, then the ground
                    string notHere = capped || !affords ? null
                        : SettlementSites.describeWhyNotHere(entityRepository, nationRepository, player.getPosition(),
                            simulation.getGameConfig().getMinDistanceBetweenSettlements(), nation.getId());
                    commands.Add(new WorldCommand("Found Settlement", CommandGroup.Nation, () =>
                        new FoundSettlementCommand(nationRepository, simulation.getEventProducer(), entityRepository, simulation.getGameConfig(), simulation.getRandom())
                            .execute(player))
                        .setEnabled(!capped && affords && notHere == null,
                            capped ? EraRules.describeSettlementCap(nation.getSettlementLimit())
                                : !affords ? describeShortfall(System.Math.Max(0, woodShort), System.Math.Max(0, stoneShort))
                                : notHere));
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
            addEatCommand(simulation, commands);

            if (!player.isCurrentlyInSettlement()) {
                // Enter Settlement is always the first place button out in the
                // world, disabled until a settlement is within reach, so it
                // cannot appear in the slot another button was in
                Settlement nearestSettlement = (Settlement) simulation.getEnvironment().getNearestEntityOfType(player.getPosition(), EntityType.SETTLEMENT);
                bool inReach = nearestSettlement != null
                    && (int) Vector3.Distance(player.getPosition(), nearestSettlement.getPosition()) < EnterSettlementDistance;
                commands.Insert(0, new WorldCommand("Enter Settlement", CommandGroup.Place, () =>
                    new EnterSettlementCommand(entityRepository).execute(player, nearestSettlement))
                    .setEnabled(inReach, describeOutOfReach(player, nearestSettlement)));

                commands.Add(new WorldCommand("Plant Sapling", CommandGroup.Place, () =>
                    new PlantSaplingCommand(entityRepository, simulation.getRandom(), simulation.getTickCounter()).execute(player))
                    .setEnabled(inventory.getNumItems(ItemType.SAPLING) > 0, "You have no saplings."));
            }
            else {
                commands.Insert(0, new WorldCommand("Exit Settlement", CommandGroup.Place, () =>
                    new ExitSettlementCommand(entityRepository).execute(player)));

                // the settlement screen again (#178): who holds it, its stores and market
                EntityId here = player.getCurrentSettlementId();
                commands.Insert(1, new WorldCommand(SettlementLabel, CommandGroup.Place, () =>
                    SettlementScreen.requestOpen(here)));

                // a settlement with no stalls has no one to buy from or sell to:
                // the market buttons are greyed with that as the reason, rather
                // than enabled and answering "This settlement does not have any
                // stalls." when pressed (playtest 0.7.1 winter s03)
                Settlement marketHere = entityRepository.getEntity(here) as Settlement;
                bool hasStalls = marketHere != null && marketHere.getMarket() != null && marketHere.getMarket().getNumStalls() > 0;

                commands.Add(new WorldCommand("Purchase Food", CommandGroup.Place, () =>
                    new PurchaseFoodFromMarketCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(hasStalls && inventory.getNumItems(ItemType.COIN) > 0,
                        hasStalls ? "You have no coins." : NoStallsReason));

                // whatever the market buys, food included (playtest 0.6.1 s03: five
                // chicken meat left the button greyed out)
                commands.Add(new WorldCommand("Sell Resources", CommandGroup.Place, () =>
                    new SellResourcesAtMarketCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(hasStalls && Market.hasAnythingToSell(inventory),
                        hasStalls ? "You are carrying nothing the market buys." : NoStallsReason));

                // the market panel (#182): pick a good, a quantity, and see the total before dealing
                commands.Add(new WorldCommand(MarketTradePanel.OpenLabel, CommandGroup.Place, () => MarketTradePanel.open(simulation))
                    .setEnabled(hasStalls, NoStallsReason));
            }

            return CommandBarLayout.order(commands);
        }

        /** The buttons for a settlement of the player's own nation that they are standing in. */
        private static void addOwnSettlementCommands(Simulation simulation, Nation nation, Settlement settlement, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            EntityRepository entityRepository = simulation.getEntityRepository();
            NationRepository nationRepository = simulation.getNationRepository();
            Market market = settlement.getMarket();

            // Always offered in the player's own settlement, with the place
            // buttons, and disabled while no one here is hungry or the player
            // has no food: playtest 0.6.2 s02 (finding 17) and 0.6.1 s02 saw it
            // come and go among the pawn buttons, so a second click on it
            // opened a pawn's dialogue. Its count is the number it will feed
            // (FeedResidentsCommand.countHungryHere, the same list as execute)
            // and is measured as three digits, so it never changes the width.
            // In autumn and winter it also stocks members short of winter
            // stores (#353), and says so when no one here is hungry.
            FeedResidentsCommand feed = new FeedResidentsCommand(entityRepository);
            Season season = simulation.getSeasonCalendar().getSeason();
            int hungryHere = feed.countHungryHere(player);
            int lowHere = feed.countLowOnStoresHere(player, season);
            commands.Add(new WorldCommand(feedResidentsLabel(hungryHere, lowHere), CommandGroup.Place, () => feed.execute(player, season))
                .setWidthLabel(FeedResidentsWidthLabel)
                // while it can act, what it does; greyed, the hover says why instead
                .setTooltip((hungryHere > 0 || lowHere > 0) && FoodItems.hasFood(player.getInventory()) ? feedResidentsTooltip(season) : null)
                .setEnabled((hungryHere > 0 || lowHere > 0) && FoodItems.hasFood(player.getInventory()),
                    hungryHere > 0 || lowHere > 0 ? "You have no food to give."
                    : FeedResidentsCommand.stocksForWinter(season) ? "No one here is hungry or short of winter stores." : "No one here is hungry."));

            // the leader, or the member it was given to (#176, SettlementOwnership)
            bool manages = SettlementOwnership.mayManage(player.getId(), nation, settlement);
            if (manages && market.getNumStalls() < market.getMaxNumStalls()) {
                Inventory carried = player.getInventory();
                commands.Add(new WorldCommand("Build Stall", CommandGroup.Build, () =>
                    new BuildStallCommand(nationRepository, entityRepository, simulation.getEventProducer()).execute(player))
                    .setEnabled(Stall.canAffordToBuild(carried), describeShortfall(
                        Stall.WOOD_COST_TO_BUILD - carried.getNumItems(ItemType.WOOD),
                        Stall.STONE_COST_TO_BUILD - carried.getNumItems(ItemType.STONE)))
                    .requireConfirm());
            }

            // one button per building the settlement has room for, always in
            // the same order, enabled when the player can pay for it
            foreach (BuildingType buildingType in BuildOrder) {
                if (settlement.canAddBuilding(buildingType)) {
                    BuildingType typeToBuild = buildingType;
                    commands.Add(new WorldCommand(buildLabel(buildingType), CommandGroup.Build, () =>
                        new BuildBuildingCommand(nationRepository, entityRepository).execute(player, typeToBuild))
                        .setEnabled(BuildingCatalog.canAfford(player.getInventory(), settlement.getInventory(), buildingType),
                            describeBuildShortfall(player.getInventory(), settlement.getInventory(), buildingType))
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
                    .setEnabled(numCoins > 0, "Your stall holds no coins."));

                // every kind of food, not just apples: the command collects the most nourishing first (#419)
                int numFood = FoodItems.countFood(stallInventory);
                commands.Add(new WorldCommand("Collect Food (" + numFood + ")", CommandGroup.Stall, () =>
                    new CollectFoodFromStallCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(numFood > 0, "Your stall holds no food."));
            }

            if (manages) {
                commands.Add(new WorldCommand("Withdraw Funds", CommandGroup.Nation, () =>
                    new WithdrawSettlementFundsCommand(nationRepository, entityRepository).execute(player)));

                int stockpile = CollectSettlementStockpileCommand.getStockpileSize(settlement);
                commands.Add(new WorldCommand("Collect Stockpile (" + stockpile + ")", CommandGroup.Nation, () =>
                    new CollectSettlementStockpileCommand(nationRepository, entityRepository).execute(player))
                    .setEnabled(stockpile > 0, "The stockpile is empty."));
            }
        }

        /** "Needs 12 more wood and 5 more stone.", naming only what is short; a count of zero or less is not. */
        private static string describeShortfall(int woodShort, int stoneShort) {
            return "Needs " + describeMissing(woodShort, stoneShort) + ".";
        }

        private static string describeMissing(int woodShort, int stoneShort) {
            string wood = woodShort + " more wood";
            string stone = stoneShort + " more stone";
            if (woodShort > 0 && stoneShort > 0) {
                return wood + " and " + stone;
            }
            return woodShort > 0 ? wood : stone;
        }

        /** What is missing for a building, which is paid for from what the player carries and the stockpile (BuildingCatalog). */
        private static string describeBuildShortfall(Inventory carried, Inventory stockpile, BuildingType type) {
            int wood = carried.getNumItems(ItemType.WOOD) + stockpile.getNumItems(ItemType.WOOD);
            int stone = carried.getNumItems(ItemType.STONE) + stockpile.getNumItems(ItemType.STONE);
            return "Needs " + describeMissing(BuildingCatalog.getWoodCost(type) - wood, BuildingCatalog.getStoneCost(type) - stone)
                + ", counting the settlement's stockpile.";
        }

        /** "The Camp of Aland is about 175 paces to the west. Walk within 50 to enter." */
        private static string describeOutOfReach(Player player, Settlement nearest) {
            if (nearest == null) {
                return "No settlement has been founded yet.";
            }
            int distance = (int) Vector3.Distance(player.getPosition(), nearest.getPosition());
            return "The " + nearest.getDisplayName() + " is about " + distance + " paces to the "
                + Compass.describe(player.getPosition(), nearest.getPosition()) + ". Walk within " + EnterSettlementDistance + " to enter.";
        }

        /** "Feed Residents (3 hungry)" */
        public static string feedResidentsLabel(int hungryHere) {
            return "Feed Residents (" + hungryHere + " hungry)";
        }

        /**
        * "Feed Residents (3 hungry)", or in autumn and winter with no one here
        * hungry but some short of winter stores, "Feed Residents (4 short for
        * winter)": it stocks members before they are hungry, and a ruler in
        * the winter2 playtest, reading "low on food", thought it fed only the
        * hungry and found no way to lay food in for members.
        */
        public static string feedResidentsLabel(int hungryHere, int lowHere) {
            if (hungryHere == 0 && lowHere > 0) {
                return "Feed Residents (" + lowHere + " short for winter)";
            }
            return feedResidentsLabel(hungryHere);
        }

        /** The longer of the two labels, so the button keeps one width whichever it shows. */
        public const string FeedResidentsWidthLabel = "Feed Residents (0 short for winter)";

        /** What Feed Residents does, on hover (and in the settlement screen's note). */
        public static string feedResidentsTooltip(Season season) {
            return FeedResidentsCommand.stocksForWinter(season)
                ? "Gives your food to the members standing here: the hungry first, then anyone short of the "
                    + SeasonalRules.WINTER_FOOD_STORE + " food each needs for winter, before they go hungry."
                : "Gives your food to the hungry members standing here.";
        }

        /** "Eat Cooked Meat (+45)", measured as the longest food's label so it keeps one width. */
        public static string eatLabel(ItemType food) {
            string name = System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(ItemNames.getSingularName(food));
            return "Eat " + name + " (+" + FoodItems.getEnergyRestored(food) + ")";
        }

        /**
        * Eat, by choice (#357): the player eats on their own only what fits
        * under a full bar, so a cooked meat waits until it can all be used.
        * The button eats now. Offered while the player carries food, and
        * disabled when the bar is full; it names what it will eat.
        */
        private static void addEatCommand(Simulation simulation, List<WorldCommand> commands) {
            Player player = simulation.getPlayer();
            if (!FoodItems.hasFood(player.getInventory())) {
                return;
            }
            ItemType? food = player.getFoodToEatNow();
            string label = food.HasValue ? eatLabel(food.Value) : "Eat";
            commands.Add(new WorldCommand(label, CommandGroup.Craft, () => player.eatNow())
                .setWidthLabel(eatLabel(ItemType.CHICKEN_MEAT))
                .setEnabled(food.HasValue, "You are full."));
        }

        /**
        * A name as long as most pawns', which the pawn buttons are measured
        * with instead of the pawn's own: each is as wide for one pawn as for
        * the next, so a different pawn stepping into reach, or the one there
        * warming to the player, moves none of the others (playtest 0.6.2 s02
        * finding 17). A longer name is shortened on the button with "...".
        */
        public const string PawnNameSlot = "Charlotte Williams";

        /** The longest relationship tier name, which the Talk button is measured with. */
        public const string TierNameSlot = "Acquaintance";

        /**
        * The most pawn buttons the bar can show for one pawn, each as wide
        * as it is measured (a leader beside a member with no order who could
        * be made merchant or trader). The bar keeps the rows these take
        * inside a settlement (CommandBarLayout.Settings.ReservedPeopleRows),
        * so residents coming and going move nothing (#486).
        */
        public static List<WorldCommand> getPawnStandIns() {
            List<WorldCommand> standIns = new List<WorldCommand>();
            foreach (string label in new string[] {
                    "Talk to " + PawnNameSlot + " (" + TierNameSlot + ")",
                    "Gift " + PawnNameSlot,
                    "Invite " + PawnNameSlot,
                    "Order " + PawnNameSlot + ": Gather Wood",
                    "Appoint " + PawnNameSlot + " Merchant",
                    "Appoint " + PawnNameSlot + " Trader" }) {
                standIns.Add(new WorldCommand(label, CommandGroup.People, () => { }));
            }
            return standIns;
        }

        /**
        * The pawn rows the bar keeps: inside a settlement, as many as the
        * stand-ins take (getPawnStandIns); out in the world, where a pawn in
        * reach is the exception, none.
        */
        public static int getReservedPeopleRows(Simulation simulation, System.Func<string, float> measure, CommandBarLayout.Settings settings) {
            if (!simulation.getPlayer().isCurrentlyInSettlement()) {
                return 0;
            }
            // at most MaxReservedPeopleRows: a window wide enough for the pawn
            // buttons in two rows keeps them all; on a phone held upright,
            // where each takes a row, keeping six empty rows would push the bar
            // up over the boxes, so only the first two are kept there
            return System.Math.Min(MaxReservedPeopleRows, CommandBarLayout.countRows(getPawnStandIns(), measure, settings));
        }

        /** The most pawn rows the bar keeps empty (getReservedPeopleRows). */
        public const int MaxReservedPeopleRows = 2;

        /** A pawn button: "Gift Ada", measured as "Gift Charlotte Williams". */
        private static WorldCommand pawnCommand(string before, Pawn pawn, string after, System.Action action) {
            return new WorldCommand(before + pawn.getName() + after, CommandGroup.People, action)
                .setWidthLabel(before + PawnNameSlot + after);
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
                new DenounceNationCommand(nationRepository, diplomacy, realm).execute(player, owner)));
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
        * levers when the player leads that pawn's nation. Talk names the pawn
        * and how it feels about the player, so the bar doubles as the
        * relationship readout for whoever is standing in front of the player.
        * Every pawn button is measured with PawnNameSlot, not the pawn's name.
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

            // The traits stay off the button (they are on the pawn's name tag
            // and in the button's tooltip): "Talk to Bob Roberts (Stranger) -
            // Glutton" made the button, and every one after it, change width
            // from pawn to pawn (playtest 0.6.2 s02 finding 17).
            commands.Add(pawnCommand("Talk to ", pawn, " (" + PawnRelations.getTierName(pawn, player) + ")", () =>
                new TalkToPawnCommand(nationRepository, eventProducer, simulation.getTickCounter()).execute(player, pawn))
                .setWidthLabel("Talk to " + PawnNameSlot + " (" + TierNameSlot + ")")
                .setTooltip("Talk to " + PawnRelations.describeWithTraits(pawn, player)));

            // Gift and Invite are always there for the pawn in reach, greyed
            // with the reason when they cannot be done (0.8.0 playtest s02:
            // next to a stranger the bar showed only the nation's buttons,
            // and inviting was a key a phone does not have). Being always
            // there, neither moves the leader's levers after them when the
            // player runs out of gifts or the pawn joins.
            bool canGift = GiftPawnCommand.canGift(player);
            WorldCommand gift = pawnCommand("Gift ", pawn, "", () =>
                new GiftPawnCommand(eventProducer, nationRepository).execute(player, pawn))
                .setEnabled(canGift,
                    "You have nothing to give. " + GiftPawnCommand.describeWhatMakesAGift());
            // a leader of another nation can be warmed but never recruited (#371): the
            // tooltip says so before the gift, the status line after it (the touch path)
            string leaderWarning = GiftPawnCommand.leaderWarning(player, pawn, nationRepository);
            if (canGift && leaderWarning.Length > 0) {
                gift.setTooltip(leaderWarning.Trim());
            }
            commands.Add(gift);

            string inviteRefusal = InvitePawnToNationCommand.whyNot(player, pawn, nationRepository);
            commands.Add(pawnCommand("Invite ", pawn, "", () =>
                new InvitePawnToNationCommand(nationRepository, eventProducer, entityRepository).execute(player, pawn))
                .setEnabled(inviteRefusal == null, inviteRefusal));

            if (LeaderPawnCommands.playerLeads(player, pawn, nationRepository)) {
                if (pawn.getOrder() == null) {
                    commands.Add(pawnCommand("Order ", pawn, ": Gather Wood", () =>
                        new OrderPawnToGatherCommand(nationRepository).execute(player, pawn)));
                }
                if (LeaderPawnCommands.canAppointMerchant(player, pawn, nationRepository, entityRepository)) {
                    commands.Add(pawnCommand("Appoint ", pawn, " Merchant", () =>
                        new AppointMerchantCommand(nationRepository, entityRepository, eventProducer).execute(player, pawn)));
                }
                // Appoint Trader is there for every member, greyed with the
                // reason when it cannot be done (#487: with the one trader
                // slot taken the button vanished and nothing said why). The
                // reason shows on hover, and on a phone a tap on the greyed
                // button puts it on the status line.
                string traderRefusal = LeaderPawnCommands.whyNotAppointTrader(player, pawn, nationRepository, entityRepository);
                commands.Add(pawnCommand("Appoint ", pawn, " Trader", () =>
                    new AppointTraderCommand(nationRepository, eventProducer, entityRepository).execute(player, pawn))
                    .setEnabled(traderRefusal == null, traderRefusal));
            }
        }
    }
}
