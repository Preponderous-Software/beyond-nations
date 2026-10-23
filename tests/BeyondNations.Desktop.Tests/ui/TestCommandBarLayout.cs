using System.Collections.Generic;
using System.Numerics;
using Xunit;

using beyondnations;
using beyondnations.desktop.input;
using beyondnations.desktop.ui;
using beyondnations.desktop.ui.screens;

namespace beyondnationstests.desktop.ui {

    /**
    * The command bar keeps its buttons still (playtest: a caravan came into
    * range and the click meant for Enter Settlement raided it; a Buy Axe
    * button pushed Exit Settlement under the cursor aimed at Purchase Food;
    * two Build buttons swapped), fits the window without running under the
    * right-hand column, and asks twice before anything that cannot be
    * undone. The build key builds what the settlement box says it will, and
    * also asks twice.
    */
    public class TestCommandBarLayout {

        // ProggyClean, the ImGui default font: 7 pixels a character, 13 high
        private static float measure(string label) {
            return label.Length * 7f;
        }

        private const float FrameHeight = 19f;

        private static CommandBarLayout.Settings settingsFor(float width, float height, float reserve, bool touch, float uiScale,
                out WorldHud.Layout layout, out bool stackCentre) {
            layout = WorldHud.Layout.compute(width, height, uiScale);
            stackCentre = WorldHud.isStackCentre(width, layout, touch);
            float bottom = height - reserve;
            return WorldHud.barSettings(width, WorldHud.getBarRight(width, layout, stackCentre), bottom, layout,
                uiScale, FrameHeight * uiScale, 8f * uiScale, 4f * uiScale, 8f * uiScale);
        }

        private static WorldCommand command(string label, CommandGroup group) {
            return new WorldCommand(label, group, () => { });
        }

        /** About the most a leader standing in their own town is offered at once. */
        private static List<WorldCommand> busySettlement() {
            List<WorldCommand> commands = new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Withdraw Funds", CommandGroup.Nation),
                command("Collect Stockpile (41)", CommandGroup.Nation),
                command("Exit Settlement", CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
                command("Build Stall", CommandGroup.Build).requireConfirm(),
                command("Build House", CommandGroup.Build).requireConfirm(),
                command("Build Farm", CommandGroup.Build).requireConfirm(),
                command("Build Storehouse", CommandGroup.Build).requireConfirm(),
                command("Build Workshop", CommandGroup.Build).requireConfirm(),
                command("Transfer Items", CommandGroup.Stall),
                command("Collect Coins (12)", CommandGroup.Stall),
                command("Collect Food (3)", CommandGroup.Stall),
                command("Craft Pickaxe", CommandGroup.Craft),
                command("Buy Axe (327)", CommandGroup.Craft),
                // the locked caravan button and the war/peace levers #361 adds
                command("Send Caravan (needs Storehouse)", CommandGroup.Trade),
                command("Offer Peace to Newham", CommandGroup.Trade),
                command("Declare War on Newham", CommandGroup.Drastic),
                command("Denounce Newham", CommandGroup.Drastic),
            };
            return CommandBarLayout.order(commands);
        }

        private static Dictionary<string, CommandBarLayout.Rect> place(List<WorldCommand> commands, CommandBarLayout.Settings settings) {
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
            Dictionary<string, CommandBarLayout.Rect> byLabel = new Dictionary<string, CommandBarLayout.Rect>();
            for (int i = 0; i < commands.Count; i++) {
                byLabel[commands[i].getLabel()] = result.Buttons[i];
            }
            return byLabel;
        }

        [Fact]
        public void theSameCommandsAlwaysGoInTheSamePlaces() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            Dictionary<string, CommandBarLayout.Rect> first = place(busySettlement(), settings);
            Dictionary<string, CommandBarLayout.Rect> second = place(busySettlement(), settings);
            Assert.Equal(first, second);
        }

        [Fact]
        public void aHostileCommandComingIntoRangeMovesNothing_AndSitsApartBelowTheRest() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> road = CommandBarLayout.order(new List<WorldCommand> {
                command("Leave Nation", CommandGroup.Drastic),
                command("Enter Settlement", CommandGroup.Place),
                command("Teleport Home", CommandGroup.Place),
                command("Plant Sapling", CommandGroup.Place),
            });
            Dictionary<string, CommandBarLayout.Rect> before = place(road, settings);

            List<WorldCommand> withCaravan = new List<WorldCommand>(road) {
                command("Buy stone (12)", CommandGroup.Trade),
                command("Sell to Caravan", CommandGroup.Trade),
                command("Escort Caravan", CommandGroup.Trade),
                command("Raid Caravan", CommandGroup.Drastic),
            };
            Dictionary<string, CommandBarLayout.Rect> after = place(CommandBarLayout.order(withCaravan), settings);

            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
            CommandBarLayout.Rect raid = after["Raid Caravan"];
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in after) {
                if (entry.Key != "Raid Caravan" && entry.Key != "Leave Nation") {
                    Assert.True(entry.Value.Bottom < raid.Y, entry.Key + " should be on a row above the raid");
                }
            }
        }

        [Fact]
        public void aBenignCommandAppearingInTheMarketDoesNotMoveExitOrPurchaseFood() {
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> market = CommandBarLayout.order(new List<WorldCommand> {
                command("Exit Settlement", CommandGroup.Place),
                command("Purchase Food", CommandGroup.Place),
                command("Sell Resources", CommandGroup.Place),
            });
            Dictionary<string, CommandBarLayout.Rect> before = place(market, settings);
            List<WorldCommand> withAxe = new List<WorldCommand>(market) { command("Buy Axe (327)", CommandGroup.Craft) };
            Dictionary<string, CommandBarLayout.Rect> after = place(CommandBarLayout.order(withAxe), settings);
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
        }

        [Fact]
        public void armingAConfirmedButtonOrACountTickingUpDoesNotNudgeItsNeighbours() {
            CommandBarLayout.Settings settings = settingsFor(1280, 720, 0, false, 1f, out _, out _);
            WorldCommand raid = command("Raid Caravan", CommandGroup.Drastic);
            Assert.True(raid.needsConfirm());
            Assert.True(CommandBarLayout.buttonWidth(raid, measure, settings) >= measure(CommandBarLayout.getArmedLabel(raid)) + settings.TextPadding);

            Dictionary<string, CommandBarLayout.Rect> nine = place(new List<WorldCommand> {
                command("Collect Stockpile (9)", CommandGroup.Nation), command("Exit Settlement", CommandGroup.Place) }, settings);
            Dictionary<string, CommandBarLayout.Rect> ten = place(new List<WorldCommand> {
                command("Collect Stockpile (10)", CommandGroup.Nation), command("Exit Settlement", CommandGroup.Place) }, settings);
            Assert.Equal(nine["Exit Settlement"], ten["Exit Settlement"]);
        }

        public static IEnumerable<object[]> Screens() {
            // width, height, touch band, touch, UI scale
            yield return new object[] { 960f, 540f, 0f, false, 1f };
            yield return new object[] { 1280f, 720f, 0f, false, 1f };
            yield return new object[] { 1280f, 720f, 0f, false, 1.5f };
            // phones as the web build sees them (#325): CSS pixels, with the
            // controls' band main.js reserves at the bottom
            yield return new object[] { 844f, 390f, 150f, true, 1f };
            yield return new object[] { 390f, 844f, 220f, true, 1f };
        }

        [Theory]
        [MemberData(nameof(Screens))]
        public void theBarFitsTheWindowAndStaysClearOfTheRightColumn(float width, float height, float reserve, bool touch, float uiScale) {
            CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out WorldHud.Layout layout, out bool stackCentre);
            List<WorldCommand> commands = busySettlement();
            CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);

            Assert.True(result.Top >= 0f, "the bar runs off the top: " + result.Top);
            // the right-hand column runs to the bottom beside the bar, except
            // on a phone held upright, where it sits at the top
            CommandBarLayout.Rect rightColumn = new CommandBarLayout.Rect {
                X = layout.RightX, Y = layout.Margin, Width = layout.BoxWidth, Height = height - reserve - 2f * layout.Margin
            };
            for (int i = 0; i < commands.Count; i++) {
                CommandBarLayout.Rect button = result.Buttons[i];
                string label = commands[i].getLabel();
                Assert.True(button.X >= 0f && button.Right <= width, label + " runs off the side");
                Assert.True(button.Y >= 0f && button.Bottom <= height - reserve, label + " runs off the bottom or under the touch controls");
                if (!stackCentre) {
                    Assert.False(button.overlaps(rightColumn), label + " is under the right-hand column");
                }
                for (int j = i + 1; j < commands.Count; j++) {
                    Assert.False(button.overlaps(result.Buttons[j]), label + " overlaps " + commands[j].getLabel());
                }
            }
        }

        [Fact]
        public void aLabelTooLongForTheBarIsClippedToIt() {
            CommandBarLayout.Settings settings = settingsFor(390, 844, 220, true, 1f, out _, out _);
            WorldCommand longOne = command("Propose Alliance to The Most Serene Republic of Somewhere", CommandGroup.Trade);
            CommandBarLayout.Result result = CommandBarLayout.compute(new List<WorldCommand> { longOne }, measure, settings);
            Assert.True(result.Buttons[0].Right <= settings.Right);
        }

        [Fact]
        public void theConfirmGateRunsOnTheSecondPressOfTheSameThingInTime() {
            ConfirmGate gate = new ConfirmGate(4.0);
            Assert.False(gate.press("Raid Caravan", 10.0));
            Assert.True(gate.isArmed("Raid Caravan", 11.0));
            Assert.True(gate.press("Raid Caravan", 11.0));
            Assert.False(gate.isArmed("Raid Caravan", 11.5));

            // something else in between, or too long a wait, starts over
            Assert.False(gate.press("Raid Caravan", 20.0));
            Assert.False(gate.press("Build Farm", 20.5));
            Assert.False(gate.press("Raid Caravan", 21.0));
            Assert.False(gate.press("Raid Caravan", 30.0));
        }

        [Fact]
        public void hostileAndIrreversibleCommandsAskTwice() {
            Assert.True(command("Denounce Newham", CommandGroup.Drastic).needsConfirm());
            Assert.False(command("Purchase Food", CommandGroup.Place).needsConfirm());
            Assert.Equal("Click again to confirm: Raid Caravan.", WorldHud.getConfirmPrompt(command("Raid Caravan", CommandGroup.Drastic), false));
            Assert.Equal("Tap again to confirm: Raid Caravan.", WorldHud.getConfirmPrompt(command("Raid Caravan", CommandGroup.Drastic), true));
        }

        // ---- against a real world ----

        private static Simulation inOwnCamp(out Settlement settlement) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            player.getInventory().addItem(ItemType.WOOD, Settlement.WOOD_COST_TO_BUILD);
            new FoundSettlementCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getEntityRepository(), simulation.getGameConfig(), simulation.getRandom())
                .execute(player);
            settlement = (Settlement) simulation.getEntityRepository().getEntity(player.getHomeSettlementId());
            new EnterSettlementCommand(simulation.getEntityRepository()).execute(player, settlement);
            Assert.True(player.isCurrentlyInSettlement());
            Inventory carried = player.getInventory();
            foreach (ItemType type in new[] { ItemType.WOOD, ItemType.STONE, ItemType.COIN }) {
                carried.removeItem(type, carried.getNumItems(type));
                settlement.getInventory().removeItem(type, settlement.getInventory().getNumItems(type));
            }
            return simulation;
        }

        private static WorldCommand find(Simulation simulation, string label) {
            return WorldCommandBar.getCommands(simulation).Find(c => c.getLabel() == label);
        }

        private static List<string> labels(Simulation simulation) {
            return WorldCommandBar.getCommands(simulation).ConvertAll(c => c.getLabel());
        }

        [Fact]
        public void inASettlement_ExitPurchaseAndSellAreAlwaysThereInTheSameOrder() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            List<string> broke = labels(simulation);
            Assert.False(find(simulation, "Purchase Food").isEnabled());
            Assert.False(find(simulation, "Sell Resources").isEnabled());

            player.getInventory().addItem(ItemType.COIN, 50);
            player.getInventory().addItem(ItemType.WOOD, 5);
            Assert.True(find(simulation, "Purchase Food").isEnabled());
            Assert.True(find(simulation, "Sell Resources").isEnabled());
            Assert.Equal(broke, labels(simulation));

            List<string> order = labels(simulation);
            Assert.True(order.IndexOf("Exit Settlement") < order.IndexOf("Purchase Food"));
            Assert.True(order.IndexOf("Purchase Food") < order.IndexOf("Sell Resources"));
        }

        [Fact]
        public void buildButtonsKeepOneOrder_AreDisabledUntilAffordable_AndAskTwice() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();

            WorldCommand house = find(simulation, "Build House");
            WorldCommand farm = find(simulation, "Build Farm");
            Assert.NotNull(house);
            Assert.NotNull(farm);
            Assert.False(house.isEnabled());
            Assert.True(house.needsConfirm());
            List<string> before = labels(simulation);
            Assert.True(before.IndexOf("Build House") < before.IndexOf("Build Farm"));

            // the planner now wants a house first; the buttons stay put
            settlement.addBuilding(BuildingType.FARM);
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            List<string> after = labels(simulation);
            Assert.True(after.IndexOf("Build House") < after.IndexOf("Build Farm"));
            Assert.True(find(simulation, "Build House").isEnabled());
        }

        [Fact]
        public void outInTheWorld_EnterSettlementHoldsItsPlaceWhenOutOfReach() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            new ExitSettlementCommand(simulation.getEntityRepository()).execute(player);
            player.setPosition(settlement.getPosition());
            List<string> near = labels(simulation);
            Assert.True(find(simulation, "Enter Settlement").isEnabled());

            player.setPosition(settlement.getPosition() + new System.Numerics.Vector3(0, 0, 400));
            Assert.False(find(simulation, "Enter Settlement").isEnabled());
            Assert.Equal(near.IndexOf("Enter Settlement"), labels(simulation).IndexOf("Enter Settlement"));
        }

        [Fact]
        public void leaveNationIsDrastic() {
            Simulation simulation = inOwnCamp(out _);
            WorldCommand leave = find(simulation, "Leave Nation");
            Assert.Equal(CommandGroup.Drastic, leave.getGroup());
            Assert.True(leave.needsConfirm());
        }

        /** Standing in a hostile neighbour's land as a leader (--stage-diplomacy): gift, peace, war, denounce. */
        private static Simulation inForeignLand(out string ownerName) {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            DiplomacyShowcase.Staged staged = DiplomacyShowcase.stage(simulation);
            simulation.getPlayer().setPosition(staged.hostileSettlement.getPosition() + new Vector3(30, 0, 0));
            ownerName = simulation.getNationRepository().getNation(staged.hostileSettlement.getNationId()).getName();
            return simulation;
        }

        [Fact]
        public void inForeignLand_WarAndDenounceAreDrastic_PeaceIsNot() {
            Simulation simulation = inForeignLand(out string name);
            WorldCommand war = find(simulation, "Declare War on " + name);
            WorldCommand peace = find(simulation, "Offer Peace to " + name);
            WorldCommand denounce = find(simulation, "Denounce " + name);
            Assert.NotNull(war);
            Assert.NotNull(peace);
            Assert.Equal(CommandGroup.Drastic, war.getGroup());
            Assert.True(war.needsConfirm());
            Assert.Equal(CommandGroup.Drastic, denounce.getGroup());
            Assert.True(denounce.needsConfirm());
            Assert.Equal(CommandGroup.Trade, peace.getGroup());
            Assert.False(peace.needsConfirm());
        }

        [Fact]
        public void inForeignLand_WarBecomingAvailableMovesNoOtherButton() {
            Simulation simulation = inForeignLand(out string name);
            CommandBarLayout.Settings settings = settingsFor(960, 540, 0, false, 1f, out _, out _);
            List<WorldCommand> all = WorldCommandBar.getCommands(simulation);
            List<WorldCommand> withoutWar = all.FindAll(c => c.getLabel() != "Declare War on " + name);
            Assert.Equal(all.Count - 1, withoutWar.Count);
            Dictionary<string, CommandBarLayout.Rect> before = place(withoutWar, settings);
            Dictionary<string, CommandBarLayout.Rect> after = place(all, settings);
            foreach (KeyValuePair<string, CommandBarLayout.Rect> entry in before) {
                Assert.Equal(entry.Value, after[entry.Key]);
            }
        }

        [Fact]
        public void inTheLeadersOwnCamp_TheLockedSendCaravanSitsWithTheTradeButtons() {
            Simulation simulation = inOwnCamp(out _);
            WorldCommand send = WorldCommandBar.getCommands(simulation).Find(c => c.getLabel().StartsWith(CaravanCommands.SendCaravanLabel));
            Assert.NotNull(send);
            Assert.NotEqual(CaravanCommands.SendCaravanLabel, send.getLabel());
            Assert.Equal(CommandGroup.Trade, send.getGroup());
            // after every place button, so it cannot move Exit, Purchase Food or Sell Resources
            List<string> order = labels(simulation);
            Assert.True(order.IndexOf("Sell Resources") < order.IndexOf(send.getLabel()));
        }

        [Theory]
        [MemberData(nameof(Screens))]
        public void theRealBarsFitTheWindowAndStayClearOfTheRightColumn(float width, float height, float reserve, bool touch, float uiScale) {
            Simulation camp = inOwnCamp(out _);
            camp.getPlayer().getInventory().addItem(ItemType.COIN, 500);
            camp.getPlayer().getInventory().addItem(ItemType.WOOD, 100);
            camp.getPlayer().getInventory().addItem(ItemType.STONE, 100);
            Simulation foreign = inForeignLand(out _);
            foreach (Simulation simulation in new[] { camp, foreign }) {
                CommandBarLayout.Settings settings = settingsFor(width, height, reserve, touch, uiScale, out WorldHud.Layout layout, out bool stackCentre);
                List<WorldCommand> commands = WorldCommandBar.getCommands(simulation);
                CommandBarLayout.Result result = CommandBarLayout.compute(commands, measure, settings);
                Assert.True(result.Top >= 0f, "the bar runs off the top: " + result.Top);
                CommandBarLayout.Rect rightColumn = new CommandBarLayout.Rect {
                    X = layout.RightX, Y = layout.Margin, Width = layout.BoxWidth, Height = height - reserve - 2f * layout.Margin
                };
                for (int i = 0; i < commands.Count; i++) {
                    CommandBarLayout.Rect button = result.Buttons[i];
                    string label = commands[i].getLabel();
                    Assert.True(button.X >= 0f && button.Right <= width, label + " runs off the side");
                    Assert.True(button.Y >= 0f && button.Bottom <= height - reserve, label + " runs off the bottom or under the touch controls");
                    if (!stackCentre) {
                        Assert.False(button.overlaps(rightColumn), label + " is under the right-hand column");
                    }
                    for (int j = i + 1; j < commands.Count; j++) {
                        Assert.False(button.overlaps(result.Buttons[j]), label + " overlaps " + commands[j].getLabel());
                    }
                }
            }
        }

        [Fact]
        public void theLeaveKeyAsksBeforeDisbanding_AndDisbandsOnTheSecondPress() {
            GameConfig config = new GameConfig();
            config.setWorldSeed(42);
            Simulation simulation = new Simulation(config);
            Player player = simulation.getPlayer();
            new NationCreateCommand(simulation.getNationRepository(), simulation.getEventProducer(), simulation.getRandom(), simulation.getNationNameGenerator())
                .execute(player);
            Nation nation = simulation.getNationRepository().getNation(player.getNationId());
            LeaveNationKey key = new LeaveNationKey();

            // playtest 0.6.0 s09: one press of L disbanded the nation (#368)
            Assert.False(key.press(simulation, 1.0));
            Assert.Equal(nation.getId(), player.getNationId());
            Assert.Equal("Press L again to disband " + nation.getName() + ".", player.getStatus().getStatus());

            // too late: asks again
            Assert.False(key.press(simulation, 1.0 + ConfirmGate.DefaultWindowSeconds + 1.0));
            Assert.NotNull(player.getNationId());

            Assert.True(key.press(simulation, 6.5));
            Assert.Null(player.getNationId());

            // with no nation, the refusal shows at once
            Assert.True(key.press(simulation, 7.0));
            Assert.Equal("You are not a member of a nation.", player.getStatus().getStatus());
        }

        [Fact]
        public void theBuildKeyBuildsWhatTheSettlementBoxNames_NotSomethingCheaper() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            Assert.Equal(BuildingType.FARM, BuildingPlanner.suggestNext(settlement));
            Assert.Equal(BuildingType.FARM, BuildKey.getTarget(simulation));

            // enough for a House (20/15) but not the Farm the box names (25/10)
            player.getInventory().addItem(ItemType.WOOD, 20);
            player.getInventory().addItem(ItemType.STONE, 15);
            BuildKey key = new BuildKey();
            Assert.False(key.press(simulation, 1.0));
            Assert.False(key.press(simulation, 1.5));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Equal(20, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.StartsWith("A Farm needs", player.getStatus().getStatus());
        }

        [Fact]
        public void theBuildKeyAsksBeforeSpending_AndBuildsOnTheSecondPress() {
            Simulation simulation = inOwnCamp(out Settlement settlement);
            Player player = simulation.getPlayer();
            player.getInventory().addItem(ItemType.WOOD, 30);
            player.getInventory().addItem(ItemType.STONE, 20);
            BuildKey key = new BuildKey();

            Assert.False(key.press(simulation, 1.0));
            Assert.Equal(0, settlement.getNumBuildings());
            Assert.Equal(30, player.getInventory().getNumItems(ItemType.WOOD));
            Assert.Equal(BuildKey.getConfirmPrompt(BuildingType.FARM), player.getStatus().getStatus());
            Assert.Equal("Press K again to build a Farm (25 wood, 10 stone).", BuildKey.getConfirmPrompt(BuildingType.FARM));

            Assert.True(key.press(simulation, 2.0));
            Assert.Equal(1, settlement.getNumBuildings(BuildingType.FARM));

            // a press long after the last one asks again
            player.getInventory().addItem(ItemType.WOOD, 100);
            player.getInventory().addItem(ItemType.STONE, 100);
            Assert.False(key.press(simulation, 3.0));
            Assert.False(key.press(simulation, 30.0));
            Assert.Equal(1, settlement.getNumBuildings());
        }
    }
}
